/**
 * @vitest-environment jsdom
 */

import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/http";
import { getPartyState, voteInParty } from "../api/partyApi";
import { setPartySession } from "../api/partySession";
import { useAppState } from "../state/AppStateContext";
import type { AppState } from "../state/AppStateContext";
import type { Movie } from "../types/movie";
import type { PartyState } from "../types/party";
import { PartyPlayPage } from "./PartyPlayPage";

vi.mock("../api/partyApi", () => ({
  getPartyState: vi.fn(),
  voteInParty: vi.fn(),
}));

vi.mock("../state/AppStateContext", () => ({
  useAppState: vi.fn(),
}));

const firstMovie: Movie = {
  id: "tt1",
  title: "First Movie",
  genres: ["Drama"],
  posterUrl: "https://example.com/first.jpg",
};
const secondMovie: Movie = {
  id: "tt2",
  title: "Second Movie",
  genres: ["Comedy"],
  posterUrl: "https://example.com/second.jpg",
};
const thirdMovie: Movie = {
  id: "tt3",
  title: "Third Movie",
  genres: ["Thriller"],
};

const playingState = (
  movies: Movie[] = [firstMovie, secondMovie],
  currentIndex = 0,
  totalMovies = 2
): PartyState => ({
  partyId: "party-1",
  joinCode: "123",
  status: "Playing",
  playerCount: 2,
  opponentPresent: true,
  opponentOnline: true,
  progress: {
    currentIndex,
    totalMovies,
    isExhausted: currentIndex >= totalMovies,
  },
  batch: movies.map((movie, offset) => ({
    orderIndex: currentIndex + offset,
    movie,
  })),
  currentMovie: movies[0] ?? null,
  matchedMovie: null,
});

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={["/party/party-1"]}>
      <Routes>
        <Route path="/party/:partyId" element={<PartyPlayPage />} />
      </Routes>
    </MemoryRouter>
  );

beforeEach(() => {
  sessionStorage.clear();
  setPartySession({ partyId: "party-1", playerToken: "token-1" });
  vi.mocked(useAppState).mockReturnValue({
    movie: null,
    isPicking: false,
    pickError: null,
  } as unknown as AppState);
  vi.mocked(getPartyState).mockResolvedValue(playingState());
  vi.mocked(voteInParty).mockResolvedValue(playingState([secondMovie], 1));
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  vi.unstubAllGlobals();
});

describe("PartyPlayPage", () => {
  it.each([
    ["Так", true],
    ["Ні", false],
  ])("%s posts a vote and advances to the returned movie", async (label, liked) => {
    renderPage();
    await screen.findByRole("heading", { name: "First Movie" });

    fireEvent.click(screen.getByRole("button", { name: label }));

    await waitFor(() =>
      expect(voteInParty).toHaveBeenCalledWith(
        "party-1",
        "token-1",
        { movieId: "tt1", liked }
      )
    );
    expect(
      await screen.findByRole("heading", { name: "Second Movie" })
    ).toBeTruthy();
  });

  it("shows cached lookahead immediately and locks voting until acknowledgement", async () => {
    const voteResult = deferred<PartyState>();
    vi.mocked(voteInParty).mockReturnValue(voteResult.promise);
    renderPage();
    await screen.findByRole("heading", { name: "First Movie" });

    fireEvent.click(screen.getByRole("button", { name: "Ні" }));

    expect(
      await screen.findByRole("heading", { name: "Second Movie" })
    ).toBeTruthy();
    const yesButton = screen.getByRole("button", { name: "Так" });
    expect((yesButton as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(yesButton);
    expect(voteInParty).toHaveBeenCalledTimes(1);

    voteResult.resolve(playingState([thirdMovie], 2, 3));
    expect(
      await screen.findByRole("heading", { name: "Third Movie" })
    ).toBeTruthy();
  });

  it("preloads poster URLs for the remaining batch", async () => {
    const preloaded: string[] = [];
    vi.stubGlobal("Image", class {
      set src(value: string) {
        preloaded.push(value);
      }
    });

    renderPage();
    await screen.findByRole("heading", { name: "First Movie" });

    await waitFor(() =>
      expect(preloaded).toContain("https://example.com/second.jpg")
    );
    expect(preloaded).not.toContain("https://example.com/first.jpg");
  });

  it("reconciles with authoritative state after a vote failure", async () => {
    vi.mocked(voteInParty).mockRejectedValue(new Error("offline"));
    vi.mocked(getPartyState)
      .mockResolvedValueOnce(playingState())
      .mockResolvedValueOnce(playingState([firstMovie], 0));
    renderPage();
    await screen.findByRole("heading", { name: "First Movie" });

    fireEvent.click(screen.getByRole("button", { name: "Ні" }));
    expect(
      await screen.findByRole("heading", { name: "Second Movie" })
    ).toBeTruthy();

    await waitFor(() => expect(getPartyState).toHaveBeenCalledTimes(2));
    expect(
      await screen.findByRole("heading", { name: "First Movie" })
    ).toBeTruthy();
  });

  it("immediately replaces an optimistic card with a terminal match", async () => {
    const voteResult = deferred<PartyState>();
    vi.mocked(voteInParty).mockReturnValue(voteResult.promise);
    renderPage();
    await screen.findByRole("heading", { name: "First Movie" });

    fireEvent.click(screen.getByRole("button", { name: "Так" }));
    await screen.findByRole("heading", { name: "Second Movie" });

    voteResult.resolve({
      ...playingState([], 1),
      status: "Matched",
      matchedMovie: firstMovie,
    });
    expect(await screen.findByText("Є збіг!")).toBeTruthy();
    expect(screen.queryByRole("heading", { name: "Second Movie" })).toBeNull();
  });

  it("renders a matched movie without voting hearts and restores the IMDb link", async () => {
    vi.mocked(getPartyState).mockResolvedValue({
      ...playingState([], 2),
      status: "Matched",
      matchedMovie: firstMovie,
    });
    renderPage();

    expect(await screen.findByText("Є збіг!")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Так" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Ні" })).toBeNull();
    expect(
      screen.getByRole("link", { name: "Відкрити на IMDb: First Movie" })
    ).toBeTruthy();
  });

  it("offers an explicit refresh after this player is exhausted", async () => {
    vi.mocked(getPartyState)
      .mockResolvedValueOnce(playingState([], 2))
      .mockResolvedValueOnce(playingState([secondMovie], 1));
    renderPage();

    fireEvent.click(await screen.findByRole("button", { name: "Оновити" }));

    await waitFor(() => expect(getPartyState).toHaveBeenCalledTimes(2));
    expect(
      await screen.findByRole("heading", { name: "Second Movie" })
    ).toBeTruthy();
  });

  it("shows the expired message when restoring an expired party", async () => {
    vi.mocked(getPartyState).mockRejectedValue(
      new ApiError("Expired", "PartyExpired")
    );
    renderPage();

    expect(await screen.findByText("Час цієї гри минув.")).toBeTruthy();
    expect(sessionStorage.getItem("w2w.party.playerToken")).toBeNull();
  });

  it("reconnects with the party id and token from sessionStorage", async () => {
    renderPage();

    await waitFor(() =>
      expect(getPartyState).toHaveBeenCalledWith("party-1", "token-1")
    );
    expect(screen.getByText("Код гри: 123")).toBeTruthy();
    expect(
      await screen.findByRole("heading", { name: "First Movie" })
    ).toBeTruthy();
  });

  it("supports a pre-batch server response during rolling deployment", async () => {
    const legacyState = playingState([firstMovie]);
    delete (legacyState as Partial<PartyState>).batch;
    vi.mocked(getPartyState).mockResolvedValue(legacyState);
    const voteResult = deferred<PartyState>();
    vi.mocked(voteInParty).mockReturnValue(voteResult.promise);

    renderPage();

    expect(
      await screen.findByRole("heading", { name: "First Movie" })
    ).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Так" }));

    expect(screen.getByRole("heading", { name: "First Movie" })).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Оновити" })).toBeNull();

    voteResult.resolve(playingState([secondMovie], 1));
    expect(
      await screen.findByRole("heading", { name: "Second Movie" })
    ).toBeTruthy();
  });
});

const deferred = <T,>() => {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((resolvePromise) => {
    resolve = resolvePromise;
  });
  return { promise, resolve };
};

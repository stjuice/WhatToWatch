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
import type { MovieDto } from "../types/movie";
import type { PartyStateDto } from "../types/party";
import { PartyPlayPage } from "./PartyPlayPage";

vi.mock("../api/partyApi", () => ({
  getPartyState: vi.fn(),
  voteInParty: vi.fn(),
}));

vi.mock("../state/AppStateContext", () => ({
  useAppState: vi.fn(),
}));

const firstMovie: MovieDto = {
  id: "tt1",
  title: "First Movie",
  genres: ["Drama"],
};
const secondMovie: MovieDto = {
  id: "tt2",
  title: "Second Movie",
  genres: ["Comedy"],
};

const playingState = (
  movie: MovieDto | null = firstMovie
): PartyStateDto => ({
  partyId: "party-1",
  joinCode: "1234",
  status: "Playing",
  playerCount: 2,
  opponentPresent: true,
  opponentOnline: true,
  progress: {
    currentIndex: movie ? 0 : 2,
    totalMovies: 2,
    isExhausted: !movie,
  },
  currentMovie: movie,
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
  vi.mocked(voteInParty).mockResolvedValue(playingState(secondMovie));
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
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

  it("renders a matched movie without voting hearts and restores the IMDb link", async () => {
    vi.mocked(getPartyState).mockResolvedValue({
      ...playingState(null),
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
      .mockResolvedValueOnce(playingState(null))
      .mockResolvedValueOnce(playingState(secondMovie));
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
    expect(screen.getByText("Код гри: 1234")).toBeTruthy();
    expect(
      await screen.findByRole("heading", { name: "First Movie" })
    ).toBeTruthy();
  });
});

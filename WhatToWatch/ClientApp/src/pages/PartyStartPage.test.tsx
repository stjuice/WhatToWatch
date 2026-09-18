/**
 * @vitest-environment jsdom
 */

import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, useLocation } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/http";
import { createParty, joinParty, suggestPartyCode } from "../api/partyApi";
import { getPartySession } from "../api/partySession";
import { useAppState } from "../state/AppStateContext";
import type { AppState } from "../state/AppStateContext";
import {
  PartyStartPage,
  resolveCarouselActiveIndex,
} from "./PartyStartPage";

vi.mock("../api/partyApi", () => ({
  createParty: vi.fn(),
  joinParty: vi.fn(),
  suggestPartyCode: vi.fn(),
}));

vi.mock("../state/AppStateContext", () => ({
  useAppState: vi.fn(),
}));

const appState = {
  watchlists: [
    { id: "family", name: "Сімейні", movies: [] },
    { id: "comedy", name: "Комедії", movies: [] },
  ],
} as unknown as AppState;

const session = {
  partyId: "party-1",
  playerToken: "token-1",
  joinCode: "246",
  status: "Playing" as const,
  playerCount: 1,
  opponentPresent: false,
  opponentOnline: false,
  progress: { currentIndex: 0, totalMovies: 2, isExhausted: false },
  batch: [],
};

const LocationProbe = () => {
  const { pathname } = useLocation();
  return <output aria-label="current path">{pathname}</output>;
};

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={["/party"]}>
      <PartyStartPage />
      <LocationProbe />
    </MemoryRouter>
  );

beforeEach(() => {
  sessionStorage.clear();
  vi.mocked(useAppState).mockReturnValue(appState);
  vi.mocked(suggestPartyCode).mockResolvedValue({ joinCode: "246" });
  vi.mocked(createParty).mockResolvedValue(session);
  vi.mocked(joinParty).mockResolvedValue({
    ...session,
    playerToken: "token-2",
    playerCount: 2,
    opponentPresent: true,
    opponentOnline: true,
  });
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("resolveCarouselActiveIndex", () => {
  it("returns the item nearest the viewport centre", () => {
    expect(resolveCarouselActiveIndex(205, [50, 150, 250, 350])).toBe(2);
    expect(resolveCarouselActiveIndex(60, [50, 150])).toBe(0);
    expect(resolveCarouselActiveIndex(100, [])).toBe(0);
  });
});

describe("PartyStartPage", () => {
  it("limits a stale suggested code to the three-digit UI format", async () => {
    vi.mocked(suggestPartyCode).mockResolvedValue({ joinCode: "2468" });

    renderPage();

    expect(await screen.findByPlaceholderText("246")).toBeTruthy();
    expect(screen.queryByPlaceholderText("2468")).toBeNull();
  });

  it("puts Всі фільми first and selects it by default", async () => {
    renderPage();
    await screen.findByPlaceholderText("246");

    const choices = screen.getAllByRole("button", { pressed: true });
    expect(choices).toHaveLength(1);
    expect(choices[0].getAttribute("aria-label")).toBe("Всі фільми");

    const allMovies = screen.getByRole("button", { name: "Всі фільми" });
    const family = screen.getByRole("button", { name: "Сімейні" });
    expect(allMovies.classList.contains("artwork-size--m")).toBe(true);
    expect(family.classList.contains("artwork-size--ms")).toBe(true);
    expect(
      screen
        .getByRole("button", { name: "Створити гру" })
        .classList.contains("artwork-size--ms")
    ).toBe(true);
    expect(
      allMovies.compareDocumentPosition(family) & Node.DOCUMENT_POSITION_FOLLOWING
    ).toBeTruthy();
  });

  it("typing a code keeps list selection enabled and switches the action to join", async () => {
    renderPage();
    const input = await screen.findByLabelText("Код гри");
    const carousel = screen.getByRole("region", {
      name: "Вибір списку фільмів",
    });

    fireEvent.change(input, { target: { value: "12a34" } });

    expect((input as HTMLInputElement).value).toBe("123");
    expect(carousel.getAttribute("aria-disabled")).toBeNull();
    expect(screen.getByRole("button", { name: "Сімейні" }).hasAttribute("disabled")).toBe(
      false
    );
    expect(screen.getByRole("button", { name: "Приєднатися" })).toBeTruthy();

    fireEvent.wheel(carousel, { deltaX: 0, deltaY: 120 });
    expect(carousel.scrollLeft).toBe(120);
  });

  it("creates with the suggested code when the field is empty", async () => {
    renderPage();
    await screen.findByPlaceholderText("246");

    fireEvent.click(screen.getByRole("button", { name: "Створити гру" }));

    await waitFor(() =>
      expect(createParty).toHaveBeenCalledWith({
        watchlistId: null,
        joinCode: "246",
      })
    );
    expect(getPartySession()).toEqual({
      partyId: "party-1",
      playerToken: "token-1",
    });
    await waitFor(() =>
      expect(screen.getByLabelText("current path").textContent).toBe(
        "/party/party-1"
      )
    );
  });

  it("fetches and displays a fresh suggestion after CodeTaken", async () => {
    vi.mocked(createParty).mockRejectedValueOnce(
      new ApiError("Already taken", "CodeTaken")
    );
    vi.mocked(suggestPartyCode)
      .mockResolvedValueOnce({ joinCode: "246" })
      .mockResolvedValueOnce({ joinCode: "135" });
    renderPage();
    await screen.findByPlaceholderText("246");

    fireEvent.click(screen.getByRole("button", { name: "Створити гру" }));

    expect(await screen.findByPlaceholderText("135")).toBeTruthy();
    expect(screen.getByRole("alert").textContent).toContain("Цей код уже зайнятий");
    expect(suggestPartyCode).toHaveBeenCalledTimes(2);
  });

  it("requires three digits and joins directly with the selected list", async () => {
    renderPage();
    const input = await screen.findByLabelText("Код гри");
    fireEvent.change(input, { target: { value: "99" } });

    const joinButton = screen.getByRole("button", { name: "Приєднатися" });
    expect((joinButton as HTMLButtonElement).disabled).toBe(true);

    fireEvent.change(input, { target: { value: "999" } });
    const enabledJoinButton = screen.getByRole("button", {
      name: "Приєднатися",
    });
    expect((enabledJoinButton as HTMLButtonElement).disabled).toBe(false);
    fireEvent.click(enabledJoinButton);

    await waitFor(() =>
      expect(joinParty).toHaveBeenCalledWith({
        joinCode: "999",
        watchlistId: null,
      })
    );
    await waitFor(() =>
      expect(screen.getByLabelText("current path").textContent).toBe(
        "/party/party-1"
      )
    );
    expect(createParty).not.toHaveBeenCalled();
  });

  it("joins with the list selected by player two", async () => {
    renderPage();
    const input = await screen.findByLabelText("Код гри");

    fireEvent.click(screen.getByRole("button", { name: "Сімейні" }));
    fireEvent.change(input, { target: { value: "123" } });
    fireEvent.click(screen.getByRole("button", { name: "Приєднатися" }));

    await waitFor(() =>
      expect(joinParty).toHaveBeenCalledWith({
        joinCode: "123",
        watchlistId: "family",
      })
    );
  });
});

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
  joinCode: "2468",
  status: "Playing" as const,
  playerCount: 1,
  opponentPresent: false,
  opponentOnline: false,
  progress: { currentIndex: 0, totalMovies: 2, isExhausted: false },
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
  vi.mocked(suggestPartyCode).mockResolvedValue({ joinCode: "2468" });
  vi.mocked(createParty).mockResolvedValue(session);
  vi.mocked(joinParty).mockResolvedValue(session);
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
  it("puts Всі фільми first and selects it by default", async () => {
    renderPage();
    await screen.findByPlaceholderText("2468");

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

  it("typing a code disables the carousel and switches the action to join", async () => {
    renderPage();
    const input = await screen.findByLabelText("Код гри");

    fireEvent.change(input, { target: { value: "12a34" } });

    expect((input as HTMLInputElement).value).toBe("1234");
    expect(
      screen.getByRole("region", { name: "Вибір списку фільмів" }).getAttribute(
        "aria-disabled"
      )
    ).toBe("true");
    expect(screen.getByRole("button", { name: "Сімейні" }).hasAttribute("disabled")).toBe(
      true
    );
    expect(screen.getByRole("button", { name: "Приєднатися" })).toBeTruthy();
  });

  it("creates with the suggested code when the field is empty", async () => {
    renderPage();
    await screen.findByPlaceholderText("2468");

    fireEvent.click(screen.getByRole("button", { name: "Створити гру" }));

    await waitFor(() =>
      expect(createParty).toHaveBeenCalledWith({
        watchlistId: null,
        joinCode: "2468",
      })
    );
    expect(joinParty).not.toHaveBeenCalled();
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
      .mockResolvedValueOnce({ joinCode: "2468" })
      .mockResolvedValueOnce({ joinCode: "1357" });
    renderPage();
    await screen.findByPlaceholderText("2468");

    fireEvent.click(screen.getByRole("button", { name: "Створити гру" }));

    expect(await screen.findByPlaceholderText("1357")).toBeTruthy();
    expect(screen.getByRole("alert").textContent).toContain("Цей код уже зайнятий");
    expect(suggestPartyCode).toHaveBeenCalledTimes(2);
  });

  it("shows InvalidCode and never falls back to creating", async () => {
    vi.mocked(joinParty).mockRejectedValueOnce(
      new ApiError("Invalid", "InvalidCode")
    );
    renderPage();
    const input = await screen.findByLabelText("Код гри");
    fireEvent.change(input, { target: { value: "9999" } });

    fireEvent.click(screen.getByRole("button", { name: "Приєднатися" }));

    expect(await screen.findByText("Гру з таким кодом не знайдено.")).toBeTruthy();
    expect(joinParty).toHaveBeenCalledWith("9999");
    expect(createParty).not.toHaveBeenCalled();
  });
});

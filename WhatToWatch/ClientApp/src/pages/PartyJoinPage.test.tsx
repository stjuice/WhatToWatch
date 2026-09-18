/**
 * @vitest-environment jsdom
 */

import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { getPartyPreview, joinParty } from "../api/partyApi";
import { getPartySession } from "../api/partySession";
import { useAppState } from "../state/AppStateContext";
import type { AppState } from "../state/AppStateContext";
import { PartyJoinPage } from "./PartyJoinPage";

vi.mock("../api/partyApi", () => ({
  getPartyPreview: vi.fn(),
  joinParty: vi.fn(),
}));

vi.mock("../state/AppStateContext", () => ({
  useAppState: vi.fn(),
}));

const LocationProbe = () => {
  const location = useLocation();
  return <output aria-label="current path">{location.pathname}</output>;
};

beforeEach(() => {
  sessionStorage.clear();
  vi.mocked(useAppState).mockReturnValue({
    watchlists: [
      { id: "family", name: "Сімейні", movies: [] },
      { id: "comedy", name: "Комедії", movies: [] },
    ],
    watchlistsLoading: false,
  } as unknown as AppState);
  vi.mocked(getPartyPreview).mockResolvedValue({
    partyId: "party-1",
    joinCode: "042",
    watchlistId: "comedy",
    status: "Playing",
    playerCount: 1,
    isFull: false,
  });
  vi.mocked(joinParty).mockResolvedValue({
    partyId: "party-1",
    playerToken: "token-2",
    joinCode: "042",
    status: "Playing",
    playerCount: 2,
    opponentPresent: true,
    opponentOnline: true,
    progress: { currentIndex: 0, totalMovies: 1, isExhausted: false },
    batch: [],
  });
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

it("preselects the first player's list, joins, stores the session, and redirects", async () => {
  render(
    <MemoryRouter initialEntries={["/party/join/042"]}>
      <Routes>
        <Route path="/party/join/:code" element={<PartyJoinPage />} />
        <Route path="/party/:partyId" element={<LocationProbe />} />
      </Routes>
    </MemoryRouter>
  );

  const selected = await screen.findByRole("button", {
    name: "Комедії",
    pressed: true,
  });
  expect(selected).toBeTruthy();

  fireEvent.click(screen.getByRole("button", { name: "Приєднатися" }));

  expect((await screen.findByLabelText("current path")).textContent).toBe(
    "/party/party-1"
  );
  expect(getPartyPreview).toHaveBeenCalledWith("042");
  expect(joinParty).toHaveBeenCalledWith({
    joinCode: "042",
    watchlistId: "comedy",
  });
  await waitFor(() =>
    expect(getPartySession()).toEqual({
      partyId: "party-1",
      playerToken: "token-2",
    })
  );
});

it("submits another selected list for combination", async () => {
  render(
    <MemoryRouter initialEntries={["/party/join/042"]}>
      <Routes>
        <Route path="/party/join/:code" element={<PartyJoinPage />} />
        <Route path="/party/:partyId" element={<LocationProbe />} />
      </Routes>
    </MemoryRouter>
  );

  fireEvent.click(await screen.findByRole("button", { name: "Сімейні" }));
  fireEvent.click(screen.getByRole("button", { name: "Приєднатися" }));

  await waitFor(() =>
    expect(joinParty).toHaveBeenCalledWith({
      joinCode: "042",
      watchlistId: "family",
    })
  );
});

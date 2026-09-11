/**
 * @vitest-environment jsdom
 */

import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { getPartyPreview, joinParty } from "../api/partyApi";
import { getPartySession } from "../api/partySession";
import { PartyJoinPage } from "./PartyJoinPage";

vi.mock("../api/partyApi", () => ({
  getPartyPreview: vi.fn(),
  joinParty: vi.fn(),
}));

const LocationProbe = () => {
  const location = useLocation();
  return <output aria-label="current path">{location.pathname}</output>;
};

beforeEach(() => {
  sessionStorage.clear();
  vi.mocked(getPartyPreview).mockResolvedValue({
    partyId: "party-1",
    joinCode: "0042",
    status: "Playing",
    playerCount: 1,
    isFull: false,
  });
  vi.mocked(joinParty).mockResolvedValue({
    partyId: "party-1",
    playerToken: "token-2",
    joinCode: "0042",
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

it("validates, joins, stores the session, and redirects to play", async () => {
  render(
    <MemoryRouter initialEntries={["/party/join/0042"]}>
      <Routes>
        <Route path="/party/join/:code" element={<PartyJoinPage />} />
        <Route path="/party/:partyId" element={<LocationProbe />} />
      </Routes>
    </MemoryRouter>
  );

  expect((await screen.findByLabelText("current path")).textContent).toBe(
    "/party/party-1"
  );
  expect(getPartyPreview).toHaveBeenCalledWith("0042");
  expect(joinParty).toHaveBeenCalledWith("0042");
  await waitFor(() =>
    expect(getPartySession()).toEqual({
      partyId: "party-1",
      playerToken: "token-2",
    })
  );
});

/**
 * @vitest-environment jsdom
 */

import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getWatchlist } from "../api/moviesApi";
import { RerollButton } from "../components/RerollButton";
import { useViewportZoom } from "../hooks/useViewportZoom";
import { HomePage } from "../pages/HomePage";
import { ListPage } from "../pages/ListPage";
import { useAppState } from "../state/AppStateContext";
import type { AppState } from "../state/AppStateContext";
import type { WatchlistDto } from "../types/movie";

vi.mock("../api/moviesApi", () => ({
  getWatchlist: vi.fn(),
}));

vi.mock("../hooks/useViewportZoom", () => ({
  useViewportZoom: vi.fn(),
}));

vi.mock("../state/AppStateContext", () => ({
  useAppState: vi.fn(),
}));

const watchlist: WatchlistDto = {
  id: "weekend",
  name: "Weekend",
  movies: [{ id: "tt1", title: "Arrival", genres: ["Sci-Fi"] }],
};

const createAppState = (overrides: Partial<AppState> = {}): AppState => ({
  watchlists: [watchlist],
  watchlistsLoading: false,
  watchlistsError: null,
  refreshWatchlists: vi.fn(async () => undefined),
  watchlistId: null,
  setActiveWatchlistId: vi.fn(),
  movie: null,
  setMovie: vi.fn(),
  isRestoringMovie: false,
  isPicking: false,
  pickError: null,
  pickRandomMovie: vi.fn(async () => null),
  ...overrides,
});

beforeEach(() => {
  vi.mocked(useViewportZoom).mockReturnValue(false);
  vi.mocked(useAppState).mockReturnValue(createAppState());
  vi.mocked(getWatchlist).mockResolvedValue(watchlist);
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("existing Button call sites", () => {
  it("renders the HomePage random action", () => {
    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    expect(
      screen.getByRole("button", { name: "Випадковий фільм з усіх списків" })
    ).toBeTruthy();
  });

  it("renders the ListPage random action", async () => {
    render(
      <MemoryRouter initialEntries={["/list/weekend"]}>
        <Routes>
          <Route path="/list/:id" element={<ListPage />} />
        </Routes>
      </MemoryRouter>
    );

    expect(
      await screen.findByRole("button", {
        name: "Випадковий фільм зі списку Weekend",
      })
    ).toBeTruthy();
  });

  it("renders the RerollButton action", () => {
    render(<RerollButton />);

    expect(screen.getByRole("button", { name: "Інший випадковий фільм" })).toBeTruthy();
  });
});

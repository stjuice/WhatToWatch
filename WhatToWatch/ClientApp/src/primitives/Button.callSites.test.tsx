/**
 * @vitest-environment jsdom
 */

import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getWatchlist } from "../api/moviesApi";
import { RerollButton } from "../components/RerollButton";
import { useViewportZoom } from "../hooks/useViewportZoom";
import { text } from "../i18n/text";
import { HomePage } from "../pages/HomePage";
import { ListPage } from "../pages/ListPage";
import { MoviePage } from "../pages/MoviePage";
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

describe("status and error text call sites", () => {
  it("keeps the ListPage loading message", () => {
    render(
      <MemoryRouter initialEntries={["/list/weekend"]}>
        <Routes>
          <Route path="/list/:id" element={<ListPage />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText(text("list.loading"))).toBeTruthy();
  });

  it("keeps the ListPage pick error", async () => {
    vi.mocked(useAppState).mockReturnValue(
      createAppState({ pickError: "Could not choose a movie" })
    );

    render(
      <MemoryRouter initialEntries={["/list/weekend"]}>
        <Routes>
          <Route path="/list/:id" element={<ListPage />} />
        </Routes>
      </MemoryRouter>
    );

    expect((await screen.findByRole("alert")).textContent).toBe("Could not choose a movie");
  });

  it("keeps the MoviePage pick error", () => {
    vi.mocked(useAppState).mockReturnValue(createAppState({ pickError: "Try again" }));

    render(<MoviePage />);

    expect(screen.getByRole("alert").textContent).toBe("Try again");
  });
});

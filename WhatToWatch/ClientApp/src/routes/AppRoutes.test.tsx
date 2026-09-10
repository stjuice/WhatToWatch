/**
 * @vitest-environment jsdom
 */

import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter, useLocation } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useViewportZoom } from "../hooks/useViewportZoom";
import { resolveGestureBackFallback } from "../layout/AppLayout";
import { useAppState } from "../state/AppStateContext";
import type { AppState } from "../state/AppStateContext";
import { AppRoutes } from "./AppRoutes";
import { routePaths } from "./routePaths";

vi.mock("../hooks/useViewportZoom", () => ({
  useViewportZoom: vi.fn(),
}));

vi.mock("../state/AppStateContext", () => ({
  useAppState: vi.fn(),
}));

const appState: AppState = {
  watchlists: [
    {
      id: "movie-night",
      name: "На вечір",
      movies: [{ id: "tt1", title: "Arrival", genres: ["Sci-Fi"] }],
    },
  ],
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
};

const LocationProbe = () => {
  const { pathname } = useLocation();
  return <output aria-label="current path">{pathname}</output>;
};

beforeEach(() => {
  vi.mocked(useViewportZoom).mockReturnValue(false);
  vi.mocked(useAppState).mockReturnValue(appState);
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("AppRoutes", () => {
  it("renders both landing buckets with their destinations", () => {
    render(
      <MemoryRouter initialEntries={[routePaths.home]}>
        <AppRoutes />
      </MemoryRouter>
    );

    expect(screen.getByRole("link", { name: "Всі списки" }).getAttribute("href")).toBe(
      routePaths.watchlists
    );
    expect(
      screen.getByRole("link", { name: "Оберемо разом" }).getAttribute("href")
    ).toBe(routePaths.party);
  });

  it("renders the watchlist grid and random button at /lists", () => {
    render(
      <MemoryRouter initialEntries={[routePaths.watchlists]}>
        <AppRoutes />
      </MemoryRouter>
    );

    expect(
      screen.getByRole("button", {
        name: "Випадковий фільм з усіх списків",
      })
    ).toBeTruthy();
    expect(
      screen.getByRole("link", { name: "Відкрити список На вечір" }).getAttribute("href")
    ).toBe(routePaths.list("movie-night"));
  });

  it("redirects an unknown route home", async () => {
    render(
      <MemoryRouter initialEntries={["/missing"]}>
        <AppRoutes />
        <LocationProbe />
      </MemoryRouter>
    );

    expect((await screen.findByLabelText("current path")).textContent).toBe(
      routePaths.home
    );
    expect(await screen.findByRole("link", { name: "Всі списки" })).toBeTruthy();
  });
});

describe("gesture-back targets", () => {
  it("falls back to /lists for a movie selected from all watchlists", () => {
    expect(resolveGestureBackFallback(routePaths.movie, null)).toBe(
      routePaths.watchlists
    );
  });
});

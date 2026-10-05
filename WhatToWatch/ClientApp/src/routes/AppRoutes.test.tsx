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

vi.mock("../api/spookieNightApi", () => ({
  getSpookieTickets: vi.fn(async () => []),
  getSpookieMovie: vi.fn(async () => ({
    id: "tt0075005",
    title: "Suspiria",
    genres: ["Horror"],
    posterUrl: "https://example.com/imdb.jpg",
  })),
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

    const partyLink = screen.getByRole("link", { name: "Оберемо разом" });
    const watchlistsLink = screen.getByRole("link", { name: "Всі списки" });

    expect(partyLink.getAttribute("href")).toBe(routePaths.party);
    expect(watchlistsLink.getAttribute("href")).toBe(routePaths.watchlists);
    expect(partyLink.classList.contains("artwork-size--ml")).toBe(true);
    expect(watchlistsLink.classList.contains("artwork-size--ml")).toBe(true);
    const partyLabel = partyLink.querySelector<HTMLElement>(".label-pill");
    const watchlistsLabel =
      watchlistsLink.querySelector<HTMLElement>(".label-pill");
    expect(partyLabel?.style.width).toBe(watchlistsLabel?.style.width);
    expect(partyLabel?.style.height).toBe(watchlistsLabel?.style.height);
    expect(
      partyLink.compareDocumentPosition(watchlistsLink) &
        Node.DOCUMENT_POSITION_FOLLOWING
    ).toBeTruthy();
  });

  it("renders the watchlist grid and random button at /lists", () => {
    render(
      <MemoryRouter initialEntries={[routePaths.watchlists]}>
        <AppRoutes />
      </MemoryRouter>
    );

    const randomButton = screen.getByRole("button", {
      name: "Випадковий фільм з усіх списків",
    });
    const listBucket = screen.getByRole("link", {
      name: "Відкрити список На вечір",
    });
    expect(randomButton.classList.contains("artwork-size--l")).toBe(true);
    expect(listBucket.classList.contains("artwork-size--ml")).toBe(true);
    expect(listBucket.getAttribute("href")).toBe(routePaths.list("movie-night"));
  });

  it("places the spookie night bucket first on the home page", () => {
    render(
      <MemoryRouter initialEntries={[routePaths.home]}>
        <AppRoutes />
      </MemoryRouter>
    );

    const spookieLink = screen.getByRole("link", { name: "Ніч-жахачка" });
    const partyLink = screen.getByRole("link", { name: "Оберемо разом" });

    expect(spookieLink.getAttribute("href")).toBe(routePaths.spookieNight);
    expect(spookieLink.classList.contains("artwork-size--ml")).toBe(true);
    expect(
      spookieLink.compareDocumentPosition(partyLink) &
        Node.DOCUMENT_POSITION_FOLLOWING
    ).toBeTruthy();
  });

  it("renders the spookie night tickets page", async () => {
    render(
      <MemoryRouter initialEntries={[routePaths.spookieNight]}>
        <AppRoutes />
      </MemoryRouter>
    );

    expect(
      await screen.findByText("Перший квиток з'явиться в суботу.")
    ).toBeTruthy();
  });

  it("renders a spookie movie with its season poster", async () => {
    render(
      <MemoryRouter initialEntries={[routePaths.spookieMovie("1")]}>
        <AppRoutes />
      </MemoryRouter>
    );

    expect(await screen.findByRole("heading", { name: "Suspiria" })).toBeTruthy();
    const poster = screen.getByAltText("Постер: Suspiria");
    expect(poster.getAttribute("src")).not.toBe("https://example.com/imdb.jpg");
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

  it("falls back to the tickets page from a spookie movie", () => {
    expect(resolveGestureBackFallback(routePaths.spookieMovie("bonus"), null)).toBe(
      routePaths.spookieNight
    );
  });

  it("falls back home from the tickets page", () => {
    expect(resolveGestureBackFallback(routePaths.spookieNight, null)).toBe(
      routePaths.home
    );
  });
});

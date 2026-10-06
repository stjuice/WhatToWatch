/**
 * @vitest-environment jsdom
 */

import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getSpookieMovie } from "../api/spookieNightApi";
import { useAppState } from "../state/AppStateContext";
import type { AppState } from "../state/AppStateContext";
import { SpookieWatchActions, SpookieWatchLinkProvider } from "../layout/spookieWatchLink";
import { routePaths } from "../routes/routePaths";
import type { SpookieMovie } from "../types/spookieNight";
import { SpookieMoviePage } from "./SpookieMoviePage";

vi.mock("../api/spookieNightApi", () => ({
  getSpookieMovie: vi.fn(),
}));

vi.mock("../state/AppStateContext", () => ({
  useAppState: vi.fn(),
}));

const movieResponse = (link: string | null): SpookieMovie => ({
  movie: {
    id: "tt1",
    title: "Suspiria",
    genres: ["Horror"],
  },
  link,
});

const renderPage = () =>
  render(
    <SpookieWatchLinkProvider>
      <MemoryRouter initialEntries={[routePaths.spookieMovie("1")]}>
        <Routes>
          <Route path={routePaths.spookieMoviePattern} element={<SpookieMoviePage />} />
        </Routes>
        <SpookieWatchActions />
      </MemoryRouter>
    </SpookieWatchLinkProvider>
  );

beforeEach(() => {
  vi.mocked(useAppState).mockReturnValue({
    movie: null,
    isPicking: false,
    pickError: null,
  } as unknown as AppState);
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("SpookieMoviePage", () => {
  it("pins the configured watch link to the bottom", async () => {
    vi.mocked(getSpookieMovie).mockResolvedValue(
      movieResponse("https://watch.example/suspiria")
    );
    renderPage();

    const link = await screen.findByRole("link", { name: "Лякатись тут" });
    expect(link.getAttribute("href")).toBe("https://watch.example/suspiria");
    expect(link.classList.contains("spookie-watch-link")).toBe(true);
    expect(link.classList.contains("button--primary")).toBe(true);
    expect(link.getAttribute("target")).toBe("_blank");
    expect(link.parentElement?.classList.contains("app__actions")).toBe(true);
  });

  it("hides the watch link when the ticket has none", async () => {
    vi.mocked(getSpookieMovie).mockResolvedValue(movieResponse(null));
    renderPage();

    expect(await screen.findByRole("heading", { name: "Suspiria" })).toBeTruthy();
    expect(screen.queryByRole("link", { name: "Лякатись тут" })).toBeNull();
  });
});

/**
 * @vitest-environment jsdom
 */

import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useAppState } from "../state/AppStateContext";
import type { AppState } from "../state/AppStateContext";
import type { Movie } from "../types/movie";
import { MoviePage } from "./MoviePage";

vi.mock("../state/AppStateContext", () => ({
  useAppState: vi.fn(),
}));

const contextMovie: Movie = {
  id: "tt-context",
  title: "Context Movie",
  genres: ["Drama"],
};
const passedMovie: Movie = {
  id: "tt-passed",
  title: "Passed Movie",
  genres: ["Comedy"],
};

beforeEach(() => {
  vi.mocked(useAppState).mockReturnValue({
    movie: contextMovie,
    isPicking: false,
    pickError: null,
  } as unknown as AppState);
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("MoviePage", () => {
  it("renders a passed movie and can hide its IMDb link", () => {
    render(<MoviePage movie={passedMovie} showImdbLink={false} />);

    expect(screen.getByRole("heading", { name: "Passed Movie" })).toBeTruthy();
    expect(screen.queryByRole("link", { name: /IMDb/ })).toBeNull();
    expect(useAppState).toHaveBeenCalledTimes(1);
  });

  it("keeps /movie context behaviour and IMDb link by default", () => {
    render(<MoviePage />);

    expect(screen.getByRole("heading", { name: "Context Movie" })).toBeTruthy();
    expect(
      screen.getByRole("link", { name: "Відкрити на IMDb: Context Movie" })
        .getAttribute("href")
    ).toContain("tt-context");
  });
});

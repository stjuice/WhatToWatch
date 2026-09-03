import { describe, expect, it } from "vitest";
import {
  GENRE_SEPARATOR,
  formatGenres,
  formatRating,
  formatRuntime,
  formatYear,
  getImdbTitleUrl,
  getMovieCoreFields,
} from "./movieCoreFields";
import type { MovieDto } from "../types/movie";

const fullMovie: MovieDto = {
  id: "tt0133093",
  title: "The Matrix",
  year: 1999,
  posterUrl: "https://example.com/matrix.jpg",
  rating: 8.7,
  plot: "A computer hacker learns from mysterious rebels about the true nature of his reality.",
  runtimeMinutes: 136,
  director: "Lana Wachowski",
  genres: ["Action", "Sci-Fi"],
};

describe("movie core fields", () => {
  it("builds the IMDb title URL from the movie id", () => {
    expect(getImdbTitleUrl("tt0133093")).toBe("https://www.imdb.com/title/tt0133093/");
  });

  it("joins genres with a pipe separator", () => {
    expect(formatGenres(["Action", "Sci-Fi"])).toBe(`Action${GENRE_SEPARATOR}Sci-Fi`);
    expect(GENRE_SEPARATOR).toBe(" | ");
  });

  it("omits empty genres instead of rendering a separator", () => {
    expect(formatGenres([])).toBeNull();
    expect(formatGenres(["", ""])).toBeNull();
    expect(formatGenres(undefined)).toBeNull();
  });

  it("formats duration as hours:minutes", () => {
    expect(formatRuntime(136)).toBe("2:16");
    expect(formatRuntime(59)).toBe("0:59");
    expect(formatRuntime(undefined)).toBeNull();
    expect(formatRuntime(-1)).toBeNull();
  });

  it("formats rating to one decimal", () => {
    expect(formatRating(8.7)).toBe("8.7");
    expect(formatRating(8)).toBe("8.0");
    expect(formatRating(undefined)).toBeNull();
  });

  it("formats year as a string", () => {
    expect(formatYear(1999)).toBe("1999");
    expect(formatYear(undefined)).toBeNull();
    expect(formatYear(Number.NaN)).toBeNull();
  });

  it("exposes Title, Genres, duration, director, description, and rating when metadata exists", () => {
    expect(getMovieCoreFields(fullMovie)).toEqual({
      title: "The Matrix",
      year: "1999",
      genres: "Action | Sci-Fi",
      duration: "2:16",
      director: "Lana Wachowski",
      description:
        "A computer hacker learns from mysterious rebels about the true nature of his reality.",
      rating: "8.7",
    });
  });

  it("leaves missing core fields null so the movie page can hide them", () => {
    expect(
      getMovieCoreFields({
        title: "Sparse Title",
        genres: [],
      })
    ).toEqual({
      title: "Sparse Title",
      year: null,
      genres: null,
      duration: null,
      director: null,
      description: null,
      rating: null,
    });
  });
});

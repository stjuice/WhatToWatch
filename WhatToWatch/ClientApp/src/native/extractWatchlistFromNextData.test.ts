import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import vm from "node:vm";
import { describe, expect, it } from "vitest";
import { extractWatchlistFromNextData } from "./extractWatchlistFromNextData";
import type { ExtractedWatchlistPage } from "./imdbImporter";

const listPath = "/list/ls055592025/";
const listOptions = {
  pathname: listPath,
  documentTitle: "Ignored when JSON has a name - IMDb",
};

/** Shared fixture shape for a normal list page. */
const listPageNextData = {
  props: {
    pageProps: {
      mainColumnData: {
        list: {
          id: "ls055592025",
          name: { originalText: "My Favourites" },
          titleListItemSearch: {
            edges: [
              {
                listItem: {
                  id: "tt0133093",
                  titleText: { text: "The Matrix" },
                  titleType: { id: "movie", text: "Movie" },
                  releaseYear: { year: 1999 },
                  primaryImage: { url: "https://example.com/matrix.jpg" },
                  ratingsSummary: { aggregateRating: 8.7 },
                  plot: {
                    plotText: {
                      plainText:
                        "A computer hacker learns from mysterious rebels about the true nature of his reality.",
                    },
                  },
                  runtime: { seconds: 8160 },
                  principalCredits: [
                    {
                      category: { id: "director", text: "Directors" },
                      credits: [
                        { name: { nameText: { text: "Lana Wachowski" } } },
                      ],
                    },
                  ],
                  titleGenres: {
                    genres: [
                      { genre: { text: "Action" } },
                      { genre: { text: "Sci-Fi" } },
                    ],
                  },
                },
              },
              {
                listItem: {
                  id: "tt0133093",
                  titleText: { text: "The Matrix" },
                },
              },
              {
                listItem: {
                  id: "tt1375666",
                  titleText: { text: "Inception" },
                  releaseYear: { year: 2010 },
                },
              },
              {
                listItem: {
                  id: "tt0903747",
                  titleText: { text: "Breaking Bad" },
                  titleType: { id: "tvSeries", text: "TV Series" },
                  releaseYear: { year: 2008 },
                },
              },
            ],
          },
        },
      },
    },
  },
};

const chartPageNextData = {
  props: {
    pageProps: {
      pageData: {
        chartTitles: {
          edges: [
            {
              node: {
                id: "tt0111161",
                titleText: { text: "The Shawshank Redemption" },
                releaseYear: { year: 1994 },
                primaryImage: { url: "https://example.com/shawshank.jpg" },
                ratingsSummary: { aggregateRating: 9.3 },
                runtime: { seconds: 8520 },
                titleGenres: { genres: [{ genre: { text: "Drama" } }] },
              },
            },
            {
              node: {
                id: "tt0068646",
                titleText: { text: "The Godfather" },
                releaseYear: { year: 1972 },
              },
            },
          ],
        },
      },
    },
  },
};

const here = dirname(fileURLToPath(import.meta.url));
const androidExtractScriptPath = resolve(
  here,
  "../../android/app/src/main/assets/imdb/extractWatchlist.js"
);

/**
 * Runs the Android WebView IIFE against a fake DOM so we can prove it matches the TS extractor.
 */
function runAndroidExtractScript(
  nextData: unknown,
  options: { pathname: string; documentTitle: string; currentUrl?: string }
): ExtractedWatchlistPage | null {
  const script = readFileSync(androidExtractScriptPath, "utf8");
  const nextDataJson = JSON.stringify(nextData);

  const fakeDocument = {
    title: options.documentTitle,
    getElementById: (id: string) =>
      id === "__NEXT_DATA__" ? { textContent: nextDataJson } : null,
  };
  const fakeLocation = {
    pathname: options.pathname,
    href:
      options.currentUrl ??
      `https://www.imdb.com${options.pathname}${options.pathname.endsWith("/") ? "" : "/"}`,
  };

  const json = vm.runInNewContext(script, {
    document: fakeDocument,
    location: fakeLocation,
  }) as string | null;

  return json ? (JSON.parse(json) as ExtractedWatchlistPage) : null;
}

describe("extractWatchlistFromNextData", () => {
  it("reads list name and movies from embedded list JSON", () => {
    const watchlist = extractWatchlistFromNextData(listPageNextData, listOptions);

    expect(watchlist).not.toBeNull();
    expect(watchlist?.listId).toBe("ls055592025");
    expect(watchlist?.title).toBe("My Favourites");
    expect(watchlist?.movies).toHaveLength(3);
  });

  it("reads titleType so the server can filter out TV shows", () => {
    const watchlist = extractWatchlistFromNextData(listPageNextData, listOptions);

    expect(
      watchlist?.movies.map((movie) => [movie.imdbId, movie.titleType])
    ).toEqual([
      ["tt0133093", "movie"],
      ["tt1375666", null],
      ["tt0903747", "tvSeries"],
    ]);
  });

  it("maps title fields and skips duplicates", () => {
    const watchlist = extractWatchlistFromNextData(listPageNextData, listOptions);
    const matrix = watchlist?.movies.find((movie) => movie.imdbId === "tt0133093");

    expect(matrix).toEqual({
      imdbId: "tt0133093",
      title: "The Matrix",
      year: 1999,
      imageUrl: "https://example.com/matrix.jpg",
      rating: 8.7,
      plot: "A computer hacker learns from mysterious rebels about the true nature of his reality.",
      runtimeMinutes: 136,
      director: "Lana Wachowski",
      genres: ["Action", "Sci-Fi"],
      titleType: "movie",
    });
    expect(watchlist?.movies.filter((movie) => movie.imdbId === "tt0133093")).toHaveLength(1);
  });

  it("keeps null year and image when missing", () => {
    const nextData = {
      props: {
        pageProps: {
          mainColumnData: {
            list: {
              name: { originalText: "Unrated" },
              titleListItemSearch: {
                edges: [
                  {
                    listItem: {
                      id: "tt9999999",
                      titleText: { text: "Unreleased Film" },
                      releaseYear: { year: null },
                    },
                  },
                ],
              },
            },
          },
        },
      },
    };

    const movie = extractWatchlistFromNextData(nextData, listOptions)?.movies[0];

    expect(movie).toEqual({
      imdbId: "tt9999999",
      title: "Unreleased Film",
      year: null,
      imageUrl: null,
      rating: null,
      plot: null,
      runtimeMinutes: null,
      director: null,
      genres: [],
      titleType: null,
    });
  });

  it("reads chart titles without a list object", () => {
    const watchlist = extractWatchlistFromNextData(chartPageNextData, {
      pathname: "/chart/moviemeter/",
      documentTitle: "Most Popular Movies",
    });

    expect(watchlist?.title).toBe("Most Popular Movies");
    expect(watchlist?.movies.map((movie) => movie.imdbId)).toEqual([
      "tt0111161",
      "tt0068646",
    ]);
    expect(watchlist?.movies[0]).toMatchObject({
      rating: 9.3,
      runtimeMinutes: 142,
      genres: ["Drama"],
    });
  });

  it("returns null when __NEXT_DATA__ has no titles yet", () => {
    expect(
      extractWatchlistFromNextData(
        { props: { pageProps: { mainColumnData: {} } } },
        listOptions
      )
    ).toBeNull();
  });

  it("ignores non-title ids", () => {
    const nextData = {
      list: {
        id: "ls055592025",
        name: { originalText: "People" },
        items: [{ id: "nm0000001", titleText: { text: "Not a movie" } }],
      },
    };

    expect(extractWatchlistFromNextData(nextData, listOptions)).toBeNull();
  });

  it("reports pagination metadata", () => {
    const nextData = {
      props: {
        pageProps: {
          mainColumnData: {
            list: {
              name: { originalText: "Paged" },
              titleListItemSearch: {
                pageInfo: { hasNextPage: true },
                edges: [
                  {
                    listItem: {
                      id: "tt0133093",
                      titleText: { text: "The Matrix" },
                    },
                  },
                ],
              },
            },
          },
        },
      },
    };

    const watchlist = extractWatchlistFromNextData(nextData, {
      ...listOptions,
      currentUrl: "https://www.imdb.com/list/ls055592025/?ref_=fn_all",
    });

    expect(watchlist?.hasNextPage).toBe(true);
    expect(watchlist?.nextPageUrl).toBe(
      "https://www.imdb.com/list/ls055592025/?ref_=fn_all&page=2"
    );
  });
});

describe("Android extractWatchlist.js asset", () => {
  it("stays in sync with the TypeScript extractor on list fixtures", () => {
    const fromTs = extractWatchlistFromNextData(listPageNextData, listOptions);
    const fromAsset = runAndroidExtractScript(listPageNextData, listOptions);

    expect(fromAsset).toEqual(fromTs);
  });

  it("stays in sync with the TypeScript extractor on chart fixtures", () => {
    const options = {
      pathname: "/chart/moviemeter/",
      documentTitle: "Most Popular Movies",
    };
    const fromTs = extractWatchlistFromNextData(chartPageNextData, options);
    const fromAsset = runAndroidExtractScript(chartPageNextData, options);

    expect(fromAsset).toEqual(fromTs);
  });

  it("returns null for empty next data", () => {
    expect(
      runAndroidExtractScript(
        { props: { pageProps: { mainColumnData: {} } } },
        listOptions
      )
    ).toBeNull();
  });
});

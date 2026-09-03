import { beforeEach, describe, expect, it, vi } from "vitest";
import { resetImdbImportStrategy } from "./imdbImport/createImdbImportStrategy";

const mocks = vi.hoisted(() => ({
  importList: vi.fn(),
  isNativePlatform: vi.fn(),
  importImdbWatchlist: vi.fn(),
  importWatchlistByUrl: vi.fn(),
}));

vi.mock("@capacitor/core", () => ({
  Capacitor: {
    isNativePlatform: () => mocks.isNativePlatform(),
  },
  registerPlugin: () => ({
    importList: mocks.importList,
  }),
}));

vi.mock("../api/moviesApi", () => ({
  importImdbWatchlist: (...args: unknown[]) => mocks.importImdbWatchlist(...args),
  importWatchlistByUrl: (...args: unknown[]) => mocks.importWatchlistByUrl(...args),
  refreshWatchlist: vi.fn(),
}));

describe("ImdbImportService", () => {
  beforeEach(() => {
    mocks.importList.mockReset();
    mocks.importImdbWatchlist.mockReset();
    mocks.importWatchlistByUrl.mockReset();
    mocks.isNativePlatform.mockReset();
    resetImdbImportStrategy();
    vi.resetModules();
  });

  it("reports unavailable outside the native app", async () => {
    mocks.isNativePlatform.mockReturnValue(false);
    const { ImdbImportService } = await import("../services/imdbImportService");

    expect(ImdbImportService.isAvailable()).toBe(false);
  });

  it("rejects empty urls on native", async () => {
    mocks.isNativePlatform.mockReturnValue(true);
    const { ImdbImportService } = await import("../services/imdbImportService");

    await expect(ImdbImportService.importFromImdb("   ")).rejects.toThrow(
      "Enter an IMDb list URL"
    );
    expect(mocks.importList).not.toHaveBeenCalled();
  });

  it("uses server import on web", async () => {
    mocks.isNativePlatform.mockReturnValue(false);
    mocks.importWatchlistByUrl.mockResolvedValue({
      id: "ls1",
      name: "Server list",
      movies: [],
    });

    const { ImdbImportService } = await import("../services/imdbImportService");
    const result = await ImdbImportService.importFromImdb("https://www.imdb.com/list/ls1/");

    expect(mocks.importWatchlistByUrl).toHaveBeenCalledWith("https://www.imdb.com/list/ls1/");
    expect(mocks.importList).not.toHaveBeenCalled();
    expect(result.name).toBe("Server list");
  });

  it("extracts via plugin then posts to the Render API on native", async () => {
    mocks.isNativePlatform.mockReturnValue(true);
    const extracted = {
      listId: "ls055592025",
      title: "My Favourites",
      movies: [
        {
          imdbId: "tt0133093",
          title: "The Matrix",
          year: 1999,
          imageUrl: null,
        },
      ],
    };
    const saved = {
      id: "ls055592025",
      name: "My Favourites",
      movies: [{ id: "tt0133093", title: "The Matrix", year: 1999, genres: [] }],
    };
    mocks.importList.mockResolvedValue(extracted);
    mocks.importImdbWatchlist.mockResolvedValue(saved);

    const { ImdbImportService } = await import("../services/imdbImportService");
    const result = await ImdbImportService.importFromImdb(
      "  https://www.imdb.com/list/ls055592025/  "
    );

    expect(mocks.importList).toHaveBeenCalledWith({
      url: "https://www.imdb.com/list/ls055592025/",
    });
    expect(mocks.importImdbWatchlist).toHaveBeenCalledWith({
      listId: "ls055592025",
      title: "My Favourites",
      url: "https://www.imdb.com/list/ls055592025/",
      movies: extracted.movies,
      hasNextPage: false,
      nextPageUrl: null,
    });
    expect(result).toEqual(saved);
  });
});

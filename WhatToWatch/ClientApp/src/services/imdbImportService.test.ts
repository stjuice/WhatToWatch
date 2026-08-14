import { beforeEach, describe, expect, it, vi } from "vitest";

const importList = vi.fn();
const isNativePlatform = vi.fn();
const importImdbWatchlist = vi.fn();

vi.mock("@capacitor/core", () => ({
  Capacitor: {
    isNativePlatform: () => isNativePlatform(),
  },
  registerPlugin: () => ({
    importList,
  }),
}));

vi.mock("../api/moviesApi", () => ({
  importImdbWatchlist: (...args: unknown[]) => importImdbWatchlist(...args),
}));

describe("ImdbImportService", () => {
  beforeEach(() => {
    importList.mockReset();
    importImdbWatchlist.mockReset();
    isNativePlatform.mockReset();
    vi.resetModules();
  });

  it("reports unavailable outside the native app", async () => {
    isNativePlatform.mockReturnValue(false);
    const { ImdbImportService } = await import("../services/imdbImportService");

    expect(ImdbImportService.isAvailable()).toBe(false);
  });

  it("rejects empty urls", async () => {
    isNativePlatform.mockReturnValue(true);
    const { ImdbImportService } = await import("../services/imdbImportService");

    await expect(ImdbImportService.importFromImdb("   ")).rejects.toThrow(
      "Enter an IMDb list URL"
    );
    expect(importList).not.toHaveBeenCalled();
  });

  it("rejects when Capacitor is not native", async () => {
    isNativePlatform.mockReturnValue(false);
    const { ImdbImportService } = await import("../services/imdbImportService");

    await expect(
      ImdbImportService.importFromImdb("https://www.imdb.com/list/ls1/")
    ).rejects.toThrow("IMDb import is only available in the mobile app");
  });

  it("extracts via plugin then posts to the Render API", async () => {
    isNativePlatform.mockReturnValue(true);
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
    importList.mockResolvedValue(extracted);
    importImdbWatchlist.mockResolvedValue(saved);

    const { ImdbImportService } = await import("../services/imdbImportService");
    const result = await ImdbImportService.importFromImdb(
      "  https://www.imdb.com/list/ls055592025/  "
    );

    expect(importList).toHaveBeenCalledWith({
      url: "https://www.imdb.com/list/ls055592025/",
    });
    expect(importImdbWatchlist).toHaveBeenCalledWith({
      listId: "ls055592025",
      title: "My Favourites",
      movies: extracted.movies,
    });
    expect(result).toEqual(saved);
  });
});

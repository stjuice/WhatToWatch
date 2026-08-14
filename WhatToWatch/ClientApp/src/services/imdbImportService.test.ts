import { beforeEach, describe, expect, it, vi } from "vitest";

const importList = vi.fn();
const isNativePlatform = vi.fn();

vi.mock("@capacitor/core", () => ({
  Capacitor: {
    isNativePlatform: () => isNativePlatform(),
  },
  registerPlugin: () => ({
    importList,
  }),
}));

describe("ImdbImportService", () => {
  beforeEach(() => {
    importList.mockReset();
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
      "Вкажіть посилання на список IMDb"
    );
    expect(importList).not.toHaveBeenCalled();
  });

  it("rejects when Capacitor is not native", async () => {
    isNativePlatform.mockReturnValue(false);
    const { ImdbImportService } = await import("../services/imdbImportService");

    await expect(
      ImdbImportService.importFromImdb("https://www.imdb.com/list/ls1/")
    ).rejects.toThrow("Імпорт через IMDb доступний лише в мобільному застосунку");
  });

  it("forwards the trimmed url to the plugin and returns the watchlist", async () => {
    isNativePlatform.mockReturnValue(true);
    const watchlist = {
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
    importList.mockResolvedValue(watchlist);

    const { ImdbImportService } = await import("../services/imdbImportService");
    const result = await ImdbImportService.importFromImdb(
      "  https://www.imdb.com/list/ls055592025/  "
    );

    expect(importList).toHaveBeenCalledWith({
      url: "https://www.imdb.com/list/ls055592025/",
    });
    expect(result).toEqual(watchlist);
  });
});

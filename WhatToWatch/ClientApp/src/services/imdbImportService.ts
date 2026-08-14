import { Capacitor } from "@capacitor/core";
import { importImdbWatchlist } from "../api/moviesApi";
import { ImdbImporter } from "../native/imdbImporter";
import type { WatchlistDto } from "../types/movie";

type ImportOptions = {
  onStatus?: (message: string) => void;
};

const isAvailable = (): boolean => Capacitor.isNativePlatform();

const importFromImdb = async (
  url: string,
  options?: ImportOptions
): Promise<WatchlistDto> => {
  if (!isAvailable()) {
    throw new Error("IMDb import is only available in the mobile app");
  }

  const trimmedUrl = url.trim();
  if (!trimmedUrl) {
    throw new Error("Enter an IMDb list URL");
  }

  options?.onStatus?.("Opening IMDb…");
  const imported = await ImdbImporter.importList({ url: trimmedUrl });

  console.log(
    `[ImdbImport] extracted ${imported.listId} "${imported.title}" (${imported.movies.length} movies)`
  );
  options?.onStatus?.(
    `Got ${imported.movies.length} movies. Saving to the server…`
  );

  try {
    const saved = await importImdbWatchlist({
      listId: imported.listId,
      title: imported.title,
      url: trimmedUrl,
      movies: imported.movies,
    });
    console.log(`[ImdbImport] saved ${saved.id} on server`);
    options?.onStatus?.(`Saved “${saved.name}”.`);
    return saved;
  } catch (error) {
    console.error("[ImdbImport] server save failed", error);
    throw error;
  }
};

export const ImdbImportService = {
  isAvailable,
  importFromImdb,
};

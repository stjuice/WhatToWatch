import { Capacitor } from "@capacitor/core";
import { ImdbImporter } from "../native/imdbImporter";
import type { ImportedWatchlist } from "../native/imdbImporter";

const isAvailable = (): boolean => Capacitor.isNativePlatform();

const importFromImdb = async (url: string): Promise<ImportedWatchlist> => {
  if (!isAvailable()) {
    throw new Error("Імпорт через IMDb доступний лише в мобільному застосунку");
  }

  const trimmedUrl = url.trim();
  if (!trimmedUrl) {
    throw new Error("Вкажіть посилання на список IMDb");
  }

  const watchlist = await ImdbImporter.importList({ url: trimmedUrl });

  console.log("[ImdbImport] imported watchlist", watchlist);
  console.log(
    `[ImdbImport] list ${watchlist.listId} "${watchlist.title}" with ${watchlist.movies.length} movies`
  );

  return watchlist;
};

export const ImdbImportService = {
  isAvailable,
  importFromImdb,
};

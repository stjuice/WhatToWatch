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
    throw new Error("Імпорт через IMDb доступний лише в мобільному застосунку");
  }

  const trimmedUrl = url.trim();
  if (!trimmedUrl) {
    throw new Error("Вкажіть посилання на список IMDb");
  }

  options?.onStatus?.("Відкриваємо IMDb…");
  const imported = await ImdbImporter.importList({ url: trimmedUrl });

  console.log(
    `[ImdbImport] extracted ${imported.listId} "${imported.title}" (${imported.movies.length} movies)`
  );
  options?.onStatus?.(`Отримано ${imported.movies.length} фільмів. Зберігаємо на сервері…`);

  try {
    const saved = await importImdbWatchlist({
      listId: imported.listId,
      title: imported.title,
      movies: imported.movies,
    });
    console.log(`[ImdbImport] saved ${saved.id} on server`);
    options?.onStatus?.(`Збережено «${saved.name}».`);
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

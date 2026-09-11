import { Capacitor } from "@capacitor/core";
import { importImdbWatchlist } from "../../api/moviesApi";
import { ImdbImporter } from "../../native/imdbImporter";
import type { Watchlist } from "../../types/movie";
import type { IImdbImportStrategy, ImportOptions } from "./ImdbImportStrategy";

export class NativeWebViewImportStrategy implements IImdbImportStrategy {
  isAvailable(): boolean {
    return Capacitor.isNativePlatform();
  }

  async import(url: string, options?: ImportOptions): Promise<Watchlist> {
    if (!this.isAvailable())
      throw new Error("IMDb import is only available in the mobile app");

    const trimmedUrl = url.trim();
    if (!trimmedUrl)
      throw new Error("Enter an IMDb list URL");

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
        ...imported,
        url: trimmedUrl,
        hasNextPage: false,
        nextPageUrl: null,
      });

      console.log(`[ImdbImport] saved ${saved.id} on server`);
      options?.onStatus?.(`Saved “${saved.name}”.`);

      return saved;
    } catch (error) {
      console.error("[ImdbImport] server save failed", error);

      throw error;
    }
  }

  async refresh(list: Watchlist, options?: ImportOptions): Promise<Watchlist> {
    const listUrl = list.url?.trim();
    if (!listUrl)
      throw new Error(`Watchlist “${list.name}” has no URL to refresh from.`);

    return this.import(listUrl, options);
  }
}

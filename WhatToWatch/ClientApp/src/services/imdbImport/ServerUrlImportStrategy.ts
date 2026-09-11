import { importWatchlistByUrl, refreshWatchlist } from "../../api/moviesApi";
import type { Watchlist } from "../../types/movie";
import type { IImdbImportStrategy, ImportOptions } from "./ImdbImportStrategy";

export class ServerUrlImportStrategy implements IImdbImportStrategy {
  isAvailable(): boolean {
    return true;
  }

  async import(url: string): Promise<Watchlist> {
    const trimmedUrl = url.trim();
    if (!trimmedUrl) {
      throw new Error("Enter an IMDb list URL");
    }

    return importWatchlistByUrl(trimmedUrl);
  }

  async refresh(list: Watchlist, _options?: ImportOptions): Promise<Watchlist> {
    return refreshWatchlist(list.id);
  }
}

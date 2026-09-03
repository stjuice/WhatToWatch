import { importWatchlistByUrl, refreshWatchlist } from "../../api/moviesApi";
import type { WatchlistDto } from "../../types/movie";
import type { IImdbImportStrategy, ImportOptions } from "./ImdbImportStrategy";

export class ServerUrlImportStrategy implements IImdbImportStrategy {
  isAvailable(): boolean {
    return true;
  }

  async import(url: string): Promise<WatchlistDto> {
    const trimmedUrl = url.trim();
    if (!trimmedUrl) {
      throw new Error("Enter an IMDb list URL");
    }

    return importWatchlistByUrl(trimmedUrl);
  }

  async refresh(list: WatchlistDto, _options?: ImportOptions): Promise<WatchlistDto> {
    return refreshWatchlist(list.id);
  }
}

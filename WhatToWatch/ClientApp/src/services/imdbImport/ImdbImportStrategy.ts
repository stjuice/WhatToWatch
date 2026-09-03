import type { WatchlistDto } from "../../types/movie";

export type ImportOptions = {
  onStatus?: (message: string) => void;
};

export interface IImdbImportStrategy {
  isAvailable(): boolean;
  import(url: string, options?: ImportOptions): Promise<WatchlistDto>;
  refresh(list: WatchlistDto, options?: ImportOptions): Promise<WatchlistDto>;
}

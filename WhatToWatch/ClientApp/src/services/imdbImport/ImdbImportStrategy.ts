import type { Watchlist } from "../../types/movie";

export type ImportOptions = {
  onStatus?: (message: string) => void;
};

export interface IImdbImportStrategy {
  isAvailable(): boolean;
  import(url: string, options?: ImportOptions): Promise<Watchlist>;
  refresh(list: Watchlist, options?: ImportOptions): Promise<Watchlist>;
}

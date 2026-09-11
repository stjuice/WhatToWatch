import { Capacitor } from "@capacitor/core";
import { createImdbImportStrategy } from "./imdbImport/createImdbImportStrategy";
import type { ImportOptions } from "./imdbImport/ImdbImportStrategy";
import type { Watchlist } from "../types/movie";

const strategy = () => createImdbImportStrategy();

const isAvailable = (): boolean => Capacitor.isNativePlatform();

const importFromImdb = async (
  url: string,
  options?: ImportOptions
): Promise<Watchlist> => strategy().import(url, options);

const refreshFromImdb = async (
  list: Watchlist,
  options?: ImportOptions
): Promise<Watchlist> => strategy().refresh(list, options);

export const ImdbImportService = {
  isAvailable,
  importFromImdb,
  refreshFromImdb,
};

export type { ImportOptions };

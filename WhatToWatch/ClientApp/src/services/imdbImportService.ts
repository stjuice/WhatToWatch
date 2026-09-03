import { Capacitor } from "@capacitor/core";
import { createImdbImportStrategy } from "./imdbImport/createImdbImportStrategy";
import type { ImportOptions } from "./imdbImport/ImdbImportStrategy";
import type { WatchlistDto } from "../types/movie";

const strategy = () => createImdbImportStrategy();

const isAvailable = (): boolean => Capacitor.isNativePlatform();

const importFromImdb = async (
  url: string,
  options?: ImportOptions
): Promise<WatchlistDto> => strategy().import(url, options);

const refreshFromImdb = async (
  list: WatchlistDto,
  options?: ImportOptions
): Promise<WatchlistDto> => strategy().refresh(list, options);

export const ImdbImportService = {
  isAvailable,
  importFromImdb,
  refreshFromImdb,
};

export type { ImportOptions };

import { registerPlugin } from "@capacitor/core";

export interface ImportedMovie {
  imdbId: string;
  title: string;
  year: number | null;
  imageUrl: string | null;
  rating: number | null;
  plot: string | null;
  runtimeMinutes: number | null;
  director: string | null;
  genres: string[];
  titleType: string | null;
}

export interface ImportedWatchlist {
  listId: string;
  title: string;
  movies: ImportedMovie[];
}

export interface ImdbImporterPlugin {
  /**
   * Opens the list inside the app's WebView so the user can log in or pass verification,
   * then resolves with the normalized watchlist and closes the screen.
   */
  importList(options: { url: string }): Promise<ImportedWatchlist>;
}

export const ImdbImporter = registerPlugin<ImdbImporterPlugin>("ImdbImporter");

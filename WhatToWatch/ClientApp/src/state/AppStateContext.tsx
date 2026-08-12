import { createContext, useCallback, useContext, useMemo, useState } from "react";
import type { Dispatch, ReactNode, SetStateAction } from "react";
import {
  clearStoredWatchlistId,
  getStoredWatchlistId,
  importWatchlist,
} from "../api/moviesApi";
import type { MovieDto } from "../types/movie";

export interface AppState {
  url: string;
  setUrl: Dispatch<SetStateAction<string>>;
  watchlistId: string | null;
  isListLoaded: boolean;
  isImporting: boolean;
  importError: string | null;
  loadList: () => Promise<void>;
  clearList: () => void;
  movie: MovieDto | null;
  setMovie: Dispatch<SetStateAction<MovieDto | null>>;
}

const AppStateContext = createContext<AppState | null>(null);

export function AppStateProvider({ children }: { children: ReactNode }) {
  const [url, setUrl] = useState("");
  const [watchlistId, setWatchlistId] = useState<string | null>(() => getStoredWatchlistId());
  const [isListLoaded, setIsListLoaded] = useState(() => Boolean(getStoredWatchlistId()));
  const [isImporting, setIsImporting] = useState(false);
  const [importError, setImportError] = useState<string | null>(null);
  const [movie, setMovie] = useState<MovieDto | null>(null);

  const loadList = useCallback(async () => {
    const trimmedUrl = url.trim();
    if (!trimmedUrl || isImporting) {
      return;
    }

    setIsImporting(true);
    setImportError(null);

    try {
      const watchlist = await importWatchlist(trimmedUrl);
      setWatchlistId(watchlist.id);
      setIsListLoaded(true);
      setMovie(null);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Не вдалося додати список";
      setImportError(message);
      setIsListLoaded(false);
      setWatchlistId(null);
      clearStoredWatchlistId();
    } finally {
      setIsImporting(false);
    }
  }, [url, isImporting]);

  const clearList = useCallback(() => {
    setUrl("");
    setWatchlistId(null);
    setIsListLoaded(false);
    setImportError(null);
    setMovie(null);
    clearStoredWatchlistId();
  }, []);

  const state = useMemo<AppState>(
    () => ({
      url,
      setUrl,
      watchlistId,
      isListLoaded,
      isImporting,
      importError,
      loadList,
      clearList,
      movie,
      setMovie,
    }),
    [url, watchlistId, isListLoaded, isImporting, importError, loadList, clearList, movie]
  );

  return <AppStateContext.Provider value={state}>{children}</AppStateContext.Provider>;
}

export function useAppState(): AppState {
  const state = useContext(AppStateContext);

  if (!state) {
    throw new Error("useAppState must be used within AppStateProvider");
  }

  return state;
}

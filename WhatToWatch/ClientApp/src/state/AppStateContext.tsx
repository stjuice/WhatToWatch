import { createContext, useCallback, useContext, useMemo, useRef, useState } from "react";
import type { Dispatch, ReactNode, SetStateAction } from "react";
import {
  clearStoredWatchlistId,
  getRandomMovie,
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
  isPicking: boolean;
  pickError: string | null;
  pickRandomMovie: () => Promise<MovieDto | null>;
}

const AppStateContext = createContext<AppState | null>(null);

export const AppStateProvider = ({ children }: { children: ReactNode }) => {
  const [url, setUrl] = useState("");
  const [watchlistId, setWatchlistId] = useState<string | null>(() => getStoredWatchlistId());
  const [isListLoaded, setIsListLoaded] = useState(() => Boolean(getStoredWatchlistId()));
  const [isImporting, setIsImporting] = useState(false);
  const [importError, setImportError] = useState<string | null>(null);
  const [movie, setMovie] = useState<MovieDto | null>(null);
  const [isPicking, setIsPicking] = useState(false);
  const [pickError, setPickError] = useState<string | null>(null);

  // Refs keep repeated taps from firing a second request before state updates.
  const isImportingRef = useRef(false);
  const isPickingRef = useRef(false);

  const loadList = useCallback(async () => {
    const trimmedUrl = url.trim();
    if (!trimmedUrl || isImportingRef.current) {
      return;
    }

    isImportingRef.current = true;
    setIsImporting(true);
    setImportError(null);
    setPickError(null);

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
      isImportingRef.current = false;
      setIsImporting(false);
    }
  }, [url]);

  const clearList = useCallback(() => {
    setUrl("");
    setWatchlistId(null);
    setIsListLoaded(false);
    setImportError(null);
    setPickError(null);
    setMovie(null);
    clearStoredWatchlistId();
  }, []);

  const pickRandomMovie = useCallback(async (): Promise<MovieDto | null> => {
    if (!watchlistId || isPickingRef.current) {
      return null;
    }

    isPickingRef.current = true;
    setIsPicking(true);
    setPickError(null);

    try {
      const picked = await getRandomMovie(watchlistId);
      setMovie(picked);
      return picked;
    } catch (error) {
      const message = error instanceof Error ? error.message : "Не вдалося обрати фільм";
      setPickError(message);
      return null;
    } finally {
      isPickingRef.current = false;
      setIsPicking(false);
    }
  }, [watchlistId]);

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
      isPicking,
      pickError,
      pickRandomMovie,
    }),
    [
      url,
      watchlistId,
      isListLoaded,
      isImporting,
      importError,
      loadList,
      clearList,
      movie,
      isPicking,
      pickError,
      pickRandomMovie,
    ]
  );

  return <AppStateContext.Provider value={state}>{children}</AppStateContext.Provider>;
};

export const useAppState = (): AppState => {
  const state = useContext(AppStateContext);

  if (!state) {
    throw new Error("useAppState must be used within AppStateProvider");
  }

  return state;
};

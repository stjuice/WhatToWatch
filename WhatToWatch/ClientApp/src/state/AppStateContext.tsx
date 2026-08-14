import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import type { Dispatch, ReactNode, SetStateAction } from "react";
import {
  clearStoredMovieId,
  clearStoredWatchlistId,
  getMovie,
  getRandomMovie,
  getStoredMovieId,
  getStoredWatchlistId,
  importWatchlist,
  setStoredMovieId,
} from "../api/moviesApi";
import { ImdbImportService } from "../services/imdbImportService";
import type { MovieDto } from "../types/movie";

export interface AppState {
  url: string;
  setUrl: Dispatch<SetStateAction<string>>;
  watchlistId: string | null;
  isListLoaded: boolean;
  isImporting: boolean;
  importError: string | null;
  nativeImportLog: string[];
  loadList: () => Promise<void>;
  importFromImdb: () => Promise<void>;
  clearList: () => void;
  movie: MovieDto | null;
  setMovie: Dispatch<SetStateAction<MovieDto | null>>;
  isRestoringMovie: boolean;
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
  const [nativeImportLog, setNativeImportLog] = useState<string[]>([]);
  const [nativeMovies, setNativeMovies] = useState<MovieDto[]>([]);
  const [movie, setMovie] = useState<MovieDto | null>(null);
  const [isRestoringMovie, setIsRestoringMovie] = useState(
    () => Boolean(getStoredWatchlistId() && getStoredMovieId())
  );
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
    setNativeImportLog([]);

    try {
      const watchlist = await importWatchlist(trimmedUrl);
      setNativeMovies([]);
      setWatchlistId(watchlist.id);
      setIsListLoaded(true);
      setMovie(null);
      clearStoredMovieId();
    } catch (error) {
      const message = error instanceof Error ? error.message : "Не вдалося додати список";
      setImportError(message);
      setIsListLoaded(false);
      setWatchlistId(null);
      clearStoredWatchlistId();
      clearStoredMovieId();
    } finally {
      isImportingRef.current = false;
      setIsImporting(false);
    }
  }, [url]);

  const importFromImdb = useCallback(async () => {
    const trimmedUrl = url.trim();
    if (!trimmedUrl || isImportingRef.current) {
      return;
    }

    isImportingRef.current = true;
    setIsImporting(true);
    setImportError(null);
    setPickError(null);
    setNativeImportLog([
      "Відкриваємо IMDb у захищеному вікні…",
      "Очікуємо завантаження списку або перевірки IMDb.",
    ]);

    try {
      const watchlist = await ImdbImportService.importFromImdb(trimmedUrl);
      const movies: MovieDto[] = watchlist.movies.map((item) => ({
        id: item.imdbId,
        title: item.title,
        ...(item.year == null ? {} : { year: item.year }),
        ...(item.imageUrl == null ? {} : { posterUrl: item.imageUrl }),
        genres: [],
      }));

      setNativeMovies(movies);
      setWatchlistId(watchlist.listId);
      setIsListLoaded(true);
      setMovie(null);
      clearStoredWatchlistId();
      clearStoredMovieId();
      setNativeImportLog([
        `Список «${watchlist.title}» отримано.`,
        `Імпортовано фільмів: ${movies.length}.`,
        "IMDb закрито. Список готовий до використання.",
      ]);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Не вдалося імпортувати список IMDb";
      const wasCancelled = message === "cancelled";
      setImportError(wasCancelled ? null : message);
      setNativeImportLog([
        wasCancelled ? "Імпорт скасовано." : `Помилка імпорту: ${message}`,
      ]);
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
    setNativeMovies([]);
    setNativeImportLog([]);
    clearStoredWatchlistId();
    clearStoredMovieId();
  }, []);

  const pickRandomMovie = useCallback(async (): Promise<MovieDto | null> => {
    if (!watchlistId || isPickingRef.current) {
      return null;
    }

    isPickingRef.current = true;
    setIsPicking(true);
    setPickError(null);

    try {
      if (nativeMovies.length > 0) {
        const picked = nativeMovies[Math.floor(Math.random() * nativeMovies.length)]!;
        setMovie(picked);
        return picked;
      }

      const picked = await getRandomMovie(watchlistId);
      setMovie(picked);
      setStoredMovieId(picked.id);
      return picked;
    } catch (error) {
      const message = error instanceof Error ? error.message : "Не вдалося обрати фільм";
      setPickError(message);
      return null;
    } finally {
      isPickingRef.current = false;
      setIsPicking(false);
    }
  }, [nativeMovies, watchlistId]);

  useEffect(() => {
    if (!isRestoringMovie) {
      return;
    }

    const storedMovieId = getStoredMovieId();
    if (!watchlistId || !storedMovieId) {
      setIsRestoringMovie(false);
      return;
    }

    let isCancelled = false;

    const restore = async () => {
      try {
        const restored = await getMovie(storedMovieId, watchlistId);
        if (!isCancelled) {
          setMovie(restored);
        }
      } catch {
        clearStoredMovieId();
      } finally {
        if (!isCancelled) {
          setIsRestoringMovie(false);
        }
      }
    };

    void restore();

    return () => {
      isCancelled = true;
    };
  }, [isRestoringMovie, watchlistId]);

  const state = useMemo<AppState>(
    () => ({
      url,
      setUrl,
      watchlistId,
      isListLoaded,
      isImporting,
      importError,
      nativeImportLog,
      loadList,
      importFromImdb,
      clearList,
      movie,
      setMovie,
      isRestoringMovie,
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
      nativeImportLog,
      loadList,
      importFromImdb,
      clearList,
      movie,
      isRestoringMovie,
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

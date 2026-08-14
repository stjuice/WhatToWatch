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
  getWatchlists,
  setStoredMovieId,
  setStoredWatchlistId,
} from "../api/moviesApi";
import type { MovieDto, WatchlistDto } from "../types/movie";

export interface AppState {
  watchlists: WatchlistDto[];
  watchlistsLoading: boolean;
  watchlistsError: string | null;
  refreshWatchlists: () => Promise<void>;
  watchlistId: string | null;
  setActiveWatchlistId: (id: string | null) => void;
  movie: MovieDto | null;
  setMovie: Dispatch<SetStateAction<MovieDto | null>>;
  isRestoringMovie: boolean;
  isPicking: boolean;
  pickError: string | null;
  pickRandomMovie: (watchlistId?: string | null) => Promise<MovieDto | null>;
}

const AppStateContext = createContext<AppState | null>(null);

export const AppStateProvider = ({ children }: { children: ReactNode }) => {
  const [watchlists, setWatchlists] = useState<WatchlistDto[]>([]);
  const [watchlistsLoading, setWatchlistsLoading] = useState(true);
  const [watchlistsError, setWatchlistsError] = useState<string | null>(null);
  const [watchlistId, setWatchlistId] = useState<string | null>(() => getStoredWatchlistId());
  const [movie, setMovie] = useState<MovieDto | null>(null);
  const [isRestoringMovie, setIsRestoringMovie] = useState(
    () => Boolean(getStoredWatchlistId() && getStoredMovieId())
  );
  const [isPicking, setIsPicking] = useState(false);
  const [pickError, setPickError] = useState<string | null>(null);

  const isPickingRef = useRef(false);

  const refreshWatchlists = useCallback(async () => {
    setWatchlistsLoading(true);
    setWatchlistsError(null);
    try {
      const lists = await getWatchlists();
      setWatchlists(lists);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Не вдалося завантажити списки";
      setWatchlistsError(message);
    } finally {
      setWatchlistsLoading(false);
    }
  }, []);

  useEffect(() => {
    void refreshWatchlists();
  }, [refreshWatchlists]);

  const setActiveWatchlistId = useCallback((id: string | null) => {
    setWatchlistId(id);
    if (id) {
      setStoredWatchlistId(id);
    } else {
      clearStoredWatchlistId();
    }
  }, []);

  const pickRandomMovie = useCallback(
    async (overrideWatchlistId?: string | null): Promise<MovieDto | null> => {
      if (isPickingRef.current) {
        return null;
      }

      const targetId =
        overrideWatchlistId === undefined ? watchlistId : overrideWatchlistId;

      isPickingRef.current = true;
      setIsPicking(true);
      setPickError(null);

      try {
        const picked = await getRandomMovie(targetId);
        setMovie(picked);
        setStoredMovieId(picked.id);
        if (targetId) {
          setWatchlistId(targetId);
          setStoredWatchlistId(targetId);
        } else {
          setWatchlistId(null);
          clearStoredWatchlistId();
        }
        return picked;
      } catch (error) {
        const message = error instanceof Error ? error.message : "Не вдалося обрати фільм";
        setPickError(message);
        return null;
      } finally {
        isPickingRef.current = false;
        setIsPicking(false);
      }
    },
    [watchlistId]
  );

  useEffect(() => {
    if (!isRestoringMovie) {
      return;
    }

    const storedMovieId = getStoredMovieId();
    const storedWatchlistId = getStoredWatchlistId();
    if (!storedWatchlistId || !storedMovieId) {
      setIsRestoringMovie(false);
      return;
    }

    let isCancelled = false;

    const restore = async () => {
      try {
        const restored = await getMovie(storedMovieId, storedWatchlistId);
        if (!isCancelled) {
          setMovie(restored);
          setWatchlistId(storedWatchlistId);
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
  }, [isRestoringMovie]);

  const state = useMemo<AppState>(
    () => ({
      watchlists,
      watchlistsLoading,
      watchlistsError,
      refreshWatchlists,
      watchlistId,
      setActiveWatchlistId,
      movie,
      setMovie,
      isRestoringMovie,
      isPicking,
      pickError,
      pickRandomMovie,
    }),
    [
      watchlists,
      watchlistsLoading,
      watchlistsError,
      refreshWatchlists,
      watchlistId,
      setActiveWatchlistId,
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

import type {
  CreateWatchlistRequest,
  ImportImdbWatchlistRequest,
  MovieDto,
  MovieFilterRequest,
  UpdateWatchlistRequest,
  WatchlistDto,
} from "../types/movie";
import { requestJson } from "./http";

const WATCHLIST_ID_KEY = "watchlistId";
const MOVIE_ID_KEY = "movieId";

export const getStoredWatchlistId = (): string | null => {
  return sessionStorage.getItem(WATCHLIST_ID_KEY);
};

export const setStoredWatchlistId = (id: string): void => {
  sessionStorage.setItem(WATCHLIST_ID_KEY, id);
};

export const clearStoredWatchlistId = (): void => {
  sessionStorage.removeItem(WATCHLIST_ID_KEY);
};

export const getStoredMovieId = (): string | null => {
  return sessionStorage.getItem(MOVIE_ID_KEY);
};

export const setStoredMovieId = (id: string): void => {
  sessionStorage.setItem(MOVIE_ID_KEY, id);
};

export const clearStoredMovieId = (): void => {
  sessionStorage.removeItem(MOVIE_ID_KEY);
};

const toSearchParams = (filter: MovieFilterRequest): URLSearchParams => {
  const params = new URLSearchParams();

  if (filter.watchlistId) {
    params.set("watchlistId", filter.watchlistId);
  }
  if (filter.query) {
    params.set("query", filter.query);
  }
  if (filter.yearFrom != null) {
    params.set("yearFrom", String(filter.yearFrom));
  }
  if (filter.yearTo != null) {
    params.set("yearTo", String(filter.yearTo));
  }
  if (filter.minRating != null) {
    params.set("minRating", String(filter.minRating));
  }
  if (filter.genres?.length) {
    for (const genre of filter.genres) {
      params.append("genres", genre);
    }
  }

  return params;
};

export const importWatchlistByUrl = async (url: string): Promise<WatchlistDto> => {
  const body: CreateWatchlistRequest = { url };
  return requestJson<WatchlistDto>("/api/watchlists", {
    method: "POST",
    body: JSON.stringify(body),
    admin: true,
  });
};

export const importImdbWatchlist = async (
  payload: ImportImdbWatchlistRequest
): Promise<WatchlistDto> => {
  return requestJson<WatchlistDto>("/api/watchlists/import-imdb", {
    method: "POST",
    body: JSON.stringify(payload),
    admin: true,
  });
};

export const getWatchlists = async (): Promise<WatchlistDto[]> => {
  return requestJson<WatchlistDto[]>("/api/watchlists");
};

export const getWatchlist = async (id: string): Promise<WatchlistDto> => {
  return requestJson<WatchlistDto>(`/api/watchlists/${encodeURIComponent(id)}`);
};

export const updateWatchlist = async (
  id: string,
  body: UpdateWatchlistRequest
): Promise<WatchlistDto> => {
  return requestJson<WatchlistDto>(`/api/watchlists/${encodeURIComponent(id)}`, {
    method: "PUT",
    body: JSON.stringify(body),
    admin: true,
  });
};

export const deleteWatchlist = async (id: string): Promise<void> => {
  await requestJson<void>(`/api/watchlists/${encodeURIComponent(id)}`, {
    method: "DELETE",
    admin: true,
  });
};

export const refreshWatchlist = async (id: string): Promise<WatchlistDto> => {
  return requestJson<WatchlistDto>(`/api/watchlists/${encodeURIComponent(id)}/refresh`, {
    method: "POST",
    admin: true,
  });
};

export const getMovies = async (filter: MovieFilterRequest): Promise<MovieDto[]> => {
  const params = toSearchParams(filter);
  const query = params.toString();
  return requestJson<MovieDto[]>(`/api/movies${query ? `?${query}` : ""}`);
};

export const getRandomMovie = async (watchlistId?: string | null): Promise<MovieDto> => {
  const params = toSearchParams({
    ...(watchlistId ? { watchlistId } : {}),
  });
  const query = params.toString();
  return requestJson<MovieDto>(`/api/movies/random${query ? `?${query}` : ""}`);
};

export const getMovie = async (id: string, watchlistId: string): Promise<MovieDto> => {
  const params = new URLSearchParams({ watchlistId });
  return requestJson<MovieDto>(
    `/api/movies/${encodeURIComponent(id)}?${params.toString()}`
  );
};

/** Legacy name kept for call sites that still import by URL via the server. */
export const importWatchlist = importWatchlistByUrl;

import type {
  CreateWatchlistRequest,
  MovieDto,
  MovieFilterRequest,
  WatchlistDto,
} from "../types/movie";

const WATCHLIST_ID_KEY = "watchlistId";

export function getStoredWatchlistId(): string | null {
  return sessionStorage.getItem(WATCHLIST_ID_KEY);
}

export function setStoredWatchlistId(id: string): void {
  sessionStorage.setItem(WATCHLIST_ID_KEY, id);
}

export function clearStoredWatchlistId(): void {
  sessionStorage.removeItem(WATCHLIST_ID_KEY);
}

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const body: unknown = await response.json();
    if (
      body &&
      typeof body === "object" &&
      "error" in body &&
      typeof (body as { error: unknown }).error === "string"
    ) {
      return (body as { error: string }).error;
    }
  } catch {
    // Fall through to status text.
  }

  return response.statusText || `Request failed (${response.status})`;
}

async function requestJson<T>(input: string, init?: RequestInit): Promise<T> {
  const response = await fetch(input, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    throw new Error(await readErrorMessage(response));
  }

  return (await response.json()) as T;
}

function toSearchParams(filter: MovieFilterRequest): URLSearchParams {
  const params = new URLSearchParams();
  params.set("watchlistId", filter.watchlistId);

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
}

export async function importWatchlist(url: string): Promise<WatchlistDto> {
  const body: CreateWatchlistRequest = { url };
  const watchlist = await requestJson<WatchlistDto>("/api/watchlists", {
    method: "POST",
    body: JSON.stringify(body),
  });

  setStoredWatchlistId(watchlist.id);
  return watchlist;
}

export async function getWatchlists(): Promise<WatchlistDto[]> {
  return requestJson<WatchlistDto[]>("/api/watchlists");
}

export async function getWatchlist(id: string): Promise<WatchlistDto> {
  return requestJson<WatchlistDto>(`/api/watchlists/${encodeURIComponent(id)}`);
}

export async function refreshWatchlist(id: string): Promise<WatchlistDto> {
  return requestJson<WatchlistDto>(`/api/watchlists/${encodeURIComponent(id)}/refresh`, {
    method: "POST",
  });
}

export async function getMovies(filter: MovieFilterRequest): Promise<MovieDto[]> {
  const params = toSearchParams(filter);
  return requestJson<MovieDto[]>(`/api/movies?${params.toString()}`);
}

export async function getRandomMovie(watchlistId: string): Promise<MovieDto> {
  const params = toSearchParams({ watchlistId });
  return requestJson<MovieDto>(`/api/movies/random?${params.toString()}`);
}

export async function getMovie(id: string, watchlistId: string): Promise<MovieDto> {
  const params = new URLSearchParams({ watchlistId });
  return requestJson<MovieDto>(
    `/api/movies/${encodeURIComponent(id)}?${params.toString()}`
  );
}

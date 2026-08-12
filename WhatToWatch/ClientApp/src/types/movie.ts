export interface MovieDto {
  id: string;
  title: string;
  year?: number;
  posterUrl?: string;
  rating?: number;
  plot?: string;
  runtimeMinutes?: number;
  director?: string;
  genres: string[];
}

export interface WatchlistDto {
  id: string;
  name: string;
  url?: string;
  lastRefreshedAt?: string;
  movies: MovieDto[];
}

export interface MovieFilterRequest {
  watchlistId: string;
  query?: string;
  yearFrom?: number;
  yearTo?: number;
  minRating?: number;
  genres?: string[];
}

export interface CreateWatchlistRequest {
  url: string;
}

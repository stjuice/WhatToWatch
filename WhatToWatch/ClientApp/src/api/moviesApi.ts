import type { MovieDto, MovieFilterRequest, WatchlistDto } from "../types/movie";

export async function getWatchlists(): Promise<WatchlistDto[]> {
  throw new Error("Not implemented");
}

export async function getWatchlist(id: string): Promise<WatchlistDto> {
  throw new Error("Not implemented");
}

export async function refreshWatchlist(id: string): Promise<void> {
  throw new Error("Not implemented");
}

export async function getMovies(filter?: MovieFilterRequest): Promise<MovieDto[]> {
  throw new Error("Not implemented");
}

export async function getRandomMovie(filter?: MovieFilterRequest): Promise<MovieDto> {
  throw new Error("Not implemented");
}

export async function getMovie(id: string): Promise<MovieDto> {
  throw new Error("Not implemented");
}

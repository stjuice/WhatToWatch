import type { Movie } from "./movie";

export interface SpookieTicket {
  key: string;
  isBonus: boolean;
  isCurrent: boolean;
  movie: Movie;
}

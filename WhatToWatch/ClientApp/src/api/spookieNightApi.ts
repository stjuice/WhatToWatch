import type { Movie } from "../types/movie";
import type { SpookieTicket } from "../types/spookieNight";
import { requestJson } from "./http";

export const getSpookieTickets = async (): Promise<SpookieTicket[]> => {
  return requestJson<SpookieTicket[]>("/api/spookie-night");
};

export const getSpookieMovie = async (key: string): Promise<Movie> => {
  return requestJson<Movie>(`/api/spookie-night/${encodeURIComponent(key)}`);
};

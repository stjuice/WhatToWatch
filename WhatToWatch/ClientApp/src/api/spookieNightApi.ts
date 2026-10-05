import type { SpookieMovie, SpookieTicket } from "../types/spookieNight";
import { requestJson } from "./http";

export const getSpookieTickets = async (): Promise<SpookieTicket[]> => {
  return requestJson<SpookieTicket[]>("/api/spookie-night");
};

export const getSpookieMovie = async (key: string): Promise<SpookieMovie> => {
  return requestJson<SpookieMovie>(`/api/spookie-night/${encodeURIComponent(key)}`);
};

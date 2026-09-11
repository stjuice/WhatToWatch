import type { Movie } from "./movie";

export const PartyStatuses = {
  Playing: "Playing",
  Matched: "Matched",
  Finished: "Finished",
  Expired: "Expired",
} as const;

export type PartyStatus =
  (typeof PartyStatuses)[keyof typeof PartyStatuses];

export interface CreatePartyRequest {
  watchlistId?: string | null;
  joinCode?: string | null;
}

export interface JoinPartyRequest {
  joinCode: string;
}

export interface PartyVoteRequest {
  movieId: string;
  liked: boolean;
}

export interface IPartyProgress {
  currentIndex: number;
  totalMovies: number;
  isExhausted: boolean;
}

export interface IPartyMovieBatchItem {
  orderIndex: number;
  movie: Movie;
}

export interface PartyState {
  partyId: string;
  joinCode: string;
  status: PartyStatus;
  playerCount: number;
  opponentPresent: boolean;
  opponentOnline: boolean;
  progress: IPartyProgress;
  batch: IPartyMovieBatchItem[];
  currentMovie?: Movie | null;
  matchedMovie?: Movie | null;
}

export interface IPartySession extends PartyState {
  playerToken: string;
}

export interface IPartyPreview {
  partyId: string;
  joinCode: string;
  status: PartyStatus;
  playerCount: number;
  isFull: boolean;
}

export interface ISuggestedCode {
  joinCode: string;
}

export interface IPartyError {
  code: string;
  message: string;
}

import type { MovieDto } from "./movie";

export type PartyStatus = "Playing" | "Matched" | "Finished" | "Expired";

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

export interface PartyProgressDto {
  currentIndex: number;
  totalMovies: number;
  isExhausted: boolean;
}

export interface PartyStateDto {
  partyId: string;
  joinCode: string;
  status: PartyStatus;
  playerCount: number;
  opponentPresent: boolean;
  opponentOnline: boolean;
  progress: PartyProgressDto;
  currentMovie?: MovieDto | null;
  matchedMovie?: MovieDto | null;
}

export interface PartySessionDto extends PartyStateDto {
  playerToken: string;
}

export interface PartyPreviewDto {
  partyId: string;
  joinCode: string;
  status: PartyStatus;
  playerCount: number;
  isFull: boolean;
}

export interface SuggestedCodeDto {
  joinCode: string;
}

export interface PartyErrorDto {
  code: string;
  message: string;
}

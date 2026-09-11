import type {
  IPartyPreview,
  IPartySession,
  PartyState,
  ISuggestedCode,
  CreatePartyRequest,
  PartyVoteRequest,
} from "../types/party";
import { requestJson } from "./http";

export const suggestPartyCode = async (): Promise<ISuggestedCode> => {
  return requestJson<ISuggestedCode>("/api/parties/suggest-code");
};

export const createParty = async (
  request: CreatePartyRequest
): Promise<IPartySession> => {
  return requestJson<IPartySession>("/api/parties", {
    method: "POST",
    body: JSON.stringify(request),
  });
};

export const joinParty = async (joinCode: string): Promise<IPartySession> => {
  return requestJson<IPartySession>("/api/parties/join", {
    method: "POST",
    body: JSON.stringify({ joinCode }),
  });
};

export const getPartyPreview = async (
  joinCode: string
): Promise<IPartyPreview> => {
  return requestJson<IPartyPreview>(
    `/api/parties/by-code/${encodeURIComponent(joinCode)}`
  );
};

export const getPartyState = async (
  partyId: string,
  playerToken: string
): Promise<PartyState> => {
  return requestJson<PartyState>(
    `/api/parties/${encodeURIComponent(partyId)}`,
    { playerToken }
  );
};

export const voteInParty = async (
  partyId: string,
  playerToken: string,
  request: PartyVoteRequest
): Promise<PartyState> => {
  return requestJson<PartyState>(
    `/api/parties/${encodeURIComponent(partyId)}/vote`,
    {
      method: "POST",
      body: JSON.stringify(request),
      playerToken,
    }
  );
};

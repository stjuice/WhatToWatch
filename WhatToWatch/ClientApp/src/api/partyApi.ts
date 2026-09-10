import type {
  CreatePartyRequest,
  PartyPreviewDto,
  PartySessionDto,
  PartyStateDto,
  PartyVoteRequest,
  SuggestedCodeDto,
} from "../types/party";
import { requestJson } from "./http";

export const suggestPartyCode = async (): Promise<SuggestedCodeDto> => {
  return requestJson<SuggestedCodeDto>("/api/parties/suggest-code");
};

export const createParty = async (
  request: CreatePartyRequest
): Promise<PartySessionDto> => {
  return requestJson<PartySessionDto>("/api/parties", {
    method: "POST",
    body: JSON.stringify(request),
  });
};

export const joinParty = async (joinCode: string): Promise<PartySessionDto> => {
  return requestJson<PartySessionDto>("/api/parties/join", {
    method: "POST",
    body: JSON.stringify({ joinCode }),
  });
};

export const getPartyPreview = async (
  joinCode: string
): Promise<PartyPreviewDto> => {
  return requestJson<PartyPreviewDto>(
    `/api/parties/by-code/${encodeURIComponent(joinCode)}`
  );
};

export const getPartyState = async (
  partyId: string,
  playerToken: string
): Promise<PartyStateDto> => {
  return requestJson<PartyStateDto>(
    `/api/parties/${encodeURIComponent(partyId)}`,
    { playerToken }
  );
};

export const voteInParty = async (
  partyId: string,
  playerToken: string,
  request: PartyVoteRequest
): Promise<PartyStateDto> => {
  return requestJson<PartyStateDto>(
    `/api/parties/${encodeURIComponent(partyId)}/vote`,
    {
      method: "POST",
      body: JSON.stringify(request),
      playerToken,
    }
  );
};

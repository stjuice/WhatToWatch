export const PARTY_ID_STORAGE = "w2w.party.partyId";
export const PLAYER_TOKEN_STORAGE = "w2w.party.playerToken";

export interface StoredPartySession {
  partyId: string;
  playerToken: string;
}

export const getPartySession = (): StoredPartySession | null => {
  try {
    const partyId = sessionStorage.getItem(PARTY_ID_STORAGE);
    const playerToken = sessionStorage.getItem(PLAYER_TOKEN_STORAGE);

    return partyId && playerToken ? { partyId, playerToken } : null;
  } catch {
    return null;
  }
};

export const setPartySession = (session: StoredPartySession): void => {
  sessionStorage.setItem(PARTY_ID_STORAGE, session.partyId);
  sessionStorage.setItem(PLAYER_TOKEN_STORAGE, session.playerToken);
};

export const clearPartySession = (): void => {
  sessionStorage.removeItem(PARTY_ID_STORAGE);
  sessionStorage.removeItem(PLAYER_TOKEN_STORAGE);
};

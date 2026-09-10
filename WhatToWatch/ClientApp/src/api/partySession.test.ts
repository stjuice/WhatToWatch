/**
 * @vitest-environment jsdom
 */

import { beforeEach, describe, expect, it } from "vitest";
import {
  clearPartySession,
  getPartySession,
  PARTY_ID_STORAGE,
  PLAYER_TOKEN_STORAGE,
  setPartySession,
} from "./partySession";

beforeEach(() => {
  sessionStorage.clear();
});

describe("party session storage", () => {
  it("round-trips a party id and player token", () => {
    const session = { partyId: "party-1", playerToken: "token-1" };

    setPartySession(session);

    expect(getPartySession()).toEqual(session);
    expect(sessionStorage.getItem(PARTY_ID_STORAGE)).toBe("party-1");
    expect(sessionStorage.getItem(PLAYER_TOKEN_STORAGE)).toBe("token-1");
  });

  it("requires both values and clears them together", () => {
    sessionStorage.setItem(PARTY_ID_STORAGE, "party-1");
    expect(getPartySession()).toBeNull();

    clearPartySession();

    expect(sessionStorage.getItem(PARTY_ID_STORAGE)).toBeNull();
    expect(sessionStorage.getItem(PLAYER_TOKEN_STORAGE)).toBeNull();
  });
});

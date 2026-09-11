import { describe, expect, it } from "vitest";
import { ApiError } from "../api/http";
import { ukr } from "../i18n/ukr";
import { getPartyErrorText, partyErrorTextKeys } from "./partyText";

const serverErrorCodes = [
  "InvalidCode",
  "CodeTaken",
  "PartyExpired",
  "PartyFull",
  "AlreadyFinished",
  "InvalidPlayerToken",
  "NotYourCurrentMovie",
  "NoMovies",
  "NoCodesAvailable",
] as const;

describe("party error text", () => {
  it("maps every server error code to a Ukrainian text key that exists", () => {
    expect(Object.keys(partyErrorTextKeys).sort()).toEqual(
      [...serverErrorCodes].sort()
    );

    for (const code of serverErrorCodes) {
      expect(ukr).toHaveProperty(partyErrorTextKeys[code]);
    }
  });

  it("uses localized text for a known error code", () => {
    expect(
      getPartyErrorText(
        new ApiError("Server fallback", "NotYourCurrentMovie"),
        "party.errors.vote"
      )
    ).toBe(ukr["party.errors.notYourCurrentMovie"]);
  });

  it("falls back to the server message for an unknown error code", () => {
    expect(
      getPartyErrorText(
        new ApiError("New server message", "FuturePartyError"),
        "party.errors.action"
      )
    ).toBe("New server message");
  });
});

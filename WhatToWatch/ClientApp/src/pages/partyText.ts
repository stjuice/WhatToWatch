import { ApiError } from "../api/http";
import { text } from "../i18n/text";
import { TextKeys } from "../i18n/textKeys";
import type { UkrKey } from "../i18n/ukr";

export const partyErrorCodes = {
  InvalidCode: "InvalidCode",
  CodeTaken: "CodeTaken",
  PartyExpired: "PartyExpired",
  PartyFull: "PartyFull",
  AlreadyFinished: "AlreadyFinished",
  InvalidPlayerToken: "InvalidPlayerToken",
  NotYourCurrentMovie: "NotYourCurrentMovie",
  NoMovies: "NoMovies",
  NoCodesAvailable: "NoCodesAvailable",
} as const;

export type PartyErrorCode =
  (typeof partyErrorCodes)[keyof typeof partyErrorCodes];

export const partyErrorTextKeys = {
  [partyErrorCodes.InvalidCode]: TextKeys.Party_Errors_InvalidCode,
  [partyErrorCodes.CodeTaken]: TextKeys.Party_Errors_CodeTaken,
  [partyErrorCodes.PartyExpired]: TextKeys.Party_Expired,
  [partyErrorCodes.PartyFull]: TextKeys.Party_Errors_Full,
  [partyErrorCodes.AlreadyFinished]: TextKeys.Party_Errors_Finished,
  [partyErrorCodes.InvalidPlayerToken]:
    TextKeys.Party_Errors_InvalidPlayerToken,
  [partyErrorCodes.NotYourCurrentMovie]:
    TextKeys.Party_Errors_NotYourCurrentMovie,
  [partyErrorCodes.NoMovies]: TextKeys.Party_Errors_NoMovies,
  [partyErrorCodes.NoCodesAvailable]: TextKeys.Party_Errors_NoCodesAvailable,
} as const satisfies Record<PartyErrorCode, UkrKey>;

const isPartyErrorCode = (code: string): code is PartyErrorCode =>
  Object.prototype.hasOwnProperty.call(partyErrorTextKeys, code);

export const getPartyErrorText = (
  error: unknown,
  fallbackKey: UkrKey
): string => {
  if (error instanceof ApiError && error.code && isPartyErrorCode(error.code))
    return text(partyErrorTextKeys[error.code]);

  return error instanceof Error ? error.message : text(fallbackKey);
};

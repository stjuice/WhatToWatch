namespace WhatToWatch.Services;

public sealed class PartyException(
    string code,
    string message,
    int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;

    public static PartyException InvalidCode() =>
        new("InvalidCode", "Join code must contain exactly four digits.", 400);

    public static PartyException CodeTaken() =>
        new("CodeTaken", "That join code is already in use.", 409);

    public static PartyException PartyExpired() =>
        new("PartyExpired", "This party has expired.", 410);

    public static PartyException PartyFull() =>
        new("PartyFull", "This party already has two players.", 409);

    public static PartyException AlreadyFinished() =>
        new("AlreadyFinished", "This party is no longer accepting votes.", 409);

    public static PartyException InvalidPlayerToken() =>
        new("InvalidPlayerToken", "The player token is invalid.", 401);

    public static PartyException NotYourCurrentMovie() =>
        new("NotYourCurrentMovie", "The vote is not for the player's current movie.", 409);

    public static PartyException NoMovies() =>
        new("NoMovies", "No movies are available for this party.", 422);

    public static PartyException NoCodesAvailable() =>
        new("NoCodesAvailable", "No join codes are currently available.", 503);
}

import { describe, expect, it } from "vitest";
import { getSpookiePoster } from "./spookiePosters";

describe("getSpookiePoster", () => {
  it.each(["1", "2", "3", "4"])("returns the season poster for ticket %s", (key) => {
    expect(getSpookiePoster(key)).toBeTruthy();
  });

  it("returns nothing for the bonus ticket so the IMDb poster is used", () => {
    expect(getSpookiePoster("bonus")).toBeUndefined();
  });
});

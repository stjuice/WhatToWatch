import { describe, expect, it } from "vitest";
import { LIST_VISIBLE_ROWS, splitWatchlistHeadline } from "./listMarquee";

describe("list marquee", () => {
  it("keeps the expanded viewport at eight rows", () => {
    expect(LIST_VISIBLE_ROWS).toBe(8);
  });

  it("uses the full name as the title when there is no year line", () => {
    expect(splitWatchlistHeadline("Хорори")).toEqual({
      title: "Хорори",
      subtitle: null,
    });
  });

  it("splits a newline title and year range", () => {
    expect(splitWatchlistHeadline("TOP 100 animated movies:\n2000 – 2012")).toEqual({
      title: "TOP 100 animated movies:",
      subtitle: "2000 – 2012",
    });
  });

  it("splits a colon plus year range and keeps the colon on the title", () => {
    expect(splitWatchlistHeadline("TOP 100 animated movies: 2000-2012")).toEqual({
      title: "TOP 100 animated movies:",
      subtitle: "2000 – 2012",
    });
  });

  it("splits a trailing year range without a colon", () => {
    expect(splitWatchlistHeadline("Мультфільми 2000 — 2012")).toEqual({
      title: "Мультфільми",
      subtitle: "2000 – 2012",
    });
  });

  it("does not treat a single year as a subtitle", () => {
    expect(splitWatchlistHeadline("The Matrix 1999")).toEqual({
      title: "The Matrix 1999",
      subtitle: null,
    });
  });

  it("does not split a colon that is not followed by a year range", () => {
    expect(splitWatchlistHeadline("Mission: Impossible")).toEqual({
      title: "Mission: Impossible",
      subtitle: null,
    });
  });

  it("returns an empty title for blank names", () => {
    expect(splitWatchlistHeadline("   ")).toEqual({ title: "", subtitle: null });
  });
});

import { describe, expect, it } from "vitest";
import {
  COMPACT_LABEL_FRAME,
  LARGE_LABEL_FRAME,
  fitLabelFontRem,
} from "./watchlistBucketLabel";

describe("watchlist bucket label", () => {
  it("uses the largest font for short titles", () => {
    expect(fitLabelFontRem("Хорори", COMPACT_LABEL_FRAME)).toBe(COMPACT_LABEL_FRAME.maxFontRem);
  });

  it("shrinks longer titles below the short-title size", () => {
    const short = fitLabelFontRem("Комедії", COMPACT_LABEL_FRAME);
    const long = fitLabelFontRem(
      "Фільми на новорічні свята з усією родиною",
      COMPACT_LABEL_FRAME
    );

    expect(long).toBeLessThan(short);
    expect(long).toBeGreaterThanOrEqual(COMPACT_LABEL_FRAME.minFontRem);
  });

  it("fills the compact frame for a typical two-word title", () => {
    const font = fitLabelFontRem("Мультфільми для дітей", COMPACT_LABEL_FRAME);
    expect(font).toBeGreaterThanOrEqual(0.9);
    expect(font).toBeLessThanOrEqual(COMPACT_LABEL_FRAME.maxFontRem);
  });

  it("never goes below the minimum, even for one unbreakable word", () => {
    expect(fitLabelFontRem("Нерозривнедовгеслововодинрядок", COMPACT_LABEL_FRAME)).toBe(
      COMPACT_LABEL_FRAME.minFontRem
    );
  });

  it("keeps every result inside the frame's font bounds", () => {
    const titles = ["A", "Хорори", "Мультфільми для дітей", "Довгий список фільмів на вечір"];

    for (const title of titles) {
      const font = fitLabelFontRem(title, COMPACT_LABEL_FRAME);
      expect(font).toBeGreaterThanOrEqual(COMPACT_LABEL_FRAME.minFontRem);
      expect(font).toBeLessThanOrEqual(COMPACT_LABEL_FRAME.maxFontRem);
    }
  });

  it("gives the same title a bigger font in the zoomed-in frame", () => {
    const title = "Мультфільми для дітей";
    expect(fitLabelFontRem(title, LARGE_LABEL_FRAME)).toBeGreaterThan(
      fitLabelFontRem(title, COMPACT_LABEL_FRAME)
    );
  });

  it("falls back to the maximum font for blank titles", () => {
    expect(fitLabelFontRem("   ", COMPACT_LABEL_FRAME)).toBe(COMPACT_LABEL_FRAME.maxFontRem);
  });
});

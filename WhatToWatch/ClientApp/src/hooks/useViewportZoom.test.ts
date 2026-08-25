import { describe, expect, it } from "vitest";
import { resolveZoomedIn } from "./useViewportZoom";

describe("resolveZoomedIn", () => {
  it("stays zoomed out at the default scale", () => {
    expect(resolveZoomedIn(1, false)).toBe(false);
  });

  it("switches to the large layout once the pinch passes the threshold", () => {
    expect(resolveZoomedIn(1.25, false)).toBe(true);
  });

  it("holds the large layout through small scale dips", () => {
    expect(resolveZoomedIn(1.15, true)).toBe(true);
    expect(resolveZoomedIn(1.15, false)).toBe(false);
  });

  it("returns to the grid when the user zooms back out", () => {
    expect(resolveZoomedIn(1, true)).toBe(false);
  });

  it("keeps the current layout when the scale is unusable", () => {
    expect(resolveZoomedIn(Number.NaN, true)).toBe(true);
    expect(resolveZoomedIn(0, false)).toBe(false);
  });
});

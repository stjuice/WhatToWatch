import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  getPlatform: vi.fn(),
}));

vi.mock("@capacitor/core", () => ({
  Capacitor: {
    getPlatform: () => mocks.getPlatform(),
  },
}));

describe("isAndroidApk", () => {
  beforeEach(() => {
    mocks.getPlatform.mockReset();
    vi.resetModules();
  });

  it("returns true in the Android APK", async () => {
    mocks.getPlatform.mockReturnValue("android");
    const { isAndroidApk } = await import("./isAndroidApk");

    expect(isAndroidApk()).toBe(true);
  });

  it("returns false in the browser and installed PWA", async () => {
    mocks.getPlatform.mockReturnValue("web");
    const { isAndroidApk } = await import("./isAndroidApk");

    expect(isAndroidApk()).toBe(false);
  });

  it("returns false on other native platforms", async () => {
    mocks.getPlatform.mockReturnValue("ios");
    const { isAndroidApk } = await import("./isAndroidApk");

    expect(isAndroidApk()).toBe(false);
  });
});

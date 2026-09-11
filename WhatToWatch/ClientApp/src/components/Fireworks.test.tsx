/**
 * @vitest-environment jsdom
 */

import { act, cleanup, fireEvent, render } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { Fireworks } from "./Fireworks";

const setReducedMotion = (matches: boolean) => {
  Object.defineProperty(window, "matchMedia", {
    configurable: true,
    writable: true,
    value: vi.fn().mockReturnValue({
      matches,
      media: "(prefers-reduced-motion: reduce)",
      onchange: null,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }),
  });
};

beforeEach(() => {
  vi.useFakeTimers();
  setReducedMotion(false);
});

afterEach(() => {
  cleanup();
  vi.clearAllTimers();
  vi.useRealTimers();
  vi.restoreAllMocks();
});

describe("Fireworks", () => {
  it("stays in the DOM until its duration and fade have both elapsed", () => {
    const { container } = render(<Fireworks durationMs={100} fadeMs={50} />);

    act(() => vi.advanceTimersByTime(99));
    expect(container.querySelector(".fireworks")).not.toBeNull();

    act(() => vi.advanceTimersByTime(1));
    expect(container.querySelector(".fireworks")).not.toBeNull();

    act(() => vi.advanceTimersByTime(49));
    expect(container.querySelector(".fireworks")).not.toBeNull();

    act(() => vi.advanceTimersByTime(1));
    expect(container.querySelector(".fireworks")).toBeNull();
  });

  it("starts fading early when the user taps", () => {
    const { container } = render(<Fireworks durationMs={5000} fadeMs={50} />);

    fireEvent.pointerDown(window);
    expect(container.querySelector(".fireworks--fading")).not.toBeNull();

    act(() => vi.advanceTimersByTime(50));
    expect(container.querySelector(".fireworks")).toBeNull();
  });

  it("applies the fading class before removing the overlay", () => {
    const { container } = render(<Fireworks durationMs={100} fadeMs={50} />);

    act(() => vi.advanceTimersByTime(100));
    const overlay = container.querySelector(".fireworks");
    expect(overlay?.classList.contains("fireworks--fading")).toBe(true);

    act(() => vi.advanceTimersByTime(50));
    expect(container.querySelector(".fireworks")).toBeNull();
  });

  it("renders nothing when reduced motion is preferred", () => {
    setReducedMotion(true);

    const { container } = render(<Fireworks />);

    expect(container.querySelector(".fireworks")).toBeNull();
    expect(vi.getTimerCount()).toBe(0);
  });

  it("cleans up its timers and pointer listener on unmount", () => {
    const addListener = vi.spyOn(window, "addEventListener");
    const removeListener = vi.spyOn(window, "removeEventListener");
    const { unmount } = render(<Fireworks />);
    const pointerCall = addListener.mock.calls.find(
      ([eventName]) => eventName === "pointerdown"
    );

    expect(pointerCall).toBeDefined();
    expect(vi.getTimerCount()).toBe(1);

    unmount();

    expect(vi.getTimerCount()).toBe(0);
    expect(removeListener).toHaveBeenCalledWith("pointerdown", pointerCall?.[1]);
  });
});

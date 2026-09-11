/**
 * @vitest-environment jsdom
 */

import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import popcorn from "../assets/popcorn-full.svg";
import { COMPACT_LABEL_FRAME, fitLabelFontRem } from "./labelPill";
import { LabelPill } from "./LabelPillView";
import { PopcornBucket } from "./PopcornBucket";
import { PopcornButton } from "./PopcornButton";
import { WatchlistBucket } from "./WatchlistBucket";

afterEach(cleanup);

describe("LabelPill", () => {
  it("applies the fitted font size", () => {
    const label = "Мультфільми для дітей";
    render(<LabelPill label={label} frame={COMPACT_LABEL_FRAME} />);

    expect(screen.getByText(label).style.fontSize).toBe(
      `${fitLabelFontRem(label, COMPACT_LABEL_FRAME)}rem`
    );
  });
});

describe("PopcornBucket", () => {
  it("renders as a router link", () => {
    render(
      <MemoryRouter>
        <PopcornBucket
          art={popcorn}
          label="Всі списки"
          size="ml"
          frame={COMPACT_LABEL_FRAME}
          labelCenter="58%"
          to="/lists"
        />
      </MemoryRouter>
    );

    expect(screen.getByRole("link", { name: "Всі списки" }).getAttribute("href")).toBe(
      "/lists"
    );
  });

  it("renders as a button", () => {
    const onClick = vi.fn();
    render(
      <PopcornBucket
        art={popcorn}
        label="Почати"
        size="m"
        frame={COMPACT_LABEL_FRAME}
        labelCenter="58%"
        onClick={onClick}
      />
    );

    fireEvent.click(screen.getByRole("button", { name: "Почати" }));
    expect(onClick).toHaveBeenCalledOnce();
  });
});

describe("PopcornButton", () => {
  it("forwards disabled and busy state", () => {
    render(
      <PopcornButton
        art={popcorn}
        label="Вибираємо"
        size="l"
        disabled
        busy
        onClick={() => undefined}
      />
    );

    const button = screen.getByRole("button", { name: "Вибираємо" });
    expect(button.hasAttribute("disabled")).toBe(true);
    expect(button.getAttribute("aria-busy")).toBe("true");
  });
});

describe("WatchlistBucket", () => {
  it("still links to its list", () => {
    render(
      <MemoryRouter>
        <WatchlistBucket id="family night" name="Сімейні" size="ml" />
      </MemoryRouter>
    );

    expect(screen.getByRole("link").getAttribute("href")).toBe(
      "/list/family%20night"
    );
  });
});

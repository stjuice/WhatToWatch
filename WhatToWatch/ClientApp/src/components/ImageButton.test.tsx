/**
 * @vitest-environment jsdom
 */

import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import { MainButton } from "./MainButton";
import { NoButton } from "./NoButton";
import { PartyButton } from "./PartyButton";
import { RandomButton } from "./RandomButton";
import { YesButton } from "./YesButton";

afterEach(cleanup);

describe("specific image buttons", () => {
  it.each([
    [MainButton, "Main", "l", "popcorn-full.svg"],
    [RandomButton, "Random", "ml", "popcorn-pivot.svg"],
    [YesButton, "Yes", "m", "yes.svg"],
    [NoButton, "No", "s", "no.svg"],
    [PartyButton, "Party", "ms", "tinder.svg"],
  ] as const)("renders %s with its asset and size", (Component, label, size, asset) => {
    render(<Component label={label} size={size} />);

    const button = screen.getByRole("button", { name: label });
    expect(button.classList.contains(`artwork-size--${size}`)).toBe(true);
    expect(button.querySelector("img")?.getAttribute("src")).toContain(asset);
  });

  it("keeps the polymorphic Link behavior", () => {
    render(
      <MemoryRouter>
        <PartyButton label="Start party" size="ms" to="/party" />
      </MemoryRouter>
    );

    expect(screen.getByRole("link", { name: "Start party" }).getAttribute("href")).toBe(
      "/party"
    );
  });
});

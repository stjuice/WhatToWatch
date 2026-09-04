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
    [MainButton, "Main", "large", "popcorn-full.svg"],
    [RandomButton, "Random", "middle", "popcorn-pivot.svg"],
    [YesButton, "Yes", "small", "yes.svg"],
    [NoButton, "No", "small", "no.svg"],
    [PartyButton, "Party", "large", "tinder.svg"],
  ] as const)("renders %s with its asset and size", (Component, label, size, asset) => {
    render(<Component label={label} size={size} />);

    const button = screen.getByRole("button", { name: label });
    expect(button.classList.contains(`image-button--${size}`)).toBe(true);
    expect(button.querySelector("img")?.getAttribute("src")).toContain(asset);
  });

  it("keeps the polymorphic Link behavior", () => {
    render(
      <MemoryRouter>
        <PartyButton label="Start party" size="large" to="/party" />
      </MemoryRouter>
    );

    expect(screen.getByRole("link", { name: "Start party" }).getAttribute("href")).toBe(
      "/party"
    );
  });
});

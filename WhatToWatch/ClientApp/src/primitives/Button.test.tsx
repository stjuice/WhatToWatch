/**
 * @vitest-environment jsdom
 */

import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Button } from "./Button";

describe("Button", () => {
  it("renders its content as a button", () => {
    render(<Button>Choose a movie</Button>);

    expect(screen.getByRole("button", { name: "Choose a movie" })).toBeTruthy();
  });
});

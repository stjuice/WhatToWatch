/**
 * @vitest-environment jsdom
 */

import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { Button } from "./Button";

afterEach(cleanup);

describe("Button", () => {
  it("defaults to a native button with type button", () => {
    render(<Button>Choose a movie</Button>);

    expect(screen.getByRole("button", { name: "Choose a movie" }).getAttribute("type")).toBe(
      "button"
    );
  });

  it("renders an anchor for href", () => {
    render(<Button href="https://www.imdb.com">IMDb</Button>);

    const anchor = screen.getByRole("link", { name: "IMDb" });
    expect(anchor.tagName).toBe("A");
    expect(anchor.getAttribute("href")).toBe("https://www.imdb.com");
  });

  it("renders a router Link for to", () => {
    render(
      <MemoryRouter>
        <Button to="/lists">All lists</Button>
      </MemoryRouter>
    );

    const link = screen.getByRole("link", { name: "All lists" });
    expect(link.tagName).toBe("A");
    expect(link.getAttribute("href")).toBe("/lists");
  });

  it("applies shared, variant, and custom classes", () => {
    render(
      <Button variant="plain" className="movie-action">
        Details
      </Button>
    );

    expect(screen.getByRole("button", { name: "Details" }).className).toBe(
      "button button--plain movie-action"
    );
  });

  it("passes disabled and aria-busy through to native buttons", () => {
    render(
      <Button disabled aria-busy="true">
        Loading
      </Button>
    );

    const button = screen.getByRole("button", { name: "Loading" });
    expect(button.hasAttribute("disabled")).toBe(true);
    expect(button.getAttribute("aria-busy")).toBe("true");
  });

  it("fires onClick", () => {
    const onClick = vi.fn();
    render(<Button onClick={onClick}>Choose</Button>);

    fireEvent.click(screen.getByRole("button", { name: "Choose" }));

    expect(onClick).toHaveBeenCalledOnce();
  });
});

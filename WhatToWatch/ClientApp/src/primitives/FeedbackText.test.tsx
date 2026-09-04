/**
 * @vitest-environment jsdom
 */

import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { ErrorText } from "./ErrorText";
import { StatusText } from "./StatusText";

afterEach(cleanup);

describe("StatusText", () => {
  it("passes a custom class through", () => {
    render(<StatusText className="page-status">Loading</StatusText>);

    expect(screen.getByText("Loading").classList.contains("page-status")).toBe(true);
  });

  it("renders nothing for empty content", () => {
    const { container } = render(<StatusText>{null}</StatusText>);

    expect(container.childElementCount).toBe(0);
  });
});

describe("ErrorText", () => {
  it("renders an alert and passes a custom class through", () => {
    render(<ErrorText className="page-error">Something failed</ErrorText>);

    const error = screen.getByRole("alert");
    expect(error.textContent).toBe("Something failed");
    expect(error.classList.contains("page-error")).toBe(true);
  });

  it("renders nothing for empty content", () => {
    const { container } = render(<ErrorText>{""}</ErrorText>);

    expect(container.childElementCount).toBe(0);
  });
});

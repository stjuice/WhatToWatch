/**
 * @vitest-environment jsdom
 */

import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getSpookieTickets } from "../api/spookieNightApi";
import { routePaths } from "../routes/routePaths";
import type { SpookieTicket } from "../types/spookieNight";
import { SpookieNightPage } from "./SpookieNightPage";
import { SPOOKIE_OPENED_KEY, getOpenedSpookieTickets } from "./spookieNightStorage";

vi.mock("../api/spookieNightApi", () => ({
  getSpookieTickets: vi.fn(),
  getSpookieMovie: vi.fn(),
}));

const tickets: SpookieTicket[] = [
  {
    key: "1",
    isBonus: false,
    isCurrent: false,
    movie: { id: "tt1", title: "Repo! The Genetic Opera", genres: ["Horror", "Musical"] },
  },
  {
    key: "2",
    isBonus: false,
    isCurrent: true,
    movie: { id: "tt2", title: "Suspiria", genres: ["Horror"] },
  },
];

const LocationProbe = () => {
  const { pathname } = useLocation();
  return <output aria-label="current path">{pathname}</output>;
};

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={[routePaths.spookieNight]}>
      <Routes>
        <Route path={routePaths.spookieNight} element={<SpookieNightPage />} />
        <Route path={routePaths.spookieMoviePattern} element={null} />
      </Routes>
      <LocationProbe />
    </MemoryRouter>
  );

beforeEach(() => {
  localStorage.clear();
  vi.mocked(getSpookieTickets).mockResolvedValue(tickets);
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("SpookieNightPage", () => {
  it("shows opened tickets with their title and hides unopened ones", async () => {
    localStorage.setItem(SPOOKIE_OPENED_KEY, JSON.stringify(["1"]));
    renderPage();

    const opened = await screen.findByRole("button", { name: "Repo! The Genetic Opera" });
    const closed = screen.getByRole("button", { name: "Відкрити квиток #2" });

    expect(opened.classList.contains("spookie-ticket--opened")).toBe(true);
    expect(opened.textContent).toContain("Horror | Musical");
    expect(closed.classList.contains("spookie-ticket--opened")).toBe(false);
    expect(closed.textContent).toContain("Відкрити");
    expect(closed.textContent).not.toContain("Suspiria");
  });

  it("dims every ticket except the current one", async () => {
    renderPage();

    const past = await screen.findByRole("button", { name: "Відкрити квиток #1" });
    const current = screen.getByRole("button", { name: "Відкрити квиток #2" });

    expect(past.classList.contains("spookie-ticket--past")).toBe(true);
    expect(current.classList.contains("spookie-ticket--past")).toBe(false);
  });

  it("remembers an opened ticket and navigates to its movie", async () => {
    renderPage();

    fireEvent.click(await screen.findByRole("button", { name: "Відкрити квиток #2" }));

    expect(getOpenedSpookieTickets().has("2")).toBe(true);
    expect(screen.getByLabelText("current path").textContent).toBe(
      routePaths.spookieMovie("2")
    );
  });

  it("labels the bonus ticket as bonus", async () => {
    vi.mocked(getSpookieTickets).mockResolvedValue([
      {
        key: "bonus",
        isBonus: true,
        isCurrent: true,
        movie: { id: "tt3", title: "Halloween", genres: ["Horror"] },
      },
    ]);
    renderPage();

    const bonus = await screen.findByRole("button", { name: "Відкрити квиток bonus" });
    expect(bonus.classList.contains("spookie-ticket--bonus")).toBe(true);
  });
});

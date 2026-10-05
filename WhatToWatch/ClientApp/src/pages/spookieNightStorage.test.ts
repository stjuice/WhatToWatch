/**
 * @vitest-environment jsdom
 */

import { beforeEach, describe, expect, it } from "vitest";
import {
  SPOOKIE_OPENED_KEY,
  getOpenedSpookieTickets,
  markSpookieTicketOpened,
} from "./spookieNightStorage";

beforeEach(() => {
  localStorage.clear();
});

describe("spookieNightStorage", () => {
  it("starts with no opened tickets", () => {
    expect(getOpenedSpookieTickets().size).toBe(0);
  });

  it("keeps each opened ticket once", () => {
    markSpookieTicketOpened("1");
    markSpookieTicketOpened("bonus");
    markSpookieTicketOpened("1");

    expect(JSON.parse(localStorage.getItem(SPOOKIE_OPENED_KEY) ?? "[]")).toEqual([
      "1",
      "bonus",
    ]);
  });

  it("ignores corrupted storage", () => {
    localStorage.setItem(SPOOKIE_OPENED_KEY, "{not json");
    expect(getOpenedSpookieTickets().size).toBe(0);

    localStorage.setItem(SPOOKIE_OPENED_KEY, JSON.stringify(["2", 3, null]));
    expect([...getOpenedSpookieTickets()]).toEqual(["2"]);
  });
});

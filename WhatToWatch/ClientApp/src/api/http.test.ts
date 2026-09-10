import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { text } from "../i18n/text";
import {
  ADMIN_KEY_STORAGE,
  ApiError,
  requestJson,
  setAdminApiKey,
} from "./http";

const fetchMock = vi.fn<typeof fetch>();
const storage = new Map<string, string>();

beforeEach(() => {
  fetchMock.mockReset();
  storage.clear();
  vi.stubGlobal("fetch", fetchMock);
  vi.stubGlobal("sessionStorage", {
    getItem: (key: string) => storage.get(key) ?? null,
    setItem: (key: string, value: string) => storage.set(key, value),
    removeItem: (key: string) => storage.delete(key),
  });
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("requestJson", () => {
  it("sends the player token header without passing the custom option to fetch", async () => {
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify({ partyId: "party-1" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      })
    );

    await requestJson("/api/parties/party-1", { playerToken: "player-token" });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/parties/party-1",
      expect.objectContaining({
        headers: expect.objectContaining({
          "X-Player-Token": "player-token",
        }),
      })
    );
    expect(fetchMock.mock.calls[0]?.[1]).not.toHaveProperty("playerToken");
  });

  it("surfaces the server error code on the thrown error", async () => {
    fetchMock.mockResolvedValue(
      new Response(
        JSON.stringify({ code: "PartyFull", message: "Party already has two players." }),
        {
          status: 409,
          statusText: "Conflict",
          headers: { "Content-Type": "application/json" },
        }
      )
    );

    const error = await requestJson("/api/parties/join").catch(
      (caught: unknown) => caught
    );

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({
      message: "Party already has two players.",
      code: "PartyFull",
    });
  });

  it("keeps the localised message for network failures", async () => {
    fetchMock.mockRejectedValue(new TypeError("Failed to fetch"));

    await expect(requestJson("/api/parties/suggest-code")).rejects.toThrow(
      text("errors.apiNetwork", { base: "same-origin" })
    );
  });

  it("keeps admin authentication unchanged", async () => {
    setAdminApiKey("  admin-secret  ");
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify({ id: "list-1" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      })
    );

    await requestJson("/api/watchlists/list-1", { method: "PUT", admin: true });

    const init = fetchMock.mock.calls[0]?.[1];
    expect(storage.get(ADMIN_KEY_STORAGE)).toBe("admin-secret");
    expect(init?.headers).toEqual(
      expect.objectContaining({ "X-Admin-Key": "admin-secret" })
    );
    expect(init?.headers).not.toEqual(
      expect.objectContaining({ "X-Player-Token": expect.anything() })
    );
    expect(init).not.toHaveProperty("admin");
  });
});

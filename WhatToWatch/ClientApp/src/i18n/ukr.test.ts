/// <reference types="vite/client" />

import { expect, it } from "vitest";
import { ukr } from "./ukr";

const componentSources = import.meta.glob("../**/*.tsx", {
  eager: true,
  import: "default",
  query: "?raw",
}) as Record<string, string>;

it("has a Ukrainian entry for every literal text key used by a component", () => {
  const missingKeys = Object.entries(componentSources).flatMap(
    ([path, source]) =>
      [...source.matchAll(/\btext\(\s*["']([^"']+)["']/g)]
        .map((match) => match[1])
        .filter(
          (key) => !Object.prototype.hasOwnProperty.call(ukr, key)
        )
        .map((key) => `${path}: ${key}`)
  );

  expect(missingKeys).toEqual([]);
});

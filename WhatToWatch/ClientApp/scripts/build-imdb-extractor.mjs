import { readFile, writeFile, mkdir } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { build } from "vite";

const here = dirname(fileURLToPath(import.meta.url));
const outputPaths = [
  resolve(here, "../android/app/src/main/assets/imdb/extractWatchlist.js"),
  resolve(here, "../../../ImdbWatchlists/Scripts/extractWatchlist.js"),
];

const result = await build({
  configFile: false,
  logLevel: "silent",
  build: {
    write: false,
    minify: false,
    lib: {
      entry: resolve(here, "../src/native/imdbPageExtractorEntry.ts"),
      formats: ["iife"],
      name: "WhatToWatchImdbExtractor",
    },
  },
});

const output = Array.isArray(result) ? result[0].output : result.output;
let generated = output.find((item) => item.type === "chunk").code;

if (generated.startsWith("var WhatToWatchImdbExtractor = function()")) {
  generated = generated
    .replace(/^var WhatToWatchImdbExtractor = function\(\)\s*\{/, "(function() {")
    .replace(/\}\(\);\s*$/, "})();");
}

if (process.argv.includes("--check")) {
  let stale = false;
  for (const outputPath of outputPaths) {
    const existing = await readFile(outputPath, "utf8").catch(() => "");

    if (existing !== generated) {
      console.error(`IMDb extractor is stale: ${outputPath}`);
      stale = true;
    }
  }
  if (stale) {
    console.error("Run npm run build:imdb-extractor.");
    process.exitCode = 1;
  }
  
} else {
  for (const outputPath of outputPaths) {
    await mkdir(dirname(outputPath), { recursive: true });
    await writeFile(outputPath, generated);
  }
}

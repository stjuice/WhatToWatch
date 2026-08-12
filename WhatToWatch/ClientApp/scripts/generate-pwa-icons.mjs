import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import sharp from "sharp";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const svg = fs.readFileSync(path.join(root, "src/assets/popcorn-full.svg"), "utf8");
const match = svg.match(/data:image\/png;base64,([A-Za-z0-9+/=]+)/);
if (!match) {
  throw new Error("Embedded PNG not found in popcorn-full.svg");
}

const popcorn = Buffer.from(match[1], "base64");
const outDir = path.join(root, "public/icons");
fs.mkdirSync(outDir, { recursive: true });

async function makeIcon(size, filename) {
  const pad = Math.round(size * 0.14);
  const inner = size - pad * 2;
  const resized = await sharp(popcorn)
    .resize(inner, inner, {
      fit: "contain",
      background: { r: 0, g: 0, b: 0, alpha: 0 },
    })
    .png()
    .toBuffer();

  await sharp({
    create: {
      width: size,
      height: size,
      channels: 4,
      background: { r: 0x24, g: 0x22, b: 0x21, alpha: 1 },
    },
  })
    .composite([{ input: resized, gravity: "centre" }])
    .png()
    .toFile(path.join(outDir, filename));

  console.log(`wrote ${filename}`);
}

await makeIcon(192, "pwa-192x192.png");
await makeIcon(512, "pwa-512x512.png");
await makeIcon(180, "apple-touch-icon.png");

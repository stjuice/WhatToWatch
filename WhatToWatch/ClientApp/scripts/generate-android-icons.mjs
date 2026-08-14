// Renders src/assets/small-logo.svg into the Android launcher icons and splash
// screens. Run after changing the logo:
//   npm install --no-save sharp
//   node scripts/generate-android-icons.mjs
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import sharp from "sharp";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const res = path.join(root, "android/app/src/main/res");
const logo = fs.readFileSync(path.join(root, "src/assets/small-logo.svg"));

const BACKGROUND = { r: 0x24, g: 0x22, b: 0x21, alpha: 1 };
const TRANSPARENT = { r: 0, g: 0, b: 0, alpha: 0 };

const DENSITIES = ["mdpi", "hdpi", "xhdpi", "xxhdpi", "xxxhdpi"];
const LAUNCHER_SIZES = [48, 72, 96, 144, 192];
// Adaptive icons are 108dp canvases where only the centre 66dp is guaranteed visible.
const FOREGROUND_SIZES = [108, 162, 216, 324, 432];

// The SVG carries a wide transparent margin, so rasterise it once at a size that
// covers every output and crop to the artwork itself.
const artwork = await sharp(logo, { density: 400 })
  .trim({ background: TRANSPARENT, threshold: 0 })
  .png()
  .toBuffer();

async function renderLogo(boxWidth, boxHeight) {
  return sharp(artwork)
    .resize(boxWidth, boxHeight, { fit: "contain", background: TRANSPARENT })
    .png()
    .toBuffer();
}

async function circleMask(size) {
  const r = size / 2;
  return Buffer.from(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}"><circle cx="${r}" cy="${r}" r="${r}" fill="#fff"/></svg>`
  );
}

function canvas(width, height, background) {
  return sharp({ create: { width, height, channels: 4, background } });
}

async function write(file, pipeline) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  await pipeline.png().toFile(file);
  console.log(`wrote ${path.relative(root, file)}`);
}

async function launcherIcon(size, file, { round }) {
  const box = Math.round(size * 0.7);
  const composited = canvas(size, size, BACKGROUND).composite([
    { input: await renderLogo(box, box), gravity: "centre" },
  ]);

  if (!round) {
    await write(file, composited);
    return;
  }

  const square = await composited.png().toBuffer();
  await write(
    file,
    sharp(square).composite([{ input: await circleMask(size), blend: "dest-in" }])
  );
}

async function foregroundIcon(size, file) {
  const box = Math.round(size * 0.46);
  await write(
    file,
    canvas(size, size, TRANSPARENT).composite([
      { input: await renderLogo(box, box), gravity: "centre" },
    ])
  );
}

async function splash(file) {
  const { width, height } = await sharp(fs.readFileSync(file)).metadata();
  const box = Math.round(Math.min(width, height) * 0.3);
  await write(
    file,
    canvas(width, height, BACKGROUND).composite([
      { input: await renderLogo(box, box), gravity: "centre" },
    ])
  );
}

for (const [index, density] of DENSITIES.entries()) {
  const dir = path.join(res, `mipmap-${density}`);
  await launcherIcon(LAUNCHER_SIZES[index], path.join(dir, "ic_launcher.png"), {
    round: false,
  });
  await launcherIcon(LAUNCHER_SIZES[index], path.join(dir, "ic_launcher_round.png"), {
    round: true,
  });
  await foregroundIcon(FOREGROUND_SIZES[index], path.join(dir, "ic_launcher_foreground.png"));
}

const splashFiles = fs
  .readdirSync(res)
  .filter((dir) => dir === "drawable" || dir.startsWith("drawable-land") || dir.startsWith("drawable-port"))
  .map((dir) => path.join(res, dir, "splash.png"))
  .filter((file) => fs.existsSync(file));

for (const file of splashFiles) {
  await splash(file);
}

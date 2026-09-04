/**
 * Labels sit inside fixed-size pill frames, so the font size has to shrink
 * until the text fits instead of allowing the frame to grow with the label.
 */
export type LabelFrame = {
  widthRem: number;
  heightRem: number;
  maxFontRem: number;
  minFontRem: number;
};

export const COMPACT_LABEL_FRAME: LabelFrame = {
  widthRem: 7.5,
  heightRem: 3.3,
  maxFontRem: 1.7,
  minFontRem: 0.7,
};

export const LARGE_LABEL_FRAME: LabelFrame = {
  widthRem: 12.5,
  heightRem: 5.25,
  maxFontRem: 2.4,
  minFontRem: 1.05,
};

export const WIDE_LABEL_FRAME: LabelFrame = {
  widthRem: 12.5,
  heightRem: 4.25,
  maxFontRem: 2.2,
  minFontRem: 1,
};

/** Measured average glyph width of Lobster for Cyrillic labels, expressed in em. */
const AVG_CHAR_WIDTH_EM = 0.53;
const LINE_HEIGHT = 1.08;
/** Frame padding plus border, mirroring LabelPill.scss. */
const INSET_X_REM = 0.95;
const INSET_Y_REM = 0.6;

const clamp = (value: number, min: number, max: number): number =>
  Math.min(Math.max(value, min), max);

export const fitLabelFontRem = (label: string, frame: LabelFrame): number => {
  const words = label.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) {
    return frame.maxFontRem;
  }

  const usableWidth = Math.max(frame.widthRem - INSET_X_REM * 2, 0.1);
  const usableHeight = Math.max(frame.heightRem - INSET_Y_REM * 2, 0.1);
  const charCount = words.join(" ").length;
  const longestWord = Math.max(...words.map((word) => word.length));
  const maxLines = Math.min(
    words.length,
    Math.max(1, Math.floor(usableHeight / (LINE_HEIGHT * frame.minFontRem)))
  );

  let bestFont = 0;
  for (let lines = 1; lines <= maxLines; lines += 1) {
    const charsPerLine = Math.max(longestWord, Math.ceil(charCount / lines));
    const byWidth = usableWidth / (AVG_CHAR_WIDTH_EM * charsPerLine);
    const byHeight = usableHeight / (lines * LINE_HEIGHT);
    bestFont = Math.max(bestFont, Math.min(byWidth, byHeight));
  }

  const fitted = clamp(bestFont, frame.minFontRem, frame.maxFontRem);
  return Math.round(fitted * 100) / 100;
};

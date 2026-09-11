export type ArtworkSize = "s" | "ms" | "m" | "ml" | "l";

export const artworkSizeClass = (size: ArtworkSize): string =>
  `artwork-size--${size}`;

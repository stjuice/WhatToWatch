import popcornBucket from "../assets/popcorn-full.svg";
import { text } from "../i18n/text";
import { routePaths } from "../routes/routePaths";
import type { ArtworkSize } from "./artworkSize";
import { COMPACT_LABEL_FRAME } from "./labelPill";
import { PopcornBucket } from "./PopcornBucket";
import "./WatchlistBucket.scss";

export interface WatchlistBucketProps {
  id: string;
  name: string;
  size: ArtworkSize;
}

export const WatchlistBucket = ({ id, name, size }: WatchlistBucketProps) => {
  return (
    <PopcornBucket
      className="watchlist-bucket"
      art={popcornBucket}
      label={name}
      size={size}
      frame={COMPACT_LABEL_FRAME}
      labelCenter="58%"
      to={routePaths.list(id)}
      ariaLabel={text("watchlists.openList", { name })}
    />
  );
};

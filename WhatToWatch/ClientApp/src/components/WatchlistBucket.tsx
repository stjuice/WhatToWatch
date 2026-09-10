import popcornBucket from "../assets/popcorn-full.svg";
import { text } from "../i18n/text";
import { routePaths } from "../routes/routePaths";
import {
  COMPACT_LABEL_FRAME,
  LARGE_LABEL_FRAME,
} from "./labelPill";
import { PopcornBucket } from "./PopcornBucket";
import "./WatchlistBucket.scss";

export type WatchlistBucketSize = "compact" | "large";

export interface WatchlistBucketProps {
  id: string;
  name: string;
  size: WatchlistBucketSize;
}

export const WatchlistBucket = ({ id, name, size }: WatchlistBucketProps) => {
  const frame = size === "large" ? LARGE_LABEL_FRAME : COMPACT_LABEL_FRAME;

  return (
    <PopcornBucket
      className={`watchlist-bucket watchlist-bucket--${size}`}
      art={popcornBucket}
      label={name}
      frame={frame}
      labelCenter="58%"
      to={routePaths.list(id)}
      ariaLabel={text("watchlists.openList", { name })}
    />
  );
};

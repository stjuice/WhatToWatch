import { Link } from "react-router-dom";
import popcornBucket from "../assets/popcorn-full.svg";
import { text } from "../i18n/text";
import { routePaths } from "../routes/routePaths";
import {
  COMPACT_LABEL_FRAME,
  LARGE_LABEL_FRAME,
  fitLabelFontRem,
} from "./watchlistBucketLabel";
import "./WatchlistBucket.scss";

export type WatchlistBucketSize = "compact" | "large";

export interface WatchlistBucketProps {
  id: string;
  name: string;
  size: WatchlistBucketSize;
}

export const WatchlistBucket = ({ id, name, size }: WatchlistBucketProps) => {
  const frame = size === "large" ? LARGE_LABEL_FRAME : COMPACT_LABEL_FRAME;
  const fontRem = fitLabelFontRem(name, frame);

  return (
    <Link
      className={`watchlist-bucket watchlist-bucket--${size}`}
      to={routePaths.list(id)}
      aria-label={text("home.openList", { name })}
    >
      <img className="watchlist-bucket__art" src={popcornBucket} alt="" draggable={false} />
      <span
        className="watchlist-bucket__frame"
        style={{ width: `${frame.widthRem}rem`, height: `${frame.heightRem}rem` }}
      >
        <span className="watchlist-bucket__label" style={{ fontSize: `${fontRem}rem` }}>
          {name}
        </span>
      </span>
    </Link>
  );
};

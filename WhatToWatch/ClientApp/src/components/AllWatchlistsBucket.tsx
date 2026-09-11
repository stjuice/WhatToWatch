import popcornFull from "../assets/popcorn-full.svg";
import { text } from "../i18n/text";
import { routePaths } from "../routes/routePaths";
import { COMPACT_LABEL_FRAME } from "./labelPill";
import { PopcornBucket } from "./PopcornBucket";
import "./AllWatchlistsBucket.scss";

export const AllWatchlistsBucket = () => (
  <PopcornBucket
    className="all-watchlists-bucket"
    art={popcornFull}
    label={text("home.watchlists")}
    size="ml"
    frame={COMPACT_LABEL_FRAME}
    labelCenter="58%"
    to={routePaths.watchlists}
  />
);

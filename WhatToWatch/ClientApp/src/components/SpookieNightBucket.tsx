import popcornSpookie from "../assets/season/halloween/popcorn-spookie.svg";
import { text } from "../i18n/text";
import { routePaths } from "../routes/routePaths";
import { HOME_LABEL_FRAME } from "./labelPill";
import { PopcornBucket } from "./PopcornBucket";
import "./SpookieNightBucket.scss";

export const SpookieNightBucket = () => (
  <PopcornBucket
    className="spookie-night-bucket"
    art={popcornSpookie}
    label={text("home.spookieNight")}
    size="ml"
    frame={HOME_LABEL_FRAME}
    labelCenter="100%"
    to={routePaths.spookieNight}
  />
);

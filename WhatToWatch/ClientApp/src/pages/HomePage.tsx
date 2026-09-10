import popcornFull from "../assets/popcorn-full.svg";
import party from "../assets/tinder.svg";
import {
  LARGE_LABEL_FRAME,
  WIDE_LABEL_FRAME,
} from "../components/labelPill";
import { PopcornBucket } from "../components/PopcornBucket";
import { text } from "../i18n/text";
import { routePaths } from "../routes/routePaths";
import "./HomePage.scss";

export const HomePage = () => (
  <div className="home-page">
    <PopcornBucket
      className="home-page__bucket home-page__bucket--lists"
      art={popcornFull}
      label={text("home.watchlists")}
      frame={LARGE_LABEL_FRAME}
      labelCenter="var(--home-bucket-label-center)"
      to={routePaths.watchlists}
    />
    <PopcornBucket
      className="home-page__bucket home-page__bucket--party"
      art={party}
      label={text("home.party")}
      frame={WIDE_LABEL_FRAME}
      labelCenter="var(--home-bucket-label-center)"
      to={routePaths.party}
    />
  </div>
);

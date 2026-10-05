import { AllWatchlistsBucket } from "../components/AllWatchlistsBucket";
import { PartyBucket } from "../components/PartyBucket";
import { SpookieNightBucket } from "../components/SpookieNightBucket";
import { text } from "../i18n/text";
import { routePaths } from "../routes/routePaths";
import "./HomePage.scss";

export const HomePage = () => (
  <div className="home-page">
    <SpookieNightBucket />
    <PartyBucket
      label={text("home.party")}
      size="ml"
      to={routePaths.party}
    />
    <AllWatchlistsBucket />
  </div>
);

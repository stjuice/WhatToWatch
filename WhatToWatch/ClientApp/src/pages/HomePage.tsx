import { AllWatchlistsBucket } from "../components/AllWatchlistsBucket";
import { PartyBucket } from "../components/PartyBucket";
import { text } from "../i18n/text";
import { routePaths } from "../routes/routePaths";
import "./HomePage.scss";

export const HomePage = () => (
  <div className="home-page">
    <PartyBucket
      label={text("home.party")}
      to={routePaths.party}
    />
    <AllWatchlistsBucket />
  </div>
);

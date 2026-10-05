import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getSpookieTickets } from "../api/spookieNightApi";
import { SpookieButton } from "../components/SpookieButton";
import { text } from "../i18n/text";
import { ErrorText } from "../primitives/ErrorText";
import { StatusText } from "../primitives/StatusText";
import { routePaths } from "../routes/routePaths";
import type { SpookieTicket } from "../types/spookieNight";
import { getOpenedSpookieTickets, markSpookieTicketOpened } from "./spookieNightStorage";
import "./SpookieNightPage.scss";

export const SpookieNightPage = () => {
  const navigate = useNavigate();
  const [tickets, setTickets] = useState<SpookieTicket[]>([]);
  const [opened] = useState(getOpenedSpookieTickets);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        const loaded = await getSpookieTickets();

        if (!cancelled)
          setTickets(loaded);
      } catch (err) {
        if (!cancelled)
          setError(err instanceof Error ? err.message : text("spookie.loadFailed"));
      } finally {
        if (!cancelled)
          setLoading(false);
      }
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  const openTicket = (key: string) => {
    markSpookieTicketOpened(key);
    navigate(routePaths.spookieMovie(key));
  };

  return (
    <div className="spookie-night">
      {loading ? (
        <StatusText className="spookie-night__status">{text("spookie.loading")}</StatusText>
      ) : null}
      <ErrorText className="spookie-night__error">{error}</ErrorText>

      {!loading && !error && tickets.length === 0 ? (
        <StatusText className="spookie-night__status">{text("spookie.empty")}</StatusText>
      ) : null}

      <ul className="spookie-night__tickets">
        {tickets.map((ticket) => (
          <li key={ticket.key} className="spookie-night__item">
            <SpookieButton
              ticket={ticket}
              opened={opened.has(ticket.key)}
              onClick={() => openTicket(ticket.key)}
            />
          </li>
        ))}
      </ul>
    </div>
  );
};

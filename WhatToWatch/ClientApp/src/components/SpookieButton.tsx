import ticketArt from "../assets/season/halloween/ticket.svg";
import { text } from "../i18n/text";
import { getMovieCoreFields } from "../pages/movieCoreFields";
import { Button } from "../primitives/Button";
import type { SpookieTicket } from "../types/spookieNight";
import "./SpookieButton.scss";

export interface SpookieButtonProps {
  ticket: SpookieTicket;
  opened: boolean;
  onClick: () => void;
}

const ticketLabel = (ticket: SpookieTicket): string =>
  ticket.isBonus
    ? text("spookie.bonus")
    : text("spookie.ticketNumber", { number: ticket.key });

export const SpookieButton = ({ ticket, opened, onClick }: SpookieButtonProps) => {
  const fields = getMovieCoreFields(ticket.movie);
  const label = ticketLabel(ticket);
  const className = [
    "spookie-ticket",
    opened ? "spookie-ticket--opened" : null,
    ticket.isCurrent ? null : "spookie-ticket--past",
    ticket.isBonus ? "spookie-ticket--bonus" : null,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <Button
      variant="plain"
      className={className}
      aria-label={
        opened && fields.title
          ? fields.title
          : text("spookie.openTicketAria", { label })
      }
      onClick={onClick}
    >
      <img className="spookie-ticket__art" src={ticketArt} alt="" draggable={false} />
      <span className="spookie-ticket__stub">{label}</span>
      <span className="spookie-ticket__body">
        {opened ? (
          <>
            <span className="spookie-ticket__title">{fields.title}</span>
            {fields.genres ? (
              <span className="spookie-ticket__genres">{fields.genres}</span>
            ) : null}
          </>
        ) : (
          <span className="spookie-ticket__open">{text("spookie.open")}</span>
        )}
      </span>
    </Button>
  );
};

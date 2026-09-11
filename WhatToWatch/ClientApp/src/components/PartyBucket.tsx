import type { MouseEventHandler } from "react";
import party from "../assets/tinder.svg";
import type { ArtworkSize } from "./artworkSize";
import { COMPACT_LABEL_FRAME } from "./labelPill";
import { PopcornBucket } from "./PopcornBucket";
import "./PartyBucket.scss";

interface CommonPartyBucketProps {
  label: string;
  size: ArtworkSize;
  ariaLabel?: string;
  className?: string;
}

type PartyBucketLinkProps = CommonPartyBucketProps & {
  to: string;
  onClick?: never;
  disabled?: never;
};

type PartyBucketButtonProps = CommonPartyBucketProps & {
  to?: never;
  onClick: MouseEventHandler<HTMLButtonElement>;
  disabled?: boolean;
};

export type PartyBucketProps =
  | PartyBucketLinkProps
  | PartyBucketButtonProps;

export const PartyBucket = ({
  label,
  size,
  ariaLabel,
  className,
  ...actionProps
}: PartyBucketProps) => (
  <PopcornBucket
    {...actionProps}
    className={["party-bucket", className].filter(Boolean).join(" ")}
    art={party}
    label={label}
    size={size}
    ariaLabel={ariaLabel}
    frame={COMPACT_LABEL_FRAME}
    labelCenter="78%"
  />
);

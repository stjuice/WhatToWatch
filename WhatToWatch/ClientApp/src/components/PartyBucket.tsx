import type { MouseEventHandler } from "react";
import party from "../assets/tinder.svg";
import type { LabelFrame } from "./labelPill";
import { PopcornBucket } from "./PopcornBucket";
import "./PartyBucket.scss";

const PARTY_LABEL_FRAME: LabelFrame = {
  widthRem: 14.5,
  heightRem: 3.25,
  maxFontRem: 1.5,
  minFontRem: 0.9,
};

interface CommonPartyBucketProps {
  label: string;
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
  ariaLabel,
  className,
  ...actionProps
}: PartyBucketProps) => (
  <PopcornBucket
    {...actionProps}
    className={["party-bucket", className].filter(Boolean).join(" ")}
    art={party}
    label={label}
    ariaLabel={ariaLabel}
    frame={PARTY_LABEL_FRAME}
    labelCenter="78%"
  />
);

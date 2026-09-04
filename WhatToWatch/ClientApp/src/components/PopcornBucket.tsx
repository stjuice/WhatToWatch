import type { MouseEventHandler } from "react";
import { Button } from "../primitives/Button";
import type { LabelFrame } from "./labelPill";
import { LabelPill } from "./LabelPillView";
import "./PopcornBucket.scss";

interface CommonPopcornBucketProps {
  art: string;
  label: string;
  ariaLabel?: string;
  frame: LabelFrame;
  labelCenter: string;
  className?: string;
}

type PopcornBucketLinkProps = CommonPopcornBucketProps & {
  to: string;
  onClick?: never;
  disabled?: never;
};

type PopcornBucketButtonProps = CommonPopcornBucketProps & {
  to?: never;
  onClick: MouseEventHandler<HTMLButtonElement>;
  disabled?: boolean;
};

export type PopcornBucketProps =
  | PopcornBucketLinkProps
  | PopcornBucketButtonProps;

export const PopcornBucket = ({
  art,
  label,
  ariaLabel,
  frame,
  labelCenter,
  className,
  ...actionProps
}: PopcornBucketProps) => (
  <Button
    {...actionProps}
    variant="plain"
    className={["popcorn-bucket", className].filter(Boolean).join(" ")}
    aria-label={ariaLabel ?? label}
  >
    <img
      className="popcorn-bucket__art"
      src={art}
      alt=""
      draggable={false}
    />
    <LabelPill
      className="popcorn-bucket__label"
      label={label}
      frame={frame}
      style={{ top: labelCenter }}
    />
  </Button>
);

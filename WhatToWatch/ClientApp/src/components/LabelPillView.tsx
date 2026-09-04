import type { CSSProperties } from "react";
import type { LabelFrame } from "./labelPill";
import { fitLabelFontRem } from "./labelPill";
import "./LabelPill.scss";

export interface LabelPillProps {
  label: string;
  frame: LabelFrame;
  className?: string;
  style?: CSSProperties;
}

export const LabelPill = ({
  label,
  frame,
  className,
  style,
}: LabelPillProps) => {
  const fontRem = fitLabelFontRem(label, frame);

  return (
    <span
      className={["label-pill", className].filter(Boolean).join(" ")}
      style={{
        ...style,
        width: `${frame.widthRem}rem`,
        height: `${frame.heightRem}rem`,
      }}
    >
      <span
        className="label-pill__text"
        style={{ fontSize: `${fontRem}rem` }}
      >
        {label}
      </span>
    </span>
  );
};

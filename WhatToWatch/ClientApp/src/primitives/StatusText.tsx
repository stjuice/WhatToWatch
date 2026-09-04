import type { ReactNode } from "react";
import "./StatusText.scss";

export interface StatusTextProps {
  children?: ReactNode;
  className?: string;
}

export const StatusText = ({ children, className }: StatusTextProps) => {
  if (children === null || children === undefined || children === false || children === "")
    return null;

  const classNames = ["status-text", className].filter(Boolean).join(" ");

  return <p className={classNames}>{children}</p>;
};

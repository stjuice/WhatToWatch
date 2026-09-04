import type { ReactNode } from "react";
import "./ErrorText.scss";

export interface ErrorTextProps {
  children?: ReactNode;
  className?: string;
}

export const ErrorText = ({ children, className }: ErrorTextProps) => {
  if (children === null || children === undefined || children === false || children === "")
    return null;

  const classNames = ["error-text", className].filter(Boolean).join(" ");

  return (
    <p className={classNames} role="alert">
      {children}
    </p>
  );
};

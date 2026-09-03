import { Capacitor } from "@capacitor/core";

export const isAndroidApk = (): boolean => Capacitor.getPlatform() === "android";

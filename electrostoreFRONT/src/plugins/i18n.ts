import { createI18n } from "vue-i18n";
import messages from "@/locales";

function getBrowserLang(): string {
	const navigatorLang = navigator.language || (navigator as Navigator & { userLanguage?: string }).userLanguage || "en";
	return navigatorLang.split("-")[0];
}

function isLangAvailable(lang: string): boolean {
	return Object.keys(messages).includes(lang);
}

const browserLang = getBrowserLang();
const defaultLang = "en";
const locale = isLangAvailable(browserLang) ? browserLang : defaultLang;

export const i18n = createI18n({
	legacy: false,
	locale: locale,
	fallbackLocale: defaultLang,
	messages,
});

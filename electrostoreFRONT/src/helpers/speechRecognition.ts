// The speech recognition of the browser (Web Speech API), used before the stt of the server:
// the audio is converted while the user speaks, nothing is uploaded by the application.

export interface SpeechRecognitionAlternativeLike {
	transcript: string;
}

export interface SpeechRecognitionResultLike {
	readonly isFinal: boolean;
	readonly length: number;
	[index: number]: SpeechRecognitionAlternativeLike;
}

export interface SpeechRecognitionEventLike {
	readonly results: ArrayLike<SpeechRecognitionResultLike>;
}

export interface SpeechRecognitionErrorEventLike {
	readonly error: string;
}

export interface SpeechRecognitionLike {
	lang: string;
	continuous: boolean;
	interimResults: boolean;
	// Chrome 139+: the recognition runs in the browser (a language pack is installed), no audio leaves the device
	processLocally?: boolean;
	onresult: ((event: SpeechRecognitionEventLike) => void) | null;
	onerror: ((event: SpeechRecognitionErrorEventLike) => void) | null;
	onend: (() => void) | null;
	start(): void;
	stop(): void;
	abort(): void;
}

export interface SpeechRecognitionStatic {
	new (): SpeechRecognitionLike;
	available?: (options: { langs: string[]; processLocally: boolean }) => Promise<string>;
	install?: (options: { langs: string[]; processLocally: boolean }) => Promise<boolean>;
}

// "unknown": the browser cannot tell (no on-device recognition API)
export type LocalSpeechStatus = "available" | "downloadable" | "downloading" | "unavailable" | "unknown";

export function getSpeechRecognition(): SpeechRecognitionStatic | null {
	if (typeof window === "undefined") {
		return null;
	}
	const scope = window as unknown as { SpeechRecognition?: SpeechRecognitionStatic; webkitSpeechRecognition?: SpeechRecognitionStatic };
	return scope.SpeechRecognition ?? scope.webkitSpeechRecognition ?? null;
}

// Whether the on-device recognition of a language is ready, can be downloaded or is not offered by the browser
export async function localSpeechStatus(lang: string, recognition: SpeechRecognitionStatic | null = getSpeechRecognition()): Promise<LocalSpeechStatus> {
	if (!recognition || typeof recognition.available !== "function") {
		return "unknown";
	}
	try {
		const status = await recognition.available({ langs: [lang], processLocally: true });
		return status === "available" || status === "downloadable" || status === "downloading" || status === "unavailable" ? status : "unknown";
	} catch {
		return "unknown";
	}
}

// Download the language pack of the on-device recognition
export async function installLocalSpeech(lang: string, recognition: SpeechRecognitionStatic | null = getSpeechRecognition()): Promise<boolean> {
	if (!recognition || typeof recognition.install !== "function") {
		return false;
	}
	try {
		return await recognition.install({ langs: [lang], processLocally: true });
	} catch {
		return false;
	}
}

// BCP 47 tag asked to the recognition: the region of the browser when it speaks the language of the application
export function recognitionLang(locale: string, navigatorLang = typeof navigator === "undefined" ? "" : navigator.language): string {
	const language = locale.split("-")[0].toLowerCase();
	if (navigatorLang && navigatorLang.split("-")[0].toLowerCase() === language) {
		return navigatorLang;
	}
	const defaults: Record<string, string> = { fr: "fr-FR", en: "en-US" };
	return defaults[language] ?? locale;
}

// What has been understood since the start of the recognition: the final words and the ones still being guessed
export function transcriptOf(results: ArrayLike<SpeechRecognitionResultLike>): { final: string, interim: string } {
	let final = "";
	let interim = "";
	for (let index = 0; index < results.length; index++) {
		const result = results[index];
		const text = result[0]?.transcript ?? "";
		if (result.isFinal) {
			final += text;
		} else {
			interim += text;
		}
	}
	return { final, interim };
}

// The spoken words are added to what the user already typed
export function joinSpeechText(base: string, spoken: string): string {
	const text = spoken.trim();
	if (text === "") {
		return base;
	}
	const start = base.trimEnd();
	return start === "" ? text : `${start} ${text}`;
}

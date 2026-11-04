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

export type LocalSpeechStatus = "available" | "downloadable" | "downloading" | "unavailable" | "unknown";

export function getSpeechRecognition(): SpeechRecognitionStatic | null {
	if (typeof window === "undefined") {
		return null;
	}
	const scope = window as unknown as { SpeechRecognition?: SpeechRecognitionStatic; webkitSpeechRecognition?: SpeechRecognitionStatic };
	return scope.SpeechRecognition ?? scope.webkitSpeechRecognition ?? null;
}

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

export async function hasAudioInput(): Promise<boolean> {
	if (typeof navigator === "undefined" || typeof navigator.mediaDevices?.enumerateDevices !== "function") {
		return true;
	}
	try {
		const devices = await navigator.mediaDevices.enumerateDevices();
		return devices.some((device) => device.kind === "audioinput");
	} catch {
		return true;
	}
}

export function micErrorKey(error: string): string {
	switch (error) {
	case "NotFoundError":
	case "OverconstrainedError":
	case "DevicesNotFoundError":
	case "audio-capture":
		return "common.VAiMicMissing";
	case "NotReadableError":
	case "TrackStartError":
		return "common.VAiMicBusy";
	case "NotAllowedError":
	case "SecurityError":
	case "PermissionDeniedError":
	case "not-allowed":
		return "common.VAiMicDenied";
	default:
		return "common.VAiMicError";
	}
}

export function recognitionLang(locale: string, navigatorLang = typeof navigator === "undefined" ? "" : navigator.language): string {
	const language = locale.split("-")[0].toLowerCase();
	if (navigatorLang?.split("-")[0].toLowerCase() === language) {
		return navigatorLang;
	}
	const defaults: Record<string, string> = { fr: "fr-FR", en: "en-US" };
	return defaults[language] ?? locale;
}

export function transcriptOf(results: ArrayLike<SpeechRecognitionResultLike>): { final: string, interim: string } {
	let final = "";
	let interim = "";
	for (const result of Array.from(results)) {
		const text = result[0]?.transcript ?? "";
		if (result.isFinal) {
			final += text;
		} else {
			interim += text;
		}
	}
	return { final, interim };
}

export function joinSpeechText(base: string, spoken: string): string {
	const text = spoken.trim();
	if (text === "") {
		return base;
	}
	const start = base.trimEnd();
	return start === "" ? text : `${start} ${text}`;
}

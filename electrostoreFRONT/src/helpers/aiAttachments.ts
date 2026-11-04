// Files handed to the assistant: images (read by a model able to see), text files (read into the prompt)
// and audio files (transcribed by the stt of the server). The limits are the ones of the api (config store).

export type AiFileKind = "image" | "text" | "audio";

export interface AiFileLimits {
	maxCount: number;
	imageMimeTypes: string[];
	imageExtensions: string[];
	maxImageSizeMb: number;
	textExtensions: string[];
	maxTextSizeMb: number;
	audio: { mimeTypes: string[], extensions: string[], maxSizeMb: number } | null;
}

export type AiFileCheck =
	| { kind: AiFileKind }
	| { error: "type" }
	| { error: "empty" }
	| { error: "tooBig", maxSizeMb: number };

export interface AiFileLike {
	name: string;
	type: string;
	size: number;
}

export function fileExtension(name: string): string {
	return name.includes(".") ? "." + name.split(".").pop()!.toLowerCase() : "";
}

export function checkAiFile(file: AiFileLike, limits: AiFileLimits): AiFileCheck {
	const extension = fileExtension(file.name);
	const mime = file.type.split(";")[0].toLowerCase();
	let kind: AiFileKind | null = null;
	let maxSizeMb = 0;
	if (limits.textExtensions.includes(extension)) {
		kind = "text";
		maxSizeMb = limits.maxTextSizeMb;
	} else if (limits.imageMimeTypes.includes(mime) && limits.imageExtensions.includes(extension)) {
		kind = "image";
		maxSizeMb = limits.maxImageSizeMb;
	} else if (limits.audio && (limits.audio.mimeTypes.includes(mime) || limits.audio.extensions.includes(extension))) {
		kind = "audio";
		maxSizeMb = limits.audio.maxSizeMb;
	}
	if (kind === null) {
		return { error: "type" };
	}
	if (file.size <= 0) {
		return { error: "empty" };
	}
	if (file.size > maxSizeMb * 1024 * 1024) {
		return { error: "tooBig", maxSizeMb };
	}
	return { kind };
}

export function formatFileSize(bytes: number): string {
	if (bytes < 1024) {
		return `${bytes} B`;
	}
	return bytes < 1024 * 1024 ? `${(bytes / 1024).toFixed(0)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

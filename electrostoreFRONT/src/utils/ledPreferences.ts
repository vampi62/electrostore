export type LedShowParams = { color: string; timeshow: number; animation: number };
export type LedPreferences = { add: LedShowParams; remove: LedShowParams };
export type LedOperation = keyof LedPreferences;

export const LED_ANIMATIONS = [1, 2, 3, 4, 5];

const defaultPreferences = (): LedPreferences => ({
	add: { color: "#00ff00", timeshow: 30, animation: 2 },
	remove: { color: "#ff0000", timeshow: 30, animation: 4 },
});

// preferences are stored per user in the localStorage of the browser
const storageKey = (idUser: number | string | undefined) => `ledPreferences_${idUser ?? "anonymous"}`;

export function loadLedPreferences(idUser: number | string | undefined): LedPreferences {
	const defaults = defaultPreferences();
	try {
		const saved = JSON.parse(localStorage.getItem(storageKey(idUser)) ?? "{}") ?? {};
		return { add: { ...defaults.add, ...saved.add }, remove: { ...defaults.remove, ...saved.remove } };
	} catch {
		return defaults;
	}
}

export function saveLedPreferences(idUser: number | string | undefined, preferences: LedPreferences) {
	localStorage.setItem(storageKey(idUser), JSON.stringify(preferences));
}

// convert the stored preference to the query params expected by the "show led" endpoints
export function toShowQuery(params: LedShowParams) {
	const hex = params.color.replace("#", "");
	return {
		red: Number.parseInt(hex.substring(0, 2), 16),
		green: Number.parseInt(hex.substring(2, 4), 16),
		blue: Number.parseInt(hex.substring(4, 6), 16),
		timeshow: params.timeshow,
		animation: params.animation,
	};
}

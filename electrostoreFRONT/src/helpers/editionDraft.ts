import { isNewId } from "@/utils";
import type { StoreGeneric } from "pinia";

// Unsaved changes of a view and their draft (serializable copy).
// Convention of the stores: the edition of an element is store[mainKey][id], the changes staged for its nested resources
// (pending creations / modifications / deletions) are in every store[*Ready][id].

export interface EditionDraft {
	edition: Record<string, any>;
	ready: Record<string, any>;
	// number of staged files that cannot be kept in a draft (File objects)
	lost: number;
}

// fields of an edition that are only used by the UI
const IGNORED_FIELDS = new Set(["loading", "isFormData", "pushChange"]);

// never written in the localStorage
const SENSITIVE_FIELDS = /password/i;

const isEmpty = (value: unknown) => value === undefined || value === null || value === "";

function sameValue(a: unknown, b: unknown) {
	if (isEmpty(a) && isEmpty(b)) {
		return true;
	}
	// the API may return a number where the form holds a string
	if (typeof a === "object" || typeof b === "object") {
		return JSON.stringify(a) === JSON.stringify(b);
	}
	return `${a}` === `${b}`;
}

const readyKeys = (store: StoreGeneric) => Object.keys(store.$state).filter((key) => key.endsWith("Ready"));

export function isEditionDirty(store: StoreGeneric, id: string, mainKey: string, sourceKey: string): boolean {
	if (!id) {
		return false;
	}
	if (readyKeys(store).some((key) => Object.keys(store.$state[key]?.[id] ?? {}).length > 0)) {
		return true;
	}
	const edition = store.$state[mainKey]?.[id];
	if (!edition) {
		return false;
	}
	// an element that is not loaded yet has nothing to compare with
	const source = isNewId(id) ? {} : store.$state[sourceKey]?.[id];
	if (!source) {
		return false;
	}
	return Object.entries(edition).some(([key, value]) => {
		if (IGNORED_FIELDS.has(key)) {
			return false;
		}
		if (value instanceof Blob) {
			return true;
		}
		if (key.startsWith("unset_")) {
			return false;
		}
		return !sameValue(value, source[key]);
	});
}

export function snapshotEdition(store: StoreGeneric, id: string, mainKey: string): EditionDraft {
	let lost = 0;
	const edition: Record<string, unknown> = {};
	for (const [key, value] of Object.entries(store.$state[mainKey]?.[id] ?? {})) {
		if (IGNORED_FIELDS.has(key) || SENSITIVE_FIELDS.test(key)) {
			continue;
		}
		if (value instanceof Blob) {
			lost++;
		} else {
			edition[key] = value;
		}
	}
	const ready: Record<string, any> = {};
	for (const key of readyKeys(store)) {
		const entries: Record<string, any> = {};
		for (const [entryId, entry] of Object.entries(store.$state[key]?.[id] ?? {})) {
			// an entry holding a file would be pushed without it
			if (Object.values(entry as object).some((value) => value instanceof Blob)) {
				lost++;
			} else {
				entries[entryId] = entry;
			}
		}
		if (Object.keys(entries).length) {
			ready[key] = entries;
		}
	}
	return JSON.parse(JSON.stringify({ edition, ready, lost }));
}

export function applyDraft(store: StoreGeneric, id: string, mainKey: string, draft: EditionDraft) {
	Object.assign(store.$state[mainKey][id], draft.edition, { loading: false });
	for (const [key, entries] of Object.entries(draft.ready)) {
		if (store.$state[key] !== undefined) {
			store.$state[key][id] = entries;
		}
	}
}

import type { StoreGeneric } from "pinia";

// Unsaved changes of a view and their draft (serializable copy).
// Convention of the stores: the edition of an element is store[mainKey][id], the changes staged for its nested resources
// (pending creations / modifications / deletions) are in every store[*Ready][id], or, when the nested resources have their own store, in the related ones.

export interface EditionDraft {
	edition: Record<string, unknown>;
	ready: Record<string, Record<string, unknown>>;
	// number of staged files that cannot be kept in a draft (File objects)
	lost: number;
}

// Changes staged in the store of a nested resource (see createResource), entries are indexed by the rest of their composite key
export interface RelatedStaged {
	// name of the entries in the draft
	key: string;
	get: (id: string) => Record<string, unknown>;
	set: (id: string, entries: Record<string, object>) => void;
}

// fields of an edition that are only used by the UI
const IGNORED_FIELDS = new Set(["loading", "isFormData", "pushChange"]);

// never written in the localStorage
const SENSITIVE_FIELDS = /password/i;

const isEmpty = (value: unknown) => value === undefined || value === null || value === "";

// Comparable form of a value: empty values are dropped, primitives are compared as strings (the API may return a number where the form holds a string),
// the keys of the objects are sorted and a file is described by its metadata
function normalize(value: unknown): unknown {
	if (isEmpty(value)) {
		return undefined;
	}
	if (value instanceof Blob) {
		const file = value as File;
		return `blob:${file.name ?? ""}:${value.size}:${file.lastModified ?? 0}`;
	}
	if (Array.isArray(value)) {
		return value.map((item) => normalize(item) ?? null);
	}
	if (typeof value === "object") {
		const result: Record<string, unknown> = {};
		for (const key of Object.keys(value as object).sort()) {
			const normalized = normalize((value as Record<string, unknown>)[key]);
			if (normalized !== undefined) {
				result[key] = normalized;
			}
		}
		return result;
	}
	return String(value);
}

const readyKeys = (store: StoreGeneric) => Object.keys(store.$state).filter((key) => key.endsWith("Ready"));

// State of the edition of an element (edition + staged changes), as a string so two states can be compared.
// The baseline of an element is its state right after loadToEdition (default values and preset included): only what differs from it is a change of the user.
export function fingerprintEdition(store: StoreGeneric, id: string, mainKey: string, related: RelatedStaged[] = []): string {
	const edition: Record<string, unknown> = {};
	for (const [key, value] of Object.entries(store.$state[mainKey]?.[id] ?? {})) {
		if (!IGNORED_FIELDS.has(key)) {
			edition[key] = value;
		}
	}
	const ready: Record<string, unknown> = {};
	for (const key of readyKeys(store)) {
		ready[key] = store.$state[key]?.[id] ?? {};
	}
	for (const staged of related) {
		ready[staged.key] = staged.get(id);
	}
	return JSON.stringify(normalize({ edition, ready }) ?? {});
}

export function snapshotEdition(store: StoreGeneric, id: string, mainKey: string, related: RelatedStaged[] = []): EditionDraft {
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
	const ready: Record<string, Record<string, unknown>> = {};
	const staged = [
		...readyKeys(store).map((key) => ({ key, entries: store.$state[key]?.[id] ?? {} })),
		...related.map((relatedStaged) => ({ key: relatedStaged.key, entries: relatedStaged.get(id) })),
	];
	for (const { key, entries: stagedEntries } of staged) {
		const entries: Record<string, unknown> = {};
		for (const [entryId, entry] of Object.entries(stagedEntries)) {
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

export function applyDraft(store: StoreGeneric, id: string, mainKey: string, draft: EditionDraft, related: RelatedStaged[] = []) {
	Object.assign(store.$state[mainKey][id], draft.edition, { loading: false });
	for (const [key, entries] of Object.entries(draft.ready)) {
		const relatedStaged = related.find((candidate) => candidate.key === key);
		if (relatedStaged) {
			relatedStaged.set(id, entries as Record<string, object>);
		} else if (store.$state[key] !== undefined) {
			store.$state[key][id] = entries;
		}
	}
}

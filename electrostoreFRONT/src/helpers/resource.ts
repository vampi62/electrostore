import { buildFormData, fetchWrapper, buildQuery } from "@/helpers";

import type { RSQLFilter, RSQLSort } from "@/types/rsql";
import type { Body, EntityTypes, IdKey, KeyParts, ResourceOptions, ResourceRoute, Ready } from "@/types/resource";
import type { StoreGeneric } from "pinia";
import type { FetchPage, PaginatedResponseDto, ReadBulkDto } from "@/types/fetch";

const baseUrl = `${import.meta.env.VITE_API_URL}`;
const KEY_SEPARATOR = ":";
const NEW_ID_PREFIX = "new-";

function isSaved(parts: Record<string, string | number>): boolean {
	return !Object.values(parts).some((value) => String(value).startsWith(NEW_ID_PREFIX));
}

export function createResource<T extends EntityTypes>({ compositeKey, generatedField, routeOnlyFields = [], path, entityPath, countKey, stateKey, loadingKey, editionKey, readyKey, onHydrate }: ResourceOptions<T>) {
	type ReadExtended = "readExtended" extends keyof T ? T["readExtended"] : T["readBasic"];
	type Read = "readBasic" extends keyof T ? T["readBasic"] : T["readExtended"];
	type Parts = KeyParts<T>;
	type Records = Record<string, unknown>;

	function toKey(parts: Parts): string {
		return compositeKey.map((field) => {
			const value = parts[field];
			if (value === undefined || value === null) {
				throw new Error(`Missing part "${field}" of the key`);
			}
			return String(value);
		}).join(KEY_SEPARATOR);
	}

	function toParts(key: string): Record<string, string> {
		const values = key.split(KEY_SEPARATOR);
		return Object.fromEntries(compositeKey.map((field, index) => [field, values[index]]));
	}

	function toScopeKey(scope: Parts): string {
		const fields = [...compositeKey, ...(Object.keys(scope) as Array<IdKey<T>>).filter((field) => !compositeKey.includes(field) && !routeOnlyFields.includes(field))];
		return fields
			.filter((field) => scope[field] !== undefined && scope[field] !== null)
			.map((field) => `${field}=${scope[field]}`)
			.join("&");
	}

	function matches(key: string, part: Parts): boolean {
		const parts = toParts(key);
		return (Object.entries(part) as Array<[string, string | undefined]>).every(([field, value]) => value === undefined || value === null || parts[field] === String(value));
	}

	// Keeps the records whose key matches every given part
	function pick<R>(records: Record<string, R> | undefined, part: Parts): Record<string, R> {
		return Object.fromEntries(Object.entries(records ?? {}).filter(([key]) => matches(key, part)));
	}

	// Keys of the records in a scope: the fields of the key are read in the key, the others (e.g. id_user) in the record
	function inScope(records: Record<string, unknown> | undefined, scope: Parts): string[] {
		return Object.entries(records ?? {}).filter(([key, record]) => {
			const parts = toParts(key);
			return (Object.entries(scope) as Array<[string, string | undefined]>).every(([field, value]) => {
				if (value === undefined || value === null || routeOnlyFields.includes(field)) {
					return true;
				}
				const current = compositeKey.includes(field as IdKey<T>) ? parts[field] : (record as Record<string, unknown> | undefined)?.[field];
				// a field the record does not carry (e.g. id_store of a box link) cannot exclude it
				return current === undefined || String(current) === String(value);
			});
		}).map(([key]) => key);
	}

	// Same as slice, but reads and writes go to the records: what is set or deleted on the result is set or deleted in them
	function live<R>(records: Record<string, R>, part: Parts): Record<string, R> {
		const rest = compositeKey.filter((field) => part[field] === undefined || part[field] === null);
		const toFull = (restKey: string) => {
			const values = restKey.split(KEY_SEPARATOR);
			return toKey({ ...part, ...Object.fromEntries(rest.map((field, index) => [field, values[index]])) } as Parts);
		};
		return new Proxy({} as Record<string, R>, {
			get: (_, name) => (typeof name === "string" ? records[toFull(name)] : undefined),
			set: (_, name, value: R) => {
				if (typeof name === "string") {
					records[toFull(name)] = value;
				}
				return true;
			},
			deleteProperty: (_, name) => {
				if (typeof name === "string") {
					delete records[toFull(name)];
				}
				return true;
			},
			has: (_, name) => typeof name === "string" && toFull(name) in records,
			ownKeys: () => Object.keys(slice(records, part)),
			getOwnPropertyDescriptor: (_, name) => (typeof name === "string" && toFull(name) in records ? { enumerable: true, configurable: true, value: records[toFull(name)] } : undefined),
		});
	}

	// Same as pick, the keys keep only the fields the part does not give: { id_item: 3 } turns "3:12" into "12"
	function slice<R>(records: Record<string, R> | undefined, part: Parts): Record<string, R> {
		const rest = compositeKey.filter((field) => part[field] === undefined || part[field] === null);
		return Object.fromEntries(Object.entries(pick(records, part)).map(([key, value]) => {
			const parts = toParts(key);
			return [rest.map((field) => parts[field]).join(KEY_SEPARATOR), value];
		}));
	}

	// { "3:12": a, "3:13": b, "4:12": c } by id_item => { "3": { "12": a, "13": b }, "4": { "12": c } }
	function group<R>(records: Record<string, R> | undefined, field: IdKey<T>): Record<string, Record<string, R>> {
		const rest = compositeKey.filter((candidate) => candidate !== field);
		const grouped: Record<string, Record<string, R>> = {};
		for (const [key, value] of Object.entries(records ?? {})) {
			const parts = toParts(key);
			grouped[parts[field]] ??= {};
			grouped[parts[field]][rest.map((candidate) => parts[candidate]).join(KEY_SEPARATOR)] = value;
		}
		return grouped;
	}

	function keyFields(parts: Parts): Parts {
		return Object.fromEntries(compositeKey.map((field) => [field, parts[field]])) as Parts;
	}

	// The only key field a scope does not give (the one the bulk ids refer to)
	function missingField(scope: Parts): IdKey<T> {
		const missing = compositeKey.filter((field) => scope[field] === undefined || scope[field] === null);
		if (missing.length !== 1) {
			throw new Error("A bulk scope must give every key part but one");
		}
		return missing[0];
	}

	function resolveUrl(routes: Array<ResourceRoute<T>>, parts: Parts): string {
		const route = routes.find((candidate) => candidate.keys.every((field) => parts[field] !== undefined && parts[field] !== null));
		if (!route) {
			throw new Error(`No route matches the key parts "${Object.keys(parts).join(", ")}"`);
		}
		// a temporary id never reaches the api
		if (!isSaved(parts as Record<string, string | number>)) {
			throw new Error(`The key parts ${JSON.stringify(Object.values(parts))} hold an id that is not saved yet`);
		}
		return route.url(parts);
	}

	const collectionUrl = (scope: Parts) => resolveUrl(path, scope);
	const entityUrl = (key: Parts) => resolveUrl(entityPath, key);

	// The generated field of a record about to be created is a temporary id until the api answers
	function isKeySaved(parts: Record<string, string | number>, status?: string): boolean {
		const checked = status === "created" && generatedField ? Object.fromEntries(Object.entries(parts).filter(([field]) => field !== generatedField)) : parts;
		return isSaved(checked);
	}

	// A resource without scope (e.g. /item) keeps its count as a number, the others one per scope
	function setCount(store: StoreGeneric, scope: Parts, total: number) {
		const scopeKey = toScopeKey(scope);
		if (scopeKey === "") {
			store[countKey] = total;
			return;
		}
		store[countKey] ??= {};
		store[countKey][scopeKey] = total;
	}

	function ensureCount(store: StoreGeneric, scope: Parts) {
		const scopeKey = toScopeKey(scope);
		if (scopeKey === "") {
			store[countKey] ??= 0;
			return;
		}
		store[countKey] ??= {};
		store[countKey][scopeKey] ??= 0;
	}

	function bumpCounts(store: StoreGeneric, parts: Parts, delta: number) {
		if (typeof store[countKey] === "number") {
			store[countKey] = Math.max(store[countKey] + delta, 0);
			return;
		}
		for (const scope of Object.keys(store[countKey] ?? {})) {
			const inScope = scope.split("&").every((pair) => {
				const [field, value] = pair.split("=");
				return String(parts[field as IdKey<T>]) === value;
			});
			if (inScope) {
				store[countKey][scope] = Math.max((store[countKey][scope] ?? 0) + delta, 0);
			}
		}
	}

	const resource = {
		// pure helpers, usable in getters
		toKey,
		toParts,
		toScopeKey,
		pick,
		slice,
		live,
		group,
		async getByInterval(this: StoreGeneric, scope: Parts, limit = 100, offset = 0, expand: string[] = [], filter: RSQLFilter[] = [], sort: RSQLSort = {}, clear = false): Promise<FetchPage> {
			this[stateKey] ??= {};
			if (clear) {
				for (const key of inScope(this[stateKey], scope)) {
					delete this[stateKey][key];
				}
			}
			this[loadingKey] = true;
			try {
				const query = buildQuery({ offset, limit, expand, filter, sort });
				const res = await fetchWrapper.get<PaginatedResponseDto<ReadExtended>>({ url: `${baseUrl}${collectionUrl(scope)}?${query}`, useToken: "access" });
				for (const entity of res.data) {
					this[stateKey][toKey(entity as Parts)] = entity;
					onHydrate?.(this, entity, expand);
				}
				setCount(this, scope, res.pagination?.total ?? 0);
				return [res.pagination?.next_offset ?? 0, res.pagination?.has_more ?? false];
			} finally {
				this[loadingKey] = false;
			}
		},
		async getById(this: StoreGeneric, key: Parts, expand: string[] = []) {
			this[stateKey] ??= {};
			const stateId = toKey(key);
			try {
				const query = buildQuery({ expand });
				const data = await fetchWrapper.get<ReadExtended>({ url: `${baseUrl}${entityUrl(key)}?${query}`, useToken: "access" });
				this[stateKey][stateId] = data;
				onHydrate?.(this, data, expand);
			} finally {
				if (this[stateKey][stateId]) {
					this[stateKey][stateId].loading = false;
				}
			}
		},
		async create(this: StoreGeneric, scope: Parts, params: Body<T, "create">, isFormData = false): Promise<string> {
			this[stateKey] ??= {};
			// if isFormData, params is converted to FormData and sent as multipart/form-data
			const body = isFormData ? buildFormData(params as Record<string, string | Blob>) : params;
			const data = await fetchWrapper.post<Read>({ url: `${baseUrl}${collectionUrl(scope)}`, useToken: "access", body, contentFile: isFormData });
			const key = toKey(data as Parts);
			this[stateKey][key] = data;
			ensureCount(this, scope);
			bumpCounts(this, data as Parts, 1);
			return key;
		},
		async update(this: StoreGeneric, key: Parts, params: Body<T, "update">, isFormData = false) {
			this[stateKey] ??= {};
			const body = isFormData ? buildFormData(params as Record<string, string | Blob>) : params;
			this[stateKey][toKey(key)] = await fetchWrapper.put<Read>({ url: `${baseUrl}${entityUrl(key)}`, useToken: "access", body, contentFile: isFormData });
		},
		async remove(this: StoreGeneric, key: Parts) {
			await fetchWrapper.delete({ url: `${baseUrl}${entityUrl(key)}`, useToken: "access" });
			const record = this[stateKey]?.[toKey(key)];
			delete this[stateKey]?.[toKey(key)];
			bumpCounts(this, { ...record, ...key }, -1);
		},
		async createBulk(this: StoreGeneric, scope: Parts, params: Body<T, "createBulk">): Promise<ReadBulkDto<Read>> {
			this[stateKey] ??= {};
			const res = await fetchWrapper.post<ReadBulkDto<Read>>({ url: `${baseUrl}${collectionUrl(scope)}/bulk`, useToken: "access", body: params });
			ensureCount(this, scope);
			for (const entity of res.valide) {
				this[stateKey][toKey(entity as Parts)] = entity;
				bumpCounts(this, entity as Parts, 1);
			}
			return res;
		},
		async updateBulk(this: StoreGeneric, scope: Parts, params: Body<T, "updateBulk">) {
			this[stateKey] ??= {};
			const res = await fetchWrapper.put<ReadBulkDto<Read>>({ url: `${baseUrl}${collectionUrl(scope)}/bulk`, useToken: "access", body: params });
			for (const entity of res.valide) {
				this[stateKey][toKey(entity as Parts)] = entity;
			}
		},
		// ids are the values of the one key field the scope does not give
		async removeBulk(this: StoreGeneric, scope: Parts, ids: Body<T, "deleteBulk">) {
			const field = missingField(scope);
			const res = await fetchWrapper.delete<ReadBulkDto<Read>>({ url: `${baseUrl}${collectionUrl(scope)}/bulk`, useToken: "access", body: ids });
			for (const id of res.valide as unknown as Array<string | number>) {
				const key = { ...scope, [field]: id } as Parts;
				delete this[stateKey]?.[toKey(key)];
				bumpCounts(this, key, -1);
			}
		},
		// Replaces the records of a scope with the ones an expanded parent brought along; entities left undefined only update the total
		hydrate(this: StoreGeneric, scope: Parts, entities: unknown[] | null | undefined, total?: number) {
			this[stateKey] ??= {};
			if (entities) {
				for (const key of inScope(this[stateKey], scope)) {
					delete this[stateKey][key];
				}
				for (const entity of entities) {
					if (compositeKey.every((field) => (entity as Parts)[field] !== undefined && (entity as Parts)[field] !== null)) {
						this[stateKey][toKey(entity as Parts)] = entity;
					}
				}
			}
			if (total !== undefined) {
				setCount(this, scope, total);
			}
		},
		// Sets records without touching the others, e.g. the box brought along by a link
		upsert(this: StoreGeneric, entities: unknown[]) {
			this[stateKey] ??= {};
			for (const entity of entities) {
				if (compositeKey.every((field) => (entity as Parts)[field] !== undefined && (entity as Parts)[field] !== null)) {
					this[stateKey][toKey(entity as Parts)] = entity;
				}
			}
		},
		getAvailableNewId(this: StoreGeneric, scope: Parts = {}): string {
			if (!editionKey || !generatedField) {
				throw new Error("Edition key and generated field are required to reserve a new id");
			}
			this[editionKey] ??= {};
			let i = 1;
			while (Object.hasOwn(this[editionKey], toKey({ ...scope, [generatedField]: NEW_ID_PREFIX + i } as Parts))) {
				i++;
			}
			const id = NEW_ID_PREFIX + i;
			this[editionKey][toKey({ ...scope, [generatedField]: id } as Parts)] = {};
			return id;
		},
		// preset: "field:value;field:value"
		loadEditionPreset(this: StoreGeneric, key: Parts, preset: string | null = null) {
			if (!editionKey || !preset) {
				return;
			}
			this[editionKey] ??= {};
			const stateId = toKey(key);
			this[editionKey][stateId] ??= {};
			for (const pair of preset.split(";")) {
				const [field, value] = pair.split(":");
				if (field && value) {
					this[editionKey][stateId][field] = value;
				}
			}
		},
		setEdition(this: StoreGeneric, key: Parts, data: object) {
			if (!editionKey) {
				return;
			}
			this[editionKey] ??= {};
			this[editionKey][toKey(key)] = data;
		},
		removeEdition(this: StoreGeneric, key: Parts) {
			if (editionKey) {
				delete this[editionKey]?.[toKey(key)];
			}
		},
		removeReady(this: StoreGeneric, key: Parts) {
			if (readyKey) {
				delete this[readyKey]?.[toKey(key)];
			}
		},
		// Replaces the edition / ready entries of a scope, entries are keyed like the result of slice
		setStagedSlice(this: StoreGeneric, record: "edition" | "ready", scope: Parts, entries: Record<string, object>) {
			const target = record === "edition" ? editionKey : readyKey;
			if (!target) {
				return;
			}
			this[target] ??= {};
			for (const key of Object.keys(pick(this[target] as Records, scope))) {
				delete this[target][key];
			}
			const rest = compositeKey.filter((field) => scope[field] === undefined || scope[field] === null);
			for (const [entryKey, entry] of Object.entries(entries)) {
				const values = entryKey.split(KEY_SEPARATOR);
				const parts = { ...scope, ...Object.fromEntries(rest.map((field, index) => [field, values[index]])) } as Parts;
				this[target][toKey(parts)] = { ...entry, ...keyFields(parts) };
			}
		},
		valideEditionByKey(this: StoreGeneric, key: Parts, status = "modified", isFormData = false) {
			if (!readyKey || !editionKey) {
				return;
			}
			this[readyKey] ??= {};
			const stateId = toKey(key);
			const edition = this[editionKey]?.[stateId] ?? {};
			// compare this[stateKey]?.[stateId] with the edition to determine if changes exist
			if (JSON.stringify(this[stateKey]?.[stateId] ?? {}) === JSON.stringify(edition) && status !== "deleted") {
				delete this[readyKey][stateId];
				return;
			}
			// check if this[readyKey][stateId] already exists with status "created" and if the new changes has the status "deleted"
			if (this[readyKey][stateId]?.status === "created" && status === "deleted") {
				delete this[readyKey][stateId];
				return;
			}
			this[readyKey][stateId] = { ...edition, ...keyFields(key), status, isFormData };
		},
		copyByKeyPart(this: StoreGeneric, field: IdKey<T>, oldValue: string | number, newValue: string | number) {
			if (!readyKey || !editionKey) {
				return;
			}
			for (const recordKey of [editionKey, readyKey]) {
				this[recordKey] ??= {};
				for (const [key, entry] of Object.entries(pick(this[recordKey] as Records, { [field]: oldValue } as Parts))) {
					const newKey = toKey({ ...toParts(key), [field]: newValue } as Parts);
					this[recordKey][newKey] = { ...(entry as object), [field]: newValue };
				}
			}
		},
		clearEdition(this: StoreGeneric, part: Parts) {
			for (const recordKey of [editionKey, readyKey]) {
				if (!recordKey) {
					continue;
				}
				for (const key of Object.keys(pick(this[recordKey] as Records, part))) {
					delete this[recordKey][key];
				}
			}
		},
		async pushChange(this: StoreGeneric, scope: Parts) {
			if (!readyKey) {
				return;
			}
			const readyEntries = pick(this[readyKey] as Record<string, Ready<{ create: T["create"], update: T["update"] }>>, scope);
			if (!isSaved(scope as Record<string, string | number>)) {
				return;
			}
			// Skip the ones already pushed, and the ones whose key still holds a temporary id: they stay in ready for a later push
			const pendingEntries = Object.entries(readyEntries).filter(([stateId, entry]) => !entry.pushChange && isKeySaved(toParts(stateId), entry.status));
			let pendingChanges = Promise.resolve();
			for (const [stateId, entry] of pendingEntries) {
				const { status, isFormData, ...data } = entry;
				const key = toParts(stateId) as Parts;
				pendingChanges = pendingChanges.then(async() => {
					if (status === "created") {
						// the scope is carried by the url
						const payload = data as Body<T, "create">;
						await resource.create.call(this, scope, payload, isFormData);
					} else if (status === "modified") {
						const payload = data as Body<T, "update">;
						await resource.update.call(this, key, payload, isFormData);
					} else if (status === "deleted") {
						await resource.remove.call(this, key);
					}
					entry.pushChange = true;
				});
			}
			await pendingChanges;
		},
	};

	// Actions of a resource identified by one field (e.g. /item), they keep the signatures of the stores: an id instead of a key, no scope
	const idField = compositeKey[0];
	const single = {
		getByInterval(this: StoreGeneric, limit = 100, offset = 0, expand: string[] = [], filter: RSQLFilter[] = [], sort: RSQLSort = {}, clear = false): Promise<FetchPage> {
			return resource.getByInterval.call(this, {}, limit, offset, expand, filter, sort, clear);
		},
		getById(this: StoreGeneric, id: string, expand: string[] = []): Promise<void> {
			return resource.getById.call(this, { [idField]: id } as Parts, expand);
		},
		create(this: StoreGeneric, params: Body<T, "create">, isFormData = false): Promise<string> {
			return resource.create.call(this, {}, params, isFormData);
		},
		update(this: StoreGeneric, id: string, params: Body<T, "update">, isFormData = false): Promise<void> {
			return resource.update.call(this, { [idField]: id } as Parts, params, isFormData);
		},
		remove(this: StoreGeneric, id: string): Promise<void> {
			return resource.remove.call(this, { [idField]: id } as Parts);
		},
		createBulk(this: StoreGeneric, params: Body<T, "createBulk">): Promise<ReadBulkDto<Read>> {
			return resource.createBulk.call(this, {}, params);
		},
		getAvailableNewId(this: StoreGeneric): string {
			return resource.getAvailableNewId.call(this);
		},
		loadEditionPreset(this: StoreGeneric, id: string, preset: string | null = null) {
			resource.loadEditionPreset.call(this, { [idField]: id } as Parts, preset);
		},
	};
	return { ...resource, single };
}

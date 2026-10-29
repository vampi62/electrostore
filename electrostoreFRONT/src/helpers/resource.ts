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

export function createResource<T extends EntityTypes>({ compositeKey, path, entityPath, countKey, stateKey, loadingKey, editionKey, readyKey, onHydrate }: ResourceOptions<T>) {
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
		return compositeKey
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

	function bumpCounts(store: StoreGeneric, parts: Parts, delta: number) {
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
		async getByInterval(this: StoreGeneric, scope: Parts, limit = 100, offset = 0, expand: string[] = [], filter: RSQLFilter[] = [], sort: RSQLSort = {}, clear = false): Promise<FetchPage> {
			this[stateKey] ??= {};
			this[countKey] ??= {};
			if (clear) {
				for (const key of Object.keys(pick(this[stateKey], scope))) {
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
				this[countKey][toScopeKey(scope)] = res.pagination?.total ?? 0;
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
			this[countKey] ??= {};
			// if isFormData, params is converted to FormData and sent as multipart/form-data
			const body = isFormData ? buildFormData(params as Record<string, string | Blob>) : params;
			const data = await fetchWrapper.post<Read>({ url: `${baseUrl}${collectionUrl(scope)}`, useToken: "access", body, contentFile: isFormData });
			const key = toKey(data as Parts);
			this[stateKey][key] = data;
			this[countKey][toScopeKey(scope)] ??= 0;
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
			delete this[stateKey]?.[toKey(key)];
			bumpCounts(this, key, -1);
		},
		async createBulk(this: StoreGeneric, scope: Parts, params: Body<T, "createBulk">): Promise<ReadBulkDto<Read>> {
			this[stateKey] ??= {};
			this[countKey] ??= {};
			const res = await fetchWrapper.post<ReadBulkDto<Read>>({ url: `${baseUrl}${collectionUrl(scope)}/bulk`, useToken: "access", body: params });
			this[countKey][toScopeKey(scope)] ??= 0;
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
			const pendingEntries = Object.entries(readyEntries).filter(([stateId, entry]) => !entry.pushChange && isSaved(toParts(stateId)));
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
	return resource;
}

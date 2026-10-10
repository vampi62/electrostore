import { buildFormData, fetchWrapper, buildQuery } from "@/helpers";

import type { RSQLFilter, RSQLSort } from "@/types/rsql";
import type { Body, EntityTypes, NestedResourceOptions } from "@/types/resource";
import type { StoreGeneric } from "pinia";
import type { PaginatedResponseDto, ReadBulkDto } from "@/types/fetch";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export function createNestedResource<T extends EntityTypes>({ path, idField, countKey, stateKey, loadingKey, editionKey, readyKey, onHydrate }: NestedResourceOptions<T>) {
	type ReadExtended = "readExtended" extends keyof T ? T["readExtended"] : T["readBasic"];
	type Read = "readBasic" extends keyof T ? T["readBasic"] : T["readExtended"];
	const resource: Record<string, any> = {
		async getByInterval(this: StoreGeneric, idParentResource: string, limit = 100, offset = 0, expand: string[] = [], filter: RSQLFilter[] = [], sort: RSQLSort = {}, clear = false) {
			if (!this[stateKey][String(idParentResource)] || clear) {
				this[stateKey][String(idParentResource)] = {};
			}
			this[loadingKey] = true;
			try {
				const query = buildQuery({ offset, limit, expand, filter, sort });
				const res = await fetchWrapper.get<PaginatedResponseDto<ReadExtended>>({ url: `${baseUrl}${path(idParentResource)}?${query}`, useToken: "access" });
				for (const entity of res.data) {
					this[stateKey][String(idParentResource)][entity[idField]] = entity;
					onHydrate?.(this, entity, expand);
				}
				this[countKey][String(idParentResource)] = res.pagination?.total ?? 0;
				return [res.pagination?.next_offset ?? 0, res.pagination?.has_more ?? false];
			} finally {
				this[loadingKey] = false;
			}
		},
		async getById(this: StoreGeneric, idParentResource: string, id: string, expand: string[] = []) {
			this[stateKey][idParentResource] ??= {};
			try {
				const query = buildQuery({ expand });
				const data = await fetchWrapper.get<ReadExtended>({ url: `${baseUrl}${path(idParentResource)}/${id}?${query}`, useToken: "access" });
				this[stateKey][idParentResource][id] = data;
				onHydrate?.(this, data, expand);
			} finally {
				if (this[stateKey][idParentResource][id]) {
					this[stateKey][idParentResource][id].loading = false;
				}
			}
		},
		async create(this: StoreGeneric, idParentResource: string, params: Body<T, "create">) {
			this[stateKey][idParentResource] ??= {};
			// if param is FormData, we need to set the content type to multipart/form-data
			const data = await fetchWrapper.post<Read>({ url: `${baseUrl}${path(idParentResource)}`, useToken: "access", body: params, contentFile: params instanceof FormData });
			this[stateKey][idParentResource][data[idField]] = data;
			this[countKey][idParentResource] = (this[countKey][idParentResource] ?? 0) + 1;
			return data[idField];
		},
		async update(this: StoreGeneric, idParentResource: string, id: string, params: Body<T, "update">) {
			this[stateKey][idParentResource] ??= {};
			this[stateKey][idParentResource][id] = await fetchWrapper.put<Read>({ url: `${baseUrl}${path(idParentResource)}/${id}`, useToken: "access", body: params, contentFile: params instanceof FormData });
		},
		async remove(this: StoreGeneric, idParentResource: string, id: string) {
			await fetchWrapper.delete({ url: `${baseUrl}${path(idParentResource)}/${id}`, useToken: "access" });
			delete this[stateKey][idParentResource]?.[id];
			this[countKey][idParentResource] = (this[countKey][idParentResource] ?? 1) - 1;
		},
		async createBulk(this: StoreGeneric, idParentResource: string, params: Body<T, "createBulk">) {
			this[stateKey][idParentResource] ??= {};
			const res = await fetchWrapper.post<ReadBulkDto<Read>>({ url: `${baseUrl}${path(idParentResource)}/bulk`, useToken: "access", body: params });
			for (const entity of res.valide) {
				this[stateKey][idParentResource][entity[idField]] = entity;
			}
			this[countKey][idParentResource] = (this[countKey][idParentResource] ?? 0) + res.valide.length;
			return res;
		},
		async updateBulk(this: StoreGeneric, idParentResource: string, params: Body<T, "updateBulk">) {
			this[stateKey][idParentResource] ??= {};
			const res = await fetchWrapper.put<ReadBulkDto<Read>>({ url: `${baseUrl}${path(idParentResource)}/bulk`, useToken: "access", body: params });
			for (const entity of res.valide) {
				this[stateKey][idParentResource][entity[idField]] = entity;
			}
		},
		async removeBulk(this: StoreGeneric, idParentResource: string, ids: Body<T, "deleteBulk">) {
			this[stateKey][idParentResource] ??= {};
			const res = await fetchWrapper.delete<ReadBulkDto<Read>>({ url: `${baseUrl}${path(idParentResource)}/bulk`, useToken: "access", body: ids });
			for (const id of res.valide) {
				delete this[stateKey][idParentResource]?.[id];
			}
			this[countKey][idParentResource] = (this[countKey][idParentResource] ?? 0) - res.valide.length;
			this[countKey][idParentResource] = Math.max(this[countKey][idParentResource], 0);
		},
		getAvailableNewId(this: StoreGeneric, idParentResource: string) {
			if (!editionKey) {
				return;
			}
			this[editionKey][idParentResource] ??= {};
			let i = 1;
			while (Object.hasOwn(this[editionKey][idParentResource], `new-${i}`)) {
				i++;
			}
			const id = `new-${i}`;
			this[editionKey][idParentResource][id] = {};
			return id;
		},
		valideEditionById(this: StoreGeneric, idParentResource: string, id: string, status = "modified", isFormData = false) {
			if (!readyKey || !editionKey) {
				return;
			}
			this[readyKey][idParentResource] ??= {};
			const edition = this[editionKey][idParentResource]?.[id] ?? {};
			// compare this[stateKey][idParentResource]?.[id] with the edition to determine if changes exist
			if (JSON.stringify(this[stateKey][idParentResource]?.[id] ?? {}) === JSON.stringify(edition) && status !== "deleted") {
				delete this[readyKey][idParentResource][id];
				return;
			}
			// check if this[readyKey][idParentResource][id] already exists with status "created" and if the new changes has the status "deleted"
			if (this[readyKey][idParentResource][id]?.status === "created" && status === "deleted") {
				delete this[readyKey][idParentResource][id];
				return;
			}
			this[readyKey][idParentResource][id] = { ...edition, [idField]: id, status, isFormData };
		},
		copyPerId(this: StoreGeneric, idParentResource: string, oldId: string, newId: string) {
			if (!readyKey || !editionKey) {
				return;
			}
			this[editionKey][idParentResource] ??= {};
			this[readyKey][idParentResource] ??= {};
			if (this[editionKey][idParentResource][oldId] !== undefined) {
				this[editionKey][idParentResource][newId] = { ...this[editionKey][idParentResource][oldId], [idField]: newId };
			}
			if (this[readyKey][idParentResource][oldId] !== undefined) {
				this[readyKey][idParentResource][newId] = { ...this[readyKey][idParentResource][oldId], [idField]: newId };
			}
		},
		copyAllId(this: StoreGeneric, oldIdParentResource: string, newIdParentResource: string) {
			if (!readyKey || !editionKey) {
				return;
			}
			this[editionKey][newIdParentResource] = { ...this[editionKey][oldIdParentResource] };
			this[readyKey][newIdParentResource] = { ...this[readyKey][oldIdParentResource] };
			for (const [id, entry] of Object.entries(this[editionKey][newIdParentResource])) {
				this[editionKey][newIdParentResource][id] = { ...(entry as object), [idField]: id };
			}
			for (const [id, entry] of Object.entries(this[readyKey][newIdParentResource])) {
				this[readyKey][newIdParentResource][id] = { ...(entry as object), [idField]: id };
			}
		},
		async pushChange(this: StoreGeneric, idParentResource: string) {
			if (!readyKey) {
				return;
			}
			const readyEntries = { ...this[readyKey][idParentResource] };
			for (const [id, entry] of Object.entries(readyEntries) as [string, any][]) {
				const { status, isFormData, ...data } = entry;
				const isNewId = String(id).startsWith("new-");
				if (data?.pushChange) {
					continue; // Skip if already pushed
				}
				if (status === "created") {
					await resource.create.call(this, idParentResource, isFormData ? buildFormData(data) : data);
				} else if (status === "modified" && !isNewId) {
					await resource.update.call(this, idParentResource, id, isFormData ? buildFormData(data) : data);
				} else if (status === "deleted" && !isNewId) {
					await resource.remove.call(this, idParentResource, id);
				}
				entry.pushChange = true;
			}
		},
	};
	return resource;
}

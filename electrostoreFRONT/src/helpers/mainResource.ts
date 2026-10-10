import { fetchWrapper, buildQuery } from "@/helpers";

import type { RSQLFilter, RSQLSort } from "@/types/rsql";
import type { Body, EntityTypes, MainResourceOptions } from "@/types/resource";
import type { StoreGeneric } from "pinia";
import { PaginatedResponseDto, ReadBulkDto } from "@/types/fetch";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export function createMainResource<T extends EntityTypes>({ path, idField, countKey, stateKey, loadingKey, editionKey, onHydrate }: MainResourceOptions<T>) {
	type ReadExtended = "readExtended" extends keyof T ? T["readExtended"] : T["readBasic"];
	type Read = "readBasic" extends keyof T ? T["readBasic"] : T["readExtended"];
	return {
		async getByList(this: StoreGeneric, idResearch: string[] = [], expand: string[] = [], clear = false) {
			if (!this[stateKey] || clear) {
				this[stateKey] = {};
			}
			this[loadingKey] = true;
			try {
				const query = buildQuery({ idResearch, expand });
				const res = await fetchWrapper.get<PaginatedResponseDto<ReadExtended>>({ url: `${baseUrl}${path()}?${query}`, useToken: "access" });
				for (const entity of res.data) {
					this[stateKey][entity[idField]] = entity;
					onHydrate?.(this, entity, expand);
				}
				this[countKey] = res.pagination?.total ?? 0;
				return [res.pagination?.next_offset ?? 0, res.pagination?.has_more ?? false];
			} finally {
				this[loadingKey] = false;
			}
		},
		async getByInterval(this: StoreGeneric, limit = 100, offset = 0, expand: string[] = [], filter: RSQLFilter[] = [], sort: RSQLSort = {}, clear = false) {
			if (!this[stateKey] || clear) {
				this[stateKey] = {};
			}
			this[loadingKey] = true;
			try {
				const query = buildQuery({ offset, limit, expand, filter, sort });
				const res = await fetchWrapper.get<PaginatedResponseDto<ReadExtended>>({ url: `${baseUrl}${path()}?${query}`, useToken: "access" });
				for (const entity of res.data) {
					this[stateKey][entity[idField]] = entity;
					onHydrate?.(this, entity, expand);
				}
				this[countKey] = res.pagination?.total ?? 0;
				return [res.pagination?.next_offset ?? 0, res.pagination?.has_more ?? false];
			} finally {
				this[loadingKey] = false;
			}
		},
		async getById(this: StoreGeneric, id: string, expand: string[] = []) {
			this[stateKey] ??= {};
			try {
				const query = buildQuery({ expand });
				const data = await fetchWrapper.get<ReadExtended>({ url: `${baseUrl}${path()}/${id}?${query}`, useToken: "access" });
				this[stateKey][id] = data;
				onHydrate?.(this, data, expand);
			} finally {
				if (this[stateKey][id]) {
					this[stateKey][id].loading = false;
				}
			}
		},
		async create(this: StoreGeneric, params: Body<T, "create">) {
			this[stateKey] ??= {};
			// if param is FormData, we need to set the content type to multipart/form-data
			const data = await fetchWrapper.post<Read>({ url: `${baseUrl}${path()}`, useToken: "access", body: params, contentFile: params instanceof FormData });
			this[stateKey][data[idField]] = data;
			this[countKey] = (this[countKey] ?? 0) + 1;
			return data[idField] as string;
		},
		async update(this: StoreGeneric, id: string, params: Body<T, "update">) {
			this[stateKey] ??= {};
			this[stateKey][id] = await fetchWrapper.put<Read>({ url: `${baseUrl}${path()}/${id}`, useToken: "access", body: params, contentFile: params instanceof FormData });
		},
		async remove(this: StoreGeneric, id: string) {
			await fetchWrapper.delete({ url: `${baseUrl}${path()}/${id}`, useToken: "access" });
			delete this[stateKey]?.[id];
			this[countKey] = (this[countKey] ?? 1) - 1;
		},
		async createBulk(this: StoreGeneric, params: Body<T, "createBulk">) {
			this[stateKey] ??= {};
			const res = await fetchWrapper.post<ReadBulkDto<Read>>({ url: `${baseUrl}${path()}/bulk`, useToken: "access", body: params });
			for (const entity of res.valide) {
				this[stateKey][entity[idField]] = entity;
			}
			this[countKey] = (this[countKey] ?? 0) + res.valide.length;
			return res;
		},
		async updateBulk(this: StoreGeneric, params: Body<T, "updateBulk">) {
			this[stateKey] ??= {};
			const res = await fetchWrapper.put<ReadBulkDto<Read>>({ url: `${baseUrl}${path()}/bulk`, useToken: "access", body: params });
			for (const entity of res.valide) {
				this[stateKey][entity[idField]] = entity;
			}
		},
		async removeBulk(this: StoreGeneric, ids: Body<T, "deleteBulk">) {
			this[stateKey] ??= {};
			const res = await fetchWrapper.delete<ReadBulkDto<Read>>({ url: `${baseUrl}${path()}/bulk`, useToken: "access", body: ids });
			for (const id of res.valide) {
				delete this[stateKey]?.[id];
			}
			this[countKey] = (this[countKey] ?? 0) - res.valide.length;
			this[countKey] = Math.max(this[countKey], 0);
		},
		getAvailableNewId(this: StoreGeneric) {
			if (!editionKey) {
				throw new Error("Edition key is not defined");
			}
			this[editionKey] ??= {};
			let i = 1;
			while (Object.hasOwn(this[editionKey], `new-${i}`)) {
				i++;
			}
			const id = `new-${i}`;
			this[editionKey][id] = {};
			return id;
		},
		loadEditionPreset(this: StoreGeneric, id: string, preset: string | null = null) {
			if (!editionKey) {
				throw new Error("Edition key is not defined");
			}
			if (!preset) {
				return;
			}
			this[editionKey] ??= {};
			if (!this[editionKey][id]) {
				this[editionKey][id] = {};
			}
			for (const pair of preset.split(";")) {
				const [key, value] = pair.split(":");
				if (key && value) {
					this[editionKey][id][key] = value;
				}
			}
		},
	};
}

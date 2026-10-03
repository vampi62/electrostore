import { fetchWrapper, buildQuery } from "@/helpers";

import type { RSQLFilter, RSQLSort } from "@/types/rsql";
import type { MainResourceOptions } from "@/types/resource";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export function createMainResource({ path, idField, countKey, stateKey, loadingKey, editionKey, onHydrate }: MainResourceOptions) {
	return {
		async getByList(this: any, idResearch: any[] = [], expand: string[] = [], clear = false, externalParam: any[] = []) {
			if (!this[stateKey] || clear) {
				this[stateKey] = {};
			}
			this[loadingKey] = true;
			try {
				const query = buildQuery({ idResearch, expand });
				const res = await fetchWrapper.get({ url: `${baseUrl}${path()}?${query}`, useToken: "access" });
				for (const entity of res.data) {
					this[stateKey][entity[idField]] = entity;
					onHydrate?.(this, entity, expand, externalParam);
				}
				this[countKey] = res.pagination?.total ?? 0;
				return [res.pagination?.nextOffset ?? 0, res.pagination?.hasMore ?? false];
			} finally {
				this[loadingKey] = false;
			}
		},
		async getByInterval(this: any, limit = 100, offset = 0, expand: string[] = [], filter: RSQLFilter[] = [], sort: RSQLSort = {}, clear = false, externalParam: any[] = []) {
			if (!this[stateKey] || clear) {
				this[stateKey] = {};
			}
			this[loadingKey] = true;
			try {
				const query = buildQuery({ offset, limit, expand, filter, sort });
				const res = await fetchWrapper.get({ url: `${baseUrl}${path()}?${query}`, useToken: "access" });
				for (const entity of res.data) {
					this[stateKey][entity[idField]] = entity;
					onHydrate?.(this, entity, expand, externalParam);
				}
				this[countKey] = res.pagination?.total ?? 0;
				return [res.pagination?.nextOffset ?? 0, res.pagination?.hasMore ?? false];
			} finally {
				this[loadingKey] = false;
			}
		},
		async getById(this: any, id: any, expand: string[] = [], externalParam: any[] = []) {
			this[stateKey] ??= {};
			try {
				const query = buildQuery({ expand });
				const data = await fetchWrapper.get({ url: `${baseUrl}${path()}/${id}?${query}`, useToken: "access" });
				this[stateKey][id] = data;
				onHydrate?.(this, data, expand, externalParam);
			} finally {
				if (this[stateKey][id]) {
					this[stateKey][id].loading = false;
				}
			}
		},
		async create(this: any, params: any, externalParam: any[] = []) {
			this[stateKey] ??= {};
			// if param is FormData, we need to set the content type to multipart/form-data
			const data = await fetchWrapper.post({ url: `${baseUrl}${path()}`, useToken: "access", body: params, contentFile: params instanceof FormData });
			this[stateKey][data[idField]] = data;
			this[countKey] = (this[countKey] ?? 0) + 1;
			return data[idField];
		},
		async update(this: any, id: any, params: any, externalParam: any[] = []) {
			this[stateKey] ??= {};
			this[stateKey][id] = await fetchWrapper.put({ url: `${baseUrl}${path()}/${id}`, useToken: "access", body: params, contentFile: params instanceof FormData });
		},
		async remove(this: any, id: any, externalParam: any[] = []) {
			await fetchWrapper.delete({ url: `${baseUrl}${path()}/${id}`, useToken: "access" });
			delete this[stateKey]?.[id];
			this[countKey] = (this[countKey] ?? 1) - 1;
		},
		async createBulk(this: any, params: any, externalParam: any[] = []) {
			this[stateKey] ??= {};
			const res = await fetchWrapper.post({ url: `${baseUrl}${path()}/bulk`, useToken: "access", body: params });
			for (const entity of res.valide) {
				this[stateKey][entity[idField]] = entity;
			}
			this[countKey] = (this[countKey] ?? 0) + res.valide.length;
			return res;
		},
		async updateBulk(this: any, params: any, externalParam: any[] = []) {
			this[stateKey] ??= {};
			const res = await fetchWrapper.put({ url: `${baseUrl}${path()}/bulk`, useToken: "access", body: params });
			for (const entity of res.valide) {
				this[stateKey][entity[idField]] = entity;
			}
		},
		async removeBulk(this: any, ids: any[], externalParam: any[] = []) {
			this[stateKey] ??= {};
			const res = await fetchWrapper.delete({ url: `${baseUrl}${path()}/bulk`, useToken: "access", body: ids });
			for (const id of res.valide) {
				delete this[stateKey]?.[id];
			}
			this[countKey] = (this[countKey] ?? 0) - res.valide.length;
			this[countKey] = Math.max(this[countKey], 0);
		},
		getAvailableNewId(this: any) {
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
		loadEditionPreset(this: any, id: any, preset: string | null = null) {
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

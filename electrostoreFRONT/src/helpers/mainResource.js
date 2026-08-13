
import { fetchWrapper, buildQuery } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export function createMainResource({ path, idField, countKey, stateKey, loadingKey, onHydrate }) {
	return {
		async getByList(idResearch = [], expand = [], clear = false, externalParam = []) {
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
		async getByInterval({ limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false, externalParam = [] } = {}) {
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
		async getById(id, expand = [], externalParam = []) {
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
		async create(params, externalParam = []) {
			this[stateKey] ??= {};
			// if param is FormData, we need to set the content type to multipart/form-data
			const data = await fetchWrapper.post({ url: `${baseUrl}${path()}`, useToken: "access", body: params, contentFile: params instanceof FormData });
			this[stateKey][data[idField]] = data;
			this[countKey] = (this[countKey] ?? 0) + 1;
			return data[idField];
		},
		async update(id, params, externalParam = []) {
			this[stateKey] ??= {};
			this[stateKey][id] = await fetchWrapper.put({ url: `${baseUrl}${path()}/${id}`, useToken: "access", body: params });
		},
		async remove(id, externalParam = []) {
			await fetchWrapper.delete({ url: `${baseUrl}${path()}/${id}`, useToken: "access" });
			delete this[stateKey]?.[id];
			this[countKey] = (this[countKey] ?? 1) - 1;
		},
		async createBulk(params, externalParam = []) {
			this[stateKey] ??= {};
			const res = await fetchWrapper.post({ url: `${baseUrl}${path()}/bulk`, useToken: "access", body: params });
			for (const entity of res.valide) {
				this[stateKey][entity[idField]] = entity;
			}
			this[countKey] = (this[countKey] ?? 0) + res.valide.length;
			return res;
		},
		async updateBulk(params, externalParam = []) {
			this[stateKey] ??= {};
			const res = await fetchWrapper.put({ url: `${baseUrl}${path()}/bulk`, useToken: "access", body: params });
			for (const entity of res.valide) {
				this[stateKey][entity[idField]] = entity;
			}
		},
		async removeBulk(ids, externalParam = []) {
			this[stateKey] ??= {};
			const res = await fetchWrapper.delete({ url: `${baseUrl}${path()}/bulk`, useToken: "access", body: ids });
			for (const id of res.valide) {
				delete this[stateKey]?.[id];
			}
			this[countKey] = (this[countKey] ?? 0) - res.valide.length;
			this[countKey] = Math.max(this[countKey], 0);
		},
	};
}
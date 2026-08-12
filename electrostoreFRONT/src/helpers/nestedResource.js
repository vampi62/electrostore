import { fetchWrapper, buildQuery } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export function createNestedResource({ path, idField, countKey, stateKey, loadingKey, onHydrate }) {
	return {
		async getByInterval(idParentResource, { limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false, externalParam = [] } = {}) {
			if (!this[stateKey][String(idParentResource)] || clear) {
				this[stateKey][String(idParentResource)] = {};
			}
			this[loadingKey] = true;
			try {
				const query = buildQuery({ offset, limit, expand, filter, sort });
				const res = await fetchWrapper.get({ url: `${baseUrl}${path(idParentResource)}?${query}`, useToken: "access" });
				for (const entity of res.data) {
					this[stateKey][String(idParentResource)][entity[idField]] = entity;
					onHydrate?.(this, entity, expand, externalParam);
				}
				this[countKey][String(idParentResource)] = res.pagination?.total ?? 0;
				return [res.pagination?.nextOffset ?? 0, res.pagination?.hasMore ?? false];
			} finally {
				this[loadingKey] = false;
			}
		},
		async getById(idParentResource, id, expand = [], externalParam = []) {
			this[stateKey][idParentResource] ??= {};
			try {
				const query = buildQuery({ expand });
				const data = await fetchWrapper.get({ url: `${baseUrl}${path(idParentResource)}/${id}?${query}`, useToken: "access" });
				this[stateKey][idParentResource][id] = data;
				onHydrate?.(this, data, expand, externalParam);
			} finally {
				if (this[stateKey][idParentResource][id]) {
					this[stateKey][idParentResource][id].loading = false;
				}
			}
		},
		async create(idParentResource, params, externalParam = []) {
			this[stateKey][idParentResource] ??= {};
			// if param is FormData, we need to set the content type to multipart/form-data
			const data = await fetchWrapper.post({ url: `${baseUrl}${path(idParentResource)}`, useToken: "access", body: params, contentFile: params instanceof FormData });
			this[stateKey][idParentResource][data[idField]] = data;
			this[countKey][idParentResource] = (this[countKey][idParentResource] ?? 0) + 1;
			return data[idField];
		},
		async update(idParentResource, id, params, externalParam = []) {
			this[stateKey][idParentResource] ??= {};
			this[stateKey][idParentResource][id] = await fetchWrapper.put({ url: `${baseUrl}${path(idParentResource)}/${id}`, useToken: "access", body: params });
		},
		async remove(idParentResource, id, externalParam = []) {
			await fetchWrapper.delete({ url: `${baseUrl}${path(idParentResource)}/${id}`, useToken: "access" });
			delete this[stateKey][idParentResource]?.[id];
			this[countKey][idParentResource] = (this[countKey][idParentResource] ?? 1) - 1;
		},
		async createBulk(idParentResource, params, externalParam = []) {
			this[stateKey][idParentResource] ??= {};
			const res = await fetchWrapper.post({ url: `${baseUrl}${path(idParentResource)}/bulk`, useToken: "access", body: params });
			for (const entity of res.valide) {
				this[stateKey][idParentResource][entity[idField]] = entity;
			}
			this[countKey][idParentResource] = (this[countKey][idParentResource] ?? 0) + res.valide.length;
			return res;
		},
		async updateBulk(idParentResource, params, externalParam = []) {
			this[stateKey][idParentResource] ??= {};
			const res = await fetchWrapper.put({ url: `${baseUrl}${path(idParentResource)}/bulk`, useToken: "access", body: params });
			for (const entity of res.valide) {
				this[stateKey][idParentResource][entity[idField]] = entity;
			}
		},
		async removeBulk(idParentResource, ids, externalParam = []) {
			this[stateKey][idParentResource] ??= {};
			const res = await fetchWrapper.delete({ url: `${baseUrl}${path(idParentResource)}/bulk`, useToken: "access", body: ids });
			for (const id of res.valide) {
				delete this[stateKey][idParentResource]?.[id];
			}
			this[countKey][idParentResource] = (this[countKey][idParentResource] ?? 0) - res.valide.length;
			this[countKey][idParentResource] = Math.max(this[countKey][idParentResource], 0);
		},
	};
}
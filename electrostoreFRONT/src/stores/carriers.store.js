import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useCarriersStore = defineStore("carriers", {
	state: () => ({
		carriersLoading: false,
		carriersTotalCount: 0,
		carriers: {},
	}),
	actions: {
		async getCarrierByList(idResearch = []) {
			this.carriersLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const paramString = [idResearchString].join("&");
			const newCarrierList = await fetchWrapper.get({
				url: `${baseUrl}/carrier?${paramString}`,
				useToken: "access",
			});
			for (const carrier of newCarrierList["data"]) {
				this.carriers[carrier.id_carrier] = carrier;
			}
			this.carriersLoading = false;
		},
		async getCarrierByInterval(limit = 100, offset = 0, filter = "", sort = "", clear = false) {
			this.carriersLoading = true;
			if (clear) {
				this.carriers = {};
			}
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, filterString, sortString].join("&");
			const newCarrierList = await fetchWrapper.get({
				url: `${baseUrl}/carrier?${paramString}`,
				useToken: "access",
			});
			for (const carrier of newCarrierList["data"]) {
				this.carriers[carrier.id_carrier] = carrier;
			}
			this.carriersTotalCount = newCarrierList["pagination"]?.["total"] || 0;
			this.carriersLoading = false;
			return [newCarrierList["pagination"]?.["nextOffset"] || 0, newCarrierList["pagination"]?.["hasMore"] || false];
		},
		async getCarrierById(id) {
			if (!this.carriers[id]) {
				this.carriers[id] = {};
			}
			this.carriers[id].loading = true;
			this.carriers[id] = await fetchWrapper.get({
				url: `${baseUrl}/carrier/${id}`,
				useToken: "access",
			});
		},
	},
});

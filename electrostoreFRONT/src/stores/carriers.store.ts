import { defineStore } from "pinia";

import { createMainResource } from "@/helpers";

import type { components } from "@/types/api";
type ReadCarrierDto = components["schemas"]["ReadCarrierDto"];

const carrierResource = createMainResource({
	path: () => "/carrier",
	idField: "id_carrier",
	stateKey: "carriers",
	countKey: "carriersTotalCount",
	loadingKey: "carriersLoading",
});

export const useCarriersStore = defineStore("carriers", {
	state: () => ({
		carriersLoading: false,
		carriersTotalCount: 0,
		carriers: {} as Record<string, ReadCarrierDto>,
	}),
	actions: {
		getCarrierByList: carrierResource.getByList,
		getCarrierByInterval: carrierResource.getByInterval,
		getCarrierById: carrierResource.getById,
	},
});

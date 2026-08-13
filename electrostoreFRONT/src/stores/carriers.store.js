import { defineStore } from "pinia";

import { createMainResource } from "@/helpers";

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
		carriers: {},
	}),
	actions: {
		getCarrierByList: carrierResource.getByList,
		getCarrierByInterval: carrierResource.getByInterval,
		getCarrierById: carrierResource.getById,
	},
});

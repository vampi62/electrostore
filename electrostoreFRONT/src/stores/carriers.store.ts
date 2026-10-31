import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import type { components } from "@/types/api";
import { isNewId } from "@/utils/newId";
type ReadCarrierDto = components["schemas"]["ReadCarrierDto"];

const carrierResource = createResource<{ readExtended: ReadCarrierDto }>({
	compositeKey: ["id_carrier"],
	path: [{ keys: [], url: () => "/carrier" }],
	entityPath: [{ keys: ["id_carrier"], url: ({ id_carrier }) => `/carrier/${id_carrier}` }],
	stateKey: "carriers",
	countKey: "carriersTotalCount",
	loadingKey: "carriersLoading",
	editionKey: "carrierEdition",
});

export const useCarriersStore = defineStore("carriers", {
	state: () => ({
		carriersLoading: false,
		carriersTotalCount: 0,
		carriers: {} as Record<string, ReadCarrierDto>,
		carrierEdition: {} as Record<string, Partial<ReadCarrierDto> & { loading?: boolean }>,
	}),
	actions: {
		getCarrierByInterval: carrierResource.single.getByInterval,
		getCarrierById: carrierResource.single.getById,
		loadToEdition(id: string, preset: string | null = null) {
			if (!isNewId(id) && this.carriers[id]) {
				this.carrierEdition[id] = {
					loading: false,
					...this.carriers[id],
				};
			} else {
				this.carrierEdition[id] = {
					loading: false,
				};
			}
			carrierResource.single.loadEditionPreset.call(this, id, preset);
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.carrierEdition[id]) {
				this.carrierEdition[id] = {};
			}
			this.carrierEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.carrierEdition[id];
		},
	},
});

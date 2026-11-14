import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import type { RSQLFilter } from "@/types/rsql";
import type { components } from "@/types/api";
import { isNewId } from "@/utils/newId";
type ReadCarrierDto = components["schemas"]["ReadCarrierDto"];

const carrierResource = createResource<{ readExtended: ReadCarrierDto }>({
	compositeKey: ["id_carrier"],
	path: [{ keys: [], url: () => "/carrier" }],
	entityPath: [{ keys: ["id_carrier"], url: ({ id_carrier }) => `/carrier/${id_carrier}` }],
	stateKey: "carriers",
	countKey: "carriersTotalCount",
	editionKey: "carrierEdition",
});

export const useCarriersStore = defineStore("carriers", {
	state: () => ({
		carriersTotalCount: {} as Record<string, number>,
		carriers: {} as Record<string, ReadCarrierDto>,
		carrierEdition: {} as Record<string, Partial<ReadCarrierDto> & { loading?: boolean }>,
	}),
	getters: {
		// number of records of the list read with these filters
		carriersCount: (state) => (filters?: RSQLFilter[]) => state.carriersTotalCount[carrierResource.toCountKey({}, filters)] ?? 0,
	},
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

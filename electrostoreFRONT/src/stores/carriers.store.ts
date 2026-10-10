import { defineStore } from "pinia";

import { createMainResource } from "@/helpers";

import type { components } from "@/types/api";
import { isNewId } from "@/utils/newId";
type ReadCarrierDto = components["schemas"]["ReadCarrierDto"];

type EditionCarrierDto = {
	loading?: boolean;
} & Partial<ReadCarrierDto>;

const carrierResource = createMainResource<{ readExtended: ReadCarrierDto }>({
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
		carrierEdition: {} as Record<string, EditionCarrierDto>,
	}),
	actions: {
		getCarrierByList: carrierResource.getByList,
		getCarrierByInterval: carrierResource.getByInterval,
		getCarrierById: carrierResource.getById,
		loadToEdition(id: string, preset = null) {
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
			carrierResource.loadEditionPreset.call(this, id, preset);
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

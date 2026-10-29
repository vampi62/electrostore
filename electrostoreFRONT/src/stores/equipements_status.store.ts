import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import type { components } from "@/types/api";

type ReadEquipementStatusDto = components["schemas"]["ReadEquipementStatusDto"];

const equipementStatusResource = createResource<{ readBasic: ReadEquipementStatusDto }>({
	compositeKey: ["id_equipement", "id_equipement_status"],
	path: [
		{ keys: ["id_equipement"], url: ({ id_equipement }) => `/equipement/${id_equipement}/status-history` },
	],
	entityPath: [
		{ keys: ["id_equipement", "id_equipement_status"], url: ({ id_equipement, id_equipement_status }) => `/equipement/${id_equipement}/status-history/${id_equipement_status}` },
	],
	stateKey: "equipementStatus",
	countKey: "equipementStatusTotalCount",
	loadingKey: "equipementStatusLoading",
});

// Records are indexed by the composite key "<id_equipement>:<id_equipement_status>"
export const useEquipementsStatusStore = defineStore("equipementsStatus", {
	state: () => ({
		equipementStatusLoading: false,
		// indexed by scope: "id_equipement=3"
		equipementStatusTotalCount: {} as Record<string, number>,
		equipementStatus: {} as Record<string, ReadEquipementStatusDto>,
	}),
	getters: {
		// the records of a equipement, indexed by the rest of the key
		equipementStatusByEquipement: (state) => (id: string) => equipementStatusResource.slice(state.equipementStatus, { id_equipement: id }),
		equipementStatusCountByEquipement: (state) => (id: string) => state.equipementStatusTotalCount[equipementStatusResource.toScopeKey({ id_equipement: id })] ?? 0,
	},
	actions: {
		getEquipementStatusByInterval: equipementStatusResource.getByInterval,
		getEquipementStatusById: equipementStatusResource.getById,
		hydrateEquipementStatus: equipementStatusResource.hydrate,
	},
});

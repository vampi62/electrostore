import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useStoresBoxsStore, useEquipementsStore } from "@/stores";

import type { Edition, Ready } from "@/types/resource";
import type { components } from "@/types/api";

type ReadEquipementBoxDto = components["schemas"]["ReadEquipementBoxDto"];
type ReadExtendedEquipementBoxDto = components["schemas"]["ReadExtendedEquipementBoxDto"];
type CreateEquipementBoxByEquipementDto = components["schemas"]["CreateEquipementBoxByEquipementDto"];
type CreateEquipementBoxByBoxDto = components["schemas"]["CreateEquipementBoxByBoxDto"];
type CreateEquipementBox = CreateEquipementBoxByEquipementDto | CreateEquipementBoxByBoxDto;

const equipementBoxResource = createResource<{ readExtended: ReadExtendedEquipementBoxDto, readBasic: ReadEquipementBoxDto, create: CreateEquipementBox }>({
	compositeKey: ["id_equipement", "id_box"],
	routeOnlyFields: ["id_store"],
	path: [
		{ keys: ["id_equipement"], url: ({ id_equipement }) => `/equipement/${id_equipement}/box` },
		{ keys: ["id_store", "id_box"], url: ({ id_store, id_box }) => `/store/${id_store}/box/${id_box}/equipement` },
	],
	entityPath: [
		{ keys: ["id_equipement", "id_box"], url: ({ id_equipement, id_box }) => `/equipement/${id_equipement}/box/${id_box}` },
	],
	stateKey: "equipementBoxs",
	countKey: "equipementBoxsTotalCount",
	loadingKey: "equipementBoxsLoading",
	editionKey: "equipementBoxEdition",
	readyKey: "equipementBoxReady",
	onHydrate: (store, entity: ReadExtendedEquipementBoxDto, expand) => {
		if (expand.includes("box") && entity.box) {
			useStoresBoxsStore().upsertBoxs([entity.box]);
		}
		if (expand.includes("equipement") && entity.equipement && entity.id_equipement) {
			useEquipementsStore().equipements[entity.id_equipement] = entity.equipement;
		}
	},
});

// Records are indexed by the composite key "<id_equipement>:<id_box>"
export const useEquipementsBoxsStore = defineStore("equipementsBoxs", {
	state: () => ({
		equipementBoxsLoading: false,
		// indexed by scope: "id_equipement=3"
		equipementBoxsTotalCount: {} as Record<string, number>,
		equipementBoxs: {} as Record<string, ReadEquipementBoxDto>,
		equipementBoxEdition: {} as Record<string, Edition<{ create: CreateEquipementBox }>>,
		equipementBoxReady: {} as Record<string, Ready<{ create: CreateEquipementBox }>>,
	}),
	getters: {
		// the records of a equipement, indexed by the rest of the key
		equipementBoxsByEquipement: (state) => (id: string) => equipementBoxResource.slice(state.equipementBoxs, { id_equipement: id }),
		equipementBoxsCountByEquipement: (state) => (id: string) => state.equipementBoxsTotalCount[equipementBoxResource.toScopeKey({ id_equipement: id })] ?? 0,
		equipementBoxEditionByEquipement: (state) => (id: string) => equipementBoxResource.live(state.equipementBoxEdition, { id_equipement: id }),
		equipementBoxReadyByEquipement: (state) => (id: string) => equipementBoxResource.live(state.equipementBoxReady, { id_equipement: id }),
		// the records of a box, indexed by the rest of the key
		equipementBoxsByBox: (state) => (id: string) => equipementBoxResource.slice(state.equipementBoxs, { id_box: id }),
		equipementBoxsCountByBox: (state) => (id: string) => state.equipementBoxsTotalCount[equipementBoxResource.toScopeKey({ id_box: id })] ?? 0,
		equipementBoxEditionByBox: (state) => (id: string) => equipementBoxResource.live(state.equipementBoxEdition, { id_box: id }),
		equipementBoxReadyByBox: (state) => (id: string) => equipementBoxResource.live(state.equipementBoxReady, { id_box: id }),
	},
	actions: {
		getEquipementBoxByInterval: equipementBoxResource.getByInterval,
		getEquipementBoxById: equipementBoxResource.getById,
		createEquipementBox: equipementBoxResource.create,
		deleteEquipementBox: equipementBoxResource.remove,
		hydrateEquipementBoxs: equipementBoxResource.hydrate,
		upsertEquipementBoxs: equipementBoxResource.upsert,
		setEquipementBoxEdition: equipementBoxResource.setEdition,
		removeEquipementBoxEdition: equipementBoxResource.removeEdition,
		removeEquipementBoxReady: equipementBoxResource.removeReady,
		setEquipementBoxStagedSlice: equipementBoxResource.setStagedSlice,
		valideEquipementBoxEditionByKey: equipementBoxResource.valideEditionByKey,
		copyEquipementBoxByKeyPart: equipementBoxResource.copyByKeyPart,
		clearEquipementBoxEdition: equipementBoxResource.clearEdition,
		pushEquipementBoxChange: equipementBoxResource.pushChange,
	},
});

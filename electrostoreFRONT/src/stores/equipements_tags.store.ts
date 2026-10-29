import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useTagsStore, useEquipementsStore } from "@/stores";

import type { Edition, Ready } from "@/types/resource";
import type { components } from "@/types/api";

type ReadEquipementTagDto = components["schemas"]["ReadEquipementTagDto"];
type ReadExtendedEquipementTagDto = components["schemas"]["ReadExtendedEquipementTagDto"];
type CreateEquipementTagByEquipementDto = components["schemas"]["CreateEquipementTagByEquipementDto"];
type CreateEquipementTagByTagDto = components["schemas"]["CreateEquipementTagByTagDto"];
type CreateEquipementTag = CreateEquipementTagByEquipementDto | CreateEquipementTagByTagDto;

const equipementTagResource = createResource<{ readExtended: ReadExtendedEquipementTagDto, readBasic: ReadEquipementTagDto, create: CreateEquipementTag, createBulk: CreateEquipementTag[], deleteBulk: number[] }>({
	compositeKey: ["id_equipement", "id_tag"],
	path: [
		{ keys: ["id_equipement"], url: ({ id_equipement }) => `/equipement/${id_equipement}/tag` },
		{ keys: ["id_tag"], url: ({ id_tag }) => `/tag/${id_tag}/equipement` },
	],
	entityPath: [
		{ keys: ["id_equipement", "id_tag"], url: ({ id_equipement, id_tag }) => `/equipement/${id_equipement}/tag/${id_tag}` },
	],
	stateKey: "equipementTags",
	countKey: "equipementTagsTotalCount",
	loadingKey: "equipementTagsLoading",
	editionKey: "equipementTagEdition",
	readyKey: "equipementTagReady",
	onHydrate: (store, entity: ReadExtendedEquipementTagDto, expand) => {
		if (expand.includes("tag") && entity.tag && entity.id_tag) {
			useTagsStore().tags[entity.id_tag] = entity.tag;
		}
		if (expand.includes("equipement") && entity.equipement && entity.id_equipement) {
			useEquipementsStore().equipements[entity.id_equipement] = entity.equipement;
		}
	},
});

// Records are indexed by the composite key "<id_equipement>:<id_tag>"
export const useEquipementsTagsStore = defineStore("equipementsTags", {
	state: () => ({
		equipementTagsLoading: false,
		// indexed by scope: "id_equipement=3"
		equipementTagsTotalCount: {} as Record<string, number>,
		equipementTags: {} as Record<string, ReadEquipementTagDto>,
		equipementTagEdition: {} as Record<string, Edition<{ create: CreateEquipementTag }>>,
		equipementTagReady: {} as Record<string, Ready<{ create: CreateEquipementTag }>>,
	}),
	getters: {
		// the records of a equipement, indexed by the rest of the key
		equipementTagsByEquipement: (state) => (id: string) => equipementTagResource.slice(state.equipementTags, { id_equipement: id }),
		equipementTagsCountByEquipement: (state) => (id: string) => state.equipementTagsTotalCount[equipementTagResource.toScopeKey({ id_equipement: id })] ?? 0,
		equipementTagsGroupedByEquipement: (state) => equipementTagResource.group(state.equipementTags, "id_equipement"),
		equipementTagEditionByEquipement: (state) => (id: string) => equipementTagResource.live(state.equipementTagEdition, { id_equipement: id }),
		equipementTagReadyByEquipement: (state) => (id: string) => equipementTagResource.live(state.equipementTagReady, { id_equipement: id }),
		// the records of a tag, indexed by the rest of the key
		equipementTagsByTag: (state) => (id: string) => equipementTagResource.slice(state.equipementTags, { id_tag: id }),
		equipementTagsCountByTag: (state) => (id: string) => state.equipementTagsTotalCount[equipementTagResource.toScopeKey({ id_tag: id })] ?? 0,
		equipementTagsGroupedByTag: (state) => equipementTagResource.group(state.equipementTags, "id_tag"),
		equipementTagEditionByTag: (state) => (id: string) => equipementTagResource.live(state.equipementTagEdition, { id_tag: id }),
		equipementTagReadyByTag: (state) => (id: string) => equipementTagResource.live(state.equipementTagReady, { id_tag: id }),
	},
	actions: {
		getEquipementTagByInterval: equipementTagResource.getByInterval,
		getEquipementTagById: equipementTagResource.getById,
		createEquipementTag: equipementTagResource.create,
		deleteEquipementTag: equipementTagResource.remove,
		createEquipementTagBulk: equipementTagResource.createBulk,
		deleteEquipementTagBulk: equipementTagResource.removeBulk,
		hydrateEquipementTags: equipementTagResource.hydrate,
		setEquipementTagEdition: equipementTagResource.setEdition,
		removeEquipementTagEdition: equipementTagResource.removeEdition,
		removeEquipementTagReady: equipementTagResource.removeReady,
		setEquipementTagStagedSlice: equipementTagResource.setStagedSlice,
		valideEquipementTagEditionByKey: equipementTagResource.valideEditionByKey,
		copyEquipementTagByKeyPart: equipementTagResource.copyByKeyPart,
		clearEquipementTagEdition: equipementTagResource.clearEdition,
		pushEquipementTagChange: equipementTagResource.pushChange,
	},
});

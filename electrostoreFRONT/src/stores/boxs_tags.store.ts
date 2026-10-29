import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useStoresBoxsStore, useTagsStore } from "@/stores";

import type { Edition, Ready } from "@/types/resource";
import type { components } from "@/types/api";

type ReadBoxTagDto = components["schemas"]["ReadBoxTagDto"];
type ReadExtendedBoxTagDto = components["schemas"]["ReadExtendedBoxTagDto"];
type CreateBoxTagByBoxDto = components["schemas"]["CreateBoxTagByBoxDto"];
type CreateBoxTagByTagDto = components["schemas"]["CreateBoxTagByTagDto"];
type CreateBoxTag = CreateBoxTagByBoxDto | CreateBoxTagByTagDto;

const boxTagResource = createResource<{ readExtended: ReadExtendedBoxTagDto, readBasic: ReadBoxTagDto, create: CreateBoxTag, createBulk: CreateBoxTag[], deleteBulk: number[] }>({
	compositeKey: ["id_box", "id_tag"],
	routeOnlyFields: ["id_store"],
	path: [
		{ keys: ["id_store", "id_box"], url: ({ id_store, id_box }) => `/store/${id_store}/box/${id_box}/tag` },
		{ keys: ["id_tag"], url: ({ id_tag }) => `/tag/${id_tag}/box` },
	],
	entityPath: [
		{ keys: ["id_tag", "id_box"], url: ({ id_tag, id_box }) => `/tag/${id_tag}/box/${id_box}` },
	],
	stateKey: "boxTags",
	countKey: "boxTagsTotalCount",
	loadingKey: "boxTagsLoading",
	editionKey: "boxTagEdition",
	readyKey: "boxTagReady",
	onHydrate: (store, entity: ReadExtendedBoxTagDto, expand) => {
		if (expand.includes("tag") && entity.tag && entity.id_tag) {
			useTagsStore().tags[entity.id_tag] = entity.tag;
		}
		if (expand.includes("box") && entity.box) {
			useStoresBoxsStore().upsertBoxs([entity.box]);
		}
	},
});

// Records are indexed by the composite key "<id_box>:<id_tag>"
export const useBoxsTagsStore = defineStore("boxsTags", {
	state: () => ({
		boxTagsLoading: false,
		// indexed by scope: "id_box=3"
		boxTagsTotalCount: {} as Record<string, number>,
		boxTags: {} as Record<string, ReadBoxTagDto>,
		boxTagEdition: {} as Record<string, Edition<{ create: CreateBoxTag }>>,
		boxTagReady: {} as Record<string, Ready<{ create: CreateBoxTag }>>,
	}),
	getters: {
		// the records of a box, indexed by the rest of the key
		boxTagsByBox: (state) => (id: string) => boxTagResource.slice(state.boxTags, { id_box: id }),
		boxTagsCountByBox: (state) => (id: string) => state.boxTagsTotalCount[boxTagResource.toScopeKey({ id_box: id })] ?? 0,
		boxTagsGroupedByBox: (state) => boxTagResource.group(state.boxTags, "id_box"),
		boxTagEditionByBox: (state) => (id: string) => boxTagResource.live(state.boxTagEdition, { id_box: id }),
		boxTagReadyByBox: (state) => (id: string) => boxTagResource.live(state.boxTagReady, { id_box: id }),
		// the records of a tag, indexed by the rest of the key
		boxTagsByTag: (state) => (id: string) => boxTagResource.slice(state.boxTags, { id_tag: id }),
		boxTagsCountByTag: (state) => (id: string) => state.boxTagsTotalCount[boxTagResource.toScopeKey({ id_tag: id })] ?? 0,
		boxTagsGroupedByTag: (state) => boxTagResource.group(state.boxTags, "id_tag"),
		boxTagEditionByTag: (state) => (id: string) => boxTagResource.live(state.boxTagEdition, { id_tag: id }),
		boxTagReadyByTag: (state) => (id: string) => boxTagResource.live(state.boxTagReady, { id_tag: id }),
	},
	actions: {
		getBoxTagByInterval: boxTagResource.getByInterval,
		getBoxTagById: boxTagResource.getById,
		createBoxTag: boxTagResource.create,
		deleteBoxTag: boxTagResource.remove,
		createBoxTagBulk: boxTagResource.createBulk,
		deleteBoxTagBulk: boxTagResource.removeBulk,
		hydrateBoxTags: boxTagResource.hydrate,
		upsertBoxTags: boxTagResource.upsert,
		setBoxTagEdition: boxTagResource.setEdition,
		removeBoxTagEdition: boxTagResource.removeEdition,
		removeBoxTagReady: boxTagResource.removeReady,
		setBoxTagStagedSlice: boxTagResource.setStagedSlice,
		valideBoxTagEditionByKey: boxTagResource.valideEditionByKey,
		copyBoxTagByKeyPart: boxTagResource.copyByKeyPart,
		clearBoxTagEdition: boxTagResource.clearEdition,
		pushBoxTagChange: boxTagResource.pushChange,
	},
});

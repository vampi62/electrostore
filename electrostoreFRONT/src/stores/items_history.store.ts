import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import type { components } from "@/types/api";

type ReadItemHistoryDto = components["schemas"]["ReadItemHistoryDto"];

const itemHistoryResource = createResource<{ readBasic: ReadItemHistoryDto }>({
	compositeKey: ["id_item", "id_item_history"],
	path: [{ keys: ["id_item"], url: ({ id_item }) => `/item/${id_item}/history` }],
	entityPath: [{ keys: ["id_item", "id_item_history"], url: ({ id_item, id_item_history }) => `/item/${id_item}/history/${id_item_history}` }],
	stateKey: "itemHistory",
	countKey: "itemHistoryTotalCount",
	loadingKey: "itemHistoryLoading",
});

// Records are indexed by the composite key "<id_item>:<id_item_history>"
export const useItemsHistoryStore = defineStore("itemsHistory", {
	state: () => ({
		itemHistoryLoading: false,
		// indexed by scope: "id_item=3"
		itemHistoryTotalCount: {} as Record<string, number>,
		itemHistory: {} as Record<string, ReadItemHistoryDto>,
	}),
	getters: {
		// the records of an item, indexed by id_item_history
		itemHistoryByItem: (state) => (idItem: string) => itemHistoryResource.slice(state.itemHistory, { id_item: idItem }),
		itemHistoryCountByItem: (state) => (idItem: string) => state.itemHistoryTotalCount[itemHistoryResource.toScopeKey({ id_item: idItem })] ?? 0,
	},
	actions: {
		getItemHistoryByInterval: itemHistoryResource.getByInterval,
		getItemHistoryById: itemHistoryResource.getById,
		hydrateItemHistory: itemHistoryResource.hydrate,
	},
});

import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import type { Edition } from "@/types/resource";
import type { components } from "@/types/api";

type ReadLedDto = components["schemas"]["ReadLedDto"];
type CreateLedByStoreDto = components["schemas"]["CreateLedByStoreDto"];
type UpdateLedByStoreDto = components["schemas"]["UpdateLedByStoreDto"];
type UpdateBulkLedByStoreDto = components["schemas"]["UpdateBulkLedByStoreDto"];

const ledResource = createResource<{ readBasic: ReadLedDto, create: CreateLedByStoreDto, update: UpdateLedByStoreDto, createBulk: CreateLedByStoreDto[], updateBulk: UpdateBulkLedByStoreDto[], deleteBulk: number[] }>({
	compositeKey: ["id_store", "id_led"],
	generatedField: "id_led",
	path: [
		{ keys: ["id_store"], url: ({ id_store }) => `/store/${id_store}/led` },
	],
	entityPath: [
		{ keys: ["id_store", "id_led"], url: ({ id_store, id_led }) => `/store/${id_store}/led/${id_led}` },
	],
	stateKey: "leds",
	countKey: "ledsTotalCount",
	loadingKey: "ledsLoading",
	editionKey: "ledEdition",
});

// Records are indexed by the composite key "<id_store>:<id_led>"
export const useStoresLedsStore = defineStore("storesLeds", {
	state: () => ({
		ledsLoading: false,
		// indexed by scope: "id_store=3"
		ledsTotalCount: {} as Record<string, number>,
		leds: {} as Record<string, ReadLedDto>,
		ledEdition: {} as Record<string, Edition<{ create: CreateLedByStoreDto, update: UpdateBulkLedByStoreDto }>>,
	}),
	getters: {
		// the records of a store, indexed by the rest of the key
		ledsByStore: (state) => (id: string) => ledResource.slice(state.leds, { id_store: id }),
		ledsCountByStore: (state) => (id: string) => state.ledsTotalCount[ledResource.toScopeKey({ id_store: id })] ?? 0,
		ledEditionByStore: (state) => (id: string) => ledResource.live(state.ledEdition, { id_store: id }),
	},
	actions: {
		getLedByInterval: ledResource.getByInterval,
		getLedById: ledResource.getById,
		createLed: ledResource.create,
		updateLed: ledResource.update,
		deleteLed: ledResource.remove,
		createLedBulk: ledResource.createBulk,
		updateLedBulk: ledResource.updateBulk,
		deleteLedBulk: ledResource.removeBulk,
		hydrateLeds: ledResource.hydrate,
		upsertLeds: ledResource.upsert,
		getAvailableNewLedId: ledResource.getAvailableNewId,
		setLedEdition: ledResource.setEdition,
		removeLedEdition: ledResource.removeEdition,
		setLedStagedSlice: ledResource.setStagedSlice,
		clearLedEdition: ledResource.clearEdition,
	},
});

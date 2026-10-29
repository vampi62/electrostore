import { defineStore } from "pinia";

import { fetchWrapper, createResource } from "@/helpers";
import { isNewId } from "@/utils";
import { StorePositionMode } from "@/enums";

import { useStoresBoxsStore, useStoresLedsStore, useStoresTagsStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Edition } from "@/types/resource";
import type { ShowParams } from "@/types/store";
import type { components } from "@/types/api";
type ReadStoreDto = components["schemas"]["ReadStoreDto"];
type ReadExtendedStoreDto = components["schemas"]["ReadExtendedStoreDto"];
type CreateStoreDto = components["schemas"]["CreateStoreDto"];
type UpdateStoreDto = components["schemas"]["UpdateStoreDto"];
type CreateStoreCompleteDto = components["schemas"]["CreateStoreCompleteDto"];
type UpdateStoreCompleteDto = components["schemas"]["UpdateStoreCompleteDto"];
type ReadStoreCompleteDto = components["schemas"]["ReadStoreCompleteDto"];
type CreateBoxByStoreDto = components["schemas"]["CreateBoxByStoreDto"];
type UpdateBulkBoxByStoreDto = components["schemas"]["UpdateBulkBoxByStoreDto"];
type CreateLedByStoreDto = components["schemas"]["CreateLedByStoreDto"];
type UpdateBulkLedByStoreDto = components["schemas"]["UpdateBulkLedByStoreDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

// What an expanded store brings along is handed to the store of each nested resource
const EXPAND_HANDLERS: Record<string, (idStore: string, data: ReadExtendedStoreDto) => void> = {
	boxs: (idStore, data) => useStoresBoxsStore().hydrateBoxs({ id_store: idStore }, data.boxs ?? []),
	leds: (idStore, data) => useStoresLedsStore().hydrateLeds({ id_store: idStore }, data.leds ?? []),
	stores_tags: (idStore, data) => useStoresTagsStore().hydrateStoreTags({ id_store: idStore }, data.stores_tags ?? []),
};

function hydrateStore(idStore: string, storeData: ReadExtendedStoreDto, expand: string[] = []) {
	const scope = { id_store: idStore };
	useStoresBoxsStore().hydrateBoxs(scope, undefined, storeData.boxs_count);
	useStoresLedsStore().hydrateLeds(scope, undefined, storeData.leds_count);
	useStoresTagsStore().hydrateStoreTags(scope, undefined, storeData.stores_tags_count);
	for (const key of expand) {
		EXPAND_HANDLERS[key]?.(idStore, storeData);
	}
}

const storeResource = createResource<{ readExtended: ReadExtendedStoreDto, readBasic: ReadStoreDto, create: CreateStoreDto, update: UpdateStoreDto }>({
	compositeKey: ["id_store"],
	generatedField: "id_store",
	path: [{ keys: [], url: () => "/store" }],
	entityPath: [{ keys: ["id_store"], url: ({ id_store }) => `/store/${id_store}` }],
	stateKey: "stores",
	countKey: "storesTotalCount",
	loadingKey: "storesLoading",
	editionKey: "storeEdition",
	onHydrate: (store: StoreGeneric, entity: ReadExtendedStoreDto, expand) => {
		if (entity.id_store === undefined) {
			return;
		}
		hydrateStore(String(entity.id_store), entity, expand);
	},
});

// The leds and boxes of a store are edited as a whole, from copies of the saved ones
function seedEditionFromSaved(idStore: string) {
	const ledsStore = useStoresLedsStore();
	const boxsStore = useStoresBoxsStore();
	const ledEdition = ledsStore.ledEditionByStore(idStore);
	const boxEdition = boxsStore.boxEditionByStore(idStore);
	for (const [key, led] of Object.entries(ledsStore.ledsByStore(idStore))) {
		ledEdition[key] = { ...led };
	}
	for (const [key, box] of Object.entries(boxsStore.boxsByStore(idStore))) {
		boxEdition[key] = { ...box };
	}
}

function clearNestedEdition(idStore: string) {
	const scope = { id_store: idStore };
	useStoresLedsStore().clearLedEdition(scope);
	useStoresBoxsStore().clearBoxEdition(scope);
	useStoresTagsStore().clearStoreTagEdition(scope);
}

export const useStoresStore = defineStore("stores", {
	state: () => ({
		storesLoading: false,
		storesTotalCount: 0,
		stores: {} as Record<string, ReadStoreDto>,
		storeEdition: {} as Record<string, Edition<{ create: CreateStoreDto, update: UpdateStoreDto }>>,
	}),
	actions: {
		getStoreByInterval: storeResource.single.getByInterval,
		getStoreById: storeResource.single.getById,
		createStore: storeResource.single.create,
		getAvailableNewStoreId: storeResource.single.getAvailableNewId,
		updateStore: storeResource.single.update,
		deleteStore: storeResource.single.remove,
		async createStoreComplete(params: CreateStoreCompleteDto) {
			const res = await fetchWrapper.post<ReadStoreCompleteDto>({
				url: `${baseUrl}/store/complete`,
				useToken: "access",
				body: params,
			});
			const idStore = String(res.store.id_store);
			this.stores[idStore] = res.store;
			return idStore;
		},
		async updateStoreComplete(id: string, params: UpdateStoreCompleteDto) {
			const res = await fetchWrapper.put<ReadStoreCompleteDto>({
				url: `${baseUrl}/store/${id}/complete`,
				useToken: "access",
				body: params,
			});
			this.stores[id] = res.store;
		},
		loadToEdition(id: string, preset: string | null = null) {
			clearNestedEdition(id);
			if (!isNewId(id) && this.stores[id]) {
				this.storeEdition[id] = {
					loading: false,
					name_store: this.stores[id].name_store ?? undefined,
					mqtt_name_store: this.stores[id].mqtt_name_store ?? undefined,
					xlength_store: this.stores[id].xlength_store,
					ylength_store: this.stores[id].ylength_store,
					position_mode_store: this.stores[id].position_mode_store,
					id_zone: this.stores[id].id_zone ?? 0,
					xmin_store: this.stores[id].xmin_store,
					ymin_store: this.stores[id].ymin_store,
					xmax_store: this.stores[id].xmax_store,
					ymax_store: this.stores[id].ymax_store,
				};
				seedEditionFromSaved(id);
			} else {
				this.storeEdition[id] = {
					loading: false,
					position_mode_store: StorePositionMode.Grid,
					id_zone: 0,
				};
				storeResource.single.loadEditionPreset.call(this, id, preset);
			}
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.storeEdition[id]) {
				this.storeEdition[id] = {};
			}
			this.storeEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.storeEdition[id];
			clearNestedEdition(id);
		},
		async saveAllChanges(id: string) {
			let realId = id;
			const leds = Object.values(useStoresLedsStore().ledEditionByStore(id));
			const boxs = Object.values(useStoresBoxsStore().boxEditionByStore(id));
			const storesTagsStore = useStoresTagsStore();
			if (isNewId(id)) {
				realId = await this.createStoreComplete({
					store: this.storeEdition[id] as CreateStoreDto,
					leds: leds as CreateLedByStoreDto[],
					boxs: boxs as CreateBoxByStoreDto[],
				});
				storesTagsStore.copyStoreTagByKeyPart("id_store", id, realId);
			} else {
				await this.updateStoreComplete(id, {
					store: this.storeEdition[id] as UpdateStoreDto,
					leds: leds as UpdateBulkLedByStoreDto[],
					boxs: boxs as UpdateBulkBoxByStoreDto[],
				});
			}
			await storesTagsStore.pushStoreTagChange({ id_store: realId });
			return realId;
		},

		async showBoxById(idStore: string, id: string, params: ShowParams) {
			await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${id}/show?${new URLSearchParams(
					Object.entries(params)
						.filter(([, v]) => v !== undefined && v !== null)
						.map(([k, v]) => [k, String(v)]),
				)}`,
				useToken: "access",
			});
		},
		async showLedById(idStore: string, id: string, params: ShowParams) {
			await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/led/${id}/show?${new URLSearchParams(
					Object.entries(params)
						.filter(([, v]) => v !== undefined && v !== null)
						.map(([k, v]) => [k, String(v)]),
				)}`,
				useToken: "access",
			});
		},
	},
});

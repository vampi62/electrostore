import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";
import { isNewId } from "@/utils";
import { StorePositionMode } from "@/enums";

import { useTagsStore, useItemsStore, useEquipementsStore } from "@/stores";

import type { components } from "@/types/api";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";
type ReadStoreDto = components["schemas"]["ReadStoreDto"];
type ReadBoxDto = components["schemas"]["ReadBoxDto"];
type ReadLedDto = components["schemas"]["ReadLedDto"];
type ReadStoreTagDto = components["schemas"]["ReadStoreTagDto"];
type ReadItemBoxDto = components["schemas"]["ReadItemBoxDto"];
type ReadBoxTagDto = components["schemas"]["ReadBoxTagDto"];
type ReadEquipementBoxDto = components["schemas"]["ReadEquipementBoxDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS_STORE: Record<string, (store: any, idStore: string, data: any) => void> = {
	boxs: (store, idStore, data) => {
		store.boxs[idStore] = {};
		for (const box of data.boxs) {
			store.boxs[idStore][box.id_box] = box;
		}
	},
	leds: (store, idStore, data) => {
		store.leds[idStore] = {};
		for (const led of data.leds) {
			store.leds[idStore][led.id_led] = led;
		}
	},
	stores_tags: (store, idStore, data) => {
		store.storeTags[idStore] = {};
		for (const tag of data.stores_tags) {
			store.storeTags[idStore][tag.id_tag] = tag;
		}
	},
};
const EXPAND_HANDLERS_BOX: Record<string, (store: any, idBox: string, data: any) => void> = {
	item_boxs: (store, idBox, data) => {
		store.boxItems[idBox] = {};
		for (const item of data.item_boxs) {
			store.boxItems[idBox][item.id_item] = item;
		}
	},
	box_tags: (store, idBox, data) => {
		store.boxTags[idBox] = {};
		for (const tag of data.box_tags) {
			store.boxTags[idBox][tag.id_tag] = tag;
		}
	},
};

function hydrateStore(store: any, idStore: string, storeData: any, expand: string[] = []) {
	store.boxsTotalCount[idStore] = storeData.boxs_count;
	store.ledsTotalCount[idStore] = storeData.leds_count;
	store.storeTagsTotalCount[idStore] = storeData.stores_tags_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS_STORE[key]) {
			EXPAND_HANDLERS_STORE[key](store, idStore, storeData);
		}
	}
}

function hydrateBox(store: any, idStore: string, idBox: string, boxData: any, expand: string[] = []) {
	store.boxs[idStore][idBox] = boxData;
	store.boxItemsTotalCount[idBox] = boxData.item_boxs_count;
	store.boxTagsTotalCount[idBox] = boxData.box_tags_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS_BOX[key]) {
			EXPAND_HANDLERS_BOX[key](store, idBox, boxData);
		}
	}
}

const storeResource = createMainResource({
	path: () => "/store",
	idField: "id_store",
	stateKey: "stores",
	countKey: "storesTotalCount",
	loadingKey: "storesLoading",
	editionKey: "storeEdition",
	onHydrate: (store, entity, expand) => {
		hydrateStore(store, entity.id_store, entity, expand);
	},
});

const boxResource = createNestedResource({
	path: (idStore) => `/store/${idStore}/box`,
	idField: "id_box",
	stateKey: "boxs",
	countKey: "boxsTotalCount",
	loadingKey: "boxsLoading",
	editionKey: "boxEdition",
	readyKey: "boxReady",
	onHydrate: (store, entity, expand) => {
		hydrateBox(store, entity.id_store, entity.id_box, entity, expand);
	},
});
const ledResource = createNestedResource({
	path: (idStore) => `/store/${idStore}/led`,
	idField: "id_led",
	stateKey: "leds",
	countKey: "ledsTotalCount",
	loadingKey: "ledsLoading",
	editionKey: "ledEdition",
	readyKey: "ledReady",
});
const storeTagResource = createNestedResource({
	path: (idStore) => `/store/${idStore}/tag`,
	idField: "id_tag",
	stateKey: "storeTags",
	countKey: "storeTagsTotalCount",
	loadingKey: "storeTagsLoading",
	editionKey: "storeTagEdition",
	readyKey: "storeTagReady",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("tag")) {
			const tagsStore = useTagsStore();
			tagsStore.tags[entity.id_tag] = entity.tag;
		}
	},
});

export const useStoresStore = defineStore("stores",{
	state: () => ({
		storesLoading: false,
		storesTotalCount: 0,
		stores: {} as Record<string, ReadStoreDto>,
		storeEdition: {} as Record<string, any>,

		boxsLoading: false,
		boxsTotalCount: {} as Record<string, number>,
		boxs: {} as Record<string, Record<string, ReadBoxDto>>,
		boxEdition: {} as Record<string, any>,
		boxReady: {} as Record<string, any>,

		ledsLoading: false,
		ledsTotalCount: {} as Record<string, number>,
		leds: {} as Record<string, Record<string, ReadLedDto>>,
		ledEdition: {} as Record<string, any>,
		ledReady: {} as Record<string, any>,

		storeTagsLoading: false,
		storeTagsTotalCount: {} as Record<string, number>,
		storeTags: {} as Record<string, ReadStoreTagDto>,
		storeTagEdition: {} as Record<string, any>,
		storeTagReady: {} as Record<string, any>,

		boxItemsLoading: false,
		boxItemsTotalCount: {} as Record<string, number>,
		boxItems: {} as Record<string, Record<string, ReadItemBoxDto>>,
		boxItemEdition: {} as Record<string, any>,
		boxItemReady: {} as Record<string, any>,

		boxEquipementsLoading: false,
		boxEquipementsTotalCount: {} as Record<string, number>,
		boxEquipements: {} as Record<string, Record<string, ReadEquipementBoxDto>>,

		boxTagsLoading: false,
		boxTagsTotalCount: {} as Record<string, number>,
		boxTags: {} as Record<string, Record<string, ReadBoxTagDto>>,
		boxTagEdition: {} as Record<string, any>,
		boxTagReady: {} as Record<string, any>,
	}),
	actions: {
		getStoreByList: storeResource.getByList,
		getStoreByInterval: storeResource.getByInterval,
		getStoreById: storeResource.getById,
		createStore: storeResource.create,
		getAvailableNewStoreId: storeResource.getAvailableNewId,
		updateStore: storeResource.update,
		deleteStore: storeResource.remove,
		async createStoreComplete(params: { store: any; leds: any[]; boxs: any[] }) {
			const store = await fetchWrapper.post({
				url: `${baseUrl}/store/complete`,
				useToken: "access",
				body: params,
			});
			this.stores[store.store.id_store] = store.store;
			return store.store.id_store;
		},
		async updateStoreComplete(id: string, params: { store: any; leds: any[]; boxs: any[] }) {
			this.stores[id] = await fetchWrapper.put({
				url: `${baseUrl}/store/${id}/complete`,
				useToken: "access",
				body: params,
			});
		},
		loadToEdition(id: string, preset = null) {
			if (!isNewId(id) && this.stores[id]) {
				this.storeEdition[id] = {
					loading: false,
					id_store: this.stores[id].id_store,
					name_store: this.stores[id].name_store,
					mqtt_name_store: this.stores[id].mqtt_name_store,
					xlength_store: this.stores[id].xlength_store,
					ylength_store: this.stores[id].ylength_store,
					position_mode_store: this.stores[id].position_mode_store,
					is_mqtt_connected_store: this.stores[id].is_mqtt_connected_store,
					mqtt_last_seen_store: this.stores[id].mqtt_last_seen_store,
					id_zone: this.stores[id].id_zone ?? 0,
					xmin_store: this.stores[id].xmin_store,
					ymin_store: this.stores[id].ymin_store,
					xmax_store: this.stores[id].xmax_store,
					ymax_store: this.stores[id].ymax_store,
				};
				this.ledEdition[id] = { ...this.leds[id] };
				this.ledReady[id] = {};
				this.boxEdition[id] = { ...this.boxs[id] };
				this.boxReady[id] = {};
				this.storeTagEdition[id] = { ...this.storeTags[id] };
				this.storeTagReady[id] = {};
			} else {
				this.storeEdition[id] = {
					loading: false,
					position_mode_store: StorePositionMode.Grid,
					id_zone: 0,
				};
				storeResource.loadEditionPreset.call(this, id, preset);
				this.ledEdition[id] = {};
				this.ledReady[id] = {};
				this.boxEdition[id] = {};
				this.boxReady[id] = {};
				this.storeTagEdition[id] = {};
				this.storeTagReady[id] = {};
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
			delete this.ledEdition[id];
			delete this.ledReady[id];
			delete this.boxEdition[id];
			delete this.boxReady[id];
			delete this.storeTagEdition[id];
			delete this.storeTagReady[id];
		},
		async saveAllChanges(id: string) {
			let realId = id;
			const payload = {
				store: this.storeEdition[id],
				leds: Object.values(this.ledEdition[id] ?? {}),
				boxs: Object.values(this.boxEdition[id] ?? {}),
			};
			if (isNewId(id)) {
				realId = await this.createStoreComplete(payload);
				this.copyTagStoreAllId(id, realId);
			} else {
				await this.updateStoreComplete(id, payload);
			}
			await this.pushTagStoreChange(realId);
			return realId;
		},

		getBoxByInterval: boxResource.getByInterval,
		getBoxById: boxResource.getById,
		createBox: boxResource.create,
		updateBox: boxResource.update,
		deleteBox: boxResource.remove,
		createBoxBulk: boxResource.createBulk,
		updateBoxBulk: boxResource.updateBulk,
		deleteBoxBulk: boxResource.removeBulk,
		getAvailableNewBoxId: boxResource.getAvailableNewId,
		valideBoxEditionById: boxResource.valideEditionById,
		copyBoxPerId: boxResource.copyPerId,
		copyBoxAllId: boxResource.copyAllId,
		pushBoxChange: boxResource.pushChange,
		async showBoxById(idStore: string, id: string, params: any) {
			await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${id}/show`,
				useToken: "access",
				body: params,
			});
		},

		getLedByInterval: ledResource.getByInterval,
		getLedById: ledResource.getById,
		createLed: ledResource.create,
		updateLed: ledResource.update,
		deleteLed: ledResource.remove,
		createLedBulk: ledResource.createBulk,
		updateLedBulk: ledResource.updateBulk,
		deleteLedBulk: ledResource.removeBulk,
		getAvailableNewLedId: ledResource.getAvailableNewId,
		valideLedEditionById: ledResource.valideEditionById,
		copyLedPerId: ledResource.copyPerId,
		copyLedAllId: ledResource.copyAllId,
		pushLedChange: ledResource.pushChange,
		async showLedById(idStore: string, id: string, params: any) {
			await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/led/${id}/show`,
				useToken: "access",
				body: params,
			});
		},

		getTagStoreByInterval: storeTagResource.getByInterval,
		getTagStoreById: storeTagResource.getById,
		createTagStore: storeTagResource.create,
		deleteTagStore: storeTagResource.remove,
		createTagStoreBulk: storeTagResource.createBulk,
		deleteTagStoreBulk: storeTagResource.removeBulk,
		getAvailableNewTagStoreId: storeTagResource.getAvailableNewId,
		valideTagStoreEditionById: storeTagResource.valideEditionById,
		copyTagStorePerId: storeTagResource.copyPerId,
		copyTagStoreAllId: storeTagResource.copyAllId,
		pushTagStoreChange: storeTagResource.pushChange,

		async getBoxItemByInterval(idStore: string, idBox: string, limit = 100, offset = 0, expand: string[] = [], filter: RSQLFilter[] = [], sort: RSQLSort = {}, clear = false) {
			if (!this.boxItems[idBox] || clear) {
				this.boxItems[idBox] = {};
			}
			this.boxItemsLoading = true;
			const itemsStore = useItemsStore();
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newItemList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item?${paramString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				this.boxItems[idBox][item.id_item] = item;
				if (expand.includes("item")) {
					itemsStore.items[item.id_item] = item.item;
				}
			}
			this.boxItemsTotalCount[idBox] = newItemList["pagination"]?.["total"] || 0;
			this.boxItemsLoading = false;
			return [newItemList["pagination"]?.["nextOffset"] || 0, newItemList["pagination"]?.["hasMore"] || false];
		},
		async getBoxItemById(idStore: string, idBox: string, id: string, expand: string[] = []) {
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			if (!this.boxItems[idBox][id]) {
				this.boxItems[idBox][id] = {};
			}
			const itemsStore = useItemsStore();
			const paramString = buildQuery({ expand });
			const boxItem = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item/${id}?${paramString}`,
				useToken: "access",
			});
			this.boxItems[idBox][id] = boxItem;
			if (expand.includes("item")) {
				itemsStore.items[id] = boxItem.item;
			}
		},
		async createBoxItem(idStore: string, idBox: string, params: any) {
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			const boxItem = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item`,
				useToken: "access",
				body: params,
			});
			this.boxItems[idBox][boxItem.id_item] = boxItem;
		},
		async updateBoxItem(idStore: string, idBox: string, id: string, params: any) {
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			this.boxItems[idBox][id] = await fetchWrapper.put({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteBoxItem(idStore: string, idBox: string, id: string) {
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item/${id}`,
				useToken: "access",
			});
			delete this.boxItems[idBox][id];
		},

		async getBoxEquipementByInterval(idStore: string, idBox: string, limit = 100, offset = 0, expand: string[] = [], filter: RSQLFilter[] = [], sort: RSQLSort = {}, clear = false) {
			if (!this.boxEquipements[idBox] || clear) {
				this.boxEquipements[idBox] = {};
			}
			this.boxEquipementsLoading = true;
			const equipementsStore = useEquipementsStore();
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newEquipementList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/equipement?${paramString}`,
				useToken: "access",
			});
			for (const equipement of newEquipementList["data"]) {
				this.boxEquipements[idBox][equipement.id_equipement] = equipement;
				if (expand.includes("equipement")) {
					equipementsStore.equipements[equipement.id_equipement] = equipement.equipement;
				}
			}
			this.boxEquipementsTotalCount[idBox] = newEquipementList["pagination"]?.["total"] || 0;
			this.boxEquipementsLoading = false;
			return [newEquipementList["pagination"]?.["nextOffset"] || 0, newEquipementList["pagination"]?.["hasMore"] || false];
		},
		async createBoxEquipement(idStore: string, idBox: string, params: any) {
			if (!this.boxEquipements[idBox]) {
				this.boxEquipements[idBox] = {};
			}
			const boxEquipement = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/equipement`,
				useToken: "access",
				body: params,
			});
			this.boxEquipements[idBox][boxEquipement.id_equipement] = boxEquipement;
		},
		async deleteBoxEquipement(idStore: string, idBox: string, id: string) {
			if (!this.boxEquipements[idBox]) {
				this.boxEquipements[idBox] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/equipement/${id}`,
				useToken: "access",
			});
			delete this.boxEquipements[idBox][id];
		},

		async getBoxTagByInterval(idStore: string, idBox: string, limit = 100, offset = 0, expand: string[] = [], filter: RSQLFilter[] = [], sort: RSQLSort = {}, clear = false) {
			if (!this.boxTags[idBox] || clear) {
				this.boxTags[idBox] = {};
			}
			this.boxTagsLoading = true;
			const tagsStore = useTagsStore();
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newTagList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag?${paramString}`,
				useToken: "access",
			});
			for (const tag of newTagList["data"]) {
				this.boxTags[idBox][tag.id_tag] = tag;
				if (expand.includes("tag")) {
					tagsStore.tags[tag.id_tag] = tag.tag;
				}
			}
			this.boxTagsTotalCount[idBox] = newTagList["pagination"]?.["total"] || 0;
			this.boxTagsLoading = false;
			return [newTagList["pagination"]?.["nextOffset"] || 0, newTagList["pagination"]?.["hasMore"] || false];
		},
		async getBoxTagById(idStore: string, idBox: string, id: string, expand: string[] = []) {
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			if (!this.boxTags[idBox][id]) {
				this.boxTags[idBox][id] = {};
			}
			const tagsStore = useTagsStore();
			const paramString = buildQuery({ expand });
			const boxTag = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/${id}?${paramString}`,
				useToken: "access",
			});
			this.boxTags[idBox][id] = boxTag;
			if (expand.includes("tag")) {
				tagsStore.tags[id] = boxTag.tag;
			}
		},
		async createBoxTag(idStore: string, idBox: string, params: any) {
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			const boxTag = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag`,
				useToken: "access",
				body: params,
			});
			this.boxTags[idBox][boxTag.id_tag] = boxTag;
		},
		async deleteBoxTag(idStore: string, idBox: string, id: string) {
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/${id}`,
				useToken: "access",
			});
			delete this.boxTags[idBox][id];
		},
		async createBoxTagBulk(idStore: string, idBox: string, params: any) {
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			const boxTagBulk = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of boxTagBulk["valide"]) {
				this.boxTags[idBox][tag.id_tag] = tag;
			}
		},
		async deleteBoxTagBulk(idStore: string, idBox: string, params: any) {
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			const boxTagBulk = await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of boxTagBulk["valide"]) {
				delete this.boxTags[idBox][tag.id_tag];
			}
		},
	},
});

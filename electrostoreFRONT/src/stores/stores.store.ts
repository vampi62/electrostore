import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";
import { isNewId } from "@/utils";
import { StorePositionMode } from "@/enums";

import { useTagsStore, useItemsStore, useEquipementsStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Ready, Edition } from "@/types/resource";
import type { PaginatedResponseDto, ReadBulkDto } from "@/types/fetch";
import type { components } from "@/types/api";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";
type ReadStoreDto = components["schemas"]["ReadStoreDto"];
type ReadExtendedStoreDto = components["schemas"]["ReadExtendedStoreDto"];
type CreateStoreDto = components["schemas"]["CreateStoreDto"];
type UpdateStoreDto = components["schemas"]["UpdateStoreDto"];
type CreateStoreCompleteDto = components["schemas"]["CreateStoreCompleteDto"];
type UpdateStoreCompleteDto = components["schemas"]["UpdateStoreCompleteDto"];
type ReadStoreCompleteDto = components["schemas"]["ReadStoreCompleteDto"];

type ReadBoxDto = components["schemas"]["ReadBoxDto"];
type ReadExtendedBoxDto = components["schemas"]["ReadExtendedBoxDto"];
type CreateBoxByStoreDto = components["schemas"]["CreateBoxByStoreDto"];
type UpdateBoxByStoreDto = components["schemas"]["UpdateBoxByStoreDto"];
type UpdateBulkBoxByStoreDto = components["schemas"]["UpdateBulkBoxByStoreDto"];

type ReadLedDto = components["schemas"]["ReadLedDto"];
type CreateLedByStoreDto = components["schemas"]["CreateLedByStoreDto"];
type UpdateLedByStoreDto = components["schemas"]["UpdateLedByStoreDto"];
type UpdateBulkLedByStoreDto = components["schemas"]["UpdateBulkLedByStoreDto"];

type ReadStoreTagDto = components["schemas"]["ReadStoreTagDto"];
type ReadExtendedStoreTagDto = components["schemas"]["ReadExtendedStoreTagDto"];
type CreateStoreTagByStoreDto = components["schemas"]["CreateStoreTagByStoreDto"];

type ReadItemBoxDto = components["schemas"]["ReadItemBoxDto"];
type ReadExtendedItemBoxDto = components["schemas"]["ReadExtendedItemBoxDto"];
type CreateItemBoxByBoxDto = components["schemas"]["CreateItemBoxByBoxDto"];
type UpdateItemBoxDto = components["schemas"]["UpdateItemBoxDto"];

type ReadBoxTagDto = components["schemas"]["ReadBoxTagDto"];
type ReadExtendedBoxTagDto = components["schemas"]["ReadExtendedBoxTagDto"];
type CreateBoxTagByBoxDto = components["schemas"]["CreateBoxTagByBoxDto"];

type ReadEquipementBoxDto = components["schemas"]["ReadEquipementBoxDto"];
type ReadExtendedEquipementBoxDto = components["schemas"]["ReadExtendedEquipementBoxDto"];
type CreateEquipementBoxByBoxDto = components["schemas"]["CreateEquipementBoxByBoxDto"];

// parameters of the mqtt show command of a led or a box (colors, duration, animation)
type ShowParams = Record<string, number>;

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS_STORE: Record<string, (store: StoreGeneric, idStore: string, data: ReadExtendedStoreDto) => void> = {
	boxs: (store, idStore, data) => {
		store.boxs[idStore] = {};
		for (const box of data.boxs ?? []) {
			store.boxs[idStore][box.id_box] = box;
		}
	},
	leds: (store, idStore, data) => {
		store.leds[idStore] = {};
		for (const led of data.leds ?? []) {
			store.leds[idStore][led.id_led] = led;
		}
	},
	stores_tags: (store, idStore, data) => {
		store.storeTags[idStore] = {};
		for (const tag of data.stores_tags ?? []) {
			store.storeTags[idStore][tag.id_tag] = tag;
		}
	},
};
const EXPAND_HANDLERS_BOX: Record<string, (store: StoreGeneric, idBox: string, data: ReadExtendedBoxDto) => void> = {
	item_boxs: (store, idBox, data) => {
		store.boxItems[idBox] = {};
		for (const item of data.item_boxs ?? []) {
			store.boxItems[idBox][item.id_item] = item;
		}
	},
	box_tags: (store, idBox, data) => {
		store.boxTags[idBox] = {};
		for (const tag of data.box_tags ?? []) {
			store.boxTags[idBox][tag.id_tag] = tag;
		}
	},
};

function hydrateStore(store: StoreGeneric, idStore: string, storeData: ReadExtendedStoreDto, expand: string[] = []) {
	store.boxsTotalCount[idStore] = storeData.boxs_count;
	store.ledsTotalCount[idStore] = storeData.leds_count;
	store.storeTagsTotalCount[idStore] = storeData.stores_tags_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS_STORE[key]) {
			EXPAND_HANDLERS_STORE[key](store, idStore, storeData);
		}
	}
}

function hydrateBox(store: StoreGeneric, idStore: string, idBox: string, boxData: ReadExtendedBoxDto, expand: string[] = []) {
	store.boxs[idStore][idBox] = boxData;
	store.boxItemsTotalCount[idBox] = boxData.item_boxs_count;
	store.boxTagsTotalCount[idBox] = boxData.box_tags_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS_BOX[key]) {
			EXPAND_HANDLERS_BOX[key](store, idBox, boxData);
		}
	}
}

const storeResource = createMainResource<{ readExtended: ReadExtendedStoreDto, readBasic: ReadStoreDto, create: CreateStoreDto, update: UpdateStoreDto }>({
	path: () => "/store",
	idField: "id_store",
	stateKey: "stores",
	countKey: "storesTotalCount",
	loadingKey: "storesLoading",
	editionKey: "storeEdition",
	onHydrate: (store, entity: ReadExtendedStoreDto, expand) => {
		if (entity.id_store === undefined) {
			return;
		}
		hydrateStore(store, String(entity.id_store), entity, expand);
	},
});

const boxResource = createNestedResource<{ readExtended: ReadExtendedBoxDto, readBasic: ReadBoxDto, create: CreateBoxByStoreDto, update: UpdateBoxByStoreDto, createBulk: CreateBoxByStoreDto[], updateBulk: UpdateBulkBoxByStoreDto[], deleteBulk: number[] }>({
	path: (idStore) => `/store/${idStore}/box`,
	idField: "id_box",
	stateKey: "boxs",
	countKey: "boxsTotalCount",
	loadingKey: "boxsLoading",
	editionKey: "boxEdition",
	readyKey: "boxReady",
	onHydrate: (store, entity: ReadExtendedBoxDto, expand) => {
		if (entity.id_store === undefined || entity.id_box === undefined) {
			return;
		}
		hydrateBox(store, String(entity.id_store), String(entity.id_box), entity, expand);
	},
});
const ledResource = createNestedResource<{ readBasic: ReadLedDto, create: CreateLedByStoreDto, update: UpdateLedByStoreDto, createBulk: CreateLedByStoreDto[], updateBulk: UpdateBulkLedByStoreDto[], deleteBulk: number[] }>({
	path: (idStore) => `/store/${idStore}/led`,
	idField: "id_led",
	stateKey: "leds",
	countKey: "ledsTotalCount",
	loadingKey: "ledsLoading",
	editionKey: "ledEdition",
	readyKey: "ledReady",
});
const storeTagResource = createNestedResource<{ readExtended: ReadExtendedStoreTagDto, readBasic: ReadStoreTagDto, create: CreateStoreTagByStoreDto, createBulk: CreateStoreTagByStoreDto[], deleteBulk: number[] }>({
	path: (idStore) => `/store/${idStore}/tag`,
	idField: "id_tag",
	stateKey: "storeTags",
	countKey: "storeTagsTotalCount",
	loadingKey: "storeTagsLoading",
	editionKey: "storeTagEdition",
	readyKey: "storeTagReady",
	onHydrate: (store, entity: ReadExtendedStoreTagDto, expand) => {
		if (expand.includes("tag") && entity.tag && entity.id_tag) {
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
		storeEdition: {} as Record<string, Edition<{ create: CreateStoreDto, update: UpdateStoreDto }>>,

		boxsLoading: false,
		boxsTotalCount: {} as Record<string, number>,
		boxs: {} as Record<string, Record<string, ReadBoxDto>>,
		boxEdition: {} as Record<string, Record<string, Edition<{ create: CreateBoxByStoreDto, update: UpdateBulkBoxByStoreDto }>>>,
		boxReady: {} as Record<string, Record<string, Ready<{ create: CreateBoxByStoreDto, update: UpdateBoxByStoreDto }>>>,

		ledsLoading: false,
		ledsTotalCount: {} as Record<string, number>,
		leds: {} as Record<string, Record<string, ReadLedDto>>,
		ledEdition: {} as Record<string, Record<string, Edition<{ create: CreateLedByStoreDto, update: UpdateBulkLedByStoreDto }>>>,
		ledReady: {} as Record<string, Record<string, Ready<{ create: CreateLedByStoreDto, update: UpdateLedByStoreDto }>>>,

		storeTagsLoading: false,
		storeTagsTotalCount: {} as Record<string, number>,
		storeTags: {} as Record<string, Record<string, ReadStoreTagDto>>,
		storeTagEdition: {} as Record<string, Record<string, Edition<{ create: CreateStoreTagByStoreDto }>>>,
		storeTagReady: {} as Record<string, Record<string, Ready<{ create: CreateStoreTagByStoreDto }>>>,

		boxItemsLoading: false,
		boxItemsTotalCount: {} as Record<string, number>,
		boxItems: {} as Record<string, Record<string, ReadItemBoxDto>>,
		boxItemEdition: {} as Record<string, Record<string, Edition<{ create: CreateItemBoxByBoxDto, update: UpdateItemBoxDto }>>>,
		boxItemReady: {} as Record<string, Record<string, Ready<{ create: CreateItemBoxByBoxDto, update: UpdateItemBoxDto }>>>,

		boxEquipementsLoading: false,
		boxEquipementsTotalCount: {} as Record<string, number>,
		boxEquipements: {} as Record<string, Record<string, ReadEquipementBoxDto>>,

		boxTagsLoading: false,
		boxTagsTotalCount: {} as Record<string, number>,
		boxTags: {} as Record<string, Record<string, ReadBoxTagDto>>,
		boxTagEdition: {} as Record<string, Record<string, Edition<{ create: CreateBoxTagByBoxDto }>>>,
		boxTagReady: {} as Record<string, Record<string, Ready<{ create: CreateBoxTagByBoxDto }>>>,
	}),
	actions: {
		getStoreByList: storeResource.getByList,
		getStoreByInterval: storeResource.getByInterval,
		getStoreById: storeResource.getById,
		createStore: storeResource.create,
		getAvailableNewStoreId: storeResource.getAvailableNewId,
		updateStore: storeResource.update,
		deleteStore: storeResource.remove,
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
			const leds = Object.values(this.ledEdition[id] ?? {});
			const boxs = Object.values(this.boxEdition[id] ?? {});
			if (isNewId(id)) {
				realId = await this.createStoreComplete({
					store: this.storeEdition[id] as CreateStoreDto,
					leds: leds as CreateLedByStoreDto[],
					boxs: boxs as CreateBoxByStoreDto[],
				});
				this.copyTagStoreAllId(id, realId);
			} else {
				await this.updateStoreComplete(id, {
					store: this.storeEdition[id] as UpdateStoreDto,
					leds: leds as UpdateBulkLedByStoreDto[],
					boxs: boxs as UpdateBulkBoxByStoreDto[],
				});
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
		async showBoxById(idStore: string, id: string, params: ShowParams) {
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
		async showLedById(idStore: string, id: string, params: ShowParams) {
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
			const res = await fetchWrapper.get<PaginatedResponseDto<ReadExtendedItemBoxDto>>({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item?${paramString}`,
				useToken: "access",
			});
			for (const item of res.data) {
				this.boxItems[idBox][String(item.id_item)] = item;
				if (expand.includes("item") && item.item && item.id_item) {
					itemsStore.items[item.id_item] = item.item;
				}
			}
			this.boxItemsTotalCount[idBox] = res.pagination?.total ?? 0;
			this.boxItemsLoading = false;
			return [res.pagination?.next_offset ?? 0, res.pagination?.has_more ?? false];
		},
		async getBoxItemById(idStore: string, idBox: string, id: string, expand: string[] = []) {
			this.boxItems[idBox] ??= {};
			const itemsStore = useItemsStore();
			const paramString = buildQuery({ expand });
			const boxItem = await fetchWrapper.get<ReadExtendedItemBoxDto>({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item/${id}?${paramString}`,
				useToken: "access",
			});
			this.boxItems[idBox][id] = boxItem;
			if (expand.includes("item") && boxItem.item) {
				itemsStore.items[id] = boxItem.item;
			}
		},
		async createBoxItem(idStore: string, idBox: string, params: CreateItemBoxByBoxDto) {
			this.boxItems[idBox] ??= {};
			const boxItem = await fetchWrapper.post<ReadItemBoxDto>({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item`,
				useToken: "access",
				body: params,
			});
			this.boxItems[idBox][String(boxItem.id_item)] = boxItem;
		},
		async updateBoxItem(idStore: string, idBox: string, id: string, params: UpdateItemBoxDto) {
			this.boxItems[idBox] ??= {};
			this.boxItems[idBox][id] = await fetchWrapper.put<ReadItemBoxDto>({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteBoxItem(idStore: string, idBox: string, id: string) {
			this.boxItems[idBox] ??= {};
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
			const res = await fetchWrapper.get<PaginatedResponseDto<ReadExtendedEquipementBoxDto>>({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/equipement?${paramString}`,
				useToken: "access",
			});
			for (const equipement of res.data) {
				this.boxEquipements[idBox][String(equipement.id_equipement)] = equipement;
				if (expand.includes("equipement") && equipement.equipement && equipement.id_equipement) {
					equipementsStore.equipements[equipement.id_equipement] = equipement.equipement;
				}
			}
			this.boxEquipementsTotalCount[idBox] = res.pagination?.total ?? 0;
			this.boxEquipementsLoading = false;
			return [res.pagination?.next_offset ?? 0, res.pagination?.has_more ?? false];
		},
		async createBoxEquipement(idStore: string, idBox: string, params: CreateEquipementBoxByBoxDto) {
			this.boxEquipements[idBox] ??= {};
			const boxEquipement = await fetchWrapper.post<ReadEquipementBoxDto>({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/equipement`,
				useToken: "access",
				body: params,
			});
			this.boxEquipements[idBox][String(boxEquipement.id_equipement)] = boxEquipement;
		},
		async deleteBoxEquipement(idStore: string, idBox: string, id: string) {
			this.boxEquipements[idBox] ??= {};
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
			const res = await fetchWrapper.get<PaginatedResponseDto<ReadExtendedBoxTagDto>>({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag?${paramString}`,
				useToken: "access",
			});
			for (const tag of res.data) {
				this.boxTags[idBox][String(tag.id_tag)] = tag;
				if (expand.includes("tag") && tag.tag && tag.id_tag) {
					tagsStore.tags[tag.id_tag] = tag.tag;
				}
			}
			this.boxTagsTotalCount[idBox] = res.pagination?.total ?? 0;
			this.boxTagsLoading = false;
			return [res.pagination?.next_offset ?? 0, res.pagination?.has_more ?? false];
		},
		async getBoxTagById(idStore: string, idBox: string, id: string, expand: string[] = []) {
			this.boxTags[idBox] ??= {};
			const tagsStore = useTagsStore();
			const paramString = buildQuery({ expand });
			const boxTag = await fetchWrapper.get<ReadExtendedBoxTagDto>({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/${id}?${paramString}`,
				useToken: "access",
			});
			this.boxTags[idBox][id] = boxTag;
			if (expand.includes("tag") && boxTag.tag) {
				tagsStore.tags[id] = boxTag.tag;
			}
		},
		async createBoxTag(idStore: string, idBox: string, params: CreateBoxTagByBoxDto) {
			this.boxTags[idBox] ??= {};
			const boxTag = await fetchWrapper.post<ReadBoxTagDto>({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag`,
				useToken: "access",
				body: params,
			});
			this.boxTags[idBox][String(boxTag.id_tag)] = boxTag;
		},
		async deleteBoxTag(idStore: string, idBox: string, id: string) {
			this.boxTags[idBox] ??= {};
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/${id}`,
				useToken: "access",
			});
			delete this.boxTags[idBox][id];
		},
		async createBoxTagBulk(idStore: string, idBox: string, params: CreateBoxTagByBoxDto[]) {
			this.boxTags[idBox] ??= {};
			const boxTagBulk = await fetchWrapper.post<ReadBulkDto<ReadBoxTagDto>>({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of boxTagBulk.valide) {
				this.boxTags[idBox][String(tag.id_tag)] = tag;
			}
		},
		async deleteBoxTagBulk(idStore: string, idBox: string, params: number[]) {
			this.boxTags[idBox] ??= {};
			const boxTagBulk = await fetchWrapper.delete<ReadBulkDto<ReadBoxTagDto>>({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of boxTagBulk.valide) {
				delete this.boxTags[idBox][String(tag.id_tag)];
			}
		},
	},
});

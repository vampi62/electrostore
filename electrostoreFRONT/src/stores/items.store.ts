import { defineStore } from "pinia";

import { fetchWrapper, createMainResource, createNestedResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useTagsStore, useStoresStore, useCommandsStore, useProjectsStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Ready, Edition } from "@/types/resource";
import type { components, paths } from "@/types/api";
type ReadItemDto = components["schemas"]["ReadItemDto"];
type ReadExtendedItemDto = components["schemas"]["ReadExtendedItemDto"];
type CreateItemDto = NonNullable<paths["/api/item"]["post"]["requestBody"]>["content"]["multipart/form-data"];
type UpdateItemDto = NonNullable<paths["/api/item/{id_item}"]["put"]["requestBody"]>["content"]["multipart/form-data"];

type ReadItemDocumentDto = components["schemas"]["ReadItemDocumentDto"];
type CreateItemDocumentByItemDto = NonNullable<paths["/api/item/{id_item}/document"]["post"]["requestBody"]>["content"]["multipart/form-data"];
type UpdateItemDocumentDto = components["schemas"]["UpdateItemDocumentDto"];

type ReadItemBoxDto = components["schemas"]["ReadItemBoxDto"];
type ReadExtendedItemBoxDto = components["schemas"]["ReadExtendedItemBoxDto"];
type CreateItemBoxByItemDto = components["schemas"]["CreateItemBoxByItemDto"];
type UpdateItemBoxDto = components["schemas"]["UpdateItemBoxDto"];

type ReadItemTagDto = components["schemas"]["ReadItemTagDto"];
type ReadExtendedItemTagDto = components["schemas"]["ReadExtendedItemTagDto"];
type CreateItemTagByItemDto = components["schemas"]["CreateItemTagByItemDto"];

type ReadCommandItemDto = components["schemas"]["ReadCommandItemDto"];
type ReadExtendedCommandItemDto = components["schemas"]["ReadExtendedCommandItemDto"];
type CreateCommandItemByItemDto = components["schemas"]["CreateCommandItemByItemDto"];
type UpdateCommandItemDto = components["schemas"]["UpdateCommandItemDto"];

type ReadProjectItemDto = components["schemas"]["ReadProjectItemDto"];
type ReadExtendedProjectItemDto = components["schemas"]["ReadExtendedProjectItemDto"];
type CreateProjectItemByItemDto = components["schemas"]["CreateProjectItemByItemDto"];
type UpdateProjectItemDto = components["schemas"]["UpdateProjectItemDto"];

type ReadItemHistoryDto = components["schemas"]["ReadItemHistoryDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS: Record<string, (store: StoreGeneric, idItem: string, data: ReadExtendedItemDto) => void> = {
	item_documents: (store, idItem, data) => {
		store.documents[idItem] = {};
		for (const document of data.item_documents ?? []) {
			if (!document.id_item_document) {
				continue;
			}
			store.documents[idItem][document.id_item_document] = document;
		}
	},
	item_boxs: (store, idItem, data) => {
		store.itemBoxs[idItem] = {};
		for (const itemBox of data.item_boxs ?? []) {
			if (!itemBox.id_box) {
				continue;
			}
			store.itemBoxs[idItem][itemBox.id_box] = itemBox;
		}
	},
	item_tags: (store, idItem, data) => {
		store.itemTags[idItem] = {};
		for (const itemTag of data.item_tags ?? []) {
			if (!itemTag.id_tag) {
				continue;
			}
			store.itemTags[idItem][itemTag.id_tag] = itemTag;
		}
	},
	command_items: (store, idItem, data) => {
		store.itemCommands[idItem] = {};
		for (const itemCommand of data.command_items ?? []) {
			if (!itemCommand.id_command) {
				continue;
			}
			store.itemCommands[idItem][itemCommand.id_command] = itemCommand;
		}
	},
	project_items: (store, idItem, data) => {
		store.itemProjects[idItem] = {};
		for (const itemProject of data.project_items ?? []) {
			if (!itemProject.id_project) {
				continue;
			}
			store.itemProjects[idItem][itemProject.id_project] = itemProject;
		}
	},
	item_history: (store, idItem, data) => {
		store.itemHistory[idItem] = {};
		for (const itemHistory of data.item_history ?? []) {
			if (!itemHistory.id_item_history) {
				continue;
			}
			store.itemHistory[idItem][itemHistory.id_item_history] = itemHistory;
		}
	},
};

function hydrateItem(store: StoreGeneric, idItem: string, item: ReadExtendedItemDto, expand: string[] = []) {
	if (item.url_thumbnail_item && !store.thumbnailsURL[idItem]) {
		store.showThumbnailById(idItem);
	}
	store.documentsTotalCount[idItem] = item.item_documents_count;
	store.itemBoxsTotalCount[idItem] = item.item_boxs_count;
	store.itemTagsTotalCount[idItem] = item.item_tags_count;
	store.itemCommandsTotalCount[idItem] = item.command_items_count;
	store.itemProjectsTotalCount[idItem] = item.project_items_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idItem, item);
		}
	}
}

const itemResource = createMainResource<{ readExtended: ReadExtendedItemDto, readBasic: ReadItemDto, create: CreateItemDto, update: UpdateItemDto }>({
	path: () => "/item",
	idField: "id_item",
	stateKey: "items",
	countKey: "itemsTotalCount",
	loadingKey: "itemsLoading",
	editionKey: "itemEdition",
	onHydrate: (store, entity: ReadExtendedItemDto, expand) => {
		if (entity.id_item === undefined) {
			return;
		}
		hydrateItem(store, String(entity.id_item), entity, expand);
	},
});

const documentResource = createNestedResource<{ readBasic: ReadItemDocumentDto, create: CreateItemDocumentByItemDto, update: UpdateItemDocumentDto }>({
	path: (idItem) => `/item/${idItem}/document`,
	idField: "id_item_document",
	stateKey: "documents",
	countKey: "documentsTotalCount",
	loadingKey: "documentsLoading",
	editionKey: "documentEdition",
	readyKey: "documentReady",
});
const itemBoxResource = createNestedResource<{ readExtended: ReadExtendedItemBoxDto, readBasic: ReadItemBoxDto, create: CreateItemBoxByItemDto, update: UpdateItemBoxDto }>({
	path: (idItem) => `/item/${idItem}/box`,
	idField: "id_box",
	stateKey: "itemBoxs",
	countKey: "itemBoxsTotalCount",
	loadingKey: "itemBoxsLoading",
	editionKey: "itemBoxEdition",
	readyKey: "itemBoxReady",
	onHydrate: (store, entity: ReadExtendedItemBoxDto, expand) => {
		if (expand.includes("box") && entity.box && entity.id_box && entity.box.id_store) {
			const storesStore = useStoresStore();
			storesStore.boxs[entity.box.id_store] ??= {};
			storesStore.boxs[entity.box.id_store][entity.id_box] = entity.box;
		}
	},
});
const itemTagResource = createNestedResource<{ readExtended: ReadExtendedItemTagDto, readBasic: ReadItemTagDto, create: CreateItemTagByItemDto }>({
	path: (idItem) => `/item/${idItem}/tag`,
	idField: "id_tag",
	stateKey: "itemTags",
	countKey: "itemTagsTotalCount",
	loadingKey: "itemTagsLoading",
	editionKey: "itemTagEdition",
	readyKey: "itemTagReady",
	onHydrate: (store, entity: ReadExtendedItemTagDto, expand) => {
		if (expand.includes("tag") && entity.tag && entity.id_tag) {
			const tagsStore = useTagsStore();
			tagsStore.tags[entity.id_tag] = entity.tag;
		}
	},
});
const itemCommandResource = createNestedResource<{ readExtended: ReadExtendedCommandItemDto, readBasic: ReadCommandItemDto, create: CreateCommandItemByItemDto, update: UpdateCommandItemDto }>({
	path: (idItem) => `/item/${idItem}/command`,
	idField: "id_command",
	stateKey: "itemCommands",
	countKey: "itemCommandsTotalCount",
	loadingKey: "itemCommandsLoading",
	editionKey: "itemCommandEdition",
	readyKey: "itemCommandReady",
	onHydrate: (store, entity: ReadExtendedCommandItemDto, expand) => {
		if (expand.includes("command") && entity.command && entity.id_command) {
			const commandsStore = useCommandsStore();
			commandsStore.commands[entity.id_command] = entity.command;
		}
	},
});
const itemProjectResource = createNestedResource<{ readExtended: ReadExtendedProjectItemDto, readBasic: ReadProjectItemDto, create: CreateProjectItemByItemDto, update: UpdateProjectItemDto }>({
	path: (idItem) => `/item/${idItem}/project`,
	idField: "id_project",
	stateKey: "itemProjects",
	countKey: "itemProjectsTotalCount",
	loadingKey: "itemProjectsLoading",
	editionKey: "itemProjectEdition",
	readyKey: "itemProjectReady",
	onHydrate: (store, entity: ReadExtendedProjectItemDto, expand) => {
		if (expand.includes("project") && entity.project && entity.id_project) {
			const projectsStore = useProjectsStore();
			projectsStore.projects[entity.id_project] = entity.project;
		}
	},
});
const itemHistoryResource = createNestedResource<{ readBasic: ReadItemHistoryDto }>({
	path: (idItem) => `/item/${idItem}/history`,
	idField: "id_item_history",
	stateKey: "itemHistory",
	countKey: "itemHistoryTotalCount",
	loadingKey: "itemHistoryLoading",
});

export const useItemsStore = defineStore("items",{
	state: () => ({
		itemsLoading: false,
		itemsTotalCount: 0,
		items: {} as Record<string, ReadItemDto>,
		itemEdition: {} as Record<string, Edition<{ create: CreateItemDto, update: UpdateItemDto }>>,

		documentsLoading: false,
		documentsTotalCount: {} as Record<string, number>,
		documents: {} as Record<string, Record<string, ReadItemDocumentDto>>,
		documentEdition: {} as Record<string, Record<string, Edition<{ create: CreateItemDocumentByItemDto, update: UpdateItemDocumentDto }>>>,
		documentReady: {} as Record<string, Record<string, Ready<{ create: CreateItemDocumentByItemDto, update: UpdateItemDocumentDto }>>>,

		itemBoxsLoading: false,
		itemBoxsTotalCount: {} as Record<string, number>,
		itemBoxs: {} as Record<string, Record<string, ReadItemBoxDto>>,
		itemBoxEdition: {} as Record<string, Record<string, Edition<{ create: CreateItemBoxByItemDto, update: UpdateItemBoxDto }>>>,
		itemBoxReady: {} as Record<string, Record<string, Ready<{ create: CreateItemBoxByItemDto, update: UpdateItemBoxDto }>>>,

		itemTagsLoading: false,
		itemTagsTotalCount: {} as Record<string, number>,
		itemTags: {} as Record<string, Record<string, ReadItemTagDto>>,
		itemTagEdition: {} as Record<string, Record<string, Edition<{ create: CreateItemTagByItemDto }>>>,
		itemTagReady: {} as Record<string, Record<string, Ready<{ create: CreateItemTagByItemDto }>>>,

		itemCommandsLoading: false,
		itemCommandsTotalCount: {} as Record<string, number>,
		itemCommands: {} as Record<string, Record<string, ReadCommandItemDto>>,
		itemCommandEdition: {} as Record<string, Record<string, Edition<{ create: CreateCommandItemByItemDto, update: UpdateCommandItemDto }>>>,
		itemCommandReady: {} as Record<string, Record<string, Ready<{ create: CreateCommandItemByItemDto, update: UpdateCommandItemDto }>>>,

		itemProjectsLoading: false,
		itemProjectsTotalCount: {} as Record<string, number>,
		itemProjects: {} as Record<string, Record<string, ReadProjectItemDto>>,
		itemProjectEdition: {} as Record<string, Record<string, Edition<{ create: CreateProjectItemByItemDto, update: UpdateProjectItemDto }>>>,
		itemProjectReady: {} as Record<string, Record<string, Ready<{ create: CreateProjectItemByItemDto, update: UpdateProjectItemDto }>>>,

		imagesURL: {} as Record<string, string>,
		thumbnailsURL: {} as Record<string, string>,

		itemHistoryLoading: false,
		itemHistoryTotalCount: {} as Record<string, number>,
		itemHistory: {} as Record<string, Record<string, ReadItemHistoryDto>>,
	}),
	actions: {
		getItemByList: itemResource.getByList,
		getItemByInterval: itemResource.getByInterval,
		getItemById: itemResource.getById,
		createItem: itemResource.create,
		getAvailableNewItemId: itemResource.getAvailableNewId,
		updateItem: itemResource.update,
		deleteItem: itemResource.remove,
		loadToEdition(id: string, preset: string | null = null) {
			if (!isNewId(id) && this.items[id]) {
				this.itemEdition[id] = {
					loading: false,
					reference_name_item: this.items[id].reference_name_item ?? undefined,
					friendly_name_item: this.items[id].friendly_name_item ?? undefined,
					description_item: this.items[id].description_item ?? undefined,
					threshold_min_item: this.items[id].threshold_min_item,
				};
			} else {
				this.itemEdition[id] = {
					loading: false,
				};
				itemResource.loadEditionPreset.call(this, id, preset);
			}
			this.documentEdition[id] = {};
			this.documentReady[id] = {};
			this.itemBoxEdition[id] = {};
			this.itemBoxReady[id] = {};
			this.itemTagEdition[id] = {};
			this.itemTagReady[id] = {};
			this.itemCommandEdition[id] = {};
			this.itemCommandReady[id] = {};
			this.itemProjectEdition[id] = {};
			this.itemProjectReady[id] = {};
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.itemEdition[id]) {
				this.itemEdition[id] = {};
			}
			this.itemEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.itemEdition[id];
			delete this.documentEdition[id];
			delete this.documentReady[id];
			delete this.itemBoxEdition[id];
			delete this.itemBoxReady[id];
			delete this.itemTagEdition[id];
			delete this.itemTagReady[id];
			delete this.itemCommandEdition[id];
			delete this.itemCommandReady[id];
			delete this.itemProjectEdition[id];
			delete this.itemProjectReady[id];
		},
		async saveAllChanges(id: string) {
			let realId = id;
			const { isFormData, ...data } = this.itemEdition[id];
			const imageChanged = !!data.img_file || !!data.unset_img_item;
			if (isNewId(id)) {
				realId = await this.createItem(data as CreateItemDto, isFormData);
				this.copyDocumentAllId(id, realId);
				this.copyItemBoxAllId(id, realId);
				this.copyItemTagAllId(id, realId);
				this.copyItemCommandAllId(id, realId);
				this.copyItemProjectAllId(id, realId);
			} else {
				await this.updateItem(id, data as UpdateItemDto, isFormData);
			}
			await Promise.all([
				this.pushDocumentChange(realId),
				this.pushItemBoxChange(realId),
				this.pushItemTagChange(realId),
				this.pushItemCommandChange(realId),
				this.pushItemProjectChange(realId),
			]);
			if (imageChanged) {
				delete this.thumbnailsURL[realId];
				delete this.imagesURL[realId];
				void this.showThumbnailById(realId);
				void this.showImageById(realId);
			}
			return realId;
		},

		getDocumentByInterval: documentResource.getByInterval,
		getDocumentById: documentResource.getById,
		createDocument: documentResource.create,
		updateDocument: documentResource.update,
		deleteDocument: documentResource.remove,
		getAvailableNewDocumentId: documentResource.getAvailableNewId,
		valideDocumentEditionById: documentResource.valideEditionById,
		copyDocumentPerId: documentResource.copyPerId,
		copyDocumentAllId: documentResource.copyAllId,
		pushDocumentChange: documentResource.pushChange,
		async downloadDocument(idItem: string, id: string) {
			return await fetchWrapper.image({
				url: `${baseUrl}/item/${idItem}/document/${id}/download`,
				useToken: "access",
			});
		},

		getItemBoxByInterval: itemBoxResource.getByInterval,
		getItemBoxById: itemBoxResource.getById,
		createItemBox: itemBoxResource.create,
		updateItemBox: itemBoxResource.update,
		deleteItemBox: itemBoxResource.remove,
		getAvailableNewItemBoxId: itemBoxResource.getAvailableNewId,
		valideItemBoxEditionById: itemBoxResource.valideEditionById,
		copyItemBoxPerId: itemBoxResource.copyPerId,
		copyItemBoxAllId: itemBoxResource.copyAllId,
		pushItemBoxChange: itemBoxResource.pushChange,

		getItemTagByInterval: itemTagResource.getByInterval,
		getItemTagById: itemTagResource.getById,
		createItemTag: itemTagResource.create,
		deleteItemTag: itemTagResource.remove,
		createItemTagBulk: itemTagResource.createBulk,
		deleteItemTagBulk: itemTagResource.removeBulk,
		getAvailableNewItemTagId: itemTagResource.getAvailableNewId,
		valideItemTagEditionById: itemTagResource.valideEditionById,
		copyItemTagPerId: itemTagResource.copyPerId,
		copyItemTagAllId: itemTagResource.copyAllId,
		pushItemTagChange: itemTagResource.pushChange,

		getItemCommandByInterval: itemCommandResource.getByInterval,
		getItemCommandById: itemCommandResource.getById,
		createItemCommand: itemCommandResource.create,
		updateItemCommand: itemCommandResource.update,
		deleteItemCommand: itemCommandResource.remove,
		createItemCommandBulk: itemCommandResource.createBulk,
		getAvailableNewItemCommandId: itemCommandResource.getAvailableNewId,
		valideItemCommandEditionById: itemCommandResource.valideEditionById,
		copyItemCommandPerId: itemCommandResource.copyPerId,
		copyItemCommandAllId: itemCommandResource.copyAllId,
		pushItemCommandChange: itemCommandResource.pushChange,

		getItemProjectByInterval: itemProjectResource.getByInterval,
		getItemProjectById: itemProjectResource.getById,
		createItemProject: itemProjectResource.create,
		updateItemProject: itemProjectResource.update,
		deleteItemProject: itemProjectResource.remove,
		createItemProjectBulk: itemProjectResource.createBulk,
		getAvailableNewItemProjectId: itemProjectResource.getAvailableNewId,
		valideItemProjectEditionById: itemProjectResource.valideEditionById,
		copyItemProjectPerId: itemProjectResource.copyPerId,
		copyItemProjectAllId: itemProjectResource.copyAllId,
		pushItemProjectChange: itemProjectResource.pushChange,

		async showImageById(id: string) {
			if (this.imagesURL[id]) {
				return;
			}
			const response = await fetchWrapper.image({
				url: `${baseUrl}/item/${id}/picture`,
				useToken: "access",
			});
			this.imagesURL[id] = URL.createObjectURL(response);
		},
		async showThumbnailById(id: string) {
			if (this.thumbnailsURL[id]) {
				return;
			}
			const response = await fetchWrapper.image({
				url: `${baseUrl}/item/${id}/thumbnail`,
				useToken: "access",
			});
			this.thumbnailsURL[id] = URL.createObjectURL(response);
		},

		getItemHistoryByInterval: itemHistoryResource.getByInterval,
		getItemHistoryById: itemHistoryResource.getById,
	},
});

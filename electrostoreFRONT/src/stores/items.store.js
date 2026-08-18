import { defineStore } from "pinia";

import { fetchWrapper, createMainResource, createNestedResource } from "@/helpers";

import { useTagsStore, useStoresStore, useCommandsStore, useProjectsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
	item_documents: (store, idItem, data) => {
		store.documents[idItem] = {};
		for (const document of data) {
			store.documents[idItem][document.id_item_document] = document;
		}
	},
	item_boxs: (store, idItem, data) => {
		store.itemBoxs[idItem] = {};
		for (const itemBox of data) {
			store.itemBoxs[idItem][itemBox.id_box] = itemBox;
		}
	},
	item_tags: (store, idItem, data) => {
		store.itemTags[idItem] = {};
		for (const itemTag of data) {
			store.itemTags[idItem][itemTag.id_tag] = itemTag;
		}
	},
	item_commands: (store, idItem, data) => {
		store.itemCommands[idItem] = {};
		for (const itemCommand of data) {
			store.itemCommands[idItem][itemCommand.id_command] = itemCommand;
		}
	},
	project_items: (store, idItem, data) => {
		store.itemProjects[idItem] = {};
		for (const itemProject of data) {
			store.itemProjects[idItem][itemProject.id_project] = itemProject;
		}
	},
	images: (store, idItem, data) => {
		store.images[idItem] = {};
		for (const image of data) {
			store.images[idItem][image.id_img] = image;
		}
	},
	item_history: (store, idItem, data) => {
		store.itemHistory[idItem] = {};
		for (const itemHistory of data) {
			store.itemHistory[idItem][itemHistory.id_item_history] = itemHistory;
		}
	},
};

function hydrateItem(store, idItem, item, expand = []) {
	if (item.id_img && !this.thumbnailsURL[item.id_img]) {
		this.showThumbnailById(item.id_item, item.id_img);
	}
	store.documentsTotalCount[idItem] = item["item_documents_count"];
	store.itemBoxsTotalCount[idItem] = item["item_boxs_count"];
	store.itemTagsTotalCount[idItem] = item["item_tags_count"];
	store.itemCommandsTotalCount[idItem] = item["command_items_count"];
	store.itemProjectsTotalCount[idItem] = item["project_items_count"];
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idItem, item[key]);
		}
	}
}

const itemResource = createMainResource({
	path: () => "/item",
	idField: "id_item",
	stateKey: "items",
	countKey: "itemsTotalCount",
	loadingKey: "itemsLoading",
	onHydrate: (store, entity, expand) => {
		hydrateItem(store, entity.id_item, entity, expand);
	},
	/* onUpdate: (store, entity) => {
		if (store.items[entity.id_item].id_img) {
			store.showImageById(store.items[entity.id_item].id_item, store.items[entity.id_item].id_img);
		}
	}, */
});

const documentResource = createNestedResource({
	path: (idItem) => `/item/${idItem}/document`,
	idField: "id_item_document",
	stateKey: "documents",
	countKey: "documentsTotalCount",
	loadingKey: "documentsLoading",
	editionKey: "documentEdition",
	readyKey: "documentReady",
});
const itemBoxResource = createNestedResource({
	path: (idItem) => `/item/${idItem}/box`,
	idField: "id_item_box",
	stateKey: "itemBoxs",
	countKey: "itemBoxsTotalCount",
	loadingKey: "itemBoxsLoading",
	editionKey: "itemBoxEdition",
	readyKey: "itemBoxReady",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("box")) {
			const storeStore = useStoresStore();
			if (!storeStore.boxs[entity.id_store]) {
				storeStore.boxs[entity.id_store] = {};
			}
			storeStore.boxs[entity.id_store][entity.id_box] = entity.box;
		}
	},
});
const itemTagResource = createNestedResource({
	path: (idItem) => `/item/${idItem}/tag`,
	idField: "id_item_tag",
	stateKey: "itemTags",
	countKey: "itemTagsTotalCount",
	loadingKey: "itemTagsLoading",
	editionKey: "itemTagEdition",
	readyKey: "itemTagReady",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("tag")) {
			const tagsStore = useTagsStore();
			tagsStore.tags[entity.id_tag] = entity.tag;
		}
	},
});
const itemCommandResource = createNestedResource({
	path: (idItem) => `/item/${idItem}/command`,
	idField: "id_command_item",
	stateKey: "itemCommands",
	countKey: "itemCommandsTotalCount",
	loadingKey: "itemCommandsLoading",
	editionKey: "itemCommandEdition",
	readyKey: "itemCommandReady",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("command")) {
			const commandsStore = useCommandsStore();
			commandsStore.commands[entity.id_command] = entity.command;
		}
	},
});
const itemProjectResource = createNestedResource({
	path: (idItem) => `/item/${idItem}/project`,
	idField: "id_project",
	stateKey: "itemProjects",
	countKey: "itemProjectsTotalCount",
	loadingKey: "itemProjectsLoading",
	editionKey: "itemProjectEdition",
	readyKey: "itemProjectReady",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("project")) {
			const projectsStore = useProjectsStore();
			projectsStore.projects[entity.id_project] = entity.project;
		}
	},
});
const imageResource = createNestedResource({
	path: (idItem) => `/item/${idItem}/img`,
	idField: "id_image",
	stateKey: "images",
	countKey: "imagesTotalCount",
	loadingKey: "imagesLoading",
	editionKey: "imageEdition",
	readyKey: "imageReady",
	onHydrate: (store, entity, expand, externalParam) => {
		if (externalParam?.loadImages && !store.imagesURL[entity.id_image]) {
			store.showImageById(store, externalParam.idItem, entity.id_image);
		}
		if (externalParam?.loadThumbnails && !store.thumbnailsURL[entity.id_image]) {
			store.showThumbnailById(store, externalParam.idItem, entity.id_image);
		}
	},
	/* onRemove: (store, idImage) => {
		if (store.imagesURL[idImage]) {
			URL.revokeObjectURL(store.imagesURL[idImage]);
			delete store.imagesURL[idImage];
		}
		if (store.thumbnailsURL[idImage]) {
			URL.revokeObjectURL(store.thumbnailsURL[idImage]);
			delete store.thumbnailsURL[idImage];
		}
	}, */
});
const itemHistoryResource = createNestedResource({
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
		items: {},
		itemEdition: {},

		documentsLoading: false,
		documentsTotalCount: {},
		documents: {},
		documentEdition: {},
		documentReady: {},

		itemBoxsLoading: false,
		itemBoxsTotalCount: {},
		itemBoxs: {},
		itemBoxEdition: {},
		itemBoxReady: {},

		itemTagsLoading: false,
		itemTagsTotalCount: {},
		itemTags: {},
		itemTagEdition: {},
		itemTagReady: {},

		itemCommandsLoading: false,
		itemCommandsTotalCount: {},
		itemCommands: {},
		itemCommandEdition: {},
		itemCommandReady: {},

		itemProjectsLoading: false,
		itemProjectsTotalCount: {},
		itemProjects: {},
		itemProjectEdition: {},
		itemProjectReady: {},

		imagesLoading: false,
		imagesTotalCount: {},
		images: {},
		imagesURL: {},
		thumbnailsURL: {},
		imageEdition: {},
		imageReady: {},

		itemHistoryLoading: false,
		itemHistoryTotalCount: {},
		itemHistory: {},
	}),
	actions: {
		getItemByList: itemResource.getByList,
		getItemByInterval: itemResource.getByInterval,
		getItemById: itemResource.getById,
		createItem: itemResource.create,
		updateItem: itemResource.update,
		deleteItem: itemResource.remove,
		loadToEdition(id, preset = null) {
			this.itemEdition[id] = {};
			if (preset) {
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.itemEdition[id][key] = value;
					}
				});
			}
			if (id !== "new" && this.items[id]) {
				this.itemEdition[id] = {
					loading: false,
					id_item: this.items[id].id_item,
					reference_name_item: this.items[id].reference_name_item,
					friendly_name_item: this.items[id].friendly_name_item,
					description_item: this.items[id].description_item,
					threshold_min_item: this.items[id].threshold_min_item,
					id_img: this.items[id].id_img,
				};
			} else {
				this.itemEdition[id] = {
					loading: false,
				};
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
			this.imageEdition[id] = {};
			this.imageReady[id] = {};
		},
		setLoadingEdition(id, loading) {
			if (!this.itemEdition[id]) {
				this.itemEdition[id] = {};
			}
			this.itemEdition[id].loading = loading;
		},
		clearEdition(id) {
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
			delete this.imageEdition[id];
			delete this.imageReady[id];
		},
		async saveAllChanges(id) {
			let realId = id;
			if (id === "new") {
				realId = await this.createItem(this.itemEdition[id]);
				this.copyDocumentAllId(id, realId);
				this.copyItemBoxAllId(id, realId);
				this.copyItemTagAllId(id, realId);
				this.copyItemCommandAllId(id, realId);
				this.copyItemProjectAllId(id, realId);
				this.copyImageAllId(id, realId);
			} else {
				await this.updateItem(id, this.itemEdition[id]);
			}
			await Promise.all([
				this.pushDocumentChange(realId),
				this.pushItemBoxChange(realId),
				this.pushItemTagChange(realId),
				this.pushItemCommandChange(realId),
				this.pushItemProjectChange(realId),
				this.pushImageChange(realId),
			]);
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
		async downloadDocument(idItem, id) {
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
		deleteItemTagBulk: itemTagResource.deleteBulk,
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

		getImageByInterval: imageResource.getByInterval,
		getImageById: imageResource.getById,
		createImage: imageResource.create,
		updateImage: imageResource.update,
		deleteImage: imageResource.remove,
		getAvailableNewImageId: imageResource.getAvailableNewId,
		valideImageEditionById: imageResource.valideEditionById,
		copyImagePerId: imageResource.copyPerId,
		copyImageAllId: imageResource.copyAllId,
		pushImageChange: imageResource.pushChange,
		async showImageById(id_item, id_img) {
			if (this.imagesURL[id_img]) {
				return;
			}
			const response = await fetchWrapper.image({
				url: `${baseUrl}/item/${id_item}/img/${id_img}/picture`,
				useToken: "access",
			});
			const url = URL.createObjectURL(response);
			this.imagesURL[id_img] = url;
		},
		async showThumbnailById(id_item, id_img) {
			if (this.thumbnailsURL[id_img]) {
				return;
			}
			const response = await fetchWrapper.image({
				url: `${baseUrl}/item/${id_item}/img/${id_img}/thumbnail`,
				useToken: "access",
			});
			const url = URL.createObjectURL(response);
			this.thumbnailsURL[id_img] = url;
		},

		getItemHistoryByInterval: itemHistoryResource.getByInterval,
		getItemHistoryById: itemHistoryResource.getById,
	},
});

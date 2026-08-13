import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";

import { useTagsStore, useStoresStore, useCommandsStore, useProjetsStore, useUsersStore } from "@/stores";
import { onUpdated } from "vue";

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
	item_projets: (store, idItem, data) => {
		store.itemProjets[idItem] = {};
		for (const itemProjet of data) {
			store.itemProjets[idItem][itemProjet.id_projet] = itemProjet;
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
	store.itemProjetsTotalCount[idItem] = item["projet_items_count"];
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
});
const itemBoxResource = createNestedResource({
	path: (idItem) => `/item/${idItem}/box`,
	idField: "id_item_box",
	stateKey: "itemBoxs",
	countKey: "itemBoxsTotalCount",
	loadingKey: "itemBoxsLoading",
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
	onHydrate: (store, entity, expand) => {
		if (expand.includes("command")) {
			const commandsStore = useCommandsStore();
			commandsStore.commands[entity.id_command] = entity.command;
		}
	},
});
const itemProjetResource = createNestedResource({
	path: (idItem) => `/item/${idItem}/projet`,
	idField: "id_projet_item",
	stateKey: "itemProjets",
	countKey: "itemProjetsTotalCount",
	loadingKey: "itemProjetsLoading",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("projet")) {
			const projetsStore = useProjetsStore();
			projetsStore.projets[entity.id_projet] = entity.projet;
		}
	},
});
const imageResource = createNestedResource({
	path: (idItem) => `/item/${idItem}/img`,
	idField: "id_image",
	stateKey: "images",
	countKey: "imagesTotalCount",
	loadingKey: "imagesLoading",
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

		itemBoxsLoading: false,
		itemBoxsTotalCount: {},
		itemBoxs: {},
		itemBoxEdition: {},

		itemTagsLoading: false,
		itemTagsTotalCount: {},
		itemTags: {},
		itemTagEdition: {},

		itemCommandsLoading: false,
		itemCommandsTotalCount: {},
		itemCommands: {},
		itemCommandEdition: {},

		itemProjetsLoading: false,
		itemProjetsTotalCount: {},
		itemProjets: {},
		itemProjetEdition: {},

		imagesLoading: false,
		imagesTotalCount: {},
		images: {},
		imagesURL: {},
		thumbnailsURL: {},
		imageEdition: {},

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
					seuil_min_item: this.items[id].seuil_min_item,
					id_img: this.items[id].id_img,
				};
			} else {
				this.itemEdition[id] = {
					loading: false,
				};
			}
			this.documentEdition[id] = {};
			this.itemBoxEdition[id] = {};
			this.itemTagEdition[id] = {};
			this.itemCommandEdition[id] = {};
			this.itemProjetEdition[id] = {};
			this.imageEdition[id] = {};
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
			delete this.itemBoxEdition[id];
			delete this.itemTagEdition[id];
			delete this.itemCommandEdition[id];
			delete this.itemProjetEdition[id];
			delete this.imageEdition[id];
		},

		getDocumentByInterval: documentResource.getByInterval,
		getDocumentById: documentResource.getById,
		createDocument: documentResource.create,
		updateDocument: documentResource.update,
		deleteDocument: documentResource.remove,
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

		getItemTagByInterval: itemTagResource.getByInterval,
		getItemTagById: itemTagResource.getById,
		createItemTag: itemTagResource.create,
		deleteItemTag: itemTagResource.remove,
		createItemTagBulk: itemTagResource.createBulk,
		deleteItemTagBulk: itemTagResource.deleteBulk,

		getItemCommandByInterval: itemCommandResource.getByInterval,
		getItemCommandById: itemCommandResource.getById,
		createItemCommand: itemCommandResource.create,
		updateItemCommand: itemCommandResource.update,
		deleteItemCommand: itemCommandResource.remove,
		createItemCommandBulk: itemCommandResource.createBulk,

		getItemProjetByInterval: itemProjetResource.getByInterval,
		getItemProjetById: itemProjetResource.getById,
		createItemProjet: itemProjetResource.create,
		updateItemProjet: itemProjetResource.update,
		deleteItemProjet: itemProjetResource.remove,
		createItemProjetBulk: itemProjetResource.createBulk,

		getImageByInterval: imageResource.getByInterval,
		getImageById: imageResource.getById,
		createImage: imageResource.create,
		updateImage: imageResource.update,
		deleteImage: imageResource.remove,
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

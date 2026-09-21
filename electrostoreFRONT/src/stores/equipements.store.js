import { defineStore } from "pinia";

import { fetchWrapper, createMainResource, createNestedResource } from "@/helpers";

import { useTagsStore, useStoresStore, useUsersStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
	equipement_tags: (store, idEquipement, data) => {
		store.equipementTags[idEquipement] = {};
		for (const equipementTag of data) {
			store.equipementTags[idEquipement][equipementTag.id_tag] = equipementTag;
		}
	},
	equipement_boxs: (store, idEquipement, data) => {
		store.equipementBoxs[idEquipement] = {};
		for (const equipementBox of data) {
			store.equipementBoxs[idEquipement][equipementBox.id_box] = equipementBox;
		}
	},
};

function hydrateEquipement(store, idEquipement, equipement, expand = []) {
	if (equipement.url_thumbnail_equipement && !store.thumbnailsURL[idEquipement]) {
		store.showThumbnailById(idEquipement);
	}
	store.equipementTagsTotalCount[idEquipement] = equipement.equipement_tags_count;
	store.equipementBoxsTotalCount[idEquipement] = equipement.equipement_boxs_count;
	store.equipementDocumentsTotalCount[idEquipement] = equipement.equipement_documents_count;
	store.equipementMaintenancesTotalCount[idEquipement] = equipement.equipement_maintenances_count;
	store.equipementStatusHistoryTotalCount[idEquipement] = equipement.equipement_status_history_count;
	store.equipementCommentsTotalCount[idEquipement] = equipement.equipement_comments_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idEquipement, equipement[key]);
		}
	}
}

const equipementResource = createMainResource({
	path: () => "/equipement",
	idField: "id_equipement",
	stateKey: "equipements",
	countKey: "equipementsTotalCount",
	loadingKey: "equipementsLoading",
	onHydrate: (store, entity, expand) => {
		hydrateEquipement(store, entity.id_equipement, entity, expand);
	},
});

const equipementTagResource = createNestedResource({
	path: (idEquipement) => `/equipement/${idEquipement}/tag`,
	idField: "id_tag",
	stateKey: "equipementTags",
	countKey: "equipementTagsTotalCount",
	loadingKey: "equipementTagsLoading",
	editionKey: "equipementTagEdition",
	readyKey: "equipementTagReady",
	onHydrate: (store, idEquipement, entity, expand) => {
		if (expand.includes("tag")) {
			const tagsStore = useTagsStore();
			tagsStore.tags[entity.id_tag] = entity.tag;
		}
	},
});

const equipementBoxResource = createNestedResource({
	path: (idEquipement) => `/equipement/${idEquipement}/box`,
	idField: "id_box",
	stateKey: "equipementBoxs",
	countKey: "equipementBoxsTotalCount",
	loadingKey: "equipementBoxsLoading",
	editionKey: "equipementBoxEdition",
	readyKey: "equipementBoxReady",
	onHydrate: (store, idEquipement, entity, expand) => {
		if (expand.includes("box") && entity.box) {
			const storesStore = useStoresStore();
			storesStore.boxs[entity.box.id_store] ??= {};
			storesStore.boxs[entity.box.id_store][entity.id_box] = entity.box;
		}
	},
});

const equipementDocumentResource = createNestedResource({
	path: (idEquipement) => `/equipement/${idEquipement}/document`,
	idField: "id_equipement_document",
	stateKey: "equipementDocuments",
	countKey: "equipementDocumentsTotalCount",
	loadingKey: "equipementDocumentsLoading",
	editionKey: "equipementDocumentEdition",
	readyKey: "equipementDocumentReady",
});

const equipementMaintenanceResource = createNestedResource({
	path: (idEquipement) => `/equipement/${idEquipement}/maintenance`,
	idField: "id_equipement_maintenance",
	stateKey: "equipementMaintenances",
	countKey: "equipementMaintenancesTotalCount",
	loadingKey: "equipementMaintenancesLoading",
	editionKey: "equipementMaintenanceEdition",
	readyKey: "equipementMaintenanceReady",
	onHydrate: (store, idEquipement, entity, expand) => {
		if (expand.includes("user") && entity.user) {
			const usersStore = useUsersStore();
			usersStore.users[entity.id_user] = entity.user;
		}
	},
});

const equipementCommentResource = createNestedResource({
	path: (idEquipement) => `/equipement/${idEquipement}/comment`,
	idField: "id_equipement_comment",
	stateKey: "equipementComments",
	countKey: "equipementCommentsTotalCount",
	loadingKey: "equipementCommentsLoading",
	editionKey: "equipementCommentEdition",
	onHydrate: (store, idEquipement, entity, expand) => {
		if (expand.includes("user") && entity.user) {
			const usersStore = useUsersStore();
			usersStore.users[entity.id_user] = entity.user;
		}
	},
});

const equipementStatusHistoryResource = createNestedResource({
	path: (idEquipement) => `/equipement/${idEquipement}/status-history`,
	idField: "id_equipement_status",
	stateKey: "equipementStatusHistory",
	countKey: "equipementStatusHistoryTotalCount",
	loadingKey: "equipementStatusHistoryLoading",
});

export const useEquipementsStore = defineStore("equipements", {
	state: () => ({
		equipementsLoading: false,
		equipementsTotalCount: 0,
		equipements: {},
		equipementEdition: {},

		equipementTagsLoading: false,
		equipementTagsTotalCount: {},
		equipementTags: {},
		equipementTagEdition: {},
		equipementTagReady: {},

		equipementBoxsLoading: false,
		equipementBoxsTotalCount: {},
		equipementBoxs: {},
		equipementBoxEdition: {},
		equipementBoxReady: {},

		equipementDocumentsLoading: false,
		equipementDocumentsTotalCount: {},
		equipementDocuments: {},
		equipementDocumentEdition: {},
		equipementDocumentReady: {},

		equipementMaintenancesLoading: false,
		equipementMaintenancesTotalCount: {},
		equipementMaintenances: {},
		equipementMaintenanceEdition: {},
		equipementMaintenanceReady: {},

		equipementCommentsLoading: false,
		equipementCommentsTotalCount: {},
		equipementComments: {},
		equipementCommentEdition: {},

		equipementStatusHistoryLoading: false,
		equipementStatusHistoryTotalCount: {},
		equipementStatusHistory: {},

		imagesURL: {},
		thumbnailsURL: {},
	}),
	actions: {
		getEquipementByList: equipementResource.getByList,
		getEquipementByInterval: equipementResource.getByInterval,
		getEquipementById: equipementResource.getById,
		createEquipement: equipementResource.create,
		updateEquipement: equipementResource.update,
		deleteEquipement: equipementResource.remove,
		loadToEdition(id, preset = null) {
			this.equipementEdition[id] = {};
			equipementResource.loadEditionPreset(id, preset);
			if (id !== "new" && this.equipements[id]) {
				this.equipementEdition[id] = {
					loading: false,
					reference_name_equipement: this.equipements[id].reference_name_equipement,
					friendly_name_equipement: this.equipements[id].friendly_name_equipement,
					description_equipement: this.equipements[id].description_equipement,
					status_equipement: this.equipements[id].status_equipement,
					url_thumbnail_equipement: this.equipements[id].url_thumbnail_equipement,
				};
			} else {
				this.equipementEdition[id] = {
					loading: false,
					status_equipement: 0,
				};
			}
			this.equipementTagEdition[id] = {};
			this.equipementTagReady[id] = {};
			this.equipementBoxEdition[id] = {};
			this.equipementBoxReady[id] = {};
			this.equipementDocumentEdition[id] = {};
			this.equipementDocumentReady[id] = {};
		},
		setLoadingEdition(id, loading) {
			if (!this.equipementEdition[id]) {
				this.equipementEdition[id] = {};
			}
			this.equipementEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.equipementEdition[id];
			delete this.equipementTagEdition[id];
			delete this.equipementTagReady[id];
			delete this.equipementBoxEdition[id];
			delete this.equipementBoxReady[id];
			delete this.equipementDocumentEdition[id];
			delete this.equipementDocumentReady[id];
		},
		async saveAllChanges(id) {
			let realId = id;
			const { isFormData, ...data } = this.equipementEdition[id];
			const imageChanged = !!data.img_file || !!data.unset_img_equipement;
			if (id === "new") {
				realId = await this.createEquipement(isFormData ? new FormData(Object.entries(data)) : data);
				this.copyEquipementTagAllId(id, realId);
				this.copyEquipementBoxAllId(id, realId);
				this.copyEquipementDocumentAllId(id, realId);
			} else {
				await this.updateEquipement(id, isFormData ? new FormData(Object.entries(data)) : data);
			}
			await Promise.all([
				this.pushEquipementTagChange(realId),
				this.pushEquipementBoxChange(realId),
				this.pushEquipementDocumentChange(realId),
			]);
			if (imageChanged) {
				delete this.thumbnailsURL[realId];
				delete this.imagesURL[realId];
				this.showThumbnailById(realId);
				this.showImageById(realId);
			}
			return realId;
		},

		getEquipementTagByInterval: equipementTagResource.getByInterval,
		getEquipementTagById: equipementTagResource.getById,
		createEquipementTag: equipementTagResource.create,
		deleteEquipementTag: equipementTagResource.remove,
		createEquipementTagBulk: equipementTagResource.createBulk,
		deleteEquipementTagBulk: equipementTagResource.removeBulk,
		getAvailableNewEquipementTagId: equipementTagResource.getAvailableNewId,
		valideEquipementTagEditionById: equipementTagResource.valideEditionById,
		copyEquipementTagPerId: equipementTagResource.copyPerId,
		copyEquipementTagAllId: equipementTagResource.copyAllId,
		pushEquipementTagChange: equipementTagResource.pushChange,

		getEquipementBoxByInterval: equipementBoxResource.getByInterval,
		getEquipementBoxById: equipementBoxResource.getById,
		createEquipementBox: equipementBoxResource.create,
		deleteEquipementBox: equipementBoxResource.remove,
		getAvailableNewEquipementBoxId: equipementBoxResource.getAvailableNewId,
		valideEquipementBoxEditionById: equipementBoxResource.valideEditionById,
		copyEquipementBoxPerId: equipementBoxResource.copyPerId,
		copyEquipementBoxAllId: equipementBoxResource.copyAllId,
		pushEquipementBoxChange: equipementBoxResource.pushChange,

		getEquipementDocumentByInterval: equipementDocumentResource.getByInterval,
		getEquipementDocumentById: equipementDocumentResource.getById,
		createEquipementDocument: equipementDocumentResource.create,
		updateEquipementDocument: equipementDocumentResource.update,
		deleteEquipementDocument: equipementDocumentResource.remove,
		getAvailableNewEquipementDocumentId: equipementDocumentResource.getAvailableNewId,
		valideEquipementDocumentEditionById: equipementDocumentResource.valideEditionById,
		copyEquipementDocumentPerId: equipementDocumentResource.copyPerId,
		copyEquipementDocumentAllId: equipementDocumentResource.copyAllId,
		pushEquipementDocumentChange: equipementDocumentResource.pushChange,
		async downloadEquipementDocument(idEquipement, id) {
			return await fetchWrapper.image({
				url: `${baseUrl}/equipement/${idEquipement}/document/${id}/download`,
				useToken: "access",
			});
		},

		getEquipementMaintenanceByInterval: equipementMaintenanceResource.getByInterval,
		getEquipementMaintenanceById: equipementMaintenanceResource.getById,
		createEquipementMaintenance: equipementMaintenanceResource.create,
		updateEquipementMaintenance: equipementMaintenanceResource.update,
		deleteEquipementMaintenance: equipementMaintenanceResource.remove,

		getEquipementCommentByInterval: equipementCommentResource.getByInterval,
		getEquipementCommentById: equipementCommentResource.getById,
		createEquipementComment: equipementCommentResource.create,
		updateEquipementComment: equipementCommentResource.update,
		deleteEquipementComment: equipementCommentResource.remove,

		getEquipementStatusHistoryByInterval: equipementStatusHistoryResource.getByInterval,
		getEquipementStatusHistoryById: equipementStatusHistoryResource.getById,

		async showImageById(id_equipement) {
			if (this.imagesURL[id_equipement]) {
				return;
			}
			const response = await fetchWrapper.image({
				url: `${baseUrl}/equipement/${id_equipement}/picture`,
				useToken: "access",
			});
			this.imagesURL[id_equipement] = URL.createObjectURL(response);
		},
		async showThumbnailById(id_equipement) {
			if (this.thumbnailsURL[id_equipement]) {
				return;
			}
			const response = await fetchWrapper.image({
				url: `${baseUrl}/equipement/${id_equipement}/thumbnail`,
				useToken: "access",
			});
			this.thumbnailsURL[id_equipement] = URL.createObjectURL(response);
		},
	},
});

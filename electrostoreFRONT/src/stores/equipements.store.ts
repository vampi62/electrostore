import { defineStore } from "pinia";

import { buildFormData, fetchWrapper, createMainResource, createNestedResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useTagsStore, useStoresStore, useUsersStore } from "@/stores";

import type { components } from "@/types/api";
type ReadEquipementDto = components["schemas"]["ReadEquipementDto"];
type ReadEquipementTagDto = components["schemas"]["ReadEquipementTagDto"];
type ReadEquipementBoxDto = components["schemas"]["ReadEquipementBoxDto"];
type ReadEquipementDocumentDto = components["schemas"]["ReadEquipementDocumentDto"];
type ReadEquipementMaintenanceDto = components["schemas"]["ReadEquipementMaintenanceDto"];
type ReadEquipementCommentDto = components["schemas"]["ReadEquipementCommentDto"];
type ReadEquipementStatusDto = components["schemas"]["ReadEquipementStatusDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS: Record<string, (store: any, idEquipement: any, data: any) => void> = {
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

function hydrateEquipement(store: any, idEquipement: string, equipement: any, expand: string[] = []) {
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
	editionKey: "equipementEdition",
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
	onHydrate: (store, entity, expand) => {
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
	onHydrate: (store, entity, expand) => {
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
	onHydrate: (store, entity, expand) => {
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
	onHydrate: (store, entity, expand) => {
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
		equipements: {} as Record<string, ReadEquipementDto>,
		equipementEdition: {} as Record<string, any>,

		equipementTagsLoading: false,
		equipementTagsTotalCount: {} as Record<string, number>,
		equipementTags: {} as Record<string, ReadEquipementTagDto>,
		equipementTagEdition: {} as Record<string, any>,
		equipementTagReady: {} as Record<string, any>,

		equipementBoxsLoading: false,
		equipementBoxsTotalCount: {} as Record<string, number>,
		equipementBoxs: {} as Record<string, ReadEquipementBoxDto>,
		equipementBoxEdition: {} as Record<string, any>,
		equipementBoxReady: {} as Record<string, any>,

		equipementDocumentsLoading: false,
		equipementDocumentsTotalCount: {} as Record<string, number>,
		equipementDocuments: {} as Record<string, ReadEquipementDocumentDto>,
		equipementDocumentEdition: {} as Record<string, any>,
		equipementDocumentReady: {} as Record<string, any>,

		equipementMaintenancesLoading: false,
		equipementMaintenancesTotalCount: {} as Record<string, number>,
		equipementMaintenances: {} as Record<string, ReadEquipementMaintenanceDto>,
		equipementMaintenanceEdition: {} as Record<string, any>,
		equipementMaintenanceReady: {} as Record<string, any>,

		equipementCommentsLoading: false,
		equipementCommentsTotalCount: {} as Record<string, number>,
		equipementComments: {} as Record<string, ReadEquipementCommentDto>,
		equipementCommentEdition: {} as Record<string, any>,

		equipementStatusHistoryLoading: false,
		equipementStatusHistoryTotalCount: {} as Record<string, number>,
		equipementStatusHistory: {} as Record<string, ReadEquipementStatusDto>,

		imagesURL: {} as Record<string, any>,
		thumbnailsURL: {} as Record<string, any>,
	}),
	actions: {
		getEquipementByList: equipementResource.getByList,
		getEquipementByInterval: equipementResource.getByInterval,
		getEquipementById: equipementResource.getById,
		createEquipement: equipementResource.create,
		getAvailableNewEquipementId: equipementResource.getAvailableNewId,
		updateEquipement: equipementResource.update,
		deleteEquipement: equipementResource.remove,
		loadToEdition(id: string, preset = null) {
			if (!isNewId(id) && this.equipements[id]) {
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
				equipementResource.loadEditionPreset.call(this, id, preset);
			}
			this.equipementTagEdition[id] = {};
			this.equipementTagReady[id] = {};
			this.equipementBoxEdition[id] = {};
			this.equipementBoxReady[id] = {};
			this.equipementDocumentEdition[id] = {};
			this.equipementDocumentReady[id] = {};
			this.equipementMaintenanceEdition[id] = {};
			this.equipementMaintenanceReady[id] = {};
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.equipementEdition[id]) {
				this.equipementEdition[id] = {};
			}
			this.equipementEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.equipementEdition[id];
			delete this.equipementTagEdition[id];
			delete this.equipementTagReady[id];
			delete this.equipementBoxEdition[id];
			delete this.equipementBoxReady[id];
			delete this.equipementDocumentEdition[id];
			delete this.equipementDocumentReady[id];
			delete this.equipementMaintenanceEdition[id];
			delete this.equipementMaintenanceReady[id];
		},
		async saveAllChanges(id: string) {
			let realId = id;
			const { isFormData, ...data } = this.equipementEdition[id];
			const imageChanged = !!data.img_file || !!data.unset_img_equipement;
			if (isNewId(id)) {
				realId = await this.createEquipement(isFormData ? buildFormData(data) : data);
				this.copyEquipementTagAllId(id, realId);
				this.copyEquipementBoxAllId(id, realId);
				this.copyEquipementDocumentAllId(id, realId);
				this.copyEquipementMaintenanceAllId(id, realId);
			} else {
				await this.updateEquipement(id, isFormData ? buildFormData(data) : data);
			}
			await Promise.all([
				this.pushEquipementTagChange(realId),
				this.pushEquipementBoxChange(realId),
				this.pushEquipementDocumentChange(realId),
				this.pushEquipementMaintenanceChange(realId),
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
		async downloadEquipementDocument(idEquipement: string, id: string) {
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
		getAvailableNewEquipementMaintenanceId: equipementMaintenanceResource.getAvailableNewId,
		valideEquipementMaintenanceEditionById: equipementMaintenanceResource.valideEditionById,
		copyEquipementMaintenancePerId: equipementMaintenanceResource.copyPerId,
		copyEquipementMaintenanceAllId: equipementMaintenanceResource.copyAllId,
		pushEquipementMaintenanceChange: equipementMaintenanceResource.pushChange,

		getEquipementCommentByInterval: equipementCommentResource.getByInterval,
		getEquipementCommentById: equipementCommentResource.getById,
		createEquipementComment: equipementCommentResource.create,
		updateEquipementComment: equipementCommentResource.update,
		deleteEquipementComment: equipementCommentResource.remove,

		getEquipementStatusHistoryByInterval: equipementStatusHistoryResource.getByInterval,
		getEquipementStatusHistoryById: equipementStatusHistoryResource.getById,

		async showImageById(id: string) {
			if (this.imagesURL[id]) {
				return;
			}
			const response = await fetchWrapper.image({
				url: `${baseUrl}/equipement/${id}/picture`,
				useToken: "access",
			});
			this.imagesURL[id] = URL.createObjectURL(response);
		},
		async showThumbnailById(id: string) {
			if (this.thumbnailsURL[id]) {
				return;
			}
			const response = await fetchWrapper.image({
				url: `${baseUrl}/equipement/${id}/thumbnail`,
				useToken: "access",
			});
			this.thumbnailsURL[id] = URL.createObjectURL(response);
		},
	},
});

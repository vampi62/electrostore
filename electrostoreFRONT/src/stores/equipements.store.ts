import { defineStore } from "pinia";

import { fetchWrapper, createMainResource, createNestedResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useTagsStore, useStoresStore, useUsersStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Ready, Edition, WithFile } from "@/types/resource";
import type { components, paths } from "@/types/api";
type ReadEquipementDto = components["schemas"]["ReadEquipementDto"];
type ReadExtendedEquipementDto = components["schemas"]["ReadExtendedEquipementDto"];
type CreateEquipementDto = WithFile<NonNullable<paths["/api/equipement"]["post"]["requestBody"]>["content"]["multipart/form-data"], "img_file">;
type UpdateEquipementDto = WithFile<NonNullable<paths["/api/equipement/{id_equipement}"]["put"]["requestBody"]>["content"]["multipart/form-data"], "img_file">;

type ReadEquipementTagDto = components["schemas"]["ReadEquipementTagDto"];
type ReadExtendedEquipementTagDto = components["schemas"]["ReadExtendedEquipementTagDto"];
type CreateEquipementTagByEquipementDto = components["schemas"]["CreateEquipementTagByEquipementDto"];

type ReadEquipementBoxDto = components["schemas"]["ReadEquipementBoxDto"];
type ReadExtendedEquipementBoxDto = components["schemas"]["ReadExtendedEquipementBoxDto"];
type CreateEquipementBoxByEquipementDto = components["schemas"]["CreateEquipementBoxByEquipementDto"];

type ReadEquipementDocumentDto = components["schemas"]["ReadEquipementDocumentDto"];
type CreateEquipementDocumentByEquipementDto = WithFile<NonNullable<paths["/api/equipement/{id_equipement}/document"]["post"]["requestBody"]>["content"]["multipart/form-data"], "document">;
type UpdateEquipementDocumentByEquipementDto = components["schemas"]["UpdateEquipementDocumentDto"];

type ReadEquipementMaintenanceDto = components["schemas"]["ReadEquipementMaintenanceDto"];
type ReadExtendedEquipementMaintenanceDto = components["schemas"]["ReadExtendedEquipementMaintenanceDto"];
type CreateEquipementMaintenanceByEquipementDto = components["schemas"]["CreateEquipementMaintenanceByEquipementDto"];
type UpdateEquipementMaintenanceByEquipementDto = components["schemas"]["UpdateEquipementMaintenanceDto"];

type ReadEquipementCommentDto = components["schemas"]["ReadEquipementCommentDto"];
type ReadExtendedEquipementCommentDto = components["schemas"]["ReadExtendedEquipementCommentDto"];
type CreateEquipementCommentByEquipementDto = components["schemas"]["CreateEquipementCommentByEquipementDto"];
type UpdateEquipementCommentByEquipementDto = components["schemas"]["UpdateEquipementCommentDto"];

type ReadEquipementStatusDto = components["schemas"]["ReadEquipementStatusDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS: Record<string, (store: StoreGeneric, idEquipement: string, data: ReadExtendedEquipementDto) => void> = {
	equipement_tags: (store, idEquipement, data) => {
		store.equipementTags[idEquipement] = {};
		for (const equipementTag of data.equipement_tags ?? []) {
			if (!equipementTag.id_tag) {
				continue;
			}
			store.equipementTags[idEquipement][equipementTag.id_tag] = equipementTag;
		}
	},
	equipement_boxs: (store, idEquipement, data) => {
		store.equipementBoxs[idEquipement] = {};
		for (const equipementBox of data.equipement_boxs ?? []) {
			if (!equipementBox.id_box) {
				continue;
			}
			store.equipementBoxs[idEquipement][equipementBox.id_box] = equipementBox;
		}
	},
};

function hydrateEquipement(store: StoreGeneric, idEquipement: string, equipement: ReadExtendedEquipementDto, expand: string[] = []) {
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
			EXPAND_HANDLERS[key](store, idEquipement, equipement);
		}
	}
}

const equipementResource = createMainResource<{ readExtended: ReadExtendedEquipementDto, readBasic: ReadEquipementDto, create: CreateEquipementDto, update: UpdateEquipementDto }>({
	path: () => "/equipement",
	idField: "id_equipement",
	stateKey: "equipements",
	countKey: "equipementsTotalCount",
	loadingKey: "equipementsLoading",
	editionKey: "equipementEdition",
	onHydrate: (store, entity: ReadExtendedEquipementDto, expand) => {
		hydrateEquipement(store, String(entity.id_equipement), entity, expand);
	},
});

const equipementTagResource = createNestedResource<{ readExtended: ReadExtendedEquipementTagDto, readBasic: ReadEquipementTagDto, create: CreateEquipementTagByEquipementDto }>({
	path: (idEquipement) => `/equipement/${idEquipement}/tag`,
	idField: "id_tag",
	stateKey: "equipementTags",
	countKey: "equipementTagsTotalCount",
	loadingKey: "equipementTagsLoading",
	editionKey: "equipementTagEdition",
	readyKey: "equipementTagReady",
	onHydrate: (store, entity: ReadExtendedEquipementTagDto, expand) => {
		if (expand.includes("tag") && entity.tag && entity.id_tag) {
			const tagsStore = useTagsStore();
			tagsStore.tags[entity.id_tag] = entity.tag;
		}
	},
});

const equipementBoxResource = createNestedResource<{ readExtended: ReadExtendedEquipementBoxDto, readBasic: ReadEquipementBoxDto, create: CreateEquipementBoxByEquipementDto }>({
	path: (idEquipement) => `/equipement/${idEquipement}/box`,
	idField: "id_box",
	stateKey: "equipementBoxs",
	countKey: "equipementBoxsTotalCount",
	loadingKey: "equipementBoxsLoading",
	editionKey: "equipementBoxEdition",
	readyKey: "equipementBoxReady",
	onHydrate: (store, entity: ReadExtendedEquipementBoxDto, expand) => {
		if (expand.includes("box") && entity.box && entity.id_box && entity.box.id_store) {
			const storesStore = useStoresStore();
			storesStore.boxs[entity.box.id_store] ??= {};
			storesStore.boxs[entity.box.id_store][entity.id_box] = entity.box;
		}
	},
});

const equipementDocumentResource = createNestedResource<{ readBasic: ReadEquipementDocumentDto, create: CreateEquipementDocumentByEquipementDto, update: UpdateEquipementDocumentByEquipementDto }>({
	path: (idEquipement) => `/equipement/${idEquipement}/document`,
	idField: "id_equipement_document",
	stateKey: "equipementDocuments",
	countKey: "equipementDocumentsTotalCount",
	loadingKey: "equipementDocumentsLoading",
	editionKey: "equipementDocumentEdition",
	readyKey: "equipementDocumentReady",
});

const equipementMaintenanceResource = createNestedResource<{ readExtended: ReadExtendedEquipementMaintenanceDto, readBasic: ReadEquipementMaintenanceDto, create: CreateEquipementMaintenanceByEquipementDto, update: UpdateEquipementMaintenanceByEquipementDto }>({
	path: (idEquipement) => `/equipement/${idEquipement}/maintenance`,
	idField: "id_equipement_maintenance",
	stateKey: "equipementMaintenances",
	countKey: "equipementMaintenancesTotalCount",
	loadingKey: "equipementMaintenancesLoading",
	editionKey: "equipementMaintenanceEdition",
	readyKey: "equipementMaintenanceReady",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("user") && entity.user && entity.id_user) {
			const usersStore = useUsersStore();
			usersStore.users[entity.id_user] = entity.user;
		}
	},
});

const equipementCommentResource = createNestedResource<{ readExtended: ReadExtendedEquipementCommentDto, readBasic: ReadEquipementCommentDto, create: CreateEquipementCommentByEquipementDto, update: UpdateEquipementCommentByEquipementDto }>({
	path: (idEquipement) => `/equipement/${idEquipement}/comment`,
	idField: "id_equipement_comment",
	stateKey: "equipementComments",
	countKey: "equipementCommentsTotalCount",
	loadingKey: "equipementCommentsLoading",
	editionKey: "equipementCommentEdition",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("user") && entity.user && entity.id_user) {
			const usersStore = useUsersStore();
			usersStore.users[entity.id_user] = entity.user;
		}
	},
});

const equipementStatusHistoryResource = createNestedResource<{ readBasic: ReadEquipementStatusDto }>({
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
		equipementEdition: {} as Record<string, Edition<{ create: CreateEquipementDto, update: UpdateEquipementDto }>>,

		equipementTagsLoading: false,
		equipementTagsTotalCount: {} as Record<string, number>,
		equipementTags: {} as Record<string, Record<string, ReadEquipementTagDto>>,
		equipementTagEdition: {} as Record<string, Record<string, Edition<{ create: CreateEquipementTagByEquipementDto }>>>,
		equipementTagReady: {} as Record<string, Record<string, Ready<{ create: CreateEquipementTagByEquipementDto }>>>,

		equipementBoxsLoading: false,
		equipementBoxsTotalCount: {} as Record<string, number>,
		equipementBoxs: {} as Record<string, Record<string, ReadEquipementBoxDto>>,
		equipementBoxEdition: {} as Record<string, Record<string, Edition<{ create: CreateEquipementBoxByEquipementDto }>>>,
		equipementBoxReady: {} as Record<string, Record<string, Ready<{ create: CreateEquipementBoxByEquipementDto }>>>,

		equipementDocumentsLoading: false,
		equipementDocumentsTotalCount: {} as Record<string, number>,
		equipementDocuments: {} as Record<string, Record<string, ReadEquipementDocumentDto>>,
		equipementDocumentEdition: {} as Record<string, Record<string, Edition<{ create: CreateEquipementDocumentByEquipementDto, update: UpdateEquipementDocumentByEquipementDto }>>>,
		equipementDocumentReady: {} as Record<string, Record<string, Ready<{ create: CreateEquipementDocumentByEquipementDto, update: UpdateEquipementDocumentByEquipementDto }>>>,

		equipementMaintenancesLoading: false,
		equipementMaintenancesTotalCount: {} as Record<string, number>,
		equipementMaintenances: {} as Record<string, Record<string, ReadEquipementMaintenanceDto>>,
		equipementMaintenanceEdition: {} as Record<string, Record<string, Edition<{ create: CreateEquipementMaintenanceByEquipementDto, update: UpdateEquipementMaintenanceByEquipementDto }>>>,
		equipementMaintenanceReady: {} as Record<string, Record<string, Ready<{ create: CreateEquipementMaintenanceByEquipementDto, update: UpdateEquipementMaintenanceByEquipementDto }>>>,

		equipementCommentsLoading: false,
		equipementCommentsTotalCount: {} as Record<string, number>,
		equipementComments: {} as Record<string, Record<string, ReadEquipementCommentDto>>,
		equipementCommentEdition: {} as Record<string, Record<string, Record<string, Edition<{ create: CreateEquipementCommentByEquipementDto, update: UpdateEquipementCommentByEquipementDto }>>>>,

		equipementStatusHistoryLoading: false,
		equipementStatusHistoryTotalCount: {} as Record<string, number>,
		equipementStatusHistory: {} as Record<string, Record<string, ReadEquipementStatusDto>>,

		imagesURL: {} as Record<string, string>,
		thumbnailsURL: {} as Record<string, string>,
	}),
	actions: {
		getEquipementByInterval: equipementResource.getByInterval,
		getEquipementById: equipementResource.getById,
		createEquipement: equipementResource.create,
		getAvailableNewEquipementId: equipementResource.getAvailableNewId,
		updateEquipement: equipementResource.update,
		deleteEquipement: equipementResource.remove,
		loadToEdition(id: string, preset: string | null = null) {
			if (!isNewId(id) && this.equipements[id]) {
				this.equipementEdition[id] = {
					loading: false,
					reference_name_equipement: this.equipements[id].reference_name_equipement ?? undefined,
					friendly_name_equipement: this.equipements[id].friendly_name_equipement ?? undefined,
					description_equipement: this.equipements[id].description_equipement ?? undefined,
					status_equipement: this.equipements[id].status_equipement,
				};
			} else {
				this.equipementEdition[id] = {
					loading: false,
					status_equipement: 0,
				};
				equipementResource.loadEditionPreset.call(this, id, preset);
			}
			// a comment being edited is saved on its own, saving the element must not discard it
			this.equipementCommentEdition[id] ??= {};
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
			delete this.equipementCommentEdition[id];
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
			const { ...data } = this.equipementEdition[id];
			const imageChanged = !!data.img_file || !!data.unset_img_equipement;
			if (isNewId(id)) {
				realId = await this.createEquipement(data as CreateEquipementDto, true);
				this.copyEquipementTagAllId(id, realId);
				this.copyEquipementBoxAllId(id, realId);
				this.copyEquipementDocumentAllId(id, realId);
				this.copyEquipementMaintenanceAllId(id, realId);
			} else {
				await this.updateEquipement(id, data as UpdateEquipementDto, true);
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
				void this.showThumbnailById(realId);
				void this.showImageById(realId);
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

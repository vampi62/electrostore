import { defineStore } from "pinia";

import { fetchWrapper, createResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useEquipementsTagsStore, useEquipementsBoxsStore, useEquipementsDocumentsStore, useEquipementsMaintenancesStore, useEquipementsCommentsStore, useEquipementsStatusStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Edition, WithFile } from "@/types/resource";
import type { components, paths } from "@/types/api";
type ReadEquipementDto = components["schemas"]["ReadEquipementDto"];
type ReadExtendedEquipementDto = components["schemas"]["ReadExtendedEquipementDto"];
type CreateEquipementDto = WithFile<NonNullable<paths["/api/equipement"]["post"]["requestBody"]>["content"]["multipart/form-data"], "img_file">;
type UpdateEquipementDto = WithFile<NonNullable<paths["/api/equipement/{id_equipement}"]["put"]["requestBody"]>["content"]["multipart/form-data"], "img_file">;

const baseUrl = `${import.meta.env.VITE_API_URL}`;

// What an expanded equipement brings along is handed to the store of each nested resource
const EXPAND_HANDLERS: Record<string, (idEquipement: string, data: ReadExtendedEquipementDto) => void> = {
	equipement_tags: (idEquipement, data) => useEquipementsTagsStore().hydrateEquipementTags({ id_equipement: idEquipement }, data.equipement_tags ?? []),
	equipement_boxs: (idEquipement, data) => useEquipementsBoxsStore().hydrateEquipementBoxs({ id_equipement: idEquipement }, data.equipement_boxs ?? []),
};

function hydrateEquipement(store: StoreGeneric, idEquipement: string, equipement: ReadExtendedEquipementDto, expand: string[] = []) {
	if (equipement.url_thumbnail_equipement && !store.thumbnailsURL[idEquipement]) {
		store.showThumbnailById(idEquipement);
	}
	const scope = { id_equipement: idEquipement };
	useEquipementsTagsStore().hydrateEquipementTags(scope, undefined, equipement.equipement_tags_count);
	useEquipementsBoxsStore().hydrateEquipementBoxs(scope, undefined, equipement.equipement_boxs_count);
	useEquipementsDocumentsStore().hydrateEquipementDocuments(scope, undefined, equipement.equipement_documents_count);
	useEquipementsMaintenancesStore().hydrateEquipementMaintenances(scope, undefined, equipement.equipement_maintenances_count);
	useEquipementsStatusStore().hydrateEquipementStatus(scope, undefined, equipement.equipement_status_history_count);
	useEquipementsCommentsStore().hydrateEquipementComments(scope, undefined, equipement.equipement_comments_count);
	for (const key of expand) {
		EXPAND_HANDLERS[key]?.(idEquipement, equipement);
	}
}

const equipementResource = createResource<{ readExtended: ReadExtendedEquipementDto, readBasic: ReadEquipementDto, create: CreateEquipementDto, update: UpdateEquipementDto }>({
	compositeKey: ["id_equipement"],
	generatedField: "id_equipement",
	path: [{ keys: [], url: () => "/equipement" }],
	entityPath: [{ keys: ["id_equipement"], url: ({ id_equipement }) => `/equipement/${id_equipement}` }],
	stateKey: "equipements",
	countKey: "equipementsTotalCount",
	loadingKey: "equipementsLoading",
	editionKey: "equipementEdition",
	onHydrate: (store: StoreGeneric, entity: ReadExtendedEquipementDto, expand) => {
		hydrateEquipement(store, String(entity.id_equipement), entity, expand);
	},
});

// Discards what is staged for the nested resources of an equipement; a comment being edited is saved on its own, saving the element must not discard it
function clearNestedEdition(idEquipement: string) {
	const scope = { id_equipement: idEquipement };
	useEquipementsTagsStore().clearEquipementTagEdition(scope);
	useEquipementsBoxsStore().clearEquipementBoxEdition(scope);
	useEquipementsDocumentsStore().clearEquipementDocumentEdition(scope);
	useEquipementsMaintenancesStore().clearEquipementMaintenanceEdition(scope);
}

export const useEquipementsStore = defineStore("equipements", {
	state: () => ({
		equipementsLoading: false,
		equipementsTotalCount: 0,
		equipements: {} as Record<string, ReadEquipementDto>,
		equipementEdition: {} as Record<string, Edition<{ create: CreateEquipementDto, update: UpdateEquipementDto }>>,

		imagesURL: {} as Record<string, string>,
		thumbnailsURL: {} as Record<string, string>,
	}),
	actions: {
		getEquipementByInterval: equipementResource.single.getByInterval,
		getEquipementById: equipementResource.single.getById,
		createEquipement: equipementResource.single.create,
		getAvailableNewEquipementId: equipementResource.single.getAvailableNewId,
		updateEquipement: equipementResource.single.update,
		deleteEquipement: equipementResource.single.remove,
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
				equipementResource.single.loadEditionPreset.call(this, id, preset);
			}
			clearNestedEdition(id);
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.equipementEdition[id]) {
				this.equipementEdition[id] = {};
			}
			this.equipementEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.equipementEdition[id];
			clearNestedEdition(id);
			useEquipementsCommentsStore().clearEquipementCommentEdition({ id_equipement: id });
		},
		async saveAllChanges(id: string) {
			let realId = id;
			const { ...data } = this.equipementEdition[id];
			const imageChanged = !!data.img_file || !!data.unset_img_equipement;
			const tagsStore = useEquipementsTagsStore();
			const boxsStore = useEquipementsBoxsStore();
			const documentsStore = useEquipementsDocumentsStore();
			const maintenancesStore = useEquipementsMaintenancesStore();
			if (isNewId(id)) {
				realId = await this.createEquipement(data as CreateEquipementDto, true);
				// what was staged for the new equipement now belongs to the saved one
				tagsStore.copyEquipementTagByKeyPart("id_equipement", id, realId);
				boxsStore.copyEquipementBoxByKeyPart("id_equipement", id, realId);
				documentsStore.copyEquipementDocumentByKeyPart("id_equipement", id, realId);
				maintenancesStore.copyEquipementMaintenanceByKeyPart("id_equipement", id, realId);
			} else {
				await this.updateEquipement(id, data as UpdateEquipementDto, true);
			}
			const scope = { id_equipement: realId };
			await Promise.all([
				tagsStore.pushEquipementTagChange(scope),
				boxsStore.pushEquipementBoxChange(scope),
				documentsStore.pushEquipementDocumentChange(scope),
				maintenancesStore.pushEquipementMaintenanceChange(scope),
			]);
			if (imageChanged) {
				delete this.thumbnailsURL[realId];
				delete this.imagesURL[realId];
				// reload in the background (not awaited); allSettled never rejects, so no unhandled rejection
				Promise.allSettled([this.showThumbnailById(realId)]);
			}
			return realId;
		},

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

import { defineStore } from "pinia";

import { fetchWrapper, createResource } from "@/helpers";

import type { Edition, Ready, WithFile } from "@/types/resource";
import type { components, paths } from "@/types/api";

type ReadEquipementDocumentDto = components["schemas"]["ReadEquipementDocumentDto"];
type CreateEquipementDocumentByEquipementDto = WithFile<NonNullable<paths["/api/equipement/{id_equipement}/document"]["post"]["requestBody"]>["content"]["multipart/form-data"], "document">;
type UpdateEquipementDocumentDto = components["schemas"]["UpdateEquipementDocumentDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const equipementDocumentResource = createResource<{ readBasic: ReadEquipementDocumentDto, create: CreateEquipementDocumentByEquipementDto, update: UpdateEquipementDocumentDto }>({
	compositeKey: ["id_equipement", "id_equipement_document"],
	generatedField: "id_equipement_document",
	path: [
		{ keys: ["id_equipement"], url: ({ id_equipement }) => `/equipement/${id_equipement}/document` },
	],
	entityPath: [
		{ keys: ["id_equipement", "id_equipement_document"], url: ({ id_equipement, id_equipement_document }) => `/equipement/${id_equipement}/document/${id_equipement_document}` },
	],
	stateKey: "equipementDocuments",
	countKey: "equipementDocumentsTotalCount",
	loadingKey: "equipementDocumentsLoading",
	editionKey: "equipementDocumentEdition",
	readyKey: "equipementDocumentReady",
});

// Records are indexed by the composite key "<id_equipement>:<id_equipement_document>"
export const useEquipementsDocumentsStore = defineStore("equipementsDocuments", {
	state: () => ({
		equipementDocumentsLoading: false,
		// indexed by scope: "id_equipement=3"
		equipementDocumentsTotalCount: {} as Record<string, number>,
		equipementDocuments: {} as Record<string, ReadEquipementDocumentDto>,
		equipementDocumentEdition: {} as Record<string, Edition<{ create: CreateEquipementDocumentByEquipementDto, update: UpdateEquipementDocumentDto }>>,
		equipementDocumentReady: {} as Record<string, Ready<{ create: CreateEquipementDocumentByEquipementDto, update: UpdateEquipementDocumentDto }>>,
	}),
	getters: {
		// the records of a equipement, indexed by the rest of the key
		equipementDocumentsByEquipement: (state) => (id: string) => equipementDocumentResource.slice(state.equipementDocuments, { id_equipement: id }),
		equipementDocumentsCountByEquipement: (state) => (id: string) => state.equipementDocumentsTotalCount[equipementDocumentResource.toScopeKey({ id_equipement: id })] ?? 0,
		equipementDocumentEditionByEquipement: (state) => (id: string) => equipementDocumentResource.live(state.equipementDocumentEdition, { id_equipement: id }),
		equipementDocumentReadyByEquipement: (state) => (id: string) => equipementDocumentResource.live(state.equipementDocumentReady, { id_equipement: id }),
	},
	actions: {
		getEquipementDocumentByInterval: equipementDocumentResource.getByInterval,
		getEquipementDocumentById: equipementDocumentResource.getById,
		createEquipementDocument: equipementDocumentResource.create,
		updateEquipementDocument: equipementDocumentResource.update,
		deleteEquipementDocument: equipementDocumentResource.remove,
		hydrateEquipementDocuments: equipementDocumentResource.hydrate,
		getAvailableNewEquipementDocumentId: equipementDocumentResource.getAvailableNewId,
		setEquipementDocumentEdition: equipementDocumentResource.setEdition,
		removeEquipementDocumentEdition: equipementDocumentResource.removeEdition,
		removeEquipementDocumentReady: equipementDocumentResource.removeReady,
		setEquipementDocumentStagedSlice: equipementDocumentResource.setStagedSlice,
		valideEquipementDocumentEditionByKey: equipementDocumentResource.valideEditionByKey,
		copyEquipementDocumentByKeyPart: equipementDocumentResource.copyByKeyPart,
		clearEquipementDocumentEdition: equipementDocumentResource.clearEdition,
		pushEquipementDocumentChange: equipementDocumentResource.pushChange,
		async downloadEquipementDocument(idEquipement: string, id: string) {
			return await fetchWrapper.image({
				url: `${baseUrl}/equipement/${idEquipement}/document/${id}/download`,
				useToken: "access",
			});
		},
	},
});

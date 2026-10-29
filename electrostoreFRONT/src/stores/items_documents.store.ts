import { defineStore } from "pinia";

import { fetchWrapper, createResource } from "@/helpers";

import type { Ready, Edition, WithFile } from "@/types/resource";
import type { components, paths } from "@/types/api";

type ReadItemDocumentDto = components["schemas"]["ReadItemDocumentDto"];
type CreateItemDocumentByItemDto = WithFile<NonNullable<paths["/api/item/{id_item}/document"]["post"]["requestBody"]>["content"]["multipart/form-data"], "document">;
type UpdateItemDocumentDto = components["schemas"]["UpdateItemDocumentDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const itemDocumentResource = createResource<{ readBasic: ReadItemDocumentDto, create: CreateItemDocumentByItemDto, update: UpdateItemDocumentDto }>({
	compositeKey: ["id_item", "id_item_document"],
	generatedField: "id_item_document",
	path: [{ keys: ["id_item"], url: ({ id_item }) => `/item/${id_item}/document` }],
	entityPath: [{ keys: ["id_item", "id_item_document"], url: ({ id_item, id_item_document }) => `/item/${id_item}/document/${id_item_document}` }],
	stateKey: "itemDocuments",
	countKey: "itemDocumentsTotalCount",
	loadingKey: "itemDocumentsLoading",
	editionKey: "itemDocumentEdition",
	readyKey: "itemDocumentReady",
});

// Records are indexed by the composite key "<id_item>:<id_item_document>"
export const useItemsDocumentsStore = defineStore("itemsDocuments", {
	state: () => ({
		itemDocumentsLoading: false,
		// indexed by scope: "id_item=3"
		itemDocumentsTotalCount: {} as Record<string, number>,
		itemDocuments: {} as Record<string, ReadItemDocumentDto>,
		itemDocumentEdition: {} as Record<string, Edition<{ create: CreateItemDocumentByItemDto, update: UpdateItemDocumentDto }>>,
		itemDocumentReady: {} as Record<string, Ready<{ create: CreateItemDocumentByItemDto, update: UpdateItemDocumentDto }>>,
	}),
	getters: {
		// the records of an item, indexed by id_item_document
		itemDocumentsByItem: (state) => (idItem: string) => itemDocumentResource.slice(state.itemDocuments, { id_item: idItem }),
		itemDocumentEditionByItem: (state) => (idItem: string) => itemDocumentResource.slice(state.itemDocumentEdition, { id_item: idItem }),
		itemDocumentReadyByItem: (state) => (idItem: string) => itemDocumentResource.slice(state.itemDocumentReady, { id_item: idItem }),
		itemDocumentsCountByItem: (state) => (idItem: string) => state.itemDocumentsTotalCount[itemDocumentResource.toScopeKey({ id_item: idItem })] ?? 0,
	},
	actions: {
		getItemDocumentByInterval: itemDocumentResource.getByInterval,
		getItemDocumentById: itemDocumentResource.getById,
		createItemDocument: itemDocumentResource.create,
		updateItemDocument: itemDocumentResource.update,
		deleteItemDocument: itemDocumentResource.remove,
		hydrateItemDocuments: itemDocumentResource.hydrate,
		getAvailableNewItemDocumentId: itemDocumentResource.getAvailableNewId,
		setItemDocumentEdition: itemDocumentResource.setEdition,
		removeItemDocumentEdition: itemDocumentResource.removeEdition,
		removeItemDocumentReady: itemDocumentResource.removeReady,
		setItemDocumentStagedSlice: itemDocumentResource.setStagedSlice,
		valideItemDocumentEditionByKey: itemDocumentResource.valideEditionByKey,
		copyItemDocumentByKeyPart: itemDocumentResource.copyByKeyPart,
		clearItemDocumentEdition: itemDocumentResource.clearEdition,
		pushItemDocumentChange: itemDocumentResource.pushChange,
		async downloadItemDocument(idItem: string, id: string) {
			return await fetchWrapper.image({
				url: `${baseUrl}/item/${idItem}/document/${id}/download`,
				useToken: "access",
			});
		},
	},
});

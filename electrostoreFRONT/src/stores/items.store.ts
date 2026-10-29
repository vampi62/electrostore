import { defineStore } from "pinia";

import { fetchWrapper, createResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useItemsDocumentsStore, useItemsBoxsStore, useItemsTagsStore, useItemsCommandsStore, useItemsProjectsStore, useItemsHistoryStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Edition, WithFile } from "@/types/resource";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";
import type { FetchPage } from "@/types/fetch";
import type { components, paths } from "@/types/api";
type ReadItemDto = components["schemas"]["ReadItemDto"];
type ReadExtendedItemDto = components["schemas"]["ReadExtendedItemDto"];
type CreateItemDto = WithFile<NonNullable<paths["/api/item"]["post"]["requestBody"]>["content"]["multipart/form-data"], "img_file">;
type UpdateItemDto = WithFile<NonNullable<paths["/api/item/{id_item}"]["put"]["requestBody"]>["content"]["multipart/form-data"], "img_file">;

const baseUrl = `${import.meta.env.VITE_API_URL}`;

// What an expanded item brings along is handed to the store of each nested resource
const EXPAND_HANDLERS: Record<string, (idItem: string, data: ReadExtendedItemDto) => void> = {
	item_documents: (idItem, data) => useItemsDocumentsStore().hydrateItemDocuments({ id_item: idItem }, data.item_documents ?? []),
	item_boxs: (idItem, data) => useItemsBoxsStore().hydrateItemBoxs({ id_item: idItem }, data.item_boxs ?? []),
	item_tags: (idItem, data) => useItemsTagsStore().hydrateItemTags({ id_item: idItem }, data.item_tags ?? []),
	command_items: (idItem, data) => useItemsCommandsStore().hydrateItemCommands({ id_item: idItem }, data.command_items ?? []),
	project_items: (idItem, data) => useItemsProjectsStore().hydrateItemProjects({ id_item: idItem }, data.project_items ?? []),
	item_history: (idItem, data) => useItemsHistoryStore().hydrateItemHistory({ id_item: idItem }, data.item_history ?? []),
};

function hydrateItem(store: StoreGeneric, idItem: string, item: ReadExtendedItemDto, expand: string[] = []) {
	if (item.url_thumbnail_item && !store.thumbnailsURL[idItem]) {
		store.showThumbnailById(idItem);
	}
	const scope = { id_item: idItem };
	useItemsDocumentsStore().hydrateItemDocuments(scope, undefined, item.item_documents_count);
	useItemsBoxsStore().hydrateItemBoxs(scope, undefined, item.item_boxs_count);
	useItemsTagsStore().hydrateItemTags(scope, undefined, item.item_tags_count);
	useItemsCommandsStore().hydrateItemCommands(scope, undefined, item.command_items_count);
	useItemsProjectsStore().hydrateItemProjects(scope, undefined, item.project_items_count);
	for (const key of expand) {
		EXPAND_HANDLERS[key]?.(idItem, item);
	}
}

const itemResource = createResource<{ readExtended: ReadExtendedItemDto, readBasic: ReadItemDto, create: CreateItemDto, update: UpdateItemDto }>({
	compositeKey: ["id_item"],
	generatedField: "id_item",
	path: [{ keys: [], url: () => "/item" }],
	entityPath: [{ keys: ["id_item"], url: ({ id_item }) => `/item/${id_item}` }],
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

// Discards what is staged for the nested resources of an item
function clearNestedEdition(idItem: string) {
	const scope = { id_item: idItem };
	useItemsDocumentsStore().clearItemDocumentEdition(scope);
	useItemsBoxsStore().clearItemBoxEdition(scope);
	useItemsTagsStore().clearItemTagEdition(scope);
	useItemsCommandsStore().clearItemCommandEdition(scope);
	useItemsProjectsStore().clearItemProjectEdition(scope);
}

export const useItemsStore = defineStore("items", {
	state: () => ({
		itemsLoading: false,
		itemsTotalCount: 0,
		items: {} as Record<string, ReadItemDto>,
		itemEdition: {} as Record<string, Edition<{ create: CreateItemDto, update: UpdateItemDto }>>,

		imagesURL: {} as Record<string, string>,
		thumbnailsURL: {} as Record<string, string>,
	}),
	actions: {
		getItemByInterval(limit = 100, offset = 0, expand: string[] = [], filter: RSQLFilter[] = [], sort: RSQLSort = {}, clear = false): Promise<FetchPage> {
			return itemResource.getByInterval.call(this, {}, limit, offset, expand, filter, sort, clear);
		},
		getItemById(id: string, expand: string[] = []): Promise<void> {
			return itemResource.getById.call(this, { id_item: id }, expand);
		},
		createItem(params: CreateItemDto, isFormData = false): Promise<string> {
			return itemResource.create.call(this, {}, params, isFormData);
		},
		getAvailableNewItemId(): string {
			return itemResource.getAvailableNewId.call(this);
		},
		updateItem(id: string, params: UpdateItemDto, isFormData = false): Promise<void> {
			return itemResource.update.call(this, { id_item: id }, params, isFormData);
		},
		deleteItem(id: string): Promise<void> {
			return itemResource.remove.call(this, { id_item: id });
		},
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
				itemResource.loadEditionPreset.call(this, { id_item: id }, preset);
			}
			clearNestedEdition(id);
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.itemEdition[id]) {
				this.itemEdition[id] = {};
			}
			this.itemEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.itemEdition[id];
			clearNestedEdition(id);
		},
		async saveAllChanges(id: string) {
			let realId = id;
			const { isFormData, ...data } = this.itemEdition[id];
			const imageChanged = !!data.img_file || !!data.unset_img_item;
			const documentsStore = useItemsDocumentsStore();
			const boxsStore = useItemsBoxsStore();
			const tagsStore = useItemsTagsStore();
			const commandsStore = useItemsCommandsStore();
			const projectsStore = useItemsProjectsStore();
			if (isNewId(id)) {
				realId = await this.createItem(data as CreateItemDto, isFormData);
				// what was staged for the new item now belongs to the saved one
				documentsStore.copyItemDocumentByKeyPart("id_item", id, realId);
				boxsStore.copyItemBoxByKeyPart("id_item", id, realId);
				tagsStore.copyItemTagByKeyPart("id_item", id, realId);
				commandsStore.copyItemCommandByKeyPart("id_item", id, realId);
				projectsStore.copyItemProjectByKeyPart("id_item", id, realId);
			} else {
				await this.updateItem(id, data as UpdateItemDto, isFormData);
			}
			const scope = { id_item: realId };
			await Promise.all([
				documentsStore.pushItemDocumentChange(scope),
				boxsStore.pushItemBoxChange(scope),
				tagsStore.pushItemTagChange(scope),
				commandsStore.pushItemCommandChange(scope),
				projectsStore.pushItemProjectChange(scope),
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
	},
});

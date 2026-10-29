import { defineStore } from "pinia";

import { createResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useStoresTagsStore, useBoxsTagsStore, useItemsTagsStore, useEquipementsTagsStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Edition } from "@/types/resource";
import type { components } from "@/types/api";
type ReadTagDto = components["schemas"]["ReadTagDto"];
type ReadExtendedTagDto = components["schemas"]["ReadExtendedTagDto"];
type CreateTagDto = components["schemas"]["CreateTagDto"];
type UpdateTagDto = components["schemas"]["UpdateTagDto"];

// What an expanded tag brings along is handed to the store of each nested resource
const EXPAND_HANDLERS: Record<string, (idTag: string, data: ReadExtendedTagDto) => void> = {
	stores_tags: (idTag, data) => useStoresTagsStore().hydrateStoreTags({ id_tag: idTag }, data.stores_tags ?? []),
	boxs_tags: (idTag, data) => useBoxsTagsStore().hydrateBoxTags({ id_tag: idTag }, data.boxs_tags ?? []),
	items_tags: (idTag, data) => useItemsTagsStore().hydrateItemTags({ id_tag: idTag }, data.items_tags ?? []),
	equipement_tags: (idTag, data) => useEquipementsTagsStore().hydrateEquipementTags({ id_tag: idTag }, data.equipement_tags ?? []),
};

function hydrateTag(idTag: string, tag: ReadExtendedTagDto, expand: string[] = []) {
	const scope = { id_tag: idTag };
	useStoresTagsStore().hydrateStoreTags(scope, undefined, tag.stores_tags_count);
	useBoxsTagsStore().hydrateBoxTags(scope, undefined, tag.boxs_tags_count);
	useItemsTagsStore().hydrateItemTags(scope, undefined, tag.items_tags_count);
	useEquipementsTagsStore().hydrateEquipementTags(scope, undefined, tag.equipement_tags_count);
	for (const key of expand) {
		EXPAND_HANDLERS[key]?.(idTag, tag);
	}
}

const tagResource = createResource<{ readExtended: ReadExtendedTagDto, readBasic: ReadTagDto, create: CreateTagDto, update: UpdateTagDto, createBulk: CreateTagDto[] }>({
	compositeKey: ["id_tag"],
	generatedField: "id_tag",
	path: [{ keys: [], url: () => "/tag" }],
	entityPath: [{ keys: ["id_tag"], url: ({ id_tag }) => `/tag/${id_tag}` }],
	stateKey: "tags",
	countKey: "tagsTotalCount",
	loadingKey: "tagsLoading",
	editionKey: "tagEdition",
	onHydrate: (store: StoreGeneric, entity: ReadExtendedTagDto, expand) => {
		if (entity.id_tag === undefined) {
			return;
		}
		hydrateTag(String(entity.id_tag), entity, expand);
	},
});

// Discards what is staged for the nested resources of a tag
function clearNestedEdition(idTag: string) {
	const scope = { id_tag: idTag };
	useItemsTagsStore().clearItemTagEdition(scope);
	useStoresTagsStore().clearStoreTagEdition(scope);
	useBoxsTagsStore().clearBoxTagEdition(scope);
	useEquipementsTagsStore().clearEquipementTagEdition(scope);
}

export const useTagsStore = defineStore("tags", {
	state: () => ({
		tagsLoading: false,
		tagsTotalCount: 0,
		tags: {} as Record<string, ReadTagDto>,
		tagEdition: {} as Record<string, Edition<{ create: CreateTagDto, update: UpdateTagDto }>>,
	}),
	actions: {
		getTagByInterval: tagResource.single.getByInterval,
		getTagById: tagResource.single.getById,
		createTag: tagResource.single.create,
		getAvailableNewTagId: tagResource.single.getAvailableNewId,
		updateTag: tagResource.single.update,
		deleteTag: tagResource.single.remove,
		createTagBulk: tagResource.single.createBulk,
		loadToEdition(id: string, preset: string | null = null) {
			if (!isNewId(id) && this.tags[id]) {
				this.tagEdition[id] = {
					name_tag: this.tags[id].name_tag ?? undefined,
					weight_tag: this.tags[id].weight_tag,
					loading: false,
				};
			} else {
				this.tagEdition[id] = {
					loading: false,
				};
				tagResource.single.loadEditionPreset.call(this, id, preset);
			}
			clearNestedEdition(id);
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.tagEdition[id]) {
				this.tagEdition[id] = {};
			}
			this.tagEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.tagEdition[id];
			clearNestedEdition(id);
		},
		async saveAllChanges(id: string) {
			let realId = id;
			const storesTagsStore = useStoresTagsStore();
			const boxsTagsStore = useBoxsTagsStore();
			const itemsTagsStore = useItemsTagsStore();
			const equipementsTagsStore = useEquipementsTagsStore();
			if (isNewId(id)) {
				realId = await this.createTag(this.tagEdition[id] as CreateTagDto);
				// what was staged for the new tag now belongs to the saved one
				storesTagsStore.copyStoreTagByKeyPart("id_tag", id, realId);
				boxsTagsStore.copyBoxTagByKeyPart("id_tag", id, realId);
				itemsTagsStore.copyItemTagByKeyPart("id_tag", id, realId);
				equipementsTagsStore.copyEquipementTagByKeyPart("id_tag", id, realId);
			} else {
				await this.updateTag(id, this.tagEdition[id]);
			}
			const scope = { id_tag: realId };
			await Promise.all([
				storesTagsStore.pushStoreTagChange(scope),
				boxsTagsStore.pushBoxTagChange(scope),
				itemsTagsStore.pushItemTagChange(scope),
				equipementsTagsStore.pushEquipementTagChange(scope),
			]);
			return realId;
		},
	},
});

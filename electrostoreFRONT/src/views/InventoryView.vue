<script setup>
import { onMounted, ref } from "vue";

import { useItemsStore, useTagsStore } from "@/stores";
const itemsStore = useItemsStore();
const tagsStore = useTagsStore();

async function fetchTagData(minOffset, maxOffset) {
	let tagsNotFound = [];
	for (let id = minOffset; id < maxOffset; id++) {
		for (const tag in itemsStore.itemTags[id]) {
			if (!tagsStore.tags[tag]) {
				tagsNotFound.push(tag);
			}
		}
	}
	if (tagsNotFound.length > 0) {
		await tagsStore.getTagByList(tagsNotFound);
	}
}

const filter = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "items.FilterName", compareMethod: "=like=" },
	{ key: "friendly_name_item", value: "", type: "text", label: "items.FilterFriendlyName", compareMethod: "=like=" },
	{ key: "seuil_min_item", value: "", type: "number", label: "items.FilterSeuilMin", compareMethod: "=ge=" },
	{ key: "seuil_min_item", value: "", type: "number", label: "items.FilterSeuilMax", compareMethod: "=le=" },
	{ key: "ItemsTags.Tag.nom_tag", value: "", type: "text", label: "items.FilterTag", compareMethod: "=like=" },
]);
const tableauLabel = ref([
	{ label: "items.Name", sortable: true, key: "reference_name_item", valueKey: "reference_name_item", type: "text" },
	{ label: "items.FriendlyName", sortable: true, key: "friendly_name_item", valueKey: "friendly_name_item", type: "text" },
	{ label: "items.Seuil", sortable: true, key: "seuil_min_item", valueKey: "seuil_min_item", type: "number" },
	{ label: "items.Description", sortable: false, key: "description_item", valueKey: "description_item", type: "text" },

	{ label: "items.Tags", sortable: false, key: "ItemsTags.Tag.nom_tag", sourceKey: "id_item", type: "link-list", 
		storeLinkId: 1, storeRessourceId: 2, storeLinkKeyJoinSource: "id_item", storeLinkKeyJoinRessource: "id_tag", valueKey: "nom_tag",
		ressourcePrint: [{ from: "ressource", valueKey: "nom_tag" }] },

	{ label: "items.Img", sortable: false, key: "id_img", sourceKey: "id_img", type: "image", 
		storeRessourceId: 3, valueKey: "id_img" },

	{ label: "items.Quantity", sortable: false, key: "quantity_item", valueKey: "quantity_item", type: "number" },
]);
const tableauMeta = ref({
	key: "id_item",
	path: "/inventory/",
	expand: ["item_boxs", "item_tags"],
});
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('items.Title') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/inventory/new'">
				{{ $t('items.Add') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="itemsStore.items" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[itemsStore.items,itemsStore.itemTags,tagsStore.tags,itemsStore.thumbnailsURL]"
		:filters="filter"
		:loading="itemsStore.itemsLoading"
		:total-count="Number(itemsStore.itemsTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => itemsStore.getItemByInterval(limit, offset, expand, filter, sort, clear)"
		:list-fetch-function="[(minOffset, maxOffset) => fetchTagData(minOffset, maxOffset)]"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>

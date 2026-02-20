<script setup>
import { onMounted, ref } from "vue";

import { useItemsStore, useTagsStore } from "@/stores";
const itemsStore = useItemsStore();
const tagsStore = useTagsStore();

async function updateQuantityData(minOffset, maxOffset) {
	for (const id = minOffset; id < maxOffset; id++) {
		const item = itemsStore.items[id];
		if (item) {
			item.custom_quantity_item = getTotalQuantity(item.item_boxs);
		}
	}
}
async function fetchTagData(minOffset, maxOffset) {
	let tagsNotFound = [];
	for (const id = minOffset; id < maxOffset; id++) {
		for (const tag in itemsStore.itemTags[id]) {
			if (!tagsStore.tags[tag]) {
				tagsNotFound.push(tag);
			}
		}
	}
	if (tagsNotFound.length > 0) {
		await tagsStore.getTagByList(tagsNotFound);
	}
	filter.value[6].options = Object.fromEntries(Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag]));
}

function getTotalQuantity(itembox) {
	if (!itembox) {
		return 0;
	}
	return itembox.reduce((total, box) => total + box.qte_item_box, 0);
}

const filter = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "item.VInventoryFilterName", compareMethod: "contain" },
	{ key: "friendly_name_item", value: "", type: "text", label: "item.VInventoryFilterFriendlyName", compareMethod: "contain" },
	{ key: "seuil_min_item", value: "", type: "number", label: "item.VInventoryFilterSeuilMin", compareMethod: ">=" },
	{ key: "seuil_min_item", value: "", type: "number", label: "item.VInventoryFilterSeuilMax", compareMethod: "<=" },
	{ key: "qte_item_box", subPath: "item_boxs", value: "", type: "number", label: "item.VInventoryFilterQuantityMin", compareMethod: ">=" },
	{ key: "qte_item_box", subPath: "item_boxs", value: "", type: "number", label: "item.VInventoryFilterQuantityMax", compareMethod: "<=" },
	{ key: "id_tag", subPath: "item_tags", value: "", type: "datalist", typeData: "int", options: Object.fromEntries(Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag])), label: "item.VInventoryFilterTag", compareMethod: "=" },
]);
const tableauLabel = ref([
	{ label: "item.VInventoryName", sortable: true, key: "reference_name_item", type: "text" },
	{ label: "item.VInventoryFriendlyName", sortable: true, key: "friendly_name_item", type: "text" },
	{ label: "item.VInventorySeuil", sortable: true, key: "seuil_min_item", type: "number" },
	{ label: "item.VInventoryDescription", sortable: false, key: "description_item", type: "text" },
	{ label: "item.VInventoryTags", sortable: false, key: "", type: "list", list: { idStoreLink: 1, idStoreRessource: 2, key: "id_item", keyStoreLink: "id_tag", ressourcePrint: [{ type: "ressource", key: "nom_tag" }] } },
	{ label: "item.VInventoryImg", sortable: false, key: "id_img", type: "image", idStoreImg: 3, store: 4, keyStore: "id_item" },
	{ label: "item.VInventoryQuantity", sortable: true, key: "custom_quantity_item", type: "number" },
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
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('item.VInventoryTitle') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/inventory/new'">
				{{ $t('item.VInventoryAdd') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="itemsStore.items" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[itemsStore.items,itemsStore.itemTags,tagsStore.tags,itemsStore.thumbnailsURL,itemsStore.items]"
		:filters="filter"
		:loading="itemsStore.itemsLoading"
		:total-count="Number(itemsStore.itemsTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => itemsStore.getItemByInterval(limit, offset, expand, filter, sort, clear)"
		:list-fetch-function="[(minOffset, maxOffset) => fetchTagData(minOffset, maxOffset), (minOffset, maxOffset) => updateQuantityData(minOffset, maxOffset)]"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>

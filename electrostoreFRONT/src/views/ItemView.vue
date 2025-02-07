<script setup>
import { onMounted, onBeforeUnmount, ref, computed, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const itemId = route.params.id;

import { useItemsStore, useTagsStore, useStoresStore, useCommandsStore, useProjetsStore } from "@/stores";
const itemsStore = useItemsStore();
const tagsStore = useTagsStore();
const storesStore = useStoresStore();
const commandsStore = useCommandsStore();
const projetsStore = useProjetsStore();

async function fetchData() {
	if (itemId !== "new") {
		itemsStore.itemEdition = {
			loading: false,
		};
		try {
			await itemsStore.getItemById(itemId);
		} catch {
			delete itemsStore.items[itemId];
			addNotification({ message: "item.VItemNotFound", type: "error", i18n: true });
			router.push("/items");
			return;
		}
		itemsStore.getItemTagByInterval(itemId, 100, 0, ["tag"]);
		itemsStore.itemEdition = {
			loading: false,
			nom_item: itemsStore.items[itemId].nom_item,
			description_item: itemsStore.items[itemId].description_item,
			prix_item: itemsStore.items[itemId].prix_item,
			stock_item: itemsStore.items[itemId].stock_item,
		};
	} else {
		itemsStore.itemEdition = {
			loading: false,
		};
	}
}
onMounted(() => {
	fetchData();
});
onBeforeUnmount(() => {
	itemsStore.itemEdition = {
		loading: false,
	};
});
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('item.VItemTitle') }}</h2>
		<div class="flex space-x-4">
			<button type="button" @click="itemSave" v-if="itemId == 'new'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="itemsStore.itemEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('item.VItemAdd') }}
			</button>
			<button type="button" @click="itemUpdate" v-else
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="itemsStore.itemEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('item.VItemUpdate') }}
			</button>
			<button type="button" @click="itemDeleteOpenModal" v-if="itemId != 'new'"
				class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">
				{{ $t('item.VItemDelete') }}
			</button>
			<RouterLink to="/inventory"
				class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">
				{{ $t('item.VItemBack') }}
			</RouterLink>
		</div>
	</div>
</template>

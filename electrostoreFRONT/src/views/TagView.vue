<script setup>
import { onMounted, onBeforeUnmount, ref, computed, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const tagId = route.params.id;

import { useTagsStore, useStoresStore, useItemsStore } from "@/stores";
const tagsStore = useTagsStore();
const storesStore = useStoresStore();
const itemsStore = useItemsStore();

async function fetchData() {
	if (tagId !== "new") {
		tagsStore.tagEdition = {
			loading: false,
		};
		try {
			await tagsStore.getTagById(tagId);
		} catch {
			delete tagsStore.tags[tagId];
			addNotification({ message: "tag.VTagUpdated", type: "error", i18n: true });
			router.push("/tags");
			return;
		}
		tagsStore.getTagItemByInterval(tagId, 100, 0, ["item"]);
		tagsStore.tagEdition = {
			loading: false,
			nom_tag: tagsStore.tags[tagId].nom_tag,
			description_tag: tagsStore.tags[tagId].description_tag,
			store_id: tagsStore.tags[tagId].store_id,
		};
	} else {
		tagsStore.tagEdition = {
			loading: false,
		};
	}
}
onMounted(() => {
	fetchData();
});
onBeforeUnmount(() => {
	tagsStore.tagEdition = {
		loading: false,
	};
});
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('tag.VTagTitle') }}</h2>
		<div class="flex space-x-4">
			<button type="button" @click="tagSave" v-if="tagId == 'new'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="tagsStore.tagEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>{{
						$t('tag.VTagAdd') }}</button>
			<button type="button" @click="tagUpdate" v-else
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="tagsStore.tagEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>{{
						$t('tag.VTagUpdate') }}</button>
			<button type="button" @click="tagDeleteOpenModal" v-if="tagId != 'new'"
				class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">{{ $t('tag.VTagDelete') }}</button>
			<RouterLink to="/tags"
				class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">{{
					$t('tag.VTagBack') }}</RouterLink>
		</div>
	</div>
</template>

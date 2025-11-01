<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const tagId = ref(route.params.id);

import { useConfigsStore, useTagsStore, useStoresStore, useItemsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const tagsStore = useTagsStore();
const storesStore = useStoresStore();
const itemsStore = useItemsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (tagId.value === "new") {
		tagsStore.tagEdition = {
			loading: false,
		};
	} else {
		tagsStore.tagEdition = {
			loading: true,
		};
		try {
			await tagsStore.getTagById(tagId.value);
		} catch {
			delete tagsStore.tags[tagId.value];
			addNotification({ message: "tag.VTagNotFound", type: "error", i18n: true });
			router.push("/tags");
			return;
		}
		tagsStore.getTagItemByInterval(tagId.value, 100, 0, ["item"]);
		tagsStore.getTagStoreByInterval(tagId.value, 100, 0, ["store"]);
		tagsStore.getTagBoxByInterval(tagId.value, 100, 0, ["box"]);
		tagsStore.tagEdition = {
			loading: false,
			nom_tag: tagsStore.tags[tagId.value].nom_tag,
			poids_tag: tagsStore.tags[tagId.value].poids_tag,
		};
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	tagsStore.tagEdition = {
		loading: false,
	};
});

const tagDeleteModalShow = ref(false);
const tagSave = async() => {
	try {
		createSchema().validateSync(tagsStore.tagEdition, { abortEarly: false });
		if (tagId.value === "new") {
			await tagsStore.createTag({ ...tagsStore.tagEdition });
			addNotification({ message: "tag.VTagCreated", type: "success", i18n: true });
		} else {
			await tagsStore.updateTag(tagId.value, { ...tagsStore.tagEdition });
			addNotification({ message: "tag.VTagUpdated", type: "success", i18n: true });
		}
	} catch (e) {
		if (e.inner) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
	if (tagId.value === "new") {
		tagId.value = String(tagsStore.tagEdition.id_tag);
		router.push("/tags/" + tagId.value);
	}
};
const tagDelete = async() => {
	try {
		await tagsStore.deleteTag(tagId.value);
		addNotification({ message: "tag.VTagDeleted", type: "success", i18n: true });
		router.push("/tags");
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	tagDeleteModalShow.value = false;
};

// Items
const itemModalShow = ref(false);
const itemLoaded = ref(false);
const itemOpenAddModal = () => {
	itemModalShow.value = true;
	if (!itemLoaded.value) {
		fetchAllItems();
	}
};
async function fetchAllItems() {
	let offset = 0;
	const limit = 100;
	do {
		await itemsStore.getItemByInterval(limit, offset);
		offset += limit;
	} while (offset < itemsStore.itemsTotalCount);
	itemLoaded.value = true;
}
const itemSave = async(item) => {
	try {
		await tagsStore.createTagItem(tagId.value, item);
		addNotification({ message: "tag.VTagItemAdded", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const itemDelete = async(item) => {
	try {
		await tagsStore.deleteTagItem(tagId.value, item.id_item);
		addNotification({ message: "tag.VTagItemDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};

const filteredItems = ref([]);
const updateFilteredItems = (newValue) => {
	filteredItems.value = newValue;
};
const filterItem = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "", placeholder: t("tag.VTagItemFilterPlaceholder"), compareMethod: "contain", class: "w-full" },
]);

// Stores
const storeModalShow = ref(false);
const storeitemLoaded = ref(false);
const storeOpenAddModal = () => {
	storeModalShow.value = true;
	if (!storeitemLoaded.value) {
		fetchAllStores();
	}
};
async function fetchAllStores() {
	let offset = 0;
	const limit = 100;
	do {
		await storesStore.getStoreByInterval(limit, offset);
		offset += limit;
	} while (offset < storesStore.storesTotalCount);
	storeitemLoaded.value = true;
}
const storeSave = async(store) => {
	try {
		await tagsStore.createTagStore(tagId.value, store);
		addNotification({ message: "tag.VTagStoreAdded", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const storeDelete = async(store) => {
	try {
		await tagsStore.deleteTagStore(tagId.value, store.id_store);
		addNotification({ message: "tag.VTagStoreDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};

const filteredStores = ref([]);
const updateFilteredStores = (newValue) => {
	filteredStores.value = newValue;
};
const filterStore = ref([
	{ key: "nom_store", value: "", type: "text", label: "", placeholder: t("tag.VTagStoreFilterPlaceholder"), compareMethod: "contain", class: "w-full" },
]);

// Boxs
const boxModalShow = ref(false);
const boxSave = async(box) => {
	try {
		await tagsStore.createTagBox(tagId.value, box);
		addNotification({ message: "tag.VTagBoxAdded", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const boxDelete = async(box) => {
	try {
		await tagsStore.deleteTagBox(tagId.value, box.id_box);
		addNotification({ message: "tag.VTagBoxDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};

const createSchema = () => {
	return Yup.object().shape({
		nom_tag: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("tag.VTagNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
			.required(t("tag.VTagNameRequired")),
		poids_tag: Yup.number()
			.min(0, t("tag.VTagPoidsMin"))
			.typeError(t("tag.VTagPoidsNumber"))
			.required(t("tag.VTagPoidsRequired")),
	});
};

const labelForm = [
	{ key: "nom_tag", label: "tag.VTagName", type: "text" },
	{ key: "poids_tag", label: "tag.VTagPoids", type: "number" },
];
const labelTableauItem = ref([
	{ label: "tag.VTagItemName", sortable: true, key: "reference_name_item", keyStore: "id_item", store: "1", type: "text" },
	{ label: "tag.VTagItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauStore = ref([
	{ label: "tag.VTagStoreName", sortable: true, key: "nom_store", keyStore: "id_store", store: "1", type: "text" },
	{ label: "tag.VTagStoreActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => storeDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauBox = ref([
	{ label: "tag.VTagBoxId", sortable: true, key: "id_box", keyStore: "id_box", store: "1", type: "number" },
	{ label: "tag.VTagBoxActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => boxDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);

const labelTableauModalItem = ref([
	{ label: "tag.VTagItemName", sortable: true, key: "reference_name_item", type: "text" },
	{ label: "tag.VTagItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			condition: "!store[1]?.[rowData.id_item]",
			action: (row) => itemSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			condition: "store[1]?.[rowData.id_item]",
			action: (row) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauModalStore = ref([
	{ label: "tag.VTagStoreName", sortable: true, key: "nom_store", type: "text" },
	{ label: "tag.VTagStoreActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			condition: "!store[1]?.[rowData.id_store]",
			action: (row) => storeSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			condition: "store[1]?.[rowData.id_store]",
			action: (row) => storeDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('tag.VTagTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/tags', save: { roleRequired: 0, loading: tagsStore.tagEdition.loading }, delete: { roleRequired: 0 } }"
			:id="tagId" :store-user="authStore.user" @button-save="tagSave" @button-delete="tagDeleteModalShow = true"/>
	</div>
	<div v-if="tagsStore.tags[tagId] || tagId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="tagsStore.tagEdition"/>
		</div>
		<CollapsibleSection title="tag.VTagItems"
			:total-count="Number(tagsStore.tagsItemTotalCount[tagId] || 0)" :id-page="tagId">
			<template #append-row>
				<button type="button" @click="itemOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.VTagAddItem') }}
				</button>
				<Tableau :labels="labelTableauItem" :meta="{ key: 'id_item' }"
					:store-data="[tagsStore.tagsItem[tagId],itemsStore.items]"
					:loading="tagsStore.tagsItemLoading"
					:total-count="Number(tagsStore.tagsItemTotalCount[tagId] || 0)"
					:loaded-count="Object.keys(tagsStore.tagsItem[tagId] || {}).length"
					:fetch-function="(offset, limit) => tagsStore.getTagItemByInterval(tagId, limit, offset, ['item'])"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="tag.VTagStores"
			:total-count="Number(tagsStore.tagsStoreTotalCount[tagId] || 0)" :id-page="tagId">
			<template #append-row>
				<button type="button" @click="storeOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.VTagAddStore') }}
				</button>
				<Tableau :labels="labelTableauStore" :meta="{ key: 'id_store' }"
					:store-data="[tagsStore.tagsStore[tagId],storesStore.stores]"
					:loading="tagsStore.tagsStoreLoading"
					:total-count="Number(tagsStore.tagsStoreTotalCount[tagId] || 0)"
					:loaded-count="Object.keys(tagsStore.tagsStore[tagId] || {}).length"
					:fetch-function="(offset, limit) => tagsStore.getTagStoreByInterval(tagId, limit, offset, ['store'])"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="tag.VTagBoxs"
			:total-count="Number(tagsStore.tagsBoxTotalCount[tagId] || 0)" :id-page="tagId">
			<template #append-row>
				<!-- <button type="button" @click="boxOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.VTagAddBox') }}
				</button> -->
				<Tableau :labels="labelTableauBox" :meta="{ key: 'id_box' }"
					:store-data="[tagsStore.tagsBox[tagId],storesStore.boxs]"
					:loading="tagsStore.tagsBoxLoading"
					:total-count="Number(tagsStore.tagsBoxTotalCount[tagId] || 0)"
					:loaded-count="Object.keys(tagsStore.tagsBox[tagId] || {}).length"
					:fetch-function="(offset, limit) => tagsStore.getTagBoxByInterval(tagId, limit, offset, ['box'])"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('tag.VTagLoading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="tagDeleteModalShow" @close-modal="tagDeleteModalShow = false"
		:delete-action="tagDelete" :text-title="'tag.VTagDeleteTitle'" :text-p="'tag.VTagDeleteText'"/>

	<div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('tag.VTagItemTitle') }}</h2>
				<button type="button" @click="itemModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterItem" :store-data="itemsStore.items" @output-filter="updateFilteredItems" />

			<!-- Tableau Items -->
			<Tableau :labels="labelTableauModalItem" :meta="{ key: 'id_item' }"
				:store-data="[filteredItems, tagsStore.tagsItem[tagId]]"
				:loading="tagsStore.tagsItemLoading"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>

	<div v-if="storeModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="storeModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('tag.VTagStoreTitle') }}</h2>
				<button type="button" @click="storeModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterStore" :store-data="storesStore.stores" @output-filter="updateFilteredStores" />

			<!-- Tableau Stores -->
			<Tableau :labels="labelTableauModalStore" :meta="{ key: 'id_store' }"
				:store-data="[filteredStores, tagsStore.tagsStore[tagId]]"
				:loading="tagsStore.tagsStoreLoading"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

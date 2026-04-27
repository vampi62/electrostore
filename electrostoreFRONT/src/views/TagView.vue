<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const tagId = ref(route.params.id);
const preset = ref(route.query.preset || null);

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
		if (preset.value) {
			preset.value.split(";").forEach((pair) => {
				const [key, value] = pair.split(":");
				if (key && value) {
					tagsStore.tagEdition[key] = value;
				}
			});
		}
	} else {
		tagsStore.tagEdition = {
			loading: true,
		};
		try {
			await tagsStore.getTagById(tagId.value);
		} catch {
			delete tagsStore.tags[tagId.value];
			addNotification({ message: t("tag.NotFound"), type: "error" });
			router.push("/tags");
			return;
		}
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
			const newId = await tagsStore.createTag({ ...tagsStore.tagEdition });
			addNotification({ message: t("tag.Created"), type: "success" });
			tagId.value = String(newId);
			router.push("/tags/" + tagId.value);
		} else {
			await tagsStore.updateTag(tagId.value, { ...tagsStore.tagEdition });
			addNotification({ message: t("tag.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const tagDelete = async() => {
	try {
		await tagsStore.deleteTag(tagId.value);
		addNotification({ message: t("tag.Deleted"), type: "success" });
		router.push("/tags");
	} catch (e) {
		addNotification({ message: e, type: "error" });
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
		addNotification({ message: t("tag.ItemAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const itemDelete = async(item) => {
	try {
		await tagsStore.deleteTagItem(tagId.value, item.id_item);
		addNotification({ message: t("tag.ItemDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const filterItem = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "", placeholder: t("tag.ItemFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
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
		addNotification({ message: t("tag.StoreAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const storeDelete = async(store) => {
	try {
		await tagsStore.deleteTagStore(tagId.value, store.id_store);
		addNotification({ message: t("tag.StoreDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const filterStore = ref([
	{ key: "nom_store", value: "", type: "text", label: "", placeholder: t("tag.StoreFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

// Boxs
const boxModalShow = ref(false);
const boxSave = async(box) => {
	try {
		await tagsStore.createTagBox(tagId.value, box);
		addNotification({ message: t("tag.BoxAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const boxDelete = async(box) => {
	try {
		await tagsStore.deleteTagBox(tagId.value, box.id_box);
		addNotification({ message: t("tag.BoxDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const createSchema = () => {
	return Yup.object().shape({
		nom_tag: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("tag.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
			.required(t("tag.NameRequired")),
		poids_tag: Yup.number()
			.min(0, t("tag.PoidsMin"))
			.typeError(t("tag.PoidsNumber"))
			.required(t("tag.PoidsRequired")),
	});
};

const labelForm = [
	{ key: "nom_tag", label: "tag.Name", type: "text" },
	{ key: "poids_tag", label: "tag.Poids", type: "number" },
];
const labelTableauItem = ref([
	{ label: "tag.ItemName", sortable: true, key: "Item.reference_name_item", sourceKey: "id_item", type: "text", 
		storeRessourceId: 1, valueKey: "reference_name_item" },

	{ label: "tag.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
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
	{ label: "tag.StoreName", sortable: true, key: "Store.nom_store", sourceKey: "id_store", type: "text", 
		storeRessourceId: 1, valueKey: "nom_store" },

	{ label: "tag.StoreActions", sortable: false, key: "", type: "buttons", buttons: [
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
	{ label: "tag.BoxId", sortable: true, key: "Box.id_box", sourceKey: "id_box", type: "number", 
		storeRessourceId: 1, valueKey: "id_box" },

	{ label: "tag.BoxActions", sortable: false, key: "", type: "buttons", buttons: [
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
	{ label: "tag.ItemName", sortable: true, key: "reference_name_item", valueKey: "reference_name_item", type: "text" },
	{ label: "tag.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "!store[1]?.[rowData.id_item]",
			action: (row) => itemSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "store[1]?.[rowData.id_item]",
			action: (row) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauModalStore = ref([
	{ label: "tag.StoreName", sortable: true, key: "nom_store", valueKey: "nom_store", type: "text" },
	{ label: "tag.StoreActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "!store[1]?.[rowData.id_store]",
			action: (row) => storeSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "store[1]?.[rowData.id_store]",
			action: (row) => storeDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('tag.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/tags',
				create: { showCondition: tagId === 'new' && authStore.hasPermission([0, 1, 2]), loading: tagsStore.tagEdition?.loading },
				update: { showCondition: tagId !== 'new' && authStore.hasPermission([0, 1, 2]), loading: tagsStore.tagEdition?.loading },
				delete: { showCondition: tagId !== 'new' && authStore.hasPermission([0, 1, 2]) }
			}"
			@button-create="tagSave" @button-update="tagSave" @button-delete="tagDeleteModalShow = true"/>
	</div>
	<div v-if="tagsStore.tags[tagId] || tagId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="tagsStore.tagEdition"/>
		</div>
		<CollapsibleSection title="tag.Items"
			:total-count="Number(tagsStore.tagsItemTotalCount[tagId] || 0)" :permission="tagId !=='new'">
			<template #append-row>
				<button type="button" @click="itemOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.AddItem') }}
				</button>
				<Tableau :labels="labelTableauItem" :meta="{ key: 'id_item', expand: ['item'] }"
					:store-data="[tagsStore.tagsItem[tagId],itemsStore.items]"
					:loading="tagsStore.tagsItemLoading"
					:total-count="Number(tagsStore.tagsItemTotalCount[tagId] || 0)"
					:fetch-function="tagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => tagsStore.getTagItemByInterval(tagId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="tag.Stores"
			:total-count="Number(tagsStore.tagsStoreTotalCount[tagId] || 0)" :permission="tagId !=='new'">
			<template #append-row>
				<button type="button" @click="storeOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.AddStore') }}
				</button>
				<Tableau :labels="labelTableauStore" :meta="{ key: 'id_store', expand: ['store'] }"
					:store-data="[tagsStore.tagsStore[tagId],storesStore.stores]"
					:loading="tagsStore.tagsStoreLoading"
					:total-count="Number(tagsStore.tagsStoreTotalCount[tagId] || 0)"
					:fetch-function="tagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => tagsStore.getTagStoreByInterval(tagId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="tag.Boxs"
			:total-count="Number(tagsStore.tagsBoxTotalCount[tagId] || 0)" :permission="tagId !=='new'">
			<template #append-row>
				<!-- <button type="button" @click="boxOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.AddBox') }}
				</button> -->
				<Tableau :labels="labelTableauBox" :meta="{ key: 'id_box', expand: ['box'] }"
					:store-data="[tagsStore.tagsBox[tagId],storesStore.boxs]"
					:loading="tagsStore.tagsBoxLoading"
					:total-count="Number(tagsStore.tagsBoxTotalCount[tagId] || 0)"
					:fetch-function="tagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => tagsStore.getTagBoxByInterval(tagId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('tag.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="tagDeleteModalShow" @close-modal="tagDeleteModalShow = false"
		:delete-action="tagDelete" :text-title="'tag.DeleteTitle'" :text-p="'tag.DeleteText'"/>

	<div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('tag.ItemTitle') }}</h2>
				<button type="button" @click="itemModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterItem" :store-data="itemsStore.items" />

			<!-- Tableau Items -->
			<Tableau :labels="labelTableauModalItem" :meta="{ key: 'id_item', preventClear: true }"
				:store-data="[itemsStore.items, tagsStore.tagsItem[tagId]]"
				:filters="filterItem"
				:loading="tagsStore.tagsItemLoading"
				:total-count="Number(itemsStore.itemsTotalCount || 0)"
				:fetch-function="tagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>

	<div v-if="storeModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="storeModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('tag.StoreTitle') }}</h2>
				<button type="button" @click="storeModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterStore" :store-data="storesStore.stores" />

			<!-- Tableau Stores -->
			<Tableau :labels="labelTableauModalStore" :meta="{ key: 'id_store', preventClear: true }"
				:store-data="[storesStore.stores, tagsStore.tagsStore[tagId]]"
				:filters="filterStore"
				:loading="tagsStore.tagsStoreLoading"
				:total-count="Number(storesStore.storesTotalCount || 0)"
				:fetch-function="tagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => storesStore.getStoreByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

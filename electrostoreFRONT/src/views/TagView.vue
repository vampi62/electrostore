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

import { useConfigsStore, useTagsStore, useStoresStore, useItemsStore, useEquipementsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const tagsStore = useTagsStore();
const storesStore = useStoresStore();
const itemsStore = useItemsStore();
const equipementsStore = useEquipementsStore();
const authStore = useAuthStore();

const formContainer = ref(null);

async function fetchAllData() {
	if (tagId.value === "new") {
		tagsStore.loadToEdition(tagId.value, preset.value);
	} else {
		tagsStore.setLoadingEdition(tagId.value, true);
		try {
			await tagsStore.getTagById(tagId.value);
		} catch {
			delete tagsStore.tags[tagId.value];
			addNotification({ message: t("tag.NotFound"), type: "error" });
			router.push("/tags");
			return;
		}
		tagsStore.loadToEdition(tagId.value);
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	tagsStore.clearEdition(tagId.value);
});

const tagDeleteModalShow = ref(false);
const tagSave = async() => {
	try {
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("tag.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			tagsStore.setLoadingEdition(tagId.value, false);
			return;
		}
		const id = await tagsStore.saveAllChanges(tagId.value);
		tagsStore.loadToEdition(id);
		if (tagId.value === "new") {
			addNotification({ message: t("tag.Created"), type: "success" });
			tagId.value = String(id);
			router.push("/tags/" + tagId.value);
		} else {
			addNotification({ message: t("tag.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		tagsStore.setLoadingEdition(tagId.value, false);
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
function itemSave(row) {
	try {
		if (tagsStore.tagItemReady[tagId.value]?.[row.id_item]?.status === "deleted") {
			delete tagsStore.tagItemReady[tagId.value][row.id_item];
			addNotification({ message: t("tag.ItemRestored"), type: "success" });
			return;
		}
		tagsStore.tagItemEdition[tagId.value][row.id_item] = { id_item: row.id_item };
		tagsStore.valideTagItemEditionById(tagId.value, row.id_item, "created");
		delete tagsStore.tagItemEdition[tagId.value][row.id_item];
		addNotification({ message: t("tag.ItemAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function itemDelete(item) {
	try {
		if (tagsStore.tagItemReady[tagId.value]?.[item.id_item]?.status === "created") {
			delete tagsStore.tagItemReady[tagId.value][item.id_item];
		} else {
			tagsStore.valideTagItemEditionById(tagId.value, item.id_item, "deleted");
		}
		addNotification({ message: t("tag.ItemDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function itemRestore(item) {
	try {
		delete tagsStore.tagItemReady[tagId.value][item.id_item];
		addNotification({ message: t("tag.ItemRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

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
function storeSave(row) {
	try {
		if (tagsStore.tagStoreReady[tagId.value]?.[row.id_store]?.status === "deleted") {
			delete tagsStore.tagStoreReady[tagId.value][row.id_store];
			addNotification({ message: t("tag.StoreRestored"), type: "success" });
			return;
		}
		tagsStore.tagStoreEdition[tagId.value][row.id_store] = { id_store: row.id_store };
		tagsStore.valideTagStoreEditionById(tagId.value, row.id_store, "created");
		delete tagsStore.tagStoreEdition[tagId.value][row.id_store];
		addNotification({ message: t("tag.StoreAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function storeDelete(store) {
	try {
		if (tagsStore.tagStoreReady[tagId.value]?.[store.id_store]?.status === "created") {
			delete tagsStore.tagStoreReady[tagId.value][store.id_store];
		} else {
			tagsStore.valideTagStoreEditionById(tagId.value, store.id_store, "deleted");
		}
		addNotification({ message: t("tag.StoreDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function storeRestore(store) {
	try {
		delete tagsStore.tagStoreReady[tagId.value][store.id_store];
		addNotification({ message: t("tag.StoreRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

const filterStore = ref([
	{ key: "name_store", value: "", type: "text", label: "", placeholder: t("tag.StoreFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
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
const boxDelete = (box) => {
	try {
		tagsStore.valideTagBoxEditionById(tagId.value, box.id_box, "deleted");
		addNotification({ message: t("tag.BoxDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const boxRestore = (box) => {
	try {
		delete tagsStore.tagBoxReady[tagId.value][box.id_box];
		addNotification({ message: t("tag.BoxRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

// Equipements
const equipementModalShow = ref(false);
const equipementLoaded = ref(false);
const equipementOpenAddModal = () => {
	equipementModalShow.value = true;
	if (!equipementLoaded.value) {
		fetchAllEquipements();
	}
};
async function fetchAllEquipements() {
	let offset = 0;
	const limit = 100;
	do {
		await equipementsStore.getEquipementByInterval(limit, offset);
		offset += limit;
	} while (offset < equipementsStore.equipementsTotalCount);
	equipementLoaded.value = true;
}
function equipementSave(row) {
	try {
		if (tagsStore.tagEquipementReady[tagId.value]?.[row.id_equipement]?.status === "deleted") {
			delete tagsStore.tagEquipementReady[tagId.value][row.id_equipement];
			addNotification({ message: t("tag.EquipementRestored"), type: "success" });
			return;
		}
		tagsStore.tagEquipementEdition[tagId.value][row.id_equipement] = { id_equipement: row.id_equipement };
		tagsStore.valideTagEquipementEditionById(tagId.value, row.id_equipement, "created");
		delete tagsStore.tagEquipementEdition[tagId.value][row.id_equipement];
		addNotification({ message: t("tag.EquipementAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function equipementDelete(equipement) {
	try {
		if (tagsStore.tagEquipementReady[tagId.value]?.[equipement.id_equipement]?.status === "created") {
			delete tagsStore.tagEquipementReady[tagId.value][equipement.id_equipement];
		} else {
			tagsStore.valideTagEquipementEditionById(tagId.value, equipement.id_equipement, "deleted");
		}
		addNotification({ message: t("tag.EquipementDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function equipementRestore(equipement) {
	try {
		delete tagsStore.tagEquipementReady[tagId.value][equipement.id_equipement];
		addNotification({ message: t("tag.EquipementRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

const filterEquipement = ref([
	{ key: "reference_name_equipement", value: "", type: "text", label: "", placeholder: t("tag.EquipementFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

const createSchema = () => {
	const edition = tagsStore.tagEdition[tagId.value];
	const shape = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.name_tag = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("tag.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("tag.NameRequired"));
	shape.weight_tag = Yup.number()
		.min(0, t("tag.PoidsMin"))
		.typeError(t("tag.PoidsNumber"))
		.required(t("tag.PoidsRequired"));
	return Yup.object().shape(shape);
};

const labelForm = [
	{ key: "name_tag", label: "tag.Name", type: "text" },
	{ key: "weight_tag", label: "tag.Poids", type: "number" },
];
const labelTableauItem = ref([
	{ label: "tag.ItemName", sortable: true, key: "Item.reference_name_item", sourceKey: "id_item", type: "text",
		storeRessourceId: 1, valueKey: "reference_name_item" },

	{ label: "tag.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row) => itemRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauStore = ref([
	{ label: "tag.StoreName", sortable: true, key: "Store.name_store", sourceKey: "id_store", type: "text",
		storeRessourceId: 1, valueKey: "name_store" },

	{ label: "tag.StoreActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row) => storeRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
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
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row) => boxRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row) => boxDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);

const labelTableauEquipement = ref([
	{ label: "tag.EquipementName", sortable: true, key: "Equipement.reference_name_equipement", sourceKey: "id_equipement", type: "text",
		storeRessourceId: 1, valueKey: "reference_name_equipement" },

	{ label: "tag.EquipementActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row) => equipementRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row) => equipementDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauModalEquipement = ref([
	{ label: "tag.EquipementName", sortable: true, key: "reference_name_equipement", valueKey: "reference_name_equipement", type: "text" },
	{ label: "tag.EquipementActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "!ready?.status && !store[1]?.[rowData.id_equipement]",
			action: (row) => equipementSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-rotate-left",
			showCondition: "ready?.status === 'deleted'",
			action: (row) => equipementRestore(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "ready?.status && ready?.status !== 'deleted'",
			action: (row) => equipementDelete(row),
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
			icon: "fa-solid fa-plus",
			showCondition: "!ready?.status && !store[1]?.[rowData.id_item]",
			action: (row) => itemSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-rotate-left",
			showCondition: "ready?.status === 'deleted'",
			action: (row) => itemRestore(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "ready?.status && ready?.status !== 'deleted'",
			action: (row) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauModalStore = ref([
	{ label: "tag.StoreName", sortable: true, key: "name_store", valueKey: "name_store", type: "text" },
	{ label: "tag.StoreActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "!ready?.status && !store[1]?.[rowData.id_store]",
			action: (row) => storeSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-rotate-left",
			showCondition: "ready?.status === 'deleted'",
			action: (row) => storeRestore(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "ready?.status && ready?.status !== 'deleted'",
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
				create: { showCondition: tagId === 'new' && authStore.hasPermission([0, 1, 2]), loading: tagsStore.tagEdition[tagId]?.loading },
				update: { showCondition: tagId !== 'new' && authStore.hasPermission([0, 1, 2]), loading: tagsStore.tagEdition[tagId]?.loading },
				delete: { showCondition: tagId !== 'new' && authStore.hasPermission([0, 1, 2]) }
			}"
			@button-create="tagSave" @button-update="tagSave" @button-delete="tagDeleteModalShow = true"/>
	</div>
	<div v-if="tagsStore.tags[tagId] || tagId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="tagsStore.tagEdition[tagId]"/>
		</div>
		<CollapsibleSection title="tag.Items"
			:total-count="Number(tagsStore.tagsItemTotalCount[tagId] || 0)">
			<template #append-row>
				<button type="button" @click="itemOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.AddItem') }}
				</button>
				<Tableau :labels="labelTableauItem" :meta="{ key: 'id_item', expand: ['item'] }"
					:store-data="[tagsStore.tagsItem[tagId],itemsStore.items]"
					:store-ready="tagsStore.tagItemReady[tagId]"
					:loading="tagsStore.tagsItemLoading"
					:total-count="Number(tagsStore.tagsItemTotalCount[tagId] || 0)"
					:fetch-function="tagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => tagsStore.getTagItemByInterval(tagId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="tag.Stores"
			:total-count="Number(tagsStore.tagsStoreTotalCount[tagId] || 0)">
			<template #append-row>
				<button type="button" @click="storeOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.AddStore') }}
				</button>
				<Tableau :labels="labelTableauStore" :meta="{ key: 'id_store', expand: ['store'] }"
					:store-data="[tagsStore.tagsStore[tagId],storesStore.stores]"
					:store-ready="tagsStore.tagStoreReady[tagId]"
					:loading="tagsStore.tagsStoreLoading"
					:total-count="Number(tagsStore.tagsStoreTotalCount[tagId] || 0)"
					:fetch-function="tagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => tagsStore.getTagStoreByInterval(tagId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="tag.Boxs"
			:total-count="Number(tagsStore.tagsBoxTotalCount[tagId] || 0)">
			<template #append-row>
				<Tableau :labels="labelTableauBox" :meta="{ key: 'id_box', expand: ['box'] }"
					:store-data="[tagsStore.tagsBox[tagId],storesStore.boxs]"
					:store-ready="tagsStore.tagBoxReady[tagId]"
					:loading="tagsStore.tagsBoxLoading"
					:total-count="Number(tagsStore.tagsBoxTotalCount[tagId] || 0)"
					:fetch-function="tagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => tagsStore.getTagBoxByInterval(tagId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="tag.Equipements"
			:total-count="Number(tagsStore.tagsEquipementTotalCount[tagId] || 0)">
			<template #append-row>
				<button type="button" @click="equipementOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.AddEquipement') }}
				</button>
				<Tableau :labels="labelTableauEquipement" :meta="{ key: 'id_equipement', expand: ['equipement'] }"
					:store-data="[tagsStore.tagsEquipement[tagId],equipementsStore.equipements]"
					:store-ready="tagsStore.tagEquipementReady[tagId]"
					:loading="tagsStore.tagsEquipementLoading"
					:total-count="Number(tagsStore.tagsEquipementTotalCount[tagId] || 0)"
					:fetch-function="tagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => tagsStore.getTagEquipementByInterval(tagId, limit, offset, expand, filter, sort, clear) : undefined"
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
				:store-ready="tagsStore.tagItemReady[tagId]"
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
				:store-ready="tagsStore.tagStoreReady[tagId]"
				:filters="filterStore"
				:loading="tagsStore.tagsStoreLoading"
				:total-count="Number(storesStore.storesTotalCount || 0)"
				:fetch-function="tagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => storesStore.getStoreByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>

	<div v-if="equipementModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="equipementModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('tag.EquipementTitle') }}</h2>
				<button type="button" @click="equipementModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterEquipement" :store-data="equipementsStore.equipements" />

			<!-- Tableau Equipements -->
			<Tableau :labels="labelTableauModalEquipement" :meta="{ key: 'id_equipement', preventClear: true }"
				:store-data="[equipementsStore.equipements, tagsStore.tagsEquipement[tagId]]"
				:store-ready="tagsStore.tagEquipementReady[tagId]"
				:filters="filterEquipement"
				:loading="tagsStore.tagsEquipementLoading"
				:total-count="Number(equipementsStore.equipementsTotalCount || 0)"
				:fetch-function="tagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => equipementsStore.getEquipementByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

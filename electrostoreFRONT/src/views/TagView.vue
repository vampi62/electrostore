<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import * as Yup from "yup";

import { useViewScroll, useTabGuard } from "@/composables";
import type { useNotification } from "@/composables";
import type FormContainer from "@/components/FormContainer.vue";
import { isNewId } from "@/utils";
import { useConfigsStore, useTagsStore, useItemsTagsStore, useStoresTagsStore, useStoresBoxsStore, useBoxsTagsStore, useEquipementsTagsStore, useStoresStore, useItemsStore, useEquipementsStore, useAuthStore } from "@/stores";

import type { FilterLabel } from "@/types/filter";
import type { TableauLabel, TableauRowData } from "@/types/tableau";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";

const { addNotification } = inject("useNotification") as ReturnType<typeof useNotification>;
const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const configsStore = useConfigsStore();
const tagsStore = useTagsStore();
const itemsTagsStore = useItemsTagsStore();
const storesTagsStore = useStoresTagsStore();
const boxsTagsStore = useBoxsTagsStore();
const storesBoxsStore = useStoresBoxsStore();
const equipementsTagsStore = useEquipementsTagsStore();
const storesStore = useStoresStore();
const itemsStore = useItemsStore();
const equipementsStore = useEquipementsStore();
const authStore = useAuthStore();

const tagId = ref(route.params.id as string);
const preset = ref(typeof route.query.preset === "string" ? route.query.preset : null);

// every new element has its own edition space in the store, so several tabs can create an element at the same time
if (isNewId(tagId.value)) {
	tagId.value = tagsStore.getAvailableNewTagId();
}

const formContainer = ref<InstanceType<typeof FormContainer> | null>(null);

async function fetchAllData() {
	if (isNewId(tagId.value)) {
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
		if (isNewId(tagId.value)) {
			addNotification({ message: t("tag.Created"), type: "success" });
			const tempId = tagId.value;
			tagId.value = String(id);
			tagsStore.clearEdition(tempId);
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
function itemSave(row: TableauRowData) {
	try {
		if (itemsTagsStore.itemTagReadyByTag(tagId.value)?.[row.id_item]?.status === "deleted") {
			delete itemsTagsStore.itemTagReadyByTag(tagId.value)[row.id_item];
			addNotification({ message: t("tag.ItemRestored"), type: "success" });
			return;
		}
		itemsTagsStore.itemTagEditionByTag(tagId.value)[row.id_item] = { id_item: row.id_item };
		itemsTagsStore.valideItemTagEditionByKey({ id_tag: tagId.value, id_item: row.id_item }, "created");
		delete itemsTagsStore.itemTagEditionByTag(tagId.value)[row.id_item];
		addNotification({ message: t("tag.ItemAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function itemDelete(item: TableauRowData) {
	try {
		if (itemsTagsStore.itemTagReadyByTag(tagId.value)?.[item.id_item]?.status === "created") {
			delete itemsTagsStore.itemTagReadyByTag(tagId.value)[item.id_item];
		} else {
			itemsTagsStore.valideItemTagEditionByKey({ id_tag: tagId.value, id_item: item.id_item }, "deleted");
		}
		addNotification({ message: t("tag.ItemDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function itemRestore(item: TableauRowData) {
	try {
		delete itemsTagsStore.itemTagReadyByTag(tagId.value)[item.id_item];
		addNotification({ message: t("tag.ItemRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

const filterItem = ref<FilterLabel[]>([
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
function storeSave(row: TableauRowData) {
	try {
		if (storesTagsStore.storeTagReadyByTag(tagId.value)?.[row.id_store]?.status === "deleted") {
			delete storesTagsStore.storeTagReadyByTag(tagId.value)[row.id_store];
			addNotification({ message: t("tag.StoreRestored"), type: "success" });
			return;
		}
		storesTagsStore.storeTagEditionByTag(tagId.value)[row.id_store] = { id_store: row.id_store };
		storesTagsStore.valideStoreTagEditionByKey({ id_tag: tagId.value, id_store: row.id_store }, "created");
		delete storesTagsStore.storeTagEditionByTag(tagId.value)[row.id_store];
		addNotification({ message: t("tag.StoreAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function storeDelete(store: TableauRowData) {
	try {
		if (storesTagsStore.storeTagReadyByTag(tagId.value)?.[store.id_store]?.status === "created") {
			delete storesTagsStore.storeTagReadyByTag(tagId.value)[store.id_store];
		} else {
			storesTagsStore.valideStoreTagEditionByKey({ id_tag: tagId.value, id_store: store.id_store }, "deleted");
		}
		addNotification({ message: t("tag.StoreDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function storeRestore(store: TableauRowData) {
	try {
		delete storesTagsStore.storeTagReadyByTag(tagId.value)[store.id_store];
		addNotification({ message: t("tag.StoreRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

const filterStore = ref<FilterLabel[]>([
	{ key: "name_store", value: "", type: "text", label: "", placeholder: t("tag.StoreFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

// Boxs
const boxModalShow = ref(false);
const boxSave = async(box: TableauRowData) => {
	try {
		await boxsTagsStore.createBoxTag({ id_tag: tagId.value }, { id_box: box.id_box });
		addNotification({ message: t("tag.BoxAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const boxDelete = (box: TableauRowData) => {
	try {
		boxsTagsStore.valideBoxTagEditionByKey({ id_tag: tagId.value, id_box: box.id_box }, "deleted");
		addNotification({ message: t("tag.BoxDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const boxRestore = (box: TableauRowData) => {
	try {
		delete boxsTagsStore.boxTagReadyByTag(tagId.value)[box.id_box];
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
function equipementSave(row: TableauRowData) {
	try {
		if (equipementsTagsStore.equipementTagReadyByTag(tagId.value)?.[row.id_equipement]?.status === "deleted") {
			delete equipementsTagsStore.equipementTagReadyByTag(tagId.value)[row.id_equipement];
			addNotification({ message: t("tag.EquipementRestored"), type: "success" });
			return;
		}
		equipementsTagsStore.equipementTagEditionByTag(tagId.value)[row.id_equipement] = { id_equipement: row.id_equipement };
		equipementsTagsStore.valideEquipementTagEditionByKey({ id_tag: tagId.value, id_equipement: row.id_equipement }, "created");
		delete equipementsTagsStore.equipementTagEditionByTag(tagId.value)[row.id_equipement];
		addNotification({ message: t("tag.EquipementAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function equipementDelete(equipement: TableauRowData) {
	try {
		if (equipementsTagsStore.equipementTagReadyByTag(tagId.value)?.[equipement.id_equipement]?.status === "created") {
			delete equipementsTagsStore.equipementTagReadyByTag(tagId.value)[equipement.id_equipement];
		} else {
			equipementsTagsStore.valideEquipementTagEditionByKey({ id_tag: tagId.value, id_equipement: equipement.id_equipement }, "deleted");
		}
		addNotification({ message: t("tag.EquipementDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function equipementRestore(equipement: TableauRowData) {
	try {
		delete equipementsTagsStore.equipementTagReadyByTag(tagId.value)[equipement.id_equipement];
		addNotification({ message: t("tag.EquipementRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

const filterEquipement = ref<FilterLabel[]>([
	{ key: "reference_name_equipement", value: "", type: "text", label: "", placeholder: t("tag.EquipementFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

const createSchema = () => {
	const edition = tagsStore.tagEdition[tagId.value];
	const shape: Record<string, Yup.AnySchema> = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.name_tag = Yup.string()
		.max(configsStore.getConfigNumber("max_length_name"), t("tag.NameMaxLength", { count: configsStore.getConfigNumber("max_length_name") }))
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
const labelTableauItem = ref<TableauLabel[]>([
	{ label: "tag.ItemName", sortable: true, key: "Item.reference_name_item", sourceKey: "id_item", type: "text",
		storeRessourceId: 1, valueKey: "reference_name_item" },

	{ label: "tag.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row: TableauRowData) => itemRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row: TableauRowData) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauStore = ref<TableauLabel[]>([
	{ label: "tag.StoreName", sortable: true, key: "Store.name_store", sourceKey: "id_store", type: "text",
		storeRessourceId: 1, valueKey: "name_store" },

	{ label: "tag.StoreActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row: TableauRowData) => storeRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row: TableauRowData) => storeDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauBox = ref<TableauLabel[]>([
	{ label: "tag.BoxId", sortable: true, key: "Box.id_box", sourceKey: "id_box", type: "number",
		storeRessourceId: 1, valueKey: "id_box" },

	{ label: "tag.BoxActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row: TableauRowData) => boxRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row: TableauRowData) => boxDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);

const labelTableauEquipement = ref<TableauLabel[]>([
	{ label: "tag.EquipementName", sortable: true, key: "Equipement.reference_name_equipement", sourceKey: "id_equipement", type: "text",
		storeRessourceId: 1, valueKey: "reference_name_equipement" },

	{ label: "tag.EquipementActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row: TableauRowData) => equipementRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row: TableauRowData) => equipementDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauModalEquipement = ref<TableauLabel[]>([
	{ label: "tag.EquipementName", sortable: true, key: "reference_name_equipement", valueKey: "reference_name_equipement", type: "text" },
	{ label: "tag.EquipementActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "!ready?.status && !store[1]?.[rowData.id_equipement]",
			action: (row: TableauRowData) => equipementSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-rotate-left",
			showCondition: "ready?.status === 'deleted'",
			action: (row: TableauRowData) => equipementRestore(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "ready?.status && ready?.status !== 'deleted'",
			action: (row: TableauRowData) => equipementDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauModalItem = ref<TableauLabel[]>([
	{ label: "tag.ItemName", sortable: true, key: "reference_name_item", valueKey: "reference_name_item", type: "text" },
	{ label: "tag.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "!ready?.status && !store[1]?.[rowData.id_item]",
			action: (row: TableauRowData) => itemSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-rotate-left",
			showCondition: "ready?.status === 'deleted'",
			action: (row: TableauRowData) => itemRestore(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "ready?.status && ready?.status !== 'deleted'",
			action: (row: TableauRowData) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauModalStore = ref<TableauLabel[]>([
	{ label: "tag.StoreName", sortable: true, key: "name_store", valueKey: "name_store", type: "text" },
	{ label: "tag.StoreActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "!ready?.status && !store[1]?.[rowData.id_store]",
			action: (row: TableauRowData) => storeSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-rotate-left",
			showCondition: "ready?.status === 'deleted'",
			action: (row: TableauRowData) => storeRestore(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "ready?.status && ready?.status !== 'deleted'",
			action: (row: TableauRowData) => storeDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
useViewScroll(true);
useTabGuard(tagsStore, () => tagId.value, "tagEdition", "tags", [
	{ key: "tagItemReady", get: (id) => itemsTagsStore.itemTagReadyByTag(id), set: (id, entries) => itemsTagsStore.setItemTagStagedSlice("ready", { id_tag: id }, entries) },
	{ key: "tagStoreReady", get: (id) => storesTagsStore.storeTagReadyByTag(id), set: (id, entries) => storesTagsStore.setStoreTagStagedSlice("ready", { id_tag: id }, entries) },
	{ key: "tagBoxReady", get: (id) => boxsTagsStore.boxTagReadyByTag(id), set: (id, entries) => boxsTagsStore.setBoxTagStagedSlice("ready", { id_tag: id }, entries) },
	{ key: "tagEquipementReady", get: (id) => equipementsTagsStore.equipementTagReadyByTag(id), set: (id, entries) => equipementsTagsStore.setEquipementTagStagedSlice("ready", { id_tag: id }, entries) },
]);
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('tag.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/tags',
				create: { showCondition: isNewId(tagId) && authStore.hasPermission([0, 1, 2]), loading: tagsStore.tagEdition[tagId]?.loading },
				update: { showCondition: !isNewId(tagId) && authStore.hasPermission([0, 1, 2]), loading: tagsStore.tagEdition[tagId]?.loading },
				delete: { showCondition: !isNewId(tagId) && authStore.hasPermission([0, 1, 2]) }
			}"
			@button-create="tagSave" @button-update="tagSave" @button-delete="tagDeleteModalShow = true"/>
	</div>
	<div v-if="tagsStore.tags[tagId] || isNewId(tagId)" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-edition="tagsStore.tagEdition[tagId]" :store-data="tagsStore.tags[tagId]"/>
		</div>
		<CollapsibleSection title="tag.Items"
			:total-count="itemsTagsStore.itemTagsCountByTag(tagId)">
			<template #append-row>
				<button type="button" @click="itemOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.AddItem') }}
				</button>
				<Tableau :labels="labelTableauItem" :meta="{ key: 'id_item', expand: ['item'] }"
					:store-data="[itemsTagsStore.itemTagsByTag(tagId),itemsStore.items]"
					:store-ready="itemsTagsStore.itemTagReadyByTag(tagId)"
					:loading="itemsTagsStore.itemTagsLoading"
					:total-count="itemsTagsStore.itemTagsCountByTag(tagId)"
					:fetch-function="!isNewId(tagId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => itemsTagsStore.getItemTagByInterval({ id_tag: tagId }, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="tag.Stores"
			:total-count="storesTagsStore.storeTagsCountByTag(tagId)">
			<template #append-row>
				<button type="button" @click="storeOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.AddStore') }}
				</button>
				<Tableau :labels="labelTableauStore" :meta="{ key: 'id_store', expand: ['store'] }"
					:store-data="[storesTagsStore.storeTagsByTag(tagId),storesStore.stores]"
					:store-ready="storesTagsStore.storeTagReadyByTag(tagId)"
					:loading="storesTagsStore.storeTagsLoading"
					:total-count="storesTagsStore.storeTagsCountByTag(tagId)"
					:fetch-function="!isNewId(tagId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => storesTagsStore.getStoreTagByInterval({ id_tag: tagId }, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="tag.Boxs"
			:total-count="boxsTagsStore.boxTagsCountByTag(tagId)">
			<template #append-row>
				<Tableau :labels="labelTableauBox" :meta="{ key: 'id_box', expand: ['box'] }"
					:store-data="[boxsTagsStore.boxTagsByTag(tagId),storesBoxsStore.boxsById]"
					:store-ready="boxsTagsStore.boxTagReadyByTag(tagId)"
					:loading="boxsTagsStore.boxTagsLoading"
					:total-count="boxsTagsStore.boxTagsCountByTag(tagId)"
					:fetch-function="!isNewId(tagId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => boxsTagsStore.getBoxTagByInterval({ id_tag: tagId }, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="tag.Equipements"
			:total-count="equipementsTagsStore.equipementTagsCountByTag(tagId)">
			<template #append-row>
				<button type="button" @click="equipementOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.AddEquipement') }}
				</button>
				<Tableau :labels="labelTableauEquipement" :meta="{ key: 'id_equipement', expand: ['equipement'] }"
					:store-data="[equipementsTagsStore.equipementTagsByTag(tagId),equipementsStore.equipements]"
					:store-ready="equipementsTagsStore.equipementTagReadyByTag(tagId)"
					:loading="equipementsTagsStore.equipementTagsLoading"
					:total-count="equipementsTagsStore.equipementTagsCountByTag(tagId)"
					:fetch-function="!isNewId(tagId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => equipementsTagsStore.getEquipementTagByInterval({ id_tag: tagId }, limit, offset, expand, filter, sort, clear) : undefined"
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
				:store-data="[itemsStore.items, itemsTagsStore.itemTagsByTag(tagId)]"
				:store-ready="itemsTagsStore.itemTagReadyByTag(tagId)"
				:filters="filterItem"
				:loading="itemsTagsStore.itemTagsLoading"
				:total-count="Number(itemsStore.itemsTotalCount || 0)"
				:fetch-function="!isNewId(tagId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => itemsStore.getItemByInterval(limit, offset, expand, filter, sort, clear) : undefined"
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
				:store-data="[storesStore.stores, storesTagsStore.storeTagsByTag(tagId)]"
				:store-ready="storesTagsStore.storeTagReadyByTag(tagId)"
				:filters="filterStore"
				:loading="storesTagsStore.storeTagsLoading"
				:total-count="Number(storesStore.storesTotalCount || 0)"
				:fetch-function="!isNewId(tagId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => storesStore.getStoreByInterval(limit, offset, expand, filter, sort, clear) : undefined"
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
				:store-data="[equipementsStore.equipements, equipementsTagsStore.equipementTagsByTag(tagId)]"
				:store-ready="equipementsTagsStore.equipementTagReadyByTag(tagId)"
				:filters="filterEquipement"
				:loading="equipementsTagsStore.equipementTagsLoading"
				:total-count="Number(equipementsStore.equipementsTotalCount || 0)"
				:fetch-function="!isNewId(tagId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => equipementsStore.getEquipementByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

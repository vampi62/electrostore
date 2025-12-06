<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const storeId = ref(route.params.id);

import { useConfigsStore, useStoresStore, useTagsStore, useItemsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const storesStore = useStoresStore();
const tagsStore = useTagsStore();
const itemsStore = useItemsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (storeId.value === "new") {
		storesStore.storeEdition[storeId.value] = {
			loading: false,
		};
		storesStore.ledEdition[storeId.value] = {};
		storesStore.boxEdition[storeId.value] = {};
	} else {
		storesStore.storeEdition[storeId.value] = {
			loading: true,
		};
		try {
			await storesStore.getStoreById(storeId.value, ["boxs", "leds"]);
		} catch {
			delete storesStore.stores[storeId.value];
			addNotification({ message: "store.VStoreNotFound", type: "error", i18n: true });
			router.push("/stores");
			return;
		}
		storesStore.getTagStoreByInterval(storeId.value, 100, 0, ["tag"]);
		storesStore.storeEdition[storeId.value] = {
			loading: false,
			id_store: storesStore.stores[storeId.value].id_store,
			nom_store: storesStore.stores[storeId.value].nom_store,
			mqtt_name_store: storesStore.stores[storeId.value].mqtt_name_store,
			xlength_store: storesStore.stores[storeId.value].xlength_store,
			ylength_store: storesStore.stores[storeId.value].ylength_store,
		};
		storesStore.ledEdition[storeId.value] = { ...storesStore.leds[storeId.value] };
		storesStore.boxEdition[storeId.value] = { ...storesStore.boxs[storeId.value] };
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	storesStore.storeEdition[storeId.value] = {
		loading: false,
	};
	storesStore.ledEdition[storeId.value] = {};
	storesStore.boxEdition[storeId.value] = {};
});

// store
const storeGrid = ref(null);
const storeDeleteModalShow = ref(false);
const storeSave = async() => {
	try {
		createSchema().validateSync(storesStore.storeEdition[storeId.value], { abortEarly: false });
		if (!storeGrid.value.checkOutOfGrid()) {
			return;
		}
		if (storeId.value === "new") {
			await storesStore.createStoreComplete(storeId.value, { 
				store: storesStore.storeEdition[storeId.value],
				leds: Object.values(storesStore.ledEdition[storeId.value]),
				boxs: Object.values(storesStore.boxEdition[storeId.value]),
			});
			addNotification({ message: "store.VStoreCreated", type: "success", i18n: true });
		} else {
			await storesStore.updateStoreComplete(storeId.value, { 
				store: storesStore.storeEdition[storeId.value],
				leds: Object.values(storesStore.ledEdition[storeId.value]),
				boxs: Object.values(storesStore.boxEdition[storeId.value]),
			});
			addNotification({ message: "store.VStoreUpdated", type: "success", i18n: true });
			await storesStore.getStoreById(storeId.value, ["boxs", "leds"]);
			storesStore.storeEdition[storeId.value] = {
				loading: false,
				id_store: storesStore.stores[storeId.value].id_store,
				nom_store: storesStore.stores[storeId.value].nom_store,
				mqtt_name_store: storesStore.stores[storeId.value].mqtt_name_store,
				xlength_store: storesStore.stores[storeId.value].xlength_store,
				ylength_store: storesStore.stores[storeId.value].ylength_store,
			};
			storesStore.ledEdition[storeId.value] = { ...storesStore.leds[storeId.value] };
			storesStore.boxEdition[storeId.value] = { ...storesStore.boxs[storeId.value] };
		}
		storesStore.storeEdition[storeId.value].loading = false;
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		storesStore.storeEdition[storeId.value].loading = false;
		return;
	}
	if (storeId.value === "new") {
		storeId.value = String(storesStore.storeEdition[storeId.value].store.id_store);
		router.push("/stores/" + storeId.value);
		// reload the store data
		await storesStore.getStoreById(storesStore.storeEdition[storeId.value].store.id_store, ["boxs", "leds"]);
		storesStore.ledEdition[storeId.value] = { ...storesStore.leds[storesStore.storeEdition[storeId.value].store.id_store] };
		storesStore.boxEdition[storeId.value] = { ...storesStore.boxs[storesStore.storeEdition[storeId.value].store.id_store] };
		storesStore.storeEdition[storeId.value] = {
			loading: false,
			id_store: storesStore.storeEdition[storeId.value].store.id_store,
			nom_store: storesStore.storeEdition[storeId.value].store.nom_store,
			mqtt_name_store: storesStore.storeEdition[storeId.value].store.mqtt_name_store,
			xlength_store: storesStore.storeEdition[storeId.value].store.xlength_store,
			ylength_store: storesStore.storeEdition[storeId.value].store.ylength_store,
		};
	}
};
const storeDelete = async() => {
	try {
		await storesStore.deleteStore(storeId.value);
		addNotification({ message: "store.VStoreDeleted", type: "success", i18n: true });
		router.push("/stores");
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	storeDeleteModalShow.value = false;
};

const createSchema = () => {
	return Yup.object().shape({
		nom_store: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("store.VStoreNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
			.required(t("store.VStoreNameRequired")),
		mqtt_name_store: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("store.VStoreMQTTNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
			.required(t("store.VStoreMQTTNameRequired")),
		xlength_store: Yup.number()
			.min(1, t("store.VStoreXLengthMin"))
			.typeError(t("store.VStoreXLengthType"))
			.required(t("store.VStoreXLengthRequired")),
		ylength_store: Yup.number()
			.min(1, t("store.VStoreYLengthMin"))
			.typeError(t("store.VStoreYLengthType"))
			.required(t("store.VStoreYLengthRequired")),
	});
};

const schemaItem = Yup.object().shape({
	qte_item_box: Yup.number()
		.required(t("store.VStoreItemQuantityRequired"))
		.typeError(t("store.VStoreItemQuantityNumber"))
		.min(0, t("store.VStoreItemQuantityMin")),
	seuil_max_item_item_box: Yup.number()
		.required(t("store.VStoreItemMaxThresholdRequired"))
		.typeError(t("store.VStoreItemMaxThresholdNumber"))
		.min(1, t("store.VStoreItemMaxThresholdMin")),
});

// box & item
const storeBoxEditModalShow = ref(false);
const storeItemAddModalShow = ref(false);
const boxId = ref(0);
const showBoxContent = async(idBox) => {
	boxId.value = idBox;
	try {
		let offset = 0;
		const limit = 100;
		do {
			await storesStore.getBoxItemByInterval(storeId.value, idBox, limit, offset, ["item"]);
			offset += limit;
		} while (offset < storesStore.boxItemsTotalCount[idBox]);
		for (const item of Object.values(itemsStore.items)) {
			if (item.id_img) {
				await itemsStore.showImageById(item.id_item, item.id_img);
			}
		}
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	storeBoxEditModalShow.value = true;
};
const itemLoaded = ref(false);
const itemOpenAddModal = () => {
	storeItemAddModalShow.value = true;
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
	if (storesStore.boxItems[boxId.value][item.id_item]) {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await storesStore.updateBoxItem(storeId.value, boxId.value, item.tmp.id_item, item.tmp);
			addNotification({ message: "store.VStoreItemUpdated", type: "success", i18n: true });
			item.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error", i18n: false });
			return;
		}
	} else {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await storesStore.createBoxItem(storeId.value, boxId.value, item.tmp);
			addNotification({ message: "store.VStoreItemAdded", type: "success", i18n: true });
			item.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error", i18n: false });
			return;
		}
	}
};
const itemDelete = async(item) => {
	try {
		await storesStore.deleteBoxItem(storeId.value, boxId.value, item.id_item);
		addNotification({ message: "store.VStoreItemDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};

const filteredItems = ref([]);
const updateFilteredItems = (newValue) => {
	filteredItems.value = newValue;
};
const filterItem = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "", placeholder: t("store.VStoreItemFilterPlaceholder"), compareMethod: "contain", class: "w-full" },
]);

// tag
const filterTag = ref([
	{ key: "nom_tag", value: "", type: "text", label: "", placeholder: t("store.VStoreTagFilterPlaceholder"), compareMethod: "contain", class: "w-full" },
]);
function tagSave(id_tag) {
	try {
		storesStore.createTagStore(storeId.value,  { id_tag: id_tag });
		addNotification({ message: "store.VStoreTagAdded", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
}
function tagDelete(id_tag) {
	try {
		storesStore.deleteTagStore(storeId.value, id_tag);
		addNotification({ message: "store.VStoreTagDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
}

const labelTableauBoxItem = ref([
	{ label: "store.VStoreItemName", sortable: true, key: "reference_name_item", type: "text", store: 1, keyStore: "id_item" },
	{ label: "store.VStoreItemQuantity", sortable: true, key: "qte_item_box", type: "number" },
	{ label: "store.VStoreItemMaxThreshold", sortable: true, key: "seuil_max_item_item_box", type: "number" },
	{ label: "store.VStoreItemImg", sortable: false, key: "id_img", type: "image", idStoreImg: 2, store: 1, keyStore: "id_item" },
]);
const metaTableauBoxItem = ref({
	key: "id_item",
	path: "/inventory/",
});
const labelTableauModalItem = ref([
	{ label: "store.VStoreItemName", sortable: true, key: "reference_name_item", type: "text" },
	{ label: "store.VStoreItemQuantity", sortable: true, key: "qte_item_box", keyStore: "id_item", store: "1", type: "number", canEdit: true },
	{ label: "store.VStoreItemMaxThreshold", sortable: true, key: "seuil_max_item_item_box", keyStore: "id_item", store: "1", type: "number", canEdit: true },
	{ label: "store.VStoreItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			condition: "store[1]?.[rowData.id_item] === undefined",
			action: (row) => {
				row.tmp = { qte_item_box: 0, seuil_max_item_item_box: 1, id_item: row.id_item };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-edit",
			condition: "store[1]?.[rowData.id_item] && !rowData.tmp",
			action: (row) => {
				row.tmp = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			condition: "rowData.tmp",
			action: (row) => itemSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			condition: "rowData.tmp",
			action: (row) => {
				row.tmp = null;
			},
			class: "px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500",
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
const labelForm = ref([
	{ key: "nom_store", label: "store.VStoreName", tledEditionype: "text", condition: "session?.role_user === 2" },
	{ key: "mqtt_name_store", label: "store.VStoreMQTTName", type: "text", condition: "session?.role_user === 2" },
	{ key: "xlength_store", label: "store.VStoreXLength", type: "number", condition: "session?.role_user === 2" },
	{ key: "ylength_store", label: "store.VStoreYLength", type: "number", condition: "session?.role_user === 2" },
]);
const labelTableauModalTag = ref([
	{ label: "store.VStoreTagName", sortable: true, key: "nom_tag", type: "text" },
	{ label: "store.VStoreTagActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			condition: "!store[1]?.[rowData.id_tag]",
			action: (row) => tagSave(row.id_tag),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			condition: "store[1]?.[rowData.id_tag]",
			action: (row) => tagDelete(row.id_tag),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
</script>
<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('store.VStoreTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/stores', save: { roleRequired: 2, loading: storesStore.storeEdition[storeId]?.loading }, delete: { roleRequired: 2 } }"
			:id="storeId" :store-user="authStore.user" @button-save="storeSave" @button-delete="storeDeleteModalShow = true"/>
	</div>
	<div v-if="storesStore.stores[storeId] || storeId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="storesStore.storeEdition[storeId] || {}" :store-user="authStore.user"/>
			<Tags :current-tags="storesStore.storeTags[storeId] || {}" :tags-store="tagsStore.tags" :can-edit="storeId !== 'new' && authStore.user.role_user >= 1"
				:delete-function="(value) => tagDelete(value)"
				:fetch-function="(offset, limit) => tagsStore.getTagByInterval(limit, offset)"
				:total-count="Number(tagsStore.tagsTotalCount || 0)"
				:filter-modal="filterTag"
				:tableau-modal="{ 'label': labelTableauModalTag, 'meta': { key: 'id_tag' }, 'css': { component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }
								, 'loading': tagsStore.tagsLoading }"
				:meta ="{ 'keyPoids': 'poids_tag', 'keyName': 'nom_tag' }"
				/>
		</div>
		<div class="mb-6 flex justify-between flex-wrap whitespace-pre">
			<div class="flex-1">
				<Store ref="storeGrid"
					:store-data="storesStore.storeEdition[storeId] || {}"
					:led-edition="storesStore.ledEdition[storeId] || {}"
					:box-edition="storesStore.boxEdition[storeId] || {}"
					:can-edit="storeId !== 'new' && authStore.user?.role_user === 2"
					:store-func="{ showLedById: (id,data) => storesStore.showLedById(storeId, id, data), showBoxById: (id,data) => storesStore.showBoxById(storeId, id, data) }"
					@open-box-content="(id) => showBoxContent(id)"
				/>
			</div>
			<div :class="storeBoxEditModalShow ? 'flex' : 'hidden'" class="flex-col max-w-1/2 min-h-32 bg-gray-200 px-2 py-2 rounded" id="storeInputTag">
				<div class="flex justify-between items-center border-b pb-3">
					<h2 class="text-xl mb-4">{{ $t('store.VStoreBoxContent') }} (Id : {{ boxId }})</h2>
					<button type="button" @click="storeBoxEditModalShow = false"
						class="text-xl text-gray-500 hover:text-gray-700">&times;</button>
				</div>
				<div>
					<Tableau v-if="boxId != null" :labels="labelTableauBoxItem" :meta="metaTableauBoxItem"
						:store-data="[storesStore.boxItems[boxId],itemsStore.items,itemsStore.thumbnailsURL]"
						:loading="storesStore.boxItemsLoading"
						:total-count="Number(storesStore.boxItemsTotalCount[boxId] || 0)"
						:loaded-count="Object.keys(storesStore.boxItems[boxId] || {}).length"
						:fetch-function="(offset, limit) => storesStore.getBoxItems(storeId, boxId, offset, limit)"
						:tableau-css="{ component: 'max-h-80', tr: 'transition duration-150 ease-in-out cursor-pointer hover:bg-gray-300 even:bg-gray-100' }"
					>
						<template #append-row>
							<tr @click="itemOpenAddModal()"
								class="transition duration-150 ease-in-out hover:bg-gray-300 cursor-pointer">
								<td colspan="4" class="text-center">
									{{ $t('store.VStoreAddItem') }}
								</td>
							</tr>
						</template>
					</Tableau>
				</div>
			</div>
		</div>
	</div>
	<div v-else>
		{{ $t('store.VStoreLoading') }}
	</div>

	<ModalDeleteConfirm :show-modal="storeDeleteModalShow" @close-modal="storeDeleteModalShow = false"
		:delete-action="storeDelete" :text-title="'store.VStoreDeleteTitle'"
		:text-p="'store.VStoreDeleteText'"/>

	<div v-if="tagModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center z-50"
		@click="tagModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('store.VStoreAddTag') }}</h2>
				<button type="button" @click="tagModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterTag" :store-data="tagsStore.tags" @output-filter="updateFilteredTags" />

			<!-- Tableau Items -->
			<Tableau :labels="labelTableauModalTag" :meta="{ key: 'id_tag' }"
				:store-data="[filteredTags,storesStore.storeTags[storeId]]"
				:loading="tagsStore.tagsLoading"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>

	<div v-if="storeItemAddModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center z-50"
		@click="storeItemAddModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('store.VStoreItemTitle') }}</h2>
				<button type="button" @click="storeItemAddModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<FilterContainer class="my-4 flex gap-4" :filters="filterItem" :store-data="itemsStore.items" @output-filter="updateFilteredItems" />

			<Tableau id="storeItemTable" :labels="labelTableauModalItem" :meta="{ key: 'id_item' }"
				:store-data="[filteredItems,storesStore.boxItems[boxId]]"
				:loading="itemsStore.itemsLoading" :schema="schemaItem"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>
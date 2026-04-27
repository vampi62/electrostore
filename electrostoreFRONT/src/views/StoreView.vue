<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const storeId = ref(route.params.id);
const preset = ref(route.query.preset || null);

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
		if (preset.value) {
			preset.value.split(";").forEach((pair) => {
				const [key, value] = pair.split(":");
				if (key && value) {
					storesStore.storeEdition[key] = value;
				}
			});
		}
	} else {
		storesStore.storeEdition[storeId.value] = {
			loading: true,
		};
		try {
			await storesStore.getStoreById(storeId.value, ["boxs", "leds"]);
		} catch {
			delete storesStore.stores[storeId.value];
			addNotification({ message: t("store.NotFound"), type: "error" });
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
			const newId = await storesStore.createStoreComplete(storeId.value, { 
				store: storesStore.storeEdition[storeId.value],
				leds: Object.values(storesStore.ledEdition[storeId.value]),
				boxs: Object.values(storesStore.boxEdition[storeId.value]),
			});
			addNotification({ message: t("store.Created"), type: "success" });
			storeId.value = String(newId);
			router.push("/stores/" + storeId.value);
			// reload the store data
			await storesStore.getStoreById(storeId.value, ["boxs", "leds"]);
			storesStore.ledEdition[storeId.value] = { ...storesStore.leds[storeId.value] };
			storesStore.boxEdition[storeId.value] = { ...storesStore.boxs[storeId.value] };
			storesStore.storeEdition[storeId.value] = {
				loading: false,
				id_store: storesStore.stores[storeId.value].id_store,
				nom_store: storesStore.stores[storeId.value].nom_store,
				mqtt_name_store: storesStore.stores[storeId.value].mqtt_name_store,
				xlength_store: storesStore.stores[storeId.value].xlength_store,
				ylength_store: storesStore.stores[storeId.value].ylength_store,
			};
		} else {
			await storesStore.updateStoreComplete(storeId.value, { 
				store: storesStore.storeEdition[storeId.value],
				leds: Object.values(storesStore.ledEdition[storeId.value]),
				boxs: Object.values(storesStore.boxEdition[storeId.value]),
			});
			addNotification({ message: t("store.Updated"), type: "success" });
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
		addNotification({ message: e, type: "error" });
		storesStore.storeEdition[storeId.value].loading = false;
		return;
	}
};
const storeDelete = async() => {
	try {
		await storesStore.deleteStore(storeId.value);
		addNotification({ message: t("store.Deleted"), type: "success" });
		router.push("/stores");
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	storeDeleteModalShow.value = false;
};

const createSchema = () => {
	return Yup.object().shape({
		nom_store: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("store.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
			.required(t("store.NameRequired")),
		mqtt_name_store: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("store.MQTTNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
			.required(t("store.MQTTNameRequired")),
		xlength_store: Yup.number()
			.min(1, t("store.XLengthMin"))
			.typeError(t("store.XLengthType"))
			.required(t("store.XLengthRequired")),
		ylength_store: Yup.number()
			.min(1, t("store.YLengthMin"))
			.typeError(t("store.YLengthType"))
			.required(t("store.YLengthRequired")),
	});
};

const schemaItem = Yup.object().shape({
	qte_item_box: Yup.number()
		.required(t("store.ItemQuantityRequired"))
		.typeError(t("store.ItemQuantityNumber"))
		.min(0, t("store.ItemQuantityMin")),
	seuil_max_item_item_box: Yup.number()
		.required(t("store.ItemMaxThresholdRequired"))
		.typeError(t("store.ItemMaxThresholdNumber"))
		.min(1, t("store.ItemMaxThresholdMin")),
});

// box & item
const storeBoxEditModalShow = ref(false);
const storeItemAddModalShow = ref(false);
const boxId = ref(null);
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
				await itemsStore.showThumbnailById(item.id_item, item.id_img);
			}
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	storeBoxEditModalShow.value = true;
};
const itemSave = async(item) => {
	if (storesStore.boxItems[boxId.value][item.id_item]) {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await storesStore.updateBoxItem(storeId.value, boxId.value, item.tmp.id_item, item.tmp);
			addNotification({ message: t("store.ItemUpdated"), type: "success" });
			item.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error" });
			return;
		}
	} else {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await storesStore.createBoxItem(storeId.value, boxId.value, item.tmp);
			addNotification({ message: t("store.ItemAdded"), type: "success" });
			item.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error" });
			return;
		}
	}
};
const itemDelete = async(item) => {
	try {
		await storesStore.deleteBoxItem(storeId.value, boxId.value, item.id_item);
		addNotification({ message: t("store.ItemDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const filterItem = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "", placeholder: t("store.ItemFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

// tag
const tagModalShow = ref(false);
const filterTag = ref([
	{ key: "nom_tag", value: "", type: "text", label: "", placeholder: t("store.TagFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);
function tagSave(id_tag) {
	try {
		storesStore.createTagStore(storeId.value,  { id_tag: id_tag });
		addNotification({ message: t("store.TagAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function tagDelete(id_tag) {
	try {
		storesStore.deleteTagStore(storeId.value, id_tag);
		addNotification({ message: t("store.TagDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

const labelTableauBoxItem = ref([
	{ label: "store.ItemName", sortable: true, key: "Item.reference_name_item", sourceKey: "id_item", type: "text", 
		storeRessourceId: 1,  valueKey: "reference_name_item" },
	{ label: "store.ItemQuantity", sortable: true, key: "qte_item_box", valueKey: "qte_item_box", type: "number" },
	{ label: "store.ItemMaxThreshold", sortable: true, key: "seuil_max_item_item_box", valueKey: "seuil_max_item_item_box", type: "number" },
	{ label: "store.ItemImg", sortable: false, key: "Item.Img.id_img", sourceKey: "id_item", type: "image",
		storeLinkId: 1, storeRessourceId: 2, storeLinkKeyJoinSource: "id_item", storeLinkKeyJoinRessource: "id_img", valueKey: "id_img" },
]);
const metaTableauBoxItem = ref({
	key: "id_item",
	path: "/inventory/",
	expand: ["item"],
});
const labelTableauModalItem = ref([
	{ label: "store.ItemName", sortable: true, key: "reference_name_item", valueKey: "reference_name_item", type: "text" },
	{ label: "store.ItemQuantity", sortable: true, key: "ItemsBoxs.qte_item_box", sourceKey: "id_item", type: "number", canEdit: true, 
		storeRessourceId: 1, valueKey: "qte_item_box" },
	{ label: "store.ItemMaxThreshold", sortable: true, key: "ItemsBoxs.seuil_max_item_item_box", sourceKey: "id_item", type: "number", canEdit: true, 
		storeRessourceId: 1, valueKey: "seuil_max_item_item_box" },
	{ label: "store.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "store[1]?.[rowData.id_item] === undefined && !rowData.tmp",
			action: (row) => {
				row.tmp = { qte_item_box: 0, seuil_max_item_item_box: 1, id_item: row.id_item };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "store[1]?.[rowData.id_item] && !rowData.tmp",
			action: (row) => {
				row.tmp = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "rowData.tmp",
			action: (row) => itemSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "rowData.tmp",
			action: (row) => {
				row.tmp = null;
			},
			class: "px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500",
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
const labelForm = ref([
	{ key: "nom_store", label: "store.Name", tledEditionype: "text", enableCondition: "func.hasPermission([2])" },
	{ key: "mqtt_name_store", label: "store.MQTTName", type: "text", enableCondition: "func.hasPermission([2])" },
	{ key: "xlength_store", label: "store.XLength", type: "number", enableCondition: "func.hasPermission([2])" },
	{ key: "ylength_store", label: "store.YLength", type: "number", enableCondition: "func.hasPermission([2])" },
]);
const labelTableauModalTag = ref([
	{ label: "store.TagName", sortable: true, key: "nom_tag", valueKey: "nom_tag", type: "text" },
	{ label: "store.TagActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "!store[1]?.[rowData.id_tag]",
			action: (row) => tagSave(row.id_tag),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "store[1]?.[rowData.id_tag]",
			action: (row) => tagDelete(row.id_tag),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>
<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('store.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/stores',
				create: { showCondition: storeId === 'new' && authStore.hasPermission([2]), loading: storesStore.storeEdition[storeId]?.loading },
				update: { showCondition: storeId !== 'new' && authStore.hasPermission([2]), loading: storesStore.storeEdition[storeId]?.loading },
				delete: { showCondition: storeId !== 'new' && authStore.hasPermission([2]) }
			}"
			@button-create="storeSave" @button-update="storeSave" @button-delete="storeDeleteModalShow = true"/>
	</div>
	<div v-if="storesStore.stores[storeId] || storeId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="storesStore.storeEdition[storeId] || {}" :store-user="authStore.user"
				:store-function="{ hasPermission: (validPerm) => authStore.hasPermission(validPerm) }"/>
			<Tags :current-tags="storesStore.storeTags[storeId] || {}" :tags-store="tagsStore.tags" :can-edit="storeId !== 'new' && authStore.hasPermission([1, 2])"
				:delete-function="(value) => tagDelete(value)"
				:filter-modal="filterTag"
				:tableau-modal="{ 'label': labelTableauModalTag, 'meta': { key: 'id_tag', preventClear: true }, 'css': { component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }
								, 'loading': tagsStore.tagsLoading, 'fetchFunction': (limit, offset, expand, filter, sort, clear) => tagsStore.getTagByInterval(limit, offset, expand, filter, sort, clear)
								, 'totalCount': Number(tagsStore.tagsTotalCount || 0) }"
				:meta ="{ 'keyPoids': 'poids_tag', 'keyName': 'nom_tag' }"
				/>
		</div>
		<div class="mb-6 flex justify-between flex-wrap whitespace-pre">
			<div class="flex-1">
				<Store ref="storeGrid"
					:store-data="storesStore.storeEdition[storeId] || {}"
					:led-edition="storesStore.ledEdition[storeId] || {}"
					:box-edition="storesStore.boxEdition[storeId] || {}"
					:can-edit="authStore.hasPermission([2])"
					:store-func="{ showLedById: (id,data) => storesStore.showLedById(storeId, id, data), showBoxById: (id,data) => storesStore.showBoxById(storeId, id, data) }"
					@open-box-content="(id) => showBoxContent(id)"
				/>
			</div>
			<div :class="storeBoxEditModalShow ? 'flex' : 'hidden'" class="flex-col max-w-1/2 min-h-32 bg-gray-200 px-2 py-2 rounded" id="storeInputTag">
				<div class="flex justify-between items-center border-b pb-3">
					<h2 class="text-xl mb-4">{{ $t('store.BoxContent') }} (Id : {{ boxId }})</h2>
					<button type="button" @click="storeBoxEditModalShow = false"
						class="text-xl text-gray-500 hover:text-gray-700">&times;</button>
				</div>
				<div>
					<Tableau v-if="boxId != null" :labels="labelTableauBoxItem" :meta="metaTableauBoxItem"
						:store-data="[storesStore.boxItems[boxId],itemsStore.items,itemsStore.thumbnailsURL]"
						:loading="storesStore.boxItemsLoading"
						:total-count="Number(storesStore.boxItemsTotalCount[boxId] || 0)"
						:fetch-function="storeId !== 'new' && boxId != null ? (limit, offset, expand, filter, sort, clear) => storesStore.getBoxItemByInterval(storeId, boxId, limit, offset, expand, filter, sort, clear) : undefined"
						:tableau-css="{ component: 'max-h-80', tr: 'transition duration-150 ease-in-out cursor-pointer hover:bg-gray-300 even:bg-gray-100' }"
					>
						<template #append-row>
							<tr @click="storeItemAddModalShow = true"
								class="transition duration-150 ease-in-out hover:bg-gray-300 cursor-pointer">
								<td colspan="4" class="text-center">
									{{ $t('store.AddItem') }}
								</td>
							</tr>
						</template>
					</Tableau>
				</div>
			</div>
		</div>
	</div>
	<div v-else>
		{{ $t('store.Loading') }}
	</div>

	<ModalDeleteConfirm :show-modal="storeDeleteModalShow" @close-modal="storeDeleteModalShow = false"
		:delete-action="storeDelete" :text-title="'store.DeleteTitle'"
		:text-p="'store.DeleteText'"/>

	<div v-if="tagModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center z-50"
		@click="tagModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('store.AddTag') }}</h2>
				<button type="button" @click="tagModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<FilterContainer class="my-4 flex gap-4" :filters="filterTag" :store-data="tagsStore.tags" />

			<Tableau :labels="labelTableauModalTag" :meta="{ key: 'id_tag' }"
				:store-data="[tagsStore.tags,storesStore.storeTags[storeId]]"
				:filters="filterTag"
				:loading="tagsStore.tagsLoading"
				:total-count="Number(tagsStore.tagsTotalCount || 0)"
				:fetch-function="storeId !== 'new' ? (limit, offset, expand, filter, sort, clear) => tagsStore.getTagByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>

	<div v-if="storeItemAddModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center z-50"
		@click="storeItemAddModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('store.ItemTitle') }}</h2>
				<button type="button" @click="storeItemAddModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<FilterContainer class="my-4 flex gap-4" :filters="filterItem" :store-data="itemsStore.items" />

			<Tableau id="storeItemTable" :labels="labelTableauModalItem" :meta="{ key: 'id_item' }"
				:store-data="[itemsStore.items, storesStore.boxItems[boxId]]"
				:filters="filterItem"
				:loading="itemsStore.itemsLoading" :schema="schemaItem"
				:total-count="Number(itemsStore.itemsTotalCount || 0)"
				:fetch-function="storeId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>
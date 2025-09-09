<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
let storeId = route.params.id;

import { useConfigsStore, useStoresStore, useTagsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const storesStore = useStoresStore();
const tagsStore = useTagsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (storeId !== "new") {
		storesStore.storeEdition[storeId] = {
			loading: true,
		};
		try {
			await storesStore.getStoreById(storeId, ["boxs", "leds"]);
		} catch {
			delete storesStore.stores[storeId];
			addNotification({ message: "store.VStoreNotFound", type: "error", i18n: true });
			router.push("/stores");
			return;
		}
		storesStore.getTagStoreByInterval(storeId, 100, 0, ["tag"]);
		storesStore.storeEdition[storeId] = {
			loading: false,
			id_store: storesStore.stores[storeId].id_store,
			nom_store: storesStore.stores[storeId].nom_store,
			mqtt_name_store: storesStore.stores[storeId].mqtt_name_store,
			xlength_store: storesStore.stores[storeId].xlength_store,
			ylength_store: storesStore.stores[storeId].ylength_store,
		};
		storesStore.ledEdition[storeId] = { ...storesStore.leds[storeId] };
		storesStore.boxEdition[storeId] = { ...storesStore.boxs[storeId] };
	} else {
		storesStore.storeEdition[storeId] = {
			loading: false,
		};
		storesStore.ledEdition[storeId] = {};
		storesStore.boxEdition[storeId] = {};
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	storesStore.storeEdition[storeId] = {
		loading: false,
	};
	storesStore.ledEdition = {};
	storesStore.boxEdition = {};
});

// store
const storeDeleteModalShow = ref(false);
const storeSave = async() => {
	try {
		createSchema().validateSync(storesStore.storeEdition[storeId], { abortEarly: false });
		if (storeId !== "new") {
			await storesStore.updateStoreComplete(storeId, { 
				store: storesStore.storeEdition[storeId],
				leds: Object.values(storesStore.ledEdition),
				boxs: Object.values(storesStore.boxEdition),
			});
			addNotification({ message: "store.VStoreUpdated", type: "success", i18n: true });
			await storesStore.getStoreById(storeId, ["boxs", "leds"]);
			storesStore.storeEdition[storeId] = {
				loading: false,
				id_store: storesStore.stores[storeId].id_store,
				nom_store: storesStore.stores[storeId].nom_store,
				mqtt_name_store: storesStore.stores[storeId].mqtt_name_store,
				xlength_store: storesStore.stores[storeId].xlength_store,
				ylength_store: storesStore.stores[storeId].ylength_store,
			};
			storesStore.ledEdition[storeId] = { ...storesStore.leds[storeId] };
			storesStore.boxEdition[storeId] = { ...storesStore.boxs[storeId] };
		} else {
			await storesStore.createStoreComplete(storeId, { 
				store: storesStore.storeEdition[storeId],
				leds: Object.values(storesStore.ledEdition),
				boxs: Object.values(storesStore.boxEdition),
			});
			addNotification({ message: "store.VStoreCreated", type: "success", i18n: true });
		}
		storesStore.storeEdition[storeId].loading = false;
	} catch (e) {
		if (e.inner) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			storesStore.storeEdition[storeId].loading = false;
			return;
		}
		addNotification({ message: e, type: "error", i18n: false });
		storesStore.storeEdition[storeId].loading = false;
		return;
	}
	if (storeId === "new") {
		storeId = String(storesStore.storeEdition[storeId].store.id_store);
		router.push("/stores/" + storeId);
		// reload the store data
		await storesStore.getStoreById(storesStore.storeEdition[storeId].store.id_store, ["boxs", "leds"]);
		storesStore.ledEdition = { ...storesStore.leds[storesStore.storeEdition[storeId].store.id_store] };
		storesStore.boxEdition = { ...storesStore.boxs[storesStore.storeEdition[storeId].store.id_store] };
		storesStore.storeEdition[storeId] = {
			loading: false,
			id_store: storesStore.storeEdition[storeId].store.id_store,
			nom_store: storesStore.storeEdition[storeId].store.nom_store,
			mqtt_name_store: storesStore.storeEdition[storeId].store.mqtt_name_store,
			xlength_store: storesStore.storeEdition[storeId].store.xlength_store,
			ylength_store: storesStore.storeEdition[storeId].store.ylength_store,
		};
	}
};
const storeDelete = async() => {
	try {
		await storesStore.deleteStore(storeId);
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

const tagModalShow = ref(false);
const tagLoad = ref(false);
const filteredTags = ref([]);
const updateFilteredTags = (newValue) => {
	filteredTags.value = newValue;
};
const filterTag = ref([
	{ key: "nom_tag", value: "", type: "text", label: "", placeholder: t("store.VStoreTagFilterPlaceholder"), compareMethod: "contain", class: "w-full" },
]);

const tagOpenAddModal = () => {
	tagModalShow.value = true;
	if (!tagLoad.value) {
		fetchAllTags();
	}
};
async function fetchAllTags() {
	let offset = 0;
	const limit = 100;
	do {
		await tagsStore.getTagByInterval(limit, offset);
		offset += limit;
	} while (offset < tagsStore.tagsTotalCount);
	tagLoad.value = true;
}

function tagSave(id_tag) {
	try {
		storesStore.createTagStore(storeId,  { id_tag: id_tag });
		addNotification({ message: "store.VStoreTagAdded", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
}
function tagDelete(id_tag) {
	try {
		storesStore.deleteTagStore(storeId, id_tag);
		addNotification({ message: "store.VStoreTagDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
}

const labelForm = ref([
	{ key: "nom_store", label: "store.VStoreName", type: "text", condition: "session?.role_user === 2" },
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
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			condition: "store[1]?.[rowData.id_tag]",
			action: (row) => tagDelete(row.id_tag),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
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
			<Tags :current-tags="storesStore.storeTags[storeId] || {}" :tags-store="tagsStore.tags" :can-edit="storeId !== 'new'"
				:delete-function="(value) => tagDelete(value)" @openModalTag="tagOpenAddModal"/>
		</div>
		<div class="mb-6 flex justify-between whitespace-pre">
			<Store
				:store-data="storesStore.storeEdition[storeId] || {}"
				:led-edition="storesStore.ledEdition[storeId] || {}"
				:box-edition="storesStore.boxEdition[storeId] || {}"
				:can-edit="storeId !== 'new' && authStore.user?.role_user === 2"
			/>
		</div>
	</div>
	<div v-else>
		{{ $t('store.VStoreLoading') }}
	</div>

	<ModalDeleteConfirm :show-modal="storeDeleteModalShow" @close-modal="storeDeleteModalShow = false"
		@delete-confirmed="storeDelete" :text-title="'store.VStoreDeleteTitle'"
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

</template>

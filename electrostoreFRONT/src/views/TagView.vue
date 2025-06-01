<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const tagId = route.params.id;

import { useConfigsStore, useTagsStore, useStoresStore, useItemsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const tagsStore = useTagsStore();
const storesStore = useStoresStore();
const itemsStore = useItemsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (tagId !== "new") {
		tagsStore.tagEdition = {
			loading: true,
		};
		try {
			await tagsStore.getTagById(tagId);
		} catch {
			delete tagsStore.tags[tagId];
			addNotification({ message: "tag.VTagNotFound", type: "error", i18n: true });
			router.push("/tags");
			return;
		}
		tagsStore.getTagItemByInterval(tagId, 100, 0, ["item"]);
		tagsStore.getTagStoreByInterval(tagId, 100, 0, ["store"]);
		tagsStore.getTagBoxByInterval(tagId, 100, 0, ["box"]);
		tagsStore.tagEdition = {
			loading: false,
			nom_tag: tagsStore.tags[tagId].nom_tag,
			poids_tag: tagsStore.tags[tagId].poids_tag,
		};
	} else {
		tagsStore.tagEdition = {
			loading: false,
		};
		showItems.value = false;
		showBoxs.value = false;
		showStores.value = false;
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

const showItems = ref(true);
const showBoxs = ref(true);
const showStores = ref(true);

const toggleItems = () => {
	if (tagId === "new") {
		return;
	}
	showItems.value = !showItems.value;
};
const toggleBoxs = () => {
	if (tagId === "new") {
		return;
	}
	showBoxs.value = !showBoxs.value;
};
const toggleStores = () => {
	if (tagId === "new") {
		return;
	}
	showStores.value = !showStores.value;
};

const tagDeleteModalShow = ref(false);
const tagSave = async() => {
	try {
		await schemaTag.validate(tagsStore.tagEdition, { abortEarly: false });
		if (tagId !== "new") {
			await tagsStore.updateTag(tagId, { ...tagsStore.tagEdition });
			addNotification({ message: "tag.VTagUpdated", type: "success", i18n: true });
		} else {
			await tagsStore.createTag({ ...tagsStore.tagEdition });
			addNotification({ message: "tag.VTagCreated", type: "success", i18n: true });
		}
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	if (tagId === "new") {
		router.push("/tags/" + tagsStore.tagEdition.id_tag);
	}
};
const tagDelete = async() => {
	try {
		await tagsStore.deleteTag(tagId);
		addNotification({ message: "tag.VTagDeleted", type: "success", i18n: true });
		router.push("/tags");
	} catch (e) {
		addNotification({ message: "tag.VTagDeleteError", type: "error", i18n: true });
	}
	tagDeleteModalShow.value = false;
};

// Items
const itemModalShow = ref(false);
const itemOpenAddModal = () => {
	itemModalShow.value = true;
	itemsStore.getItemByInterval();
};
const itemSave = async(item) => {
	try {
		await tagsStore.createTagItem(tagId, item);
		addNotification({ message: "tag.VTagItemAdded", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
};
const itemDelete = async(item) => {
	try {
		await tagsStore.deleteTagItem(tagId, item.id_item);
		addNotification({ message: "tag.VTagItemDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "tag.VTagItemDeleteError", type: "error", i18n: true });
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
const storeOpenAddModal = () => {
	storeModalShow.value = true;
	storesStore.getStoreByInterval();
};
const storeSave = async(store) => {
	try {
		await tagsStore.createTagStore(tagId, store);
		addNotification({ message: "tag.VTagStoreAdded", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
};
const storeDelete = async(store) => {
	try {
		await tagsStore.deleteTagStore(tagId, store.id_store);
		addNotification({ message: "tag.VTagStoreDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "tag.VTagStoreDeleteError", type: "error", i18n: true });
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
		await tagsStore.createTagBox(tagId, box);
		addNotification({ message: "tag.VTagBoxAdded", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
};
const boxDelete = async(box) => {
	try {
		await tagsStore.deleteTagBox(tagId, box.id_box);
		addNotification({ message: "tag.VTagBoxDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "tag.VTagBoxDeleteError", type: "error", i18n: true });
	}
};

const schemaTag = Yup.object().shape({
	nom_tag: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("tag.VTagNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("tag.VTagNameRequired")),
	poids_tag: Yup.number()
		.min(0, t("tag.VTagPoidsMin"))
		.typeError(t("tag.VTagPoidsNumber"))
		.required(t("tag.VTagPoidsRequired")),
});

</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('tag.VTagTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/tags', save: { roleRequired: 0, loading: tagsStore.tagEdition.loading }, delete: { roleRequired: 0 } }"
			:id="tagId" :store-user="authStore.user" @button-save="tagSave" @button-delete="tagDeleteModalShow = true"/>
	</div>
	<div v-if="tagsStore.tags[tagId] || tagId == 'new'">
		<div class="mb-6 flex justify-between">
			<Form :validation-schema="schemaTag" v-slot="{ errors }" @submit.prevent="">
				<table class="table-auto text-gray-700">
					<tbody>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('tag.VTagName') }}:</td>
							<td class="flex flex-col">
								<Field name="nom_tag" type="text"
									v-model="tagsStore.tagEdition.nom_tag"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.nom_tag }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.nom_tag || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('tag.VTagPoids') }}:</td>
							<td class="flex flex-col">
								<Field name="poids_tag" type="text" v-model="tagsStore.tagEdition.poids_tag"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.poids_tag }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.poids_tag || ' ' }}</span>
							</td>
						</tr>
					</tbody>
				</table>
			</Form>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleItems" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !tagsStore.tagsItemLoading && tagId != 'new', 'cursor-not-allowed': tagId == 'new' }">
				{{ $t('tag.VTagItems') }} ({{ tagsStore.tagsItemTotalCount[tagId] || 0 }})
			</h3>
			<div v-if="!tagsStore.tagsItemLoading && showItems" class="p-2">
				<button type="button" @click="itemOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.VTagAddItem') }}
				</button>
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead>
							<tr>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('tag.VTagItemName') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('tag.VTagItemActions') }}
								</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="item in tagsStore.tagsItem[tagId]" :key="item.id_item">
								<td class="px-4 py-2 border-b border-gray-200">
									{{ itemsStore.items[item.id_item].reference_name_item }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200 space-x-2">
									<button type="button" @click="itemDelete(item)"
										class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
										{{ $t('tag.VTagItemDelete') }}
									</button>
								</td>
							</tr>
						</tbody>
					</table>
				</div>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleStores" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !tagsStore.tagsStoreLoading && tagId != 'new', 'cursor-not-allowed': tagId == 'new' }">
				{{ $t('tag.VTagStores') }} ({{ tagsStore.tagsStoreTotalCount[tagId] || 0 }})
			</h3>
			<div v-if="!tagsStore.tagsStoreLoading && showStores" class="p-2">
				<button type="button" @click="storeOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.VTagAddStore') }}
				</button>
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead>
							<tr>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('tag.VTagStoreName') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('tag.VTagStoreActions') }}
								</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="store in tagsStore.tagsStore[tagId]" :key="store.id_store">
								<td class="px-4 py-2 border-b border-gray-200">
									{{ storesStore.stores[store.id_store].nom_store }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200 space-x-2">
									<button type="button" @click="storeDelete(store)"
										class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
										{{ $t('tag.VTagStoreDelete') }}
									</button>
								</td>
							</tr>
						</tbody>
					</table>
				</div>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleBoxs" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !tagsStore.tagsBoxLoading && tagId != 'new', 'cursor-not-allowed': tagId == 'new' }">
				{{ $t('tag.VTagBoxs') }} ({{ tagsStore.tagsBoxTotalCount[tagId] || 0 }})
			</h3>
			<div v-if="!tagsStore.tagsBoxLoading && showBoxs" class="p-2">
				<!-- <button type="button" @click="boxOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('tag.VTagAddBox') }}
				</button> -->
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead>
							<tr>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('tag.VTagBoxId') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('tag.VTagBoxActions') }}
								</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="box in tagsStore.tagsBox[tagId]" :key="box.id_box">
								<td class="px-4 py-2 border-b border-gray-200">
									{{ storesStore.boxs[box.id_box].id_box }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200 space-x-2">
									<button type="button" @click="boxDelete(box)"
										class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
										{{ $t('tag.VTagBoxDelete') }}
									</button>
								</td>
							</tr>
						</tbody>
					</table>
				</div>
			</div>
		</div>
	</div>
	<div v-else>
		<div>{{ $t('tag.VTagLoading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="tagDeleteModalShow" @close-modal="tagDeleteModalShow = false"
		@delete-confirmed="tagDelete" :text-title="'tag.VTagDeleteTitle'" :text-p="'tag.VTagDeleteText'"/>

	<div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemModalShow = false">
		<div class="bg-white rounded-lg shadow-lg w-3/4 p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('tag.VTagItemTitle') }}</h2>
				<button type="button" @click="itemModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterItem" :store-data="itemsStore.items" @output-filter="updateFilteredItems" />

			<!-- Tableau Items -->
			<div class="overflow-y-auto max-h-96 min-h-96">
				<table class="min-w-full bg-white border border-gray-200">
					<thead class="bg-gray-100 sticky top-0">
						<tr>
							<th class="px-4 py-2 border-b">{{ $t('tag.VTagItemName') }}</th>
							<th class="px-4 py-2 border-b">{{ $t('tag.VTagItemActions') }}</th>
						</tr>
					</thead>
					<tbody>
						<tr v-for="item in filteredItems" :key="item.id_item">
							<td class="px-4 py-2 border-b">{{ item.reference_name_item }}</td>
							<td class="px-4 py-2 border-b">
								<button v-if="!tagsStore.tagsItem[tagId][item.id_item]" type="button"
									@click="itemSave(item)"
									class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
									{{ $t('tag.VTagItemAdd') }}
								</button>
								<button v-else type="button" @click="itemDelete(item)"
									class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
									{{ $t('tag.VTagItemDelete') }}
								</button>
							</td>
						</tr>
					</tbody>
				</table>
			</div>
		</div>
	</div>

	<div v-if="storeModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="storeModalShow = false">
		<div class="bg-white rounded-lg shadow-lg w-3/4 p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('tag.VTagStoreTitle') }}</h2>
				<button type="button" @click="storeModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterStore" :store-data="storesStore.stores" @output-filter="updateFilteredStores" />

			<!-- Tableau Stores -->
			<div class="overflow-y-auto max-h-96 min-h-96">
				<table class="min-w-full bg-white border border-gray-200">
					<thead class="bg-gray-100 sticky top-0">
						<tr>
							<th class="px-4 py-2 border-b">{{ $t('tag.VTagStoreName') }}</th>
							<th class="px-4 py-2 border-b">{{ $t('tag.VTagStoreActions') }}</th>
						</tr>
					</thead>
					<tbody>
						<tr v-for="store in filteredStores" :key="store.id_store">
							<td class="px-4 py-2 border-b">{{ store.nom_store }}</td>
							<td class="px-4 py-2 border-b">
								<button v-if="!tagsStore.tagsStore[tagId][store.id_store]" type="button"
									@click="storeSave(store)"
									class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
									{{ $t('tag.VTagStoreAdd') }}
								</button>
								<button v-else type="button" @click="storeDelete(store)"
									class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
									{{ $t('tag.VTagStoreDelete') }}
								</button>
							</td>
						</tr>
					</tbody>
				</table>
			</div>
		</div>
	</div>
</template>

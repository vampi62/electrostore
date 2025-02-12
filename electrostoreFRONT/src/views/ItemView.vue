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
		itemsStore.getDocumentByInterval(itemId, 100, 0);
		itemsStore.getItemCommandByInterval(itemId, 100, 0, ["command"]);
		itemsStore.getItemProjetByInterval(itemId, 100, 0, ["projet"]);
		itemsStore.getImageByInterval(itemId, 100, 0);
		itemsStore.itemEdition = {
			loading: false,
			nom_item: itemsStore.items[itemId].nom_item,
			description_item: itemsStore.items[itemId].description_item,
			seuil_min_item: itemsStore.items[itemId].seuil_min_item,
			id_img: itemsStore.items[itemId].id_img,
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

const showDocuments = ref(true);
const toggleDocuments = () => {
	if (itemId === "new") {
		return;
	}
	showDocuments.value = !showDocuments.value;
};

// item
const itemDeleteModalShow = ref(false);
const itemSave = async() => {
	try {
		await schemaItem.validate(itemsStore.itemEdition, { abortEarly: false });
		await itemsStore.createItem(itemsStore.itemEdition);
		addNotification({ message: "item.VItemCreated", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	itemId = itemsStore.itemEdition.id_item;
	router.push("/inventory/" + itemsStore.itemEdition.id_item);
};
const itemUpdate = async() => {
	try {
		await schemaItem.validate(itemsStore.itemEdition, { abortEarly: false });
		await itemsStore.updateItem(itemId, { ...itemsStore.itemEdition });
		addNotification({ message: "item.VItemUpdated", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
};
const itemDelete = async() => {
	try {
		await itemsStore.deleteItem(itemId);
		addNotification({ message: "item.VItemDeleted", type: "success", i18n: true });
		router.push("/inventory");
	} catch (e) {
		addNotification({ message: "item.VItemDeleteError", type: "error", i18n: true });
	}
	itemDeleteModalShow.value = false;
};
const formatDateForDatetimeLocal = (date) => {
	const pad = (num) => String(num).padStart(2, "0");
	return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};

const sortedTags = computed(() => {
	return Object.keys(itemsStore.itemTags[itemId] || {})
		.sort((a, b) => tagsStore.tags[b].poids_tag - tagsStore.tags[a].poids_tag)
		.reduce((obj, key) => {
			obj[key] = itemsStore.itemTags[itemId][key];
			return obj;
		}, {});
});

// document
const documentAddModalShow = ref(false);
const documentEditModalShow = ref(false);
const documentDeleteModalShow = ref(false);
const documentModalData = ref({ id_item_document: null, name_item_document: "", document: null, isEdit: false });
const documentAddOpenModal = () => {
	documentModalData.value = { name_item_document: "", document: null, isEdit: false };
	documentAddModalShow.value = true;
};
const documentEditOpenModal = (doc) => {
	documentModalData.value = { id_item_document: doc.id_item_document, name_item_document: doc.name_item_document, document: null, isEdit: true };
	documentEditModalShow.value = true;
};
const documentDeleteOpenModal = (doc) => {
	documentModalData.value = doc;
	documentDeleteModalShow.value = true;
};
const documentAdd = async() => {
	try {
		schemaAddDocument.validateSync(documentModalData.value, { abortEarly: false });
		await itemsStore.createDocument(itemId, documentModalData.value);
		addNotification({ message: "item.VItemDocumentAdded", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	documentAddModalShow.value = false;
};
const documentEdit = async() => {
	try {
		schemaEditDocument.validateSync(documentModalData.value, { abortEarly: false });
		await itemsStore.updateDocument(itemId, documentModalData.value.id_item_document, documentModalData.value);
		addNotification({ message: "item.VItemDocumentUpdated", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	documentEditModalShow.value = false;
};
const documentDelete = async() => {
	try {
		await itemsStore.deleteDocument(itemId, documentModalData.value.id_item_document);
		addNotification({ message: "item.VItemDocumentDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "item.VItemDocumentDeleteError", type: "error", i18n: true });
	}
	documentDeleteModalShow.value = false;
};
const handleFileUpload = (e) => {
	documentModalData.value.document = e.target.files[0];
};
const documentDownload = async(fileContent) => {
	const file = await itemsStore.downloadDocument(itemId, fileContent.id_item_document);
	const url = window.URL.createObjectURL(new Blob([file]));
	const link = document.createElement("a");
	link.href = url;
	link.setAttribute("download", fileContent.name_item_document + "." + fileContent.type_item_document);
	document.body.appendChild(link);
	link.click();
	document.body.removeChild(link);
};
const documentView = async(fileContent) => {
	const file = await itemsStore.downloadDocument(itemId, fileContent.id_item_document);
	const blob = new Blob([file], { type: getMimeType(fileContent.type_item_document) });
	const url = window.URL.createObjectURL(blob);

	if (["pdf", "png", "jpg", "jpeg", "gif", "bmp"].includes(fileContent.type_item_document)) {
		// Ouvrir directement dans une nouvelle fenêtre
		window.open(url, "_blank");
	} else if (["doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt"].includes(fileContent.type_item_document)) {
		// Télécharger automatiquement pour les formats éditables
		const a = document.createElement("a");
		a.href = url;
		a.download = fileContent.name || `document.${fileContent.type_item_document}`;
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);
	} else {
		addNotification({ message: "item.VItemDocumentNotSupported", type: "error", i18n: true });
	}
};
const getMimeType = (type) => {
	const mimeTypes = {
		"pdf": "application/pdf",
		"doc": "application/msword",
		"docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
		"xls": "application/vnd.ms-excel",
		"xlsx": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
		"ppt": "application/vnd.ms-powerpoint",
		"pptx": "application/vnd.openxmlformats-officedocument.presentationml.presentation",
		"txt": "text/plain",
		"png": "image/png",
		"jpg": "image/jpeg",
		"jpeg": "image/jpeg",
		"gif": "image/gif",
		"bmp": "image/bmp",
	};
	return mimeTypes[type] || "application/octet-stream";
};

const schemaItem = Yup.object().shape({
	nom_item: Yup.string()
		.required(t("item.VItemNameRequired")),
	description_item: Yup.string()
		.required(t("item.VItemDescriptionRequired")),
	seuil_min_item: Yup.number()
		.required(t("item.VItemSeuilMinRequired")),
	id_img: Yup.string()
		.nullable(),
});

const schemaAddDocument = Yup.object().shape({
	name_item_document: Yup.string()
		.required(t("item.VItemDocumentNameRequired")),
	document: Yup.mixed()
		.required(t("item.VItemDocumentRequired"))
		.test("fileSize", t("item.VItemDocumentSize"), (value) => !value || value?.size <= 2000000),
});
const schemaEditDocument = Yup.object().shape({
	name_item_document: Yup.string()
		.required(t("item.VItemDocumentNameRequired")),
	document: Yup.mixed()
		.nullable(),
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
	<div v-if="itemsStore.items[itemId] || itemId == 'new'">
		<div class="mb-6 flex justify-between">
			<Form :validation-schema="schemaItem" v-slot="{ errors }" @submit.prevent="">
				<table class="table-auto text-gray-700">
					<tbody>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('item.VItemName') }}:</td>
							<td class="flex flex-col">
								<Field name="nom_item" type="text"
									v-model="itemsStore.itemEdition.nom_item"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.nom_item }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.nom_item || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('item.VItemDescription') }}:</td>
							<td class="flex flex-col">
								<Field name="description_item" v-slot="{ description_item }">
									<textarea v-bind="description_item" v-model="itemsStore.itemEdition.description_item"
										:value="itemsStore.itemEdition.description_item"
										class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300 resize-y"
										:class="{ 'border-red-500': errors.description_item }" rows="4">
									</textarea>
								</Field>
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.description_item || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('item.VItemSeuilMin') }}:</td>
							<td class="flex flex-col">
								<Field name="seuil_min_item" type="number" v-model="itemsStore.itemEdition.seuil_min_item"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.seuil_min_item }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.seuil_min_item || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('item.VItemImage') }}:</td>
							<td class="flex flex-col">
								<div class="flex justify-center items-center">
									<template v-if="itemsStore.itemEdition.id_img">
										<img v-if="itemsStore.imagesURL[itemsStore.itemEdition.id_img]"
											:src="itemsStore.imagesURL[itemsStore.itemEdition.id_img]" alt="Image"
											class="w-16 h-16 object-cover rounded" />
										<span v-else class="w-16 h-16 object-cover rounded">
											{{ $t('item.VInventoryLoading') }}
										</span>
									</template>
									<template v-else>
										<img src="../assets/nopicture.webp" alt="Image"
											class="w-16 h-16 object-cover rounded" />
									</template>
								</div>
							</td>
						</tr>
					</tbody>
				</table>
			</Form>
			<div class="w-96 h-96 bg-gray-200 px-4 py-2 rounded">
				<span v-for="(value, key) in sortedTags" :key="key">
					{{ tagsStore.tags[key].nom_tag }} ({{ tagsStore.tags[key].poids_tag }})
				</span>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleDocuments" class="text-xl font-semibold  bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !itemsStore.documentsLoading && itemId != 'new', 'cursor-not-allowed': itemId == 'new' }">
				{{ $t('item.VItemDocuments') }} ({{ itemsStore.documents[itemId] ?
					Object.keys(itemsStore.documents[itemId]).length : 0 }})
			</h3>
			<div v-if="!itemsStore.documentsLoading && showDocuments" class="p-2">
				<button type="button" @click="documentAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('item.VItemAddDocument') }}
				</button>
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead>
							<tr>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">{{
									$t('item.VItemDocumentName') }}</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">{{
									$t('item.VItemDocumentType') }}</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">{{
									$t('item.VItemDocumentDate') }}</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">{{
									$t('item.VItemDocumentActions') }}</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="document in itemsStore.documents[itemId]"
								:key="document.id_item_document">
								<td class="px-4 py-2 border-b border-gray-200">{{ document.name_item_document }}</td>
								<td class="px-4 py-2 border-b border-gray-200">{{ document.type_item_document }}</td>
								<td class="px-4 py-2 border-b border-gray-200">{{ document.date_item_document }}</td>
								<td class="px-4 py-2 border-b border-gray-200 space-x-2">
									<font-awesome-icon icon="fa-solid fa-edit" @click="documentEditOpenModal(document)"
										class="text-blue-500 cursor-pointer hover:text-blue-600" />
									<font-awesome-icon icon="fa-solid fa-eye" @click="documentView(document)"
										class="text-green-500 cursor-pointer hover:text-green-600" />
									<font-awesome-icon icon="fa-solid fa-download" @click="documentDownload(document)"
										class="text-yellow-500 cursor-pointer hover:text-yellow-600" />
									<font-awesome-icon icon="fa-solid fa-trash"
										@click="documentDeleteOpenModal(document)"
										class="text-red-500 cursor-pointer hover:text-red-600" />
								</td>
							</tr>
						</tbody>
					</table>
				</div>
			</div>
		</div>
	</div>
	<div v-else>
		<div>{{ $t('item.VItemLoading') }}</div>
	</div>

	<div v-if="itemDeleteModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="itemDeleteModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t('item.VItemDeleteTitle') }}</h2>
			<p>{{ $t('item.VItemDeleteText') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="itemDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('item.VItemDeleteConfirm') }}
				</button>
				<button type="button" @click="itemDeleteModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('item.VItemDeleteCancel') }}
				</button>
			</div>
		</div>
	</div>

	<div v-if="documentAddModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="documentAddModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<Form :validation-schema="schemaAddDocument" v-slot="{ errors }">
				<h2 class="text-xl mb-4">{{ $t('item.VItemDocumentAddTitle') }}</h2>
				<div class="flex flex-col">
					<div class="flex flex-col">
						<Field name="name_item_document" type="text"
							v-model="documentModalData.name_item_document"
							:placeholder="$t('item.VItemDocumentNamePlaceholder')"
							class="w-full p-2 border rounded"
							:class="{ 'border-red-500': errors.name_item_document }" />
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.name_item_document || ' ' }}</span>
					</div>
					<div class="flex flex-col">
						<Field name="document" type="file" @change="handleFileUpload" class="w-full p-2"
							:class="{ 'border-red-500': errors.document }" />
						<span class="h-5 w-80 text-sm">{{ $t('item.VItemDocumentSize') }}</span>
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.document || ' ' }}</span>
					</div>
				</div>
				<div class="flex justify-end space-x-2">
					<button type="button" @click="documentAddModalShow = false" class="px-4 py-2 bg-gray-300 rounded">{{
						$t('item.VItemDocumentCancel') }}</button>
					<button type="button" @click="documentAdd" class="px-4 py-2 bg-blue-500 text-white rounded">{{
						$t('item.VItemDocumentAdd') }}</button>
				</div>
			</Form>
		</div>
	</div>
	<div v-if="documentEditModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="documentEditModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<Form :validation-schema="schemaEditDocument" v-slot="{ errors }">
				<h2 class="text-xl mb-4">{{ $t('item.VItemDocumentEditTitle') }}</h2>
				<div class="flex flex-col">
					<div class="flex flex-col">
						<Field name="name_item_document" type="text"
							v-model="documentModalData.name_item_document"
							:placeholder="$t('item.VItemDocumentNamePlaceholder')"
							class="w-full p-2 border rounded"
							:class="{ 'border-red-500': errors.name_item_document }" />
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.name_item_document || ' ' }}</span>
					</div>
					<div class="flex flex-col">
						<Field name="document" type="file" @change="handleFileUpload" class="w-full p-2"
							:class="{ 'border-red-500': errors.document }" />
						<span class="h-5 w-80 text-sm">{{ $t('item.VItemDocumentSize') }}</span>
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.document || ' ' }}</span>
					</div>
				</div>
				<div class="flex justify-end space-x-2">
					<button type="button" @click="documentEditModalShow = false"
						class="px-4 py-2 bg-gray-300 rounded">{{
							$t('item.VItemDocumentCancel') }}</button>
					<button type="button" @click="documentEdit" class="px-4 py-2 bg-blue-500 text-white rounded">
						{{ $t('item.VItemDocumentEdit') }}
					</button>
				</div>
			</Form>
		</div>
	</div>
	<div v-if="documentDeleteModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="documentDeleteModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t('item.VItemDocumentDeleteTitle') }}</h2>
			<p>{{ $t('item.VItemDocumentDeleteText') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="documentDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('item.VItemDocumentDeleteConfirm') }}
				</button>
				<button type="button" @click="documentDeleteModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('item.VItemDocumentCancel') }}
				</button>
			</div>
		</div>
	</div>
</template>

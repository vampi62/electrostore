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

import { useConfigsStore, useItemsStore, useTagsStore, useStoresStore, useCommandsStore, useProjetsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const itemsStore = useItemsStore();
const tagsStore = useTagsStore();
const storesStore = useStoresStore();
const commandsStore = useCommandsStore();
const projetsStore = useProjetsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (itemId !== "new") {
		itemsStore.itemEdition = {
			loading: true,
		};
		try {
			await itemsStore.getItemById(itemId);
		} catch {
			delete itemsStore.items[itemId];
			addNotification({ message: "item.VItemNotFound", type: "error", i18n: true });
			router.push("/inventory");
			return;
		}
		itemsStore.getItemBoxByInterval(itemId, 100, 0, ["box"]);
		itemsStore.getItemTagByInterval(itemId, 100, 0, ["tag"]);
		itemsStore.getDocumentByInterval(itemId, 100, 0);
		itemsStore.getItemCommandByInterval(itemId, 100, 0, ["command"]);
		itemsStore.getItemProjetByInterval(itemId, 100, 0, ["projet"]);
		itemsStore.getImageByInterval(itemId, 100, 0);
		itemsStore.itemEdition = {
			loading: false,
			reference_name_item: itemsStore.items[itemId].reference_name_item,
			friendly_name_item: itemsStore.items[itemId].friendly_name_item,
			description_item: itemsStore.items[itemId].description_item,
			seuil_min_item: itemsStore.items[itemId].seuil_min_item,
			id_img: itemsStore.items[itemId].id_img,
		};
	} else {
		itemsStore.itemEdition = {
			loading: false,
		};
		showDocuments.value = false;
		showImages.value = false;
		showProjetItems.value = false;
		showCommandItems.value = false;
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	itemsStore.itemEdition = {
		loading: false,
	};
});

const showBoxs = ref(true);
const showDocuments = ref(true);
const showImages = ref(true);
const showProjetItems = ref(true);
const showCommandItems = ref(true);

const toggleBoxs = () => {
	if (itemId === "new") {
		return;
	}
	showBoxs.value = !showBoxs.value;
};
const toggleDocuments = () => {
	if (itemId === "new") {
		return;
	}
	showDocuments.value = !showDocuments.value;
};
const toggleImages = () => {
	if (itemId === "new") {
		return;
	}
	showImages.value = !showImages.value;
};

const toggleProjetItems = () => {
	if (itemId === "new") {
		return;
	}
	showProjetItems.value = !showProjetItems.value;
};
const toggleCommandItems = () => {
	if (itemId === "new") {
		return;
	}
	showCommandItems.value = !showCommandItems.value;
};

const toggleBoxLed = async(boxId) => {
	let storeId = itemsStore.itemBoxs[itemId][boxId]["box"].id_store;
	try {
		await storesStore.showBoxById(storeId, boxId, { "red": 255, "green": 255, "blue": 255, "timeshow": 30, "animation": 4 });
		addNotification({ message: "item.VItemBoxShowSuccess", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "item.VItemToggleError", type: "error", i18n: true });
	}
};

// item
const itemDeleteModalShow = ref(false);
const itemInputTagShow = ref(false);
const tagLoad = ref(false);
const itemSave = async() => {
	try {
		await schemaItem.validate(itemsStore.itemEdition, { abortEarly: false });
		if (itemId !== "new") {
			await itemsStore.updateItem(itemId, { ...itemsStore.itemEdition });
			addNotification({ message: "item.VItemUpdated", type: "success", i18n: true });
		} else {
			await itemsStore.createItem({ ...itemsStore.itemEdition });
			addNotification({ message: "item.VItemCreated", type: "success", i18n: true });
		}
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	if (itemId === "new") {
		router.push("/inventory/" + itemsStore.itemEdition.id_item);
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
const showInputAddTag = async() => {
	if (!tagLoad.value) {
		try {
			let offset = 0;
			const limit = 100;
			do {
				await tagsStore.getTagByInterval(limit, offset);
				offset += limit;
			} while (offset < tagsStore.tagsTotalCount);
			tagLoad.value = true;
		} catch (e) {
			console.log(e);
		}
	}
	itemInputTagShow.value = true;
};

const newTags = computed(() => {
	return Object.values(tagsStore.tags).filter((element) => {
		return !itemsStore.itemTags[itemId][element.id_tag];
	});
});

const getTotalQuantity = computed(() => {
	if (itemId === "new") {
		return 0;
	}
	return itemsStore.itemBoxs[itemId] ? Object.values(itemsStore.itemBoxs[itemId]).reduce((acc, box) => acc + box.qte_item_box, 0) : 0;
});

const sortedTags = computed(() => {
	return Object.keys(itemsStore.itemTags[itemId] || {})
		.sort((a, b) => tagsStore.tags[b].poids_tag - tagsStore.tags[a].poids_tag);
});

// box
const boxSave = async(box) => {
	if (itemsStore.itemBoxs[itemId][box.id_box]) {
		try {
			schemaBox.validateSync(box.tmp, { abortEarly: false });
			await itemsStore.updateItemBox(itemId, box.tmp.id_box, box.tmp);
			addNotification({ message: "item.VItemBoxUpdated", type: "success", i18n: true });
			box.tmp = null;
		} catch (e) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
	} else {
		try {
			schemaItem.validateSync(box.tmp, { abortEarly: false });
			await itemsStore.createItemBox(itemId, box.tmp);
			addNotification({ message: "item.VItemBoxAdded", type: "success", i18n: true });
			box.tmp = null;
		} catch (e) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
	}
};

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

// image
const imageSelectModalShow = ref(false);
const imageAddModalShow = ref(false);
const imageDeleteModalShow = ref(false);
const selectedImageId = ref(null);
const imageModalData = ref({ id_img: null, nom_img: "", image: null, isEdit: false });
const imageSelectOpenModal = () => {
	if (itemId === "new") {
		return;
	}
	if (Object.keys(itemsStore.images[itemId]).length === 0) {
		addNotification({ message: "item.VItemImageEmpty", type: "error", i18n: true });
		return;
	}
	if (itemsStore.images[itemId]) {
		imageSelectModalShow.value = true;
	}
};
const imageAddOpenModal = () => {
	imageModalData.value = { nom_img: "", image: null, isEdit: false };
	imageAddModalShow.value = true;
};
const imageDeleteOpenModal = (doc) => {
	imageModalData.value = doc;
	imageDeleteModalShow.value = true;
};
const imageAdd = async() => {
	try {
		await itemsStore.createImage(itemId, imageModalData.value);
		addNotification({ message: "item.VItemImageAdded", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "item.VItemImageAddError", type: "error", i18n: true });
	}
	imageAddModalShow.value = false;
};
const imageDelete = async() => {
	try {
		await itemsStore.deleteImage(itemId, imageModalData.value.id_img);
		addNotification({ message: "item.VItemImageDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "item.VItemImageDeleteError", type: "error", i18n: true });
	}
	imageDeleteModalShow.value = false;
};
const handleImageUpload = (e) => {
	imageModalData.value.image = e.target.files[0];
};
const imageDownload = async(imageContent) => {
	//const file = await itemsStore.downloadImage(itemId, imageContent.id_img);
	//const url = window.URL.createObjectURL(new Blob([file]));
	let url = itemsStore.imagesURL[imageContent.id_img];
	const link = document.createElement("a");
	link.href = url;
	link.setAttribute("download", imageContent.nom_img + ".png");
	document.body.appendChild(link);
	link.click();
	document.body.removeChild(link);
};

const schemaBox = Yup.object().shape({
	qte_item_box: Yup.number()
		.required(t("item.VItemBoxQuantityRequired"))
		.typeError(t("item.VItemBoxQuantityNumber"))
		.min(0, t("item.VItemBoxQuantityMin")),
	seuil_max_item_item_box: Yup.number()
		.required(t("item.VItemBoxMaxThresholdRequired"))
		.typeError(t("item.VItemBoxMaxThresholdNumber"))
		.min(1, t("item.VItemBoxMaxThresholdMin")),
});

const schemaItem = Yup.object().shape({
	reference_name_item: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("item.VItemNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("item.VItemNameRequired")),
	friendly_name_item: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("item.VItemFriendlyNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("item.VItemFriendlyNameRequired")),
	description_item: Yup.string()
		.max(configsStore.getConfigByKey("max_length_description"), t("item.VItemDescriptionMaxLength") + " " + configsStore.getConfigByKey("max_length_description") + t("common.VAllCaracters"))
		.required(t("item.VItemDescriptionRequired")),
	seuil_min_item: Yup.number()
		.min(0, t("item.VItemSeuilMinMin"))
		.typeError(t("item.VItemSeuilMinType"))
		.required(t("item.VItemSeuilMinRequired")),
	id_img: Yup.string()
		.nullable(),
});

const schemaAddDocument = Yup.object().shape({
	name_item_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("item.VItemDocumentNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("item.VItemDocumentNameRequired")),
	document: Yup.mixed()
		.required(t("item.VItemDocumentRequired"))
		.test("fileSize", t("item.VItemDocumentSize") + " " + configsStore.getConfigByKey("max_size_document_in_mb") + "Mo", (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});
const schemaEditDocument = Yup.object().shape({
	name_item_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("item.VItemDocumentNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("item.VItemDocumentNameRequired")),
	document: Yup.mixed()
		.nullable()
		.test("fileSize", t("item.VItemDocumentSize") + " " + configsStore.getConfigByKey("max_size_document_in_mb") + "Mo", (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});

const schemaAddImage = Yup.object().shape({
	nom_img: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("item.VItemImageNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("item.VItemImageNameRequired")),
	image: Yup.mixed()
		.required(t("item.VItemImageRequired"))
		.test("fileSize", t("item.VItemImageSize") + " " + configsStore.getConfigByKey("max_size_document_in_mb") + "Mo", (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});

const labelTableauDocument = ref([
	{ label: "item.VItemDocumentName", sortable: true, key: "name_item_document", type: "text" },
	{ label: "item.VItemDocumentType", sortable: true, key: "type_item_document", type: "text" },
	{ label: "item.VItemDocumentDate", sortable: true, key: "date_item_document", type: "datetime" },
	{ label: "item.VItemDocumentActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			action: (row) => documentEditOpenModal(row),
			type: "button",
			class: "text-blue-500 cursor-pointer hover:text-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-eye",
			action: (row) => documentView(row),
			type: "button",
			class: "text-green-500 cursor-pointer hover:text-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-download",
			action: (row) => documentDownload(row),
			type: "button",
			class: "text-yellow-500 cursor-pointer hover:text-yellow-600",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => documentDeleteOpenModal(row),
			type: "button",
			class: "text-red-500 cursor-pointer hover:text-red-600",
		},
	] },
]);
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('item.VItemTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/inventory', save: { roleRequired: 0, loading: itemsStore.itemEdition.loading }, delete: { roleRequired: 0 } }"
			:id="itemId" :store-user="authStore.user" @button-save="itemSave" @button-delete="itemDeleteModalShow = true"/>
	</div>
	<div v-if="itemsStore.items[itemId] || itemId == 'new'">
		<div class="mb-6 flex justify-between">
			<Form :validation-schema="schemaItem" v-slot="{ errors }" @submit.prevent="">
				<table class="table-auto text-gray-700">
					<tbody>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('item.VItemName') }}:</td>
							<td class="flex flex-col">
								<Field name="reference_name_item" type="text"
									v-model="itemsStore.itemEdition.reference_name_item"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.reference_name_item }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.reference_name_item || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('item.VItemFriendlyName') }}:</td>
							<td class="flex flex-col">
								<Field name="friendly_name_item" type="text"
									v-model="itemsStore.itemEdition.friendly_name_item"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.friendly_name_item }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.friendly_name_item || ' ' }}</span>
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
							<td class="font-semibold pr-4 align-text-top">{{ $t('item.VItemTotalQuantity') }}:</td>
							<td class="flex flex-col">
								<div class="flex space x-2">
									<span>{{ getTotalQuantity }}</span>
								</div>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('item.VItemImage') }}:</td>
							<td class="flex flex-col">
								<div class="flex justify-center items-center"
									:class="{ 'cursor-pointer': !itemsStore.itemEdition.loading && itemId != 'new', 'cursor-not-allowed': itemId == 'new' }"
									@click="imageSelectOpenModal">
									<template v-if="itemsStore.itemEdition.id_img">
										<img v-if="itemsStore.imagesURL[itemsStore.itemEdition.id_img]"
											:src="itemsStore.imagesURL[itemsStore.itemEdition.id_img]" alt="Image"
											class="w-48 h-48 object-cover rounded" />
										<span v-else class="w-48 h-48 object-cover rounded">
											{{ $t('item.VInventoryLoading') }}
										</span>
									</template>
									<template v-else>
										<img src="../assets/nopicture.webp" alt="Image"
											class="w-48 h-48 object-cover rounded" />
									</template>
								</div>
							</td>
						</tr>
					</tbody>
				</table>
			</Form>
			<div class="w-96 h-96 bg-gray-200 px-2 py-2 rounded">
				<span v-for="(value, key) in sortedTags" :key="key"
					class="bg-gray-300 p-1 rounded mr-2 mb-1">
					{{ tagsStore.tags[value].nom_tag }} ({{ tagsStore.tags[value].poids_tag }})
					<span @click="itemsStore.deleteItemTag(itemId, value)"
						class="text-red-500 cursor-pointer hover:text-red-600">
						<font-awesome-icon icon="fa-solid fa-times" />
					</span>
				</span>
				<span v-if="!itemInputTagShow" class="bg-gray-300 p-1 rounded mr-2 mb-2">
					<span @click="showInputAddTag"
						class="text-green-500 cursor-pointer hover:text-green-600">
						<font-awesome-icon icon="fa-solid fa-plus" />
					</span>
				</span>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleBoxs" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': itemId != 'new', 'cursor-not-allowed': itemId == 'new' }">
				{{ $t('item.VitemBoxs') }} ({{ itemsStore.itemBoxsTotalCount[itemId] || 0 }})
			</h3>
			<div :class="showBoxs ? 'block' : 'hidden'" class="p-2">
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead class="bg-gray-100 sticky top-0">
							<tr>
								<th class="px-4 py-2 border-b">{{ $t('item.VItemBoxId') }}</th>
								<th class="px-4 py-2 border-b">{{ $t('item.VItemBoxQuantity') }}</th>
								<th class="px-4 py-2 border-b">{{ $t('item.VItemBoxMaxThreshold') }}</th>
								<th class="px-4 py-2 border-b">{{ $t('item.VItemBoxActions') }}</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="box in itemsStore.itemBoxs[itemId]" :key="box.id_box">
								<td class="px-4 py-2 border-b">{{ box.id_box }}</td>
								<td class="px-4 py-2 border-b">
									<template v-if="box.tmp">
										<Form :validation-schema="schemaBox" v-slot="{ errors }">
											<Field name="qte_item_box" type="number" v-model="box.tmp.qte_item_box"
												class="w-20 p-2 border rounded-lg"
												:class="{ 'border-red-500': errors.qte_item_box }" />
										</Form>
									</template>
									<template v-else-if="itemsStore.itemBoxs[itemId][box.id_box]">
										<div>{{ itemsStore.itemBoxs[itemId][box.id_box].qte_item_box }}</div>
									</template>
									<template v-else>
										<div></div>
									</template>
								</td>
								<td class="px-4 py-2 border-b">
									<template v-if="box.tmp">
										<Form :validation-schema="schemaBox" v-slot="{ errors }">
											<Field name="seuil_max_item_item_box" type="number"
												v-model="box.tmp.seuil_max_item_item_box" class="w-20 p-2 border rounded-lg"
												:class="{ 'border-red-500': errors.seuil_max_item_item_box }" />
										</Form>
									</template>
									<template v-else-if="itemsStore.itemBoxs[itemId][box.id_box]">
										<div>{{ itemsStore.itemBoxs[itemId][box.id_box].seuil_max_item_item_box }}</div>
									</template>
									<template v-else>
										<div></div>
									</template>
								</td>
								<td class="px-4 py-2 border-b">
									<template v-if="box.tmp">
										<button type="button" @click="boxSave(box)"
											class="px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600">
											{{ $t('item.VItemBoxSave') }}
										</button>
										<button type="button" @click="box.tmp = null"
											class="px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
											{{ $t('item.VItemBoxCancel') }}
										</button>
									</template>
									<template v-else>
										<button v-if="!itemsStore.itemBoxs[itemId][box.id_box]" type="button"
											@click="box.tmp = { qte_item_box: 0, seuil_max_item_item_box: 1, id_box: box.id_box }"
											class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
											{{ $t('item.VItemBoxAdd') }}
										</button>
										<button v-else type="button"
											@click="box.tmp = { ...itemsStore.itemBoxs[itemId][box.id_box] }"
											class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
											{{ $t('item.VItemBoxEdit') }}
										</button>
									</template>
									<button type="button" @click="toggleBoxLed(box.id_box)"
										class="px-3 py-1 bg-yellow-500 text-white rounded-lg hover:bg-yellow-600">
										{{ $t('item.VItemBoxShow') }}
									</button>
									
								</td>
							</tr>
						</tbody>
					</table>
				</div>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleDocuments" class="text-xl font-semibold  bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !itemsStore.documentsLoading && itemId != 'new', 'cursor-not-allowed': itemId == 'new' }">
				{{ $t('item.VItemDocuments') }} ({{ itemsStore.documentsTotalCount[itemId] || 0 }})
			</h3>
			<div v-if="!itemsStore.documentsLoading && showDocuments" class="p-2">
				<button type="button" @click="documentAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('item.VItemAddDocument') }}
				</button>
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<Tableau :labels="labelTableauDocument" :store-data="[itemsStore.documents[itemId]]" :meta="{ key: 'id_command_document' }"
						:loading="itemsStore.documentsLoading"
						:tableau-css="{ table: 'min-w-full table-auto', thead: 'bg-gray-100', th: 'px-4 py-2 text-center bg-gray-200 sticky top-0', tbody: '', tr: 'transition duration-150 ease-in-out hover:bg-gray-200', td: 'px-4 py-2 border-b border-gray-200' }"
					/>
				</div>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleImages" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !itemsStore.images[itemId]?.loading && itemId != 'new', 'cursor-not-allowed': itemId == 'new' }">
				{{ $t('item.VItemImages') }} ({{ itemsStore.imagesTotalCount[itemId] || 0 }})
			</h3>
			<div v-if="!itemsStore.images.imagesLoading && showImages" class="p-2">
				<button type="button" @click="imageAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('item.VItemAddImage') }}
				</button>
				<div class="flex flex-wrap relative" @click="selectedImageId = null">
					<template v-if="itemsStore.images[itemId]">
						<div v-for="image in itemsStore.images[itemId]" :key="image.id_img" @click.stop
							class="w-48 h-48 bg-gray-200 rounded m-2 flex items-center relative">
							<template v-if="itemsStore.imagesURL[image.id_img]">
								<img :src="itemsStore.imagesURL[image.id_img]"
									class="w-48 h-48 object-cover rounded"
									@click="selectedImageId === image.id_img ? selectedImageId = null : selectedImageId = image.id_img" />
							</template>
							<template v-else>
								{{ $t('item.VItemImageLoading') }}
							</template>
							<div v-if="selectedImageId === image.id_img"
								class="absolute inset-0 flex flex-col justify-center items-center bg-black bg-opacity-75 text-white p-2 rounded">
								<p class="w-full break-words">{{ image.nom_img }}</p>
								<p class="w-full break-words">{{ image.date_img ? new Date(image.date_img).toLocaleString() : '' }}</p>
								<div class="flex space-x-2">
									<font-awesome-icon icon="fa-solid fa-download"
										@click="imageDownload(image)"
										class="text-yellow-500 cursor-pointer hover:text-yellow-600" />
									<font-awesome-icon icon="fa-solid fa-trash"
										@click="imageDeleteOpenModal(image)"
										class="text-red-500 cursor-pointer hover:text-red-600" />
								</div>
							</div>
						</div>
					</template>
				</div>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleCommandItems" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !itemsStore.itemCommandsLoading && itemId != 'new', 'cursor-not-allowed': itemId == 'new' }">
				{{ $t('item.VItemCommands') }} ({{ itemsStore.itemCommandsTotalCount[itemId] || 0 }})
			</h3>
			<div v-if="!itemsStore.itemCommandsLoading && showCommandItems" class="p-2">
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead>
							<tr>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('item.VItemCommandDate') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('item.VItemCommandStatus') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('item.VItemCommandQte') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('item.VItemCommandPrice') }}
								</th>
							</tr>
						</thead>
						<tbody>
							<template v-if="!itemsStore.itemCommandsLoading">
								<RouterLink v-for="command in itemsStore.itemCommands[itemId]"
									:key="command.id_command" :to="'/commands/' + command.id_command"
									custom v-slot="{ navigate }">
									<tr @click="navigate" class="cursor-pointer hover:bg-gray-200">
										<td class="px-4 py-2 border-b border-gray-200">
											{{ commandsStore.commands[command.id_command].date_command }}
										</td>
										<td class="px-4 py-2 border-b border-gray-200">
											{{ commandsStore.commands[command.id_command].status_command }}
										</td>
										<td class="px-4 py-2 border-b border-gray-200">
											{{ command.qte_command_item }}
										</td>
										<td class="px-4 py-2 border-b border-gray-200">
											{{ command.prix_command_item }}
										</td>
									</tr>
								</RouterLink>
							</template>
						</tbody>
					</table>
				</div>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleProjetItems" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !itemsStore.itemProjetsLoading && itemId != 'new', 'cursor-not-allowed': itemId == 'new' }">
				{{ $t('item.VItemProjets') }} ({{ itemsStore.itemProjetsTotalCount[itemId] || 0 }})
			</h3>
			<div v-if="!itemsStore.itemProjetsLoading && showProjetItems" class="p-2">
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead>
							<tr>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('item.VItemProjetName') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('item.VItemProjetDate') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('item.VItemProjetDateFin') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('item.VItemProjetStatus') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('item.VItemProjetQte') }}
								</th>
							</tr>
						</thead>
						<tbody>
							<template v-if="!itemsStore.itemProjetsLoading">
								<RouterLink v-for="projet in itemsStore.itemProjets[itemId]"
									:key="projet.id_projet" :to="'/projets/' + projet.id_projet"
									custom v-slot="{ navigate }">
									<tr @click="navigate" class="cursor-pointer hover:bg-gray-200">
										<td class="px-4 py-2 border-b border-gray-200">
											{{ projetsStore.projets[projet.id_projet].nom_projet }}
										</td>
										<td class="px-4 py-2 border-b border-gray-200">
											{{ projetsStore.projets[projet.id_projet].date_debut_projet }}
										</td>
										<td class="px-4 py-2 border-b border-gray-200">
											{{ projetsStore.projets[projet.id_projet].date_fin_projet }}
										</td>
										<td class="px-4 py-2 border-b border-gray-200">
											{{ projetsStore.projets[projet.id_projet].status_projet }}
										</td>
										<td class="px-4 py-2 border-b border-gray-200">
											{{ projet.qte_projet_item }}
										</td>
									</tr>
								</RouterLink>
							</template>
						</tbody>
					</table>
				</div>
			</div>
		</div>
	</div>
	<div v-else>
		<div>{{ $t('item.VItemLoading') }}</div>
	</div>

	<div v-if="itemInputTagShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemInputTagShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('item.VItemTagAdd') }}</h2>
				<button type="button" @click="itemInputTagShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<div class="flex flex-wrap">
				<template v-for="(tag, key) in newTags" :key="key">
					<div class="bg-gray-200 p-1 rounded mr-2 mb-2 cursor-pointer"
						@click="itemsStore.createItemTag(itemId, { id_tag: tag.id_tag })">
						{{ tag.nom_tag }} ({{ tag.poids_tag }})
					</div>
				</template>
			</div>
		</div>
	</div>

	<ModalDeleteConfirm :show-modal="itemDeleteModalShow" @close-modal="itemDeleteModalShow = false"
		@delete-confirmed="itemDelete" :text-title="'item.VItemDeleteTitle'"
		:text-p="'item.VItemDeleteText'"/>

	<div v-if="imageSelectModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="imageSelectModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<div class="flex justify-between items-center mb-2">
				<h2 class="text-xl">{{ $t('item.VItemImageSelectTitle') }}</h2>
				<div class="flex space-x-2 items-center cursor-pointer bg-gray-200 p-2 rounded" @click="imageSelectModalShow = false">
					<font-awesome-icon icon="fa-solid fa-times"
					class="cursor-pointer" />
				</div>
			</div>
			<div class="flex flex-wrap">
				<template v-if="itemsStore.images[itemId]">
					<div v-for="image in itemsStore.images[itemId]" :key="image.id_img"
						class="w-24 h-24 bg-gray-200 rounded m-2 flex items-center justify-center cursor-pointer">
						<template v-if="itemsStore.imagesURL[image.id_img]">
							<img :src="itemsStore.imagesURL[image.id_img]" alt="Image"
								:class="itemsStore.itemEdition.id_img == image.id_img ? 'border-2 border-blue-500' : 'border-2 border-transparent'"
								class="w-24 h-24 object-cover rounded"
								@click="itemsStore.itemEdition.id_img = image.id_img" />
						</template>
						<template v-else>
							{{ $t('item.VItemImageLoading') }}
						</template>
					</div>
				</template>
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
						<span class="h-5 w-80 text-sm">{{ $t('item.VItemDocumentSize') }} ({{ configsStore.getConfigByKey("max_size_document_in_mb") }}Mo)</span>
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.document || ' ' }}</span>
					</div>
				</div>
				<div class="flex justify-end space-x-2">
					<button type="button" @click="documentAddModalShow = false" class="px-4 py-2 bg-gray-300 rounded">
						{{ $t('item.VItemDocumentCancel') }}
					</button>
					<button type="button" @click="documentAdd" class="px-4 py-2 bg-blue-500 text-white rounded">
						{{ $t('item.VItemDocumentAdd') }}
					</button>
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
						<span class="h-5 w-80 text-sm">{{ $t('item.VItemDocumentSize') }} ({{ configsStore.getConfigByKey("max_size_document_in_mb") }}Mo)</span>
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.document || ' ' }}</span>
					</div>
				</div>
				<div class="flex justify-end space-x-2">
					<button type="button" @click="documentEditModalShow = false"
						class="px-4 py-2 bg-gray-300 rounded">
						{{ $t('item.VItemDocumentCancel') }}
					</button>
					<button type="button" @click="documentEdit" class="px-4 py-2 bg-blue-500 text-white rounded">
						{{ $t('item.VItemDocumentEdit') }}
					</button>
				</div>
			</Form>
		</div>
	</div>
	<ModalDeleteConfirm :show-modal="documentDeleteModalShow" @close-modal="documentDeleteModalShow = false"
		@delete-confirmed="documentDelete" :text-title="'item.VItemDocumentDeleteTitle'"
		:text-p="'item.VItemDocumentDeleteText'"/>

	<div v-if="imageAddModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="imageAddModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<Form :validation-schema="schemaAddImage" v-slot="{ errors }">
				<h2 class="text-xl mb-4">{{ $t('item.VItemImageAddTitle') }}</h2>
				<div class="flex flex-col">
					<div class="flex flex-col">
						<Field name="nom_img" type="text"
							v-model="imageModalData.nom_img"
							:placeholder="$t('item.VItemImageNamePlaceholder')"
							class="w-full p-2 border rounded"
							:class="{ 'border-red-500': errors.nom_img }" />
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.nom_img || ' ' }}</span>
					</div>
					<div class="flex flex-col">
						<Field name="image" type="file" @change="handleImageUpload" class="w-full p-2" accept="image/*" />
						<span class="h-5 w-80 text-sm">{{ $t('item.VItemImageSize') }} ({{ configsStore.getConfigByKey("max_size_document_in_mb") }}Mo)</span>
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.image || ' ' }}</span>
					</div>
				</div>
				<div class="flex justify-end space-x-2">
					<button type="button" @click="imageAddModalShow = false"
						class="px-4 py-2 bg-gray-300 rounded">
						{{ $t('item.VItemImageCancel') }}
					</button>
					<button type="button" @click="imageAdd" class="px-4 py-2 bg-blue-500 text-white rounded">
						{{ $t('item.VItemImageAdd') }}
					</button>
				</div>
			</Form>
		</div>
	</div>
	<ModalDeleteConfirm :show-modal="imageDeleteModalShow" @close-modal="imageDeleteModalShow = false"
		@delete-confirmed="imageDelete" :text-title="'item.VItemImageDeleteTitle'"
		:text-p="'item.VItemImageDeleteText'"/>
</template>

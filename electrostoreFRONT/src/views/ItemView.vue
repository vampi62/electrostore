<script setup>
import { onMounted, onBeforeUnmount, ref, computed, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const itemId = ref(route.params.id);

import { downloadFile, viewFile } from "@/utils";

import { useConfigsStore, useItemsStore, useTagsStore, useStoresStore, useCommandsStore, useProjetsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const itemsStore = useItemsStore();
const tagsStore = useTagsStore();
const storesStore = useStoresStore();
const commandsStore = useCommandsStore();
const projetsStore = useProjetsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (itemId.value === "new") {
		itemsStore.itemEdition = {
			loading: false,
		};
	} else {
		itemsStore.itemEdition = {
			loading: true,
		};
		try {
			await itemsStore.getItemById(itemId.value);
		} catch {
			delete itemsStore.items[itemId.value];
			addNotification({ message: "item.VItemNotFound", type: "error", i18n: true });
			router.push("/inventory");
			return;
		}
		itemsStore.getItemBoxByInterval(itemId.value, 100, 0, ["box"]);
		itemsStore.getItemTagByInterval(itemId.value, 100, 0, ["tag"]);
		itemsStore.getDocumentByInterval(itemId.value, 100, 0);
		itemsStore.getItemCommandByInterval(itemId.value, 100, 0, ["command"]);
		itemsStore.getItemProjetByInterval(itemId.value, 100, 0, ["projet"]);
		itemsStore.getImageByInterval(itemId.value, 100, 0);
		itemsStore.itemEdition = {
			loading: false,
			id_item: itemsStore.items[itemId.value].id_item,
			reference_name_item: itemsStore.items[itemId.value].reference_name_item,
			friendly_name_item: itemsStore.items[itemId.value].friendly_name_item,
			description_item: itemsStore.items[itemId.value].description_item,
			seuil_min_item: itemsStore.items[itemId.value].seuil_min_item,
			id_img: itemsStore.items[itemId.value].id_img,
		};
	}
}
onMounted(() => {
	fetchAllData();
	window.addEventListener("click", () => {
		selectedImageId.value = null;
	});
});
onBeforeUnmount(() => {
	itemsStore.itemEdition = {
		loading: false,
	};
	window.removeEventListener("click", () => {
		selectedImageId.value = null;
	});
});

const toggleBoxLed = async(boxId) => {
	let storeId = itemsStore.itemBoxs[itemId.value][boxId]["box"].id_store;
	try {
		await storesStore.showBoxById(storeId, boxId, { "red": 255, "green": 255, "blue": 255, "timeshow": 30, "animation": 4 });
		addNotification({ message: "item.VItemBoxShowSuccess", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};

// item
const itemDeleteModalShow = ref(false);
const itemSave = async() => {
	try {
		createSchema().validateSync(itemsStore.itemEdition, { abortEarly: false });
		if (itemId.value === "new") {
			await itemsStore.createItem({ ...itemsStore.itemEdition });
			addNotification({ message: "item.VItemCreated", type: "success", i18n: true });
		} else {
			await itemsStore.updateItem(itemId.value, { ...itemsStore.itemEdition });
			addNotification({ message: "item.VItemUpdated", type: "success", i18n: true });
		}
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
	if (itemId.value === "new") {
		itemId.value = String(itemsStore.itemEdition.id_item);
		router.push("/inventory/" + itemId.value);
	}
};
const itemDelete = async() => {
	try {
		await itemsStore.deleteItem(itemId.value);
		addNotification({ message: "item.VItemDeleted", type: "success", i18n: true });
		router.push("/inventory");
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	itemDeleteModalShow.value = false;
};

const getTotalQuantity = computed(() => {
	if (itemId.value === "new") {
		return 0;
	}
	return itemsStore.itemBoxs[itemId.value] ? Object.values(itemsStore.itemBoxs[itemId.value]).reduce((acc, box) => acc + box.qte_item_box, 0) : 0;
});

// box
const boxSave = async(box) => {
	if (itemsStore.itemBoxs[itemId.value][box.id_box]) {
		try {
			schemaBox.validateSync(box.tmp, { abortEarly: false });
			await itemsStore.updateItemBox(itemId.value, box.tmp.id_box, box.tmp);
			addNotification({ message: "item.VItemBoxUpdated", type: "success", i18n: true });
			box.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error", i18n: false });
			return;
		}
	} else {
		try {
			createSchema().validateSync(box.tmp, { abortEarly: false });
			await itemsStore.createItemBox(itemId.value, box.tmp);
			addNotification({ message: "item.VItemBoxAdded", type: "success", i18n: true });
			box.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error", i18n: false });
			return;
		}
	}
};

// document
const documentAddModalShow = ref(false);
const documentDeleteModalShow = ref(false);
const documentModalData = ref({ id_item_document: null, name_item_document: "", document: null });
const documentAddOpenModal = () => {
	documentModalData.value = { name_item_document: "", document: null };
	documentAddModalShow.value = true;
};
const documentDeleteOpenModal = (doc) => {
	documentModalData.value = doc;
	documentDeleteModalShow.value = true;
};
const documentAdd = async() => {
	try {
		schemaAddDocument.validateSync(documentModalData.value, { abortEarly: false });
		await itemsStore.createDocument(itemId.value, documentModalData.value);
		addNotification({ message: "item.VItemDocumentAdded", type: "success", i18n: true });
		documentAddModalShow.value = false;
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const documentEdit = async(row) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		await itemsStore.updateDocument(itemId.value, row.id_item_document, row);
		addNotification({ message: "item.VItemDocumentUpdated", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const documentDelete = async() => {
	try {
		await itemsStore.deleteDocument(itemId.value, documentModalData.value.id_item_document);
		addNotification({ message: "item.VItemDocumentDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	documentDeleteModalShow.value = false;
};
const documentDownload = async(fileContent) => {
	const file = await itemsStore.downloadDocument(itemId.value, fileContent.id_item_document);
	downloadFile(file, { keyName: fileContent.name_item_document, keyType: fileContent.type_item_document });
};
const documentView = async(fileContent) => {
	const file = await itemsStore.downloadDocument(itemId.value, fileContent.id_item_document);
	if (viewFile(file, { keyName: fileContent.name_item_document, keyType: fileContent.type_item_document })) {
		addNotification({ message: "item.VItemDocumentOpenInNewTab", type: "success", i18n: true });
	} else {
		addNotification({ message: "item.VItemDocumentNotSupported", type: "error", i18n: true });
	}
};

// image
const imageSelectModalShow = ref(false);
const imageAddModalShow = ref(false);
const imageDeleteModalShow = ref(false);
const selectedImageId = ref(null);
const imageModalData = ref({ id_img: null, nom_img: "", description_img: "undefined", image: null });
const imageSelectOpenModal = () => {
	if (itemId.value === "new") {
		return;
	}
	if (Object.keys(itemsStore.images[itemId.value]).length === 0) {
		addNotification({ message: "item.VItemImageEmpty", type: "error", i18n: true });
		return;
	}
	if (itemsStore.images[itemId.value]) {
		imageSelectModalShow.value = true;
	}
};
const imageAddOpenModal = () => {
	imageModalData.value = { nom_img: "", description_img: "undefined", image: null };
	imageAddModalShow.value = true;
};
const imageDeleteOpenModal = (doc) => {
	imageModalData.value = doc;
	imageDeleteModalShow.value = true;
};
const imageAdd = async() => {
	try {
		await itemsStore.createImage(itemId.value, imageModalData.value);
		addNotification({ message: "item.VItemImageAdded", type: "success", i18n: true });
		imageAddModalShow.value = false;
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};
const imageDelete = async() => {
	try {
		await itemsStore.deleteImage(itemId.value, imageModalData.value.id_img);
		addNotification({ message: "item.VItemImageDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	imageDeleteModalShow.value = false;
};
const imageDownload = async(imageContent) => {
	if (!itemsStore.imagesURL[imageContent.id_img]) {
		await itemsStore.showImageById(itemId.value, imageContent.id_img);
	}
	if (!itemsStore.imagesURL[imageContent.id_img]) {
		addNotification({ message: "item.VItemImageDownloadError", type: "error", i18n: true });
		return;
	}
	downloadFile(itemsStore.imagesURL[imageContent.id_img], { keyName: imageContent.nom_img, keyType: "image/png" });
};

// tag
const filterTag = ref([
	{ key: "nom_tag", value: "", type: "text", label: "", placeholder: t("item.VItemTagFilterPlaceholder"), compareMethod: "contain", class: "w-full" },
]);
function tagSave(id_tag) {
	try {
		itemsStore.createItemTag(itemId.value,  { id_tag: id_tag });
		addNotification({ message: "item.VItemTagAdded", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
}
function tagDelete(id_tag) {
	try {
		itemsStore.deleteItemTag(itemId.value, id_tag);
		addNotification({ message: "item.VItemTagDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
}

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

const createSchema = () => {
	return Yup.object().shape({
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
};

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
});

const schemaAddImage = Yup.object().shape({
	nom_img: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("item.VItemImageNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("item.VItemImageNameRequired")),
	image: Yup.mixed()
		.required(t("item.VItemImageRequired"))
		.test("fileSize", t("item.VItemImageSize") + " " + configsStore.getConfigByKey("max_size_document_in_mb") + "Mo", (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});

const labelForm = [
	{ key: "reference_name_item", label: "item.VItemName", type: "text" },
	{ key: "friendly_name_item", label: "item.VItemFriendlyName", type: "text" },
	{ key: "description_item", label: "item.VItemDescription", type: "textarea" },
	{ key: "seuil_min_item", label: "item.VItemSeuilMin", type: "number" },
	{ key: "quantity", label: "item.VItemTotalQuantity", type: "computed", value: getTotalQuantity },
	{ key: "id_img", label: "item.VItemImage", type: "custom" },
];
const labelTableauModalTag = ref([
	{ label: "item.VItemTagName", sortable: true, key: "nom_tag", type: "text" },
	{ label: "item.VItemTagActions", sortable: false, key: "", type: "buttons", buttons: [
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
const labelTableauDocument = ref([
	{ label: "item.VItemDocumentName", sortable: true, key: "name_item_document", type: "text", canEdit: true },
	{ label: "item.VItemDocumentType", sortable: true, key: "type_item_document", type: "text" },
	{ label: "item.VItemDocumentDate", sortable: true, key: "created_at", type: "datetime" },
	{ label: "item.VItemDocumentActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			condition: "!rowData.tmp",
			action: (row) => {
				row.tmp = { ...row };
			},
			class: "text-blue-500 cursor-pointer hover:text-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			condition: "rowData.tmp",
			action: (row) => {
				row.tmp = null;
			},
			class: "text-gray-500 cursor-pointer hover:text-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			condition: "rowData.tmp",
			action: (row) => documentEdit(row.tmp),
			class: "text-green-500 cursor-pointer hover:text-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-eye",
			action: (row) => documentView(row),
			class: "text-green-500 cursor-pointer hover:text-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-download",
			action: (row) => documentDownload(row),
			class: "text-yellow-500 cursor-pointer hover:text-yellow-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => documentDeleteOpenModal(row),
			class: "text-red-500 cursor-pointer hover:text-red-600",
		},
	] },
]);
const labelTableauBox = ref([
	{ label: "item.VItemBoxId", sortable: true, key: "id_box", type: "text" },
	{ label: "item.VItemBoxQuantity", sortable: true, key: "qte_item_box", type: "number", canEdit: true },
	{ label: "item.VItemBoxMaxThreshold", sortable: true, key: "seuil_max_item_item_box", type: "number", canEdit: true },
	{ label: "item.VItemBoxActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			condition: "!rowData.tmp",
			action: (row) => {
				row.tmp = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			condition: "rowData.tmp",
			action: (row) => boxSave(row),
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
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-eye",
			action: (row) => toggleBoxLed(row.id_box),
			class: "px-3 py-1 bg-yellow-500 text-white rounded-lg hover:bg-yellow-600",
			animation: true,
		},
	] },
]);
const labelTableauCommand = ref([
	{ label: "item.VItemCommandDate", sortable: true, key: "date_command", keyStore: "id_command", store: "1", type: "datetime" },
	{ label: "item.VItemCommandStatus", sortable: true, key: "status_command", keyStore: "id_command", store: "1", type: "text" },
	{ label: "item.VItemCommandQte", sortable: true, key: "qte_command_item", type: "number" },
	{ label: "item.VItemCommandPrice", sortable: true, key: "prix_command_item", type: "number" },
]);
const labelTableauProjet = ref([
	{ label: "item.VItemProjetName", sortable: true, key: "nom_projet", keyStore: "id_projet", store: "1", type: "text" },
	{ label: "item.VItemProjetDate", sortable: true, key: "date_debut_projet", keyStore: "id_projet", store: "1", type: "datetime" },
	{ label: "item.VItemProjetDateFin", sortable: true, key: "date_fin_projet", keyStore: "id_projet", store: "1", type: "datetime" },
	{ label: "item.VItemProjetStatus", sortable: true, key: "status_projet", keyStore: "id_projet", store: "1", type: "text" },
	{ label: "item.VItemProjetQte", sortable: true, key: "qte_projet_item", type: "number" },
]);
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('item.VItemTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/inventory', save: { roleRequired: 0, loading: itemsStore.itemEdition.loading }, delete: { roleRequired: 0 } }"
			:id="itemId" :store-user="authStore.user" @button-save="itemSave" @button-delete="itemDeleteModalShow = true"/>
	</div>
	<div v-if="itemsStore.items[itemId] || itemId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="itemsStore.itemEdition">
				<template #id_img>
					<div class="flex justify-center items-center"
						:class="{ 'cursor-pointer': !itemsStore.itemEdition.loading && itemId != 'new', 'cursor-not-allowed': itemId == 'new' }"
						@click="imageSelectOpenModal">
						<template v-if="itemsStore.itemEdition.id_img">
							<img v-if="itemsStore.thumbnailsURL[itemsStore.itemEdition.id_img]"
								:src="itemsStore.thumbnailsURL[itemsStore.itemEdition.id_img]" alt="Main"
								class="w-48 h-48 object-cover rounded" />
							<span v-else class="w-48 h-48 object-cover rounded">
								{{ $t('item.VInventoryLoading') }}
							</span>
						</template>
						<template v-else>
							<img src="../assets/nopicture.webp" alt="Not Found"
								class="w-48 h-48 object-cover rounded" />
						</template>
					</div>
				</template>
			</FormContainer>
			<Tags :current-tags="itemsStore.itemTags[itemId] || {}" :tags-store="tagsStore.tags" :can-edit="itemId !== 'new' && authStore.user.role_user >= 1"
				:delete-function="(value) => tagDelete(value)"
				:fetch-function="(offset, limit) => tagsStore.getTagByInterval(limit, offset)"
				:total-count="Number(tagsStore.tagsTotalCount || 0)"
				:filter-modal="filterTag"
				:tableau-modal="{ 'label': labelTableauModalTag, 'meta': { key: 'id_tag' }, 'css': { component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }
								, 'loading': tagsStore.tagsLoading }"
				:meta ="{ 'keyPoids': 'poids_tag', 'keyName': 'nom_tag' }"
				/>
		</div>
		<CollapsibleSection title="item.VitemBoxs"
			:total-count="Number(itemsStore.itemBoxsTotalCount[itemId] || 0)" :id-page="itemId">
			<template #append-row>
				<Tableau :labels="labelTableauBox" :meta="{ key: 'id_box' }"
					:store-data="[itemsStore.itemBoxs[itemId]]"
					:loading="itemsStore.itemBoxsLoading" :schema="schemaBox"
					:total-count="Number(itemsStore.itemBoxsTotalCount[itemId])"
					:loaded-count="Object.keys(itemsStore.itemBoxs[itemId] || {}).length"
					:fetch-function="(offset, limit) => itemsStore.getItemBoxByInterval(itemId, limit, offset)"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.VItemDocuments"
			:total-count="Number(itemsStore.documentsTotalCount[itemId] || 0)" :id-page="itemId">
			<template #append-row>
				<button type="button" @click="documentAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('item.VItemAddDocument') }}
				</button>
				<Tableau :labels="labelTableauDocument" :meta="{ key: 'id_item_document' }"
					:store-data="[itemsStore.documents[itemId]]"
					:loading="itemsStore.documentsLoading"
					:total-count="Number(itemsStore.documentsTotalCount[itemId])"
					:loaded-count="Object.keys(itemsStore.documents[itemId] || {}).length"
					:fetch-function="(offset, limit) => itemsStore.getDocumentByInterval(itemId, limit, offset)"
					:tableau-css="{ component: 'max-h-64' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.VItemImages"
			:total-count="Number(itemsStore.imagesTotalCount[itemId] || 0)" :id-page="itemId">
			<template #append-row>
				<button type="button" @click="imageAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('item.VItemAddImage') }}
				</button>
				<div class="flex flex-wrap relative">
					<template v-if="itemsStore.images[itemId]">
						<div v-for="image in itemsStore.images[itemId]" :key="image.id_img" @click.stop
							class="w-48 h-48 bg-gray-200 rounded m-2 flex items-center relative">
							<template v-if="itemsStore.thumbnailsURL[image.id_img]">
								<img :src="itemsStore.thumbnailsURL[image.id_img]"
									class="w-48 h-48 object-cover rounded" :alt="image.nom_img"
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
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.VItemCommands"
			:total-count="Number(itemsStore.itemCommandsTotalCount[itemId] || 0)" :id-page="itemId">
			<template #append-row>
				<Tableau :labels="labelTableauCommand" :meta="{ key: 'id_item', path: '/commands/' }"
					:store-data="[itemsStore.itemCommands[itemId],commandsStore.commands]"
					:loading="itemsStore.itemCommandsLoading"
					:total-count="Number(itemsStore.itemCommandsTotalCount[itemId])"
					:loaded-count="Object.keys(itemsStore.itemCommands[itemId] || {}).length"
					:fetch-function="(offset, limit) => itemsStore.getItemCommandByInterval(itemId, limit, offset)"
					:tableau-css="{ component: 'max-h-64' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.VItemProjets"
			:total-count="Number(itemsStore.itemProjetsTotalCount[itemId] || 0)" :id-page="itemId">
			<template #append-row>
				<Tableau :labels="labelTableauProjet" :meta="{ key: 'id_projet', path: '/projets/' }"
					:store-data="[itemsStore.itemProjets[itemId],projetsStore.projets]"
					:loading="itemsStore.itemProjetsLoading"
					:total-count="Number(itemsStore.itemProjetsTotalCount[itemId])"
					:loaded-count="Object.keys(itemsStore.itemProjets[itemId] || {}).length"
					:fetch-function="(offset, limit) => itemsStore.getItemProjetByInterval(itemId, limit, offset)"
					:tableau-css="{ component: 'max-h-64' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('item.VItemLoading') }}</div>
	</div>
	
	<ModalDeleteConfirm :show-modal="itemDeleteModalShow" @close-modal="itemDeleteModalShow = false"
		:delete-action="itemDelete" :text-title="'item.VItemDeleteTitle'"
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
						<template v-if="itemsStore.thumbnailsURL[image.id_img]">
							<img :src="itemsStore.thumbnailsURL[image.id_img]" :alt="image.nom_img"
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

	<ModalAddFile :show-modal="documentAddModalShow" @close-modal="documentAddModalShow = false"
		:text-title="'item.VItemDocumentAddTitle'" :schema-add="schemaAddDocument"
		:modal-data="documentModalData" :add-action="documentAdd" :key-name-document="'name_item_document'" :key-file-document="'document'"
		:max-size-in-mb="configsStore.getConfigByKey('max_size_document_in_mb')"
		:text-max-size="'item.VItemDocumentSize'" :text-placeholder-document="'item.VItemDocumentNamePlaceholder'"
	/>

	<ModalDeleteConfirm :show-modal="documentDeleteModalShow" @close-modal="documentDeleteModalShow = false"
		:delete-action="documentDelete" :text-title="'item.VItemDocumentDeleteTitle'"
		:text-p="'item.VItemDocumentDeleteText'"/>

	<ModalAddFile :show-modal="imageAddModalShow" @close-modal="imageAddModalShow = false"
		:text-title="'item.VItemImageAddTitle'" :schema-add="schemaAddImage"
		:modal-data="imageModalData" :add-action="imageAdd" :key-name-document="'nom_img'" :key-file-document="'image'"
		:max-size-in-mb="configsStore.getConfigByKey('max_size_document_in_mb')"
		:text-max-size="'item.VItemImageSize'" :text-placeholder-document="'item.VItemImageNamePlaceholder'"
	/>

	<ModalDeleteConfirm :show-modal="imageDeleteModalShow" @close-modal="imageDeleteModalShow = false"
		:delete-action="imageDelete" :text-title="'item.VItemImageDeleteTitle'"
		:text-p="'item.VItemImageDeleteText'"/>
</template>

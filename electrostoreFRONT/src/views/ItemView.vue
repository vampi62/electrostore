<script setup>
import { onMounted, onBeforeUnmount, ref, computed, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const itemId = ref(route.params.id);
const preset = ref(route.query.preset || null);

import { downloadFile, viewFile } from "@/utils";

import { useConfigsStore, useItemsStore, useTagsStore, useStoresStore, useCommandsStore, useProjetsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const itemsStore = useItemsStore();
const tagsStore = useTagsStore();
const storesStore = useStoresStore();
const commandsStore = useCommandsStore();
const projetsStore = useProjetsStore();
const authStore = useAuthStore();

const formContainer = ref(null);

async function fetchAllData() {
	if (itemId.value === "new") {
		loadToEdition(itemId.value);
		if (preset.value) {
			preset.value.split(";").forEach((pair) => {
				const [key, value] = pair.split(":");
				if (key && value) {
					itemsStore.itemEdition[key] = value;
				}
			});
		}
	} else {
		itemsStore.itemEdition = {
			loading: true,
		};
		try {
			await itemsStore.getItemById(itemId.value);
		} catch {
			delete itemsStore.items[itemId.value];
			addNotification({ message: t("item.NotFound"), type: "error" });
			router.push("/inventory");
			return;
		}
		itemsStore.getItemTagByInterval(itemId.value, 100, 0, ["tag"]);
		itemsStore.getImageByInterval(itemId.value, 100, 0);
		loadToEdition(itemId.value);
	}
}
function loadToEdition(id) {
	if (id === "new") {
		itemsStore.itemEdition = {
			loading: false,
		};
	} else {
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
		addNotification({ message: t("item.BoxShowSuccess"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

// item
const itemDeleteModalShow = ref(false);
const itemSave = async() => {
	try {
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("item.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			itemsStore.itemEdition.loading = false;
			return;
		}
		if (itemId.value === "new") {
			const newId = await itemsStore.createItem({ ...itemsStore.itemEdition });
			loadToEdition(newId);
			addNotification({ message: t("item.Created"), type: "success" });
			itemId.value = String(newId);
			router.push("/inventory/" + itemId.value);
		} else {
			await itemsStore.updateItem(itemId.value, { ...itemsStore.itemEdition });
			loadToEdition(itemId.value);
			addNotification({ message: t("item.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		itemsStore.itemEdition.loading = false;
	}
};
const itemDelete = async() => {
	try {
		await itemsStore.deleteItem(itemId.value);
		addNotification({ message: t("item.Deleted"), type: "success" });
		router.push("/inventory");
	} catch (e) {
		addNotification({ message: e, type: "error" });
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
			addNotification({ message: t("item.BoxUpdated"), type: "success" });
			box.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error" });
			return;
		}
	} else {
		try {
			createSchema().validateSync(box.tmp, { abortEarly: false });
			await itemsStore.createItemBox(itemId.value, box.tmp);
			addNotification({ message: t("item.BoxAdded"), type: "success" });
			box.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error" });
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
		addNotification({ message: t("item.DocumentAdded"), type: "success" });
		documentAddModalShow.value = false;
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const documentEdit = async(row) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		await itemsStore.updateDocument(itemId.value, row.id_item_document, row);
		addNotification({ message: t("item.DocumentUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const documentDelete = async() => {
	try {
		await itemsStore.deleteDocument(itemId.value, documentModalData.value.id_item_document);
		addNotification({ message: t("item.DocumentDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
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
		addNotification({ message: t("item.DocumentOpenInNewTab"), type: "success" });
	} else {
		addNotification({ message: t("item.DocumentNotSupported"), type: "error" });
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
		addNotification({ message: t("item.ImageEmpty"), type: "error" });
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
		addNotification({ message: t("item.ImageAdded"), type: "success" });
		imageAddModalShow.value = false;
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const imageDelete = async() => {
	try {
		await itemsStore.deleteImage(itemId.value, imageModalData.value.id_img);
		addNotification({ message: t("item.ImageDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	imageDeleteModalShow.value = false;
};
const imageDownload = async(imageContent) => {
	if (!itemsStore.imagesURL[imageContent.id_img]) {
		await itemsStore.showImageById(itemId.value, imageContent.id_img);
	}
	if (!itemsStore.imagesURL[imageContent.id_img]) {
		addNotification({ message: t("item.ImageDownloadError"), type: "error" });
		return;
	}
	downloadFile(itemsStore.imagesURL[imageContent.id_img], { keyName: imageContent.nom_img, keyType: "image/png" });
};

// tag
const filterTag = ref([
	{ key: "nom_tag", value: "", type: "text", label: "", placeholder: t("item.TagFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);
function tagSave(id_tag) {
	try {
		itemsStore.createItemTag(itemId.value,  { id_tag: id_tag });
		addNotification({ message: t("item.TagAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function tagDelete(id_tag) {
	try {
		itemsStore.deleteItemTag(itemId.value, id_tag);
		addNotification({ message: t("item.TagDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

const schemaBox = Yup.object().shape({
	qte_item_box: Yup.number()
		.required(t("item.BoxQuantityRequired"))
		.typeError(t("item.BoxQuantityNumber"))
		.min(0, t("item.BoxQuantityMin")),
	seuil_max_item_item_box: Yup.number()
		.required(t("item.BoxMaxThresholdRequired"))
		.typeError(t("item.BoxMaxThresholdNumber"))
		.min(1, t("item.BoxMaxThresholdMin")),
});

const createSchema = () => {
	const edition = itemsStore.itemEdition;
	const shape = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.reference_name_item = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("item.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("item.NameRequired"));
	shape.friendly_name_item = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("item.FriendlyNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("item.FriendlyNameRequired"));
	shape.description_item = Yup.string()
		.max(configsStore.getConfigByKey("max_length_description"), t("item.DescriptionMaxLength", { count: configsStore.getConfigByKey("max_length_description") }))
		.required(t("item.DescriptionRequired"));
	shape.seuil_min_item = Yup.number()
		.min(0, t("item.SeuilMinMin"))
		.typeError(t("item.SeuilMinType"))
		.required(t("item.SeuilMinRequired"));
	shape.id_img = Yup.string()
		.nullable();
	return Yup.object().shape(shape);
};

const schemaAddDocument = Yup.object().shape({
	name_item_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("item.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("item.DocumentNameRequired")),
	document: Yup.mixed()
		.required(t("item.DocumentRequired"))
		.test("fileSize", t("item.DocumentSize", { count: configsStore.getConfigByKey("max_size_document_in_mb") }), (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});
const schemaEditDocument = Yup.object().shape({
	name_item_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("item.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("item.DocumentNameRequired")),
});

const schemaAddImage = Yup.object().shape({
	nom_img: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("item.ImageNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("item.ImageNameRequired")),
	image: Yup.mixed()
		.required(t("item.ImageRequired"))
		.test("fileSize", t("item.ImageSize") + " " + configsStore.getConfigByKey("max_size_document_in_mb") + "Mo", (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});

const labelForm = [
	{ key: "reference_name_item", label: "item.Name", type: "text" },
	{ key: "friendly_name_item", label: "item.FriendlyName", type: "text" },
	{ key: "description_item", label: "item.Description", type: "textarea" },
	{ key: "seuil_min_item", label: "item.SeuilMin", type: "number" },
	{ key: "quantity", label: "item.TotalQuantity", type: "computed", value: getTotalQuantity },
	{ key: "id_img", label: "item.Image", type: "custom" },
];
const labelTableauModalTag = ref([
	{ label: "item.TagName", sortable: true, key: "nom_tag", valueKey: "nom_tag", type: "text" },
	{ label: "item.TagActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "!store[1]?.[rowData.id_tag]",
			action: (row) => tagSave(row.id_tag),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "store[1]?.[rowData.id_tag]",
			action: (row) => tagDelete(row.id_tag),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauDocument = ref([
	{ label: "item.DocumentName", sortable: true, key: "name_item_document", valueKey: "name_item_document", type: "text", canEdit: true },
	{ label: "item.DocumentType", sortable: true, key: "type_item_document", valueKey: "type_item_document", type: "text" },
	{ label: "item.DocumentDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "item.DocumentActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_item_document",
			action: (row) => {
				itemsStore.documentEdition[row.id_item_document] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_item_document",
			action: (row) => {
				delete itemsStore.documentEdition[row.id_item_document];
			},
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_item_document",
			action: (row) => documentEdit(itemsStore.documentEdition[row.id_item_document]),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-eye",
			action: (row) => documentView(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-download",
			action: (row) => documentDownload(row),
			class: "px-3 py-1 bg-yellow-500 text-white rounded-lg hover:bg-yellow-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => documentDeleteOpenModal(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauBox = ref([
	{ label: "item.BoxId", sortable: true, key: "id_box", valueKey: "id_box", type: "number" },
	{ label: "item.BoxQuantity", sortable: true, key: "qte_item_box", valueKey: "qte_item_box", type: "number", canEdit: true },
	{ label: "item.BoxMaxThreshold", sortable: true, key: "seuil_max_item_item_box", valueKey: "seuil_max_item_item_box", type: "number", canEdit: true },
	{ label: "item.BoxActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_box",
			action: (row) => {
				itemsStore.itemBoxEdition[row.id_box] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_box",
			action: (row) => {
				delete itemsStore.itemBoxEdition[row.id_box];
			},
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_box",
			action: (row) => boxSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
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
	{ label: "item.CommandDate", sortable: true, key: "Command.date_command", sourceKey: "id_command", type: "datetime", 
		storeRessourceId: 1, valueKey: "date_command" },
	{ label: "item.CommandStatus", sortable: true, key: "Command.status_command", sourceKey: "id_command", type: "text", 
		storeRessourceId: 1, valueKey: "status_command" },

	{ label: "item.CommandQte", sortable: true, key: "qte_command_item", valueKey: "qte_command_item", type: "number" },
	{ label: "item.CommandPrice", sortable: true, key: "prix_command_item", valueKey: "prix_command_item", type: "number" },
]);
const labelTableauProjet = ref([
	{ label: "item.ProjetName", sortable: true, key: "Projet.nom_projet", sourceKey: "id_projet", type: "text", 
		storeRessourceId: 1, valueKey: "nom_projet" },
	{ label: "item.ProjetDate", sortable: true, key: "Projet.date_debut_projet", sourceKey: "id_projet", type: "datetime", 
		storeRessourceId: 1, valueKey: "date_debut_projet" },
	{ label: "item.ProjetDateFin", sortable: true, key: "Projet.date_fin_projet", sourceKey: "id_projet", type: "datetime", 
		storeRessourceId: 1, valueKey: "date_fin_projet" },
	{ label: "item.ProjetStatus", sortable: true, key: "Projet.status_projet", sourceKey: "id_projet", type: "text", 
		storeRessourceId: 1, valueKey: "status_projet" },

	{ label: "item.ProjetQte", sortable: true, key: "qte_projet_item", valueKey: "qte_projet_item", type: "number" },
]);
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('item.Title') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/inventory', save: { roleRequired: authStore.hasPermission([0, 1, 2]), loading: itemsStore.itemEdition.loading }, delete: { roleRequired: authStore.hasPermission([0, 1, 2]) } }"
			:id="itemId" @button-save="itemSave" @button-delete="itemDeleteModalShow = true"/>
	</div>
	<div v-if="itemsStore.items[itemId] || itemId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="itemsStore.itemEdition">
				<template #id_img>
					<div class="flex justify-center items-center"
						:class="{ 'cursor-pointer': !itemsStore.itemEdition?.loading && itemId != 'new', 'cursor-not-allowed': itemId == 'new' }"
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
			<Tags :current-tags="itemsStore.itemTags[itemId] || {}" :tags-store="tagsStore.tags" :can-edit="itemId !== 'new' && authStore.hasPermission([1, 2])"
				:delete-function="(value) => tagDelete(value)"
				:filter-modal="filterTag"
				:tableau-modal="{ 'label': labelTableauModalTag, 'meta': { key: 'id_tag', preventClear: true }, 'css': { component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }
								, 'loading': tagsStore.tagsLoading, 'fetchFunction': (limit, offset, expand, filter, sort, clear) => tagsStore.getTagByInterval(limit, offset, expand, filter, sort, clear)
								, 'totalCount': Number(tagsStore.tagsTotalCount || 0) }"
				:meta ="{ 'keyPoids': 'poids_tag', 'keyName': 'nom_tag' }"
				/>
		</div>
		<CollapsibleSection title="item.Boxs"
			:total-count="Number(itemsStore.itemBoxsTotalCount[itemId] || 0)" :permission="itemId !=='new'">
			<template #append-row>
				<Tableau :labels="labelTableauBox" :meta="{ key: 'id_box', expand: ['box'] }"
					:store-data="[itemsStore.itemBoxs[itemId]]"
					:store-edition="itemsStore.itemBoxEdition"
					:loading="itemsStore.itemBoxsLoading"
					:schema="schemaBox"
					:total-count="Number(itemsStore.itemBoxsTotalCount[itemId])"
					:fetch-function="itemId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemBoxByInterval(itemId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.Documents"
			:total-count="Number(itemsStore.documentsTotalCount[itemId] || 0)" :permission="itemId !=='new'">
			<template #append-row>
				<button type="button" @click="documentAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('item.AddDocument') }}
				</button>
				<Tableau :labels="labelTableauDocument" :meta="{ key: 'id_item_document' }"
					:store-data="[itemsStore.documents[itemId]]"
					:store-edition="itemsStore.documentEdition"
					:schema="schemaEditDocument"
					:loading="itemsStore.documentsLoading"
					:total-count="Number(itemsStore.documentsTotalCount[itemId])"
					:fetch-function="itemId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getDocumentByInterval(itemId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.Images"
			:total-count="Number(itemsStore.imagesTotalCount[itemId] || 0)" :permission="itemId !=='new'">
			<template #append-row>
				<button type="button" @click="imageAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('item.AddImage') }}
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
								{{ $t('item.ImageLoading') }}
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
		<CollapsibleSection title="item.Commands"
			:total-count="Number(itemsStore.itemCommandsTotalCount[itemId] || 0)" :permission="itemId !=='new'">
			<template #append-row>
				<Tableau :labels="labelTableauCommand" :meta="{ key: 'id_item', path: '/commands/', expand: ['command'] }"
					:store-data="[itemsStore.itemCommands[itemId],commandsStore.commands]"
					:loading="itemsStore.itemCommandsLoading"
					:total-count="Number(itemsStore.itemCommandsTotalCount[itemId])"
					:fetch-function="itemId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemCommandByInterval(itemId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.Projets"
			:total-count="Number(itemsStore.itemProjetsTotalCount[itemId] || 0)" :permission="itemId !=='new'">
			<template #append-row>
				<Tableau :labels="labelTableauProjet" :meta="{ key: 'id_projet', path: '/projets/', expand: ['projet'] }"
					:store-data="[itemsStore.itemProjets[itemId],projetsStore.projets]"
					:loading="itemsStore.itemProjetsLoading"
					:total-count="Number(itemsStore.itemProjetsTotalCount[itemId])"
					:fetch-function="itemId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemProjetByInterval(itemId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('item.Loading') }}</div>
	</div>
	
	<ModalDeleteConfirm :show-modal="itemDeleteModalShow" @close-modal="itemDeleteModalShow = false"
		:delete-action="itemDelete" :text-title="'item.DeleteTitle'"
		:text-p="'item.DeleteText'"/>

	<div v-if="imageSelectModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="imageSelectModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<div class="flex justify-between items-center mb-2">
				<h2 class="text-xl">{{ $t('item.ImageSelectTitle') }}</h2>
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
							{{ $t('item.ImageLoading') }}
						</template>
					</div>
				</template>
			</div>
		</div>
	</div>

	<ModalAddFile :show-modal="documentAddModalShow" @close-modal="documentAddModalShow = false"
		:text-title="'item.DocumentAddTitle'" :schema-add="schemaAddDocument"
		:modal-data="documentModalData" :add-action="documentAdd" :key-name-document="'name_item_document'" :key-file-document="'document'"
		:max-size-in-mb="configsStore.getConfigByKey('max_size_document_in_mb')"
		:text-max-size="'item.DocumentSize'" :text-placeholder-document="'item.DocumentNamePlaceholder'"
		file-type="document"
	/>

	<ModalDeleteConfirm :show-modal="documentDeleteModalShow" @close-modal="documentDeleteModalShow = false"
		:delete-action="documentDelete" :text-title="'item.DocumentDeleteTitle'"
		:text-p="'item.DocumentDeleteText'"/>

	<ModalAddFile :show-modal="imageAddModalShow" @close-modal="imageAddModalShow = false"
		:text-title="'item.ImageAddTitle'" :schema-add="schemaAddImage"
		:modal-data="imageModalData" :add-action="imageAdd" :key-name-document="'nom_img'" :key-file-document="'image'"
		:max-size-in-mb="configsStore.getConfigByKey('max_size_document_in_mb')"
		:text-max-size="'item.ImageSize'" :text-placeholder-document="'item.ImageNamePlaceholder'"
		file-type="image"
	/>

	<ModalDeleteConfirm :show-modal="imageDeleteModalShow" @close-modal="imageDeleteModalShow = false"
		:delete-action="imageDelete" :text-title="'item.ImageDeleteTitle'"
		:text-p="'item.ImageDeleteText'"/>
</template>

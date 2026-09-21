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

import { ItemHistoryType } from "@/enums";

import { useConfigsStore, useItemsStore, useTagsStore, useStoresStore, useCommandsStore, useProjectsStore, useAuthStore, useUsersStore } from "@/stores";
const configsStore = useConfigsStore();
const itemsStore = useItemsStore();
const tagsStore = useTagsStore();
const storesStore = useStoresStore();
const commandsStore = useCommandsStore();
const projectsStore = useProjectsStore();
const authStore = useAuthStore();
const usersStore = useUsersStore();

const formContainer = ref(null);

async function fetchAllData() {
	if (itemId.value === "new") {
		itemsStore.loadToEdition(itemId.value, preset.value);
	} else {
		itemsStore.setLoadingEdition(itemId.value, true);
		try {
			await itemsStore.getItemById(itemId.value);
		} catch {
			delete itemsStore.items[itemId.value];
			addNotification({ message: t("item.NotFound"), type: "error" });
			router.push("/inventory");
			return;
		}
		itemsStore.getItemTagByInterval(itemId.value, 100, 0, ["tag"]);
		itemsStore.loadToEdition(itemId.value);
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	itemsStore.clearEdition(itemId.value);
	resetImageSelection();
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
			itemsStore.setLoadingEdition(itemId.value, false);
			return;
		}
		itemsStore.itemEdition[itemId.value].isFormData = true;
		const id = await itemsStore.saveAllChanges(itemId.value);
		itemsStore.loadToEdition(id);
		if (itemId.value === "new") {
			addNotification({ message: t("item.Created"), type: "success" });
			itemId.value = String(id);
			router.push("/inventory/" + itemId.value);
		} else {
			addNotification({ message: t("item.Updated"), type: "success" });
		}
		resetImageSelection();
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		itemsStore.setLoadingEdition(itemId.value, false);
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
	return itemsStore.itemBoxs[itemId.value] ? Object.values(itemsStore.itemBoxs[itemId.value]).reduce((acc, box) => acc + box.quantity_item_box, 0) : 0;
});

// box
const boxEdit = (box) => {
	try {
		schemaBox.validateSync(box, { abortEarly: false });
		itemsStore.valideItemBoxEditionById(itemId.value, box.id_box, "modified");
		delete itemsStore.itemBoxEdition[itemId.value][box.id_box];
		addNotification({ message: t("item.BoxUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const boxRestore = (row) => {
	try {
		delete itemsStore.itemBoxReady[itemId.value][row.id_box];
		addNotification({ message: t("item.BoxRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const boxDelete = (row) => {
	try {
		itemsStore.valideItemBoxEditionById(itemId.value, row.id_box, "deleted");
		delete itemsStore.itemBoxEdition[itemId.value][row.id_box];
		addNotification({ message: t("item.BoxDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

// document
const documentAddModalShow = ref(false);
const documentAdd = async(files) => {
	for (const file of files) {
		const documentModalData = { name_item_document: file.name, document: file.document };
		const newId = itemsStore.getAvailableNewDocumentId(itemId.value);
		itemsStore.documentEdition[itemId.value][newId] = documentModalData;
		try {
			schemaAddDocument.validateSync(documentModalData, { abortEarly: false });
			itemsStore.valideDocumentEditionById(itemId.value, newId, "created", true);
			delete itemsStore.documentEdition[itemId.value][newId];
			addNotification({ message: t("item.DocumentAdded"), type: "success" });
		} catch (e) {
			addNotification({ message: e, type: "error" });
		}
	}
	documentAddModalShow.value = false;
};
const documentEdit = (row) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		itemsStore.valideDocumentEditionById(itemId.value, row.id_item_document, 
			itemsStore.documentEdition[itemId.value][row.id_item_document]?.status === "created" ? "created" : "modified");
		delete itemsStore.documentEdition[itemId.value][row.id_item_document];
		addNotification({ message: t("item.DocumentUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const documentRestore = (row) => {
	try {
		delete itemsStore.documentReady[itemId.value][row.id_item_document];
		addNotification({ message: t("item.DocumentRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const documentDelete = (row) => {
	try {
		itemsStore.valideDocumentEditionById(itemId.value, row.id_item_document, "deleted");
		delete itemsStore.documentEdition[itemId.value][row.id_item_document];
		addNotification({ message: t("item.DocumentDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
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
const imageInputRef = ref(null);
const localImagePreviewUrl = ref(null);
const triggerImageInput = () => {
	if (itemsStore.itemEdition[itemId.value]?.loading) {
		return;
	}
	imageInputRef.value?.click();
};
const onImageFileChange = (event) => {
	const file = event.target.files?.[0];
	event.target.value = "";
	if (!file) {
		return;
	}
	if (localImagePreviewUrl.value) {
		URL.revokeObjectURL(localImagePreviewUrl.value);
	}
	localImagePreviewUrl.value = URL.createObjectURL(file);
	itemsStore.itemEdition[itemId.value].img_file = file;
	itemsStore.itemEdition[itemId.value].unset_img_item = false;
};
const removeImage = () => {
	if (localImagePreviewUrl.value) {
		URL.revokeObjectURL(localImagePreviewUrl.value);
		localImagePreviewUrl.value = null;
	}
	itemsStore.itemEdition[itemId.value].img_file = null;
	itemsStore.itemEdition[itemId.value].unset_img_item = true;
};
const resetImageSelection = () => {
	if (localImagePreviewUrl.value) {
		URL.revokeObjectURL(localImagePreviewUrl.value);
	}
	localImagePreviewUrl.value = null;
};

// tag
const filterTag = ref([
	{ key: "name_tag", value: "", type: "text", label: "", placeholder: t("item.TagFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);
function tagSave(id_tag) {
	try {
		// re-adding a tag that is only pending deletion locally just cancels that pending deletion
		if (itemsStore.itemTagReady[itemId.value]?.[id_tag]?.status === "deleted") {
			delete itemsStore.itemTagReady[itemId.value][id_tag];
			addNotification({ message: t("item.TagRestored"), type: "success" });
			return;
		}
		itemsStore.itemTagEdition[itemId.value][id_tag] = { id_tag };
		itemsStore.valideItemTagEditionById(itemId.value, id_tag, "created");
		delete itemsStore.itemTagEdition[itemId.value][id_tag];
		addNotification({ message: t("item.TagAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function tagDelete(id_tag) {
	try {
		// a tag that was only staged as a pending creation is simply dropped, nothing to push
		if (itemsStore.itemTagReady[itemId.value]?.[id_tag]?.status === "created") {
			delete itemsStore.itemTagReady[itemId.value][id_tag];
		} else {
			itemsStore.valideItemTagEditionById(itemId.value, id_tag, "deleted");
		}
		addNotification({ message: t("item.TagDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function tagRestore(id_tag) {
	try {
		delete itemsStore.itemTagReady[itemId.value][id_tag];
		addNotification({ message: t("item.TagRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

// item history enum
const projectTypeStatus = ref({ [ItemHistoryType.ItemCreated]: t("item.HistoryTypeItemCreated"), [ItemHistoryType.ItemUpdated]: t("item.HistoryTypeItemUpdated"),
	[ItemHistoryType.ItemDeleted]: t("item.HistoryTypeItemDeleted"), [ItemHistoryType.StockAdded]: t("item.HistoryTypeStockAdded"),
	[ItemHistoryType.StockRemoved]: t("item.HistoryTypeStockRemoved"), [ItemHistoryType.StockUpdated]: t("item.HistoryTypeStockUpdated") });

const schemaBox = Yup.object().shape({
	quantity_item_box: Yup.number()
		.required(t("item.BoxQuantityRequired"))
		.typeError(t("item.BoxQuantityNumber"))
		.min(0, t("item.BoxQuantityMin")),
	threshold_max_item_item_box: Yup.number()
		.required(t("item.BoxMaxThresholdRequired"))
		.typeError(t("item.BoxMaxThresholdNumber"))
		.min(1, t("item.BoxMaxThresholdMin")),
});

const createSchema = () => {
	const edition = itemsStore.itemEdition[itemId.value];
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
		.nullable()
		.optional()
		.max(configsStore.getConfigByKey("max_length_description"), t("item.DescriptionMaxLength", { count: configsStore.getConfigByKey("max_length_description") }));
	shape.threshold_min_item = Yup.number()
		.min(0, t("item.SeuilMinMin"))
		.typeError(t("item.SeuilMinType"))
		.required(t("item.SeuilMinRequired"));
	shape.img_file = Yup.mixed()
		.nullable()
		.test("fileSize", t("item.ImageSize") + " " + configsStore.getConfigByKey("max_size_image_in_mb") + "Mo", (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_image_in_mb"))) * 1024 * 1024);
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

const labelForm = [
	{ key: "reference_name_item", label: "item.Name", type: "text" },
	{ key: "friendly_name_item", label: "item.FriendlyName", type: "text" },
	{ key: "description_item", label: "item.Description", type: "textarea" },
	{ key: "threshold_min_item", label: "item.SeuilMin", type: "number" },
	{ key: "quantity", label: "item.TotalQuantity", type: "computed", value: getTotalQuantity },
	{ key: "img_file", label: "item.Image", type: "custom" },
];
const labelTableauModalTag = ref([
	{ label: "item.TagName", sortable: true, key: "name_tag", valueKey: "name_tag", type: "text" },
	{ label: "item.TagActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "!ready?.status && !store[1]?.[rowData.id_tag]",
			action: (row) => tagSave(row.id_tag),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-rotate-left",
			showCondition: "ready?.status === 'deleted'",
			action: (row) => tagRestore(row.id_tag),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "(ready?.status && ready?.status !== 'deleted') || (store[1]?.[rowData.id_tag] && !ready?.status)",
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
			showCondition: "!edition?.id_item_document && ready?.status !== 'deleted'",
			action: (row) => {
				itemsStore.documentEdition[itemId.value][row.id_item_document] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_item_document",
			action: (row) => {
				delete itemsStore.documentEdition[itemId.value][row.id_item_document];
			},
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_item_document",
			action: (row) => documentEdit(itemsStore.documentEdition[itemId.value][row.id_item_document]),
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
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row) => documentRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row) => documentDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauHistory = ref([
	{ label: "item.HistoryDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "item.HistoryType", sortable: true, key: "type_item_history", valueKey: "type_item_history", type: "enum", options: projectTypeStatus },
	{ label: "item.HistoryQuantityChange", sortable: true, key: "quantity_change_item_history", valueKey: "quantity_change_item_history", type: "number" },
	{ label: "item.HistoryOldQuantity", sortable: true, key: "old_quantity_item_history", valueKey: "old_quantity_item_history", type: "number" },
	{ label: "item.HistoryNewQuantity", sortable: true, key: "new_quantity_item_history", valueKey: "new_quantity_item_history", type: "number" },
	{ label: "item.HistoryBoxId", sortable: true, key: "id_box", valueKey: "id_box", type: "number" },
	{ label: "item.HistoryUser", sortable: true, key: "User.email_user", valueKey: "email_user", type: "text", storeRessourceId: 1, sourceKey: "id_user" },
	{ label: "item.HistoryNotes", sortable: false, key: "notes_item_history", valueKey: "notes_item_history", type: "text" },
]);
const labelTableauBox = ref([
	{ label: "item.BoxId", sortable: true, key: "id_box", valueKey: "id_box", type: "number" },
	{ label: "item.BoxQuantity", sortable: true, key: "quantity_item_box", valueKey: "quantity_item_box", type: "number", canEdit: true },
	{ label: "item.BoxMaxThreshold", sortable: true, key: "threshold_max_item_item_box", valueKey: "threshold_max_item_item_box", type: "number", canEdit: true },
	{ label: "item.BoxActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_box && ready?.status !== 'deleted'",
			action: (row) => {
				itemsStore.itemBoxEdition[itemId.value][row.id_box] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_box",
			action: (row) => {
				delete itemsStore.itemBoxEdition[itemId.value][row.id_box];
			},
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_box",
			action: (row) => boxEdit(itemsStore.itemBoxEdition[itemId.value][row.id_box]),
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
		},
	] },
]);
const labelTableauCommand = ref([
	{ label: "item.CommandDate", sortable: true, key: "Command.date_command", sourceKey: "id_command", type: "datetime", 
		storeRessourceId: 1, valueKey: "date_command" },
	{ label: "item.CommandStatus", sortable: true, key: "Command.status_command", sourceKey: "id_command", type: "text", 
		storeRessourceId: 1, valueKey: "status_command" },

	{ label: "item.CommandQte", sortable: true, key: "quantity_command_item", valueKey: "quantity_command_item", type: "number" },
	{ label: "item.CommandPrice", sortable: true, key: "price_command_item", valueKey: "price_command_item", type: "number" },
]);
const labelTableauProject = ref([
	{ label: "item.ProjectName", sortable: true, key: "Project.name_project", sourceKey: "id_project", type: "text", 
		storeRessourceId: 1, valueKey: "name_project" },
	{ label: "item.ProjectDate", sortable: true, key: "Project.date_start_project", sourceKey: "id_project", type: "datetime", 
		storeRessourceId: 1, valueKey: "date_start_project" },
	{ label: "item.ProjectDateEnd", sortable: true, key: "Project.date_end_project", sourceKey: "id_project", type: "datetime", 
		storeRessourceId: 1, valueKey: "date_end_project" },
	{ label: "item.ProjectStatus", sortable: true, key: "Project.status_project", sourceKey: "id_project", type: "text", 
		storeRessourceId: 1, valueKey: "status_project" },

	{ label: "item.ProjectQuantity", sortable: true, key: "quantity_project_item", valueKey: "quantity_project_item", type: "number" },
]);
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('item.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/inventory',
				create: { showCondition: itemId === 'new' && authStore.hasPermission([0, 1, 2]), loading: itemsStore.itemEdition[itemId]?.loading },
				update: { showCondition: itemId !== 'new' && authStore.hasPermission([0, 1, 2]), loading: itemsStore.itemEdition[itemId]?.loading },
				delete: { showCondition: itemId !== 'new' && authStore.hasPermission([0, 1, 2]) }
			}"
			@button-create="itemSave" @button-update="itemSave" @button-delete="itemDeleteModalShow = true"/>
	</div>
	<div v-if="itemsStore.items[itemId] || itemId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="itemsStore.itemEdition[itemId]">
				<template #img_file>
					<div class="flex flex-col items-center gap-2">
						<div class="flex justify-center items-center cursor-pointer"
							:class="{ 'opacity-50 cursor-not-allowed': itemsStore.itemEdition[itemId]?.loading }"
							@click="triggerImageInput">
							<img v-if="localImagePreviewUrl" :src="localImagePreviewUrl" alt="Preview"
								class="w-48 h-48 object-cover rounded" />
							<img v-else-if="!itemsStore.itemEdition[itemId]?.unset_img_item && itemsStore.itemEdition[itemId]?.url_thumbnail_item && itemsStore.thumbnailsURL[itemId]"
								:src="itemsStore.thumbnailsURL[itemId]" alt="Main"
								class="w-48 h-48 object-cover rounded" />
							<span v-else-if="!itemsStore.itemEdition[itemId]?.unset_img_item && itemsStore.itemEdition[itemId]?.url_thumbnail_item"
								class="w-48 h-48 object-cover rounded flex items-center justify-center">
								{{ $t('item.Loading') }}
							</span>
							<img v-else src="../assets/nopicture.webp" alt="Not Found"
								class="w-48 h-48 object-cover rounded" />
						</div>
						<button v-if="localImagePreviewUrl || (!itemsStore.itemEdition[itemId]?.unset_img_item && itemsStore.itemEdition[itemId]?.url_thumbnail_item)"
							type="button" @click="removeImage"
							class="text-sm text-red-500 hover:text-red-600">
							{{ $t('item.ImageRemove') }}
						</button>
						<input ref="imageInputRef" type="file" accept="image/*" class="hidden" @change="onImageFileChange" />
					</div>
				</template>
			</FormContainer>
			<Tags :current-tags="itemsStore.itemTags[itemId] || {}" :ready-store="itemsStore.itemTagReady[itemId] || {}" :tags-store="tagsStore.tags" :can-edit="authStore.hasPermission([1, 2])"
				:delete-function="(value) => tagDelete(value)"
				:restore-function="(value) => tagRestore(value)"
				:filter-modal="filterTag"
				:tableau-modal="{ 'label': labelTableauModalTag, 'meta': { key: 'id_tag', preventClear: true }, 'css': { component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }
								, 'loading': tagsStore.tagsLoading, 'fetchFunction': (limit, offset, expand, filter, sort, clear) => tagsStore.getTagByInterval(limit, offset, expand, filter, sort, clear)
								, 'totalCount': Number(tagsStore.tagsTotalCount || 0) }"
				:meta ="{ 'keyPoids': 'weight_tag', 'keyName': 'name_tag' }"
				/>
		</div>
		<CollapsibleSection title="item.Boxs"
			:total-count="Number(itemsStore.itemBoxsTotalCount[itemId] || 0)">
			<template #append-row>
				<Tableau :labels="labelTableauBox" :meta="{ key: 'id_box', expand: ['box'] }"
					:store-data="[itemsStore.itemBoxs[itemId]]"
					:store-edition="itemsStore.itemBoxEdition[itemId]"
					:store-ready="itemsStore.itemBoxReady[itemId]"
					:loading="itemsStore.itemBoxsLoading"
					:schema="schemaBox"
					:total-count="Number(itemsStore.itemBoxsTotalCount[itemId])"
					:fetch-function="itemId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemBoxByInterval(itemId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.Documents"
			:total-count="Number(itemsStore.documentsTotalCount[itemId] || 0)">
			<template #append-row>
				<button type="button" @click="documentAddModalShow = true"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('item.AddDocument') }}
				</button>
				<Tableau :labels="labelTableauDocument" :meta="{ key: 'id_item_document' }"
					:store-data="[itemsStore.documents[itemId]]"
					:store-edition="itemsStore.documentEdition[itemId]"
					:store-ready="itemsStore.documentReady[itemId]"
					:schema="schemaEditDocument"
					:loading="itemsStore.documentsLoading"
					:total-count="Number(itemsStore.documentsTotalCount[itemId])"
					:fetch-function="itemId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getDocumentByInterval(itemId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.Commands"
			:total-count="Number(itemsStore.itemCommandsTotalCount[itemId] || 0)">
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
		<CollapsibleSection title="item.Projects"
			:total-count="Number(itemsStore.itemProjectsTotalCount[itemId] || 0)">
			<template #append-row>
				<Tableau :labels="labelTableauProject" :meta="{ key: 'id_project', path: '/projects/', expand: ['project'] }"
					:store-data="[itemsStore.itemProjects[itemId],projectsStore.projects]"
					:loading="itemsStore.itemProjectsLoading"
					:total-count="Number(itemsStore.itemProjectsTotalCount[itemId])"
					:fetch-function="itemId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemProjectByInterval(itemId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.History"
			:total-count="Number(itemsStore.itemHistoryTotalCount[itemId] || 0)">
			<template #append-row>
				<Tableau :labels="labelTableauHistory" :meta="{ key: 'id_item_history', expand: ['user'] }"
					:store-data="[itemsStore.itemHistory[itemId], usersStore.users]"
					:loading="itemsStore.itemHistoryLoading"
					:total-count="Number(itemsStore.itemHistoryTotalCount[itemId])"
					:fetch-function="itemId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemHistoryByInterval(itemId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
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

	<ModalMultipleFiles
		:show-modal="documentAddModalShow"
		@close-modal="documentAddModalShow = false"
		@files-saved="documentAdd"
		file-type="document"
	/>
</template>

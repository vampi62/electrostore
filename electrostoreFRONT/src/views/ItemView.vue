<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref, computed, inject } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import * as Yup from "yup";

import { useViewScroll, useTabGuard } from "@/composables";
import type { useNotification } from "@/composables";
import type FormContainer from "@/components/FormContainer.vue";
import { downloadFile, viewFile, isNewId, loadLedPreferences, toShowQuery } from "@/utils";
import { ItemHistoryType } from "@/enums";
import { useConfigsStore, useItemsStore, useItemsDocumentsStore, useItemsBoxsStore, useItemsTagsStore, useItemsCommandsStore, useItemsProjectsStore, useItemsHistoryStore, useTagsStore, useStoresStore, useCommandsStore, useProjectsStore, useAuthStore, useUsersStore } from "@/stores";

import type { FilterLabel } from "@/types/filter";
import type { TableauLabel, TableauRowData } from "@/types/tableau";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";

const { addNotification } = inject("useNotification") as ReturnType<typeof useNotification>;
const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const configsStore = useConfigsStore();
const itemsStore = useItemsStore();
const itemDocumentsStore = useItemsDocumentsStore();
const itemBoxsStore = useItemsBoxsStore();
const itemTagsStore = useItemsTagsStore();
const itemCommandsStore = useItemsCommandsStore();
const itemProjectsStore = useItemsProjectsStore();
const itemHistoryStore = useItemsHistoryStore();
const tagsStore = useTagsStore();
const storesStore = useStoresStore();
const commandsStore = useCommandsStore();
const projectsStore = useProjectsStore();
const authStore = useAuthStore();
const usersStore = useUsersStore();

const itemId = ref(route.params.id as string);
const preset = ref(typeof route.query.preset === "string" ? route.query.preset : null);

// every new element has its own edition space in the store, so several tabs can create an element at the same time
if (isNewId(itemId.value)) {
	itemId.value = itemsStore.getAvailableNewItemId();
}

const formContainer = ref<InstanceType<typeof FormContainer> | null>(null);

// composite keys of the nested records of the item
const documentKey = (idDocument: string | number) => ({ id_item: itemId.value, id_item_document: String(idDocument) });
const boxKey = (idBox: string | number) => ({ id_item: itemId.value, id_box: String(idBox) });
const tagKey = (idTag: string | number) => ({ id_item: itemId.value, id_tag: String(idTag) });

async function fetchAllData() {
	if (isNewId(itemId.value)) {
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
		itemTagsStore.getItemTagByInterval({ id_item: itemId.value }, 100, 0, ["tag"]);
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

const toggleBoxLed = async(boxId: string) => {
	const itemBox: TableauRowData = itemBoxsStore.itemBoxsByItem(itemId.value)[boxId];
	const storeId = itemBox.box.id_store;
	try {
		await storesStore.showBoxById(storeId, boxId, toShowQuery(loadLedPreferences(authStore.user?.id_user).remove));
		addNotification({ message: t("item.BoxShowSuccess"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const stockOperationModalShow = ref(false);

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
		if (isNewId(itemId.value)) {
			addNotification({ message: t("item.Created"), type: "success" });
			const tempId = itemId.value;
			itemId.value = String(id);
			itemsStore.clearEdition(tempId);
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
	if (isNewId(itemId.value)) {
		return 0;
	}
	return Object.values(itemBoxsStore.itemBoxsByItem(itemId.value)).reduce((acc, box) => acc + (box.quantity_item_box ?? 0), 0);
});

// box
const boxEdit = (box: TableauRowData) => {
	try {
		schemaBox.validateSync(box, { abortEarly: false });
		itemBoxsStore.valideItemBoxEditionByKey(boxKey(box.id_box), "modified");
		itemBoxsStore.removeItemBoxEdition(boxKey(box.id_box));
		addNotification({ message: t("item.BoxUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const boxRestore = (row: TableauRowData) => {
	try {
		itemBoxsStore.removeItemBoxReady(boxKey(row.id_box));
		addNotification({ message: t("item.BoxRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const boxDelete = (row: TableauRowData) => {
	try {
		itemBoxsStore.valideItemBoxEditionByKey(boxKey(row.id_box), "deleted");
		itemBoxsStore.removeItemBoxEdition(boxKey(row.id_box));
		addNotification({ message: t("item.BoxDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

// document
const documentAddModalShow = ref(false);
const documentAdd = async(files: { name: string; document: File }[]) => {
	for (const file of files) {
		const documentModalData = { name_item_document: file.name, document: file.document, type_item_document: file.document.type, created_at: new Date() };
		const newId = itemDocumentsStore.getAvailableNewItemDocumentId({ id_item: itemId.value });
		itemDocumentsStore.setItemDocumentEdition(documentKey(newId), documentModalData);
		try {
			schemaAddDocument.validateSync(documentModalData, { abortEarly: false });
			itemDocumentsStore.valideItemDocumentEditionByKey(documentKey(newId), "created", true);
			itemDocumentsStore.removeItemDocumentEdition(documentKey(newId));
			addNotification({ message: t("item.DocumentAdded"), type: "success" });
		} catch (e) {
			addNotification({ message: e, type: "error" });
		}
	}
	documentAddModalShow.value = false;
};
const documentEdit = (row: TableauRowData) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		itemDocumentsStore.valideItemDocumentEditionByKey(documentKey(row.id_item_document),
			itemDocumentsStore.itemDocumentReadyByItem(itemId.value)[row.id_item_document]?.status === "created" ? "created" : "modified");
		itemDocumentsStore.removeItemDocumentEdition(documentKey(row.id_item_document));
		addNotification({ message: t("item.DocumentUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const documentRestore = (row: TableauRowData) => {
	try {
		itemDocumentsStore.removeItemDocumentReady(documentKey(row.id_item_document));
		addNotification({ message: t("item.DocumentRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const documentDelete = (row: TableauRowData) => {
	try {
		itemDocumentsStore.valideItemDocumentEditionByKey(documentKey(row.id_item_document), "deleted");
		itemDocumentsStore.removeItemDocumentEdition(documentKey(row.id_item_document));
		addNotification({ message: t("item.DocumentDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const documentDownload = async(fileContent: TableauRowData) => {
	const file = await itemDocumentsStore.downloadItemDocument(itemId.value, fileContent.id_item_document);
	downloadFile(file, { keyName: fileContent.name_item_document, keyType: fileContent.type_item_document });
};
const documentView = async(fileContent: TableauRowData) => {
	const file = await itemDocumentsStore.downloadItemDocument(itemId.value, fileContent.id_item_document);
	if (viewFile(file, { keyName: fileContent.name_item_document, keyType: fileContent.type_item_document })) {
		addNotification({ message: t("item.DocumentOpenInNewTab"), type: "success" });
	} else {
		addNotification({ message: t("item.DocumentNotSupported"), type: "error" });
	}
};

// image
const imageInputRef = ref<HTMLInputElement | null>(null);
const localImagePreviewUrl = ref<string | null>(null);
const triggerImageInput = () => {
	if (itemsStore.itemEdition[itemId.value]?.loading) {
		return;
	}
	imageInputRef.value?.click();
};
const onImageFileChange = (event: Event) => {
	const input = event.target as HTMLInputElement;
	const file = input.files?.[0];
	input.value = "";
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
	delete itemsStore.itemEdition[itemId.value].img_file;
	itemsStore.itemEdition[itemId.value].unset_img_item = true;
};
const resetImageSelection = () => {
	if (localImagePreviewUrl.value) {
		URL.revokeObjectURL(localImagePreviewUrl.value);
	}
	localImagePreviewUrl.value = null;
};

// tag
const filterTag = ref<FilterLabel[]>([
	{ key: "name_tag", value: "", type: "text", label: "", placeholder: t("item.TagFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);
function tagSave(id_tag: number) {
	try {
		// re-adding a tag that is only pending deletion locally just cancels that pending deletion
		if (itemTagsStore.itemTagReadyByItem(itemId.value)[id_tag]?.status === "deleted") {
			itemTagsStore.removeItemTagReady(tagKey(id_tag));
			addNotification({ message: t("item.TagRestored"), type: "success" });
			return;
		}
		itemTagsStore.setItemTagEdition(tagKey(id_tag), { id_tag });
		itemTagsStore.valideItemTagEditionByKey(tagKey(id_tag), "created");
		itemTagsStore.removeItemTagEdition(tagKey(id_tag));
		addNotification({ message: t("item.TagAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function tagDelete(id_tag: string) {
	try {
		// a tag that was only staged as a pending creation is simply dropped, nothing to push
		if (itemTagsStore.itemTagReadyByItem(itemId.value)[id_tag]?.status === "created") {
			itemTagsStore.removeItemTagReady(tagKey(id_tag));
		} else {
			itemTagsStore.valideItemTagEditionByKey(tagKey(id_tag), "deleted");
		}
		addNotification({ message: t("item.TagDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function tagRestore(id_tag: string) {
	try {
		itemTagsStore.removeItemTagReady(tagKey(id_tag));
		addNotification({ message: t("item.TagRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

// item history enum
const itemHistoryTypeOptions = { [ItemHistoryType.ItemCreated]: t("item.HistoryTypeItemCreated"), [ItemHistoryType.ItemUpdated]: t("item.HistoryTypeItemUpdated"),
	[ItemHistoryType.ItemDeleted]: t("item.HistoryTypeItemDeleted"), [ItemHistoryType.StockAdded]: t("item.HistoryTypeStockAdded"),
	[ItemHistoryType.StockRemoved]: t("item.HistoryTypeStockRemoved"), [ItemHistoryType.StockUpdated]: t("item.HistoryTypeStockUpdated") };

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
	const shape: Record<string, Yup.AnySchema> = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.reference_name_item = Yup.string()
		.max(configsStore.getConfigNumber("max_length_name"), t("item.NameMaxLength", { count: configsStore.getConfigNumber("max_length_name") }))
		.required(t("item.NameRequired"));
	shape.friendly_name_item = Yup.string()
		.max(configsStore.getConfigNumber("max_length_name"), t("item.FriendlyNameMaxLength", { count: configsStore.getConfigNumber("max_length_name") }))
		.required(t("item.FriendlyNameRequired"));
	shape.description_item = Yup.string()
		.nullable()
		.optional()
		.max(configsStore.getConfigNumber("max_length_description"), t("item.DescriptionMaxLength", { count: configsStore.getConfigNumber("max_length_description") }));
	shape.threshold_min_item = Yup.number()
		.min(0, t("item.SeuilMinMin"))
		.typeError(t("item.SeuilMinType"))
		.required(t("item.SeuilMinRequired"));
	shape.img_file = Yup.mixed()
		.nullable()
		.test("fileSize", t("item.ImageSize") + " " + configsStore.getConfigNumber("max_size_image_in_mb") + "Mo", (value) => !value || (value as File).size <= (Number(configsStore.getConfigNumber("max_size_image_in_mb"))) * 1024 * 1024);
	return Yup.object().shape(shape);
};

const schemaAddDocument = Yup.object().shape({
	name_item_document: Yup.string()
		.max(configsStore.getConfigNumber("max_length_name"), t("item.DocumentNameMaxLength", { count: configsStore.getConfigNumber("max_length_name") }))
		.required(t("item.DocumentNameRequired")),
	document: Yup.mixed()
		.required(t("item.DocumentRequired"))
		.test("fileSize", t("item.DocumentSize", { count: configsStore.getConfigNumber("max_size_document_in_mb") }), (value) => !value || (value as File).size <= (Number(configsStore.getConfigNumber("max_size_document_in_mb"))) * 1024 * 1024),
});
const schemaEditDocument = Yup.object().shape({
	name_item_document: Yup.string()
		.max(configsStore.getConfigNumber("max_length_name"), t("item.DocumentNameMaxLength", { count: configsStore.getConfigNumber("max_length_name") }))
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
const labelTableauModalTag = ref<TableauLabel[]>([
	{ label: "item.TagName", sortable: true, key: "name_tag", valueKey: "name_tag", type: "text" },
	{ label: "item.TagActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "!ready?.status && !store[1]?.[rowData.id_tag]",
			action: (row: TableauRowData) => tagSave(row.id_tag),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-rotate-left",
			showCondition: "ready?.status === 'deleted'",
			action: (row: TableauRowData) => tagRestore(row.id_tag),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "(ready?.status && ready?.status !== 'deleted') || (store[1]?.[rowData.id_tag] && !ready?.status)",
			action: (row: TableauRowData) => tagDelete(row.id_tag),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauDocument = ref<TableauLabel[]>([
	{ label: "item.DocumentName", sortable: true, key: "name_item_document", valueKey: "name_item_document", type: "text", canEdit: true },
	{ label: "item.DocumentType", sortable: true, key: "type_item_document", valueKey: "type_item_document", type: "text" },
	{ label: "item.DocumentDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "item.DocumentActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_item_document && ready?.status !== 'deleted'",
			action: (row: TableauRowData) => {
				itemDocumentsStore.setItemDocumentEdition(documentKey(row.id_item_document), { ...row });
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_item_document",
			action: (row: TableauRowData) => {
				itemDocumentsStore.removeItemDocumentEdition(documentKey(row.id_item_document));
			},
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_item_document",
			action: (row: TableauRowData) => documentEdit(itemDocumentsStore.itemDocumentEditionByItem(itemId.value)[row.id_item_document]),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-eye",
			action: (row: TableauRowData) => documentView(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-download",
			action: (row: TableauRowData) => documentDownload(row),
			class: "px-3 py-1 bg-yellow-500 text-white rounded-lg hover:bg-yellow-600",
			animation: true,
		},
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row: TableauRowData) => documentRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row: TableauRowData) => documentDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauHistory = ref<TableauLabel[]>([
	{ label: "item.HistoryDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "item.HistoryType", sortable: true, key: "type_item_history", valueKey: "type_item_history", type: "enum", options: itemHistoryTypeOptions },
	{ label: "item.HistoryQuantityChange", sortable: true, key: "quantity_change_item_history", valueKey: "quantity_change_item_history", type: "number" },
	{ label: "item.HistoryOldQuantity", sortable: true, key: "old_quantity_item_history", valueKey: "old_quantity_item_history", type: "number" },
	{ label: "item.HistoryNewQuantity", sortable: true, key: "new_quantity_item_history", valueKey: "new_quantity_item_history", type: "number" },
	{ label: "item.HistoryBoxId", sortable: true, key: "id_box", valueKey: "id_box", type: "number" },
	{ label: "item.HistoryUser", sortable: true, key: "User.email_user", valueKey: "email_user", type: "text", storeRessourceId: 1, sourceKey: "id_user" },
	{ label: "item.HistoryNotes", sortable: false, key: "notes_item_history", valueKey: "notes_item_history", type: "text" },
]);
const labelTableauBox = ref<TableauLabel[]>([
	{ label: "item.BoxId", sortable: true, key: "id_box", valueKey: "id_box", type: "number" },
	{ label: "item.BoxQuantity", sortable: true, key: "quantity_item_box", valueKey: "quantity_item_box", type: "number", canEdit: true },
	{ label: "item.BoxMaxThreshold", sortable: true, key: "threshold_max_item_item_box", valueKey: "threshold_max_item_item_box", type: "number", canEdit: true },
	{ label: "item.BoxActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_box && ready?.status !== 'deleted'",
			action: (row: TableauRowData) => {
				itemBoxsStore.setItemBoxEdition(boxKey(row.id_box), { ...row });
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_box",
			action: (row: TableauRowData) => {
				itemBoxsStore.removeItemBoxEdition(boxKey(row.id_box));
			},
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_box",
			action: (row: TableauRowData) => boxEdit(itemBoxsStore.itemBoxEditionByItem(itemId.value)[row.id_box]),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-eye",
			action: (row: TableauRowData) => toggleBoxLed(row.id_box),
			class: "px-3 py-1 bg-yellow-500 text-white rounded-lg hover:bg-yellow-600",
			animation: true,
		},
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
		},
	] },
]);
const labelTableauCommand = ref<TableauLabel[]>([
	{ label: "item.CommandDate", sortable: true, key: "Command.date_command", sourceKey: "id_command", type: "datetime", 
		storeRessourceId: 1, valueKey: "date_command" },
	{ label: "item.CommandStatus", sortable: true, key: "Command.status_command", sourceKey: "id_command", type: "text", 
		storeRessourceId: 1, valueKey: "status_command" },

	{ label: "item.CommandQte", sortable: true, key: "quantity_command_item", valueKey: "quantity_command_item", type: "number" },
	{ label: "item.CommandPrice", sortable: true, key: "price_command_item", valueKey: "price_command_item", type: "number" },
]);
const labelTableauProject = ref<TableauLabel[]>([
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
useViewScroll(true);
useTabGuard(itemsStore, () => itemId.value, "itemEdition", "items", [
	{ key: "documentReady", get: (id) => itemDocumentsStore.itemDocumentReadyByItem(id), set: (id, entries) => itemDocumentsStore.setItemDocumentStagedSlice("ready", { id_item: id }, entries) },
	{ key: "itemBoxReady", get: (id) => itemBoxsStore.itemBoxReadyByItem(id), set: (id, entries) => itemBoxsStore.setItemBoxStagedSlice("ready", { id_item: id }, entries) },
	{ key: "itemTagReady", get: (id) => itemTagsStore.itemTagReadyByItem(id), set: (id, entries) => itemTagsStore.setItemTagStagedSlice("ready", { id_item: id }, entries) },
	{ key: "itemCommandReady", get: (id) => itemCommandsStore.itemCommandReadyByItem(id), set: (id, entries) => itemCommandsStore.setItemCommandStagedSlice("ready", { id_item: id }, entries) },
	{ key: "itemProjectReady", get: (id) => itemProjectsStore.itemProjectReadyByItem(id), set: (id, entries) => itemProjectsStore.setItemProjectStagedSlice("ready", { id_item: id }, entries) },
]);
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('item.Title') }}</h2>
		<button v-if="!isNewId(itemId) && authStore.hasPermission([0, 1, 2])" type="button" @click="stockOperationModalShow = true"
			class="bg-blue-500 text-white px-4 py-2 rounded mb-4 mr-auto hover:bg-blue-600">
			<i class="fa-solid fa-boxes-stacked mr-1"></i>{{ $t('item.StockOperationButton') }}
		</button>
		<TopButtonEditElement
			:main-config="{ path: '/inventory',
				create: { showCondition: isNewId(itemId) && authStore.hasPermission([0, 1, 2]), loading: itemsStore.itemEdition[itemId]?.loading },
				update: { showCondition: !isNewId(itemId) && authStore.hasPermission([0, 1, 2]), loading: itemsStore.itemEdition[itemId]?.loading },
				delete: { showCondition: !isNewId(itemId) && authStore.hasPermission([0, 1, 2]) }
			}"
			@button-create="itemSave" @button-update="itemSave" @button-delete="itemDeleteModalShow = true"/>
	</div>
	<div v-if="itemsStore.items[itemId] || isNewId(itemId)" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-edition="itemsStore.itemEdition[itemId]" :store-data="itemsStore.items[itemId]">
				<template #img_file>
					<div class="flex flex-col items-center gap-2">
						<div class="flex justify-center items-center cursor-pointer"
							:class="{ 'opacity-50 cursor-not-allowed': itemsStore.itemEdition[itemId]?.loading }"
							@click="triggerImageInput">
							<img v-if="localImagePreviewUrl" :src="localImagePreviewUrl" alt="Preview"
								class="w-48 h-48 object-cover rounded" />
							<img v-else-if="!itemsStore.itemEdition[itemId]?.unset_img_item && itemsStore.items[itemId]?.url_thumbnail_item && itemsStore.thumbnailsURL[itemId]"
								:src="itemsStore.thumbnailsURL[itemId]" alt="Main"
								class="w-48 h-48 object-cover rounded" />
							<span v-else-if="!itemsStore.itemEdition[itemId]?.unset_img_item && itemsStore.items[itemId]?.url_thumbnail_item"
								class="w-48 h-48 object-cover rounded flex items-center justify-center">
								{{ $t('item.Loading') }}
							</span>
							<img v-else src="../assets/nopicture.webp" alt="Not Found"
								class="w-48 h-48 object-cover rounded" />
						</div>
						<button v-if="localImagePreviewUrl || (!itemsStore.itemEdition[itemId]?.unset_img_item && itemsStore.items[itemId]?.url_thumbnail_item)"
							type="button" @click="removeImage"
							class="text-sm text-red-500 hover:text-red-600">
							{{ $t('item.ImageRemove') }}
						</button>
						<input ref="imageInputRef" type="file" accept="image/*" class="hidden" @change="onImageFileChange" />
					</div>
				</template>
			</FormContainer>
			<Tags :current-tags="itemTagsStore.itemTagsByItem(itemId)" :ready-store="itemTagsStore.itemTagReadyByItem(itemId)" :tags-store="tagsStore.tags" :can-edit="authStore.hasPermission([1, 2])"
				:delete-function="(value: string) => tagDelete(value)"
				:restore-function="(value: string) => tagRestore(value)"
				:filter-modal="filterTag"
				:tableau-modal="{ 'label': labelTableauModalTag, 'meta': { key: 'id_tag', preventClear: true }, 'css': { component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }
								, 'loading': tagsStore.tagsLoading, 'fetchFunction': (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => tagsStore.getTagByInterval(limit, offset, expand, filter, sort, clear)
								, 'totalCount': Number(tagsStore.tagsTotalCount || 0) }"
				:meta ="{ 'keyPoids': 'weight_tag', 'keyName': 'name_tag' }"
				/>
		</div>
		<CollapsibleSection title="item.Boxs"
			:total-count="itemBoxsStore.itemBoxsCountByItem(itemId)">
			<template #append-row>
				<Tableau :labels="labelTableauBox" :meta="{ key: 'id_box', expand: ['box'] }"
					:store-data="[itemBoxsStore.itemBoxsByItem(itemId)]"
					:store-edition="itemBoxsStore.itemBoxEditionByItem(itemId)"
					:store-ready="itemBoxsStore.itemBoxReadyByItem(itemId)"
					:loading="itemBoxsStore.itemBoxsLoading"
					:schema="schemaBox"
					:total-count="itemBoxsStore.itemBoxsCountByItem(itemId)"
					:fetch-function="!isNewId(itemId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => itemBoxsStore.getItemBoxByInterval({ id_item: itemId }, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.Documents"
			:total-count="itemDocumentsStore.itemDocumentsCountByItem(itemId)">
			<template #append-row>
				<button type="button" @click="documentAddModalShow = true"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('item.AddDocument') }}
				</button>
				<Tableau :labels="labelTableauDocument" :meta="{ key: 'id_item_document' }"
					:store-data="[itemDocumentsStore.itemDocumentsByItem(itemId)]"
					:store-edition="itemDocumentsStore.itemDocumentEditionByItem(itemId)"
					:store-ready="itemDocumentsStore.itemDocumentReadyByItem(itemId)"
					:schema="schemaEditDocument"
					:loading="itemDocumentsStore.itemDocumentsLoading"
					:total-count="itemDocumentsStore.itemDocumentsCountByItem(itemId)"
					:fetch-function="!isNewId(itemId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => itemDocumentsStore.getItemDocumentByInterval({ id_item: itemId }, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.Commands"
			:total-count="itemCommandsStore.itemCommandsCountByItem(itemId)">
			<template #append-row>
				<Tableau :labels="labelTableauCommand" :meta="{ key: 'id_item', path: '/commands/', expand: ['command'] }"
					:store-data="[itemCommandsStore.itemCommandsByItem(itemId),commandsStore.commands]"
					:loading="itemCommandsStore.itemCommandsLoading"
					:total-count="itemCommandsStore.itemCommandsCountByItem(itemId)"
					:fetch-function="!isNewId(itemId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => itemCommandsStore.getItemCommandByInterval({ id_item: itemId }, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.Projects"
			:total-count="itemProjectsStore.itemProjectsCountByItem(itemId)">
			<template #append-row>
				<Tableau :labels="labelTableauProject" :meta="{ key: 'id_project', path: '/projects/', expand: ['project'] }"
					:store-data="[itemProjectsStore.itemProjectsByItem(itemId),projectsStore.projects]"
					:loading="itemProjectsStore.itemProjectsLoading"
					:total-count="itemProjectsStore.itemProjectsCountByItem(itemId)"
					:fetch-function="!isNewId(itemId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => itemProjectsStore.getItemProjectByInterval({ id_item: itemId }, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="item.History"
			:total-count="itemHistoryStore.itemHistoryCountByItem(itemId)">
			<template #append-row>
				<Tableau :labels="labelTableauHistory" :meta="{ key: 'id_item_history', expand: ['user'], sort: 'created_at', sortOrder: 'desc' }"
					:store-data="[itemHistoryStore.itemHistoryByItem(itemId), usersStore.users]"
					:loading="itemHistoryStore.itemHistoryLoading"
					:total-count="itemHistoryStore.itemHistoryCountByItem(itemId)"
					:fetch-function="!isNewId(itemId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => itemHistoryStore.getItemHistoryByInterval({ id_item: itemId }, limit, offset, expand, filter, sort, clear) : undefined"
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

	<ModalStockOperation :show-modal="stockOperationModalShow" :item-id="itemId" @close-modal="stockOperationModalShow = false" />

	<ModalMultipleFiles
		:show-modal="documentAddModalShow"
		@close-modal="documentAddModalShow = false"
		@files-saved="documentAdd"
		file-type="document"
	/>
</template>

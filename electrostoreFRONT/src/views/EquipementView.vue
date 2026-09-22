<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const equipementId = ref(route.params.id);
const preset = ref(route.query.preset || null);

import { downloadFile, viewFile } from "@/utils";

import { EquipementStatus, EquipementMaintenanceType } from "@/enums";

import { useConfigsStore, useEquipementsStore, useTagsStore, useStoresStore, useUsersStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const equipementsStore = useEquipementsStore();
const tagsStore = useTagsStore();
const storesStore = useStoresStore();
const usersStore = useUsersStore();
const authStore = useAuthStore();

const formContainer = ref(null);

async function fetchAllData() {
	if (equipementId.value === "new") {
		equipementsStore.loadToEdition(equipementId.value, preset.value);
	} else {
		equipementsStore.setLoadingEdition(equipementId.value, true);
		try {
			await equipementsStore.getEquipementById(equipementId.value, ["equipement_tags", "equipement_boxs"]);
		} catch {
			delete equipementsStore.equipements[equipementId.value];
			addNotification({ message: t("equipement.NotFound"), type: "error" });
			router.push("/equipements");
			return;
		}
		equipementsStore.loadToEdition(equipementId.value);
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	equipementsStore.clearEdition(equipementId.value);
	resetImageSelection();
});

// image
const imageInputRef = ref(null);
const localImagePreviewUrl = ref(null);
const triggerImageInput = () => {
	if (equipementsStore.equipementEdition[equipementId.value]?.loading) {
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
	equipementsStore.equipementEdition[equipementId.value].img_file = file;
	equipementsStore.equipementEdition[equipementId.value].unset_img_equipement = false;
};
const removeImage = () => {
	if (localImagePreviewUrl.value) {
		URL.revokeObjectURL(localImagePreviewUrl.value);
		localImagePreviewUrl.value = null;
	}
	equipementsStore.equipementEdition[equipementId.value].img_file = null;
	equipementsStore.equipementEdition[equipementId.value].unset_img_equipement = true;
};
const resetImageSelection = () => {
	if (localImagePreviewUrl.value) {
		URL.revokeObjectURL(localImagePreviewUrl.value);
	}
	localImagePreviewUrl.value = null;
};

// equipement
const equipementDeleteModalShow = ref(false);
const equipementSave = async() => {
	try {
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("equipement.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			equipementsStore.setLoadingEdition(equipementId.value, false);
			return;
		}
		equipementsStore.equipementEdition[equipementId.value].isFormData = true;
		const id = await equipementsStore.saveAllChanges(equipementId.value);
		equipementsStore.loadToEdition(id);
		if (equipementId.value === "new") {
			addNotification({ message: t("equipement.Created"), type: "success" });
			equipementId.value = String(id);
			router.push("/equipements/" + equipementId.value);
		} else {
			addNotification({ message: t("equipement.Updated"), type: "success" });
		}
		resetImageSelection();
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		equipementsStore.setLoadingEdition(equipementId.value, false);
	}
};
const equipementDelete = async() => {
	try {
		await equipementsStore.deleteEquipement(equipementId.value);
		addNotification({ message: t("equipement.Deleted"), type: "success" });
		router.push("/equipements");
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	equipementDeleteModalShow.value = false;
};

const createSchema = () => {
	const edition = equipementsStore.equipementEdition[equipementId.value];
	const shape = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.reference_name_equipement = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("equipement.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("equipement.NameRequired"));
	shape.friendly_name_equipement = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("equipement.FriendlyNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("equipement.FriendlyNameRequired"));
	shape.description_equipement = Yup.string()
		.nullable()
		.optional()
		.max(configsStore.getConfigByKey("max_length_description"), t("equipement.DescriptionMaxLength", { count: configsStore.getConfigByKey("max_length_description") }));
	shape.status_equipement = Yup.string()
		.required(t("equipement.StatusRequired"));
	shape.img_file = Yup.mixed()
		.nullable()
		.test("fileSize", t("equipement.ImageSize") + " " + configsStore.getConfigByKey("max_size_image_in_mb") + "Mo", (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_image_in_mb"))) * 1024 * 1024);
	return Yup.object().shape(shape);
};

// tag
const filterTag = ref([
	{ key: "name_tag", value: "", type: "text", label: "", placeholder: t("equipement.TagFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);
function tagSave(id_tag) {
	try {
		// re-adding a tag that is only pending deletion locally just cancels that pending deletion
		if (equipementsStore.equipementTagReady[equipementId.value]?.[id_tag]?.status === "deleted") {
			delete equipementsStore.equipementTagReady[equipementId.value][id_tag];
			addNotification({ message: t("equipement.TagRestored"), type: "success" });
			return;
		}
		equipementsStore.equipementTagEdition[equipementId.value][id_tag] = { id_tag };
		equipementsStore.valideEquipementTagEditionById(equipementId.value, id_tag, "created");
		delete equipementsStore.equipementTagEdition[equipementId.value][id_tag];
		addNotification({ message: t("equipement.TagAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function tagDelete(id_tag) {
	try {
		// a tag that was only staged as a pending creation is simply dropped, nothing to push
		if (equipementsStore.equipementTagReady[equipementId.value]?.[id_tag]?.status === "created") {
			delete equipementsStore.equipementTagReady[equipementId.value][id_tag];
		} else {
			equipementsStore.valideEquipementTagEditionById(equipementId.value, id_tag, "deleted");
		}
		addNotification({ message: t("equipement.TagDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function tagRestore(id_tag) {
	try {
		delete equipementsStore.equipementTagReady[equipementId.value][id_tag];
		addNotification({ message: t("equipement.TagRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

// box
const boxDelete = (row) => {
	try {
		equipementsStore.valideEquipementBoxEditionById(equipementId.value, row.id_box, "deleted");
		addNotification({ message: t("equipement.BoxUnlinked"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const boxRestore = (row) => {
	try {
		delete equipementsStore.equipementBoxReady[equipementId.value][row.id_box];
		addNotification({ message: t("equipement.BoxRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

// document
const documentAddModalShow = ref(false);
const documentAdd = async(files) => {
	for (const file of files) {
		const documentModalData = { name_equipement_document: file.name, document: file.document, type_equipement_document: file.document.type, created_at: new Date() };
		const newId = equipementsStore.getAvailableNewEquipementDocumentId(equipementId.value);
		equipementsStore.equipementDocumentEdition[equipementId.value][newId] = documentModalData;
		try {
			schemaAddDocument.validateSync(documentModalData, { abortEarly: false });
			equipementsStore.valideEquipementDocumentEditionById(equipementId.value, newId, "created", true);
			delete equipementsStore.equipementDocumentEdition[equipementId.value][newId];
			addNotification({ message: t("equipement.DocumentAdded"), type: "success" });
		} catch (e) {
			addNotification({ message: e, type: "error" });
		}
	}
	documentAddModalShow.value = false;
};
const documentEdit = (row) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		equipementsStore.valideEquipementDocumentEditionById(equipementId.value, row.id_equipement_document,
			equipementsStore.equipementDocumentEdition[equipementId.value][row.id_equipement_document]?.status === "created" ? "created" : "modified");
		delete equipementsStore.equipementDocumentEdition[equipementId.value][row.id_equipement_document];
		addNotification({ message: t("equipement.DocumentUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const documentRestore = (row) => {
	try {
		delete equipementsStore.equipementDocumentReady[equipementId.value][row.id_equipement_document];
		addNotification({ message: t("equipement.DocumentRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const documentDelete = (row) => {
	try {
		equipementsStore.valideEquipementDocumentEditionById(equipementId.value, row.id_equipement_document, "deleted");
		addNotification({ message: t("equipement.DocumentDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const documentDownload = async(fileContent) => {
	const file = await equipementsStore.downloadEquipementDocument(equipementId.value, fileContent.id_equipement_document);
	downloadFile(file, { keyName: fileContent.name_equipement_document, keyType: fileContent.type_equipement_document });
};
const documentView = async(fileContent) => {
	const file = await equipementsStore.downloadEquipementDocument(equipementId.value, fileContent.id_equipement_document);
	if (viewFile(file, { keyName: fileContent.name_equipement_document, keyType: fileContent.type_equipement_document })) {
		addNotification({ message: t("equipement.DocumentOpenInNewTab"), type: "success" });
	} else {
		addNotification({ message: t("equipement.DocumentNotSupported"), type: "error" });
	}
};

// maintenance
const maintenanceTypeOptions = {
	[EquipementMaintenanceType.Preventive]: t("equipement.MaintenanceTypePreventive"),
	[EquipementMaintenanceType.Corrective]: t("equipement.MaintenanceTypeCorrective"),
	[EquipementMaintenanceType.Inspection]: t("equipement.MaintenanceTypeInspection"),
};
const maintenanceForm = ref({ type_equipement_maintenance: EquipementMaintenanceType.Preventive, date_planned_equipement_maintenance: "", description_equipement_maintenance: "" });
const schemaMaintenance = Yup.object().shape({
	type_equipement_maintenance: Yup.number().required(t("equipement.MaintenanceTypeRequired")),
	date_planned_equipement_maintenance: Yup.string().required(t("equipement.MaintenanceDateRequired")),
});
const maintenanceAdd = () => {
	try {
		schemaMaintenance.validateSync(maintenanceForm.value, { abortEarly: false });
		const newId = equipementsStore.getAvailableNewEquipementMaintenanceId(equipementId.value);
		equipementsStore.equipementMaintenanceEdition[equipementId.value][newId] = {
			type_equipement_maintenance: Number(maintenanceForm.value.type_equipement_maintenance),
			date_planned_equipement_maintenance: new Date(maintenanceForm.value.date_planned_equipement_maintenance).toISOString(),
			description_equipement_maintenance: maintenanceForm.value.description_equipement_maintenance || null,
		};
		equipementsStore.valideEquipementMaintenanceEditionById(equipementId.value, newId, "created");
		delete equipementsStore.equipementMaintenanceEdition[equipementId.value][newId];
		addNotification({ message: t("equipement.MaintenanceAdded"), type: "success" });
		maintenanceForm.value = { type_equipement_maintenance: EquipementMaintenanceType.Preventive, date_planned_equipement_maintenance: "", description_equipement_maintenance: "" };
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const maintenanceMarkDone = (row) => {
	try {
		equipementsStore.equipementMaintenanceEdition[equipementId.value][row.id_equipement_maintenance] = { ...row, date_done_equipement_maintenance: new Date().toISOString() };
		equipementsStore.valideEquipementMaintenanceEditionById(equipementId.value, row.id_equipement_maintenance, "modified");
		delete equipementsStore.equipementMaintenanceEdition[equipementId.value][row.id_equipement_maintenance];
		addNotification({ message: t("equipement.MaintenanceDone"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const maintenanceRestore = (row) => {
	try {
		delete equipementsStore.equipementMaintenanceReady[equipementId.value][row.id_equipement_maintenance];
		addNotification({ message: t("equipement.MaintenanceRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const maintenanceDelete = (row) => {
	try {
		equipementsStore.valideEquipementMaintenanceEditionById(equipementId.value, row.id_equipement_maintenance, "deleted");
		addNotification({ message: t("equipement.MaintenanceDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const schemaAddDocument = Yup.object().shape({
	name_equipement_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("equipement.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("equipement.DocumentNameRequired")),
	document: Yup.mixed()
		.required(t("equipement.DocumentRequired"))
		.test("fileSize", t("equipement.DocumentSize", { count: configsStore.getConfigByKey("max_size_document_in_mb") }), (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});
const schemaEditDocument = Yup.object().shape({
	name_equipement_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("equipement.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("equipement.DocumentNameRequired")),
});

const equipementStatusOptions = {
	[EquipementStatus.Operational]: t("equipement.StatusOperational"),
	[EquipementStatus.InMaintenance]: t("equipement.StatusInMaintenance"),
	[EquipementStatus.OutOfService]: t("equipement.StatusOutOfService"),
	[EquipementStatus.Retired]: t("equipement.StatusRetired"),
};

const labelForm = [
	{ key: "reference_name_equipement", label: "equipement.Name", type: "text" },
	{ key: "friendly_name_equipement", label: "equipement.FriendlyName", type: "text" },
	{ key: "description_equipement", label: "equipement.Description", type: "textarea" },
	{ key: "status_equipement", label: "equipement.Status", type: "select", options: equipementStatusOptions, typeData: "number" },
	{ key: "img_file", label: "equipement.Image", type: "custom" },
];
const labelTableauModalTag = ref([
	{ label: "equipement.TagName", sortable: true, key: "name_tag", valueKey: "name_tag", type: "text" },
	{ label: "equipement.TagActions", sortable: false, key: "", type: "buttons", buttons: [
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
const labelTableauBox = ref([
	{ label: "equipement.BoxId", sortable: false, key: "id_box", valueKey: "id_box", type: "number" },
	{ label: "equipement.BoxStoreId", sortable: false, key: "box.id_store", sourceKey: "id_box", type: "number",
		storeRessourceId: 1, valueKey: "id_store" },
	{ label: "equipement.BoxActions", sortable: false, key: "", type: "buttons", buttons: [
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
const labelTableauDocument = ref([
	{ label: "equipement.DocumentName", sortable: true, key: "name_equipement_document", valueKey: "name_equipement_document", type: "text", canEdit: true },
	{ label: "equipement.DocumentType", sortable: true, key: "type_equipement_document", valueKey: "type_equipement_document", type: "text" },
	{ label: "equipement.DocumentDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "equipement.DocumentActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_equipement_document && ready?.status !== 'deleted'",
			action: (row) => {
				equipementsStore.equipementDocumentEdition[equipementId.value][row.id_equipement_document] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_equipement_document",
			action: (row) => {
				delete equipementsStore.equipementDocumentEdition[equipementId.value][row.id_equipement_document];
			},
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_equipement_document",
			action: (row) => documentEdit(equipementsStore.equipementDocumentEdition[equipementId.value][row.id_equipement_document]),
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
const labelTableauMaintenance = ref([
	{ label: "equipement.MaintenanceType", sortable: true, key: "type_equipement_maintenance", valueKey: "type_equipement_maintenance", type: "enum", options: maintenanceTypeOptions },
	{ label: "equipement.MaintenanceDatePlanned", sortable: true, key: "date_planned_equipement_maintenance", valueKey: "date_planned_equipement_maintenance", type: "datetime" },
	{ label: "equipement.MaintenanceDateDone", sortable: true, key: "date_done_equipement_maintenance", valueKey: "date_done_equipement_maintenance", type: "datetime" },
	{ label: "equipement.MaintenanceDescription", sortable: false, key: "description_equipement_maintenance", valueKey: "description_equipement_maintenance", type: "text" },
	{ label: "equipement.MaintenanceActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-check",
			showCondition: "!rowData.date_done_equipement_maintenance && ready?.status !== 'deleted'",
			action: (row) => maintenanceMarkDone(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row) => maintenanceRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row) => maintenanceDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauStatusHistory = ref([
	{ label: "equipement.HistoryDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "equipement.HistoryStatus", sortable: true, key: "status_equipement", valueKey: "status_equipement", type: "enum", options: equipementStatusOptions },
]);
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('equipement.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/equipements',
				create: { showCondition: equipementId === 'new' && authStore.hasPermission([1, 2]), loading: equipementsStore.equipementEdition[equipementId]?.loading },
				update: { showCondition: equipementId !== 'new' && authStore.hasPermission([1, 2]), loading: equipementsStore.equipementEdition[equipementId]?.loading },
				delete: { showCondition: equipementId !== 'new' && authStore.hasPermission([1, 2]) }
			}"
			@button-create="equipementSave" @button-update="equipementSave" @button-delete="equipementDeleteModalShow = true"/>
	</div>
	<div v-if="equipementsStore.equipements[equipementId] || equipementId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="equipementsStore.equipementEdition[equipementId]">
				<template #img_file>
					<div class="flex flex-col items-center gap-2">
						<div class="flex justify-center items-center cursor-pointer"
							:class="{ 'opacity-50 cursor-not-allowed': equipementsStore.equipementEdition[equipementId]?.loading }"
							@click="triggerImageInput">
							<img v-if="localImagePreviewUrl" :src="localImagePreviewUrl" alt="Preview"
								class="w-48 h-48 object-cover rounded" />
							<img v-else-if="!equipementsStore.equipementEdition[equipementId]?.unset_img_equipement && equipementsStore.equipementEdition[equipementId]?.url_thumbnail_equipement && equipementsStore.thumbnailsURL[equipementId]"
								:src="equipementsStore.thumbnailsURL[equipementId]" alt="Main"
								class="w-48 h-48 object-cover rounded" />
							<span v-else-if="!equipementsStore.equipementEdition[equipementId]?.unset_img_equipement && equipementsStore.equipementEdition[equipementId]?.url_thumbnail_equipement"
								class="w-48 h-48 object-cover rounded flex items-center justify-center">
								{{ $t('equipement.Loading') }}
							</span>
							<img v-else src="../assets/nopicture.webp" alt="Not Found"
								class="w-48 h-48 object-cover rounded" />
						</div>
						<button v-if="localImagePreviewUrl || (!equipementsStore.equipementEdition[equipementId]?.unset_img_equipement && equipementsStore.equipementEdition[equipementId]?.url_thumbnail_equipement)"
							type="button" @click="removeImage"
							class="text-sm text-red-500 hover:text-red-600">
							{{ $t('equipement.ImageRemove') }}
						</button>
						<input ref="imageInputRef" type="file" accept="image/*" class="hidden" @change="onImageFileChange" />
					</div>
				</template>
			</FormContainer>
			<Tags :current-tags="equipementsStore.equipementTags[equipementId] || {}" :ready-store="equipementsStore.equipementTagReady[equipementId] || {}" :tags-store="tagsStore.tags" :can-edit="authStore.hasPermission([1, 2])"
				:delete-function="(value) => tagDelete(value)"
				:restore-function="(value) => tagRestore(value)"
				:filter-modal="filterTag"
				:tableau-modal="{ 'label': labelTableauModalTag, 'meta': { key: 'id_tag', preventClear: true }, 'css': { component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }
								, 'loading': tagsStore.tagsLoading, 'fetchFunction': (limit, offset, expand, filter, sort, clear) => tagsStore.getTagByInterval(limit, offset, expand, filter, sort, clear)
								, 'totalCount': Number(tagsStore.tagsTotalCount || 0) }"
				:meta ="{ 'keyPoids': 'weight_tag', 'keyName': 'name_tag' }"
				/>
		</div>
		<CollapsibleSection title="equipement.Boxs"
			:total-count="Number(equipementsStore.equipementBoxsTotalCount[equipementId] || 0)">
			<template #append-row>
				<p class="text-sm text-gray-600 mb-2">{{ $t('equipement.BoxHint') }}</p>
				<Tableau :labels="labelTableauBox" :meta="{ key: 'id_box' }"
					:store-data="[equipementsStore.equipementBoxs[equipementId], storesStore.stores]"
					:store-ready="equipementsStore.equipementBoxReady[equipementId]"
					:loading="equipementsStore.equipementBoxsLoading"
					:total-count="Number(equipementsStore.equipementBoxsTotalCount[equipementId])"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="equipement.Documents"
			:total-count="Number(equipementsStore.equipementDocumentsTotalCount[equipementId] || 0)">
			<template #append-row>
				<button type="button" @click="documentAddModalShow = true"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('equipement.AddDocument') }}
				</button>
				<Tableau :labels="labelTableauDocument" :meta="{ key: 'id_equipement_document' }"
					:store-data="[equipementsStore.equipementDocuments[equipementId]]"
					:store-edition="equipementsStore.equipementDocumentEdition[equipementId]"
					:store-ready="equipementsStore.equipementDocumentReady[equipementId]"
					:schema="schemaEditDocument"
					:loading="equipementsStore.equipementDocumentsLoading"
					:total-count="Number(equipementsStore.equipementDocumentsTotalCount[equipementId])"
					:fetch-function="equipementId !== 'new' ? (limit, offset, expand, filter, sort, clear) => equipementsStore.getEquipementDocumentByInterval(equipementId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="equipement.Maintenances"
			:total-count="Number(equipementsStore.equipementMaintenancesTotalCount[equipementId] || 0)">
			<template #append-row>
				<div class="flex flex-wrap items-end gap-2 mb-4">
					<div class="flex flex-col">
						<label class="text-sm font-semibold">{{ $t('equipement.MaintenanceType') }}</label>
						<select v-model="maintenanceForm.type_equipement_maintenance" class="border border-gray-300 rounded px-2 py-1">
							<option v-for="(label, value) in maintenanceTypeOptions" :key="value" :value="value">{{ label }}</option>
						</select>
					</div>
					<div class="flex flex-col">
						<label class="text-sm font-semibold">{{ $t('equipement.MaintenanceDatePlanned') }}</label>
						<input type="datetime-local" v-model="maintenanceForm.date_planned_equipement_maintenance" class="border border-gray-300 rounded px-2 py-1" />
					</div>
					<div class="flex flex-col flex-1 min-w-[150px]">
						<label class="text-sm font-semibold">{{ $t('equipement.MaintenanceDescription') }}</label>
						<input type="text" v-model="maintenanceForm.description_equipement_maintenance" class="border border-gray-300 rounded px-2 py-1" />
					</div>
					<button type="button" @click="maintenanceAdd" class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600">
						{{ $t('equipement.MaintenanceAdd') }}
					</button>
				</div>
				<Tableau :labels="labelTableauMaintenance" :meta="{ key: 'id_equipement_maintenance' }"
					:store-data="[equipementsStore.equipementMaintenances[equipementId]]"
					:store-ready="equipementsStore.equipementMaintenanceReady[equipementId]"
					:loading="equipementsStore.equipementMaintenancesLoading"
					:total-count="Number(equipementsStore.equipementMaintenancesTotalCount[equipementId])"
					:fetch-function="equipementId !== 'new' ? (limit, offset, expand, filter, sort, clear) => equipementsStore.getEquipementMaintenanceByInterval(equipementId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="equipement.Comments"
			:total-count="Number(equipementsStore.equipementCommentsTotalCount[equipementId] || 0)" :permission="equipementId !== 'new'">
			<template #append-row>
				<Comment :meta="{ key: 'id_equipement_comment', contenu: 'content_equipement_comment', canEdit: true, roleRequired: authStore.hasPermission([2]), expand: ['user'] }"
					:store-data="[equipementsStore.equipementComments[equipementId], usersStore.users]"
					:store-user="authStore.user" :store-config="configsStore"
					:store-function="{
						create: (data) => equipementsStore.createEquipementComment(equipementId, data),
						update: (id, data) => equipementsStore.updateEquipementComment(equipementId, id, data),
						delete: (id) => equipementsStore.deleteEquipementComment(equipementId, id),
					}"
					:loading="equipementsStore.equipementCommentsLoading"
					:total-count="Number(equipementsStore.equipementCommentsTotalCount[equipementId]) || 0"
					:fetch-function="equipementId !== 'new' ? (limit, offset, expand, filter, sort, clear) => equipementsStore.getEquipementCommentByInterval(equipementId, limit, offset, expand, filter, sort, clear) : undefined"
					:texte-modal-delete="{ textTitle: 'equipement.CommentDeleteTitle', textP: 'equipement.CommentDeleteText' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="equipement.StatusHistory"
			:total-count="Number(equipementsStore.equipementStatusHistoryTotalCount[equipementId] || 0)">
			<template #append-row>
				<Tableau :labels="labelTableauStatusHistory" :meta="{ key: 'id_equipement_status' }"
					:store-data="[equipementsStore.equipementStatusHistory[equipementId]]"
					:loading="equipementsStore.equipementStatusHistoryLoading"
					:total-count="Number(equipementsStore.equipementStatusHistoryTotalCount[equipementId])"
					:fetch-function="equipementId !== 'new' ? (limit, offset, expand, filter, sort, clear) => equipementsStore.getEquipementStatusHistoryByInterval(equipementId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('equipement.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="equipementDeleteModalShow" @close-modal="equipementDeleteModalShow = false"
		:delete-action="equipementDelete" :text-title="'equipement.DeleteTitle'"
		:text-p="'equipement.DeleteText'"/>

	<ModalMultipleFiles
		:show-modal="documentAddModalShow"
		@close-modal="documentAddModalShow = false"
		@files-saved="documentAdd"
		file-type="document"
	/>
</template>

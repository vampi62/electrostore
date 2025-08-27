<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
let projetId = route.params.id;

import { useConfigsStore, useProjetsStore, useUsersStore, useItemsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const projetsStore = useProjetsStore();
const usersStore = useUsersStore();
const itemsStore = useItemsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (projetId !== "new") {
		projetsStore.projetEdition = {
			loading: true,
		};
		try {
			await projetsStore.getProjetById(projetId);
		} catch {
			delete projetsStore.projets[projetId];
			addNotification({ message: "projet.VProjetNotFound", type: "error", i18n: true });
			router.push("/projets");
			return;
		}
		projetsStore.getCommentaireByInterval(projetId, 100, 0, ["user"]);
		projetsStore.getDocumentByInterval(projetId, 100, 0);
		projetsStore.getItemByInterval(projetId, 100, 0, ["item"]);
		projetsStore.projetEdition = {
			loading: false,
			nom_projet: projetsStore.projets[projetId].nom_projet,
			description_projet: projetsStore.projets[projetId].description_projet,
			url_projet: projetsStore.projets[projetId].url_projet,
			status_projet: projetsStore.projets[projetId].status_projet,
			date_debut_projet: projetsStore.projets[projetId].date_debut_projet,
			date_fin_projet: projetsStore.projets[projetId].date_fin_projet,
		};
		usersStore.users[authStore.user.id_user] = authStore.user; // avoids undefined user when the current user posts first comment
	} else {
		projetsStore.projetEdition = {
			loading: false,
		};
		showDocuments.value = false;
		showItems.value = false;
		showCommentaires.value = false;
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	projetsStore.projetEdition = {
		loading: false,
	};
});

const showDocuments = ref(true);
const showItems = ref(true);
const showCommentaires = ref(true);

const toggleDocuments = () => {
	if (projetId === "new") {
		return;
	}
	showDocuments.value = !showDocuments.value;
};
const toggleItems = () => {
	if (projetId === "new") {
		return;
	}
	showItems.value = !showItems.value;
};
const toggleCommentaires = () => {
	if (projetId === "new") {
		return;
	}
	showCommentaires.value = !showCommentaires.value;
};

// projet
const projetDeleteModalShow = ref(false);
const projetTypeStatus = ref([["En attente", t("projet.VProjetStatus1")], ["En cours", t("projet.VProjetStatus2")], ["Terminée", t("projet.VProjetStatus3")], ["Annulée", t("projet.VProjetStatus4")]]);
const projetSave = async() => {
	try {
		await schemaProjet.validate(projetsStore.projetEdition, { abortEarly: false });
		if (projetId !== "new") {
			await projetsStore.updateProjet(projetId, { ...projetsStore.projetEdition });
			addNotification({ message: "projet.VProjetUpdated", type: "success", i18n: true });
		} else {
			await projetsStore.createProjet({ ...projetsStore.projetEdition });
			addNotification({ message: "projet.VProjetCreated", type: "success", i18n: true });
		}
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
	if (projetId === "new") {
		projetId = String(projetsStore.projetEdition.id_projet);
		router.push("/projets/" + projetId);
	}
};
const projetDelete = async() => {
	try {
		await projetsStore.deleteProjet(projetId);
		addNotification({ message: "projet.VProjetDeleted", type: "success", i18n: true });
		router.push("/projets");
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	projetDeleteModalShow.value = false;
};

// document
const documentAddModalShow = ref(false);
const documentDeleteModalShow = ref(false);
const documentModalData = ref({ id_projet_document: null, name_projet_document: "", document: null, isEdit: false });
const documentAddOpenModal = () => {
	documentModalData.value = { name_projet_document: "", document: null, isEdit: false };
	documentAddModalShow.value = true;
};
const documentDeleteOpenModal = (doc) => {
	documentModalData.value = doc;
	documentDeleteModalShow.value = true;
};
const documentAdd = async() => {
	try {
		schemaAddDocument.validateSync(documentModalData.value, { abortEarly: false });
		await projetsStore.createDocument(projetId, documentModalData.value);
		addNotification({ message: "projet.VProjetDocumentAdded", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
	documentAddModalShow.value = false;
};
const documentEdit = async(row) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		await projetsStore.updateDocument(projetId, row.id_projet_document, row);
		addNotification({ message: "projet.VProjetDocumentUpdated", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const documentDelete = async() => {
	try {
		await projetsStore.deleteDocument(projetId, documentModalData.value.id_projet_document);
		addNotification({ message: "projet.VProjetDocumentDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	documentDeleteModalShow.value = false;
};
const handleFileUpload = (e) => {
	documentModalData.value.document = e.target.files[0];
};
const documentDownload = async(fileContent) => {
	const file = await projetsStore.downloadDocument(projetId, fileContent.id_projet_document);
	const url = window.URL.createObjectURL(new Blob([file]));
	const link = document.createElement("a");
	link.href = url;
	link.setAttribute("download", fileContent.name_projet_document + "." + fileContent.type_projet_document);
	document.body.appendChild(link);
	link.click();
	document.body.removeChild(link);
};
const documentView = async(fileContent) => {
	const file = await projetsStore.downloadDocument(projetId, fileContent.id_projet_document);
	const blob = new Blob([file], { type: getMimeType(fileContent.type_projet_document) });
	const url = window.URL.createObjectURL(blob);

	if (["pdf", "png", "jpg", "jpeg", "gif", "bmp"].includes(fileContent.type_projet_document)) {
		// Ouvrir directement dans une nouvelle fenêtre
		window.open(url, "_blank");
	} else if (["doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt"].includes(fileContent.type_projet_document)) {
		// Télécharger automatiquement pour les formats éditables
		const a = document.createElement("a");
		a.href = url;
		a.download = fileContent.name || `document.${fileContent.type_projet_document}`;
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);
	} else {
		addNotification({ message: "projet.VProjetDocumentNotSupported", type: "error", i18n: true });
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

// item
const itemModalShow = ref(false);
const itemLoaded = ref(false);
const itemOpenAddModal = () => {
	itemModalShow.value = true;
	if (!itemLoaded.value) {
		fetchAllItems();
	}
};
async function fetchAllItems() {
	let offset = 0;
	const limit = 100;
	do {
		await itemsStore.getItemByInterval(limit, offset);
		offset += limit;
	} while (offset < itemsStore.itemsTotalCount);
	itemLoaded.value = true;
}
const itemSave = async(item) => {
	if (projetsStore.items[projetId][item.id_item]) {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await projetsStore.updateItem(projetId, item.tmp.id_item, item.tmp);
			addNotification({ message: "projet.VProjetItemUpdated", type: "success", i18n: true });
			item.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error", i18n: false });
			return;
		}
	} else {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await projetsStore.createItem(projetId, item.tmp);
			addNotification({ message: "projet.VProjetItemAdded", type: "success", i18n: true });
			item.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error", i18n: false });
			return;
		}
	}
};
const itemDelete = async(item) => {
	try {
		await projetsStore.deleteItem(projetId, item.id_item);
		addNotification({ message: "projet.VProjetItemDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};

const filteredItems = ref([]);
const updateFilteredItems = (newValue) => {
	filteredItems.value = newValue;
};
const filterItem = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "", placeholder: t("command.VCommandItemFilterPlaceholder"), compareMethod: "contain", class: "w-full" },
]);

const schemaProjet = Yup.object().shape({
	nom_projet: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("projet.VProjetNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("projet.VProjetNameRequired")),
	description_projet: Yup.string()
		.max(configsStore.getConfigByKey("max_length_description"), t("projet.VProjetDescriptionMaxLength") + " " + configsStore.getConfigByKey("max_length_description") + t("common.VAllCaracters"))
		.required(t("projet.VProjetDescriptionRequired")),
	url_projet: Yup.string()
		.max(configsStore.getConfigByKey("max_length_url"), t("projet.VProjetUrlMaxLength") + " " + configsStore.getConfigByKey("max_length_url") + t("common.VAllCaracters"))
		.url(t("projet.VProjetUrlInvalid"))
		.required(t("projet.VProjetUrlRequired")),
	status_projet: Yup.string()
		.max(configsStore.getConfigByKey("max_length_status"), t("projet.VProjetStatusMaxLength") + " " + configsStore.getConfigByKey("max_length_status") + t("common.VAllCaracters"))
		.required(t("projet.VProjetStatusRequired")),
	date_debut_projet: Yup.date()
		.required(t("projet.VProjetStartDateRequired")),
	date_fin_projet: Yup.date()
		.required(t("projet.VProjetEndDateRequired"))
		.nullable()
		.optional(),
});

const schemaAddDocument = Yup.object().shape({
	name_projet_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("projet.VProjetDocumentNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("projet.VProjetDocumentNameRequired")),
	document: Yup.mixed()
		.required(t("projet.VProjetDocumentRequired"))
		.test("fileSize", t("projet.VProjetDocumentSize") + " " + configsStore.getConfigByKey("max_size_document_in_mb") + "Mo", (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});
const schemaEditDocument = Yup.object().shape({
	name_projet_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("projet.VProjetDocumentNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("projet.VProjetDocumentNameRequired")),
});

const schemaItem = Yup.object().shape({
	qte_projet_item: Yup.number()
		.min(1, t("projet.VProjetItemQuantityMin"))
		.typeError(t("projet.VProjetItemQuantityType"))
		.required(t("projet.VProjetItemQuantityRequired")),
});

const labelTableauDocument = ref([
	{ label: "projet.VProjetDocumentName", sortable: true, key: "name_projet_document", type: "text", canEdit: true },
	{ label: "projet.VProjetDocumentType", sortable: true, key: "type_projet_document", type: "text" },
	{ label: "projet.VProjetDocumentDate", sortable: true, key: "date_projet_document", type: "datetime" },
	{ label: "projet.VProjetDocumentActions", sortable: false, key: "", type: "buttons", buttons: [
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
		},
		{
			label: "",
			icon: "fa-solid fa-eye",
			action: (row) => documentView(row),
			class: "text-green-500 cursor-pointer hover:text-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-download",
			action: (row) => documentDownload(row),
			class: "text-yellow-500 cursor-pointer hover:text-yellow-600",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => documentDeleteOpenModal(row),
			class: "text-red-500 cursor-pointer hover:text-red-600",
		},
	] },
]);
const labelTableauItem = ref([
	{ label: "projet.VProjetItemName", sortable: true, key: "reference_name_item", keyStore: "id_item", store: "1", type: "text" },
	{ label: "projet.VProjetItemQuantity", sortable: true, key: "qte_projet_item", type: "number", canEdit: true },
	{ label: "projet.VProjetItemActions", sortable: false, key: "", type: "buttons", buttons: [
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
			action: (row) => itemSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			condition: "rowData.tmp",
			action: (row) => {
				row.tmp = null;
			},
			class: "px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauModalItem = ref([
	{ label: "projet.VProjetItemName", sortable: true, key: "reference_name_item", type: "text" },
	{ label: "projet.VProjetItemQuantity", sortable: true, key: "qte_projet_item", keyStore: "id_item", store: "1", type: "number", canEdit: true },
	{ label: "projet.VProjetItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			condition: "store[1]?.[rowData.id_item] === undefined",
			action: (row) => {
				row.tmp = { qte_projet_item: 1, id_item: row.id_item };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-edit",
			condition: "store[1]?.[rowData.id_item] && !rowData.tmp",
			action: (row) => {
				row.tmp = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			condition: "rowData.tmp",
			action: (row) => itemSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			condition: "rowData.tmp",
			action: (row) => {
				row.tmp = null;
			},
			class: "px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			condition: "store[1]?.[rowData.id_item]",
			action: (row) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('projet.VProjetTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/projets', save: { roleRequired: 0, loading: projetsStore.projetEdition.loading }, delete: { roleRequired: 0 } }"
			:id="projetId" :store-user="authStore.user" @button-save="projetSave" @button-delete="projetDeleteModalShow = true"/>
	</div>
	<div :class="projetsStore.projets[projetId] || projetId == 'new' ? 'block' : 'hidden'">
		<div class="mb-6 flex justify-between">
			<Form :validation-schema="schemaProjet" v-slot="{ errors }" @submit.prevent="">
				<div class="flex flex-col text-gray-700 space-y-2">
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="nom_projet">{{ $t('projet.VProjetName') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="nom_projet" type="text"
								v-model="projetsStore.projetEdition.nom_projet"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.nom_projet }" />
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.nom_projet || ' ' }}</span>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="description_projet">{{ $t('projet.VProjetDescription') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="description_projet" v-slot="{ field }">
								<textarea v-bind="field" v-model="projetsStore.projetEdition.description_projet"
									:value="projetsStore.projetEdition.description_projet"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300 resize-y"
									:class="{ 'border-red-500': errors.description_projet }" rows="4"></textarea>
							</Field>
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.description_projet || ' ' }}</span>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="url_projet">{{ $t('projet.VProjetUrl') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="url_projet" type="text" v-model="projetsStore.projetEdition.url_projet"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.url_projet }" />
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.url_projet || ' ' }}</span>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="status_projet">{{ $t('projet.VProjetStatus') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="status_projet" as="select"
								v-model="projetsStore.projetEdition.status_projet"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.status_projet }">
								<option value="" disabled> -- {{ $t('projet.VProjetStatusSelect') }} -- </option>
								<option v-for="status in projetTypeStatus" :key="status" :value="status[0]">
									{{ status[1] }}
								</option>
							</Field>
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.status_projet || ' ' }}</span>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="date_debut_projet">{{ $t('projet.VProjetStartDate') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="date_debut_projet" type="datetime-local"
								v-model="projetsStore.projetEdition.date_debut_projet"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.date_debut_projet }" />
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.date_debut_projet || ' ' }}</span>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="date_fin_projet">{{ $t('projet.VProjetEndDate') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="date_fin_projet" type="datetime-local"
								v-model="projetsStore.projetEdition.date_fin_projet"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.date_fin_projet }" />
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.date_fin_projet || ' ' }}</span>
						</div>
					</div>
				</div>
			</Form>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleDocuments" class="text-xl font-semibold  bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': projetId != 'new', 'cursor-not-allowed': projetId == 'new' }">
				{{ $t('projet.VProjetDocuments') }} ({{ projetsStore.documentsTotalCount[projetId] || 0 }})
			</h3>
			<div :class="showDocuments ? 'block' : 'hidden'" class="p-2">
				<button type="button" @click="documentAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projet.VProjetAddDocument') }}
				</button>
				<Tableau :labels="labelTableauDocument" :meta="{ key: 'id_projet_document' }"
					:store-data="[projetsStore.documents[projetId]]"
					:loading="projetsStore.documentsLoading"
					:total-count="Number(projetsStore.documentsTotalCount[projetId])"
					:loaded-count="Object.keys(projetsStore.documents[projetId] || {}).length"
					:fetch-function="(offset, limit) => projetsStore.getDocumentByInterval(projetId, limit, offset)"
					:tableau-css="{ component: 'max-h-64' }"
				/>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleItems" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': projetId != 'new', 'cursor-not-allowed': projetId == 'new' }">
				{{ $t('projet.VProjetItems') }} ({{ projetsStore.itemsTotalCount[projetId] || 0 }})
			</h3>
			<div :class="showItems ? 'block' : 'hidden'" class="p-2">
				<button type="button" @click="itemOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projet.VProjetAddItem') }}
				</button>
				<Tableau :labels="labelTableauItem" :meta="{ key: 'id_item' }"
					:store-data="[projetsStore.items[projetId],itemsStore.items]"
					:loading="projetsStore.itemsLoading" :schema="schemaItem"
					:total-count="Number(projetsStore.itemsTotalCount[projetId])"
					:loaded-count="Object.keys(projetsStore.items[projetId] || {}).length"
					:fetch-function="(offset, limit) => projetsStore.getItemByInterval(projetId, limit, offset, ['item'])"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleCommentaires" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': projetId != 'new', 'cursor-not-allowed': projetId == 'new' }">
				{{ $t('projet.VProjetCommentaires') }} ({{ projetsStore.commentairesTotalCount[projetId] || 0 }})
			</h3>
			<div :class="showCommentaires ? 'block' : 'hidden'" class="p-2">
				<Commentaire :meta="{ contenu: 'contenu_projet_commentaire', key: 'id_projet_commentaire', CanEdit: true }"
					:store-data="[projetsStore.commentaires[projetId],usersStore.users,authStore.user,configsStore]"
					:store-function="{ create: (data) => projetsStore.createCommentaire(projetId, data), update: (id, data) => projetsStore.updateCommentaire(projetId, id, data), delete: (id) => projetsStore.deleteCommentaire(projetId, id) }"
					:loading="projetsStore.commentairesLoading" :texte-modal-delete="{ textTitle: 'projet.VProjetCommentDeleteTitle', textP: 'projet.VProjetCommentDeleteText' }"
					:total-count="Number(projetsStore.commentairesTotalCount[projetId])"
					:loaded-count="Object.keys(projetsStore.commentaires[projetId] || {}).length"
					:fetch-function="(offset, limit) => projetsStore.getCommentaireByInterval(projetId, limit, offset)"
				/>
			</div>
		</div>
	</div>
	<div :class="!projetsStore.projets[projetId] && projetId != 'new' ? 'block' : 'hidden'"
		class="text-center">
		<div>{{ $t('projet.VProjetLoading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="projetDeleteModalShow" @close-modal="projetDeleteModalShow = false"
		@delete-confirmed="projetDelete" :text-title="'projet.VProjetDeleteTitle'"
		:text-p="'projet.VProjetDeleteText'"/>

	<div v-if="documentAddModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="documentAddModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<Form :validation-schema="schemaAddDocument" v-slot="{ errors }">
				<h2 class="text-xl mb-4">{{ $t('projet.VProjetDocumentAddTitle') }}</h2>
				<div class="flex flex-col">
					<div class="flex flex-col">
						<Field name="name_projet_document" type="text"
							v-model="documentModalData.name_projet_document"
							:placeholder="$t('projet.VProjetDocumentNamePlaceholder')"
							class="w-full p-2 border rounded"
							:class="{ 'border-red-500': errors.name_projet_document }" />
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.name_projet_document || ' ' }}</span>
					</div>
					<div class="flex flex-col">
						<Field name="document" type="file" @change="handleFileUpload" class="w-full p-2"
							:class="{ 'border-red-500': errors.document }" />
						<span class="h-5 w-80 text-sm">{{ $t('projet.VProjetDocumentSize') }} ({{ configsStore.getConfigByKey("max_size_document_in_mb") }}Mo)</span>
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.document || ' ' }}</span>
					</div>
				</div>
				<div class="flex justify-end space-x-2">
					<button type="button" @click="documentAddModalShow = false" class="px-4 py-2 bg-gray-300 rounded">
						{{ $t('projet.VProjetDocumentCancel') }}
					</button>
					<button type="button" @click="documentAdd" class="px-4 py-2 bg-blue-500 text-white rounded">
						{{ $t('projet.VProjetDocumentAdd') }}
					</button>
				</div>
			</Form>
		</div>
	</div>
	<ModalDeleteConfirm :show-modal="documentDeleteModalShow" @close-modal="documentDeleteModalShow = false"
		@delete-confirmed="documentDelete" :text-title="'projet.VProjetDocumentDeleteTitle'"
		:text-p="'projet.VProjetDocumentDeleteText'"/>

	<div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemModalShow = false">
		<div class="bg-white rounded-lg shadow-lg w-3/4 p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('projet.VProjetItemTitle') }}</h2>
				<button type="button" @click="itemModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterItem" :store-data="itemsStore.items" @output-filter="updateFilteredItems" />

			<!-- Tableau Items -->
			<Tableau :labels="labelTableauModalItem" :meta="{ key: 'id_item' }"
				:store-data="[filteredItems,projetsStore.items[projetId]]"
				:loading="projetsStore.itemsLoading" :schema="schemaItem"
				:tableau-css="{ component: 'min-h-96 max-h-96', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

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
const commandId = route.params.id;

import { useConfigsStore, useCommandsStore, useUsersStore, useItemsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const commandsStore = useCommandsStore();
const usersStore = useUsersStore();
const itemsStore = useItemsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (commandId !== "new") {
		commandsStore.commandEdition = {
			loading: true,
		};
		try {
			await commandsStore.getCommandById(commandId);
		} catch {
			delete commandsStore.commands[commandId];
			addNotification({ message: "command.VCommandNotFound", type: "error", i18n: true });
			router.push("/commands");
			return;
		}
		commandsStore.getCommentaireByInterval(commandId, 100, 0, ["user"]);
		commandsStore.getDocumentByInterval(commandId, 100, 0);
		commandsStore.getItemByInterval(commandId, 100, 0, ["item"]);
		commandsStore.commandEdition = {
			prix_command: commandsStore.commands[commandId].prix_command,
			url_command: commandsStore.commands[commandId].url_command,
			status_command: commandsStore.commands[commandId].status_command,
			date_command: commandsStore.commands[commandId].date_command,
			date_livraison_command: commandsStore.commands[commandId].date_livraison_command,
			loading: false,
		};
		usersStore.users[authStore.user.id_user] = authStore.user; // avoids undefined user when the current user posts first comment
	} else {
		commandsStore.commandEdition = {
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
	commandsStore.commandEdition = {
		loading: false,
	};
});

const showDocuments = ref(true);
const showItems = ref(true);
const showCommentaires = ref(true);

const toggleDocuments = () => {
	if (commandId === "new") {
		return;
	}
	showDocuments.value = !showDocuments.value;
};
const toggleItems = () => {
	if (commandId === "new") {
		return;
	}
	showItems.value = !showItems.value;
};
const toggleCommentaires = () => {
	if (commandId === "new") {
		return;
	}
	showCommentaires.value = !showCommentaires.value;
};

// commande
const commandDeleteModalShow = ref(false);
const commandTypeStatus = ref([["En attente", t("command.VCommandStatus1")], ["En cours", t("command.VCommandStatus2")], ["Terminée", t("command.VCommandStatus3")], ["Annulée", t("command.VCommandStatus4")]]);
const commandSave = async() => {
	try {
		await schemaCommand.validate(commandsStore.commandEdition, { abortEarly: false });
		if (commandId !== "new") {
			await commandsStore.updateCommand(commandId, { ...commandsStore.commandEdition });
			addNotification({ message: "command.VCommandUpdated", type: "success", i18n: true });
		} else {
			await commandsStore.createCommand({ ...commandsStore.commandEdition });
			addNotification({ message: "command.VCommandCreated", type: "success", i18n: true });
		}
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	if (commandId === "new") {
		router.push("/commands/" + commandsStore.commandEdition.id_command);
	}
};
const commandDelete = async() => {
	try {
		await commandsStore.deleteCommand(commandId);
		addNotification({ message: "command.VCommandDeleted", type: "success", i18n: true });
		router.push("/commands");
	} catch (e) {
		addNotification({ message: "command.VCommandDeleteError", type: "error", i18n: true });
	}
	commandDeleteModalShow.value = false;
};
const formatDateForDatetimeLocal = (date) => {
	if (typeof date === "string") {
		date = new Date(date);
	}
	const pad = (num) => String(num).padStart(2, "0");
	return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};

// document
const documentAddModalShow = ref(false);
const documentEditModalShow = ref(false);
const documentDeleteModalShow = ref(false);
const documentModalData = ref({ id_command_document: null, name_command_document: "", document: null, isEdit: false });
const documentAddOpenModal = () => {
	documentModalData.value = { name_command_document: "", document: null, isEdit: false };
	documentAddModalShow.value = true;
};
const documentEditOpenModal = (doc) => {
	documentModalData.value = { id_command_document: doc.id_command_document, name_command_document: doc.name_command_document, document: null, isEdit: true };
	documentEditModalShow.value = true;
};
const documentDeleteOpenModal = (doc) => {
	documentModalData.value = doc;
	documentDeleteModalShow.value = true;
};
const documentAdd = async() => {
	try {
		schemaAddDocument.validateSync(documentModalData.value, { abortEarly: false });
		await commandsStore.createDocument(commandId, documentModalData.value);
		addNotification({ message: "command.VCommandDocumentAdded", type: "success", i18n: true });
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
		await commandsStore.updateDocument(commandId, documentModalData.value.id_command_document, documentModalData.value);
		addNotification({ message: "command.VCommandDocumentUpdated", type: "success", i18n: true });
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
		await commandsStore.deleteDocument(commandId, documentModalData.value.id_command_document);
		addNotification({ message: "command.VCommandDocumentDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "command.VCommandDocumentDeleteError", type: "error", i18n: true });
	}
	documentDeleteModalShow.value = false;
};
const handleFileUpload = (e) => {
	documentModalData.value.document = e.target.files[0];
};
const documentDownload = async(fileContent) => {
	const file = await commandsStore.downloadDocument(commandId, fileContent.id_command_document);
	const url = window.URL.createObjectURL(new Blob([file]));
	const link = document.createElement("a");
	link.href = url;
	link.setAttribute("download", fileContent.name_command_document + "." + fileContent.type_command_document);
	document.body.appendChild(link);
	link.click();
	document.body.removeChild(link);
};
const documentView = async(fileContent) => {
	const file = await commandsStore.downloadDocument(commandId, fileContent.id_command_document);
	const blob = new Blob([file], { type: getMimeType(fileContent.type_command_document) });
	const url = window.URL.createObjectURL(blob);

	if (["pdf", "png", "jpg", "jpeg", "gif", "bmp"].includes(fileContent.type_command_document)) {
		// Ouvrir directement dans une nouvelle fenêtre
		window.open(url, "_blank");
	} else if (["doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt"].includes(fileContent.type_command_document)) {
		// Télécharger automatiquement pour les formats éditables
		const a = document.createElement("a");
		a.href = url;
		a.download = fileContent.name || `document.${fileContent.type_command_document}`;
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);
	} else {
		addNotification({ message: "command.VCommandDocumentNotSupported", type: "error", i18n: true });
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
const itemOpenAddModal = () => {
	itemModalShow.value = true;
	itemsStore.getItemByInterval();
};
const itemSave = async(item) => {
	if (commandsStore.items[commandId][item.id_item]) {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await commandsStore.updateItem(commandId, item.tmp.id_item, item.tmp);
			addNotification({ message: "command.VCommandItemUpdated", type: "success", i18n: true });
			item.tmp = null;
		} catch (e) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
	} else {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await commandsStore.createItem(commandId, item.tmp);
			addNotification({ message: "command.VCommandItemAdded", type: "success", i18n: true });
			item.tmp = null;
		} catch (e) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
	}
};
const itemDelete = async(item) => {
	try {
		await commandsStore.deleteItem(commandId, item.id_item);
		addNotification({ message: "command.VCommandItemDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "command.VCommandItemDeleteError", type: "error", i18n: true });
	}
};

const filteredItems = ref([]);
const updateFilteredItems = (newValue) => {
	filteredItems.value = newValue;
};
const filterItem = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "", placeholder: t("command.VCommandItemFilterPlaceholder"), compareMethod: "contain", class: "w-full" },
]);

const schemaCommand = Yup.object().shape({
	prix_command: Yup.number()
		.min(0, t("command.VCommandPriceMin"))
		.typeError(t("command.VCommandPriceNumber"))
		.required(t("command.VCommandPriceRequired")),
	url_command: Yup.string()
		.max(configsStore.getConfigByKey("max_length_url"), t("command.VCommandUrlMaxLength") + " " + configsStore.getConfigByKey("max_length_url") + t("common.VAllCaracters"))
		.url(t("command.VCommandUrlInvalid"))
		.required(t("command.VCommandUrlRequired")),
	date_command: Yup.date()
		.typeError(t("command.VCommandDateInvalid"))
		.required(t("command.VCommandDateRequired")),
	status_command: Yup.string()
		.max(configsStore.getConfigByKey("max_length_status"), t("command.VCommandStatusMaxLength") + " " + configsStore.getConfigByKey("max_length_status") + t("common.VAllCaracters"))
		.required(t("command.VCommandStatusRequired")),
	date_livraison_command: Yup.date()
		.nullable()
		.optional(),
});

const schemaAddDocument = Yup.object().shape({
	name_command_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("command.VCommandDocumentNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("command.VCommandDocumentNameRequired")),
	document: Yup.mixed()
		.required(t("command.VCommandDocumentRequired"))
		.test("fileSize", t("command.VCommandDocumentSize") + " " + configsStore.getConfigByKey("max_size_document_in_mb") + "Mo", (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});
const schemaEditDocument = Yup.object().shape({
	name_command_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("command.VCommandDocumentNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("command.VCommandDocumentNameRequired")),
});

const schemaItem = Yup.object().shape({
	qte_command_item: Yup.number()
		.required(t("command.VCommandItemQuantityRequired"))
		.typeError(t("command.VCommandItemQuantityNumber"))
		.min(1, t("command.VCommandItemQuantityMin")),
	prix_command_item: Yup.number()
		.required(t("command.VCommandItemPriceRequired"))
		.typeError(t("command.VCommandItemPriceNumber"))
		.min(1, t("command.VCommandItemPriceMin")),
});

const labelTableauDocument = ref([
	{ label: "command.VCommandDocumentName", sortable: true, key: "name_command_document", type: "text" },
	{ label: "command.VCommandDocumentType", sortable: true, key: "type_command_document", type: "text" },
	{ label: "command.VCommandDocumentDate", sortable: true, key: "date_command_document", type: "datetime" },
	{ label: "command.VCommandDocumentActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			action: (row) => documentEditOpenModal(row),
			class: "text-blue-500 cursor-pointer hover:text-blue-600",
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
	{ label: "command.VCommandItemName", sortable: true, key: "reference_name_item", keyStore: "id_item", store: "1", type: "text" },
	{ label: "command.VCommandItemQuantity", sortable: true, key: "qte_command_item", type: "number", canEdit: true },
	{ label: "command.VCommandItemPrice", sortable: true, key: "prix_command_item", type: "number", canEdit: true },
	{ label: "command.VCommandItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			condition: "!rowData.tmp",
			action: (row) => {
				row.tmp = { ...row };
			},
			type: "button",
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			condition: "rowData.tmp",
			action: (row) => itemSave(row),
			type: "button",
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			condition: "rowData.tmp",
			action: (row) => {
				row.tmp = null;
			},
			type: "button",
			class: "px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => itemDelete(row),
			type: "button",
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauModalItem = ref([
	{ label: "command.VCommandItemName", sortable: true, key: "reference_name_item", type: "text" },
	{ label: "command.VCommandItemQuantity", sortable: true, key: "qte_command_item", keyStore: "id_item", store: "1", type: "number", canEdit: true },
	{ label: "command.VCommandItemPrice", sortable: true, key: "prix_command_item", keyStore: "id_item", store: "1", type: "number", canEdit: true },
	{ label: "command.VCommandItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			condition: "store[1]?.[rowData.id_item] === undefined",
			action: (row) => {
				row.tmp = { prix_command_item: 1, qte_command_item: 1, id_item: row.id_item };
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
		<h2 class="text-2xl font-bold mb-4">{{ $t('command.VCommandTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/commands', save: { roleRequired: 0, loading: commandsStore.commandEdition.loading }, delete: { roleRequired: 0 } }"
			:id="commandId" :store-user="authStore.user" @button-save="commandSave" @button-delete="commandDeleteModalShow = true"/>
	</div>
	<div :class="commandsStore.commands[commandId] || commandId == 'new' ? 'block' : 'hidden'">
		<div class="mb-6 flex justify-between">
			<Form :validation-schema="schemaCommand" v-slot="{ errors }" @submit.prevent="">
				<table class="table-auto text-gray-700">
					<tbody>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('command.VCommandPrice') }}:</td>
							<td class="flex flex-col">
								<Field name="prix_command" type="text"
									v-model="commandsStore.commandEdition.prix_command"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.prix_command }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.prix_command || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('command.VCommandUrl') }}:</td>
							<td class="flex flex-col">
								<Field name="url_command" type="text" v-model="commandsStore.commandEdition.url_command"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.url_command }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.url_command || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('command.VCommandDate') }}:</td>
							<td class="flex flex-col">
								<!-- format date permit is only YYYY-MM-DDTHH-mm-->
								<Field name="date_command" type="datetime-local"
									v-model="commandsStore.commandEdition.date_command"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.date_command }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.date_command || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('command.VCommandStatus') }}:</td>
							<td class="flex flex-col">
								<Field name="status_command" as="select"
									v-model="commandsStore.commandEdition.status_command"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.status_command }">
									<option value="" disabled> -- {{ $t('command.VCommandStatusSelect') }} -- </option>
									<option v-for="status in commandTypeStatus" :key="status" :value="status[0]">
										{{ status[1] }}
									</option>
								</Field>
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.status_command || ' ' }}</span>
							</td>
						</tr>
						<tr class="pb-4">
							<td class="font-semibold pr-4 align-text-top">{{ $t('command.VCommandDeliveryDate') }}:</td>
							<td class="flex flex-col">
								<Field name="date_livraison_command" type="datetime-local"
									v-model="commandsStore.commandEdition.date_livraison_command"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.date_livraison_command }" disabled />
								<span class="text-red-500 h-5 w-80 text-sm">
									{{ errors.date_livraison_command || ' ' }}
								</span>
							</td>
						</tr>
					</tbody>
				</table>
			</Form>
			<div>
				<!-- TODO suivie commande -->
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleDocuments" class="text-xl font-semibold  bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': commandId != 'new', 'cursor-not-allowed': commandId == 'new' }">
				{{ $t('command.VCommandDocuments') }} ({{ commandsStore.documentsTotalCount[commandId] || 0 }})
			</h3>
			<div :class="showDocuments ? 'block' : 'hidden'" class="p-2">
				<button type="button" @click="documentAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('command.VCommandAddDocument') }}
				</button>
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<Tableau :labels="labelTableauDocument" :store-data="[commandsStore.documents[commandId]]" :meta="{ key: 'id_command_document' }"
						:loading="commandsStore.documentsLoading"
						:tableau-css="{ table: 'min-w-full table-auto', thead: 'bg-gray-100', th: 'px-4 py-2 text-center bg-gray-200 sticky top-0', tbody: '', tr: 'transition duration-150 ease-in-out hover:bg-gray-200', td: 'px-4 py-2 border-b border-gray-200' }"
					/>
				</div>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleItems" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': commandId != 'new', 'cursor-not-allowed': commandId == 'new' }">
				{{ $t('command.VCommandItems') }} ({{ commandsStore.itemsTotalCount[commandId] || 0 }})
			</h3>
			<div :class="showItems ? 'block' : 'hidden'" class="p-2">
				<button type="button" @click="itemOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('command.VCommandAddItem') }}
				</button>
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<Tableau :labels="labelTableauItem" :store-data="[commandsStore.items[commandId],itemsStore.items]" :meta="{ key: 'id_item' }"
						:loading="commandsStore.itemsLoading" :schema="schemaItem"
						:tableau-css="{ table: 'min-w-full table-auto', thead: 'bg-gray-100', th: 'px-4 py-2 text-center bg-gray-200 sticky top-0', tbody: '', tr: 'transition duration-150 ease-in-out', td: 'px-4 py-2 border-b border-gray-200' }"
					/>
				</div>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleCommentaires" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': commandId != 'new', 'cursor-not-allowed': commandId == 'new' }">
				{{ $t('command.VCommandCommentaires') }} ({{ commandsStore.commentairesTotalCount[commandId] || 0 }})
			</h3>
			<div :class="showCommentaires ? 'block' : 'hidden'" class="p-2">
				<Commentaire :meta="{ contenu: 'contenu_command_commentaire', key: 'id_command_commentaire', CanEdit: true }"
					:store-data="[commandsStore.commentaires[commandId],usersStore.users,authStore.user,configsStore]"
					:store-function="{ create: (data) => commandsStore.createCommentaire(commandId, data), update: (id, data) => commandsStore.updateCommentaire(commandId, id, data), delete: (id) => commandsStore.deleteCommentaire(commandId, id) }"
					:loading="commandsStore.commentairesLoading" :texte-modal-delete="{ textTitle: 'command.VCommandCommentDeleteTitle', textP: 'command.VCommandCommentDeleteText' }"
				/>
			</div>
		</div>
	</div>
	<div :class="!commandsStore.commands[commandId] && commandId != 'new' ? 'block' : 'hidden'"
		class="text-center">
		<div>{{ $t('command.VCommandLoading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="commandDeleteModalShow" @close-modal="commandDeleteModalShow = false"
		@delete-confirmed="commandDelete" :text-title="'command.VCommandDeleteTitle'"
		:text-p="'command.VCommandDeleteText'"/>

	<div v-if="documentAddModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="documentAddModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<Form :validation-schema="schemaAddDocument" v-slot="{ errors }">
				<h2 class="text-xl mb-4">{{ $t('command.VCommandDocumentAddTitle') }}</h2>
				<div class="flex flex-col">
					<div class="flex flex-col">
						<Field name="name_command_document" type="text"
							v-model="documentModalData.name_command_document"
							:placeholder="$t('command.VCommandDocumentNamePlaceholder')"
							class="w-full p-2 border rounded"
							:class="{ 'border-red-500': errors.name_command_document }" />
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.name_command_document || ' ' }}</span>
					</div>
					<div class="flex flex-col">
						<Field name="document" type="file" @change="handleFileUpload" class="w-full p-2"
							:class="{ 'border-red-500': errors.document }" />
						<span class="h-5 w-80 text-sm">{{ $t('command.VCommandDocumentSize') }} ({{ configsStore.getConfigByKey("max_size_document_in_mb") }}Mo)</span>
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.document || ' ' }}</span>
					</div>
				</div>
				<div class="flex justify-end space-x-2">
					<button type="button" @click="documentAddModalShow = false" class="px-4 py-2 bg-gray-300 rounded">
						{{ $t('command.VCommandDocumentCancel') }}
					</button>
					<button type="button" @click="documentAdd" class="px-4 py-2 bg-blue-500 text-white rounded">
						{{ $t('command.VCommandDocumentAdd') }}
					</button>
				</div>
			</Form>
		</div>
	</div>
	<div v-if="documentEditModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="documentEditModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<Form :validation-schema="schemaEditDocument" v-slot="{ errors }">
				<h2 class="text-xl mb-4">{{ $t('command.VCommandDocumentEditTitle') }}</h2>
				<div class="flex flex-col">
					<div class="flex flex-col">
						<Field name="name_command_document" type="text"
							v-model="documentModalData.name_command_document"
							:placeholder="$t('command.VCommandDocumentNamePlaceholder')"
							class="w-full p-2 border rounded"
							:class="{ 'border-red-500': errors.name_command_document }" />
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.name_command_document || ' ' }}</span>
					</div>
				</div>
				<div class="flex justify-end space-x-2">
					<button type="button" @click="documentEditModalShow = false"
						class="px-4 py-2 bg-gray-300 rounded">
						{{ $t('command.VCommandDocumentCancel') }}
					</button>
					<button type="button" @click="documentEdit" class="px-4 py-2 bg-blue-500 text-white rounded">
						{{ $t('command.VCommandDocumentEdit') }}
					</button>
				</div>
			</Form>
		</div>
	</div>

	<ModalDeleteConfirm :show-modal="documentDeleteModalShow" @close-modal="documentDeleteModalShow = false"
		@delete-confirmed="documentDelete" :text-title="'command.VCommandDocumentDeleteTitle'"
		:text-p="'command.VCommandDocumentDeleteText'"/>

	<div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemModalShow = false">
		<div class="bg-white rounded-lg shadow-lg w-3/4 p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('command.VCommandItemTitle') }}</h2>
				<button type="button" @click="itemModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterItem" :store-data="itemsStore.items" @output-filter="updateFilteredItems" />

			<!-- Tableau Items -->
			<div class="overflow-y-auto max-h-96 min-h-96">
				<Tableau :labels="labelTableauModalItem" :store-data="[filteredItems,commandsStore.items[commandId]]" :meta="{ key: 'id_item' }"
					:loading="commandsStore.itemsLoading" :schema="schemaItem"
					:tableau-css="{ table: 'min-w-full table-auto', thead: 'bg-gray-100', th: 'px-4 py-2 text-center bg-gray-200 sticky top-0', tbody: '', tr: 'transition duration-150 ease-in-out', td: 'px-4 py-2 border-b border-gray-200' }"
				/>
			</div>
		</div>
	</div>
</template>
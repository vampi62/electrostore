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
const commandId = route.params.id;

import { useConfigsStore, useCommandsStore, useUsersStore, useItemsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const commandsStore = useCommandsStore();
const usersStore = useUsersStore();
const itemsStore = useItemsStore();
const authStore = useAuthStore();

async function fetchData() {
	if (commandId !== "new") {
		commandsStore.commandEdition = {
			loading: false,
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
	fetchData();
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
	if (commandsStore.commandEdition.status_command === "Terminée") {
		commandsStore.commandEdition.date_livraison_command = formatDateForDatetimeLocal(new Date());
	} else {
		commandsStore.commandEdition.date_livraison_command = null;
	}
	try {
		await schemaCommand.validate(commandsStore.commandEdition, { abortEarly: false });
		await commandsStore.createCommand(commandsStore.commandEdition);
		addNotification({ message: "command.VCommandCreated", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	commandId = commandsStore.commandEdition.id_command;
	router.push("/commands/" + commandsStore.commandEdition.id_command);
};
const commandUpdate = async() => {
	if (commandsStore.commandEdition.status_command === "Terminée" && commandsStore.commands[commandId].status_command !== "Terminée") {
		commandsStore.commandEdition.date_livraison_command = formatDateForDatetimeLocal(new Date());
	} else if (commandsStore.commandEdition.status_command !== "Terminée") {
		commandsStore.commandEdition.date_livraison_command = null;
	}
	try {
		await schemaCommand.validate(commandsStore.commandEdition, { abortEarly: false });
		await commandsStore.updateCommand(commandId, commandsStore.commandEdition);
		addNotification({ message: "command.VCommandUpdated", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
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
const filterText = ref("");
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

const filteredItems = computed(() => {
	return filterText.value
		? Object.values(itemsStore.items).filter((item) => item.nom_item.toLowerCase().includes(filterText.value.toLowerCase()))
		: itemsStore.items;
});

// commentaire
const commentaireModalShow = ref(false);
const commentaireModalData = ref({});
const commentaireFormNew = ref("");
const commentaireSave = async(commentaire = null) => {
	if (commentaire === null) {
		try {
			schemaCommentaire.validateSync({ contenu_command_commentaire: commentaireFormNew.value }, { abortEarly: false });
			await commandsStore.createCommentaire(commandId, {
				contenu_command_commentaire: commentaireFormNew.value,
			});
			addNotification({ message: "command.VCommandCommentAdded", type: "success", i18n: true });
			commentaireFormNew.value = "";
		} catch (e) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
	} else {
		try {
			schemaCommentaire.validateSync(commentaire.tmp, { abortEarly: false });
			await commandsStore.updateCommentaire(commandId, commentaire.id_command_commentaire, commentaire.tmp);
			addNotification({ message: "command.VCommandCommentUpdated", type: "success", i18n: true });
			commentaire.tmp = null;
		} catch (e) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
	}
};
const commentaireDelete = async() => {
	try {
		await commandsStore.deleteCommentaire(commandId, commentaireModalData.value.id_command_commentaire);
		addNotification({ message: "command.VCommandCommentDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "command.VCommandCommentDeleteError", type: "error", i18n: true });
	}
	commentaireModalShow.value = false;
};
const commentaireDeleteOpenModal = (commentaire) => {
	commentaireModalData.value = commentaire;
	commentaireModalShow.value = true;
};

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
	document: Yup.mixed()
		.nullable()
		.test("fileSize", t("command.VCommandDocumentSize") + " " + configsStore.getConfigByKey("max_size_document_in_mb") + "Mo", (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});

const schemaItem = Yup.object().shape({
	qte_command_item: Yup.number()
		.required(t("command.VCommandItemQuantityRequired"))
		.min(1, t("command.VCommandItemQuantityMin")),
	prix_command_item: Yup.number()
		.required(t("command.VCommandItemPriceRequired"))
		.min(1, t("command.VCommandItemPriceMin")),
});

const schemaCommentaire = Yup.object().shape({
	contenu_command_commentaire: Yup.string()
		.required(t("command.VCommandCommentRequired"))
		.max(configsStore.getConfigByKey("max_length_commentaire"), t("command.VCommandCommentMaxLength") + " " + configsStore.getConfigByKey("max_length_commentaire") + t("common.VAllCaracters")),
});

</script>
<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('command.VCommandTitle') }}</h2>
		<div class="flex space-x-4">
			<button type="button" @click="commandSave" v-if="commandId == 'new'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="commandsStore.commandEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('command.VCommandAdd') }}
			</button>
			<button type="button" @click="commandUpdate" v-else
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="commandsStore.commandEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('command.VCommandUpdate') }}
			</button>
			<button type="button" @click="commandDeleteModalShow = true" v-if="commandId != 'new'"
				class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">
				{{ $t('command.VCommandDelete') }}
			</button>
			<RouterLink to="/commands"
				class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">
				{{ $t('command.VCommandBack') }}
			</RouterLink>
		</div>
	</div>
	<div v-if="commandsStore.commands[commandId] || commandId == 'new'">
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
				:class="{ 'cursor-pointer': !commandsStore.documentsLoading && commandId != 'new', 'cursor-not-allowed': commandId == 'new' }">
				{{ $t('command.VCommandDocuments') }} ({{ commandsStore.documents[commandId] ?
					Object.keys(commandsStore.documents[commandId]).length : 0 }})
			</h3>
			<div v-if="!commandsStore.documentsLoading && showDocuments" class="p-2">
				<button type="button" @click="documentAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('command.VCommandAddDocument') }}
				</button>
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead>
							<tr>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('command.VCommandDocumentName') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('command.VCommandDocumentType') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('command.VCommandDocumentDate') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('command.VCommandDocumentActions') }}
								</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="document in commandsStore.documents[commandId]"
								:key="document.id_command_document">
								<td class="px-4 py-2 border-b border-gray-200">{{ document.name_command_document }}</td>
								<td class="px-4 py-2 border-b border-gray-200">{{ document.type_command_document }}</td>
								<td class="px-4 py-2 border-b border-gray-200">{{ document.date_command_document }}</td>
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
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleItems" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !commandsStore.itemsLoading && commandId != 'new', 'cursor-not-allowed': commandId == 'new' }">
				{{ $t('command.VCommandItems') }} ({{ commandsStore.items[commandId] ?
					Object.keys(commandsStore.items[commandId]).length : 0 }})
			</h3>
			<div v-if="!commandsStore.itemsLoading && showItems" class="p-2">
				<button type="button" @click="itemOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('command.VCommandAddItem') }}
				</button>
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead>
							<tr>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('command.VCommandItemName') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('command.VCommandItemQuantity') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('command.VCommandItemPrice') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('command.VCommandItemActions') }}
								</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="item in commandsStore.items[commandId]" :key="item.id_item">
								<td class="px-4 py-2 border-b border-gray-200">
									{{ itemsStore.items[item.id_item].nom_item }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200">
									<template v-if="item.tmp">
										<Form :validation-schema="schemaItem" v-slot="{ errors }">
											<Field name="qte_command_item" type="number"
												v-model="item.tmp.qte_command_item" class="w-20 p-2 border rounded-lg"
												:class="{ 'border-red-500': errors.qte_command_item }" />
										</Form>
									</template>
									<template v-else>
										{{ item.qte_command_item }}
									</template>
								</td>
								<td class="px-4 py-2 border-b border-gray-200">
									<template v-if="item.tmp">
										<Form :validation-schema="schemaItem" v-slot="{ errors }">
											<Field name="prix_command_item" type="number"
												v-model="item.tmp.prix_command_item" class="w-20 p-2 border rounded-lg"
												:class="{ 'border-red-500': errors.prix_command_item }" />
										</Form>
									</template>
									<template v-else>
										{{ item.prix_command_item }}
									</template>
								</td>
								<td class="px-4 py-2 border-b border-gray-200 space-x-2">
									<template v-if="item.tmp">
										<button type="button" @click="itemSave(item)"
											class="px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600">
											{{ $t('command.VCommandItemSave') }}
										</button>
										<button type="button" @click="item.tmp = null"
											class="px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
											{{ $t('command.VCommandItemCancel') }}
										</button>
									</template>
									<template v-else>
										<button type="button" @click="item.tmp = { ...item }"
											class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
											{{ $t('command.VCommandItemEdit') }}
										</button>
										<button type="button" @click="itemDelete(item)"
											class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
											{{ $t('command.VCommandItemDelete') }}
										</button>
									</template>
								</td>
							</tr>
						</tbody>
					</table>
				</div>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleCommentaires" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !commandsStore.commentairesLoading && commandId != 'new', 'cursor-not-allowed': commandId == 'new' }">
				{{ $t('command.VCommandCommentaires') }} ({{ commandsStore.commentaires[commandId] ?
					Object.keys(commandsStore.commentaires[commandId]).length : 0 }})
			</h3>
			<div v-if="!commandsStore.commentairesLoading && showCommentaires" class="p-2">
				<!-- Zone de saisie de commentaire -->
				<Form :validation-schema="schemaCommentaire" v-slot="{ errors }">
					<div class="flex items-center space-x-4">
						<Field name="contenu_command_commentaire" type="text" v-model="commentaireFormNew"
							:placeholder="$t('command.VCommandCommentPlaceholder')"
							class="w-full p-2 border rounded-lg"
							:class="{ 'border-red-500': errors.contenu_command_commentaire }" />
						<button type="button" @click="commentaireSave(null)"
							class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
							{{ $t('command.VCommandCommentAdd') }}
						</button>
					</div>
				</Form>
				<!-- Affichage des commentaires existants -->
				<div class="space-y-4 overflow-x-auto max-h-64 overflow-y-auto">
					<div v-for="commentaire in commandsStore.commentaires[commandId]"
						:key="commentaire.id_command_commentaire" class="flex flex-col border p-4 rounded-lg">
						<div :class="{
							'text-right': commentaire.id_user === authStore.user.id_user,
							'text-left': commentaire.id_user !== authStore.user.id_user
						}" class="text-sm text-gray-600">
							<span class="font-semibold">
								{{ usersStore.users[commentaire.id_user].nom_user }} {{
									usersStore.users[commentaire.id_user].prenom_user }}
							</span>
							<span class="text-xs text-gray-500">
								- {{ commentaire.date_command_commentaire }}
							</span>
						</div>
						<div class="text-center text-gray-800 mb-2">
							<template v-if="commentaire.tmp">
								<Form :validation-schema="schemaCommentaire" v-slot="{ errors }">
									<Field name="contenu_command_commentaire" type="text"
										v-model="commentaire.tmp.contenu_command_commentaire"
										class="w-full p-2 border rounded-lg"
										:class="{ 'border-red-500': errors.contenu_command_commentaire }" />
									<div class="flex justify-end space-x-2 mt-2">
										<button type="button" @click="commentaireSave(commentaire)"
											class="px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600">
											{{ $t('command.VCommandCommentSave') }}
										</button>
										<button type="button" @click="commentaire.tmp = null"
											class="px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
											{{ $t('command.VCommandCommentCancel') }}
										</button>
									</div>
								</Form>
							</template>
							<template v-else>
								<div :class="{
									'text-right': commentaire.id_user === authStore.user.id_user,
									'text-left': commentaire.id_user !== authStore.user.id_user
								}">
									{{ commentaire.contenu_command_commentaire }}
								</div>
								<!-- Boutons modifier/supprimer si conditions remplies -->
								<div v-if="commentaire.id_user === authStore.user.id_user || authStore.user.role === 'admin'"
									class="flex justify-end space-x-2">
									<button type="button" @click="commentaire.tmp = { ...commentaire }"
										class="px-3 py-1 bg-yellow-400 text-white rounded-lg hover:bg-yellow-500">
										{{ $t('command.VCommandCommentEdit') }}
									</button>
									<button type="button" @click="commentaireDeleteOpenModal(commentaire)"
										class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
										{{ $t('command.VCommandCommentDelete') }}
									</button>
								</div>
							</template>
						</div>
					</div>
				</div>
			</div>
		</div>
	</div>
	<div v-else>
		<div>{{ $t('command.VCommandLoading') }}</div>
	</div>

	<div v-if="commandDeleteModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="commandDeleteModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t('command.VCommandDeleteTitle') }}</h2>
			<p>{{ $t('command.VCommandDeleteText') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="commandDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('command.VCommandDeleteConfirm') }}
				</button>
				<button type="button" @click="commandDeleteModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('command.VCommandDeleteCancel') }}
				</button>
			</div>
		</div>
	</div>

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
					<div class="flex flex-col">
						<Field name="document" type="file" @change="handleFileUpload" class="w-full p-2"
							:class="{ 'border-red-500': errors.document }" />
						<span class="h-5 w-80 text-sm">{{ $t('command.VCommandDocumentSize') }} ({{ configsStore.getConfigByKey("max_size_document_in_mb") }}Mo)</span>
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.document || ' ' }}</span>
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
	<div v-if="documentDeleteModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="documentDeleteModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t('command.VCommandDocumentDeleteTitle') }}</h2>
			<p>{{ $t('command.VCommandDocumentDeleteText') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="documentDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('command.VCommandDocumentDeleteConfirm') }}
				</button>
				<button type="button" @click="documentDeleteModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('command.VCommandDocumentCancel') }}
				</button>
			</div>
		</div>
	</div>

	<div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemModalShow = false">
		<div class="bg-white rounded-lg shadow-lg w-3/4 p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('command.VCommandItemTitle') }}</h2>
				<button type="button" @click="itemModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<div class="my-4 flex gap-4">
				<input type="text" v-model="filterText"
					:placeholder="$t('command.VCommandItemFilterPlaceholder')"
					class="border p-2 rounded w-full">
			</div>

			<!-- Tableau Items -->
			<div class="overflow-y-auto max-h-96 min-h-96">
				<table class="min-w-full bg-white border border-gray-200">
					<thead class="bg-gray-100 sticky top-0">
						<tr>
							<th class="px-4 py-2 border-b">{{ $t('command.VCommandItemName') }}</th>
							<th class="px-4 py-2 border-b">{{ $t('command.VCommandItemQuantity') }}</th>
							<th class="px-4 py-2 border-b">{{ $t('command.VCommandItemPrice') }}</th>
							<th class="px-4 py-2 border-b">{{ $t('command.VCommandItemActions') }}</th>
						</tr>
					</thead>
					<tbody>
						<tr v-for="item in filteredItems" :key="item.id_item">
							<td class="px-4 py-2 border-b">{{ item.nom_item }}</td>
							<td class="px-4 py-2 border-b">
								<template v-if="item.tmp">
									<Form :validation-schema="schemaItem" v-slot="{ errors }">
										<Field name="qte_command_item" type="number" v-model="item.tmp.qte_command_item"
											class="w-20 p-2 border rounded-lg"
											:class="{ 'border-red-500': errors.qte_command_item }" />
									</Form>
								</template>
								<template v-else-if="commandsStore.items[commandId][item.id_item]">
									<div>{{ commandsStore.items[commandId][item.id_item].qte_command_item }}</div>
								</template>
								<template v-else>
									<div></div>
								</template>
							</td>
							<td class="px-4 py-2 border-b">
								<template v-if="item.tmp">
									<Form :validation-schema="schemaItem" v-slot="{ errors }">
										<Field name="prix_command_item" type="number"
											v-model="item.tmp.prix_command_item" class="w-20 p-2 border rounded-lg"
											:class="{ 'border-red-500': errors.prix_command_item }" />
									</Form>
								</template>
								<template v-else-if="commandsStore.items[commandId][item.id_item]">
									<div>{{ commandsStore.items[commandId][item.id_item].prix_command_item }}</div>
								</template>
								<template v-else>
									<div></div>
								</template>
							</td>
							<td class="px-4 py-2 border-b">
								<template v-if="item.tmp">
									<button type="button" @click="itemSave(item)"
										class="px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600">
										{{ $t('command.VCommandItemSave') }}
									</button>
									<button type="button" @click="item.tmp = null"
										class="px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
										{{ $t('command.VCommandItemCancel') }}
									</button>
								</template>
								<template v-else>
									<button v-if="!commandsStore.items[commandId][item.id_item]" type="button"
										@click="item.tmp = { prix_command_item: 1, qte_command_item: 1, id_item: item.id_item }"
										class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
										{{ $t('command.VCommandItemAdd') }}
									</button>
									<button v-else type="button"
										@click="item.tmp = { ...commandsStore.items[commandId][item.id_item] }"
										class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
										{{ $t('command.VCommandItemEdit') }}
									</button>
									<button type="button" @click="itemDelete(item)"
										class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
										{{ $t('command.VCommandItemDelete') }}
									</button>
								</template>
							</td>
						</tr>
					</tbody>
				</table>
			</div>
		</div>
	</div>
	<div v-if="commentaireModalShow" class="fixed inset-0 flex items-center justify-center bg-black bg-opacity-50"
		@click="commentaireModalShow = false">
		<div class="bg-white p-6 rounded-lg shadow-lg" @click.stop>
			<h2 class="text-lg font-semibold">{{ $t('command.VCommandCommentDeleteTitle') }}</h2>
			<p>{{ $t('command.VCommandCommentDeleteText') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="commentaireDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('command.VCommandCommentDeleteConfirm') }}
				</button>
				<button type="button" @click="commentaireModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('command.VCommandCommentDeleteCancel') }}
				</button>
			</div>
		</div>
	</div>
</template>
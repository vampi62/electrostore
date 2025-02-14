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
const projetId = route.params.id;

import { useProjetsStore, useUsersStore, useItemsStore, useAuthStore } from "@/stores";
const projetsStore = useProjetsStore();
const usersStore = useUsersStore();
const itemsStore = useItemsStore();
const authStore = useAuthStore();

async function fetchData() {
	if (projetId !== "new") {
		projetsStore.projetEdition = {
			loading: false,
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
	} else {
		projetsStore.projetEdition = {
			loading: false,
		};
	}
}
onMounted(() => {
	fetchData();
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
		await projetsStore.createProjet(projetsStore.projetEdition);
		addNotification({ message: "projet.VProjetCreated", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	projetId = projetsStore.projetEdition.id_projet;
	router.push("/projets/" + projetsStore.projetEdition.id_projet);
};
const projetUpdate = async() => {
	try {
		await schemaProjet.validate(projetsStore.projetEdition, { abortEarly: false });
		await projetsStore.updateProjet(projetId, projetsStore.projetEdition);
		addNotification({ message: "projet.VProjetUpdated", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
};
const projetDelete = async() => {
	try {
		await projetsStore.deleteProjet(projetId);
		addNotification({ message: "projet.VProjetDeleted", type: "success", i18n: true });
		router.push("/projets");
	} catch (e) {
		addNotification({ message: "projet.VProjetDeleteError", type: "error", i18n: true });
	}
	projetDeleteModalShow.value = false;
};
const formatDateForDatetimeLocal = (date) => {
	const pad = (num) => String(num).padStart(2, "0");
	return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};

// document
const documentAddModalShow = ref(false);
const documentEditModalShow = ref(false);
const documentDeleteModalShow = ref(false);
const documentModalData = ref({ id_projet_document: null, name_projet_document: "", document: null, isEdit: false });
const documentAddOpenModal = () => {
	documentModalData.value = { name_projet_document: "", document: null, isEdit: false };
	documentAddModalShow.value = true;
};
const documentEditOpenModal = (doc) => {
	documentModalData.value = { id_projet_document: doc.id_projet_document, name_projet_document: doc.name_projet_document, document: null, isEdit: true };
	documentEditModalShow.value = true;
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
		await projetsStore.updateDocument(projetId, documentModalData.value.id_projet_document, documentModalData.value);
		addNotification({ message: "projet.VProjetDocumentUpdated", type: "success", i18n: true });
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
		await projetsStore.deleteDocument(projetId, documentModalData.value.id_projet_document);
		addNotification({ message: "projet.VProjetDocumentDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "projet.VProjetDocumentDeleteError", type: "error", i18n: true });
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
const filterText = ref("");
const itemModalShow = ref(false);
const itemOpenAddModal = () => {
	itemModalShow.value = true;
	itemsStore.getItemByInterval();
};
const itemSave = async(item) => {
	if (projetsStore.items[projetId][item.id_item]) {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await projetsStore.updateItem(projetId, item.tmp.id_item, item.tmp);
			addNotification({ message: "projet.VProjetItemUpdated", type: "success", i18n: true });
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
			await projetsStore.createItem(projetId, item.tmp);
			addNotification({ message: "projet.VProjetItemAdded", type: "success", i18n: true });
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
		await projetsStore.deleteItem(projetId, item.id_item);
		addNotification({ message: "projet.VProjetItemDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "projet.VProjetItemDeleteError", type: "error", i18n: true });
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
			schemaCommentaire.validateSync({ contenu_projet_commentaire: commentaireFormNew.value }, { abortEarly: false });
			await projetsStore.createCommentaire(projetId, {
				contenu_projet_commentaire: commentaireFormNew.value,
			});
			addNotification({ message: "projet.VProjetCommentAdded", type: "success", i18n: true });
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
			await projetsStore.updateCommentaire(projetId, commentaire.id_projet_commentaire, commentaire.tmp);
			addNotification({ message: "projet.VProjetCommentUpdated", type: "success", i18n: true });
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
		await projetsStore.deleteCommentaire(projetId, commentaireModalData.value.id_projet_commentaire);
		addNotification({ message: "projet.VProjetCommentDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "projet.VProjetCommentDeleteError", type: "error", i18n: true });
	}
	commentaireModalShow.value = false;
};
const commentaireDeleteOpenModal = (commentaire) => {
	commentaireModalData.value = commentaire;
	commentaireModalShow.value = true;
};

const schemaProjet = Yup.object().shape({
	nom_projet: Yup.string()
		.required(t("projet.VProjetNameRequired")),
	description_projet: Yup.string()
		.required(t("projet.VProjetDescriptionRequired")),
	url_projet: Yup.string()
		.required(t("projet.VProjetUrlRequired")),
	status_projet: Yup.string()
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
		.required(t("projet.VProjetDocumentNameRequired")),
	document: Yup.mixed()
		.required(t("projet.VProjetDocumentRequired"))
		.test("fileSize", t("projet.VProjetDocumentSize"), (value) => !value || value?.size <= 2000000),
});
const schemaEditDocument = Yup.object().shape({
	name_projet_document: Yup.string()
		.required(t("projet.VProjetDocumentNameRequired")),
	document: Yup.mixed()
		.nullable(),
});

const schemaItem = Yup.object().shape({
	qte_projet_item: Yup.number()
		.required(t("projet.VProjetItemQuantityRequired"))
		.min(1, t("projet.VProjetItemQuantityMin")),
});

const schemaCommentaire = Yup.object().shape({
	contenu_projet_commentaire: Yup.string()
		.required(t("projet.VProjetCommentRequired")),
});

</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('projet.VProjetTitle') }}</h2>
		<div class="flex space-x-4">
			<button type="button" @click="projetSave" v-if="projetId == 'new'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="projetsStore.projetEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('projet.VProjetAdd') }}
			</button>
			<button type="button" @click="projetUpdate" v-else
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="projetsStore.projetEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('projet.VProjetUpdate') }}
			</button>
			<button type="button" @click="projetDeleteOpenModal" v-if="projetId != 'new'"
				class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">
				{{ $t('projet.VProjetDelete') }}
			</button>
			<RouterLink to="/projets"
				class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">
				{{ $t('projet.VProjetBack') }}
			</RouterLink>
		</div>
	</div>
	<div v-if="projetsStore.projets[projetId] || projetId == 'new'">
		<div class="mb-6 flex justify-between">
			<Form :validation-schema="schemaProjet" v-slot="{ errors }" @submit.prevent="">
				<table class="table-auto text-gray-700">
					<tbody>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('projet.VProjetName') }}:</td>
							<td class="flex flex-col">
								<Field name="nom_projet" type="text"
									v-model="projetsStore.projetEdition.nom_projet"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.nom_projet }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.nom_projet || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('projet.VProjetDescription') }}:</td>
							<td class="flex flex-col">
								<Field name="description_projet" v-slot="{ field }">
									<textarea v-bind="field" v-model="projetsStore.projetEdition.description_projet"
										:value="projetsStore.projetEdition.description_projet"
										class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300 resize-y"
										:class="{ 'border-red-500': errors.description_projet }" rows="4">
									</textarea>
								</Field>
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.description_projet || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('projet.VProjetUrl') }}:</td>
							<td class="flex flex-col">
								<Field name="url_projet" type="text" v-model="projetsStore.projetEdition.url_projet"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.url_projet }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.url_projet || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('projet.VProjetStatus') }}:</td>
							<td class="flex flex-col">
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
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('projet.VProjetStartDate') }}:</td>
							<td class="flex flex-col">
								<!-- format date permit is only YYYY-MM-DDTHH-mm-->
								<Field name="date_debut_projet" type="datetime-local"
									v-model="projetsStore.projetEdition.date_debut_projet"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.date_debut_projet }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.date_debut_projet || ' ' }}</span>
							</td>
						</tr>
						<tr class="pb-4">
							<td class="font-semibold pr-4 align-text-top">{{ $t('projet.VProjetEndDate') }}:</td>
							<td class="flex flex-col">
								<Field name="date_fin_projet" type="datetime-local"
									v-model="projetsStore.projetEdition.date_fin_projet"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.date_fin_projet }" />
								<span class="text-red-500 h-5 w-80 text-sm">
									{{ errors.date_fin_projet || ' ' }}
								</span>
							</td>
						</tr>
					</tbody>
				</table>
			</Form>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleDocuments" class="text-xl font-semibold  bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !projetsStore.documentsLoading && projetId != 'new', 'cursor-not-allowed': projetId == 'new' }">
				{{ $t('projet.VProjetDocuments') }} ({{ projetsStore.documents[projetId] ?
					Object.keys(projetsStore.documents[projetId]).length : 0 }})
			</h3>
			<div v-if="!projetsStore.documentsLoading && showDocuments" class="p-2">
				<button type="button" @click="documentAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projet.VProjetAddDocument') }}
				</button>
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead>
							<tr>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('projet.VProjetDocumentName') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('projet.VProjetDocumentType') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('projet.VProjetDocumentDate') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('projet.VProjetDocumentActions') }}
								</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="document in projetsStore.documents[projetId]"
								:key="document.id_projet_document">
								<td class="px-4 py-2 border-b border-gray-200">{{ document.name_projet_document }}</td>
								<td class="px-4 py-2 border-b border-gray-200">{{ document.type_projet_document }}</td>
								<td class="px-4 py-2 border-b border-gray-200">{{ document.date_projet_document }}</td>
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
				:class="{ 'cursor-pointer': !projetsStore.itemsLoading && projetId != 'new', 'cursor-not-allowed': projetId == 'new' }">
				{{ $t('projet.VProjetItems') }} ({{ projetsStore.items[projetId] ?
					Object.keys(projetsStore.items[projetId]).length : 0 }})
			</h3>
			<div v-if="!projetsStore.itemsLoading && showItems" class="p-2">
				<button type="button" @click="itemOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projet.VProjetAddItem') }}
				</button>
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead>
							<tr>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('projet.VProjetItemName') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('projet.VProjetItemQuantity') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('projet.VProjetItemActions') }}
								</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="item in projetsStore.items[projetId]" :key="item.id_item">
								<td class="px-4 py-2 border-b border-gray-200">
									{{ itemsStore.items[item.id_item].nom_item }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200">
									<template v-if="item.tmp">
										<Form :validation-schema="schemaItem" v-slot="{ errors }">
											<Field name="qte_projet_item" type="number"
												v-model="item.tmp.qte_projet_item" class="w-20 p-2 border rounded-lg"
												:class="{ 'border-red-500': errors.qte_projet_item }" />
										</Form>
									</template>
									<template v-else>
										{{ item.qte_projet_item }}
									</template>
								</td>
								<td class="px-4 py-2 border-b border-gray-200 space-x-2">
									<template v-if="item.tmp">
										<button type="button" @click="itemSave(item)"
											class="px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600">
											{{ $t('projet.VProjetItemSave') }}
										</button>
										<button type="button" @click="item.tmp = null"
											class="px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
											{{ $t('projet.VProjetItemCancel') }}
										</button>
									</template>
									<template v-else>
										<button type="button" @click="item.tmp = { ...item }"
											class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
											{{ $t('projet.VProjetItemEdit') }}
										</button>
										<button type="button" @click="itemDelete(item)"
											class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
											{{ $t('projet.VProjetItemDelete') }}
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
				:class="{ 'cursor-pointer': !projetsStore.commentairesLoading && projetId != 'new', 'cursor-not-allowed': projetId == 'new' }">
				{{ $t('projet.VProjetCommentaires') }} ({{ projetsStore.commentaires[projetId] ?
					Object.keys(projetsStore.commentaires[projetId]).length : 0 }})
			</h3>
			<div v-if="!projetsStore.commentairesLoading && showCommentaires" class="p-2">
				<!-- Zone de saisie de commentaire -->
				<Form :validation-schema="schemaCommentaire" v-slot="{ errors }">
					<div class="flex items-center space-x-4">
						<Field name="contenu_projet_commentaire" type="text" v-model="commentaireFormNew"
							:placeholder="$t('projet.VProjetCommentPlaceholder')"
							class="w-full p-2 border rounded-lg"
							:class="{ 'border-red-500': errors.contenu_projet_commentaire }" />
						<button type="button" @click="commentaireSave(null)"
							class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
							{{ $t('projet.VProjetCommentAdd') }}
						</button>
					</div>
				</Form>
				<!-- Affichage des commentaires existants -->
				<div class="space-y-4 overflow-x-auto max-h-64 overflow-y-auto">
					<div v-for="commentaire in projetsStore.commentaires[projetId]"
						:key="commentaire.id_projet_commentaire" class="flex flex-col border p-4 rounded-lg">
						<div :class="{
							'text-right': commentaire.id_user === authStore.user.id_user,
							'text-left': commentaire.id_user !== authStore.user.id_user
						}" class="text-sm text-gray-600">
							<span class="font-semibold">
								{{ usersStore.users[commentaire.id_user].nom_user }} {{
									usersStore.users[commentaire.id_user].prenom_user }}
							</span>
							<span class="text-xs text-gray-500">
								- {{ commentaire.date_projet_commentaire }}
							</span>
						</div>
						<div class="text-center text-gray-800 mb-2">
							<template v-if="commentaire.tmp">
								<Form :validation-schema="schemaCommentaire" v-slot="{ errors }">
									<Field name="contenu_projet_commentaire" type="text"
										v-model="commentaire.tmp.contenu_projet_commentaire"
										class="w-full p-2 border rounded-lg"
										:class="{ 'border-red-500': errors.contenu_projet_commentaire }" />
									<div class="flex justify-end space-x-2 mt-2">
										<button type="button" @click="commentaireSave(commentaire)"
											class="px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600">
											{{ $t('projet.VProjetCommentSave') }}
										</button>
										<button type="button" @click="commentaire.tmp = null"
											class="px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
											{{ $t('projet.VProjetCommentCancel') }}
										</button>
									</div>
								</Form>
							</template>
							<template v-else>
								<div :class="{
									'text-right': commentaire.id_user === authStore.user.id_user,
									'text-left': commentaire.id_user !== authStore.user.id_user
								}">
									{{ commentaire.contenu_projet_commentaire }}
								</div>
								<!-- Boutons modifier/supprimer si conditions remplies -->
								<div v-if="commentaire.id_user === authStore.user.id_user || authStore.user.role === 'admin'"
									class="flex justify-end space-x-2">
									<button type="button" @click="commentaire.tmp = { ...commentaire }"
										class="px-3 py-1 bg-yellow-400 text-white rounded-lg hover:bg-yellow-500">
										{{ $t('projet.VProjetCommentEdit') }}
									</button>
									<button type="button" @click="commentaireDeleteOpenModal(commentaire)"
										class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
										{{ $t('projet.VProjetCommentDelete') }}
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
		<div>{{ $t('projet.VProjetLoading') }}</div>
	</div>

	<div v-if="projetDeleteModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="projetDeleteModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t('projet.VProjetDeleteTitle') }}</h2>
			<p>{{ $t('projet.VProjetDeleteText') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="projetDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('projet.VProjetDeleteConfirm') }}
				</button>
				<button type="button" @click="projetDeleteModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('projet.VProjetDeleteCancel') }}
				</button>
			</div>
		</div>
	</div>

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
						<span class="h-5 w-80 text-sm">{{ $t('projet.VProjetDocumentSize') }}</span>
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
	<div v-if="documentEditModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="documentEditModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<Form :validation-schema="schemaEditDocument" v-slot="{ errors }">
				<h2 class="text-xl mb-4">{{ $t('projet.VProjetDocumentEditTitle') }}</h2>
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
						<span class="h-5 w-80 text-sm">{{ $t('projet.VProjetDocumentSize') }}</span>
						<span class="text-red-500 h-5 w-80 text-sm">{{ errors.document || ' ' }}</span>
					</div>
				</div>
				<div class="flex justify-end space-x-2">
					<button type="button" @click="documentEditModalShow = false"
						class="px-4 py-2 bg-gray-300 rounded">{{
							$t('projet.VProjetDocumentCancel') }}</button>
					<button type="button" @click="documentEdit" class="px-4 py-2 bg-blue-500 text-white rounded">
						{{ $t('projet.VProjetDocumentEdit') }}
					</button>
				</div>
			</Form>
		</div>
	</div>
	<div v-if="documentDeleteModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="documentDeleteModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t('projet.VProjetDocumentDeleteTitle') }}</h2>
			<p>{{ $t('projet.VProjetDocumentDeleteText') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="documentDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('projet.VProjetDocumentDeleteConfirm') }}
				</button>
				<button type="button" @click="documentDeleteModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('projet.VProjetDocumentCancel') }}
				</button>
			</div>
		</div>
	</div>

	<div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemModalShow = false">
		<div class="bg-white rounded-lg shadow-lg w-3/4 p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('projet.VProjetItemTitle') }}</h2>
				<button type="button" @click="itemModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<div class="my-4 flex gap-4">
				<input type="text" v-model="filterText"
					:placeholder="$t('projet.VProjetItemFilterPlaceholder')"
					class="border p-2 rounded w-full">
			</div>

			<!-- Tableau Items -->
			<div class="overflow-y-auto max-h-96 min-h-96">
				<table class="min-w-full bg-white border border-gray-200">
					<thead class="bg-gray-100 sticky top-0">
						<tr>
							<th class="px-4 py-2 border-b">{{ $t('projet.VProjetItemName') }}</th>
							<th class="px-4 py-2 border-b">{{ $t('projet.VProjetItemQuantity') }}</th>
							<th class="px-4 py-2 border-b">{{ $t('projet.VProjetItemActions') }}</th>
						</tr>
					</thead>
					<tbody>
						<tr v-for="item in filteredItems" :key="item.id_item">
							<td class="px-4 py-2 border-b">{{ item.nom_item }}</td>
							<td class="px-4 py-2 border-b">
								<template v-if="item.tmp">
									<Form :validation-schema="schemaItem" v-slot="{ errors }">
										<Field name="qte_projet_item" type="number" v-model="item.tmp.qte_projet_item"
											class="w-20 p-2 border rounded-lg"
											:class="{ 'border-red-500': errors.qte_projet_item }" />
									</Form>
								</template>
								<template v-else-if="projetsStore.items[projetId][item.id_item]">
									<div>{{ projetsStore.items[projetId][item.id_item].qte_projet_item }}</div>
								</template>
								<template v-else>
									<div></div>
								</template>
							</td>
							<td class="px-4 py-2 border-b">
								<template v-if="item.tmp">
									<button type="button" @click="itemSave(item)"
										class="px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600">
										{{ $t('projet.VProjetItemSave') }}
									</button>
									<button type="button" @click="item.tmp = null"
										class="px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
										{{ $t('projet.VProjetItemCancel') }}
									</button>
								</template>
								<template v-else>
									<button v-if="!projetsStore.items[projetId][item.id_item]" type="button"
										@click="item.tmp = { prix_projet_item: 1, qte_projet_item: 1, id_item: item.id_item }"
										class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
										{{ $t('projet.VProjetItemAdd') }}
									</button>
									<button v-else type="button"
										@click="item.tmp = { ...projetsStore.items[projetId][item.id_item] }"
										class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
										{{ $t('projet.VProjetItemEdit') }}
										</button>
									<button type="button" @click="itemDelete(item)"
										class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
										{{ $t('projet.VProjetItemDelete') }}
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
			<h2 class="text-lg font-semibold">{{ $t('projet.VProjetCommentDeleteTitle') }}</h2>
			<p>{{ $t('projet.VProjetCommentDeleteText') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="commentaireDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('projet.VProjetCommentDeleteConfirm') }}
				</button>
				<button type="button" @click="commentaireModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('projet.VProjetCommentDeleteCancel') }}
				</button>
			</div>
		</div>
	</div>
</template>

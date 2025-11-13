<script setup>
import { onMounted, onBeforeUnmount, computed, ref, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const projetId = ref(route.params.id);

import { downloadFile, viewFile } from "@/utils";

import { useConfigsStore, useProjetsStore, useUsersStore, useItemsStore, useProjetTagsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const projetsStore = useProjetsStore();
const usersStore = useUsersStore();
const itemsStore = useItemsStore();
const projetTagsStore = useProjetTagsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (projetId.value === "new") {
		projetsStore.projetEdition = {
			loading: false,
		};
	} else {
		projetsStore.projetEdition = {
			loading: true,
		};
		try {
			await projetsStore.getProjetById(projetId.value);
		} catch {
			delete projetsStore.projets[projetId.value];
			addNotification({ message: "projet.VProjetNotFound", type: "error", i18n: true });
			router.push("/projets");
			return;
		}
		projetsStore.getCommentaireByInterval(projetId.value, 100, 0, ["user"]);
		projetsStore.getDocumentByInterval(projetId.value, 100, 0);
		projetsStore.getItemByInterval(projetId.value, 100, 0, ["item"]);
		projetsStore.getProjetTagProjetByInterval(projetId.value, 100, 0, ["projet_tag"]);
		projetsStore.getStatusHistoryByInterval(projetId.value, 100, 0);
		projetsStore.projetEdition = {
			loading: false,
			nom_projet: projetsStore.projets[projetId.value].nom_projet,
			description_projet: projetsStore.projets[projetId.value].description_projet,
			url_projet: projetsStore.projets[projetId.value].url_projet,
			status_projet: projetsStore.projets[projetId.value].status_projet,
			date_debut_projet: projetsStore.projets[projetId.value].date_debut_projet,
			date_fin_projet: projetsStore.projets[projetId.value].date_fin_projet,
		};
		usersStore.users[authStore.user.id_user] = authStore.user; // avoids undefined user when the current user posts first comment
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
const dateDebut = computed(() => {
	// don't return the GMT offset to avoid timezone issues
	return projetsStore.projetEdition.date_debut_projet ? new Date(projetsStore.projetEdition.date_debut_projet).toISOString().replace(/.\d+Z$/, "").replace("T", " ") : null;
});
const dateFin = computed(() => {
	return projetsStore.projetEdition.date_fin_projet ? new Date(projetsStore.projetEdition.date_fin_projet).toISOString().replace(/.\d+Z$/, "").replace("T", " ") : null;
});

// tag
const filterTag = ref([
	{ key: "nom_projet_tag", value: "", type: "text", label: "", placeholder: t("projet.VProjetTagFilterPlaceholder"), compareMethod: "contain", class: "w-full" },
]);
function tagSave(id_tag) {
	try {
		projetsStore.createProjetTagProjet(projetId.value,  { id_projet_tag: id_tag });
		addNotification({ message: "projet.VProjetTagAdded", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
}
function tagDelete(id_tag) {
	try {
		projetsStore.deleteProjetTagProjet(projetId.value, id_tag);
		addNotification({ message: "projet.VProjetTagDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
}

// projet
const projetDeleteModalShow = ref(false);
const projetTypeStatus = ref([[0, t("projet.VProjetsStatus0")], [1, t("projet.VProjetsStatus1")], [2, t("projet.VProjetsStatus2")], [3, t("projet.VProjetsStatus3")], [4, t("projet.VProjetsStatus4")], [5, t("projet.VProjetsStatus5")]]);
const projetSave = async() => {
	try {
		createSchema().validateSync(projetsStore.projetEdition, { abortEarly: false });
		if (projetId.value === "new") {
			await projetsStore.createProjet({ ...projetsStore.projetEdition });
			addNotification({ message: "projet.VProjetCreated", type: "success", i18n: true });
		} else {
			await projetsStore.updateProjet(projetId.value, { ...projetsStore.projetEdition });
			addNotification({ message: "projet.VProjetUpdated", type: "success", i18n: true });
		}
	} catch (e) {
		if (e.inner) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
	if (projetId.value === "new") {
		projetId.value = String(projetsStore.projetEdition.id_projet);
		router.push("/projets/" + projetId.value);
	}
};
const projetDelete = async() => {
	try {
		await projetsStore.deleteProjet(projetId.value);
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
const documentModalData = ref({ id_projet_document: null, name_projet_document: "", document: null });
const documentAddOpenModal = () => {
	documentModalData.value = { name_projet_document: "", document: null };
	documentAddModalShow.value = true;
};
const documentDeleteOpenModal = (doc) => {
	documentModalData.value = doc;
	documentDeleteModalShow.value = true;
};
const documentAdd = async() => {
	try {
		schemaAddDocument.validateSync(documentModalData.value, { abortEarly: false });
		await projetsStore.createDocument(projetId.value, documentModalData.value);
		addNotification({ message: "projet.VProjetDocumentAdded", type: "success", i18n: true });
		documentAddModalShow.value = false;
	} catch (e) {
		if (e.inner) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const documentEdit = async(row) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		await projetsStore.updateDocument(projetId.value, row.id_projet_document, row);
		addNotification({ message: "projet.VProjetDocumentUpdated", type: "success", i18n: true });
	} catch (e) {
		if (e.inner) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const documentDelete = async() => {
	try {
		await projetsStore.deleteDocument(projetId.value, documentModalData.value.id_projet_document);
		addNotification({ message: "projet.VProjetDocumentDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	documentDeleteModalShow.value = false;
};
const documentDownload = async(fileContent) => {
	const file = await projetsStore.downloadDocument(projetId.value, fileContent.id_projet_document);
	downloadFile(file, { keyName: fileContent.name_projet_document, keyType: fileContent.type_projet_document });
};
const documentView = async(fileContent) => {
	const file = await projetsStore.downloadDocument(projetId.value, fileContent.id_projet_document);
	if (viewFile(file, { keyName: fileContent.name_projet_document, keyType: fileContent.type_projet_document })) {
		addNotification({ message: "projet.VProjetDocumentOpenInNewTab", type: "success", i18n: true });
	} else {
		addNotification({ message: "projet.VProjetDocumentNotSupported", type: "error", i18n: true });
	}
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
	if (projetsStore.items[projetId.value][item.id_item]) {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await projetsStore.updateItem(projetId.value, item.tmp.id_item, item.tmp);
			addNotification({ message: "projet.VProjetItemUpdated", type: "success", i18n: true });
			item.tmp = null;
		} catch (e) {
			if (e.inner) {
				e.inner.forEach((error) => {
					addNotification({ message: error.message, type: "error", i18n: false });
				});
				return;
			}
			addNotification({ message: e, type: "error", i18n: false });
			return;
		}
	} else {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await projetsStore.createItem(projetId.value, item.tmp);
			addNotification({ message: "projet.VProjetItemAdded", type: "success", i18n: true });
			item.tmp = null;
		} catch (e) {
			if (e.inner) {
				e.inner.forEach((error) => {
					addNotification({ message: error.message, type: "error", i18n: false });
				});
				return;
			}
			addNotification({ message: e, type: "error", i18n: false });
			return;
		}
	}
};
const itemDelete = async(item) => {
	try {
		await projetsStore.deleteItem(projetId.value, item.id_item);
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

const createSchema = () => {
	return Yup.object().shape({
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
		status_projet: Yup.number()
			.required(t("projet.VProjetStatusRequired")),
	});
};

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

const labelForm = ref([
	{ key: "nom_projet", label: "projet.VProjetName", type: "text" },
	{ key: "description_projet", label: "projet.VProjetDescription", type: "textarea", rows: 4 },
	{ key: "url_projet", label: "projet.VProjetUrl", type: "text" },
	{ key: "status_projet", label: "projet.VProjetStatus", type: "select", options: projetTypeStatus },
	{ key: "date_debut_projet", label: "projet.VProjetStartDate", type: "computed", value: dateDebut },
	{ key: "date_fin_projet", label: "projet.VProjetEndDate", type: "computed", value: dateFin },
]);
const labelTableauDocument = ref([
	{ label: "projet.VProjetDocumentName", sortable: true, key: "name_projet_document", type: "text", canEdit: true },
	{ label: "projet.VProjetDocumentType", sortable: true, key: "type_projet_document", type: "text" },
	{ label: "projet.VProjetDocumentDate", sortable: true, key: "created_at", type: "datetime" },
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
			animation: true,
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
			animation: true,
		},
	] },
]);
const labelTableauModalTag = ref([
	{ label: "projet.VProjetTagName", sortable: true, key: "nom_projet_tag", type: "text" },
	{ label: "projet.VProjetTagActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			condition: "!store[1]?.[rowData.id_projet_tag]",
			action: (row) => tagSave(row.id_projet_tag),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			condition: "store[1]?.[rowData.id_projet_tag]",
			action: (row) => tagDelete(row.id_projet_tag),
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
			condition: "store[1]?.[rowData.id_item] === undefined && !rowData.tmp",
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
			animation: true,
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
			animation: true,
		},
	] },
]);
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('projet.VProjetTitle') }}</h2>
		<RouterLink to="/projet-tags"
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block">
			{{ $t('projet.VProjetListTag') }}
		</RouterLink>
		<TopButtonEditElement :main-config="{ path: '/projets', save: { roleRequired: 0, loading: projetsStore.projetEdition.loading }, delete: { roleRequired: 0 } }"
			:id="projetId" :store-user="authStore.user" @button-save="projetSave" @button-delete="projetDeleteModalShow = true"/>
	</div>
	<div v-if="projetsStore.projets[projetId] || projetId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="projetsStore.projetEdition"/>
			<Tags :current-tags="projetsStore.projetTagProjet[projetId] || {}" :tags-store="projetTagsStore.projetTags" :can-edit="projetId !== 'new' && authStore.user.role_user >= 1"
				:delete-function="(value) => tagDelete(value)"
				:fetch-function="(offset, limit) => projetTagsStore.getProjetTagByInterval(limit, offset)"
				:total-count="Number(projetTagsStore.projetTagsTotalCount || 0)"
				:filter-modal="filterTag"
				:tableau-modal="{ 'label': labelTableauModalTag, 'meta': { key: 'id_projet_tag' }, 'css': { component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }
								, 'loading': projetTagsStore.projetTagsLoading }"
				:meta ="{ 'keyPoids': 'poids_projet_tag', 'keyName': 'nom_projet_tag' }"
				/>
		</div>
		<CollapsibleSection title="projet.VProjetDocuments"
			:total-count="Number(projetsStore.documentsTotalCount[projetId] || 0)" :id-page="projetId">
			<template #append-row>
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
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="projet.VProjetItems"
			:total-count="Number(projetsStore.itemsTotalCount[projetId] || 0)" :id-page="projetId">
			<template #append-row>
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
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="projet.VProjetCommentaires"
			:total-count="Number(projetsStore.commentairesTotalCount[projetId] || 0)" :id-page="projetId">
			<template #append-row>
				<Commentaire :meta="{ contenu: 'contenu_projet_commentaire', key: 'id_projet_commentaire', canEdit: true }"
					:store-data="[projetsStore.commentaires[projetId],usersStore.users,authStore.user,configsStore]"
					:store-function="{ create: (data) => projetsStore.createCommentaire(projetId, data), update: (id, data) => projetsStore.updateCommentaire(projetId, id, data), delete: (id) => projetsStore.deleteCommentaire(projetId, id) }"
					:loading="projetsStore.commentairesLoading" :texte-modal-delete="{ textTitle: 'projet.VProjetCommentDeleteTitle', textP: 'projet.VProjetCommentDeleteText' }"
					:total-count="Number(projetsStore.commentairesTotalCount[projetId])"
					:loaded-count="Object.keys(projetsStore.commentaires[projetId] || {}).length"
					:fetch-function="(offset, limit) => projetsStore.getCommentaireByInterval(projetId, limit, offset)"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('projet.VProjetLoading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="projetDeleteModalShow" @close-modal="projetDeleteModalShow = false"
		:delete-action="projetDelete" :text-title="'projet.VProjetDeleteTitle'"
		:text-p="'projet.VProjetDeleteText'"/>

	<ModalAddFile :show-modal="documentAddModalShow" @close-modal="documentAddModalShow = false"
		:text-title="'projet.VProjetDocumentAddTitle'" :schema-add="schemaAddDocument"
		:modal-data="documentModalData" :add-action="documentAdd" :key-name-document="'name_projet_document'" :key-file-document="'document'"
		:max-size-in-mb="configsStore.getConfigByKey('max_size_document_in_mb')"
		:text-max-size="'projet.VProjetDocumentSize'" :text-placeholder-document="'projet.VProjetDocumentNamePlaceholder'"
	/>

	<ModalDeleteConfirm :show-modal="documentDeleteModalShow" @close-modal="documentDeleteModalShow = false"
		:delete-action="documentDelete" :text-title="'projet.VProjetDocumentDeleteTitle'"
		:text-p="'projet.VProjetDocumentDeleteText'"/>

	<div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
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
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

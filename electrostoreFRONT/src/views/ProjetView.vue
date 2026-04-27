<script setup>
import { onMounted, onBeforeUnmount, computed, ref, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const projetId = ref(route.params.id);
const preset = ref(route.query.preset || null);

import { downloadFile, viewFile } from "@/utils";

import { ProjetStatus } from "@/enums";

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
		if (preset.value) {
			preset.value.split(";").forEach((pair) => {
				const [key, value] = pair.split(":");
				if (key && value) {
					projetsStore.projetEdition[key] = value;
				}
			});
		}
	} else {
		projetsStore.projetEdition = {
			loading: true,
		};
		try {
			await projetsStore.getProjetById(projetId.value);
		} catch {
			delete projetsStore.projets[projetId.value];
			addNotification({ message: t("projet.NotFound"), type: "error" });
			router.push("/projets");
			return;
		}
		projetsStore.getProjetTagProjetByInterval(projetId.value, 100, 0, ["projet_tag"]);
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
	{ key: "nom_projet_tag", value: "", type: "text", label: "", placeholder: t("projet.TagFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);
function tagSave(id_tag) {
	try {
		projetsStore.createProjetTagProjet(projetId.value,  { id_projet_tag: id_tag });
		addNotification({ message: t("projet.TagAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function tagDelete(id_tag) {
	try {
		projetsStore.deleteProjetTagProjet(projetId.value, id_tag);
		addNotification({ message: t("projet.TagDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

// projet
const projetDeleteModalShow = ref(false);
const projetTypeStatus = ref({ [ProjetStatus.NotStarted]: t("projet.Status0"), [ProjetStatus.InProgress]: t("projet.Status1"),
	[ProjetStatus.Completed]: t("projet.Status2"), [ProjetStatus.OnHold]: t("projet.Status3"),
	[ProjetStatus.Cancelled]: t("projet.Status4"), [ProjetStatus.Archived]: t("projet.Status5") });
const projetSave = async() => {
	try {
		createSchema().validateSync(projetsStore.projetEdition, { abortEarly: false });
		if (projetId.value === "new") {
			const newId = await projetsStore.createProjet({ ...projetsStore.projetEdition });
			addNotification({ message: t("projet.Created"), type: "success" });
			projetId.value = String(newId);
			router.push("/projets/" + projetId.value);
		} else {
			await projetsStore.updateProjet(projetId.value, { ...projetsStore.projetEdition });
			addNotification({ message: t("projet.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const projetDelete = async() => {
	try {
		await projetsStore.deleteProjet(projetId.value);
		addNotification({ message: t("projet.Deleted"), type: "success" });
		router.push("/projets");
	} catch (e) {
		addNotification({ message: e, type: "error" });
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
		addNotification({ message: t("projet.DocumentAdded"), type: "success" });
		documentAddModalShow.value = false;
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const documentEdit = async(row) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		await projetsStore.updateDocument(projetId.value, row.id_projet_document, row);
		addNotification({ message: t("projet.DocumentUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const documentDelete = async() => {
	try {
		await projetsStore.deleteDocument(projetId.value, documentModalData.value.id_projet_document);
		addNotification({ message: t("projet.DocumentDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
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
		addNotification({ message: t("projet.DocumentOpenInNewTab"), type: "success" });
	} else {
		addNotification({ message: t("projet.DocumentNotSupported"), type: "error" });
	}
};

// item
const itemModalShow = ref(false);
const itemSave = async(item) => {
	if (projetsStore.items[projetId.value][item.id_item]) {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await projetsStore.updateItem(projetId.value, item.tmp.id_item, item.tmp);
			addNotification({ message: t("projet.ItemUpdated"), type: "success" });
			item.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error" });
			return;
		}
	} else {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await projetsStore.createItem(projetId.value, item.tmp);
			addNotification({ message: t("projet.ItemAdded"), type: "success" });
			item.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error" });
			return;
		}
	}
};
const itemDelete = async(item) => {
	try {
		await projetsStore.deleteItem(projetId.value, item.id_item);
		addNotification({ message: t("projet.ItemDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const filterItem = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "", placeholder: t("command.ItemFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

const createSchema = () => {
	return Yup.object().shape({
		nom_projet: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("projet.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
			.required(t("projet.NameRequired")),
		description_projet: Yup.string()
			.max(configsStore.getConfigByKey("max_length_description"), t("projet.DescriptionMaxLength", { count: configsStore.getConfigByKey("max_length_description") }))
			.required(t("projet.DescriptionRequired")),
		url_projet: Yup.string()
			.max(configsStore.getConfigByKey("max_length_url"), t("projet.UrlMaxLength", { count: configsStore.getConfigByKey("max_length_url") }))
			.url(t("projet.UrlInvalid"))
			.required(t("projet.UrlRequired")),
		status_projet: Yup.number()
			.required(t("projet.StatusRequired")),
	});
};

const schemaAddDocument = Yup.object().shape({
	name_projet_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("projet.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("projet.DocumentNameRequired")),
	document: Yup.mixed()
		.required(t("projet.DocumentRequired"))
		.test("fileSize", t("projet.DocumentSize", { count: configsStore.getConfigByKey("max_size_document_in_mb") }), (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});
const schemaEditDocument = Yup.object().shape({
	name_projet_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("projet.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("projet.DocumentNameRequired")),
});

const schemaItem = Yup.object().shape({
	qte_projet_item: Yup.number()
		.min(1, t("projet.ItemQuantityMin"))
		.typeError(t("projet.ItemQuantityType"))
		.required(t("projet.ItemQuantityRequired")),
});

const labelForm = ref([
	{ key: "nom_projet", label: "projet.Name", type: "text" },
	{ key: "description_projet", label: "projet.Description", type: "textarea", rows: 4 },
	{ key: "url_projet", label: "projet.Url", type: "text" },
	{ key: "status_projet", label: "projet.Status", type: "select", options: projetTypeStatus },
	{ key: "date_debut_projet", label: "projet.StartDate", type: "computed", value: dateDebut },
	{ key: "date_fin_projet", label: "projet.EndDate", type: "computed", value: dateFin },
]);
const labelTableauHistoryStatus = ref([
	{ label: "projet.StatusType", sortable: false, key: "status_projet", valueKey: "status_projet", type: "enum", options: projetTypeStatus },
	{ label: "projet.StatusDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
]);
const labelTableauDocument = ref([
	{ label: "projet.DocumentName", sortable: true, key: "name_projet_document", valueKey: "name_projet_document", type: "text", canEdit: true },
	{ label: "projet.DocumentType", sortable: true, key: "type_projet_document", valueKey: "type_projet_document", type: "text" },
	{ label: "projet.DocumentDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "projet.DocumentActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!rowData.tmp",
			action: (row) => {
				row.tmp = { ...row };
			},
			class: "text-blue-500 cursor-pointer hover:text-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "rowData.tmp",
			action: (row) => {
				row.tmp = null;
			},
			class: "text-gray-500 cursor-pointer hover:text-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "rowData.tmp",
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
	{ label: "projet.ItemName", sortable: true, key: "Item.reference_name_item", sourceKey: "id_item", type: "text", 
		storeRessourceId: 1, valueKey: "reference_name_item" },

	{ label: "projet.ItemQuantity", sortable: true, key: "qte_projet_item", valueKey: "qte_projet_item", type: "number", canEdit: true },
	{ label: "projet.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!rowData.tmp",
			action: (row) => {
				row.tmp = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "rowData.tmp",
			action: (row) => itemSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "rowData.tmp",
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
	{ label: "projet.TagName", sortable: true, key: "nom_projet_tag", valueKey: "nom_projet_tag", type: "text" },
	{ label: "projet.TagActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "!store[1]?.[rowData.id_projet_tag]",
			action: (row) => tagSave(row.id_projet_tag),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "store[1]?.[rowData.id_projet_tag]",
			action: (row) => tagDelete(row.id_projet_tag),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauModalItem = ref([
	{ label: "projet.ItemName", sortable: true, key: "reference_name_item", valueKey: "reference_name_item", type: "text" },
	
	{ label: "projet.ItemQuantity", sortable: true, key: "ProjetsItems.qte_projet_item", sourceKey: "id_projet", type: "number", 
		storeRessourceId: 1, valueKey: "qte_projet_item", canEdit: true },

	{ label: "projet.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "store[1]?.[rowData.id_item] === undefined && !rowData.tmp",
			action: (row) => {
				row.tmp = { qte_projet_item: 1, id_item: row.id_item };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "store[1]?.[rowData.id_item] && !rowData.tmp",
			action: (row) => {
				row.tmp = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "rowData.tmp",
			action: (row) => itemSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "rowData.tmp",
			action: (row) => {
				row.tmp = null;
			},
			class: "px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "store[1]?.[rowData.id_item]",
			action: (row) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('projet.Title') }}</h2>
		<RouterLink to="/projet-tags"
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block">
			{{ $t('projet.ListTag') }}
		</RouterLink>
		<TopButtonEditElement
			:main-config="{ path: '/projets',
				create: { showCondition: projetId === 'new' && authStore.hasPermission([0, 1, 2]), loading: projetsStore.projetEdition?.loading },
				update: { showCondition: projetId !== 'new' && authStore.hasPermission([0, 1, 2]), loading: projetsStore.projetEdition?.loading },
				delete: { showCondition: projetId !== 'new' && authStore.hasPermission([0, 1, 2]) }
			}"
			@button-create="projetSave" @button-update="projetSave" @button-delete="projetDeleteModalShow = true"/>
	</div>
	<div v-if="projetsStore.projets[projetId] || projetId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="projetsStore.projetEdition"/>
			<Tags :current-tags="projetsStore.projetTagProjet[projetId] || {}" :tags-store="projetTagsStore.projetTags" :can-edit="projetId !== 'new' && authStore.hasPermission([2])"
				:delete-function="(value) => tagDelete(value)"
				:filter-modal="filterTag"
				:tableau-modal="{ 'label': labelTableauModalTag, 'meta': { key: 'id_projet_tag', preventClear: true }, 'css': { component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }
								, 'loading': projetTagsStore.projetTagsLoading, 'fetchFunction': (limit, offset, expand, filter, sort, clear) => projetTagsStore.getProjetTagByInterval(limit, offset, expand, filter, sort, clear)
								, 'totalCount': Number(projetTagsStore.projetTagsTotalCount || 0) }"
				:meta ="{ 'keyPoids': 'poids_projet_tag', 'keyName': 'nom_projet_tag' }"
				/>
		</div>
		<CollapsibleSection title="projet.HistoryStatus"
			:total-count="Number(projetsStore.statusHistoryTotalCount[projetId] || 0)" :permission="projetId !=='new'">
			<template #append-row>
				<Tableau :labels="labelTableauHistoryStatus" :meta="{ key: 'id_projet_status' }"
					:store-data="[projetsStore.statusHistory[projetId]]"
					:loading="projetsStore.statusHistoryLoading"
					:total-count="Number(projetsStore.statusHistoryTotalCount[projetId])"
					:fetch-function="projetId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projetsStore.getStatusHistoryByInterval(projetId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="projet.Documents"
			:total-count="Number(projetsStore.documentsTotalCount[projetId] || 0)" :permission="projetId !=='new'">
			<template #append-row>
				<button type="button" @click="documentAddOpenModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projet.AddDocument') }}
				</button>
				<Tableau :labels="labelTableauDocument" :meta="{ key: 'id_projet_document' }"
					:store-data="[projetsStore.documents[projetId]]"
					:loading="projetsStore.documentsLoading"
					:total-count="Number(projetsStore.documentsTotalCount[projetId])"
					:fetch-function="projetId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projetsStore.getDocumentByInterval(projetId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="projet.Items"
			:total-count="Number(projetsStore.itemsTotalCount[projetId] || 0)" :permission="projetId !=='new'">
			<template #append-row>
				<button type="button" @click="itemModalShow = true"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projet.AddItem') }}
				</button>
				<Tableau :labels="labelTableauItem" :meta="{ key: 'id_item', expand: ['item'] }"
					:store-data="[projetsStore.items[projetId], itemsStore.items]"
					:loading="projetsStore.itemsLoading" :schema="schemaItem"
					:total-count="Number(projetsStore.itemsTotalCount[projetId] || 0)"
					:fetch-function="projetId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projetsStore.getItemByInterval(projetId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="projet.Commentaires"
			:total-count="Number(projetsStore.commentairesTotalCount[projetId] || 0)" :permission="projetId !=='new'">
			<template #append-row>
				<Commentaire :meta="{ contenu: 'contenu_projet_commentaire', key: 'id_projet_commentaire', canEdit: true, roleRequired: authStore.hasPermission([1, 2]), expand: ['user'] }"
					:store-data="[projetsStore.commentaires[projetId], usersStore.users]"
					:store-user="authStore.user" :store-config="configsStore"
					:store-function="{ create: (data) => projetsStore.createCommentaire(projetId, data), update: (id, data) => projetsStore.updateCommentaire(projetId, id, data), delete: (id) => projetsStore.deleteCommentaire(projetId, id) }"
					:loading="projetsStore.commentairesLoading" :texte-modal-delete="{ textTitle: 'projet.CommentDeleteTitle', textP: 'projet.CommentDeleteText' }"
					:total-count="Number(projetsStore.commentairesTotalCount[projetId])"
					:fetch-function="projetId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projetsStore.getCommentaireByInterval(projetId, limit, offset, expand, filter, sort, clear) : undefined"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('projet.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="projetDeleteModalShow" @close-modal="projetDeleteModalShow = false"
		:delete-action="projetDelete" :text-title="'projet.DeleteTitle'"
		:text-p="'projet.DeleteText'"/>

	<ModalAddFile :show-modal="documentAddModalShow" @close-modal="documentAddModalShow = false"
		:text-title="'projet.DocumentAddTitle'" :schema-add="schemaAddDocument"
		:modal-data="documentModalData" :add-action="documentAdd" :key-name-document="'name_projet_document'" :key-file-document="'document'"
		:max-size-in-mb="configsStore.getConfigByKey('max_size_document_in_mb')"
		:text-max-size="'projet.DocumentSize'" :text-placeholder-document="'projet.DocumentNamePlaceholder'"
		file-type="document"
	/>

	<ModalDeleteConfirm :show-modal="documentDeleteModalShow" @close-modal="documentDeleteModalShow = false"
		:delete-action="documentDelete" :text-title="'projet.DocumentDeleteTitle'"
		:text-p="'projet.DocumentDeleteText'"/>

	<div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('projet.ItemTitle') }}</h2>
				<button type="button" @click="itemModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<FilterContainer class="my-4 flex gap-4" :filters="filterItem" :store-data="itemsStore.items" />

			<Tableau :labels="labelTableauModalItem" :meta="{ key: 'id_item' }"
				:store-data="[itemsStore.items, projetsStore.items[projetId]]"
				:filters="filterItem"
				:loading="projetsStore.itemsLoading" :schema="schemaItem"
				:total-count="Number(itemsStore.itemsTotalCount || 0)"
				:fetch-function="projetId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

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

const formContainer = ref(null);

async function fetchAllData() {
	if (projetId.value === "new") {
		projetsStore.loadToEdition(projetId.value, preset.value);
	} else {
		projetsStore.setLoadingEdition(projetId.value, true);
		try {
			await projetsStore.getProjetById(projetId.value);
		} catch {
			delete projetsStore.projets[projetId.value];
			addNotification({ message: t("projet.NotFound"), type: "error" });
			router.push("/projets");
			return;
		}
		projetsStore.getProjetTagProjetByInterval(projetId.value, 100, 0, ["projet_tag"]);
		projetsStore.loadToEdition(projetId.value);
		usersStore.users[authStore.user.id_user] = authStore.user; // avoids undefined user when the current user posts first comment
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	projetsStore.clearEdition(projetId.value);
});
const dateDebut = computed(() => {
	// don't return the GMT offset to avoid timezone issues
	return projetsStore.projetEdition[projetId.value].date_start_project ? new Date(projetsStore.projetEdition[projetId.value].date_start_project).toISOString().replace(/.\d+Z$/, "").replace("T", " ") : null;
});
const dateFin = computed(() => {
	return projetsStore.projetEdition[projetId.value].date_end_project ? new Date(projetsStore.projetEdition[projetId.value].date_end_project).toISOString().replace(/.\d+Z$/, "").replace("T", " ") : null;
});

// tag
const filterTag = ref([
	{ key: "name_project_tag", value: "", type: "text", label: "", placeholder: t("projet.TagFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);
function tagSave(id_tag) {
	try {
		projetsStore.createProjetTagProjet(projetId.value,  { id_project_tag: id_tag });
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

// roadmap
const projetRoadmapSteps = [
	{ id: ProjetStatus.NotStarted, name: "NotStarted" },
	{ id: ProjetStatus.InProgress, name: "InProgress" },
	{ id: ProjetStatus.Completed, name: "Completed" },
	{ id: ProjetStatus.OnHold, name: "OnHold" },
	{ id: ProjetStatus.Cancelled, name: "Cancelled" },
	{ id: ProjetStatus.Archived, name: "Archived" },
];
const projetCurrentStep = computed(() => {
	const status = projetsStore.projetEdition[projetId.value]?.status_project;
	if (status === null || status === undefined) {
		return 0;
	}
	const idx = projetRoadmapSteps.findIndex((s) => s.id === Number(status));
	return idx >= 0 ? idx : 0;
});
const projetSave = async() => {
	try {
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("projet.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			projetsStore.setLoadingEdition(projetId.value, false);
			return;
		}
		if (projetId.value === "new") {
			const newId = await projetsStore.createProjet({ ...projetsStore.projetEdition[projetId.value] });
			projetsStore.loadToEdition(newId);
			addNotification({ message: t("projet.Created"), type: "success" });
			projetId.value = String(newId);
			router.push("/projets/" + projetId.value);
		} else {
			await projetsStore.updateProjet(projetId.value, { ...projetsStore.projetEdition[projetId.value] });
			projetsStore.loadToEdition(projetId.value);
			addNotification({ message: t("projet.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		projetsStore.setLoadingEdition(projetId.value, false);
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
const documentModalData = ref({ id_project_document: null, name_project_document: "", document: null });
const documentDeleteOpenModal = (doc) => {
	documentModalData.value = doc;
	documentDeleteModalShow.value = true;
};
const documentAdd = async(files) => {
	for (const file of files) {
		documentModalData.value = { name_project_document: file.name, document: file.document };
		try {
			schemaAddDocument.validateSync(documentModalData.value, { abortEarly: false });
			const formData = new FormData();
			formData.append("name_project_document", documentModalData.value.name_project_document);
			formData.append("document", documentModalData.value.document);
			await projetsStore.createDocument(projetId.value, formData);
			addNotification({ message: t("projet.DocumentAdded"), type: "success" });
		} catch (e) {
			addNotification({ message: e, type: "error" });
		}
	}
	documentAddModalShow.value = false;
};
const documentEdit = async(row) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		await projetsStore.updateDocument(projetId.value, row.id_project_document, row);
		delete projetsStore.documentEdition[row.id_project_document];
		addNotification({ message: t("projet.DocumentUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const documentDelete = async() => {
	try {
		await projetsStore.deleteDocument(projetId.value, documentModalData.value.id_project_document);
		addNotification({ message: t("projet.DocumentDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	documentDeleteModalShow.value = false;
};
const documentDownload = async(fileContent) => {
	const file = await projetsStore.downloadDocument(projetId.value, fileContent.id_project_document);
	downloadFile(file, { keyName: fileContent.name_project_document, keyType: fileContent.type_project_document });
};
const documentView = async(fileContent) => {
	const file = await projetsStore.downloadDocument(projetId.value, fileContent.id_project_document);
	if (viewFile(file, { keyName: fileContent.name_project_document, keyType: fileContent.type_project_document })) {
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
	const edition = projetsStore.projetEdition[projetId.value];
	const shape = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.name_project = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("projet.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("projet.NameRequired"));
	shape.description_project = Yup.string()
		.nullable()
		.optional()
		.max(configsStore.getConfigByKey("max_length_description"), t("projet.DescriptionMaxLength", { count: configsStore.getConfigByKey("max_length_description") }));
	shape.url_project = Yup.string()
		.nullable()
		.optional()
		.max(configsStore.getConfigByKey("max_length_url"), t("projet.UrlMaxLength", { count: configsStore.getConfigByKey("max_length_url") }))
		.url(t("projet.UrlInvalid"));
	shape.status_project = Yup.number()
		.required(t("projet.StatusRequired"));
	return Yup.object().shape(shape);
};

const schemaAddDocument = Yup.object().shape({
	name_project_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("projet.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("projet.DocumentNameRequired")),
	document: Yup.mixed()
		.required(t("projet.DocumentRequired"))
		.test("fileSize", t("projet.DocumentSize", { count: configsStore.getConfigByKey("max_size_document_in_mb") }), (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});
const schemaEditDocument = Yup.object().shape({
	name_project_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("projet.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("projet.DocumentNameRequired")),
});

const schemaItem = Yup.object().shape({
	quantity_project_item: Yup.number()
		.min(1, t("projet.ItemQuantityMin"))
		.typeError(t("projet.ItemQuantityType"))
		.required(t("projet.ItemQuantityRequired")),
});

const labelForm = ref([
	{ key: "name_project", label: "projet.Name", type: "text" },
	{ key: "description_project", label: "projet.Description", type: "textarea", rows: 4 },
	{ key: "url_project", label: "projet.Url", type: "text" },
	{ key: "status_project", label: "projet.Status", type: "select", typeData: "number", options: projetTypeStatus },
	{ key: "date_start_project", label: "projet.StartDate", type: "computed", value: dateDebut },
	{ key: "date_end_project", label: "projet.EndDate", type: "computed", value: dateFin },
]);
const labelTableauHistoryStatus = ref([
	{ label: "projet.StatusType", sortable: false, key: "status_project", valueKey: "status_project", type: "enum", options: projetTypeStatus },
	{ label: "projet.StatusDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
]);
const labelTableauDocument = ref([
	{ label: "projet.DocumentName", sortable: true, key: "name_project_document", valueKey: "name_project_document", type: "text", canEdit: true },
	{ label: "projet.DocumentType", sortable: true, key: "type_project_document", valueKey: "type_project_document", type: "text" },
	{ label: "projet.DocumentDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "projet.DocumentActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_project_document",
			action: (row) => {
				projetsStore.documentEdition[row.id_project_document] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_project_document",
			action: (row) => {
				delete projetsStore.documentEdition[row.id_project_document];
			},
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_project_document",
			action: (row) => documentEdit(projetsStore.documentEdition[row.id_project_document]),
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
const labelTableauItem = ref([
	{ label: "projet.ItemName", sortable: true, key: "Item.reference_name_item", sourceKey: "id_item", type: "text", 
		storeRessourceId: 1, valueKey: "reference_name_item" },

	{ label: "projet.ItemQuantity", sortable: true, key: "quantity_project_item", valueKey: "quantity_project_item", type: "number", canEdit: true },
	{ label: "projet.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_item",
			action: (row) => {
				projetsStore.itemEdition[row.id_item] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_item",
			action: (row) => itemSave(projetsStore.itemEdition[row.id_item]),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_item",
			action: (row) => {
				delete projetsStore.itemEdition[row.id_item];
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
	{ label: "projet.TagName", sortable: true, key: "name_project_tag", valueKey: "name_project_tag", type: "text" },
	{ label: "projet.TagActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "!store[1]?.[rowData.id_project_tag]",
			action: (row) => tagSave(row.id_project_tag),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "store[1]?.[rowData.id_project_tag]",
			action: (row) => tagDelete(row.id_project_tag),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauModalItem = ref([
	{ label: "projet.ItemName", sortable: true, key: "reference_name_item", valueKey: "reference_name_item", type: "text" },
	
	{ label: "projet.ItemQuantity", sortable: true, key: "ProjetsItems.quantity_project_item", sourceKey: "id_project", type: "number", 
		storeRessourceId: 1, valueKey: "quantity_project_item", canEdit: true },

	{ label: "projet.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "store[1]?.[rowData.id_item] === undefined && !edition?.id_item",
			action: (row) => {
				projetsStore.itemEdition[row.id_item] = { quantity_project_item: 1, id_item: row.id_item };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "store[1]?.[rowData.id_item] && !edition?.id_item",
			action: (row) => {
				projetsStore.itemEdition[row.id_item] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_item",
			action: (row) => itemSave(projetsStore.itemEdition[row.id_item]),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_item",
			action: (row) => {
				delete projetsStore.itemEdition[row.id_item];
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
				create: { showCondition: projetId === 'new' && authStore.hasPermission([0, 1, 2]), loading: projetsStore.projetEdition[projetId]?.loading },
				update: { showCondition: projetId !== 'new' && authStore.hasPermission([0, 1, 2]), loading: projetsStore.projetEdition[projetId]?.loading },
				delete: { showCondition: projetId !== 'new' && authStore.hasPermission([0, 1, 2]) }
			}"
			@button-create="projetSave" @button-update="projetSave" @button-delete="projetDeleteModalShow = true"/>
	</div>
	<div v-if="projetsStore.projets[projetId] || projetId == 'new'" class="w-full">
		<RoadMap v-if="projetId !== 'new'"
			:steps="projetRoadmapSteps"
			:current-step="projetCurrentStep"
			mode="horizontal-bottom"
		/>
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="projetsStore.projetEdition[projetId]"/>
			<Tags :current-tags="projetsStore.projetTagProjet[projetId] || {}" :tags-store="projetTagsStore.projetTags" :can-edit="projetId !== 'new' && authStore.hasPermission([2])"
				:delete-function="(value) => tagDelete(value)"
				:filter-modal="filterTag"
				:tableau-modal="{ 'label': labelTableauModalTag, 'meta': { key: 'id_project_tag', preventClear: true }, 'css': { component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }
								, 'loading': projetTagsStore.projetTagsLoading, 'fetchFunction': (limit, offset, expand, filter, sort, clear) => projetTagsStore.getProjetTagByInterval(limit, offset, expand, filter, sort, clear)
								, 'totalCount': Number(projetTagsStore.projetTagsTotalCount || 0) }"
				:meta ="{ 'keyPoids': 'weight_project_tag', 'keyName': 'name_project_tag' }"
				/>
		</div>
		<CollapsibleSection title="projet.HistoryStatus"
			:total-count="Number(projetsStore.statusHistoryTotalCount[projetId] || 0)" :permission="projetId !=='new'">
			<template #append-row>
				<Tableau :labels="labelTableauHistoryStatus" :meta="{ key: 'id_project_status' }"
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
				<button type="button" @click="documentAddModalShow = true"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projet.AddDocument') }}
				</button>
				<Tableau :labels="labelTableauDocument" :meta="{ key: 'id_project_document' }"
					:store-data="[projetsStore.documents[projetId]]"
					:store-edition="projetsStore.documentEdition"
					:schema="schemaEditDocument"
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
					:store-edition="projetsStore.itemEdition"
					:loading="projetsStore.itemsLoading"
					:schema="schemaItem"
					:total-count="Number(projetsStore.itemsTotalCount[projetId] || 0)"
					:fetch-function="projetId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projetsStore.getItemByInterval(projetId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="projet.Commentaires"
			:total-count="Number(projetsStore.commentairesTotalCount[projetId] || 0)" :permission="projetId !=='new'">
			<template #append-row>
				<Commentaire :meta="{ contenu: 'content_project_comment', key: 'id_project_comment', canEdit: true, roleRequired: authStore.hasPermission([1, 2]), expand: ['user'] }"
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

	<ModalMultipleFiles
		:show-modal="documentAddModalShow"
		@close-modal="documentAddModalShow = false"
		@files-saved="documentAdd"
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
				:store-edition="projetsStore.itemEdition"
				:filters="filterItem"
				:loading="projetsStore.itemsLoading" :schema="schemaItem"
				:total-count="Number(itemsStore.itemsTotalCount || 0)"
				:fetch-function="projetId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

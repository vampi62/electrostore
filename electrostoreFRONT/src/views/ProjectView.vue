<script setup>
import { onMounted, onBeforeUnmount, computed, ref, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const projectId = ref(route.params.id);
const preset = ref(route.query.preset || null);

import { downloadFile, viewFile } from "@/utils";

import { ProjectStatus } from "@/enums";

import { useConfigsStore, useProjectsStore, useUsersStore, useItemsStore, useProjectTagsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const projectsStore = useProjectsStore();
const usersStore = useUsersStore();
const itemsStore = useItemsStore();
const projectTagsStore = useProjectTagsStore();
const authStore = useAuthStore();

const formContainer = ref(null);

async function fetchAllData() {
	if (projectId.value === "new") {
		projectsStore.loadToEdition(projectId.value, preset.value);
	} else {
		projectsStore.setLoadingEdition(projectId.value, true);
		try {
			await projectsStore.getProjectById(projectId.value);
		} catch {
			delete projectsStore.projects[projectId.value];
			addNotification({ message: t("project.NotFound"), type: "error" });
			router.push("/projects");
			return;
		}
		projectsStore.getProjectTagProjectByInterval(projectId.value, 100, 0, ["project_tag"]);
		projectsStore.loadToEdition(projectId.value);
		usersStore.users[authStore.user.id_user] = authStore.user; // avoids undefined user when the current user posts first comment
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	projectsStore.clearEdition(projectId.value);
});
const dateDebut = computed(() => {
	// don't return the GMT offset to avoid timezone issues
	return projectsStore.projectEdition[projectId.value].date_start_project ? new Date(projectsStore.projectEdition[projectId.value].date_start_project).toISOString().replace(/.\d+Z$/, "").replace("T", " ") : null;
});
const dateFin = computed(() => {
	return projectsStore.projectEdition[projectId.value].date_end_project ? new Date(projectsStore.projectEdition[projectId.value].date_end_project).toISOString().replace(/.\d+Z$/, "").replace("T", " ") : null;
});

// tag
const filterTag = ref([
	{ key: "name_project_tag", value: "", type: "text", label: "", placeholder: t("project.TagFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);
function tagSave(id_tag) {
	try {
		projectsStore.createProjectTagProject(projectId.value,  { id_project_tag: id_tag });
		addNotification({ message: t("project.TagAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function tagDelete(id_tag) {
	try {
		projectsStore.deleteProjectTagProject(projectId.value, id_tag);
		addNotification({ message: t("project.TagDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

// project
const projectDeleteModalShow = ref(false);
const projectTypeStatus = ref({ [ProjectStatus.NotStarted]: t("project.Status0"), [ProjectStatus.InProgress]: t("project.Status1"),
	[ProjectStatus.Completed]: t("project.Status2"), [ProjectStatus.OnHold]: t("project.Status3"),
	[ProjectStatus.Cancelled]: t("project.Status4"), [ProjectStatus.Archived]: t("project.Status5") });

// roadmap
const projectRoadmapSteps = [
	{ id: ProjectStatus.NotStarted, name: "NotStarted" },
	{ id: ProjectStatus.InProgress, name: "InProgress" },
	{ id: ProjectStatus.Completed, name: "Completed" },
	{ id: ProjectStatus.OnHold, name: "OnHold" },
	{ id: ProjectStatus.Cancelled, name: "Cancelled" },
	{ id: ProjectStatus.Archived, name: "Archived" },
];
const projectCurrentStep = computed(() => {
	const status = projectsStore.projectEdition[projectId.value]?.status_project;
	if (status === null || status === undefined) {
		return 0;
	}
	const idx = projectRoadmapSteps.findIndex((s) => s.id === Number(status));
	return idx >= 0 ? idx : 0;
});
const projectSave = async() => {
	try {
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("project.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			projectsStore.setLoadingEdition(projectId.value, false);
			return;
		}
		if (projectId.value === "new") {
			const newId = await projectsStore.createProject({ ...projectsStore.projectEdition[projectId.value] });
			projectsStore.loadToEdition(newId);
			addNotification({ message: t("project.Created"), type: "success" });
			projectId.value = String(newId);
			router.push("/projects/" + projectId.value);
		} else {
			await projectsStore.updateProject(projectId.value, { ...projectsStore.projectEdition[projectId.value] });
			projectsStore.loadToEdition(projectId.value);
			addNotification({ message: t("project.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		projectsStore.setLoadingEdition(projectId.value, false);
	}
};
const projectDelete = async() => {
	try {
		await projectsStore.deleteProject(projectId.value);
		addNotification({ message: t("project.Deleted"), type: "success" });
		router.push("/projects");
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	projectDeleteModalShow.value = false;
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
			await projectsStore.createDocument(projectId.value, formData);
			addNotification({ message: t("project.DocumentAdded"), type: "success" });
		} catch (e) {
			addNotification({ message: e, type: "error" });
		}
	}
	documentAddModalShow.value = false;
};
const documentEdit = async(row) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		await projectsStore.updateDocument(projectId.value, row.id_project_document, row);
		delete projectsStore.documentEdition[row.id_project_document];
		addNotification({ message: t("project.DocumentUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const documentDelete = async() => {
	try {
		await projectsStore.deleteDocument(projectId.value, documentModalData.value.id_project_document);
		addNotification({ message: t("project.DocumentDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	documentDeleteModalShow.value = false;
};
const documentDownload = async(fileContent) => {
	const file = await projectsStore.downloadDocument(projectId.value, fileContent.id_project_document);
	downloadFile(file, { keyName: fileContent.name_project_document, keyType: fileContent.type_project_document });
};
const documentView = async(fileContent) => {
	const file = await projectsStore.downloadDocument(projectId.value, fileContent.id_project_document);
	if (viewFile(file, { keyName: fileContent.name_project_document, keyType: fileContent.type_project_document })) {
		addNotification({ message: t("project.DocumentOpenInNewTab"), type: "success" });
	} else {
		addNotification({ message: t("project.DocumentNotSupported"), type: "error" });
	}
};

// item
const itemModalShow = ref(false);
const itemSave = async(item) => {
	if (projectsStore.items[projectId.value][item.id_item]) {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await projectsStore.updateItem(projectId.value, item.tmp.id_item, item.tmp);
			addNotification({ message: t("project.ItemUpdated"), type: "success" });
			item.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error" });
			return;
		}
	} else {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await projectsStore.createItem(projectId.value, item.tmp);
			addNotification({ message: t("project.ItemAdded"), type: "success" });
			item.tmp = null;
		} catch (e) {
			addNotification({ message: e, type: "error" });
			return;
		}
	}
};
const itemDelete = async(item) => {
	try {
		await projectsStore.deleteItem(projectId.value, item.id_item);
		addNotification({ message: t("project.ItemDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const filterItem = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "", placeholder: t("command.ItemFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

const createSchema = () => {
	const edition = projectsStore.projectEdition[projectId.value];
	const shape = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.name_project = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("project.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("project.NameRequired"));
	shape.description_project = Yup.string()
		.nullable()
		.optional()
		.max(configsStore.getConfigByKey("max_length_description"), t("project.DescriptionMaxLength", { count: configsStore.getConfigByKey("max_length_description") }));
	shape.url_project = Yup.string()
		.nullable()
		.optional()
		.max(configsStore.getConfigByKey("max_length_url"), t("project.UrlMaxLength", { count: configsStore.getConfigByKey("max_length_url") }))
		.url(t("project.UrlInvalid"));
	shape.status_project = Yup.number()
		.required(t("project.StatusRequired"));
	return Yup.object().shape(shape);
};

const schemaAddDocument = Yup.object().shape({
	name_project_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("project.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("project.DocumentNameRequired")),
	document: Yup.mixed()
		.required(t("project.DocumentRequired"))
		.test("fileSize", t("project.DocumentSize", { count: configsStore.getConfigByKey("max_size_document_in_mb") }), (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});
const schemaEditDocument = Yup.object().shape({
	name_project_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("project.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("project.DocumentNameRequired")),
});

const schemaItem = Yup.object().shape({
	quantity_project_item: Yup.number()
		.min(1, t("project.ItemQuantityMin"))
		.typeError(t("project.ItemQuantityType"))
		.required(t("project.ItemQuantityRequired")),
});

const labelForm = ref([
	{ key: "name_project", label: "project.Name", type: "text" },
	{ key: "description_project", label: "project.Description", type: "textarea", rows: 4 },
	{ key: "url_project", label: "project.Url", type: "text" },
	{ key: "status_project", label: "project.Status", type: "select", typeData: "number", options: projectTypeStatus },
	{ key: "date_start_project", label: "project.StartDate", type: "computed", value: dateDebut },
	{ key: "date_end_project", label: "project.EndDate", type: "computed", value: dateFin },
]);
const labelTableauHistoryStatus = ref([
	{ label: "project.StatusType", sortable: false, key: "status_project", valueKey: "status_project", type: "enum", options: projectTypeStatus },
	{ label: "project.StatusDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
]);
const labelTableauDocument = ref([
	{ label: "project.DocumentName", sortable: true, key: "name_project_document", valueKey: "name_project_document", type: "text", canEdit: true },
	{ label: "project.DocumentType", sortable: true, key: "type_project_document", valueKey: "type_project_document", type: "text" },
	{ label: "project.DocumentDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "project.DocumentActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_project_document",
			action: (row) => {
				projectsStore.documentEdition[row.id_project_document] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_project_document",
			action: (row) => {
				delete projectsStore.documentEdition[row.id_project_document];
			},
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_project_document",
			action: (row) => documentEdit(projectsStore.documentEdition[row.id_project_document]),
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
	{ label: "project.ItemName", sortable: true, key: "Item.reference_name_item", sourceKey: "id_item", type: "text", 
		storeRessourceId: 1, valueKey: "reference_name_item" },

	{ label: "project.ItemQuantity", sortable: true, key: "quantity_project_item", valueKey: "quantity_project_item", type: "number", canEdit: true },
	{ label: "project.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_item",
			action: (row) => {
				projectsStore.itemEdition[row.id_item] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_item",
			action: (row) => itemSave(projectsStore.itemEdition[row.id_item]),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_item",
			action: (row) => {
				delete projectsStore.itemEdition[row.id_item];
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
	{ label: "project.TagName", sortable: true, key: "name_project_tag", valueKey: "name_project_tag", type: "text" },
	{ label: "project.TagActions", sortable: false, key: "", type: "buttons", buttons: [
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
	{ label: "project.ItemName", sortable: true, key: "reference_name_item", valueKey: "reference_name_item", type: "text" },
	
	{ label: "project.ItemQuantity", sortable: true, key: "ProjectsItems.quantity_project_item", sourceKey: "id_project", type: "number", 
		storeRessourceId: 1, valueKey: "quantity_project_item", canEdit: true },

	{ label: "project.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "store[1]?.[rowData.id_item] === undefined && !edition?.id_item",
			action: (row) => {
				projectsStore.itemEdition[row.id_item] = { quantity_project_item: 1, id_item: row.id_item };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "store[1]?.[rowData.id_item] && !edition?.id_item",
			action: (row) => {
				projectsStore.itemEdition[row.id_item] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_item",
			action: (row) => itemSave(projectsStore.itemEdition[row.id_item]),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_item",
			action: (row) => {
				delete projectsStore.itemEdition[row.id_item];
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
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('project.Title') }}</h2>
		<RouterLink to="/project-tags"
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block">
			{{ $t('project.ListTag') }}
		</RouterLink>
		<TopButtonEditElement
			:main-config="{ path: '/projects',
				create: { showCondition: projectId === 'new' && authStore.hasPermission([0, 1, 2]), loading: projectsStore.projectEdition[projectId]?.loading },
				update: { showCondition: projectId !== 'new' && authStore.hasPermission([0, 1, 2]), loading: projectsStore.projectEdition[projectId]?.loading },
				delete: { showCondition: projectId !== 'new' && authStore.hasPermission([0, 1, 2]) }
			}"
			@button-create="projectSave" @button-update="projectSave" @button-delete="projectDeleteModalShow = true"/>
	</div>
	<div v-if="projectsStore.projects[projectId] || projectId == 'new'" class="w-full">
		<RoadMap v-if="projectId !== 'new'"
			:steps="projectRoadmapSteps"
			:current-step="projectCurrentStep"
			mode="horizontal-bottom"
		/>
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="projectsStore.projectEdition[projectId]"/>
			<Tags :current-tags="projectsStore.projectTagProject[projectId] || {}" :tags-store="projectTagsStore.projectTags" :can-edit="projectId !== 'new' && authStore.hasPermission([2])"
				:delete-function="(value) => tagDelete(value)"
				:filter-modal="filterTag"
				:tableau-modal="{ 'label': labelTableauModalTag, 'meta': { key: 'id_project_tag', preventClear: true }, 'css': { component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }
								, 'loading': projectTagsStore.projectTagsLoading, 'fetchFunction': (limit, offset, expand, filter, sort, clear) => projectTagsStore.getProjectTagByInterval(limit, offset, expand, filter, sort, clear)
								, 'totalCount': Number(projectTagsStore.projectTagsTotalCount || 0) }"
				:meta ="{ 'keyPoids': 'weight_project_tag', 'keyName': 'name_project_tag' }"
				/>
		</div>
		<CollapsibleSection title="project.HistoryStatus"
			:total-count="Number(projectsStore.statusHistoryTotalCount[projectId] || 0)" :permission="projectId !=='new'">
			<template #append-row>
				<Tableau :labels="labelTableauHistoryStatus" :meta="{ key: 'id_project_status' }"
					:store-data="[projectsStore.statusHistory[projectId]]"
					:loading="projectsStore.statusHistoryLoading"
					:total-count="Number(projectsStore.statusHistoryTotalCount[projectId])"
					:fetch-function="projectId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projectsStore.getStatusHistoryByInterval(projectId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="project.Documents"
			:total-count="Number(projectsStore.documentsTotalCount[projectId] || 0)" :permission="projectId !=='new'">
			<template #append-row>
				<button type="button" @click="documentAddModalShow = true"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('project.AddDocument') }}
				</button>
				<Tableau :labels="labelTableauDocument" :meta="{ key: 'id_project_document' }"
					:store-data="[projectsStore.documents[projectId]]"
					:store-edition="projectsStore.documentEdition"
					:schema="schemaEditDocument"
					:loading="projectsStore.documentsLoading"
					:total-count="Number(projectsStore.documentsTotalCount[projectId])"
					:fetch-function="projectId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projectsStore.getDocumentByInterval(projectId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="project.Items"
			:total-count="Number(projectsStore.itemsTotalCount[projectId] || 0)" :permission="projectId !=='new'">
			<template #append-row>
				<button type="button" @click="itemModalShow = true"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('project.AddItem') }}
				</button>
				<Tableau :labels="labelTableauItem" :meta="{ key: 'id_item', expand: ['item'] }"
					:store-data="[projectsStore.items[projectId], itemsStore.items]"
					:store-edition="projectsStore.itemEdition"
					:loading="projectsStore.itemsLoading"
					:schema="schemaItem"
					:total-count="Number(projectsStore.itemsTotalCount[projectId] || 0)"
					:fetch-function="projectId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projectsStore.getItemByInterval(projectId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="project.Comments"
			:total-count="Number(projectsStore.commentsTotalCount[projectId] || 0)" :permission="projectId !=='new'">
			<template #append-row>
				<Comment :meta="{ contenu: 'content_project_comment', key: 'id_project_comment', canEdit: true, roleRequired: authStore.hasPermission([1, 2]), expand: ['user'] }"
					:store-data="[projectsStore.comments[projectId], usersStore.users]"
					:store-user="authStore.user" :store-config="configsStore"
					:store-function="{ create: (data) => projectsStore.createComment(projectId, data), update: (id, data) => projectsStore.updateComment(projectId, id, data), delete: (id) => projectsStore.deleteComment(projectId, id) }"
					:loading="projectsStore.commentsLoading" :texte-modal-delete="{ textTitle: 'project.CommentDeleteTitle', textP: 'project.CommentDeleteText' }"
					:total-count="Number(projectsStore.commentsTotalCount[projectId])"
					:fetch-function="projectId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projectsStore.getCommentByInterval(projectId, limit, offset, expand, filter, sort, clear) : undefined"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('project.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="projectDeleteModalShow" @close-modal="projectDeleteModalShow = false"
		:delete-action="projectDelete" :text-title="'project.DeleteTitle'"
		:text-p="'project.DeleteText'"/>

	<ModalMultipleFiles
		:show-modal="documentAddModalShow"
		@close-modal="documentAddModalShow = false"
		@files-saved="documentAdd"
		file-type="document"
	/>

	<ModalDeleteConfirm :show-modal="documentDeleteModalShow" @close-modal="documentDeleteModalShow = false"
		:delete-action="documentDelete" :text-title="'project.DocumentDeleteTitle'"
		:text-p="'project.DocumentDeleteText'"/>

	<div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('project.ItemTitle') }}</h2>
				<button type="button" @click="itemModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<FilterContainer class="my-4 flex gap-4" :filters="filterItem" :store-data="itemsStore.items" />

			<Tableau :labels="labelTableauModalItem" :meta="{ key: 'id_item' }"
				:store-data="[itemsStore.items, projectsStore.items[projectId]]"
				:store-edition="projectsStore.itemEdition"
				:filters="filterItem"
				:loading="projectsStore.itemsLoading" :schema="schemaItem"
				:total-count="Number(itemsStore.itemsTotalCount || 0)"
				:fetch-function="projectId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

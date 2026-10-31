<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import * as Yup from "yup";

import { useViewScroll, useTabGuard } from "@/composables";
import type { useNotification } from "@/composables";
import type FormContainer from "@/components/FormContainer.vue";
import { isNewId } from "@/utils";
import { useConfigsStore, useProjectTagsStore, useProjectsTagsStore, useProjectsStore, useAuthStore } from "@/stores";

import type { FilterLabel } from "@/types/filter";
import type { TableauLabel, TableauRowData } from "@/types/tableau";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";

const { addNotification } = inject("useNotification") as ReturnType<typeof useNotification>;
const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const configsStore = useConfigsStore();
const projectTagsStore = useProjectTagsStore();
const projectsTagsStore = useProjectsTagsStore();
const projectsStore = useProjectsStore();
const authStore = useAuthStore();

const projectTagId = ref(route.params.id as string);
const preset = ref(typeof route.query.preset === "string" ? route.query.preset : null);

// every new element has its own edition space in the store, so several tabs can create an element at the same time
if (isNewId(projectTagId.value)) {
	projectTagId.value = projectTagsStore.getAvailableNewProjectTagId();
}

const formContainer = ref<InstanceType<typeof FormContainer> | null>(null);

async function fetchAllData() {
	if (isNewId(projectTagId.value)) {
		projectTagsStore.loadToEdition(projectTagId.value, preset.value);
	} else {
		projectTagsStore.setLoadingEdition(projectTagId.value, true);
		try {
			await projectTagsStore.getProjectTagById(projectTagId.value);
		} catch {
			delete projectTagsStore.projectTags[projectTagId.value];
			addNotification({ message: t("projectTag.NotFound"), type: "error" });
			router.push("/project-tags");
			return;
		}
		projectTagsStore.loadToEdition(projectTagId.value);
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	projectTagsStore.clearEdition(projectTagId.value);
});

const projectTagDeleteModalShow = ref(false);
const projectTagSave = async() => {
	try {
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("projectTag.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			projectTagsStore.setLoadingEdition(projectTagId.value, false);
			return;
		}
		const id = await projectTagsStore.saveAllChanges(projectTagId.value);
		projectTagsStore.loadToEdition(id);
		if (isNewId(projectTagId.value)) {
			addNotification({ message: t("projectTag.Created"), type: "success" });
			const tempId = projectTagId.value;
			projectTagId.value = String(id);
			projectTagsStore.clearEdition(tempId);
			router.push("/project-tags/" + projectTagId.value);
		} else {
			addNotification({ message: t("projectTag.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		projectTagsStore.setLoadingEdition(projectTagId.value, false);
	}
};
const projectTagDelete = async() => {
	try {
		await projectTagsStore.deleteProjectTag(projectTagId.value);
		addNotification({ message: t("projectTag.Deleted"), type: "success" });
		router.push("/project-tags");
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	projectTagDeleteModalShow.value = false;
};

// Projects
const projectModalShow = ref(false);
const projectLoaded = ref(false);
const projectOpenAddModal = () => {
	projectModalShow.value = true;
	if (!projectLoaded.value) {
		fetchAllProjects();
	}
};
async function fetchAllProjects() {
	let offset = 0;
	const limit = 100;
	do {
		await projectsStore.getProjectByInterval(limit, offset);
		offset += limit;
	} while (offset < projectsStore.projectsTotalCount);
	projectLoaded.value = true;
}
function projectSave(row: TableauRowData) {
	try {
		if (projectsTagsStore.projectProjectTagReadyByProjectTag(projectTagId.value)?.[row.id_project]?.status === "deleted") {
			delete projectsTagsStore.projectProjectTagReadyByProjectTag(projectTagId.value)[row.id_project];
			addNotification({ message: t("projectTag.ProjectRestored"), type: "success" });
			return;
		}
		projectsTagsStore.projectProjectTagEditionByProjectTag(projectTagId.value)[row.id_project] = { id_project: row.id_project };
		projectsTagsStore.valideProjectProjectTagEditionByKey({ id_project_tag: projectTagId.value, id_project: row.id_project }, "created");
		delete projectsTagsStore.projectProjectTagEditionByProjectTag(projectTagId.value)[row.id_project];
		addNotification({ message: t("projectTag.ProjectAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function projectDelete(project: TableauRowData) {
	try {
		if (projectsTagsStore.projectProjectTagReadyByProjectTag(projectTagId.value)?.[project.id_project]?.status === "created") {
			delete projectsTagsStore.projectProjectTagReadyByProjectTag(projectTagId.value)[project.id_project];
		} else {
			projectsTagsStore.valideProjectProjectTagEditionByKey({ id_project_tag: projectTagId.value, id_project: project.id_project }, "deleted");
		}
		addNotification({ message: t("projectTag.ProjectDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}
function projectRestore(project: TableauRowData) {
	try {
		delete projectsTagsStore.projectProjectTagReadyByProjectTag(projectTagId.value)[project.id_project];
		addNotification({ message: t("projectTag.ProjectRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
}

const filterProject = ref<FilterLabel[]>([
	{ key: "name_project", value: "", type: "text", label: "", placeholder: t("projectTag.ProjectFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

const createSchema = () => {
	const edition = projectTagsStore.projectTagEdition[projectTagId.value];
	const shape: Record<string, Yup.AnySchema> = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.name_project_tag = Yup.string()
		.max(configsStore.getConfigNumber("max_length_name"), t("projectTag.NameMaxLength", { count: configsStore.getConfigNumber("max_length_name") }))
		.required(t("projectTag.NameRequired"));
	shape.weight_project_tag = Yup.number()
		.min(0, t("projectTag.PoidsMin"))
		.typeError(t("projectTag.PoidsNumber"))
		.required(t("projectTag.PoidsRequired"));
	return Yup.object().shape(shape);
};

const labelForm = [
	{ key: "name_project_tag", label: "projectTag.Name", type: "text" },
	{ key: "weight_project_tag", label: "projectTag.Poids", type: "number" },
];
const labelTableauProject = ref<TableauLabel[]>([
	{ label: "projectTag.ProjectName", sortable: true, key: "Project.name_project", sourceKey: "id_project", type: "text",
		storeRessourceId: 1, valueKey: "name_project" },

	{ label: "projectTag.ProjectActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row: TableauRowData) => projectRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row: TableauRowData) => projectDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);

const labelTableauModalProject = ref<TableauLabel[]>([
	{ label: "projectTag.ProjectName", sortable: true, key: "name_project", valueKey: "name_project", type: "text" },
	{ label: "projectTag.ProjectActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "!ready?.status && !store[1]?.[rowData.id_project]",
			action: (row: TableauRowData) => projectSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-rotate-left",
			showCondition: "ready?.status === 'deleted'",
			action: (row: TableauRowData) => projectRestore(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "ready?.status && ready?.status !== 'deleted'",
			action: (row: TableauRowData) => projectDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
useViewScroll(true);
useTabGuard(projectTagsStore, () => projectTagId.value, "projectTagEdition", "projectTags", [
	{ key: "projectTagProjectReady", get: (id) => projectsTagsStore.projectProjectTagReadyByProjectTag(id), set: (id, entries) => projectsTagsStore.setProjectProjectTagStagedSlice("ready", { id_project_tag: id }, entries) },
]);
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('projectTag.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/project-tags',
				create: { showCondition: isNewId(projectTagId) && authStore.hasPermission([0, 1, 2]), loading: projectTagsStore.projectTagEdition[projectTagId]?.loading },
				update: { showCondition: !isNewId(projectTagId) && authStore.hasPermission([0, 1, 2]), loading: projectTagsStore.projectTagEdition[projectTagId]?.loading },
				delete: { showCondition: !isNewId(projectTagId) && authStore.hasPermission([0, 1, 2]) }
			}"
			@button-create="projectTagSave" @button-update="projectTagSave" @button-delete="projectTagDeleteModalShow = true"/>
	</div>
	<div v-if="projectTagsStore.projectTags[projectTagId] || isNewId(projectTagId)" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-edition="projectTagsStore.projectTagEdition[projectTagId]" :store-data="projectTagsStore.projectTags[projectTagId]"/>
		</div>
		<CollapsibleSection title="projectTag.Projects"
			:total-count="projectsTagsStore.projectProjectTagsCountByProjectTag(projectTagId)" :permission="!isNewId(projectTagId)">
			<template #append-row>
				<button type="button" @click="projectOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projectTag.AddProject') }}
				</button>
				<Tableau :labels="labelTableauProject" :meta="{ key: 'id_project', expand: ['project'] }"
					:store-data="[projectsTagsStore.projectProjectTagsByProjectTag(projectTagId),projectsStore.projects]"
					:store-ready="projectsTagsStore.projectProjectTagReadyByProjectTag(projectTagId)"
					:loading="projectsTagsStore.projectProjectTagsLoading"
					:total-count="projectsTagsStore.projectProjectTagsCountByProjectTag(projectTagId)"
					:fetch-function="!isNewId(projectTagId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => projectsTagsStore.getProjectProjectTagByInterval({ id_project_tag: projectTagId }, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('projectTag.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="projectTagDeleteModalShow" @close-modal="projectTagDeleteModalShow = false"
		:delete-action="projectTagDelete" :text-title="'projectTag.DeleteTitle'" :text-p="'projectTag.DeleteText'"/>

	<div v-if="projectModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="projectModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('projectTag.ProjectTitle') }}</h2>
				<button type="button" @click="projectModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterProject" :store-data="projectsStore.projects" />

			<!-- Tableau Projects -->
			<Tableau :labels="labelTableauModalProject" :meta="{ key: 'id_project' }"
				:store-data="[projectsStore.projects, projectsTagsStore.projectProjectTagsByProjectTag(projectTagId)]"
				:store-ready="projectsTagsStore.projectProjectTagReadyByProjectTag(projectTagId)"
				:filters="filterProject"
				:loading="projectsTagsStore.projectProjectTagsLoading"
				:total-count="Number(projectsStore.projectsTotalCount || 0)"
				:fetch-function="!isNewId(projectTagId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => projectsStore.getProjectByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

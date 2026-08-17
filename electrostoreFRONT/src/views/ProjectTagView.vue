<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const projectTagId = ref(route.params.id);
const preset = ref(route.query.preset || null);

import { useConfigsStore, useProjectTagsStore, useProjectsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const projectTagsStore = useProjectTagsStore();
const projectsStore = useProjectsStore();
const authStore = useAuthStore();

const formContainer = ref(null);

async function fetchAllData() {
	if (projectTagId.value === "new") {
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
		if (projectTagId.value === "new") {
			const newId = await projectTagsStore.createProjectTag({ ...projectTagsStore.projectTagEdition[projectTagId.value] });
			projectTagsStore.loadToEdition(newId);
			addNotification({ message: t("projectTag.Created"), type: "success" });
			projectTagId.value = String(newId);
			router.push("/project-tags/" + projectTagId.value);
		} else {
			await projectTagsStore.updateProjectTag(projectTagId.value, { ...projectTagsStore.projectTagEdition[projectTagId.value] });
			projectTagsStore.loadToEdition(projectTagId.value);
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
const projectSave = async(project) => {
	try {
		await projectTagsStore.createProjectTagProject(projectTagId.value, project);
		addNotification({ message: t("projectTag.ProjectAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const projectDelete = async(project) => {
	try {
		await projectTagsStore.deleteProjectTagProject(projectTagId.value, project.id_project);
		addNotification({ message: t("projectTag.ProjectDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const filterProject = ref([
	{ key: "name_project", value: "", type: "text", label: "", placeholder: t("projectTag.ProjectFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

const createSchema = () => {
	const edition = projectTagsStore.projectTagEdition[projectTagId.value];
	const shape = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.name_project_tag = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("projectTag.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
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
const labelTableauProject = ref([
	{ label: "projectTag.ProjectName", sortable: true, key: "Project.name_project", sourceKey: "id_project", type: "text", 
		storeRessourceId: 1, valueKey: "name_project" },
		
	{ label: "projectTag.ProjectActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => projectDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);

const labelTableauModalProject = ref([
	{ label: "projectTag.ProjectName", sortable: true, key: "name_project", valueKey: "name_project", type: "text" },
	{ label: "projectTag.ProjectActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "!store[1]?.[rowData.id_project]",
			action: (row) => projectSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "store[1]?.[rowData.id_project]",
			action: (row) => projectDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('projectTag.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/project-tags',
				create: { showCondition: projectTagId === 'new' && authStore.hasPermission([0, 1, 2]), loading: projectTagsStore.projectTagEdition[projectTagId]?.loading },
				update: { showCondition: projectTagId !== 'new' && authStore.hasPermission([0, 1, 2]), loading: projectTagsStore.projectTagEdition[projectTagId]?.loading },
				delete: { showCondition: projectTagId !== 'new' && authStore.hasPermission([0, 1, 2]) }
			}"
			@button-create="projectTagSave" @button-update="projectTagSave" @button-delete="projectTagDeleteModalShow = true"/>
	</div>
	<div v-if="projectTagsStore.projectTags[projectTagId] || projectTagId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="projectTagsStore.projectTagEdition[projectTagId]"/>
		</div>
		<CollapsibleSection title="projectTag.Projects"
			:total-count="Number(projectTagsStore.projectTagsProjectTotalCount[projectTagId] || 0)" :permission="projectTagId !=='new'">
			<template #append-row>
				<button type="button" @click="projectOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projectTag.AddProject') }}
				</button>
				<Tableau :labels="labelTableauProject" :meta="{ key: 'id_project', expand: ['project'] }"
					:store-data="[projectTagsStore.projectTagsProject[projectTagId],projectsStore.projects]"
					:loading="projectTagsStore.projectTagsProjectLoading"
					:total-count="Number(projectTagsStore.projectTagsProjectTotalCount[projectTagId] || 0)"
					:fetch-function="projectTagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projectTagsStore.getProjectTagProjectByInterval(projectTagId, limit, offset, expand, filter, sort, clear) : undefined"
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
				:store-data="[projectsStore.projects, projectTagsStore.projectTagsProject[projectTagId]]"
				:filters="filterProject"
				:loading="projectTagsStore.projectTagsProjectLoading"
				:total-count="Number(projectsStore.projectsTotalCount || 0)"
				:fetch-function="projectTagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projectsStore.getProjectByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

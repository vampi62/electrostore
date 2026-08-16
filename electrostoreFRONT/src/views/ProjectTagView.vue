<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const projetTagId = ref(route.params.id);
const preset = ref(route.query.preset || null);

import { useConfigsStore, useProjectTagsStore, useProjectsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const projetTagsStore = useProjectTagsStore();
const projetsStore = useProjectsStore();
const authStore = useAuthStore();

const formContainer = ref(null);

async function fetchAllData() {
	if (projetTagId.value === "new") {
		projetTagsStore.loadToEdition(projetTagId.value, preset.value);
	} else {
		projetTagsStore.setLoadingEdition(projetTagId.value, true);
		try {
			await projetTagsStore.getProjetTagById(projetTagId.value);
		} catch {
			delete projetTagsStore.projectTags[projetTagId.value];
			addNotification({ message: t("projectTag.NotFound"), type: "error" });
			router.push("/project-tags");
			return;
		}
		projetTagsStore.loadToEdition(projetTagId.value);
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	projetTagsStore.clearEdition(projetTagId.value);
});

const projetTagDeleteModalShow = ref(false);
const projetTagSave = async() => {
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
			projetTagsStore.setLoadingEdition(projetTagId.value, false);
			return;
		}
		if (projetTagId.value === "new") {
			const newId = await projetTagsStore.createProjetTag({ ...projetTagsStore.projetTagEdition[projetTagId.value] });
			projetTagsStore.loadToEdition(newId);
			addNotification({ message: t("projectTag.Created"), type: "success" });
			projetTagId.value = String(newId);
			router.push("/project-tags/" + projetTagId.value);
		} else {
			await projetTagsStore.updateProjetTag(projetTagId.value, { ...projetTagsStore.projetTagEdition[projetTagId.value] });
			projetTagsStore.loadToEdition(projetTagId.value);
			addNotification({ message: t("projectTag.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		projetTagsStore.setLoadingEdition(projetTagId.value, false);
	}
};
const projetTagDelete = async() => {
	try {
		await projetTagsStore.deleteProjetTag(projetTagId.value);
		addNotification({ message: t("projectTag.Deleted"), type: "success" });
		router.push("/project-tags");
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	projetTagDeleteModalShow.value = false;
};

// Projects
const projetModalShow = ref(false);
const projetLoaded = ref(false);
const projetOpenAddModal = () => {
	projetModalShow.value = true;
	if (!projetLoaded.value) {
		fetchAllProjets();
	}
};
async function fetchAllProjets() {
	let offset = 0;
	const limit = 100;
	do {
		await projetsStore.getProjetByInterval(limit, offset);
		offset += limit;
	} while (offset < projetsStore.projetsTotalCount);
	projetLoaded.value = true;
}
const projetSave = async(project) => {
	try {
		await projetTagsStore.createProjetTagProjet(projetTagId.value, project);
		addNotification({ message: t("projectTag.ProjetAdded"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
		return;
	}
};
const projetDelete = async(project) => {
	try {
		await projetTagsStore.deleteProjetTagProjet(projetTagId.value, project.id_project);
		addNotification({ message: t("projectTag.ProjetDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const filterProjet = ref([
	{ key: "name_project", value: "", type: "text", label: "", placeholder: t("projectTag.ProjetFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

const createSchema = () => {
	const edition = projetTagsStore.projetTagEdition[projetTagId.value];
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
const labelTableauProjet = ref([
	{ label: "projectTag.ProjetName", sortable: true, key: "Project.name_project", sourceKey: "id_project", type: "text", 
		storeRessourceId: 1, valueKey: "name_project" },
		
	{ label: "projectTag.ProjetActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => projetDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);

const labelTableauModalProjet = ref([
	{ label: "projectTag.ProjetName", sortable: true, key: "name_project", valueKey: "name_project", type: "text" },
	{ label: "projectTag.ProjetActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "!store[1]?.[rowData.id_project]",
			action: (row) => projetSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "store[1]?.[rowData.id_project]",
			action: (row) => projetDelete(row),
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
				create: { showCondition: projetTagId === 'new' && authStore.hasPermission([0, 1, 2]), loading: projetTagsStore.projetTagEdition[projetTagId]?.loading },
				update: { showCondition: projetTagId !== 'new' && authStore.hasPermission([0, 1, 2]), loading: projetTagsStore.projetTagEdition[projetTagId]?.loading },
				delete: { showCondition: projetTagId !== 'new' && authStore.hasPermission([0, 1, 2]) }
			}"
			@button-create="projetTagSave" @button-update="projetTagSave" @button-delete="projetTagDeleteModalShow = true"/>
	</div>
	<div v-if="projetTagsStore.projectTags[projetTagId] || projetTagId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="projetTagsStore.projetTagEdition[projetTagId]"/>
		</div>
		<CollapsibleSection title="projectTag.Projects"
			:total-count="Number(projetTagsStore.projetTagsProjetTotalCount[projetTagId] || 0)" :permission="projetTagId !=='new'">
			<template #append-row>
				<button type="button" @click="projetOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projectTag.AddProjet') }}
				</button>
				<Tableau :labels="labelTableauProjet" :meta="{ key: 'id_project', expand: ['project'] }"
					:store-data="[projetTagsStore.projetTagsProjet[projetTagId],projetsStore.projects]"
					:loading="projetTagsStore.projetTagsProjetLoading"
					:total-count="Number(projetTagsStore.projetTagsProjetTotalCount[projetTagId] || 0)"
					:fetch-function="projetTagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projetTagsStore.getProjetTagProjetByInterval(projetTagId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('projectTag.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="projetTagDeleteModalShow" @close-modal="projetTagDeleteModalShow = false"
		:delete-action="projetTagDelete" :text-title="'projectTag.DeleteTitle'" :text-p="'projectTag.DeleteText'"/>

	<div v-if="projetModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="projetModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('projectTag.ProjetTitle') }}</h2>
				<button type="button" @click="projetModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterProjet" :store-data="projetsStore.projects" />

			<!-- Tableau Projects -->
			<Tableau :labels="labelTableauModalProjet" :meta="{ key: 'id_project' }"
				:store-data="[projetsStore.projects, projetTagsStore.projetTagsProjet[projetTagId]]"
				:filters="filterProjet"
				:loading="projetTagsStore.projetTagsProjetLoading"
				:total-count="Number(projetsStore.projetsTotalCount || 0)"
				:fetch-function="projetTagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projetsStore.getProjetByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

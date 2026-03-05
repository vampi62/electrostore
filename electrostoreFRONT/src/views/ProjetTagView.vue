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

import { useConfigsStore, useProjetTagsStore, useProjetsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const projetTagsStore = useProjetTagsStore();
const projetsStore = useProjetsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (projetTagId.value === "new") {
		projetTagsStore.projetTagEdition = {
			loading: false,
		};
		if (preset.value) {
			preset.value.split(";").forEach((pair) => {
				const [key, value] = pair.split(":");
				if (key && value) {
					projetTagsStore.projetTagEdition[key] = value;
				}
			});
		}
	} else {
		projetTagsStore.projetTagEdition = {
			loading: true,
		};
		try {
			await projetTagsStore.getProjetTagById(projetTagId.value);
		} catch {
			delete projetTagsStore.projetTags[projetTagId.value];
			addNotification({ message: "projetTag.NotFound", type: "error", i18n: true });
			router.push("/projet-tags");
			return;
		}
		projetTagsStore.projetTagEdition = {
			loading: false,
			nom_projet_tag: projetTagsStore.projetTags[projetTagId.value].nom_projet_tag,
			poids_projet_tag: projetTagsStore.projetTags[projetTagId.value].poids_projet_tag,
		};
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	projetTagsStore.projetTagEdition = {
		loading: false,
	};
});

const projetTagDeleteModalShow = ref(false);
const projetTagSave = async() => {
	try {
		createSchema().validateSync(projetTagsStore.projetTagEdition, { abortEarly: false });
		if (projetTagId.value === "new") {
			const newId = await projetTagsStore.createProjetTag({ ...projetTagsStore.projetTagEdition });
			addNotification({ message: "projetTag.Created", type: "success", i18n: true });
			projetTagId.value = String(newId);
			router.push("/projet-tags/" + projetTagId.value);
		} else {
			await projetTagsStore.updateProjetTag(projetTagId.value, { ...projetTagsStore.projetTagEdition });
			addNotification({ message: "projetTag.Updated", type: "success", i18n: true });
		}
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const projetTagDelete = async() => {
	try {
		await projetTagsStore.deleteProjetTag(projetTagId.value);
		addNotification({ message: "projetTag.Deleted", type: "success", i18n: true });
		router.push("/projet-tags");
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	projetTagDeleteModalShow.value = false;
};

// Projets
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
const projetSave = async(projet) => {
	try {
		await projetTagsStore.createProjetTagProjet(projetTagId.value, projet);
		addNotification({ message: "projetTag.ProjetAdded", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const projetDelete = async(projet) => {
	try {
		await projetTagsStore.deleteProjetTagProjet(projetTagId.value, projet.id_projet);
		addNotification({ message: "projetTag.ProjetDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};

const filterProjet = ref([
	{ key: "nom_projet", value: "", type: "text", label: "", placeholder: t("projetTag.ProjetFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

const createSchema = () => {
	return Yup.object().shape({
		nom_projet_tag: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("projetTag.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
			.required(t("projetTag.NameRequired")),
		poids_projet_tag: Yup.number()
			.min(0, t("projetTag.PoidsMin"))
			.typeError(t("projetTag.PoidsNumber"))
			.required(t("projetTag.PoidsRequired")),
	});
};

const labelForm = [
	{ key: "nom_projet_tag", label: "projetTag.Name", type: "text" },
	{ key: "poids_projet_tag", label: "projetTag.Poids", type: "number" },
];
const labelTableauProjet = ref([
	{ label: "projetTag.ProjetName", sortable: true, key: "Projet.nom_projet", sourceKey: "id_projet", type: "text", 
		storeRessourceId: 1, valueKey: "nom_projet" },
		
	{ label: "projetTag.ProjetActions", sortable: false, key: "", type: "buttons", buttons: [
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
	{ label: "projetTag.ProjetName", sortable: true, key: "nom_projet", valueKey: "nom_projet", type: "text" },
	{ label: "projetTag.ProjetActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-save",
			condition: "!store[1]?.[rowData.id_projet]",
			action: (row) => projetSave(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			condition: "store[1]?.[rowData.id_projet]",
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
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('projetTag.Title') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/projet-tags', save: { roleRequired: authStore.hasPermission([0, 1, 2]), loading: projetTagsStore.projetTagEdition.loading }, delete: { roleRequired: authStore.hasPermission([0, 1, 2]) } }"
			:id="projetTagId" @button-save="projetTagSave" @button-delete="projetTagDeleteModalShow = true"/>
	</div>
	<div v-if="projetTagsStore.projetTags[projetTagId] || projetTagId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="projetTagsStore.projetTagEdition"/>
		</div>
		<CollapsibleSection title="projetTag.Projets"
			:total-count="Number(projetTagsStore.projetTagsProjetTotalCount[projetTagId] || 0)" :id-page="projetTagId">
			<template #append-row>
				<button type="button" @click="projetOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projetTag.AddProjet') }}
				</button>
				<Tableau :labels="labelTableauProjet" :meta="{ key: 'id_projet', expand: ['projet'] }"
					:store-data="[projetTagsStore.projetTagsProjet[projetTagId],projetsStore.projets]"
					:loading="projetTagsStore.projetTagsProjetLoading"
					:total-count="Number(projetTagsStore.projetTagsProjetTotalCount[projetTagId] || 0)"
					:fetch-function="projetTagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projetTagsStore.getProjetTagProjetByInterval(projetTagId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('projetTag.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="projetTagDeleteModalShow" @close-modal="projetTagDeleteModalShow = false"
		:delete-action="projetTagDelete" :text-title="'projetTag.DeleteTitle'" :text-p="'projetTag.DeleteText'"/>

	<div v-if="projetModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="projetModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('projetTag.ProjetTitle') }}</h2>
				<button type="button" @click="projetModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterProjet" :store-data="projetsStore.projets" />

			<!-- Tableau Projets -->
			<Tableau :labels="labelTableauModalProjet" :meta="{ key: 'id_projet' }"
				:store-data="[projetsStore.projets, projetTagsStore.projetTagsProjet[projetTagId]]"
				:filters="filterProjet"
				:loading="projetTagsStore.projetTagsProjetLoading"
				:total-count="Number(projetsStore.projetsTotalCount || 0)"
				:fetch-function="projetTagId !== 'new' ? (limit, offset, expand, filter, sort, clear) => projetsStore.getProjetByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

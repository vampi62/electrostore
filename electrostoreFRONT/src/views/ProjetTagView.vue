<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const projetTagId = ref(route.params.id);

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
	} else {
		projetTagsStore.projetTagEdition = {
			loading: true,
		};
		try {
			await projetTagsStore.getProjetTagById(projetTagId.value);
		} catch {
			delete projetTagsStore.projetTags[projetTagId.value];
			addNotification({ message: "projetTag.VProjetTagNotFound", type: "error", i18n: true });
			router.push("/projet-tags");
			return;
		}
		projetTagsStore.getProjetTagProjetByInterval(projetTagId.value, 100, 0, ["projet"]);
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
			await projetTagsStore.createProjetTag({ ...projetTagsStore.projetTagEdition });
			addNotification({ message: "projetTag.VProjetTagCreated", type: "success", i18n: true });
		} else {
			await projetTagsStore.updateProjetTag(projetTagId.value, { ...projetTagsStore.projetTagEdition });
			addNotification({ message: "projetTag.VProjetTagUpdated", type: "success", i18n: true });
		}
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
	if (projetTagId.value === "new") {
		projetTagId.value = String(projetTagsStore.projetTagEdition.id_projet_tag);
		router.push("/projet-tags/" + projetTagId.value);
	}
};
const projetTagDelete = async() => {
	try {
		await projetTagsStore.deleteProjetTag(projetTagId.value);
		addNotification({ message: "projetTag.VProjetTagDeleted", type: "success", i18n: true });
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
		addNotification({ message: "projetTag.VProjetTagProjetAdded", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const projetDelete = async(projet) => {
	try {
		await projetTagsStore.deleteProjetTagProjet(projetTagId.value, projet.id_projet);
		addNotification({ message: "projetTag.VProjetTagProjetDeleted", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};

const filteredProjets = ref([]);
const updateFilteredProjets = (newValue) => {
	filteredProjets.value = newValue;
};
const filterProjet = ref([
	{ key: "nom_projet", value: "", type: "text", label: "", placeholder: t("projetTag.VProjetTagProjetFilterPlaceholder"), compareMethod: "contain", class: "w-full" },
]);

const createSchema = () => {
	return Yup.object().shape({
		nom_projet_tag: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("projetTag.VProjetTagNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
			.required(t("projetTag.VProjetTagNameRequired")),
		poids_projet_tag: Yup.number()
			.min(0, t("projetTag.VProjetTagPoidsMin"))
			.typeError(t("projetTag.VProjetTagPoidsNumber"))
			.required(t("projetTag.VProjetTagPoidsRequired")),
	});
};

const labelForm = [
	{ key: "nom_projet_tag", label: "projetTag.VProjetTagName", type: "text" },
	{ key: "poids_projet_tag", label: "projetTag.VProjetTagPoids", type: "number" },
];
const labelTableauProjet = ref([
	{ label: "projetTag.VProjetTagProjetName", sortable: true, key: "nom_projet", keyStore: "id_projet", store: "1", type: "text" },
	{ label: "projetTag.VProjetTagProjetActions", sortable: false, key: "", type: "buttons", buttons: [
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
	{ label: "projetTag.VProjetTagProjetName", sortable: true, key: "nom_projet", type: "text" },
	{ label: "projetTag.VProjetTagProjetActions", sortable: false, key: "", type: "buttons", buttons: [
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
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('projetTag.VProjetTagTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/projet-tags', save: { roleRequired: 0, loading: projetTagsStore.projetTagEdition.loading }, delete: { roleRequired: 0 } }"
			:id="projetTagId" :store-user="authStore.user" @button-save="projetTagSave" @button-delete="projetTagDeleteModalShow = true"/>
	</div>
	<div v-if="projetTagsStore.projetTags[projetTagId] || projetTagId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="projetTagsStore.projetTagEdition"/>
		</div>
		<CollapsibleSection title="projetTag.VProjetTagProjets"
			:total-count="Number(projetTagsStore.projetTagsProjetTotalCount[projetTagId] || 0)" :id-page="projetTagId">
			<template #append-row>
				<button type="button" @click="projetOpenAddModal"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('projetTag.VProjetTagAddProjet') }}
				</button>
				<Tableau :labels="labelTableauProjet" :meta="{ key: 'id_projet' }"
					:store-data="[projetTagsStore.projetTagsProjet[projetTagId],projetsStore.projets]"
					:loading="projetTagsStore.projetTagsProjetLoading"
					:total-count="Number(projetTagsStore.projetTagsProjetTotalCount[projetTagId] || 0)"
					:loaded-count="Object.keys(projetTagsStore.projetTagsProjet[projetTagId] || {}).length"
					:fetch-function="(offset, limit) => projetTagsStore.getProjetTagProjetByInterval(projetTagId, limit, offset, ['projet'])"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('projetTag.VProjetTagLoading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="projetTagDeleteModalShow" @close-modal="projetTagDeleteModalShow = false"
		:delete-action="projetTagDelete" :text-title="'projetTag.VProjetTagDeleteTitle'" :text-p="'projetTag.VProjetTagDeleteText'"/>

	<div v-if="projetModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="projetModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('projetTag.VProjetTagProjetTitle') }}</h2>
				<button type="button" @click="projetModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<!-- Filtres -->
			<FilterContainer class="my-4 flex gap-4" :filters="filterProjet" :store-data="projetsStore.projets" @output-filter="updateFilteredProjets" />

			<!-- Tableau Projets -->
			<Tableau :labels="labelTableauModalProjet" :meta="{ key: 'id_projet' }"
				:store-data="[filteredProjets, projetTagsStore.projetTagsProjet[projetTagId]]"
				:loading="projetTagsStore.projetTagsProjetLoading"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>

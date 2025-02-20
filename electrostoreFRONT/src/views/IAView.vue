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
const iaId = route.params.id;

import { useConfigsStore, useIasStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const iasStore = useIasStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (iaId !== "new") {
		iasStore.iaEdition = {
			loading: true,
		};
		try {
			await iasStore.getIaById(iaId);
		} catch {
			delete iasStore.ias[iaId];
			addNotification({ message: "ia.VIaNotFound", type: "error", i18n: true });
			router.push("/ias");
			return;
		}
		intervalRefreshStatus = setInterval(() => {
			iasStore.getTrainStatus(iaId);
		}, 15000);
		iasStore.getTrainStatus(iaId);
		iasStore.iaEdition = {
			loading: false,
			nom_ia: iasStore.ias[iaId].nom_ia,
			description_ia: iasStore.ias[iaId].description_ia,
			date_ia: iasStore.ias[iaId].date_ia,
			trained_ia: iasStore.ias[iaId].trained_ia,
		};
	} else {
		iasStore.iaEdition = {
			loading: false,
		};
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	if (intervalRefreshStatus) {
		clearInterval(intervalRefreshStatus);
	}
	iasStore.iaEdition = {
		loading: false,
	};
});

let intervalRefreshStatus = null;
const iaDeleteModalShow = ref(false);
const iaSave = async() => {
	try {
		await schemaIa.validate(iasStore.iaEdition, { abortEarly: false });
		await iasStore.createIa(iasStore.iaEdition);
		addNotification({ message: "ia.VIaCreated", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	iaId = iasStore.iaEdition.id_ia;
	router.push("/ias/" + iasStore.iaEdition.id_ia);
};
const iaUpdate = async() => {
	try {
		await schemaIa.validate(iasStore.iaEdition, { abortEarly: false });
		await iasStore.updateIa(iaId, iasStore.iaEdition);
		addNotification({ message: "ia.VIaUpdated", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
};
const iaDelete = async() => {
	try {
		await iasStore.deleteIa(iaId);
		addNotification({ message: "ia.VIaDeleted", type: "success", i18n: true });
		router.push("/ias");
	} catch (e) {
		addNotification({ message: "ia.VIaDeleteError", type: "error", i18n: true });
	}
	iaDeleteModalShow.value = false;
};
const iaTrain = async() => {
	try {
		await iasStore.startTrain(iaId);
		addNotification({ message: "ia.VIaTrainStart", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
};
const formatDateForDatetimeLocal = (date) => {
	const pad = (num) => String(num).padStart(2, "0");
	return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};
const schemaIa = Yup.object().shape({
	nom_ia: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("ia.VIaNameMaxLength") + t("common.VAllCaracters"))
		.required(t("ia.VIaNameRequired")),
	description_ia: Yup.string()
		.max(configsStore.getConfigByKey("max_length_description"), t("ia.VIaDescriptionMaxLength") + t("common.VAllCaracters"))
		.required(t("ia.VIaDescriptionRequired")),
});
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('ia.VIaTitle') }}</h2>
		<div class="flex space-x-4">
			<button type="button" @click="iasStore.getTrainStatus(iaId)" v-if="iaId != 'new'"
				class="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600 flex items-center">
				<span v-show="iasStore.status.train?.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('ia.VIaRefresh') }}
			</button>
			<button type="button" @click="iaTrain" v-if="iaId != 'new' && authStore.user?.role_user == 'admin'"
				class="bg-green-500 text-white px-4 py-2 rounded hover:bg-green-600 flex items-center">
				<span v-show="iasStore.status.start?.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('ia.VIaTrain') }}
			</button>
			<button type="button" @click="iaSave" v-if="iaId == 'new' && authStore.user?.role_user == 'admin'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="iasStore.iaEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('ia.VIaAdd') }}
			</button>
			<button type="button" @click="iaUpdate" v-else-if="iaId != 'new' && authStore.user?.role_user == 'admin'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="iasStore.iaEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('ia.VIaUpdate') }}
			</button>
			<button type="button" @click="iaDeleteModalShow = true"
				v-if="iaId != 'new' && authStore.user?.role_user == 'admin'"
				class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">
				{{ $t('ia.VIaDelete') }}
			</button>
			<RouterLink to="/ia"
				class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">
				{{ $t('ia.VIaBack') }}
			</RouterLink>
		</div>
	</div>
	<div v-if="iasStore.ias[iaId] || iaId == 'new'">
		<div class="mb-6 flex justify-between flex-wrap">
			<Form :validation-schema="schemaIa" v-slot="{ errors }" @submit.prevent="" class="mb-6">
				<table class="table-auto text-gray-700">
					<tbody>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('ia.VIaName') }}:</td>
							<td class="flex flex-col">
								<Field name="nom_ia" type="text" v-model="iasStore.iaEdition.nom_ia"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.nom_ia }"
									:disabled="authStore.user?.role_user !== 'admin'" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.nom_ia || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('ia.VIaDescription') }}:</td>
							<td class="flex flex-col">
								<Field name="description_ia" v-slot="{ description_ia }">
									<textarea v-bind="description_ia" v-model="iasStore.iaEdition.description_ia"
										:value="iasStore.iaEdition.description_ia"
										class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300 resize-y"
										:class="{ 'border-red-500': errors.description_ia }"
										:disabled="authStore.user?.role_user !== 'admin'" rows="4">
									</textarea>
								</Field>
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.description_ia || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('ia.VIaDate') }}:</td>
							<td class="flex flex-col">
								<!-- format date permit is only YYYY-MM-DDTHH-mm-->
								<Field name="date_ia" type="datetime-local"
									v-model="iasStore.iaEdition.date_ia"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.date_ia }" disabled />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.date_ia || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('ia.VIaStatus') }}:</td>
							<td class="flex flex-col">
								<template v-if="iasStore.iaEdition.trained_ia">
									<font-awesome-icon icon="fa-solid fa-check" class="text-green-500" />
								</template>
								<template v-else>
									<font-awesome-icon icon="fa-solid fa-times" class="text-red-500" />
								</template>
							</td>
						</tr>
					</tbody>
				</table>
			</Form>
			<div class="w-96 h-96 bg-gray-200 px-4 py-2 rounded">
				<div v-for="(value, key) in iasStore.status.train" :key="key">
					<div v-if="key !== 'loading'">
						{{ key }}: {{ value }}
					</div>
				</div>
			</div>
		</div>
	</div>
	<div v-else>
		<div>{{ $t('ia.VIaLoading') }}</div>
	</div>

	<div v-if="iaDeleteModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="iaDeleteModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t('ia.VIaDeleteTitle') }}</h2>
			<p>{{ $t('ia.VIaDeleteText') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="iaDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('ia.VIaDeleteConfirm') }}
				</button>
				<button type="button" @click="iaDeleteModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('ia.VIaDeleteCancel') }}
				</button>
			</div>
		</div>
	</div>
</template>

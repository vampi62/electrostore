<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
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
		if (iaId !== "new") {
			await iasStore.updateIa(iaId, { ...iasStore.iaEdition });
			addNotification({ message: "ia.VIaUpdated", type: "success", i18n: true });
		} else {
			await iasStore.createIa({ ...iasStore.iaEdition });
			addNotification({ message: "ia.VIaCreated", type: "success", i18n: true });
		}
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	if (iaId === "new") {
		router.push("/ias/" + iasStore.iaEdition.id_ia);
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
	if (typeof date === "string") {
		date = new Date(date);
	}
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
		<TopButtonEditElement :main-config="{ path: '/ia', save: { roleRequired: 2, loading: iasStore.iaEdition.loading }, delete: { roleRequired: 2 } }"
			:optional-config="[
				{ label: 'ia.VIaTrain', roleRequired: 2, loading: iasStore.status.start?.loading, bgColor: 'bg-green-500', hoverColor: 'hover:bg-green-600', action: iaTrain },
				{ label: 'ia.VIaRefresh', roleRequired: 0, loading: iasStore.status.train?.loading, bgColor: 'bg-gray-500', hoverColor: 'hover:bg-gray-600', action: () => iasStore.getTrainStatus(iaId) },
			]"
			:id="iaId" :store-user="authStore.user" @button-save="iaSave" @button-delete="iaDeleteModalShow = true"/>
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
									:disabled="authStore.user?.role_user !== 2" />
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
										:disabled="authStore.user?.role_user !== 2" rows="4">
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

	<ModalDeleteConfirm :showModal="iaDeleteModalShow" @closeModal="iaDeleteModalShow = false"
		@deleteConfirmed="iaDelete" :textTitle="'ia.VIaDeleteTitle'" :textP="'ia.VIaDeleteText'"/>
</template>

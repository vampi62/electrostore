<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const cronJobId = ref(route.params.id);
const preset = ref(route.query.preset || null);

import { useConfigsStore, useCronJobsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const cronJobsStore = useCronJobsStore();
const authStore = useAuthStore();

const cronJobActionOptions = {
	0: t("cronJob.ActionPackageTracking"),
	1: { label: t("cronJob.ActionIARetrain"), disabled: true },
	2: { label: t("cronJob.ActionStockLowAlert"), disabled: true },
};

const formContainer = ref(null);

async function fetchAllData() {
	if (cronJobId.value === "new") {
		loadToEdition(cronJobId.value);
		if (preset.value) {
			preset.value.split(";").forEach((pair) => {
				const [key, value] = pair.split(":");
				if (key && value) {
					cronJobsStore.cronJobEdition[key] = value;
				}
			});
		}
	} else {
		cronJobsStore.cronJobEdition = {
			loading: true,
		};
		try {
			await cronJobsStore.getCronJobById(cronJobId.value);
		} catch {
			delete cronJobsStore.cronJobs[cronJobId.value];
			addNotification({ message: t("cronJob.NotFound"), type: "error" });
			router.push("/cronjobs");
			return;
		}
		loadToEdition(cronJobId.value);
	}
}
function loadToEdition(id) {
	if (id === "new") {
		cronJobsStore.cronJobEdition = {
			loading: false,
			is_enabled: true,
		};
	} else {
		cronJobsStore.cronJobEdition = {
			loading: false,
			name_cronjob: cronJobsStore.cronJobs[id].name_cronjob,
			cron_expression: cronJobsStore.cronJobs[id].cron_expression,
			action_cronjob: cronJobsStore.cronJobs[id].action_cronjob,
			params_cronjob: cronJobsStore.cronJobs[id].params_cronjob,
			is_enabled: cronJobsStore.cronJobs[id].is_enabled,
			last_run_at: cronJobsStore.cronJobs[id].last_run_at,
			next_run_at: cronJobsStore.cronJobs[id].next_run_at,
		};
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	cronJobsStore.cronJobEdition = {
		loading: false,
	};
});

const cronJobDeleteModalShow = ref(false);
const cronJobSave = async() => {
	try {
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("cronJob.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			cronJobsStore.cronJobEdition.loading = false;
			return;
		}
		if (cronJobId.value === "new") {
			const newId = await cronJobsStore.createCronJob({ ...cronJobsStore.cronJobEdition });
			loadToEdition(newId);
			addNotification({ message: t("cronJob.Created"), type: "success" });
			cronJobId.value = String(newId);
			router.push("/cronjobs/" + cronJobId.value);
		} else {
			await cronJobsStore.updateCronJob(cronJobId.value, { ...cronJobsStore.cronJobEdition });
			loadToEdition(cronJobId.value);
			addNotification({ message: t("cronJob.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		cronJobsStore.cronJobEdition.loading = false;
	}
};
const cronJobDelete = async() => {
	try {
		await cronJobsStore.deleteCronJob(cronJobId.value);
		addNotification({ message: t("cronJob.Deleted"), type: "success" });
		router.push("/cronjobs");
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	cronJobDeleteModalShow.value = false;
};
const createSchema = () => {
	const edition = cronJobsStore.cronJobEdition;
	const shape = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.name_cronjob = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("cronJob.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("cronJob.NameRequired"));
	shape.cron_expression = Yup.string()
		.required(t("cronJob.CronExpressionRequired"))
		.test("is-valid-cron", t("cronJob.CronExpressionInvalid"), (value) => {
			if (!value) {
				return false; // Required validation will handle empty values
			}
			const cronParts = value.trim().split(/\s+/);
			return cronParts.length === 5 || cronParts.length === 6;
		});
	shape.action_cronjob = Yup.string()
		.required(t("cronJob.ActionRequired"));
	return Yup.object().shape(shape);
};
const labelForm = [
	{ key: "name_cronjob", label: "cronJob.Name", type: "text", enableCondition: "func.hasPermission([2])" },
	{ key: "cron_expression", label: "cronJob.CronExpression", type: "text", enableCondition: "func.hasPermission([2])", placeholder: "cronJob.CronExpressionPlaceholder" },
	{ key: "action_cronjob", label: "cronJob.Action", type: "select", options: cronJobActionOptions, enableCondition: "func.hasPermission([2])" },
	{ key: "params_cronjob", label: "cronJob.Params", type: "textarea", rows: 4, enableCondition: "func.hasPermission([2])" },
	{ key: "is_enabled", label: "cronJob.IsEnabled", type: "checkbox", enableCondition: "func.hasPermission([2])" },
	{ key: "last_run_at", label: "cronJob.LastRun", type: "computed" },
	{ key: "next_run_at", label: "cronJob.NextRun", type: "computed" },
];
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('cronJob.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/cronjobs',
				create: { showCondition: cronJobId === 'new' && authStore.hasPermission([2]), loading: cronJobsStore.cronJobEdition?.loading },
				update: { showCondition: cronJobId !== 'new' && authStore.hasPermission([2]), loading: cronJobsStore.cronJobEdition?.loading },
				delete: { showCondition: cronJobId !== 'new' && authStore.hasPermission([2]) }
			}"
			@button-create="cronJobSave" @button-update="cronJobSave" @button-delete="cronJobDeleteModalShow = true"/>
	</div>
	<div v-if="cronJobsStore.cronJobs[cronJobId] || cronJobId == 'new'" class="w-full">
		<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="cronJobsStore.cronJobEdition" :store-user="authStore.user"
			:store-function="{ hasPermission: (validPerm) => authStore.hasPermission(validPerm) }"/>
	</div>
	<div v-else>
		<div>{{ $t('cronJob.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="cronJobDeleteModalShow" @close-modal="cronJobDeleteModalShow = false"
		:delete-action="cronJobDelete" :text-title="'cronJob.DeleteTitle'" :text-p="'cronJob.DeleteText'"/>
</template>

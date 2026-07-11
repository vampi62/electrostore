<script setup>
import { ref } from "vue";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useCronJobsStore, useAuthStore } from "@/stores";
const cronJobsStore = useCronJobsStore();
const authStore = useAuthStore();

const cronJobActionOptions = {
	0: t("cronJobs.ActionPackageTracking"),
	1: t("cronJobs.ActionIARetrain"),
	2: t("cronJobs.ActionStockLowAlert"),
};

const filter = ref([
	{ key: "name_cronjob", value: "", type: "text", label: "cronJobs.FilterName", compareMethod: "=like=" },
	{ key: "action_cronjob", value: undefined, type: "datalist", options: cronJobActionOptions, label: "cronJobs.FilterAction", compareMethod: "==" },
	{ key: "is_enabled", value: undefined, type: "datalist", typeData: "bool", options: { ["false"]: t("cronJobs.FilterEnabled0"), ["true"]: t("cronJobs.FilterEnabled1") }, label: "cronJobs.FilterEnabled", compareMethod: "==" },
]);
const tableauLabel = ref([
	{ label: "cronJobs.Name", sortable: true, key: "name_cronjob", valueKey: "name_cronjob", type: "text" },
	{ label: "cronJobs.Action", sortable: true, key: "action_cronjob", valueKey: "action_cronjob", type: "enum", options: cronJobActionOptions },
	{ label: "cronJobs.CronExpression", sortable: true, key: "cron_expression", valueKey: "cron_expression", type: "text" },
	{ label: "cronJobs.IsEnabled", sortable: true, key: "is_enabled", valueKey: "is_enabled", type: "enum", options: { [false]: t("cronJobs.FilterEnabled0"), [true]: t("cronJobs.FilterEnabled1") } },
	{ label: "cronJobs.LastRun", sortable: true, key: "last_run_at", valueKey: "last_run_at", type: "datetime" },
	{ label: "cronJobs.NextRun", sortable: true, key: "next_run_at", valueKey: "next_run_at", type: "datetime" },
]);
const tableauMeta = ref({
	key: "id_cronjob",
	path: "/cronjobs/",
	saveState: true,
	stateKey: "cronJobsTableState",
});
const filterReady = ref(false);
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('cronJobs.Title') }}</h2>
	</div>
	<div>
		<div :class="{
				'bg-blue-500 hover:bg-blue-600 cursor-pointer': authStore.hasPermission([2]),
				'bg-gray-400 cursor-not-allowed': !authStore.hasPermission([2])
			}"
			class="text-white px-4 py-2 rounded inline-block mb-2">
			<RouterLink v-if="authStore.hasPermission([2])" :to="'/cronjobs/new'">
				{{ $t('cronJobs.Add') }}
			</RouterLink>
			<span v-else class="pointer-events-none">
				{{ $t('cronJobs.Add') }}
			</span>
		</div>
		<FilterContainer :filters="filter" :store-data="cronJobsStore.cronJobs" @ready="filterReady = true" :save-state="true" state-key="cronJobsFilterState" />
	</div>
	<Tableau v-if="filterReady" :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[cronJobsStore.cronJobs]"
		:filters="filter"
		:loading="cronJobsStore.cronJobsLoading"
		:total-count="Number(cronJobsStore.cronJobsTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => cronJobsStore.getCronJobByInterval(limit, offset, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>

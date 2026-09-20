import { defineStore } from "pinia";

import { fetchWrapper, createMainResource } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const cronJobResource = createMainResource({
	path: () => "/cronjob",
	idField: "id_cronjob",
	stateKey: "cronJobs",
	countKey: "cronJobsTotalCount",
	loadingKey: "cronJobsLoading",
});

export const useCronJobsStore = defineStore("cronJobs", {
	state: () => ({
		cronJobsLoading: false,
		cronJobsTotalCount: 0,
		cronJobs: {},
		cronJobEdition: {},
	}),
	actions: {
		getCronJobByList: cronJobResource.getByList,
		getCronJobByInterval: cronJobResource.getByInterval,
		getCronJobById: cronJobResource.getById,
		createCronJob: cronJobResource.create,
		updateCronJob: cronJobResource.update,
		deleteCronJob: cronJobResource.remove,
		loadToEdition(id, preset = null) {
			this.cronJobEdition[id] = {};
			cronJobResource.loadEditionPreset(id, preset);
			if (id !== "new" && this.cronJobs[id]) {
				this.cronJobEdition[id] = {
					loading: false,
					name_cronjob: this.cronJobs[id].name_cronjob,
					cron_expression_cronjob: this.cronJobs[id].cron_expression_cronjob,
					action_cronjob: this.cronJobs[id].action_cronjob,
					params_cronjob: this.cronJobs[id].params_cronjob,
					is_enabled: this.cronJobs[id].is_enabled,
					last_run_at: this.cronJobs[id].last_run_at,
					next_run_at: this.cronJobs[id].next_run_at,
					status_cronjob: this.cronJobs[id].status_cronjob,
					last_error_cronjob: this.cronJobs[id].last_error_cronjob,
				};
			} else {
				this.cronJobEdition[id] = {
					loading: false,
					is_enabled: true,
				};
			}
		},
		setLoadingEdition(id, loading) {
			if (!this.cronJobEdition[id]) {
				this.cronJobEdition[id] = {};
			}
			this.cronJobEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.cronJobEdition[id];
		},

		async getCronJobStatus(id) {
			const status = await fetchWrapper.get({ url: `${baseUrl}/cronjob/${id}/status`, useToken: "access" });
			if (this.cronJobs[id]) {
				this.cronJobs[id].status_cronjob = status.status_cronjob;
				this.cronJobs[id].last_error_cronjob = status.last_error_cronjob;
				this.cronJobs[id].last_run_at = status.last_run_at;
				this.cronJobs[id].next_run_at = status.next_run_at;
			}
			return status;
		},
		async forceRunCronJob(id) {
			await fetchWrapper.post({ url: `${baseUrl}/cronjob/${id}/force-run`, useToken: "access" });
		},
		async forceStopCronJob(id) {
			await fetchWrapper.post({ url: `${baseUrl}/cronjob/${id}/force-stop`, useToken: "access" });
		},
	},
});

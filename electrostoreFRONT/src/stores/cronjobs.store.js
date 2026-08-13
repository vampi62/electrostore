import { defineStore } from "pinia";

import { createMainResource } from "@/helpers";

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
			if (preset) {
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.cronJobEdition[id][key] = value;
					}
				});
			}
			if (id !== "new" && this.cronJobs[id]) {
				this.cronJobEdition[id] = {
					loading: false,
					name_cronjob: this.cronJobs[id].name_cronjob,
					cron_expression: this.cronJobs[id].cron_expression,
					action_cronjob: this.cronJobs[id].action_cronjob,
					params_cronjob: this.cronJobs[id].params_cronjob,
					is_enabled: this.cronJobs[id].is_enabled,
					last_run_at: this.cronJobs[id].last_run_at,
					next_run_at: this.cronJobs[id].next_run_at,
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
	},
});

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
	},
});

import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useCronJobsStore = defineStore("cronJobs", {
	state: () => ({
		cronJobsLoading: false,
		cronJobsTotalCount: 0,
		cronJobs: {},
		cronJobEdition: {},
	}),
	actions: {
		async getCronJobByList(idResearch = []) {
			this.cronJobsLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const paramString = [idResearchString].join("&");
			const newCronJobList = await fetchWrapper.get({
				url: `${baseUrl}/cronjob?${paramString}`,
				useToken: "access",
			});
			for (const cronJob of newCronJobList["data"]) {
				this.cronJobs[cronJob.id_cronjob] = cronJob;
			}
			this.cronJobsLoading = false;
		},
		async getCronJobByInterval(limit = 100, offset = 0, filter = "", sort = "", clear = false) {
			this.cronJobsLoading = true;
			if (clear) {
				this.cronJobs = {};
			}
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, filterString, sortString].join("&");
			const newCronJobList = await fetchWrapper.get({
				url: `${baseUrl}/cronjob?${paramString}`,
				useToken: "access",
			});
			for (const cronJob of newCronJobList["data"]) {
				this.cronJobs[cronJob.id_cronjob] = cronJob;
			}
			this.cronJobsTotalCount = newCronJobList["pagination"]?.["total"] || 0;
			this.cronJobsLoading = false;
			return [newCronJobList["pagination"]?.["nextOffset"] || 0, newCronJobList["pagination"]?.["hasMore"] || false];
		},
		async getCronJobById(id) {
			if (!this.cronJobs[id]) {
				this.cronJobs[id] = {};
			}
			this.cronJobs[id].loading = true;
			this.cronJobs[id] = await fetchWrapper.get({
				url: `${baseUrl}/cronjob/${id}`,
				useToken: "access",
			});
		},
		async createCronJob(params) {
			const cronJob = await fetchWrapper.post({
				url: `${baseUrl}/cronjob`,
				useToken: "access",
				body: params,
			});
			this.cronJobs[cronJob.id_cronjob] = cronJob;
			return cronJob.id_cronjob;
		},
		async updateCronJob(id, params) {
			this.cronJobs[id] = await fetchWrapper.put({
				url: `${baseUrl}/cronjob/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteCronJob(id) {
			await fetchWrapper.delete({
				url: `${baseUrl}/cronjob/${id}`,
				useToken: "access",
			});
			delete this.cronJobs[id];
		},
	},
});

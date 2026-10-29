import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useProjectsStore } from "@/stores";

import type { Ready, Edition } from "@/types/resource";
import type { components } from "@/types/api";

type ReadProjectItemDto = components["schemas"]["ReadProjectItemDto"];
type ReadExtendedProjectItemDto = components["schemas"]["ReadExtendedProjectItemDto"];
type CreateProjectItemByItemDto = components["schemas"]["CreateProjectItemByItemDto"];
type UpdateProjectItemDto = components["schemas"]["UpdateProjectItemDto"];

const itemProjectResource = createResource<{ readExtended: ReadExtendedProjectItemDto, readBasic: ReadProjectItemDto, create: CreateProjectItemByItemDto, update: UpdateProjectItemDto, createBulk: CreateProjectItemByItemDto[] }>({
	compositeKey: ["id_item", "id_project"],
	path: [{ keys: ["id_item"], url: ({ id_item }) => `/item/${id_item}/project` }],
	entityPath: [{ keys: ["id_item", "id_project"], url: ({ id_item, id_project }) => `/item/${id_item}/project/${id_project}` }],
	stateKey: "itemProjects",
	countKey: "itemProjectsTotalCount",
	loadingKey: "itemProjectsLoading",
	editionKey: "itemProjectEdition",
	readyKey: "itemProjectReady",
	onHydrate: (store, entity: ReadExtendedProjectItemDto, expand) => {
		if (expand.includes("project") && entity.project && entity.id_project) {
			const projectsStore = useProjectsStore();
			projectsStore.projects[entity.id_project] = entity.project;
		}
	},
});

// Records are indexed by the composite key "<id_item>:<id_project>"
export const useItemsProjectsStore = defineStore("itemsProjects", {
	state: () => ({
		itemProjectsLoading: false,
		// indexed by scope: "id_item=3"
		itemProjectsTotalCount: {} as Record<string, number>,
		itemProjects: {} as Record<string, ReadProjectItemDto>,
		itemProjectEdition: {} as Record<string, Edition<{ create: CreateProjectItemByItemDto, update: UpdateProjectItemDto }>>,
		itemProjectReady: {} as Record<string, Ready<{ create: CreateProjectItemByItemDto, update: UpdateProjectItemDto }>>,
	}),
	getters: {
		// the records of an item, indexed by id_project
		itemProjectsByItem: (state) => (idItem: string) => itemProjectResource.slice(state.itemProjects, { id_item: idItem }),
		itemProjectEditionByItem: (state) => (idItem: string) => itemProjectResource.slice(state.itemProjectEdition, { id_item: idItem }),
		itemProjectReadyByItem: (state) => (idItem: string) => itemProjectResource.slice(state.itemProjectReady, { id_item: idItem }),
		itemProjectsCountByItem: (state) => (idItem: string) => state.itemProjectsTotalCount[itemProjectResource.toScopeKey({ id_item: idItem })] ?? 0,
	},
	actions: {
		getItemProjectByInterval: itemProjectResource.getByInterval,
		getItemProjectById: itemProjectResource.getById,
		createItemProject: itemProjectResource.create,
		updateItemProject: itemProjectResource.update,
		deleteItemProject: itemProjectResource.remove,
		createItemProjectBulk: itemProjectResource.createBulk,
		hydrateItemProjects: itemProjectResource.hydrate,
		setItemProjectEdition: itemProjectResource.setEdition,
		removeItemProjectEdition: itemProjectResource.removeEdition,
		removeItemProjectReady: itemProjectResource.removeReady,
		setItemProjectStagedSlice: itemProjectResource.setStagedSlice,
		valideItemProjectEditionByKey: itemProjectResource.valideEditionByKey,
		copyItemProjectByKeyPart: itemProjectResource.copyByKeyPart,
		clearItemProjectEdition: itemProjectResource.clearEdition,
		pushItemProjectChange: itemProjectResource.pushChange,
	},
});

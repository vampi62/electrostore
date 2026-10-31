import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useProjectsStore, useItemsStore } from "@/stores";

import type { Edition, Ready } from "@/types/resource";
import type { components } from "@/types/api";

type ReadProjectItemDto = components["schemas"]["ReadProjectItemDto"];
type ReadExtendedProjectItemDto = components["schemas"]["ReadExtendedProjectItemDto"];
type CreateProjectItemByItemDto = components["schemas"]["CreateProjectItemByItemDto"];
type CreateProjectItemByProjectDto = components["schemas"]["CreateProjectItemByProjectDto"];
type UpdateProjectItemDto = components["schemas"]["UpdateProjectItemDto"];
type CreateItemProject = CreateProjectItemByItemDto | CreateProjectItemByProjectDto;

const itemProjectResource = createResource<{ readExtended: ReadExtendedProjectItemDto, readBasic: ReadProjectItemDto, create: CreateItemProject, update: UpdateProjectItemDto, createBulk: CreateItemProject[] }>({
	compositeKey: ["id_item", "id_project"],
	path: [
		{ keys: ["id_item"], url: ({ id_item }) => `/item/${id_item}/project` },
		{ keys: ["id_project"], url: ({ id_project }) => `/project/${id_project}/item` },
	],
	entityPath: [
		{ keys: ["id_item", "id_project"], url: ({ id_item, id_project }) => `/item/${id_item}/project/${id_project}` },
	],
	stateKey: "itemProjects",
	countKey: "itemProjectsTotalCount",
	loadingKey: "itemProjectsLoading",
	editionKey: "itemProjectEdition",
	readyKey: "itemProjectReady",
	onHydrate: (store, entity: ReadExtendedProjectItemDto, expand) => {
		if (expand.includes("project") && entity.project && entity.id_project) {
			useProjectsStore().projects[entity.id_project] = entity.project;
		}
		if (expand.includes("item") && entity.item && entity.id_item) {
			useItemsStore().items[entity.id_item] = entity.item;
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
		itemProjectEdition: {} as Record<string, Edition<{ create: CreateItemProject, update: UpdateProjectItemDto }>>,
		itemProjectReady: {} as Record<string, Ready<{ create: CreateItemProject, update: UpdateProjectItemDto }>>,
	}),
	getters: {
		// the records of a item, indexed by the rest of the key
		itemProjectsByItem: (state) => (id: string) => itemProjectResource.slice(state.itemProjects, { id_item: id }),
		itemProjectsCountByItem: (state) => (id: string) => state.itemProjectsTotalCount[itemProjectResource.toScopeKey({ id_item: id })] ?? 0,
		itemProjectsGroupedByItem: (state) => itemProjectResource.group(state.itemProjects, "id_item"),
		itemProjectEditionByItem: (state) => (id: string) => itemProjectResource.live(state.itemProjectEdition, { id_item: id }),
		itemProjectReadyByItem: (state) => (id: string) => itemProjectResource.live(state.itemProjectReady, { id_item: id }),
		// the records of a project, indexed by the rest of the key
		itemProjectsByProject: (state) => (id: string) => itemProjectResource.slice(state.itemProjects, { id_project: id }),
		itemProjectsCountByProject: (state) => (id: string) => state.itemProjectsTotalCount[itemProjectResource.toScopeKey({ id_project: id })] ?? 0,
		itemProjectsGroupedByProject: (state) => itemProjectResource.group(state.itemProjects, "id_project"),
		itemProjectEditionByProject: (state) => (id: string) => itemProjectResource.live(state.itemProjectEdition, { id_project: id }),
		itemProjectReadyByProject: (state) => (id: string) => itemProjectResource.live(state.itemProjectReady, { id_project: id }),
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

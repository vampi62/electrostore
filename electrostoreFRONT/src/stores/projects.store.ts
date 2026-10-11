import { defineStore } from "pinia";

import { fetchWrapper, createMainResource, createNestedResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useUsersStore, useItemsStore, useProjectTagsStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Ready, Edition } from "@/types/resource";
import type { components, paths } from "@/types/api";
type ReadProjectDto = components["schemas"]["ReadProjectDto"];
type ReadExtendedProjectDto = components["schemas"]["ReadExtendedProjectDto"];
type CreateProjectDto = components["schemas"]["CreateProjectDto"];
type UpdateProjectDto = components["schemas"]["UpdateProjectDto"];

type ReadProjectCommentDto = components["schemas"]["ReadProjectCommentDto"];
type ReadExtendedProjectCommentDto = components["schemas"]["ReadExtendedProjectCommentDto"];
type CreateProjectCommentByProjectDto = components["schemas"]["CreateProjectCommentByProjectDto"];
type UpdateProjectCommentDto = components["schemas"]["UpdateProjectCommentDto"];

type ReadProjectDocumentDto = components["schemas"]["ReadProjectDocumentDto"];
type CreateProjectDocumentByProjectDto = NonNullable<paths["/api/project/{id_project}/document"]["post"]["requestBody"]>["content"]["multipart/form-data"];
type UpdateProjectDocumentDto = components["schemas"]["UpdateProjectDocumentDto"];

type ReadProjectItemDto = components["schemas"]["ReadProjectItemDto"];
type ReadExtendedProjectItemDto = components["schemas"]["ReadExtendedProjectItemDto"];
type CreateProjectItemByProjectDto = components["schemas"]["CreateProjectItemByProjectDto"];
type UpdateProjectItemDto = components["schemas"]["UpdateProjectItemDto"];

type ReadProjectProjectTagDto = components["schemas"]["ReadProjectProjectTagDto"];
type ReadExtendedProjectProjectTagDto = components["schemas"]["ReadExtendedProjectProjectTagDto"];
type CreateProjectProjectTagByProjectDto = components["schemas"]["CreateProjectProjectTagByProjectDto"];

type ReadProjectStatusDto = components["schemas"]["ReadProjectStatusDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

type ExpandData = {
	project_comments: ReadProjectCommentDto[];
	project_documents: ReadProjectDocumentDto[];
	project_items: ReadProjectItemDto[];
	project_tags: ReadProjectProjectTagDto[];
	project_status_history: ReadProjectStatusDto[];
};

const EXPAND_HANDLERS: { [K in keyof ExpandData]: (store: StoreGeneric, idProject: string, data: ExpandData[K]) => void } = {
	project_comments: (store, idProject, data) => {
		store.comments[idProject] = {};
		for (const comment of data) {
			store.comments[idProject][comment.id_project_comment] = comment;
		}
	},
	project_documents: (store, idProject, data) => {
		store.documents[idProject] = {};
		for (const document of data) {
			store.documents[idProject][document.id_project_document] = document;
		}
	},
	project_items: (store, idProject, data) => {
		store.items[idProject] = {};
		for (const item of data) {
			store.items[idProject][item.id_item] = item;
		}
	},
	project_tags: (store, idProject, data) => {
		store.projectTagProject[idProject] = {};
		for (const projectTagProject of data) {
			store.projectTagProject[idProject][projectTagProject.id_project_tag] = projectTagProject;
		}
	},
	project_status_history: (store, idProject, data) => {
		store.statusHistory[idProject] = {};
		for (const statusHistory of data) {
			store.statusHistory[idProject][statusHistory.id_project_status] = statusHistory;
		}
	},
};

function hydrateProject(store: StoreGeneric, idProject: string, project: ReadExtendedProjectDto, expand: string[] = []) {
	store.commentsTotalCount[idProject] = project.project_comments_count;
	store.documentsTotalCount[idProject] = project.project_documents_count;
	store.itemsTotalCount[idProject] = project.project_items_count;
	store.projectTagProjectTotalCount[idProject] = project.project_tags_count;
	store.statusHistoryTotalCount[idProject] = project.project_status_history_count;
	for (const key of expand) {
		if (key in EXPAND_HANDLERS) {
			const expandKey = key as keyof ExpandData;
			const handler = EXPAND_HANDLERS[expandKey] as (store: StoreGeneric, idProject: string, data: unknown[]) => void;
			handler(store, idProject, project[expandKey] ?? []);
		}
	}
}

const projectResource = createMainResource<{ readExtended: ReadExtendedProjectDto, readBasic: ReadProjectDto, create: CreateProjectDto, update: UpdateProjectDto }>({
	path: () => "/project",
	idField: "id_project",
	stateKey: "projects",
	countKey: "projectsTotalCount",
	loadingKey: "projectsLoading",
	editionKey: "projectEdition",
	onHydrate: (store, entity: ReadExtendedProjectDto, expand) => {
		if (entity.id_project === undefined) {
			return;
		}
		hydrateProject(store, String(entity.id_project), entity, expand);
	},
});

const commentResource = createNestedResource<{ readExtended: ReadExtendedProjectCommentDto, readBasic: ReadProjectCommentDto, create: CreateProjectCommentByProjectDto, update: UpdateProjectCommentDto }>({
	path: (idProject) => `/project/${idProject}/comment`,
	idField: "id_project_comment",
	stateKey: "comments",
	countKey: "commentsTotalCount",
	loadingKey: "commentsLoading",
	editionKey: "commentEdition",
	onHydrate: (store, entity: ReadExtendedProjectCommentDto, expand) => {
		if (expand.includes("user") && entity.user && entity.id_user) {
			const usersStore = useUsersStore();
			usersStore.users[entity.id_user] = entity.user;
		}
	},
});
const documentResource = createNestedResource<{ readBasic: ReadProjectDocumentDto, create: CreateProjectDocumentByProjectDto, update: UpdateProjectDocumentDto }>({
	path: (idProject) => `/project/${idProject}/document`,
	idField: "id_project_document",
	stateKey: "documents",
	countKey: "documentsTotalCount",
	loadingKey: "documentsLoading",
	editionKey: "documentEdition",
	readyKey: "documentReady",
});
const itemResource = createNestedResource<{ readExtended: ReadExtendedProjectItemDto, readBasic: ReadProjectItemDto, create: CreateProjectItemByProjectDto, update: UpdateProjectItemDto }>({
	path: (idProject) => `/project/${idProject}/item`,
	idField: "id_item",
	stateKey: "items",
	countKey: "itemsTotalCount",
	loadingKey: "itemsLoading",
	editionKey: "itemEdition",
	readyKey: "itemReady",
	onHydrate: (store, entity: ReadExtendedProjectItemDto, expand) => {
		if (expand.includes("item") && entity.item && entity.id_item) {
			const itemsStore = useItemsStore();
			itemsStore.items[entity.id_item] = entity.item;
		}
	},
});
const projectTagProjectResource = createNestedResource<{ readExtended: ReadExtendedProjectProjectTagDto, readBasic: ReadProjectProjectTagDto, create: CreateProjectProjectTagByProjectDto }>({
	path: (idProject) => `/project/${idProject}/project-tag`,
	idField: "id_project_tag",
	stateKey: "projectTagProject",
	countKey: "projectTagProjectTotalCount",
	loadingKey: "projectTagProjectLoading",
	editionKey: "projectTagProjectEdition",
	readyKey: "projectTagProjectReady",
	onHydrate: (store, entity: ReadExtendedProjectProjectTagDto, expand) => {
		if (expand.includes("project_tag") && entity.project_tag && entity.id_project_tag) {
			const projectTagsStore = useProjectTagsStore();
			projectTagsStore.projectTags[entity.id_project_tag] = entity.project_tag;
		}
	},
});
const statusHistoryResource = createNestedResource<{ readBasic: ReadProjectStatusDto }>({
	path: (idProject) => `/project/${idProject}/status-history`,
	idField: "id_project_status",
	stateKey: "statusHistory",
	countKey: "statusHistoryTotalCount",
	loadingKey: "statusHistoryLoading",
});

export const useProjectsStore = defineStore("projects",{
	state: () => ({
		projectsLoading: false,
		projectsTotalCount: 0,
		projects: {} as Record<string, ReadProjectDto>,
		projectEdition: {} as Record<string, Edition<{ create: CreateProjectDto, update: UpdateProjectDto }>>,

		commentsLoading: false,
		commentsTotalCount: {} as Record<string, number>,
		comments: {} as Record<string, Record<string, ReadProjectCommentDto>>,
		commentEdition: {} as Record<string, Record<string, Edition<{ create: CreateProjectCommentByProjectDto, update: UpdateProjectCommentDto }>>>,

		documentsLoading: false,
		documentsTotalCount: {} as Record<string, number>,
		documents: {} as Record<string, Record<string, ReadProjectDocumentDto>>,
		documentEdition: {} as Record<string, Record<string, Edition<{ create: CreateProjectDocumentByProjectDto, update: UpdateProjectDocumentDto }>>>,
		documentReady: {} as Record<string, Record<string, Ready<{ create: CreateProjectDocumentByProjectDto, update: UpdateProjectDocumentDto }>>>,

		itemsLoading: false,
		itemsTotalCount: {} as Record<string, number>,
		items: {} as Record<string, Record<string, ReadProjectItemDto>>,
		itemEdition: {} as Record<string, Edition<{ create: CreateProjectItemByProjectDto, update: UpdateProjectItemDto }>>,
		itemReady: {} as Record<string, Record<string, Ready<{ create: CreateProjectItemByProjectDto, update: UpdateProjectItemDto }>>>,

		projectTagProjectLoading: false,
		projectTagProjectTotalCount: {} as Record<string, number>,
		projectTagProject: {} as Record<string, Record<string, ReadProjectProjectTagDto>>,
		projectTagProjectEdition: {} as Record<string, Record<string, Edition<{ create: CreateProjectProjectTagByProjectDto }>>>,
		projectTagProjectReady: {} as Record<string, Record<string, Ready<{ create: CreateProjectProjectTagByProjectDto }>>>,

		statusHistoryTotalCount: {} as Record<string, number>,
		statusHistoryLoading: false,
		statusHistory: {} as Record<string, Record<string, ReadProjectStatusDto>>,
	}),
	actions: {
		getProjectByList: projectResource.getByList,
		getProjectByInterval: projectResource.getByInterval,
		getProjectById: projectResource.getById,
		createProject: projectResource.create,
		getAvailableNewProjectId: projectResource.getAvailableNewId,
		updateProject: projectResource.update,
		deleteProject: projectResource.remove,
		loadToEdition(id: string, preset: string | null = null) {
			if (!isNewId(id) && this.projects[id]) {
				this.projectEdition[id] = {
					loading: false,
					name_project: this.projects[id].name_project,
					description_project: this.projects[id].description_project,
					url_project: this.projects[id].url_project,
					status_project: this.projects[id].status_project,
					date_start_project: this.projects[id].date_start_project,
					date_end_project: this.projects[id].date_end_project,
				};
			} else {
				this.projectEdition[id] = {
					loading: false,
				};
				projectResource.loadEditionPreset.call(this, id, preset);
			}
			this.commentEdition[id] = {};
			this.documentEdition[id] = {};
			this.documentReady[id] = {};
			this.itemEdition[id] = {};
			this.itemReady[id] = {};
			this.projectTagProjectEdition[id] = {};
			this.projectTagProjectReady[id] = {};
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.projectEdition[id]) {
				this.projectEdition[id] = {};
			}
			this.projectEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.projectEdition[id];
			delete this.commentEdition[id];
			delete this.documentEdition[id];
			delete this.documentReady[id];
			delete this.itemEdition[id];
			delete this.itemReady[id];
			delete this.projectTagProjectEdition[id];
			delete this.projectTagProjectReady[id];
		},
		async saveAllChanges(id: string) {
			let realId = id;
			if (isNewId(id)) {
				realId = await this.createProject(this.projectEdition[id] as CreateProjectDto);
				this.copyDocumentAllId(id, realId);
				this.copyItemAllId(id, realId);
				this.copyProjectTagProjectAllId(id, realId);
			} else {
				await this.updateProject(realId, this.projectEdition[id]);
			}
			await Promise.all([
				this.pushDocumentChange(realId),
				this.pushItemChange(realId),
				this.pushProjectTagProjectChange(realId),
			]);
			return realId;
		},

		getCommentByInterval: commentResource.getByInterval,
		getCommentById: commentResource.getById,
		createComment: commentResource.create,
		updateComment: commentResource.update,
		deleteComment: commentResource.remove,

		getDocumentByInterval: documentResource.getByInterval,
		getDocumentById: documentResource.getById,
		createDocument: documentResource.create,
		updateDocument: documentResource.update,
		deleteDocument: documentResource.remove,
		getAvailableNewDocumentId: documentResource.getAvailableNewId,
		valideDocumentEditionById: documentResource.valideEditionById,
		copyDocumentPerId: documentResource.copyPerId,
		copyDocumentAllId: documentResource.copyAllId,
		pushDocumentChange: documentResource.pushChange,
		async downloadDocument(idProject: string, id: string) {
			return await fetchWrapper.image({
				url: `${baseUrl}/project/${idProject}/document/${id}/download`,
				useToken: "access",
			});
		},
		
		getItemByInterval: itemResource.getByInterval,
		getItemById: itemResource.getById,
		createItem: itemResource.create,
		updateItem: itemResource.update,
		deleteItem: itemResource.remove,
		createItemBulk: itemResource.createBulk,
		getAvailableNewItemId: itemResource.getAvailableNewId,
		valideItemEditionById: itemResource.valideEditionById,
		copyItemPerId: itemResource.copyPerId,
		copyItemAllId: itemResource.copyAllId,
		pushItemChange: itemResource.pushChange,
		
		getProjectTagProjectByInterval: projectTagProjectResource.getByInterval,
		getProjectTagProjectById: projectTagProjectResource.getById,
		createProjectTagProject: projectTagProjectResource.create,
		deleteProjectTagProject: projectTagProjectResource.remove,
		createProjectTagProjectBulk: projectTagProjectResource.createBulk,
		deleteProjectTagProjectBulk: projectTagProjectResource.removeBulk,
		getAvailableNewProjectTagProjectId: projectTagProjectResource.getAvailableNewId,
		valideProjectTagProjectEditionById: projectTagProjectResource.valideEditionById,
		copyProjectTagProjectPerId: projectTagProjectResource.copyPerId,
		copyProjectTagProjectAllId: projectTagProjectResource.copyAllId,
		pushProjectTagProjectChange: projectTagProjectResource.pushChange,

		getStatusHistoryByInterval: statusHistoryResource.getByInterval,
		getStatusHistoryById: statusHistoryResource.getById,
	},
});

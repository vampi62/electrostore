import { defineStore } from "pinia";

import { createResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useProjectsCommentsStore, useProjectsDocumentsStore, useProjectsStatusStore, useProjectsTagsStore, useItemsProjectsStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Edition } from "@/types/resource";
import type { components } from "@/types/api";
type ReadProjectDto = components["schemas"]["ReadProjectDto"];
type ReadExtendedProjectDto = components["schemas"]["ReadExtendedProjectDto"];
type CreateProjectDto = components["schemas"]["CreateProjectDto"];
type UpdateProjectDto = components["schemas"]["UpdateProjectDto"];

// What an expanded project brings along is handed to the store of each nested resource
const EXPAND_HANDLERS: Record<string, (idProject: string, data: ReadExtendedProjectDto) => void> = {
	project_comments: (idProject, data) => useProjectsCommentsStore().hydrateProjectComments({ id_project: idProject }, data.project_comments ?? []),
	project_documents: (idProject, data) => useProjectsDocumentsStore().hydrateProjectDocuments({ id_project: idProject }, data.project_documents ?? []),
	project_items: (idProject, data) => useItemsProjectsStore().hydrateItemProjects({ id_project: idProject }, data.project_items ?? []),
	project_tags: (idProject, data) => useProjectsTagsStore().hydrateProjectProjectTags({ id_project: idProject }, data.project_tags ?? []),
	project_status_history: (idProject, data) => useProjectsStatusStore().hydrateProjectStatus({ id_project: idProject }, data.project_status_history ?? []),
};

function hydrateProject(idProject: string, project: ReadExtendedProjectDto, expand: string[] = []) {
	const scope = { id_project: idProject };
	useProjectsCommentsStore().hydrateProjectComments(scope, undefined, project.project_comments_count);
	useProjectsDocumentsStore().hydrateProjectDocuments(scope, undefined, project.project_documents_count);
	useItemsProjectsStore().hydrateItemProjects(scope, undefined, project.project_items_count);
	useProjectsTagsStore().hydrateProjectProjectTags(scope, undefined, project.project_tags_count);
	useProjectsStatusStore().hydrateProjectStatus(scope, undefined, project.project_status_history_count);
	for (const key of expand) {
		EXPAND_HANDLERS[key]?.(idProject, project);
	}
}

const projectResource = createResource<{ readExtended: ReadExtendedProjectDto, readBasic: ReadProjectDto, create: CreateProjectDto, update: UpdateProjectDto }>({
	compositeKey: ["id_project"],
	generatedField: "id_project",
	path: [{ keys: [], url: () => "/project" }],
	entityPath: [{ keys: ["id_project"], url: ({ id_project }) => `/project/${id_project}` }],
	stateKey: "projects",
	countKey: "projectsTotalCount",
	loadingKey: "projectsLoading",
	editionKey: "projectEdition",
	onHydrate: (store: StoreGeneric, entity: ReadExtendedProjectDto, expand) => {
		if (entity.id_project === undefined) {
			return;
		}
		hydrateProject(String(entity.id_project), entity, expand);
	},
});

// Discards what is staged for the nested resources of a project; a comment being edited is saved on its own, saving the element must not discard it
function clearNestedEdition(idProject: string) {
	const scope = { id_project: idProject };
	useProjectsDocumentsStore().clearProjectDocumentEdition(scope);
	useItemsProjectsStore().clearItemProjectEdition(scope);
	useProjectsTagsStore().clearProjectProjectTagEdition(scope);
}

export const useProjectsStore = defineStore("projects", {
	state: () => ({
		projectsLoading: false,
		projectsTotalCount: 0,
		projects: {} as Record<string, ReadProjectDto>,
		projectEdition: {} as Record<string, Edition<{ create: CreateProjectDto, update: UpdateProjectDto }>>,
	}),
	actions: {
		getProjectByInterval: projectResource.single.getByInterval,
		getProjectById: projectResource.single.getById,
		createProject: projectResource.single.create,
		getAvailableNewProjectId: projectResource.single.getAvailableNewId,
		updateProject: projectResource.single.update,
		deleteProject: projectResource.single.remove,
		loadToEdition(id: string, preset: string | null = null) {
			if (!isNewId(id) && this.projects[id]) {
				this.projectEdition[id] = {
					loading: false,
					name_project: this.projects[id].name_project ?? undefined,
					description_project: this.projects[id].description_project,
					url_project: this.projects[id].url_project,
					status_project: this.projects[id].status_project,
				};
			} else {
				this.projectEdition[id] = {
					loading: false,
				};
				projectResource.single.loadEditionPreset.call(this, id, preset);
			}
			clearNestedEdition(id);
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.projectEdition[id]) {
				this.projectEdition[id] = {};
			}
			this.projectEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.projectEdition[id];
			clearNestedEdition(id);
			useProjectsCommentsStore().clearProjectCommentEdition({ id_project: id });
		},
		async saveAllChanges(id: string) {
			let realId = id;
			const documentsStore = useProjectsDocumentsStore();
			const itemsStore = useItemsProjectsStore();
			const tagsStore = useProjectsTagsStore();
			if (isNewId(id)) {
				realId = await this.createProject(this.projectEdition[id] as CreateProjectDto);
				// what was staged for the new project now belongs to the saved one
				documentsStore.copyProjectDocumentByKeyPart("id_project", id, realId);
				itemsStore.copyItemProjectByKeyPart("id_project", id, realId);
				tagsStore.copyProjectProjectTagByKeyPart("id_project", id, realId);
			} else {
				await this.updateProject(realId, this.projectEdition[id]);
			}
			const scope = { id_project: realId };
			await Promise.all([
				documentsStore.pushProjectDocumentChange(scope),
				itemsStore.pushItemProjectChange(scope),
				tagsStore.pushProjectProjectTagChange(scope),
			]);
			return realId;
		},
	},
});

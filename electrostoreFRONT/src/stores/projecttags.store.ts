import { defineStore } from "pinia";

import { createResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useProjectsTagsStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Edition } from "@/types/resource";
import type { components } from "@/types/api";
type ReadProjectTagDto = components["schemas"]["ReadProjectTagDto"];
type ReadExtendedProjectTagDto = components["schemas"]["ReadExtendedProjectTagDto"];
type CreateProjectTagDto = components["schemas"]["CreateProjectTagDto"];
type UpdateProjectTagDto = components["schemas"]["UpdateProjectTagDto"];

function hydrateProjectTag(idProjectTag: string, projectTag: ReadExtendedProjectTagDto, expand: string[] = []) {
	const scope = { id_project_tag: idProjectTag };
	const projectsTagsStore = useProjectsTagsStore();
	projectsTagsStore.hydrateProjectProjectTags(scope, undefined, projectTag.project_tags_count);
	if (expand.includes("project_tags")) {
		projectsTagsStore.hydrateProjectProjectTags(scope, projectTag.project_tags ?? []);
	}
}

const projectTagResource = createResource<{ readExtended: ReadExtendedProjectTagDto, readBasic: ReadProjectTagDto, create: CreateProjectTagDto, update: UpdateProjectTagDto, createBulk: CreateProjectTagDto[] }>({
	compositeKey: ["id_project_tag"],
	generatedField: "id_project_tag",
	path: [{ keys: [], url: () => "/project-tag" }],
	entityPath: [{ keys: ["id_project_tag"], url: ({ id_project_tag }) => `/project-tag/${id_project_tag}` }],
	stateKey: "projectTags",
	countKey: "projectTagsTotalCount",
	loadingKey: "projectTagsLoading",
	editionKey: "projectTagEdition",
	onHydrate: (store: StoreGeneric, entity: ReadExtendedProjectTagDto, expand) => {
		if (entity.id_project_tag === undefined) {
			return;
		}
		hydrateProjectTag(String(entity.id_project_tag), entity, expand);
	},
});

export const useProjectTagsStore = defineStore("projectTags", {
	state: () => ({
		projectTagsLoading: false,
		projectTagsTotalCount: 0,
		projectTags: {} as Record<string, ReadProjectTagDto>,
		projectTagEdition: {} as Record<string, Edition<{ create: CreateProjectTagDto, update: UpdateProjectTagDto }>>,
	}),
	actions: {
		getProjectTagByInterval: projectTagResource.single.getByInterval,
		getProjectTagById: projectTagResource.single.getById,
		createProjectTag: projectTagResource.single.create,
		getAvailableNewProjectTagId: projectTagResource.single.getAvailableNewId,
		updateProjectTag: projectTagResource.single.update,
		deleteProjectTag: projectTagResource.single.remove,
		createProjectTagBulk: projectTagResource.single.createBulk,
		loadToEdition(id: string, preset: string | null = null) {
			if (!isNewId(id) && this.projectTags[id]) {
				this.projectTagEdition[id] = {
					loading: false,
					name_project_tag: this.projectTags[id].name_project_tag ?? undefined,
					weight_project_tag: this.projectTags[id].weight_project_tag,
				};
			} else {
				this.projectTagEdition[id] = {
					loading: false,
				};
				projectTagResource.single.loadEditionPreset.call(this, id, preset);
			}
			useProjectsTagsStore().clearProjectProjectTagEdition({ id_project_tag: id });
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.projectTagEdition[id]) {
				this.projectTagEdition[id] = {};
			}
			this.projectTagEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.projectTagEdition[id];
			useProjectsTagsStore().clearProjectProjectTagEdition({ id_project_tag: id });
		},
		async saveAllChanges(id: string) {
			let realId = id;
			const projectsTagsStore = useProjectsTagsStore();
			if (isNewId(id)) {
				realId = await this.createProjectTag(this.projectTagEdition[id] as CreateProjectTagDto);
				// what was staged for the new tag now belongs to the saved one
				projectsTagsStore.copyProjectProjectTagByKeyPart("id_project_tag", id, realId);
			} else {
				await this.updateProjectTag(realId, this.projectTagEdition[id]);
			}
			await projectsTagsStore.pushProjectProjectTagChange({ id_project_tag: realId });
			await this.getProjectTagById(realId, ["project_tags"]);
			return realId;
		},
	},
});

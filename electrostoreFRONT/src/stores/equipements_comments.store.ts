import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useUsersStore, useEquipementsStore } from "@/stores";

import type { Edition } from "@/types/resource";
import type { components } from "@/types/api";

type ReadEquipementCommentDto = components["schemas"]["ReadEquipementCommentDto"];
type ReadExtendedEquipementCommentDto = components["schemas"]["ReadExtendedEquipementCommentDto"];
type CreateEquipementCommentByEquipementDto = components["schemas"]["CreateEquipementCommentByEquipementDto"];
type UpdateEquipementCommentDto = components["schemas"]["UpdateEquipementCommentDto"];

const equipementCommentResource = createResource<{ readExtended: ReadExtendedEquipementCommentDto, readBasic: ReadEquipementCommentDto, create: CreateEquipementCommentByEquipementDto, update: UpdateEquipementCommentDto }>({
	compositeKey: ["id_equipement", "id_equipement_comment"],
	generatedField: "id_equipement_comment",
	path: [
		{ keys: ["id_equipement"], url: ({ id_equipement }) => `/equipement/${id_equipement}/comment` },
		{ keys: ["id_user"], url: ({ id_user }) => `/user/${id_user}/equipement_comment` },
	],
	entityPath: [
		{ keys: ["id_equipement", "id_equipement_comment"], url: ({ id_equipement, id_equipement_comment }) => `/equipement/${id_equipement}/comment/${id_equipement_comment}` },
	],
	stateKey: "equipementComments",
	countKey: "equipementCommentsTotalCount",
	loadingKey: "equipementCommentsLoading",
	editionKey: "equipementCommentEdition",
	onHydrate: (store, entity: ReadExtendedEquipementCommentDto, expand) => {
		if (expand.includes("user") && entity.user && entity.id_user) {
			useUsersStore().users[entity.id_user] = entity.user;
		}
		if (expand.includes("equipement") && entity.equipement?.id_equipement) {
			useEquipementsStore().equipements[entity.equipement.id_equipement] = entity.equipement;
		}
	},
});

// Records are indexed by the composite key "<id_equipement>:<id_equipement_comment>"
export const useEquipementsCommentsStore = defineStore("equipementsComments", {
	state: () => ({
		equipementCommentsLoading: false,
		// indexed by scope: "id_equipement=3"
		equipementCommentsTotalCount: {} as Record<string, number>,
		equipementComments: {} as Record<string, ReadEquipementCommentDto>,
		equipementCommentEdition: {} as Record<string, Edition<{ create: CreateEquipementCommentByEquipementDto, update: UpdateEquipementCommentDto }>>,
	}),
	getters: {
		// the records of a equipement, indexed by the rest of the key
		equipementCommentsByEquipement: (state) => (id: string) => equipementCommentResource.slice(state.equipementComments, { id_equipement: id }),
		equipementCommentsCountByEquipement: (state) => (id: string) => state.equipementCommentsTotalCount[equipementCommentResource.toScopeKey({ id_equipement: id })] ?? 0,
		equipementCommentEditionByEquipement: (state) => (id: string) => equipementCommentResource.live(state.equipementCommentEdition, { id_equipement: id }),
		// the records loaded for a user (a field of the record, not of the key), indexed by id_equipement_comment
		equipementCommentsByUser: (state) => (id: string) => Object.fromEntries(Object.entries(state.equipementComments).filter(([, record]) => String(record.id_user) === id).map(([, record]) => [String(record.id_equipement_comment), record])),
		equipementCommentsCountByUser: (state) => (id: string) => state.equipementCommentsTotalCount[equipementCommentResource.toScopeKey({ id_user: id })] ?? 0,
	},
	actions: {
		getEquipementCommentByInterval: equipementCommentResource.getByInterval,
		getEquipementCommentById: equipementCommentResource.getById,
		createEquipementComment: equipementCommentResource.create,
		updateEquipementComment: equipementCommentResource.update,
		deleteEquipementComment: equipementCommentResource.remove,
		hydrateEquipementComments: equipementCommentResource.hydrate,
		getAvailableNewEquipementCommentId: equipementCommentResource.getAvailableNewId,
		setEquipementCommentEdition: equipementCommentResource.setEdition,
		removeEquipementCommentEdition: equipementCommentResource.removeEdition,
		clearEquipementCommentEdition: equipementCommentResource.clearEdition,
	},
});

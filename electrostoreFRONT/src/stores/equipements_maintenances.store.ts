import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useUsersStore } from "@/stores";

import type { Edition, Ready } from "@/types/resource";
import type { components } from "@/types/api";

type ReadEquipementMaintenanceDto = components["schemas"]["ReadEquipementMaintenanceDto"];
type ReadExtendedEquipementMaintenanceDto = components["schemas"]["ReadExtendedEquipementMaintenanceDto"];
type CreateEquipementMaintenanceByEquipementDto = components["schemas"]["CreateEquipementMaintenanceByEquipementDto"];
type UpdateEquipementMaintenanceDto = components["schemas"]["UpdateEquipementMaintenanceDto"];

const equipementMaintenanceResource = createResource<{ readExtended: ReadExtendedEquipementMaintenanceDto, readBasic: ReadEquipementMaintenanceDto, create: CreateEquipementMaintenanceByEquipementDto, update: UpdateEquipementMaintenanceDto }>({
	compositeKey: ["id_equipement", "id_equipement_maintenance"],
	generatedField: "id_equipement_maintenance",
	path: [
		{ keys: ["id_equipement"], url: ({ id_equipement }) => `/equipement/${id_equipement}/maintenance` },
	],
	entityPath: [
		{ keys: ["id_equipement", "id_equipement_maintenance"], url: ({ id_equipement, id_equipement_maintenance }) => `/equipement/${id_equipement}/maintenance/${id_equipement_maintenance}` },
	],
	stateKey: "equipementMaintenances",
	countKey: "equipementMaintenancesTotalCount",
	loadingKey: "equipementMaintenancesLoading",
	editionKey: "equipementMaintenanceEdition",
	readyKey: "equipementMaintenanceReady",
	onHydrate: (store, entity: ReadExtendedEquipementMaintenanceDto, expand) => {
		if (expand.includes("user") && entity.user && entity.id_user) {
			useUsersStore().users[entity.id_user] = entity.user;
		}
	},
});

// Records are indexed by the composite key "<id_equipement>:<id_equipement_maintenance>"
export const useEquipementsMaintenancesStore = defineStore("equipementsMaintenances", {
	state: () => ({
		equipementMaintenancesLoading: false,
		// indexed by scope: "id_equipement=3"
		equipementMaintenancesTotalCount: {} as Record<string, number>,
		equipementMaintenances: {} as Record<string, ReadEquipementMaintenanceDto>,
		equipementMaintenanceEdition: {} as Record<string, Edition<{ create: CreateEquipementMaintenanceByEquipementDto, update: UpdateEquipementMaintenanceDto }>>,
		equipementMaintenanceReady: {} as Record<string, Ready<{ create: CreateEquipementMaintenanceByEquipementDto, update: UpdateEquipementMaintenanceDto }>>,
	}),
	getters: {
		// the records of a equipement, indexed by the rest of the key
		equipementMaintenancesByEquipement: (state) => (id: string) => equipementMaintenanceResource.slice(state.equipementMaintenances, { id_equipement: id }),
		equipementMaintenancesCountByEquipement: (state) => (id: string) => state.equipementMaintenancesTotalCount[equipementMaintenanceResource.toScopeKey({ id_equipement: id })] ?? 0,
		equipementMaintenanceEditionByEquipement: (state) => (id: string) => equipementMaintenanceResource.live(state.equipementMaintenanceEdition, { id_equipement: id }),
		equipementMaintenanceReadyByEquipement: (state) => (id: string) => equipementMaintenanceResource.live(state.equipementMaintenanceReady, { id_equipement: id }),
	},
	actions: {
		getEquipementMaintenanceByInterval: equipementMaintenanceResource.getByInterval,
		getEquipementMaintenanceById: equipementMaintenanceResource.getById,
		createEquipementMaintenance: equipementMaintenanceResource.create,
		updateEquipementMaintenance: equipementMaintenanceResource.update,
		deleteEquipementMaintenance: equipementMaintenanceResource.remove,
		hydrateEquipementMaintenances: equipementMaintenanceResource.hydrate,
		getAvailableNewEquipementMaintenanceId: equipementMaintenanceResource.getAvailableNewId,
		setEquipementMaintenanceEdition: equipementMaintenanceResource.setEdition,
		removeEquipementMaintenanceEdition: equipementMaintenanceResource.removeEdition,
		removeEquipementMaintenanceReady: equipementMaintenanceResource.removeReady,
		setEquipementMaintenanceStagedSlice: equipementMaintenanceResource.setStagedSlice,
		valideEquipementMaintenanceEditionByKey: equipementMaintenanceResource.valideEditionByKey,
		copyEquipementMaintenanceByKeyPart: equipementMaintenanceResource.copyByKeyPart,
		clearEquipementMaintenanceEdition: equipementMaintenanceResource.clearEdition,
		pushEquipementMaintenanceChange: equipementMaintenanceResource.pushChange,
	},
});

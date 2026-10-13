import { defineStore } from "pinia";

import { fetchWrapper, createMainResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useStoresStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Edition } from "@/types/resource";
import type { components, paths } from "@/types/api";
type ReadZoneDto = components["schemas"]["ReadZoneDto"];
type ReadExtendedZoneDto = components["schemas"]["ReadExtendedZoneDto"];
type CreateZoneDto = NonNullable<paths["/api/zone"]["post"]["requestBody"]>["content"]["multipart/form-data"];
type UpdateZoneDto = NonNullable<paths["/api/zone/{id_zone}"]["put"]["requestBody"]>["content"]["multipart/form-data"];
type ReadStoreDto = components["schemas"]["ReadStoreDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

function hydrateZone(store: StoreGeneric, idZone: string, zone: ReadExtendedZoneDto, expand: string[] = []) {
	if (zone.url_thumbnail_zone && !store.thumbnailsURL[idZone]) {
		store.showThumbnailById(idZone);
	}
	store.zoneStoresTotalCount[idZone] = zone.stores_count;
	if (expand.includes("stores") && zone.stores) {
		const storesStore = useStoresStore();
		store.zoneStores[idZone] = {};
		for (const zoneStore of zone.stores) {
			if (!zoneStore.id_store) {
				continue;
			}
			store.zoneStores[idZone][zoneStore.id_store] = zoneStore;
			storesStore.stores[zoneStore.id_store] = zoneStore;
		}
	}
}

const zoneResource = createMainResource<{ readExtended: ReadExtendedZoneDto, readBasic: ReadZoneDto, create: CreateZoneDto, update: UpdateZoneDto }>({
	path: () => "/zone",
	idField: "id_zone",
	stateKey: "zones",
	countKey: "zonesTotalCount",
	loadingKey: "zonesLoading",
	editionKey: "zoneEdition",
	onHydrate: (store, entity: ReadExtendedZoneDto, expand) => {
		if (entity.id_zone === undefined) {
			return;
		}
		hydrateZone(store, String(entity.id_zone), entity, expand);
	},
});

export const useZonesStore = defineStore("zones", {
	state: () => ({
		zonesLoading: false,
		zonesTotalCount: 0,
		zones: {} as Record<string, ReadZoneDto>,
		zoneEdition: {} as Record<string, Edition<{ create: CreateZoneDto, update: UpdateZoneDto }>>,

		zoneStoresTotalCount: {} as Record<string, number>,
		zoneStores: {} as Record<string, Record<string, ReadStoreDto>>,

		imagesURL: {} as Record<string, string>,
		thumbnailsURL: {} as Record<string, string>,
	}),
	actions: {
		getZoneByList: zoneResource.getByList,
		getZoneByInterval: zoneResource.getByInterval,
		getZoneById: zoneResource.getById,
		createZone: zoneResource.create,
		getAvailableNewZoneId: zoneResource.getAvailableNewId,
		updateZone: zoneResource.update,
		deleteZone: zoneResource.remove,
		loadToEdition(id: string, preset: string | null = null) {
			if (!isNewId(id) && this.zones[id]) {
				this.zoneEdition[id] = {
					loading: false,
					name_zone: this.zones[id].name_zone ?? undefined,
					description_zone: this.zones[id].description_zone ?? undefined,
					xlength_zone: this.zones[id].xlength_zone,
					ylength_zone: this.zones[id].ylength_zone,
				};
			} else {
				this.zoneEdition[id] = {
					loading: false,
				};
				zoneResource.loadEditionPreset.call(this, id, preset);
			}
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.zoneEdition[id]) {
				this.zoneEdition[id] = {};
			}
			this.zoneEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.zoneEdition[id];
		},
		async saveAllChanges(id: string) {
			let realId = id;
			const { isFormData, ...data } = this.zoneEdition[id];
			const imageChanged = !!data.img_file || !!data.unset_img_zone;
			if (isNewId(id)) {
				realId = await this.createZone(data as CreateZoneDto, isFormData);
			} else {
				await this.updateZone(id, data as UpdateZoneDto, isFormData);
			}
			if (imageChanged) {
				delete this.thumbnailsURL[realId];
				delete this.imagesURL[realId];
				this.showThumbnailById(realId);
				this.showImageById(realId);
			}
			return realId;
		},

		async showImageById(id: string) {
			if (this.imagesURL[id]) {
				return;
			}
			const response = await fetchWrapper.image({ url: `${baseUrl}/zone/${id}/picture`, useToken: "access" });
			this.imagesURL[id] = URL.createObjectURL(response);
		},
		async showThumbnailById(id: string) {
			if (this.thumbnailsURL[id]) {
				return;
			}
			const response = await fetchWrapper.image({ url: `${baseUrl}/zone/${id}/thumbnail`, useToken: "access" });
			this.thumbnailsURL[id] = URL.createObjectURL(response);
		},
	},
});

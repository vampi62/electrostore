import { defineStore } from "pinia";

import { fetchWrapper, createMainResource } from "@/helpers";

import { useStoresStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

function hydrateZone(store, idZone, zone, expand = []) {
	if (zone.url_thumbnail_zone && !store.thumbnailsURL[idZone]) {
		store.showZoneThumbnailById(idZone);
	}
	store.zoneStoresTotalCount[idZone] = zone.stores_count;
	if (expand.includes("stores") && zone.stores) {
		const storesStore = useStoresStore();
		store.zoneStores[idZone] = {};
		for (const zoneStore of zone.stores) {
			store.zoneStores[idZone][zoneStore.id_store] = zoneStore;
			storesStore.stores[zoneStore.id_store] = zoneStore;
		}
	}
}

const zoneResource = createMainResource({
	path: () => "/zone",
	idField: "id_zone",
	stateKey: "zones",
	countKey: "zonesTotalCount",
	loadingKey: "zonesLoading",
	onHydrate: (store, entity, expand) => {
		hydrateZone(store, entity.id_zone, entity, expand);
	},
});

export const useZonesStore = defineStore("zones", {
	state: () => ({
		zonesLoading: false,
		zonesTotalCount: 0,
		zones: {},
		zoneEdition: {},

		zoneStoresTotalCount: {},
		zoneStores: {},

		imagesURL: {},
		thumbnailsURL: {},
	}),
	actions: {
		getZoneByList: zoneResource.getByList,
		getZoneByInterval: zoneResource.getByInterval,
		getZoneById: zoneResource.getById,
		createZone: zoneResource.create,
		updateZone: zoneResource.update,
		deleteZone: zoneResource.remove,
		loadToEdition(id, preset = null) {
			this.zoneEdition[id] = {};
			zoneResource.loadEditionPreset(id, preset);
			if (id !== "new" && this.zones[id]) {
				this.zoneEdition[id] = {
					loading: false,
					name_zone: this.zones[id].name_zone,
					description_zone: this.zones[id].description_zone,
					xlength_zone: this.zones[id].xlength_zone,
					ylength_zone: this.zones[id].ylength_zone,
					url_thumbnail_zone: this.zones[id].url_thumbnail_zone,
				};
			} else {
				this.zoneEdition[id] = {
					loading: false,
				};
			}
		},
		setLoadingEdition(id, loading) {
			if (!this.zoneEdition[id]) {
				this.zoneEdition[id] = {};
			}
			this.zoneEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.zoneEdition[id];
		},

		async uploadZonePicture(id, formData) {
			this.zones[id] = await fetchWrapper.post({ url: `${baseUrl}/zone/${id}/picture`, useToken: "access", body: formData, contentFile: true });
			delete this.thumbnailsURL[id];
			delete this.imagesURL[id];
		},
		async deleteZonePicture(id) {
			this.zones[id] = await fetchWrapper.delete({ url: `${baseUrl}/zone/${id}/picture`, useToken: "access" });
			delete this.thumbnailsURL[id];
			delete this.imagesURL[id];
		},
		async showZoneImageById(id_zone) {
			if (this.imagesURL[id_zone]) {
				return;
			}
			const response = await fetchWrapper.image({ url: `${baseUrl}/zone/${id_zone}/picture`, useToken: "access" });
			this.imagesURL[id_zone] = URL.createObjectURL(response);
		},
		async showZoneThumbnailById(id_zone) {
			if (this.thumbnailsURL[id_zone]) {
				return;
			}
			const response = await fetchWrapper.image({ url: `${baseUrl}/zone/${id_zone}/thumbnail`, useToken: "access" });
			this.thumbnailsURL[id_zone] = URL.createObjectURL(response);
		},
	},
});

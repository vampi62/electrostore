<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import * as Yup from "yup";

import { useViewScroll } from "@/composables";
import type { useNotification } from "@/composables";
import { isNewId } from "@/utils";
import { useConfigsStore, useZonesStore, useAuthStore } from "@/stores";

import type { TableauLabel } from "@/types/tableau";

const { addNotification } = inject("useNotification") as ReturnType<typeof useNotification>;
const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const configsStore = useConfigsStore();
const zonesStore = useZonesStore();
const authStore = useAuthStore();

const zoneId = ref(route.params.id as string);
const preset = ref(route.query.preset || null);

// every new element has its own edition space in the store, so several tabs can create an element at the same time
if (isNewId(zoneId.value)) {
	zoneId.value = zonesStore.getAvailableNewZoneId();
}

const formContainer = ref<any>(null);

async function fetchAllData() {
	if (isNewId(zoneId.value)) {
		zonesStore.loadToEdition(zoneId.value, preset.value as any);
	} else {
		zonesStore.setLoadingEdition(zoneId.value, true);
		try {
			await zonesStore.getZoneById(zoneId.value, ["stores"]);
		} catch {
			delete zonesStore.zones[zoneId.value];
			addNotification({ message: t("zone.NotFound"), type: "error" });
			router.push("/zones");
			return;
		}
		zonesStore.loadToEdition(zoneId.value);
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	zonesStore.clearEdition(zoneId.value);
	resetImageSelection();
});

// image
const imageInputRef = ref<HTMLInputElement | null>(null);
const localImagePreviewUrl = ref<string | null>(null);
const triggerImageInput = () => {
	if (zonesStore.zoneEdition[zoneId.value]?.loading) {
		return;
	}
	imageInputRef.value?.click();
};
const onImageFileChange = (event: Event) => {
	const input = event.target as HTMLInputElement;
	const file = input.files?.[0];
	input.value = "";
	if (!file) {
		return;
	}
	if (localImagePreviewUrl.value) {
		URL.revokeObjectURL(localImagePreviewUrl.value);
	}
	localImagePreviewUrl.value = URL.createObjectURL(file);
	zonesStore.zoneEdition[zoneId.value].img_file = file;
	zonesStore.zoneEdition[zoneId.value].unset_img_zone = false;
};
const removeImage = () => {
	if (localImagePreviewUrl.value) {
		URL.revokeObjectURL(localImagePreviewUrl.value);
		localImagePreviewUrl.value = null;
	}
	zonesStore.zoneEdition[zoneId.value].img_file = null;
	zonesStore.zoneEdition[zoneId.value].unset_img_zone = true;
};
const resetImageSelection = () => {
	if (localImagePreviewUrl.value) {
		URL.revokeObjectURL(localImagePreviewUrl.value);
	}
	localImagePreviewUrl.value = null;
};

// zone
const zoneDeleteModalShow = ref(false);
const zoneSave = async() => {
	try {
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("zone.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			zonesStore.setLoadingEdition(zoneId.value, false);
			return;
		}
		const edition = zonesStore.zoneEdition[zoneId.value];
		const imageChanged = !!edition.img_file || !!edition.unset_img_zone;
		const realId = await zonesStore.saveAllChanges(zoneId.value);
		zonesStore.loadToEdition(realId);
		if (imageChanged) {
			zonesStore.showThumbnailById(realId);
		}
		if (isNewId(zoneId.value)) {
			addNotification({ message: t("zone.Created"), type: "success" });
			const tempId = zoneId.value;
			zoneId.value = String(realId);
			zonesStore.clearEdition(tempId);
			router.push("/zones/" + zoneId.value);
		} else {
			addNotification({ message: t("zone.Updated"), type: "success" });
		}
		resetImageSelection();
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		zonesStore.setLoadingEdition(zoneId.value, false);
	}
};
const zoneDelete = async() => {
	try {
		await zonesStore.deleteZone(zoneId.value);
		addNotification({ message: t("zone.Deleted"), type: "success" });
		router.push("/zones");
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	zoneDeleteModalShow.value = false;
};

const createSchema = () => {
	const edition = zonesStore.zoneEdition[zoneId.value];
	const shape: any = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.name_zone = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("zone.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("zone.NameRequired"));
	shape.description_zone = Yup.string()
		.nullable()
		.optional()
		.max(configsStore.getConfigByKey("max_length_description"), t("zone.DescriptionMaxLength", { count: configsStore.getConfigByKey("max_length_description") }));
	shape.xlength_zone = Yup.number()
		.min(1, t("zone.XLengthMin"))
		.typeError(t("zone.XLengthType"))
		.required(t("zone.XLengthRequired"));
	shape.ylength_zone = Yup.number()
		.min(1, t("zone.YLengthMin"))
		.typeError(t("zone.YLengthType"))
		.required(t("zone.YLengthRequired"));
	shape.img_file = Yup.mixed()
		.nullable()
		.test("fileSize", t("zone.ImageSize") + " " + configsStore.getConfigByKey("max_size_image_in_mb") + "Mo", (value: any) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_image_in_mb"))) * 1024 * 1024);
	return Yup.object().shape(shape);
};

const labelForm = [
	{ key: "name_zone", label: "zone.Name", type: "text", enableCondition: "func.hasPermission([2])" },
	{ key: "description_zone", label: "zone.Description", type: "textarea", enableCondition: "func.hasPermission([2])" },
	{ key: "xlength_zone", label: "zone.XLength", type: "number", enableCondition: "func.hasPermission([2])" },
	{ key: "ylength_zone", label: "zone.YLength", type: "number", enableCondition: "func.hasPermission([2])" },
	{ key: "img_file", label: "zone.Image", type: "custom" },
];

const labelTableauStore = ref<TableauLabel[]>([
	{ label: "zone.StoreName", sortable: false, key: "name_store", valueKey: "name_store", type: "text" },
	{ label: "zone.StoreXMin", sortable: false, key: "xmin_store", valueKey: "xmin_store", type: "number" },
	{ label: "zone.StoreYMin", sortable: false, key: "ymin_store", valueKey: "ymin_store", type: "number" },
	{ label: "zone.StoreXMax", sortable: false, key: "xmax_store", valueKey: "xmax_store", type: "number" },
	{ label: "zone.StoreYMax", sortable: false, key: "ymax_store", valueKey: "ymax_store", type: "number" },
]);
useViewScroll(true);
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('zone.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/zones',
				create: { showCondition: isNewId(zoneId) && authStore.hasPermission([2]), loading: zonesStore.zoneEdition[zoneId]?.loading },
				update: { showCondition: !isNewId(zoneId) && authStore.hasPermission([2]), loading: zonesStore.zoneEdition[zoneId]?.loading },
				delete: { showCondition: !isNewId(zoneId) && authStore.hasPermission([2]) }
			}"
			@button-create="zoneSave" @button-update="zoneSave" @button-delete="zoneDeleteModalShow = true"/>
	</div>
	<div v-if="zonesStore.zones[zoneId] || isNewId(zoneId)" class="w-full">
		<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="zonesStore.zoneEdition[zoneId]" :store-user="authStore.user"
			:store-function="{ hasPermission: (validPerm: number[]) => authStore.hasPermission(validPerm) }">
			<template #img_file>
				<div class="flex flex-col items-center gap-2">
					<div class="flex justify-center items-center cursor-pointer"
						:class="{ 'opacity-50 cursor-not-allowed': zonesStore.zoneEdition[zoneId]?.loading }"
						@click="triggerImageInput">
						<img v-if="localImagePreviewUrl" :src="localImagePreviewUrl" alt="Preview"
							class="w-48 h-48 object-cover rounded" />
						<img v-else-if="!zonesStore.zoneEdition[zoneId]?.unset_img_zone && zonesStore.zoneEdition[zoneId]?.url_thumbnail_zone && zonesStore.thumbnailsURL[zoneId]"
							:src="zonesStore.thumbnailsURL[zoneId]" alt="Main"
							class="w-48 h-48 object-cover rounded" />
						<span v-else-if="!zonesStore.zoneEdition[zoneId]?.unset_img_zone && zonesStore.zoneEdition[zoneId]?.url_thumbnail_zone"
							class="w-48 h-48 object-cover rounded flex items-center justify-center">
							{{ $t('zone.Loading') }}
						</span>
						<img v-else src="../assets/nopicture.webp" alt="Not Found"
							class="w-48 h-48 object-cover rounded" />
					</div>
					<button v-if="localImagePreviewUrl || (!zonesStore.zoneEdition[zoneId]?.unset_img_zone && zonesStore.zoneEdition[zoneId]?.url_thumbnail_zone)"
						type="button" @click="removeImage"
						class="text-sm text-red-500 hover:text-red-600">
						{{ $t('zone.ImageRemove') }}
					</button>
					<input ref="imageInputRef" type="file" accept="image/*" class="hidden" @change="onImageFileChange" />
				</div>
			</template>
		</FormContainer>
		<CollapsibleSection title="zone.Stores"
			:total-count="Number(zonesStore.zoneStoresTotalCount[zoneId] || 0)">
			<template #append-row>
				<Tableau :labels="labelTableauStore" :meta="{ key: 'id_store', path: '/stores/' }"
					:store-data="[zonesStore.zoneStores[zoneId]]"
					:total-count="Number(zonesStore.zoneStoresTotalCount[zoneId] || 0)"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('zone.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="zoneDeleteModalShow" @close-modal="zoneDeleteModalShow = false"
		:delete-action="zoneDelete" :text-title="'zone.DeleteTitle'"
		:text-p="'zone.DeleteText'"/>
</template>

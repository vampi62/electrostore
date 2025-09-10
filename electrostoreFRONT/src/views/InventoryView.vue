<script setup>
import { onMounted, ref, inject } from "vue";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useConfigsStore, useItemsStore, useTagsStore, useIasStore, useCamerasStore } from "@/stores";
const itemsStore = useItemsStore();
const tagsStore = useTagsStore();
const iasStore = useIasStore();
const camerasStore = useCamerasStore();
const configsStore = useConfigsStore();

async function fetchAllData() {
	let tagsLink = new Set();
	let offset = 0;
	const limit = 100;
	do {
		await itemsStore.getItemByInterval(limit, offset, ["item_boxs", "item_tags"]);
		offset += limit;
	} while (offset < itemsStore.itemsTotalCount);
	for (const item of Object.values(itemsStore.items)) {
		item.custom_quantity_item = getTotalQuantity(item.item_boxs);
	}

	for (const item in itemsStore.items) {
		for (const tag in itemsStore.itemTags[item]) {
			tagsLink.add(tag);
		}
	}
	let tagsNotFound = [];
	for (const tag of Array.from(tagsLink)) {
		if (!tagsStore.tags[tag]) {
			tagsNotFound.push(tag);
		}
	}
	if (tagsNotFound.length > 0) {
		await tagsStore.getTagByList(tagsNotFound);
	}
	filter.value[6].options = Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag]);
}
onMounted(() => {
	fetchAllData();
});

function getTotalQuantity(itembox) {
	if (!itembox) {
		return 0;
	}
	return itembox.reduce((total, box) => total + box.qte_item_box, 0);
}

async function fetchIAData() {
	let offset = 0;
	const limit = 100;
	do {
		await iasStore.getIaByInterval(limit, offset);
		offset += limit;
	} while (offset < iasStore.TotalCount);
}
async function fetchCameraData() {
	let offset = 0;
	const limit = 100;
	do {
		await camerasStore.getCameraByInterval(limit, offset);
		offset += limit;
	} while (offset < camerasStore.TotalCount);
}

const selectedPageFind = ref({ ia: null, camera: null, image: null, imageBlob: null });
const fileInput = ref(null);
const showPageFind = ref(false);
const loadPageFind = async() => {
	showPageFind.value = true;
	selectedPageFind.value.ia = null;
	selectedPageFind.value.camera = null;
	selectedPageFind.value.image = null;
	iasStore.status.detect = {};
	if (selectedPageFind.value.imageBlob) {
		URL.revokeObjectURL(selectedPageFind.value.imageBlob);
		selectedPageFind.value.imageBlob = null;
	}
	await Promise.all([fetchIAData(), fetchCameraData()]);
};
const formatDateForDatetimeLocal = (date) => {
	if (typeof date === "string") {
		date = new Date(date);
	}
	const pad = (num) => String(num).padStart(2, "0");
	return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};
const handleFileUpload = (e) => {
	if (!e.target.files.length) {
		return;
	}
	selectedPageFind.value.camera = null;
	if (selectedPageFind.value.imageBlob) {
		URL.revokeObjectURL(selectedPageFind.value.imageBlob);
	}
	selectedPageFind.value.image = e.target.files[0];
	selectedPageFind.value.imageBlob = URL.createObjectURL(new Blob([selectedPageFind.value.image]));
};

const filter = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "item.VInventoryFilterName", compareMethod: "contain" },
	{ key: "friendly_name_item", value: "", type: "text", label: "item.VInventoryFilterFriendlyName", compareMethod: "contain" },
	{ key: "seuil_min_item", value: "", type: "number", label: "item.VInventoryFilterSeuilMin", compareMethod: ">=" },
	{ key: "seuil_min_item", value: "", type: "number", label: "item.VInventoryFilterSeuilMax", compareMethod: "<=" },
	{ key: "qte_item_box", subPath: "item_boxs", value: "", type: "number", label: "item.VInventoryFilterQuantityMin", compareMethod: ">=" },
	{ key: "qte_item_box", subPath: "item_boxs", value: "", type: "number", label: "item.VInventoryFilterQuantityMax", compareMethod: "<=" },
	{ key: "id_tag", subPath: "item_tags", value: "", type: "datalist", typeData: "int", options: Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag]), label: "item.VInventoryFilterTag", compareMethod: "=" },
]);
const tableauLabel = ref([
	{ label: "item.VInventoryName", sortable: true, key: "reference_name_item", type: "text" },
	{ label: "item.VInventoryFriendlyName", sortable: true, key: "friendly_name_item", type: "text" },
	{ label: "item.VInventorySeuil", sortable: true, key: "seuil_min_item", type: "number" },
	{ label: "item.VInventoryDescription", sortable: false, key: "description_item", type: "text" },
	{ label: "item.VInventoryTags", sortable: false, key: "", type: "list", list: { idStoreLink: 1, idStoreRessource: 2, key: "id_item", keyStoreLink: "id_tag", ressourcePrint: [{ type: "ressource", key: "nom_tag" }] } },
	{ label: "item.VInventoryImg", sortable: false, key: "id_img", type: "image", idStoreImg: 3, store: 4, keyStore: "id_item" },
	{ label: "item.VInventoryQuantity", sortable: true, key: "custom_quantity_item", type: "number" },
]);
const tableauMeta = ref({
	key: "id_item",
	path: "/inventory/",
});
const filteredItems = ref([]);
const updateFilteredItems = (newValue) => {
	filteredItems.value = newValue;
};
const changeCamera = (camera) => {
	if (selectedPageFind.value.camera !== null && selectedPageFind.value.camera !== camera) {
		camerasStore.stopStream(selectedPageFind.value.camera.id_camera);
	} else {
		camerasStore.getStatus(camera.id_camera);
		camerasStore.getStream(camera.id_camera);
	}
	selectedPageFind.value.image = null;
	selectedPageFind.value.imageBlob = null;
	selectedPageFind.value.camera = camera;
};
const cameraUpdateLight = async() => {
	if (!selectedPageFind.value.camera) {
		return;
	}
	await camerasStore.toggleLight(selectedPageFind.value.camera.id_camera);
	camerasStore.getStatus(selectedPageFind.value.camera.id_camera);
};
const startDetection = async() => {
	if (!selectedPageFind.value.ia) {
		return;
	}
	if (selectedPageFind.value.camera) {
		iasStore.status.detect = { loading: true };
		await camerasStore.getCapture(selectedPageFind.value.camera.id_camera,true);
		if (!camerasStore.capture[selectedPageFind.value.camera.id_camera]) {
			addNotification("error", t("item.VInventoryErrorCapture"));
			return;
		}
		await iasStore.detectItem(selectedPageFind.value.ia.id_ia, camerasStore.capture[selectedPageFind.value.camera.id_camera]);
		if (!iasStore.status.detect.predictedLabel) {
			addNotification("error", t("item.VInventoryErrorDetect"));
			return;
		}
	} else if (selectedPageFind.value.image) {
		iasStore.status.detect = { loading: true };
		await iasStore.detectItem(selectedPageFind.value.ia.id_ia, selectedPageFind.value.image);
		if (!iasStore.status.detect.predictedLabel) {
			addNotification("error", t("item.VInventoryErrorDetect"));
			return;
		}
	}
};
const openNewPage = (url) => {
	window.open(url, "_blank");
};
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('item.VInventoryTitle') }}</h2>
	</div>
	<div>
		<button @click="loadPageFind"
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2 mr-2">
			{{ $t('item.VInventoryFind') }}
		</button>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/inventory/new'">
				{{ $t('item.VInventoryAdd') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="itemsStore.items" @output-filter="updateFilteredItems" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[filteredItems,itemsStore.itemTags,tagsStore.tags,itemsStore.thumbnailsURL,itemsStore.items]"
		:loading="itemsStore.itemsLoading"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>

	<div v-if="showPageFind" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center" @click="showPageFind = false">
		<div class="bg-white p-2 rounded shadow-lg w-2/3" @click.stop>
			<div class="flex justify-between items-center p-2">
				<h2 class="text-xl font-bold mb-4">{{ $t('item.VInventoryFindTitle') }}</h2>
				<button @click="showPageFind = false" class="text-red-500 hover:text-red-600">
					<font-awesome-icon icon="fa-solid fa-times" />
				</button>
			</div>
			<div class="flex justify-between">
				<div class="flex-1 border max-w-40">
					<span class="text-lg font-bold">{{ $t('item.VInventoryFindCameraList') }}</span>
					<div class="max-h-40 overflow-y-auto">
						<div v-for="camera in camerasStore.cameras" :key="camera.id_camera" @click="camerasStore.status[camera.id_camera]?.statusCode == 200 ? changeCamera(camera) : null"
							class="p-1 border"
							:class="{
								'bg-blue-500 text-white': selectedPageFind.camera === camera,
								'bg-red-200': selectedPageFind.camera !== camera && camerasStore.status[camera.id_camera]?.statusCode !== 200,
								'bg-gray-200': selectedPageFind.camera !== camera && camerasStore.status[camera.id_camera]?.statusCode === 200,
								'cursor-pointer': camerasStore.status[camera.id_camera]?.statusCode == 200 }">
							<div>{{ camera.nom_camera }}</div>
							<span class="h-4 text-xs">{{ camera.url_camera }}</span>
						</div>
					</div>
					<span class="text-lg font-bold">{{ $t('item.VInventoryFindIAList') }}</span>
					<div class="max-h-40 overflow-y-auto">
						<div v-for="ia in iasStore.ias" :key="ia.id_ia" @click="ia.trained_ia ? selectedPageFind.ia = ia : null"
							class="p-1 border"
							:class="{
								'bg-blue-500 text-white': selectedPageFind.ia === ia,
								'bg-red-200': selectedPageFind.ia !== ia && ia.trained_ia === false,
								'bg-gray-200': selectedPageFind.ia !== ia && ia.trained_ia === true,
								'cursor-pointer': ia.trained_ia }">
							<div>{{ ia.nom_ia }}</div>
							<span class="h-4 text-xs">{{ formatDateForDatetimeLocal(ia.date_ia) }}</span>
						</div>
					</div>
				</div>
				<div class="flex flex-1 flex-col flex-grow items-center">
					<div class="w-80 h-80 bg-gray-200 px-4 py-2 rounded mb-2">
						<template v-if="selectedPageFind.camera && camerasStore.stream[selectedPageFind.camera.id_camera]">
							<img :src="camerasStore.stream[selectedPageFind.camera.id_camera]" @click="$refs.fileInput.click()"
								class="w-full h-full object-cover rounded cursor-pointer" alt="Camera Stream" />
						</template>
						<template v-else-if="selectedPageFind.imageBlob">
							<img :src="selectedPageFind.imageBlob" @click="$refs.fileInput.click()"
								class="w-full h-full object-cover rounded cursor-pointer" alt="Uploaded" />
						</template>
						<template v-else>
							<div class="flex justify-center items-center h-full cursor-pointer" @click="$refs.fileInput.click()">
								{{ $t('item.VInventoryNoCameraOrUploadAnImage') }}
							</div>
						</template>
						<input type="file" @change="handleFileUpload" class="hidden" ref="fileInput" />
					</div>
					<div class="flex w-full border-y border-r justify-between">
						<div class="flex flex-col mx-2">
							<button @click="startDetection()"
							class="px-3 py-1 rounded text-sm inline-block my-2"
							:class="selectedPageFind.ia && (selectedPageFind.camera || selectedPageFind.image) ? 'bg-blue-500 hover:bg-blue-600 text-white cursor-pointer' : 'bg-blue-200 text-gray-500 cursor-not-allowed'">
								{{ $t('item.VInventoryDetect') }}
							</button>
							<button @click="cameraUpdateLight()"
							class="px-3 py-1 rounded text-sm inline-block my-2"
							:class="selectedPageFind.camera ? 'bg-blue-500 hover:bg-blue-600 text-white cursor-pointer' : 'bg-blue-200 text-gray-500 cursor-not-allowed'">
								{{ $t('item.VInventoryLight') }}
							</button>
						</div>
						<div class="flex justify-around flex-grow items-center h-full border-l">
							<div class="flex flex-col items-center">
								<template v-if="iasStore.status.detect.loading">
									<span>{{ $t('item.VInventoryLoading') }}</span>
								</template>
								<template v-else-if="iasStore.status.detect.predictedLabel == -1">
									<span>{{ $t('item.VInventoryNoItem') }}</span>
								</template>
								<template v-else-if="iasStore.status.detect.predictedLabel > -1">
									<div>
										<span>{{ $t('item.VInventoryItem') }}: </span>
										<span>{{ itemsStore.items[iasStore.status.detect.predictedLabel].reference_name_item }}</span>
									</div>
									<div>
										<span>{{ $t('item.VInventoryScore') }}: </span>
										<span>{{ iasStore.status.detect.score }} %</span>
									</div>
								</template>
								<template v-else>
									<span>{{ $t('item.VInventoryNoDetection') }}</span>
								</template>
								<template v-if="iasStore.status.detect?.predictedLabel > -1">
									<button class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2 mr-2"
										@click="openNewPage('/inventory/' + iasStore.status.detect.predictedLabel)">
										{{ $t('item.VInventoryGoToItem') }}
									</button>
								</template>
								<template v-else>
									<button class="bg-blue-200 text-gray-500 px-3 py-1 rounded text-sm cursor-not-allowed inline-block mb-2 mr-2">
										{{ $t('item.VInventoryGoToItem') }}
									</button>
								</template>
							</div>
							<div>
								<img v-if="itemsStore.items[iasStore.status.detect.predictedLabel]?.id_img"
									:src="itemsStore.thumbnailsURL[itemsStore.items[iasStore.status.detect.predictedLabel].id_img]"
									class="w-16 h-16 object-cover rounded" alt="Main" />
								<img v-else src="../assets/nopicture.webp" class="w-16 h-16 object-cover rounded" alt="Not Found" />
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

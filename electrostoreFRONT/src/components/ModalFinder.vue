
<template>
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

<script>
import { inject } from "vue";
import { useI18n } from "vue-i18n";
import { useItemsStore, useIasStore, useCamerasStore } from "@/stores";
export default {
	name: "ModalFinder",
	methods: {
		openNewPage(url) {
			window.open(url, "_blank");
		},
		changeCamera(camera) {
			if (this.selectedPageFind.camera !== null && this.selectedPageFind.camera !== camera) {
				this.camerasStore.stopStream(this.selectedPageFind.camera.id_camera);
			} else {
				this.camerasStore.getStatus(camera.id_camera);
				this.camerasStore.getStream(camera.id_camera);
			}
			this.selectedPageFind.image = null;
			this.selectedPageFind.imageBlob = null;
			this.selectedPageFind.camera = camera;
		},
		async cameraUpdateLight() {
			if (!this.selectedPageFind.camera) {
				return;
			}
			await this.camerasStore.toggleLight(this.selectedPageFind.camera.id_camera);
			this.camerasStore.getStatus(this.selectedPageFind.camera.id_camera);
		},
		async startDetection() {
			if (!this.selectedPageFind.ia) {
				return;
			}
			if (this.selectedPageFind.camera) {
				this.iasStore.status.detect = { loading: true };
				await this.camerasStore.getCapture(this.selectedPageFind.camera.id_camera,true);
				if (!this.camerasStore.capture[this.selectedPageFind.camera.id_camera]) {
					this.addNotification("error", this.t("item.VInventoryErrorCapture"));
					return;
				}
				await this.iasStore.detectItem(this.selectedPageFind.ia.id_ia, this.camerasStore.capture[this.selectedPageFind.camera.id_camera]);
				if (!this.iasStore.status.detect.predictedLabel) {
					this.addNotification("error", this.t("item.VInventoryErrorDetect"));
					return;
				}
			} else if (this.selectedPageFind.image) {
				this.iasStore.status.detect = { loading: true };
				await this.iasStore.detectItem(this.selectedPageFind.ia.id_ia, this.selectedPageFind.image);
				if (!this.iasStore.status.detect.predictedLabel) {
					this.addNotification("error", this.t("item.VInventoryErrorDetect"));
					return;
				}
			}
		},
		handleFileUpload(e) {
			if (!e.target.files.length) {
				return;
			}
			this.selectedPageFind.camera = null;
			if (this.selectedPageFind.imageBlob) {
				URL.revokeObjectURL(this.selectedPageFind.imageBlob);
			}
			this.selectedPageFind.image = e.target.files[0];
			this.selectedPageFind.imageBlob = URL.createObjectURL(new Blob([this.selectedPageFind.image]));
		},
		async fetchIAData() {
			let offset = 0;
			const limit = 100;
			do {
				await this.iasStore.getIaByInterval(limit, offset);
				offset += limit;
			} while (offset < this.iasStore.TotalCount);
		},
		async fetchCameraData() {
			let offset = 0;
			const limit = 100;
			do {
				await this.camerasStore.getCameraByInterval(limit, offset);
				offset += limit;
			} while (offset < this.camerasStore.TotalCount);
		},
		formatDateForDatetimeLocal(date) {
			if (!date) {
				return "";
			}
			if (typeof date === "string") {
				date = new Date(date);
			}
			const pad = (num) => String(num).padStart(2, "0");
			return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
		},
		async loadPageFind() {
			this.showPageFind = true;
			this.selectedPageFind.ia = null;
			this.selectedPageFind.camera = null;
			this.selectedPageFind.image = null;
			this.iasStore.status.detect = {};
			if (this.selectedPageFind.imageBlob) {
				URL.revokeObjectURL(this.selectedPageFind.imageBlob);
				this.selectedPageFind.imageBlob = null;
			}
			await Promise.all([this.fetchIAData(), this.fetchCameraData()]);
		},
	},
	setup() {
		const { addNotification } = inject("useNotification"); 
		const { t } = useI18n();
		const iasStore = useIasStore();
		const itemsStore = useItemsStore();
		const camerasStore = useCamerasStore();
		return {
			addNotification,
			t,
			iasStore,
			itemsStore,
			camerasStore,
		};
	},
	data() {
		return {
			selectedPageFind: { ia: null, camera: null, image: null, imageBlob: null },
			fileInput: null,
			showPageFind: false,
		};
	},
};
</script>
<script setup>
import { onMounted, ref, computed, inject } from "vue";

const { addNotification } = inject("useNotification");

import { useAuthStore, useCamerasStore } from "@/stores";
const camerasStore = useCamerasStore();
const authStore = useAuthStore();

async function fetchData() {
	let offset = 0;
	const limit = 100;
	do {
		await camerasStore.getCameraByInterval(limit, offset);
		offset += limit;
	} while (offset < camerasStore.TotalCount);
}
onMounted(() => {
	fetchData();
});

const sort = ref({ key: "", order: "asc" });
function changeSort(key) {
	if (sort.value.key === key) {
		sort.value.order = sort.value.order === "asc" ? "desc" : "asc";
	} else {
		sort.value.key = key;
		sort.value.order = "asc";
	}
}
const filter = ref([
	{ key: "nom_camera", value: "", type: "text", label: "camera.VCamerasFilterName", compareMethod: "contain" },
	{ key: "url_camera", value: "", type: "text", label: "camera.VCamerasFilterUrl", compareMethod: "contain" },
]);
const filteredCameras = computed(() => {
	return Object.values(camerasStore.cameras).filter((element) => {
		return filter.value.every((f) => {
			if (f.value) {
				if (f.subPath) {
					if (f.compareMethod === "=") {
						return element[f.subPath].some((subElement) => subElement[f.key] === f.value);
					} else if (f.compareMethod === ">=") {
						return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) >= f.value;
					} else if (f.compareMethod === "<=") {
						return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) <= f.value;
					}
				} else {
					if (f.compareMethod === "=") {
						return element[f.key] === f.value;
					} else if (f.compareMethod === ">=") {
						return element[f.key] >= f.value;
					} else if (f.compareMethod === "<=") {
						return element[f.key] <= f.value;
					} else if (f.compareMethod === "contain") {
						return element[f.key].includes(f.value);
					}
				}
			}
			return true;
		});
	});
});
const sortedCameras = computed(() => {
	if (sort.value.key) {
		return Object.values(filteredCameras.value).sort((a, b) => {
			if (sort.value.order === "asc") {
				return a[sort.value.key] > b[sort.value.key] ? 1 : -1;
			} else {
				return a[sort.value.key] < b[sort.value.key] ? 1 : -1;
			}
		});
	}
	return filteredCameras.value;
});
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4">{{ $t('camera.VCamerasTitle') }}</h2>
	</div>
	<div>
		<div :disabled="authStore.user?.role_user !== 'admin'"
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/cameras/new'">
				{{ $t('camera.VCamerasAdd') }}
			</RouterLink>
		</div>
		<div>
			<div class="flex flex-wrap">
				<template v-for="f in filter" :key="f">
					<label class="text-sm text-gray-700 ml-2">{{ $t(f.label) }}</label>
					<template v-if="f.type === 'select'">
						<select v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2">
							<option value=""></option>
							<template v-if="f.options">
								<option v-for="option in f.options" :key="option[0]" :value="option[0]">{{ option[1] }}
								</option>
							</template>
						</select>
					</template>
					<template v-else-if="f.type === 'date'">
						<input type="date" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
					</template>
					<template v-else-if="f.type === 'number'">
						<input type="number" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
					</template>
					<template v-else-if="f.type === 'text'">
						<input type="text" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
					</template>
				</template>
			</div>
		</div>
	</div>
	<table class="min-w-full border-collapse border border-gray-300">
		<thead class="bg-gray-100">
			<tr>
				<th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
					@click="changeSort('nom_camera')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('camera.VCamerasName') }}</span>
						<template v-if="sort.key === 'nom_camera'">
							<template v-if="sort.order === 'asc'">
								<font-awesome-icon icon="fa-solid fa-sort-up" class="ml-2" />
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-sort-down" class="ml-2" />
							</template>
						</template>
						<template v-else>
							<font-awesome-icon icon="fa-solid fa-sort" class="ml-2" />
						</template>
					</div>
				</th>
				<th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
					@click="changeSort('url_camera')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('camera.VCamerasUrl') }}</span>
						<template v-if="sort.key === 'url_camera'">
							<template v-if="sort.order === 'asc'">
								<font-awesome-icon icon="fa-solid fa-sort-up" class="ml-2" />
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-sort-down" class="ml-2" />
							</template>
						</template>
						<template v-else>
							<font-awesome-icon icon="fa-solid fa-sort" class="ml-2" />
						</template>
					</div>
				</th>
				<th
					class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('camera.VCamerasUser') }}</span>
					</div>
				</th>
				<th
					class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('camera.VCamerasNetwork') }}</span>
					</div>
				</th>
				<th
					class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('camera.VCamerasStatus') }}</span>
					</div>
				</th>
			</tr>
		</thead>
		<tbody>
			<template v-if="!camerasStore.loading">
				<RouterLink v-for="camera in sortedCameras" :key="camera.id_camera" :to="'/cameras/' + camera.id_camera"
					custom v-slot="{ navigate }">
					<tr @click="navigate" class=" transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ camera.nom_camera }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ camera.url_camera }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700 text-center">
							<template v-if="camera.user_camera == '' && camera.password_camera == ''">
								<font-awesome-icon icon="fa-solid fa-times" class="ml-2" />
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-check" class="ml-2" />
							</template>
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700 text-center">
							<template v-if="camerasStore.status[camera.id_camera].network">
								<font-awesome-icon icon="fa-solid fa-check" class="ml-2 text-green-500" />
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-times" class="ml-2 text-red-500" />
							</template>
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700 text-center">
							<template v-if="camerasStore.status[camera.id_camera].statusCode == 200">
								<font-awesome-icon icon="fa-solid fa-check" class="ml-2 text-green-500" />
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-times" class="ml-2 text-red-500" />
							</template>
						</td>
					</tr>
				</RouterLink>
			</template>
			<template v-else>
				<div>{{ $t('camera.VCamerasLoading') }}</div>
			</template>
		</tbody>
	</table>
</template>

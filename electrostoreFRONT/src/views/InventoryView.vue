<script setup>
import { onMounted, ref, computed, inject } from "vue";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useItemsStore, useTagsStore } from "@/stores";
const itemsStore = useItemsStore();
const tagsStore = useTagsStore();

async function fetchData() {
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
	filter.value[5].options = Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag]);
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

function getTotalQuantity(itembox) {
	if (!itembox) {
		return 0;
	}
	return itembox.reduce((total, box) => total + box.qte_item_box, 0);
}

const filter = ref([
	{ key: "nom_item", value: "", type: "text", label: "item.VInventoryFilterName" },
	{ key: "seuil_min_item", value: "", type: "number", label: "item.VInventoryFilterSeuilMin", compareMethod: ">=" },
	{ key: "seuil_min_item", value: "", type: "number", label: "item.VInventoryFilterSeuilMax", compareMethod: "<=" },
	{ key: "qte_item_box", subPath: "item_boxs", value: "", type: "number", label: "item.VInventoryFilterQuantityMin", compareMethod: ">=" },
	{ key: "qte_item_box", subPath: "item_boxs", value: "", type: "number", label: "item.VInventoryFilterQuantityMax", compareMethod: "<=" },
	{ key: "id_tag", subPath: "item_tags", value: "", type: "select", options: Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag]), label: "item.VInventoryFilterTag", compareMethod: "=" },
]);
const filteredItems = computed(() => {
	return Object.values(itemsStore.items).filter((element) => {
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
const sortedItems = computed(() => {
	if (sort.value.key) {
		return Object.values(filteredItems.value).sort((a, b) => {
			if (sort.value.order === "asc") {
				return a[sort.value.key] > b[sort.value.key] ? 1 : -1;
			} else {
				return a[sort.value.key] < b[sort.value.key] ? 1 : -1;
			}
		});
	}
	return filteredItems.value;
});
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4">{{ $t('item.VInventoryTitle') }}</h2>
	</div>
	<div>
		<button
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2 mr-2">
			{{ $t('item.VInventoryFind') }}
		</button>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/inventory/new'">
				{{ $t('item.VInventoryAdd') }}
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
					@click="changeSort('nom_item')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('item.VInventoryName') }}</span>
						<template v-if="sort.key === 'nom_item'">
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
					@click="changeSort('seuil_min_item')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('item.VInventorySeuil') }}</span>
						<template v-if="sort.key === 'seuil_min_item'">
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
						<span class="flex-1">{{ $t('item.VInventoryDescription') }}</span>
					</div>
				</th>
				<th
					class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('item.VInventoryTags') }}</span>
					</div>
				</th>
				<th
					class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('item.VInventoryImg') }}</span>
					</div>
				</th>
				<th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
					@click="changeSort('custom_quantity_item')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('item.VInventoryQuantity') }}</span>
						<template v-if="sort.key === 'custom_quantity_item'">
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
			</tr>
		</thead>
		<tbody>
			<template v-if="!itemsStore.itemsLoading">
				<RouterLink v-for="item in sortedItems" :key="item.id_item" :to="'/inventory/' + item.id_item" custom
					v-slot="{ navigate }">
					<tr @click="navigate" class=" transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ item.nom_item }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ item.seuil_min_item }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ item.description_item }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							<ul>
								<li v-for="tag in itemsStore.itemTags[item.id_item]" :key="tag.id_tag">
									{{ tagsStore.tags[tag.id_tag]?.nom_tag }}
								</li>
							</ul>
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							<div class="flex justify-center items-center">
								<template v-if="item.id_img">
									<img v-if="itemsStore.imagesURL[item.id_img]"
										:src="itemsStore.imagesURL[item.id_img]" alt="Image"
										class="w-16 h-16 object-cover rounded" />
									<span v-else class="w-16 h-16 object-cover rounded">
										{{ $t('item.VInventoryLoading') }}
									</span>
								</template>
								<template v-else>
									<img src="../assets/nopicture.webp" alt="Image"
										class="w-16 h-16 object-cover rounded" />
								</template>
							</div>
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ item.custom_quantity_item
							}}
						</td>
					</tr>
				</RouterLink>
			</template>
			<template v-else>
				<div>{{ $t('item.VInventoryLoading') }}</div>
			</template>
		</tbody>
	</table>
</template>

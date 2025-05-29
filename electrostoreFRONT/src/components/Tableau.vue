<template>
	<table class="min-w-full border-collapse border border-gray-300">
		<thead class="bg-gray-100">
			<tr>
				<th v-for="(column,index) in labels"
					:key="index"
					class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative"
					:class="column.sortable ? 'cursor-pointer' : ''"
					@click="changeSort(column)">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t(column.label) }}</span>
						<template v-if="column.sortable">
							<template v-if="sort.key === column.key">
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
						</template>
					</div>
				</th>
			</tr>
		</thead>
		<tbody>
			<RouterLink v-for="data in sortedData" :key="data[meta.key]" :to="meta.path + data[meta.key]"
				custom v-slot="{ navigate }">
				<tr @click="navigate" class="transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
					<td  v-for="(column,index) in labels"
						:key="index"
						:class="column.type == 'text' ? 'text-left' : 'text-center'"
						class="border border-gray-300 px-4 py-2 text-sm text-gray-700"
					>
						<template v-if="column.type == 'text'">
							{{ data[column.key] }}
						</template>
						<template v-else-if="column.type == 'enum'">
							{{ column.options[data[column.key]] }}
						</template>
						<template v-else-if="column.type == 'date'">
							{{ new Date(data[column.key]).toLocaleDateString() }}
						</template>
						<template v-else-if="column.type == 'bool'">
							<template v-if="evaluateCondition(column.condition, data)">
								<font-awesome-icon icon="fa-solid fa-check" class="ml-2 text-green-500" />
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-times" class="ml-2 text-red-500" />
							</template>
						</template>
						<template v-else-if="column.type == 'list'">
							<ul>
								<li v-for="(item, itemIndex) in this.storeData[column.list.idStoreLink]?.[data[meta.key]] || []"
									:key="itemIndex">
									<template v-for="(ressource, ressourceIndex) in column.list.ressourcePrint" :key="ressourceIndex">
										<template v-if="ressource.type === 'link'">
											{{ item?.[ressource.key] || 'Unknown' }}
										</template>
										<template v-else-if="ressource.type === 'text'">
											{{ ressource.key }}
										</template>
										<template v-else-if="ressource.type === 'ressource'">
											{{ this.storeData[column.list.idStoreRessource][item[column.list.keyStoreLink]]?.[ressource.key] || 'Unknown' }}
										</template>
									</template>
								</li>
							</ul>
						</template>
						<template v-else-if="column.type == 'image'">
							<div class="flex justify-center items-center">
								<template v-if="data[column.key]">
									<img v-if="this.storeData[column.idStoreImg]?.[data[column.key]]"
										:src="this.storeData[column.idStoreImg]?.[data[column.key]]"
										alt=""
										class="w-16 h-16 object-cover rounded" />
									<span v-else class="w-16 h-16 object-cover rounded">
										{{ $t('item.VInventoryLoading') }}
									</span>
								</template>
								<template v-else>
									<img src="../assets/nopicture.webp" alt=" Unavailable" class="w-16 h-16 object-cover rounded" />
								</template>
							</div>
						</template>
					</td>
				</tr>
			</RouterLink>
		</tbody>
	</table>
</template>
<script>
export default {
	name: "Tableau",
	props: {
		labels: {
			type: Array,
			required: true,
		},
		meta: {
			type: Object,
			required: true,
		},
		storeData: {
			type: Array,
			required: true,
		},
		loading: {
			type: Boolean,
			default: false,
		},
		message: {
			type: Object, // message with, 'message' and 'type' properties
			default: () => ({}),
		},
	},
	computed: {
		sortedData() {
			if (this.sort.key) {
				return [...this.storeData[0]].sort((a, b) => {
					if (this.sort.order === "asc") {
						return a[this.sort.key] > b[this.sort.key] ? 1 : -1;
					} else {
						return a[this.sort.key] < b[this.sort.key] ? 1 : -1;
					}
				});
			}
			return this.storeData[0];
		},
	},
	data() {
		return {
			sort: {
				key: "",
				order: "asc",
			},
		};
	},
	methods: {
		changeSort(column) {
			if (!column.sortable) {
				return;
			}
			if (this.sort.key === column.key) {
				this.sort.order = this.sort.order === "asc" ? "desc" : "asc";
			} else {
				this.sort.key = column.key;
				this.sort.order = "asc";
			}
		},
		evaluateCondition(condition,rowData) {
			try {
				return new Function(["store","rowData"], `return ${condition}`)(this.storeData,rowData);
			} catch (error) {
				console.error("Erreur lors de l'évaluation de la condition :", error);
				return false;
			}
		},
	},
};
</script>
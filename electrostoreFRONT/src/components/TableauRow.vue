<template>
	<td  v-for="(column,index) in labels"
		:key="index"
		:class="[css, column.type == 'text' ? 'text-left' : 'text-center']"
	>
		<template v-if="column.type == 'text'">
			{{ row[column.key] }}
		</template>
		<template v-else-if="column.type == 'enum'">
			{{ column.options[row[column.key]] }}
		</template>
		<template v-else-if="column.type == 'date'">
			{{ row[column.key] ? new Date(row[column.key]).toLocaleDateString() : '' }}
		</template>
		<template v-else-if="column.type == 'datetime'">
			{{ row[column.key] ? new Date(row[column.key]).toLocaleString() : '' }}
		</template>
		<template v-else-if="column.type == 'bool'">
			<template v-if="evaluateCondition(column.condition, row)">
				<font-awesome-icon icon="fa-solid fa-check" class="ml-2 text-green-500" />
			</template>
			<template v-else>
				<font-awesome-icon icon="fa-solid fa-times" class="ml-2 text-red-500" />
			</template>
		</template>
		<template v-else-if="column.type == 'list'">
			<ul>
				<li v-for="(item, itemIndex) in this.storeData[column.list.idStoreLink]?.[row[column.list.key]] || []"
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
				<template v-if="row[column.key]">
					<img v-if="this.storeData[column.idStoreImg]?.[row[column.key]]"
						:src="this.storeData[column.idStoreImg]?.[row[column.key]]"
						alt=""
						class="w-16 h-16 object-cover rounded" />
					<span v-else class="w-16 h-16 object-cover rounded">
						{{ $t('components.VModalTableauImageLoading') }}
					</span>
				</template>
				<template v-else>
					<img src="../assets/nopicture.webp" alt=" Unavailable" class="w-16 h-16 object-cover rounded" />
				</template>
			</div>
		</template>
		<template v-else-if="column.type == 'buttons'">
			<div class="flex justify-center items-center">
				<template v-for="(button, buttonIndex) in column.buttons" :key="buttonIndex">
					<template v-if="!button?.condition || !evaluateCondition(button.condition, row)">
						<button @click="button.action(row)" :class="button.class" class="m-1">
							<span v-if="button.icon">
								<font-awesome-icon :icon="button.icon" />
								<span v-if="button.label" class="mr-2"></span>
							</span>
							<span v-if="button.label">
								{{ $t(button.label) }}
							</span>
						</button>
					</template>
				</template>
			</div>
		</template>
	</td>
</template>
<script>
export default {
	name: "TableauRow",
	props: {
		labels: {
			type: Array,
			required: true,
			// labels for the columns, each object should have a key and type property
			default: () => [],
		},
		row: {
			type: Object,
			required: true,
			// row pass by the parent component, containing the data for the current row
		},
		storeData: {
			type: Object,
			required: true,
			// storeData pass by the parent component, containing all the data needed for the row
			default: () => ({}),
		},
		css: {
			type: String,
			required: false,
			// Default CSS class tailwind for table cells
			default: "border border-gray-300 px-4 py-2 text-sm text-gray-700",
		},
	},
	methods: {
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
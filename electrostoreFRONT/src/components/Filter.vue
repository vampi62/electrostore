<template>
	<div>
		<label v-if="label.length > 0" :for="`filter-input-${this.$.uid}`" class="text-sm text-gray-700 ml-2">{{ $t(label) }}</label>
		<template v-if="type === 'select'">
			<select
				:id="`filter-input-${this.$.uid}`"
				class="border border-gray-300 rounded px-2 py-1"
				:class="[classCss, label.length > 0 ? 'ml-2' : '']"
				@change="$emit('update-text', idKey, $event.target.value)"
			>
				<option value=""></option>
				<template v-if="options">
					<option v-for="option in options" :key="option[0]" :value="option[0]">{{ option[1] }}
					</option>
				</template>
			</select>
		</template>
		<template v-else>
			<input
				:id="`filter-input-${this.$.uid}`"
				:type="type"
				class="border border-gray-300 rounded px-2 py-1"
				:class="[classCss, label.length > 0 ? 'ml-2' : '']"
				:placeholder="placeholder"
				@input="$emit('update-text', idKey, $event.target.value)"
			/>
		</template>
	</div>
</template>

<script>
export default {
	props: {
		label: {
			type: String,
			required: true,
		},
		type: {
			type: String,
			default: "text", // 'text', 'number', 'select', etc.
		},
		placeholder: {
			type: String,
			default: "",
		},
		classCss: {
			type: String,
			default: "",
		},
		options: {
			type: Array,
			default: () => [],
		},
		idKey: {
			type: Number,
			default: 0,
		},
	},
	emits: ["update-text"],
};
</script>
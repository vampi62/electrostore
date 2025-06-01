<template>
	<div>
		<label v-if="label.length > 0" :for="`filter-input-${this.$.uid}`" class="text-sm text-gray-700 ml-2">{{ $t(label) }}</label>
		<template v-if="type === 'select'">
			<select
				:id="`filter-input-${this.$.uid}`"
				class="border border-gray-300 rounded px-2 py-1"
				:class="[classCss, label.length > 0 ? 'ml-2' : '']"
				@change="$emit('updateText', $event.target.value)"
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
				@input="$emit('updateText', $event.target.value)"
			/>
		</template>
	</div>
</template>

<script>
export default {
	name: "Filter",
	props: {
		label: {
			type: String,
			required: true,
			// This should be a translation key for the label
			// e.g., 'components.FilterLabel'
			default: "",
		},
		type: {
			type: String,
			required: true,
			// This should be a valid input type
			// 'text', 'number', 'select', etc.
			default: "text",
		},
		placeholder: {
			type: String,
			required: false,
			// This should be already translated texte
			// e.g., 'write here your text'
			default: "",
		},
		classCss: {
			type: String,
			required: false,
			// This should be a CSS tailwind class for styling the input
			default: "",
		},
		options: {
			type: Array,
			required: false,
			// This should be an array of options for select input
			// e.g., [['value1', 'Label 1'], ['value2', 'Label 2']]
			// translate the labels before passing
			// to the component
			default: () => [],
		},
	},
	emits: ["updateText"],
};
</script>
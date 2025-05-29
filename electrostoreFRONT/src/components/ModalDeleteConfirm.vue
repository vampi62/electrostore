<template>
	<div v-if="showModal" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="$emit('closeModal')">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t(textTitle) }}</h2>
			<p>{{ $t(textP) }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="$emit('deleteConfirmed')"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('components.VModalDeleteConfirm') }}
				</button>
				<button type="button" @click="$emit('closeModal')"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('components.VModalDeleteCancel') }}
				</button>
			</div>
		</div>
	</div>
</template>

<script>
export default {
	name: "ModalDeleteConfirm",
	props: {
		showModal: {
			type: Boolean,
			default: false,
		},
		textTitle: {
			type: String,
			default: "Confirmation",
		},
		textP: {
			type: String,
			default: "Are you sure you want to delete this Element?",
		},
	},
	emits: ["deleteConfirmed", "closeModal"],
};
</script>
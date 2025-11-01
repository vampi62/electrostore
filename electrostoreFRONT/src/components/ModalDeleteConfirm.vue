<template>
	<div v-if="showModal" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center z-50"
		@click="$emit('closeModal')">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t(textTitle) }}</h2>
			<p>{{ $t(textP) }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<div class="relative">
					<button type="button" @click="deleteConfirm"
						class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600"
						:disabled="loading">
						{{ $t('components.VModalDeleteConfirm') }}
					</button>
					<div v-if="loading" 
						class="absolute inset-0 bg-red-500 bg-opacity-90 rounded-lg flex items-center justify-center">
						<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
					</div>
				</div>
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
			required: true,
			// This should be a boolean to control the visibility of the modal
			default: false,
		},
		textTitle: {
			type: String,
			required: true,
			// This should be a translation key for the modal title
			default: "common.VALLMissingTranslateLink",
		},
		textP: {
			type: String,
			required: true,
			// This should be a translation key for the modal paragraph text
			default: "common.VALLMissingTranslateLink",
		},
		deleteAction: {
			type: Function,
			required: false,
			// This function will be called when the delete is confirmed
			default: null,
		},
	},
	emits: ["closeModal"],
	data() {
		return {
			loading: false,
		};
	},
	methods: {
		async deleteConfirm() {
			this.loading = true;
			await this.deleteAction();
			this.loading = false;
		},
	},
};
</script>
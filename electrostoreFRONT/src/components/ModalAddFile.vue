<template>
	<div v-if="showModal" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center z-50"
		@click="$emit('closeModal')">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<Form :validation-schema="schemaAdd" v-slot="{ errors }">
				<h2 class="text-xl mb-4">{{ $t(textTitle) }}</h2>
				<div class="flex flex-col">
					<div class="flex flex-col">
						<Field :name="keyNameDocument" type="text"
							v-model="modalData[keyNameDocument]"
							:placeholder="$t(textPlaceholderDocument)"
							class="w-full p-2 border rounded"
							:class="{ 'border-red-500': errors[keyNameDocument] }" />
						<span class="text-red-500 h-5 w-full text-sm">{{ errors[keyNameDocument] || ' ' }}</span>
					</div>
					<div class="flex flex-col">
						<Field :name="keyFileDocument" type="file" @change="handleFileUpload" class="w-full p-2"
							:class="{ 'border-red-500': errors[keyFileDocument] }" />
						<span class="h-5 w-80 text-sm">{{ $t(textMaxSize) }} ({{ maxSizeInMb }}Mo)</span>
						<span class="text-red-500 h-5 w-full text-sm">{{ errors[keyFileDocument] || ' ' }}</span>
					</div>
				</div>
				<div class="flex justify-end space-x-4 mt-4">
					<button type="button" @click="$emit('closeModal')"
						class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
						{{ $t('components.VModalAddFileCancel') }}
					</button>
					<div class="relative">
						<button type="button" @click="addFile"
							class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600"
							:disabled="loading">
							{{ $t('components.VModalAddFileAdd') }}
						</button>
						<div v-if="loading" 
							class="absolute inset-0 bg-red-500 bg-opacity-90 rounded-lg flex items-center justify-center">
							<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
						</div>
					</div>
				</div>
			</Form>
		</div>
	</div>
</template>

<script>
import { Form, Field } from "vee-validate";
export default {
	name: "ModalAddFile",
	props: {
		showModal: {
			type: Boolean,
			required: true,
			// This should be a boolean to control the visibility of the modal
			default: false,
		},
		schemaAdd: {
			type: Object,
			required: true,
			// This should be a Yup validation schema for the add file form
		},
		keyNameDocument: {
			type: String,
			required: true,
			// This should be the key in the storeData for the document name
		},
		keyFileDocument: {
			type: String,
			required: true,
			// This should be the key in the storeData for the document file
		},
		textTitle: {
			type: String,
			required: true,
			// This should be a translation key for the modal title
			default: "common.VALLMissingTranslateLink",
		},
		textPlaceholderDocument: {
			type: String,
			required: false,
			// This should be a translation key for the document name placeholder
			default: "common.VALLMissingTranslateLink",
		},
		textMaxSize: {
			type: String,
			required: false,
			// This should be a translation key for the maximum size text
			default: "common.VALLMissingTranslateLink",
		},
		maxSizeInMb: {
			type: Number,
			required: false,
			// This should be the maximum file size allowed in megabytes
			default: 5,
		},
		modalData: {
			type: Object,
			required: true,
			// This should be an object containing the data for the document being added
		},
		addAction: {
			type: Function,
			required: false,
			// This function will be called when the add file is confirmed
			default: null,
		},
	},
	emits: ["closeModal"],
	data() {
		return {
			loading: false,
			file: null,
		};
	},
	components: {
		Form,
		Field,
	},
	methods: {
		async addFile() {
			this.loading = true;
			await this.addAction();
			this.loading = false;
		},
		handleFileUpload(event) {
			this.modalData[this.keyFileDocument] = event.target.files[0];
		},
	},
};
</script>
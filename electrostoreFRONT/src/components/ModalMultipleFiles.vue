<template>
	<div v-if="showModal" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center z-50"
		@click="$emit('closeModal')">
		<div class="bg-white p-6 rounded shadow-lg w-[600px] max-h-[80vh] flex flex-col" @click.stop>
			<h2 class="text-xl mb-4">{{ $t(textTitle) }}</h2>
			<div 
				@dragover.prevent="onDragOver"
				@dragleave.prevent="onDragLeave"
				@drop.prevent="onDrop"
				:class="['border-2 border-dashed rounded-lg p-8 mb-4 text-center transition-colors',
					isDragging ? 'border-blue-500 bg-blue-50' : 'border-gray-300 bg-gray-50']">
				<div class="flex flex-col items-center justify-center">
					<svg class="w-12 h-12 mb-3 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
							d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path>
					</svg>
					<p class="mb-2 text-sm text-gray-500">
						<span class="font-semibold">{{ $t('components.VModalMultipleFilesClickToUpload') }}</span>
						{{ $t('components.VModalMultipleFilesOrDragDrop') }}
					</p>
					<p class="text-xs text-gray-500">{{ $t('components.VModalMultipleFilesMaxSize') }} ({{ maxSizeInMb }}Mo)</p>
					<input
						ref="fileInput"
						type="file"
						multiple
						:accept="allowedExtensions.join(',') || ''"
						@change="onFileSelect"
						class="hidden" />
					<button
						type="button"
						@click="$refs.fileInput.click()"
						class="mt-4 px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
						{{ $t('components.VModalMultipleFilesSelectFiles') }}
					</button>
				</div>
			</div>
			<div class="flex-1 overflow-y-auto mb-4">
				<div v-if="filesList.length > 0" class="space-y-2">
					<div v-for="(fileItem, index) in filesList" :key="index"
						class="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-200">
						<div class="flex items-center flex-1 min-w-0">
							<svg class="w-5 h-5 text-gray-500 mr-3 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
								<path fill-rule="evenodd" 
									d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z"
									clip-rule="evenodd"></path>
							</svg>
							<div class="flex-1 min-w-0">
								<input 
									v-model="fileItem.name"
									type="text"
									:placeholder="$t('components.VModalMultipleFilesNamePlaceholder')"
									class="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:border-blue-500"
									:class="{ 'border-red-500': !fileItem.name }" />
								<p class="text-xs text-gray-500 mt-1">{{ formatFileSize(fileItem.document.size) }}</p>
							</div>
						</div>
						<button
							type="button"
							@click="removeFile(index)"
							class="ml-3 text-red-500 hover:text-red-700">
							<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
								<path fill-rule="evenodd" 
									d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" 
									clip-rule="evenodd"></path>
							</svg>
						</button>
					</div>
				</div>
				<div v-else class="text-center text-gray-500 py-8">
					{{ $t('components.VModalMultipleFilesNoFiles') }}
				</div>
			</div>
			<div v-if="errorMessage" class="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
				{{ errorMessage }}
			</div>
			<div class="flex justify-end space-x-4">
				<button 
					type="button" 
					@click="$emit('closeModal')"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('components.VModalMultipleFilesCancel') }}
				</button>
				<div class="relative">
					<button
						type="button"
						@click="saveFiles"
						:disabled="loading || filesList.length === 0"
						class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:bg-gray-300 disabled:cursor-not-allowed">
						{{ $t('components.VModalMultipleFilesSave') }} ({{ filesList.length }})
					</button>
					<div v-if="loading" 
						class="absolute inset-0 bg-blue-500 bg-opacity-90 rounded-lg flex items-center justify-center">
						<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script>
import { useConfigsStore } from "@/stores";

export default {
	name: "ModalMultipleFiles",
	props: {
		showModal: {
			type: Boolean,
			required: true,
			default: false,
		},
		textTitle: {
			type: String,
			required: false,
			default: "components.VModalMultipleFilesTitle",
		},
		maxSizeInMb: {
			type: Number,
			required: false,
			default: 5,
		},
		fileType: {
			type: String,
			required: false,
			default: "document",
		},
	},
	emits: ["closeModal", "filesSaved"],
	setup() {
		const configsStore = useConfigsStore();
		return {
			configsStore,
		};
	},
	data() {
		return {
			loading: false,
			isDragging: false,
			filesList: [],
			errorMessage: "",
			allowedExtensions: this.configsStore.getConfigByKey(
				this.fileType === "image" ? "allowed_image_extensions" : "allowed_document_extensions") || [],
		};
	},
	methods: {
		onDragOver(event) {
			this.isDragging = true;
		},
		onDragLeave(event) {
			this.isDragging = false;
		},
		onDrop(event) {
			this.isDragging = false;
			const files = Array.from(event.dataTransfer.files);
			this.addFiles(files);
		},
		onFileSelect(event) {
			const files = Array.from(event.target.files);
			this.addFiles(files);
			event.target.value = "";
		},
		addFiles(files) {
			this.errorMessage = "";
			const maxSizeInBytes = this.maxSizeInMb * 1024 * 1024;
			
			files.forEach((file) => {
				if (file.size > maxSizeInBytes) {
					this.errorMessage = this.$t("components.VModalMultipleFilesErrorSize", { 
						fileName: file.name, 
						maxSize: this.maxSizeInMb,
					});
					return;
				}
				if (this.allowedExtensions.length > 0) {
					const fileExt = "." + file.name.split(".").pop().toLowerCase();
					if (!this.allowedExtensions.includes(fileExt)) {
						this.errorMessage = this.$t("components.VModalMultipleFilesErrorExtension", { 
							fileName: file.name,
						});
						return;
					}
				}
				this.filesList.push({
					name: file.name.replace(/\.[^/.]+$/, ""),
					document: file,
				});
			});
		},
		removeFile(index) {
			this.filesList.splice(index, 1);
		},
		async saveFiles() {
			this.errorMessage = "";
			const hasEmptyName = this.filesList.some((item) => !item.name || item.name.trim() === "");
			if (hasEmptyName) {
				this.errorMessage = this.$t("components.VModalMultipleFilesErrorEmptyName");
				return;
			}
			this.loading = true;
			try {
				const savedFiles = [];
				for (const fileItem of this.filesList) {
					savedFiles.push({
						name: fileItem.name,
						document: fileItem.document,
					});
				}
				this.$emit("filesSaved", savedFiles);
				this.filesList = [];
				this.$emit("closeModal");
			} catch (error) {
				console.error("Error saving files:", error);
				this.errorMessage = this.$t("components.VModalMultipleFilesErrorSaving");
			} finally {
				this.loading = false;
			}
		},
		formatFileSize(bytes) {
			if (bytes === 0) {
				return "0 octet";
			}
			const k = 1024;
			const sizes = ["octets", "Ko", "Mo", "Go"];
			const i = Math.floor(Math.log(bytes) / Math.log(k));
			return Math.round(bytes / Math.pow(k, i) * 100) / 100 + " " + sizes[i];
		},
	},
};
</script>

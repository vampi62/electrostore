<template>
	<div v-if="showModal" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center z-50"
		@click="close">
		<div class="bg-white p-6 rounded shadow-lg w-full max-w-2xl max-h-[90vh] overflow-y-auto" @click.stop>
			<div class="flex justify-between items-center mb-4">
				<h2 class="text-xl">{{ $t('common.VAiTitle') }}</h2>
				<button type="button" class="text-gray-500 hover:text-gray-700" :title="$t('common.VAiClose')" @click="close">
					<font-awesome-icon icon="fa-solid fa-xmark" size="lg" />
				</button>
			</div>
			<form @submit.prevent="send">
				<textarea v-model="message" rows="4" :disabled="loading || recording" aria-label="message"
					:placeholder="$t('common.VAiPlaceholder')"
					class="w-full border border-gray-300 rounded-lg p-2 focus:outline-none focus:ring-2 focus:ring-blue-400"
					@keydown.ctrl.enter="send"></textarea>
				<div v-if="sttAvailable && !recording" :class="['mt-2 border-2 border-dashed rounded-lg p-3 text-center text-sm cursor-pointer',
					dragOver ? 'border-blue-400 bg-blue-50' : 'border-gray-300 text-gray-500']"
					@click="(($refs.audioInput as HTMLInputElement).click())" @dragover.prevent="dragOver = true" @dragleave.prevent="dragOver = false"
					@drop.prevent="onDrop">
					<font-awesome-icon icon="fa-solid fa-download" class="mr-2" />{{ $t('common.VAiDropAudio') }}
					<span class="block text-xs text-gray-400">{{ audioExtensions.join(' ') }} - max {{ maxAudioSize }} MB</span>
					<input ref="audioInput" type="file" class="hidden" :accept="audioExtensions.join(',')" @change="onFileSelected" />
				</div>
				<div v-if="audioBlob || recording" class="flex items-center gap-2 mt-2 text-sm text-gray-600">
					<span v-if="recording" class="flex items-center gap-2 text-red-500">
						<span class="w-2 h-2 bg-red-500 rounded-full animate-pulse"></span>
						{{ $t('common.VAiRecording') }}
					</span>
					<template v-else>
						<audio v-if="audioUrl" :src="audioUrl" controls class="h-8"></audio>
						<span>{{ audioName || $t('common.VAiAudioReady') }}</span>
						<button type="button" class="text-red-500 hover:text-red-600" :title="$t('common.VAiRemoveAudio')"
							:disabled="loading" @click="clearAudio">
							<font-awesome-icon icon="fa-solid fa-trash" />
						</button>
					</template>
				</div>
				<p v-if="micError" class="text-sm text-red-500 mt-2">{{ micError }}</p>
				<div class="flex justify-end items-center space-x-4 mt-4">
					<button v-if="sttAvailable" type="button" :disabled="loading"
						:title="recording ? $t('common.VAiStopRecord') : $t('common.VAiRecord')"
						:class="['px-4 py-2 rounded-lg text-white', recording ? 'bg-red-500 hover:bg-red-600' : 'bg-gray-500 hover:bg-gray-600']"
						@click="toggleRecording">
						<font-awesome-icon :icon="recording ? 'fa-solid fa-stop' : 'fa-solid fa-microphone'" />
					</button>
					<button type="submit" :disabled="!canSend"
						class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed">
						{{ loading ? $t('common.VAiSending') : $t('common.VAiSend') }}
					</button>
				</div>
			</form>
			<div v-if="response !== null" class="mt-4">
				<p class="text-sm font-medium mb-1">{{ $t('common.VAiResponse') }}</p>
				<pre class="bg-gray-100 border border-gray-300 rounded-lg p-3 text-sm whitespace-pre-wrap break-words max-h-80 overflow-y-auto">{{ response }}</pre>
			</div>
		</div>
	</div>
</template>

<script lang="ts">
import { fetchWrapper } from "@/helpers";
import { useConfigsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export default {
	name: "AiChatModal",
	props: {
		showModal: {
			type: Boolean,
			required: true,
			default: false,
		},
	},
	emits: ["closeModal"],
	setup() {
		const configsStore = useConfigsStore();
		return { configsStore };
	},
	data() {
		return {
			message: "",
			loading: false,
			response: null as string | null,
			recording: false,
			recorder: null as MediaRecorder | null,
			chunks: [] as Blob[],
			audioBlob: null as Blob | null,
			audioUrl: null as string | null,
			micError: "",
			dragOver: false,
			audioName: "",
		};
	},
	computed: {
		// the microphone is only offered when the server has a stt and the browser is able to record
		sttAvailable(): boolean {
			return this.configsStore.getStatusByKey("stt_status") === "healthy"
				&& typeof MediaRecorder !== "undefined"
				&& !!navigator.mediaDevices?.getUserMedia;
		},
		audioExtensions(): string[] {
			return this.configsStore.getConfigByKey("allowed_audio_extensions") || [];
		},
		audioMimeTypes(): string[] {
			return this.configsStore.getConfigByKey("allowed_audio_mime_types") || [];
		},
		maxAudioSize(): number {
			return this.configsStore.getConfigByKey("max_size_audio_in_mb") || 25;
		},
		canSend(): boolean {
			return !this.loading && !this.recording && (this.message.trim() !== "" || this.audioBlob !== null);
		},
	},
	watch: {
		showModal(value: boolean) {
			if (value) {
				this.configsStore.getHealth();
			} else {
				this.stopStream();
			}
		},
	},
	beforeUnmount() {
		this.stopStream();
		this.revokeAudioUrl();
	},
	methods: {
		close() {
			this.$emit("closeModal");
		},
		onDrop(event: DragEvent) {
			this.dragOver = false;
			const file = event.dataTransfer?.files?.[0];
			if (file) {
				this.setAudioFile(file);
			}
		},
		onFileSelected(event: Event) {
			const input = event.target as HTMLInputElement;
			const file = input.files?.[0];
			if (file) {
				this.setAudioFile(file);
			}
			input.value = "";
		},
		// a dropped file must match the types and size accepted by the api
		setAudioFile(file: File) {
			this.micError = "";
			const mime = file.type.split(";")[0];
			const extension = file.name.includes(".") ? "." + file.name.split(".").pop()!.toLowerCase() : "";
			if (!this.audioMimeTypes.includes(mime) && !this.audioExtensions.includes(extension)) {
				this.micError = this.$t("common.VAiAudioInvalidType", { types: this.audioExtensions.join(", ") });
				return;
			}
			if (file.size > this.maxAudioSize * 1024 * 1024) {
				this.micError = this.$t("common.VAiAudioTooBig", { size: this.maxAudioSize });
				return;
			}
			this.setAudio(file, file.name);
		},
		async send() {
			if (!this.canSend) {
				return;
			}
			this.loading = true;
			this.response = null;
			try {
				const formData = new FormData();
				// the api uses the text first, the audio is only transcribed when there is no text
				if (this.message.trim() !== "") {
					formData.append("content_ai_chat_message", this.message.trim());
				} else if (this.audioBlob) {
					formData.append("audio", this.audioBlob, this.audioName || "recording" + this.audioExtension(this.audioBlob.type));
				}
				const data = await fetchWrapper.post({ url: `${baseUrl}/ai/chat/messages`, useToken: "access", body: formData, contentFile: true });
				this.response = JSON.stringify(data, null, 2);
			} catch (error: any) {
				const detail = error?.data !== undefined ? JSON.stringify(error.data, null, 2) : "";
				this.response = `${this.$t("common.VAiError")}: ${error?.message ?? error}${detail ? "\n" + detail : ""}`;
			} finally {
				this.loading = false;
			}
		},
		async toggleRecording() {
			if (this.recording) {
				this.recorder?.stop();
				return;
			}
			this.micError = "";
			try {
				const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
				const recorder = new MediaRecorder(stream);
				this.chunks = [];
				recorder.ondataavailable = (e: BlobEvent) => {
					if (e.data.size > 0) {
						this.chunks.push(e.data);
					}
				};
				recorder.onstop = () => {
					stream.getTracks().forEach((track) => track.stop());
					this.recording = false;
					if (this.chunks.length > 0) {
						// the codecs parameter is removed, the api only accepts the bare mime type
						this.setAudio(new Blob(this.chunks, { type: (recorder.mimeType || "audio/webm").split(";")[0] }));
					}
				};
				this.recorder = recorder;
				recorder.start();
				this.recording = true;
			} catch {
				this.micError = this.$t("common.VAiMicDenied");
			}
		},
		setAudio(blob: Blob, name = "") {
			this.revokeAudioUrl();
			this.audioBlob = blob;
			this.audioName = name;
			this.audioUrl = URL.createObjectURL(blob);
		},
		clearAudio() {
			this.revokeAudioUrl();
			this.audioBlob = null;
			this.audioUrl = null;
		},
		revokeAudioUrl() {
			if (this.audioUrl) {
				URL.revokeObjectURL(this.audioUrl);
			}
		},
		// close the modal while recording: release the microphone and drop the partial recording
		stopStream() {
			if (this.recorder && this.recording) {
				this.chunks = [];
				this.recorder.stop();
			}
		},
		// MediaRecorder cannot produce mp3, it gives webm/ogg/mp4 which are accepted by the api
		audioExtension(mimeType: string): string {
			const base = mimeType.split(";")[0];
			const map: Record<string, string> = {
				"audio/webm": ".webm",
				"audio/ogg": ".ogg",
				"audio/mp4": ".m4a",
				"audio/mpeg": ".mp3",
				"audio/wav": ".wav",
			};
			return map[base] ?? ".webm";
		},
	},
};
</script>

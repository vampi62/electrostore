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
				<div v-if="reworking" class="flex justify-between items-start gap-2 mb-2 text-sm bg-blue-50 border border-blue-300 rounded-lg p-2">
					<span>{{ $t('common.VAiReworkHint') }}</span>
					<button type="button" class="text-gray-500 hover:text-gray-700 shrink-0" :title="$t('common.VAiReworkCancel')"
						:disabled="loading" @click="reworking = false">
						<font-awesome-icon icon="fa-solid fa-xmark" />
					</button>
				</div>
				<textarea ref="messageInput" v-model="message" rows="4" :disabled="loading" :readonly="recording || listening" aria-label="message"
					:placeholder="$t('common.VAiPlaceholder')"
					class="w-full border border-gray-300 rounded-lg p-2 focus:outline-none focus:ring-2 focus:ring-blue-400"
					@keydown.ctrl.enter="send" @paste="onPaste"></textarea>
				<div v-if="listening" class="flex items-center gap-2 mt-2 text-sm text-red-500">
					<span class="w-2 h-2 bg-red-500 rounded-full animate-pulse"></span>
					{{ $t('common.VAiListening') }}
				</div>
				<div v-if="sttProposal" class="mt-2 text-sm bg-yellow-50 border border-yellow-300 rounded-lg p-3" role="alert">
					<p class="mb-2">{{ $t(sttReasonKeys[sttProposal.reason]) }}</p>
					<div class="flex flex-wrap gap-2">
						<button v-if="sttProposal.canInstall" type="button" :disabled="sttInstalling"
							class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed" @click="installSpeechPack">
							<font-awesome-icon :icon="sttInstalling ? 'fa-solid fa-spinner' : 'fa-solid fa-download'" :spin="sttInstalling" class="mr-1" />
							{{ sttInstalling ? $t('common.VAiSttDownloading') : $t('common.VAiSttDownload', { lang: speechLang }) }}
						</button>
						<button v-if="sttProposal.canApi" type="button" :disabled="sttInstalling"
							class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed" @click="useApiStt">
							{{ $t('common.VAiSttUseApi') }}
						</button>
						<button type="button" :disabled="sttInstalling" class="px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed"
							@click="sttProposal = null">
							{{ $t('common.VAiConfirmCancel') }}
						</button>
					</div>
				</div>
				<div v-if="!recording" :class="['mt-2 border-2 border-dashed rounded-lg p-3 text-center text-sm cursor-pointer',
					dragOver ? 'border-blue-400 bg-blue-50' : 'border-gray-300 text-gray-500']"
					@click="(($refs.fileInput as HTMLInputElement).click())" @dragover.prevent="dragOver = true" @dragleave.prevent="dragOver = false"
					@drop.prevent="onDrop">
					<font-awesome-icon icon="fa-solid fa-paperclip" class="mr-2" />{{ $t('common.VAiDropFiles') }}
					<span class="block text-xs text-gray-400">{{ acceptedExtensions.join(' ') }}</span>
					<span class="block text-xs text-gray-400">{{ $t('common.VAiFilesLimits', { count: fileLimits.maxCount, image: fileLimits.maxImageSizeMb, text: fileLimits.maxTextSizeMb }) }}</span>
					<label for="file-input" class="sr-only">{{ $t('common.VAiDropFiles') }}</label>
					<input id="file-input" ref="fileInput" type="file" multiple class="hidden" :accept="acceptedExtensions.join(',')" @change="onFileSelected" @click.stop />
				</div>
				<ul v-if="files.length > 0" class="mt-2 flex flex-wrap gap-2">
					<li v-for="(file, index) in files" :key="`${file.name}-${file.size}-${file.lastModified}`"
						class="flex items-center gap-2 text-sm bg-gray-100 border border-gray-300 rounded-lg px-2 py-1">
						<font-awesome-icon :icon="file.type.startsWith('image/') ? 'fa-solid fa-image' : 'fa-solid fa-file'" />
						<span class="max-w-[12rem] truncate" :title="file.name">{{ file.name }}</span>
						<span class="text-xs text-gray-500">{{ formatSize(file.size) }}</span>
						<button type="button" class="text-red-500 hover:text-red-600" :title="$t('common.VAiRemoveFile')" :aria-label="$t('common.VAiRemoveFile')"
							:disabled="loading" @click="removeFile(index)">
							<font-awesome-icon icon="fa-solid fa-xmark" />
						</button>
					</li>
				</ul>
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
				<p v-if="fileError" class="text-sm text-red-500 mt-2 whitespace-pre-line">{{ fileError }}</p>
				<p v-if="micError" class="text-sm text-red-500 mt-2">{{ micError }}</p>
				<div class="flex justify-end items-center space-x-4 mt-4">
					<button v-if="micAvailable" type="button" :disabled="loading || sttInstalling"
						:title="micActive ? $t('common.VAiStopRecord') : $t('common.VAiRecord')"
						:class="['px-4 py-2 rounded-lg text-white', micActive ? 'bg-red-500 hover:bg-red-600' : 'bg-gray-500 hover:bg-gray-600']"
						@click="toggleRecording">
						<font-awesome-icon :icon="micActive ? 'fa-solid fa-stop' : 'fa-solid fa-microphone'" />
					</button>
					<button type="submit" :disabled="!canSend"
						class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 hover:cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed">
						{{ loading ? $t('common.VAiSending') : $t('common.VAiSend') }}
					</button>
				</div>
			</form>
			<p v-if="error" class="mt-4 text-sm text-red-500 whitespace-pre-wrap break-words">{{ error }}</p>
			<div v-if="aiStore.reply !== null" class="mt-4">
				<p v-if="aiStore.reply" class="text-sm whitespace-pre-wrap break-words bg-gray-100 border border-gray-300 rounded-lg p-3">{{ aiStore.reply }}</p>
				<div class="mt-4">
					<div class="flex justify-between items-center mb-1">
						<p class="text-sm font-medium">{{ $t('common.VAiPlanTitle') }}</p>
						<div v-if="rows.length > 0" class="flex space-x-2">
							<button type="button" class="px-3 py-1 text-sm bg-green-500 text-white rounded-lg hover:bg-green-600 disabled:opacity-50 disabled:cursor-not-allowed"
								:disabled="!canApplyAll" @click="applyAll">
								{{ $t('common.VAiApplyAll') }}
							</button>
							<button type="button" class="px-3 py-1 text-sm bg-gray-500 text-white rounded-lg hover:bg-gray-600" @click="requestNew">
								{{ $t('common.VAiNewRequest') }}
							</button>
						</div>
					</div>
					<p v-if="rows.length === 0" class="text-sm text-gray-500">{{ $t('common.VAiNoAction') }}</p>
					<template v-else>
						<p class="text-xs text-gray-500 mb-2">{{ $t('common.VAiPlanHint') }}</p>
						<table class="w-full text-sm border border-gray-300">
							<thead class="bg-gray-100 text-left">
								<tr>
									<th class="p-2">{{ $t('common.VAiColAction') }}</th>
									<th class="p-2">{{ $t('common.VAiColDetails') }}</th>
									<th class="p-2">{{ $t('common.VAiColStatus') }}</th>
									<th class="p-2"></th>
								</tr>
							</thead>
							<tbody>
								<tr v-for="row in rows" :key="row.id" class="border-t border-gray-200 align-top"
									:class="canOpen(row) ? 'cursor-pointer hover:bg-blue-50' : ''" :title="canOpen(row) ? $t('common.VAiOpen') : ''"
									@click="open(row)">
									<td class="p-2 font-medium">{{ actionLabel(row) }}</td>
									<td class="p-2">
										<div v-for="detail in details(row)" :key="detail.labelKey">
											<span class="text-gray-500">{{ $t(detail.labelKey) }} :</span>
											{{ detail.ref ? refLabel(detail.ref) : detail.value }}
										</div>
										<div v-if="row.error" class="text-red-500 break-words">{{ row.error }}</div>
									</td>
									<td class="p-2 whitespace-nowrap">{{ $t(statusKeys[statusOf(row)]) }}</td>
									<td class="p-2 text-right whitespace-nowrap">
										<button v-if="canApply(row)" type="button" class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600"
											@click.stop="apply(row)">
											{{ $t('common.VAiApply') }}
										</button>
										<button v-if="!row.applying" type="button" class="ml-2 px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600"
											:title="$t('common.VAiDelete')" :aria-label="$t('common.VAiDelete')" @click.stop="removeRow(row)">
											<font-awesome-icon icon="fa-solid fa-trash" />
										</button>
									</td>
								</tr>
							</tbody>
						</table>
					</template>
				</div>
				<details class="mt-4">
					<summary class="text-sm font-medium cursor-pointer">{{ $t('common.VAiResponse') }}</summary>
					<pre class="bg-gray-100 border border-gray-300 rounded-lg p-3 text-sm whitespace-pre-wrap break-words max-h-80 overflow-y-auto">{{ aiStore.raw }}</pre>
				</details>
			</div>
		</div>
		<!-- some proposals are not validated yet: validate them, abandon them or ask the assistant to rework them -->
		<div v-if="confirmContext" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center z-[60]"
			@click.stop="confirmContext = null">
			<div class="bg-white p-6 rounded shadow-lg w-full max-w-md" role="alertdialog" aria-modal="true" @click.stop>
				<h3 class="text-lg font-medium mb-2">{{ $t('common.VAiConfirmTitle') }}</h3>
				<p class="text-sm mb-4">{{ $t('common.VAiConfirmMessage', { count: aiStore.pendingCount }) }}</p>
				<div class="flex flex-col gap-2">
					<button type="button" class="px-4 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 disabled:opacity-50 disabled:cursor-not-allowed"
						:disabled="!canApplyAll" @click="confirmValidate">
						{{ $t('common.VAiConfirmValidate') }}
					</button>
					<button type="button" class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600" @click="confirmRework">
						{{ $t('common.VAiConfirmRework') }}
					</button>
					<button type="button" class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600" @click="confirmAbandon">
						{{ $t('common.VAiConfirmAbandon') }}
					</button>
					<button type="button" class="px-4 py-2 bg-gray-500 text-white rounded-lg hover:bg-gray-600" @click="confirmContext = null">
						{{ $t('common.VAiConfirmCancel') }}
					</button>
				</div>
			</div>
		</div>
	</div>
</template>

<script lang="ts">
import {
	fetchWrapper, aiActionDefinition, aiActionDetails, aiActionPath, aiActionRefs, isAiActionSaved,
	checkAiFile, formatFileSize, getSpeechRecognition, localSpeechStatus, installLocalSpeech, recognitionLang, transcriptOf, joinSpeechText,
} from "@/helpers";
import type { AiProposedAction, AiRefKind, AiFileLimits, SpeechRecognitionLike } from "@/helpers";
import { i18n } from "@/plugins";
import { useAiStore, useConfigsStore, useTabsStore, useItemsStore, useTagsStore, useStoresBoxsStore, useItemsTagsStore, useItemsBoxsStore } from "@/stores";
import type { PlanRow, AiHistoryMessage } from "@/stores";
import { ApiError, getMessage } from "@/utils";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

type RowStatus = "pending" | "opened" | "applying" | "done" | "error";

interface ChatResponse {
	message?: { content?: string | null };
	proposed_actions?: AiProposedAction[] | null;
}

const statusKeys: Record<RowStatus, string> = {
	pending: "common.VAiStatusPending",
	opened: "common.VAiStatusOpened",
	applying: "common.VAiStatusApplying",
	done: "common.VAiStatusDone",
	error: "common.VAiStatusError",
};

// why the speech recognition of the browser cannot be used, and what is proposed to the user instead
type SttReason = "unsupported" | "error" | "language";

interface SttProposal {
	reason: SttReason;
	// the language pack of the browser can be downloaded
	canInstall: boolean;
	// the stt of the server is active
	canApi: boolean;
}

const sttReasonKeys: Record<SttReason, string> = {
	unsupported: "common.VAiSttReasonUnsupported",
	error: "common.VAiSttReasonError",
	language: "common.VAiSttReasonLanguage",
};

// the api limits the size of a message
const MAX_HISTORY_MESSAGE = 4000;
const MAX_HISTORY_MESSAGES = 10;
// appended to a rework request: the proposals not applied are replaced by the new ones
const REWORK_INSTRUCTION = "\n\n(Rework request: the proposed actions that are not applied are discarded and replaced by your new proposals, so propose again every action that is still needed. Do not propose again the actions already applied.)";

const asRecord = (payload: unknown) => (payload && typeof payload === "object" ? payload as Record<string, unknown> : {});
// optional fields are left out: a FormData would send "undefined"
const optional = (value: unknown) => (value === undefined || value === null || value === "" ? undefined : String(value));

export default {
	name: "AiChatModal",
	props: {
		showModal: {
			type: Boolean,
			required: true,
		},
	},
	emits: ["closeModal"],
	setup() {
		const aiStore = useAiStore();
		const configsStore = useConfigsStore();
		const tabsStore = useTabsStore();
		const itemsStore = useItemsStore();
		const tagsStore = useTagsStore();
		const storesBoxsStore = useStoresBoxsStore();
		const itemsTagsStore = useItemsTagsStore();
		const itemsBoxsStore = useItemsBoxsStore();
		return { aiStore, configsStore, tabsStore, itemsStore, tagsStore, storesBoxsStore, itemsTagsStore, itemsBoxsStore, statusKeys, sttReasonKeys };
	},
	data() {
		return {
			message: "",
			loading: false,
			// the plan (answer and proposals of the assistant) is in the ai store: it stays when the modal is closed to let the user edit the proposals in the tabs
			error: "",
			// the next message is a request to rework the proposals not validated yet
			reworking: false,
			// the user has to decide what happens to the proposals not validated yet, before a new request ("new") or the sending of a message ("send")
			confirmContext: null as "new" | "send" | null,
			recording: false,
			recorder: null as MediaRecorder | null,
			chunks: [] as Blob[],
			audioBlob: null as Blob | null,
			audioUrl: null as string | null,
			micError: "",
			dragOver: false,
			audioName: "",
			// the images and text files the assistant reads with the message
			files: [] as File[],
			fileError: "",
			// the browser converts the voice to text while the user speaks
			listening: false,
			speech: null as SpeechRecognitionLike | null,
			// the text of the message when the recognition started: the spoken words come after it
			speechBase: "",
			// the recognition stops by itself after a silence: it is started again until the user stops it
			speechWanted: false,
			speechFailure: null as SttReason | null,
			// the recognition of the browser failed: not tried again before the user downloaded the language pack
			browserSttBroken: false,
			// the user chose the stt of the server
			preferApiStt: false,
			sttProposal: null as SttProposal | null,
			sttInstalling: false,
		};
	},
	computed: {
		rows(): PlanRow[] {
			return this.aiStore.rows;
		},
		// the server has a stt: it transcribes the audio files and the recordings
		apiSttHealthy(): boolean {
			return this.configsStore.getStatusByKey("stt_status") === "healthy";
		},
		// the voice can be recorded in the browser and sent to the stt of the server
		apiRecordAvailable(): boolean {
			return this.apiSttHealthy
				&& typeof MediaRecorder !== "undefined"
				&& !!navigator.mediaDevices?.getUserMedia;
		},
		// the microphone is offered when the browser can convert the voice, or when the server can
		micAvailable(): boolean {
			return getSpeechRecognition() !== null || this.apiRecordAvailable;
		},
		micActive(): boolean {
			return this.recording || this.listening;
		},
		// language of the recognition of the browser, the one of the application
		speechLang(): string {
			return recognitionLang(i18n.global.locale.value);
		},
		fileLimits(): AiFileLimits {
			const list = (key: string) => (this.configsStore.getConfigByKey(key) as string[] | null) ?? [];
			return {
				maxCount: this.configsStore.getConfigNumber("max_count_ai_files") || 5,
				imageMimeTypes: list("allowed_image_mime_types"),
				imageExtensions: list("allowed_image_extensions"),
				maxImageSizeMb: this.configsStore.getConfigNumber("max_size_image_in_mb") || 5,
				textExtensions: list("allowed_ai_text_extensions"),
				maxTextSizeMb: this.configsStore.getConfigNumber("max_size_document_in_mb") || 5,
				audio: this.apiSttHealthy ? {
					mimeTypes: list("allowed_audio_mime_types"),
					extensions: list("allowed_audio_extensions"),
					maxSizeMb: this.configsStore.getConfigNumber("max_size_audio_in_mb") || 25,
				} : null,
			};
		},
		acceptedExtensions(): string[] {
			const { imageExtensions, textExtensions, audio } = this.fileLimits;
			return [...new Set([...imageExtensions, ...textExtensions, ...(audio?.extensions ?? [])])];
		},
		canSend(): boolean {
			return !this.loading && !this.micActive && (this.message.trim() !== "" || this.audioBlob !== null || this.files.length > 0);
		},
		canApplyAll(): boolean {
			return this.rows.some((row) => this.canApply(row));
		},
		// pages displayed by the tabs the proposals were opened in
		tabPaths(): string[] {
			return this.rows.map((row) => this.tabOf(row)?.fullPath ?? "");
		},
	},
	watch: {
		// a proposal saved from its tab is done: the tab now displays the created record
		tabPaths(paths: string[]) {
			this.rows.forEach((row, index) => {
				if (!row.applied && paths[index] && isAiActionSaved(row.action, paths[index])) {
					row.applied = true;
					row.createdPath = paths[index].split("?")[0];
				}
			});
		},
		showModal(value: boolean) {
			if (value) {
				this.configsStore.getHealth();
			} else {
				this.confirmContext = null;
				this.sttProposal = null;
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
			this.addFiles(Array.from(event.dataTransfer?.files ?? []));
		},
		onFileSelected(event: Event) {
			const input = event.target as HTMLInputElement;
			this.addFiles(Array.from(input.files ?? []));
			input.value = "";
		},
		// a screenshot pasted in the message is attached, a pasted text is left to the textarea
		onPaste(event: ClipboardEvent) {
			const pasted = Array.from(event.clipboardData?.files ?? []);
			if (pasted.length > 0) {
				event.preventDefault();
				this.addFiles(pasted);
			}
		},
		// the files must match the types and sizes accepted by the api: an audio file is transcribed, the others are read by the assistant
		addFiles(candidates: File[]) {
			this.fileError = "";
			this.micError = "";
			const errors: string[] = [];
			for (const file of candidates) {
				const check = checkAiFile(file, this.fileLimits);
				if ("error" in check) {
					errors.push(this.fileErrorMessage(file, check));
				} else if (check.kind === "audio") {
					this.setAudio(file, file.name);
				} else if (this.files.some((attached) => attached.name === file.name && attached.size === file.size && attached.lastModified === file.lastModified)) {
					continue;
				} else if (this.files.length >= this.fileLimits.maxCount) {
					errors.push(this.$t("common.VAiTooManyFiles", { count: this.fileLimits.maxCount }));
					break;
				} else {
					this.files.push(file);
				}
			}
			this.fileError = errors.join("\n");
		},
		fileErrorMessage(file: File, check: { error: "type" | "empty" | "tooBig", maxSizeMb?: number }): string {
			if (check.error === "tooBig") {
				return this.$t("common.VAiFileTooBig", { name: file.name, size: check.maxSizeMb });
			}
			return this.$t(check.error === "empty" ? "common.VAiFileEmpty" : "common.VAiFileInvalidType", { name: file.name });
		},
		removeFile(index: number) {
			this.files.splice(index, 1);
			this.fileError = "";
		},
		formatSize(bytes: number): string {
			return formatFileSize(bytes);
		},
		async send() {
			if (!this.canSend) {
				return;
			}
			// the proposals not validated yet must not be lost silently by a new request
			if (this.aiStore.pendingCount > 0 && !this.reworking) {
				this.confirmContext = "send";
				return;
			}
			await this.submit(this.reworking);
		},
		// withHistory: the exchange so far is sent with the message, the assistant reworks its previous proposals
		async submit(withHistory: boolean) {
			this.loading = true;
			this.error = "";
			try {
				const text = this.message.trim();
				const formData = this.requestBody(text, withHistory);
				const data = await fetchWrapper.post<ChatResponse>({ url: `${baseUrl}/ai/chat/messages`, useToken: "access", body: formData, contentFile: true });
				// the plan is replaced once the answer is known: a failure keeps the previous proposals
				const kept = withHistory ? this.aiStore.rows.filter((row) => row.applied) : [];
				const reply = data.message?.content ?? "";
				this.aiStore.history = [
					...(withHistory ? this.aiStore.history : []),
					{ role: "user", content: this.sentSummary(text) },
					{ role: "assistant", content: reply },
				];
				this.aiStore.raw = JSON.stringify(data, null, 2);
				this.aiStore.reply = reply;
				this.aiStore.rows = [...kept, ...(data.proposed_actions ?? []).map((action) => ({
					id: this.aiStore.nextRowId++,
					action,
					applying: false,
					applied: false,
					error: "",
					tabId: null,
					createdPath: null,
				}))];
				this.reworking = false;
				this.message = "";
				this.files = [];
				this.fileError = "";
				this.clearAudio();
				this.loadReferences();
			} catch (error) {
				const detail = error instanceof ApiError && error.data !== undefined ? JSON.stringify(error.data, null, 2) : "";
				this.error = `${this.$t("common.VAiError")}: ${getMessage(error)}${detail ? "\n" + detail : ""}`;
			} finally {
				this.loading = false;
			}
		},
		requestBody(text: string, withHistory: boolean): FormData {
			const formData = new FormData();
			const history = withHistory ? this.historyToSend() : [];
			history.forEach((entry, index) => {
				formData.append(`history[${index}].role`, entry.role);
				formData.append(`history[${index}].content`, entry.content);
			});
			// the api uses the text first, the audio is only transcribed when there is no text
			if (text !== "") {
				formData.append("content_ai_chat_message", withHistory ? text + REWORK_INSTRUCTION : text);
			} else if (this.audioBlob) {
				formData.append("audio", this.audioBlob, this.audioName || "recording" + this.audioExtension(this.audioBlob.type));
			}
			for (const file of this.files) {
				formData.append("files", file, file.name);
			}
			return formData;
		},
		// what the user said, kept in the history: the files are not kept with the exchange, the assistant only knows their names
		sentSummary(text: string): string {
			const attached = this.files.length > 0 ? `(attached files: ${this.files.map((file) => file.name).join(", ")})` : "";
			return [text || (this.audioBlob ? "(voice message)" : ""), attached].filter(Boolean).join("\n");
		},
		// the exchange so far, the last answer of the assistant is completed with the state of its proposals (the user may have applied some of them)
		historyToSend(): AiHistoryMessage[] {
			const messages = this.aiStore.history.map((message) => ({ ...message }));
			const last = messages[messages.length - 1];
			const actions = this.aiStore.rows.map((row) => `- ${JSON.stringify(row.action)} [${row.applied ? "already applied" : "not applied"}]`);
			if (last?.role === "assistant" && actions.length > 0) {
				const suffix = "\n\nProposed actions:\n" + actions.join("\n");
				last.content = last.content.slice(0, Math.max(0, MAX_HISTORY_MESSAGE - suffix.length)) + suffix;
			}
			return messages.slice(-MAX_HISTORY_MESSAGES).map((message) => ({
				role: message.role,
				// the api refuses an empty message
				content: message.content.slice(0, MAX_HISTORY_MESSAGE) || "(no text)",
			}));
		},
		// the names of the existing items and tags an action refers to, best effort: the id is displayed when they cannot be loaded
		loadReferences() {
			const loads: Array<Promise<unknown>> = [];
			for (const { kind, id } of this.rows.flatMap((row) => aiActionRefs(row.action))) {
				if (kind === "item" && !this.itemsStore.items[id]) {
					loads.push(this.itemsStore.getItemById(id));
				} else if (kind === "tag" && !this.tagsStore.tags[id]) {
					loads.push(this.tagsStore.getTagById(id));
				}
			}
			Promise.allSettled(loads);
		},
		refLabel(ref: { kind: AiRefKind, id: string }): string {
			const name = ref.kind === "item" ? this.itemsStore.items[ref.id]?.friendly_name_item
				: ref.kind === "tag" ? this.tagsStore.tags[ref.id]?.name_tag
					: undefined;
			return name ? `${name} (#${ref.id})` : `#${ref.id}`;
		},
		actionLabel(row: PlanRow): string {
			const definition = aiActionDefinition(row.action);
			return definition ? this.$t(definition.labelKey) : String(row.action.action_type);
		},
		details(row: PlanRow) {
			return aiActionDetails(row.action);
		},
		// the tab the proposal is edited in, if it was opened and not closed
		tabOf(row: PlanRow) {
			return row.tabId ? this.tabsStore.tabs.find((tab) => tab.id === row.tabId) : undefined;
		},
		statusOf(row: PlanRow): RowStatus {
			if (row.applying) {
				return "applying";
			}
			if (row.applied) {
				return "done";
			}
			if (row.error) {
				return "error";
			}
			// a closed tab loses the edition: the proposal is back to pending
			return this.tabOf(row) ? "opened" : "pending";
		},
		canOpen(row: PlanRow): boolean {
			return aiActionPath(row.action) !== undefined;
		},
		canApply(row: PlanRow): boolean {
			const status = this.statusOf(row);
			return aiActionDefinition(row.action) !== undefined && (status === "pending" || status === "error");
		},
		// open the proposal in a tab, the user can modify it and save it there
		open(row: PlanRow) {
			const path = row.createdPath ?? aiActionPath(row.action);
			if (!path) {
				return;
			}
			if (this.tabOf(row)) {
				this.tabsStore.activate(row.tabId!);
			} else {
				this.tabsStore.open(path);
				row.tabId = this.tabsStore.activeId ?? null;
			}
			this.close();
		},
		async apply(row: PlanRow) {
			if (!this.canApply(row)) {
				return;
			}
			row.applying = true;
			row.error = "";
			try {
				row.createdPath = await this.applyAction(row.action);
				row.applied = true;
			} catch (error) {
				row.error = getMessage(error);
			} finally {
				row.applying = false;
			}
		},
		async applyAll() {
			// one after the other: a failure does not stop the others
			for (const row of this.rows) {
				await this.apply(row);
			}
		},
		// create the proposed record, returns the page of the created record if there is one
		async applyAction(action: AiProposedAction): Promise<string | null> {
			const payload = asRecord(action.payload);
			switch (action.action_type) {
			case "create_item": {
				const id = await this.itemsStore.createItem({
					reference_name_item: String(payload.reference_name_item),
					friendly_name_item: String(payload.friendly_name_item),
					threshold_min_item: Number(payload.threshold_min_item),
					...(optional(payload.description_item) !== undefined && { description_item: optional(payload.description_item) }),
				}, true);
				return `/inventory/${id}`;
			}
			case "create_tag": {
				const id = await this.tagsStore.createTag({ name_tag: String(payload.name_tag), weight_tag: Number(payload.weight_tag) });
				return `/tags/${id}`;
			}
			case "create_box": {
				await this.storesBoxsStore.createBox({ id_store: String(payload.id_store) }, {
					xstart_box: Number(payload.xstart_box),
					ystart_box: Number(payload.ystart_box),
					xend_box: Number(payload.xend_box),
					yend_box: Number(payload.yend_box),
				});
				return `/stores/${String(payload.id_store)}`;
			}
			case "attach_tag":
				await this.itemsTagsStore.createItemTag({ id_item: String(payload.id_item) }, { id_tag: Number(payload.id_tag) });
				return null;
			case "move_item_stock":
				await this.itemsBoxsStore.createItemBox({ id_item: String(payload.id_item) }, {
					id_box: Number(payload.id_box),
					quantity_item_box: Number(payload.quantity_item_box),
					threshold_max_item_item_box: Number(payload.threshold_max_item_item_box),
				});
				return null;
			default:
				throw new Error(String(action.action_type));
			}
		},
		resetPlan() {
			this.aiStore.reset();
			this.error = "";
			this.reworking = false;
		},
		removeRow(row: PlanRow) {
			if (!row.applying) {
				this.aiStore.removeRow(row.id);
			}
		},
		// "new request": proposals waiting for a validation are not dropped without the agreement of the user
		requestNew() {
			if (this.aiStore.pendingCount > 0) {
				this.confirmContext = "new";
			} else {
				this.resetPlan();
			}
		},
		// the proposals are validated or abandoned: the new request can start (and the message waiting be sent)
		async startNew(context: "new" | "send" | null) {
			this.resetPlan();
			if (context === "send") {
				await this.submit(false);
			}
		},
		async confirmValidate() {
			const context = this.confirmContext;
			this.confirmContext = null;
			await this.applyAll();
			// a proposal in error or being edited in a tab is still waiting: the plan stays to let the user finish
			if (this.aiStore.pendingCount === 0) {
				await this.startNew(context);
			}
		},
		async confirmAbandon() {
			const context = this.confirmContext;
			this.confirmContext = null;
			await this.startNew(context);
		},
		async confirmRework() {
			const context = this.confirmContext;
			this.confirmContext = null;
			if (context === "send") {
				await this.submit(true);
			} else {
				// the user describes the changes in the message box
				this.reworking = true;
				this.message = "";
				await this.$nextTick();
				(this.$refs.messageInput as HTMLTextAreaElement | undefined)?.focus();
			}
		},
		// the microphone: the speech recognition of the browser first, the stt of the server when the user chose it or when the browser has none
		async toggleRecording() {
			if (this.listening) {
				this.stopListening();
				return;
			}
			if (this.recording) {
				this.recorder?.stop();
				return;
			}
			this.micError = "";
			this.sttProposal = null;
			if (this.preferApiStt && this.apiRecordAvailable) {
				await this.startRecording();
			} else if (getSpeechRecognition() && !this.browserSttBroken) {
				await this.startListening();
			} else {
				await this.proposeStt(getSpeechRecognition() ? "error" : "unsupported");
			}
		},
		// the words are written in the message while the user speaks
		async startListening() {
			const Recognition = getSpeechRecognition();
			if (!Recognition) {
				return;
			}
			const recognition = new Recognition();
			recognition.lang = this.speechLang;
			recognition.continuous = true;
			recognition.interimResults = true;
			// the language pack of the browser is installed: the voice does not leave the device
			recognition.processLocally = await localSpeechStatus(this.speechLang, Recognition) === "available";
			recognition.onresult = (event) => {
				const { final, interim } = transcriptOf(event.results);
				this.message = joinSpeechText(this.speechBase, final + interim);
			};
			recognition.onerror = (event) => this.onSpeechError(event.error);
			recognition.onend = () => this.onSpeechEnd(recognition);
			this.speech = recognition;
			this.speechBase = this.message;
			this.speechWanted = true;
			this.speechFailure = null;
			try {
				recognition.start();
				this.listening = true;
			} catch {
				this.finishListening();
				this.micError = this.$t("common.VAiMicDenied");
			}
		},
		stopListening() {
			this.speechWanted = false;
			this.speech?.stop();
		},
		onSpeechError(error: string) {
			// a silence or a stop ends the dictation without error
			if (error === "no-speech" || error === "aborted") {
				this.speechWanted = false;
			} else if (error === "not-allowed" || error === "audio-capture") {
				this.speechWanted = false;
				this.micError = this.$t(error === "not-allowed" ? "common.VAiMicDenied" : "common.VAiMicMissing");
			} else {
				// the service of the browser is not reachable, or does not know the language
				this.speechWanted = false;
				this.browserSttBroken = true;
				this.speechFailure = error === "language-not-supported" ? "language" : "error";
			}
		},
		async onSpeechEnd(recognition: SpeechRecognitionLike) {
			if (recognition !== this.speech) {
				return;
			}
			if (this.speechWanted) {
				// the browser stopped after a silence: the dictation goes on, after what is already written
				try {
					this.speechBase = this.message;
					recognition.start();
					return;
				} catch {
					this.speechWanted = false;
				}
			}
			const failure = this.speechFailure;
			this.finishListening();
			if (failure) {
				await this.proposeStt(failure);
			}
		},
		finishListening() {
			this.listening = false;
			this.speechWanted = false;
			this.speech = null;
		},
		// the browser cannot convert the voice: the user chooses to download its language pack or to use the stt of the server
		async proposeStt(reason: SttReason) {
			const Recognition = getSpeechRecognition();
			const canInstall = Recognition !== null
				&& typeof Recognition.install === "function"
				&& await localSpeechStatus(this.speechLang, Recognition) === "downloadable";
			const canApi = this.apiRecordAvailable;
			if (!canInstall && !canApi) {
				this.micError = this.$t("common.VAiSttNone");
				return;
			}
			this.sttProposal = { reason, canInstall, canApi };
		},
		async installSpeechPack() {
			this.sttInstalling = true;
			this.micError = "";
			const installed = await installLocalSpeech(this.speechLang);
			this.sttInstalling = false;
			if (installed) {
				this.browserSttBroken = false;
				this.sttProposal = null;
				await this.startListening();
			} else {
				this.micError = this.$t("common.VAiSttDownloadFailed");
			}
		},
		async useApiStt() {
			this.preferApiStt = true;
			this.sttProposal = null;
			await this.startRecording();
		},
		// the voice is recorded and sent to the stt of the server with the message
		async startRecording() {
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
					for (const track of stream.getTracks()) {
						track.stop();
					}
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
			this.audioName = "";
		},
		revokeAudioUrl() {
			if (this.audioUrl) {
				URL.revokeObjectURL(this.audioUrl);
			}
		},
		// close the modal while recording: release the microphone and drop the partial recording
		stopStream() {
			if (this.speech && this.listening) {
				this.speechWanted = false;
				this.speech.abort();
			}
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

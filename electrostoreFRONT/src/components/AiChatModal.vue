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
					<label for="audio-input" class="sr-only">{{ $t('common.VAiDropAudio') }}</label>
					<input id="audio-input" ref="audioInput" type="file" class="hidden" :accept="audioExtensions.join(',')" @change="onFileSelected" />
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
						class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 hover:cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed">
						{{ loading ? $t('common.VAiSending') : $t('common.VAiSend') }}
					</button>
				</div>
			</form>
			<p v-if="error" class="mt-4 text-sm text-red-500 whitespace-pre-wrap break-words">{{ error }}</p>
			<div v-if="reply !== null" class="mt-4">
				<p v-if="reply" class="text-sm whitespace-pre-wrap break-words bg-gray-100 border border-gray-300 rounded-lg p-3">{{ reply }}</p>
				<div class="mt-4">
					<div class="flex justify-between items-center mb-1">
						<p class="text-sm font-medium">{{ $t('common.VAiPlanTitle') }}</p>
						<div v-if="rows.length > 0" class="flex space-x-2">
							<button type="button" class="px-3 py-1 text-sm bg-green-500 text-white rounded-lg hover:bg-green-600 disabled:opacity-50 disabled:cursor-not-allowed"
								:disabled="!canApplyAll" @click="applyAll">
								{{ $t('common.VAiApplyAll') }}
							</button>
							<button type="button" class="px-3 py-1 text-sm bg-gray-500 text-white rounded-lg hover:bg-gray-600" @click="resetPlan">
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
									<td class="p-2 text-right">
										<button v-if="canApply(row)" type="button" class="px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600"
											@click.stop="apply(row)">
											{{ $t('common.VAiApply') }}
										</button>
									</td>
								</tr>
							</tbody>
						</table>
					</template>
				</div>
				<details class="mt-4">
					<summary class="text-sm font-medium cursor-pointer">{{ $t('common.VAiResponse') }}</summary>
					<pre class="bg-gray-100 border border-gray-300 rounded-lg p-3 text-sm whitespace-pre-wrap break-words max-h-80 overflow-y-auto">{{ raw }}</pre>
				</details>
			</div>
		</div>
	</div>
</template>

<script lang="ts">
import { fetchWrapper, aiActionDefinition, aiActionDetails, aiActionPath, aiActionRefs, isAiActionSaved } from "@/helpers";
import type { AiProposedAction, AiRefKind } from "@/helpers";
import { useConfigsStore, useTabsStore, useItemsStore, useTagsStore, useStoresBoxsStore, useItemsTagsStore, useItemsBoxsStore } from "@/stores";
import { ApiError, getMessage } from "@/utils";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

type RowStatus = "pending" | "opened" | "applying" | "done" | "error";

// one proposed action of the assistant, nothing is applied until the user validates it (in a tab or from the table)
interface PlanRow {
	id: number;
	action: AiProposedAction;
	applying: boolean;
	applied: boolean;
	error: string;
	// tab opened to edit the proposal, and the page of the record once created from the table
	tabId: string | null;
	createdPath: string | null;
}

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
		const configsStore = useConfigsStore();
		const tabsStore = useTabsStore();
		const itemsStore = useItemsStore();
		const tagsStore = useTagsStore();
		const storesBoxsStore = useStoresBoxsStore();
		const itemsTagsStore = useItemsTagsStore();
		const itemsBoxsStore = useItemsBoxsStore();
		return { configsStore, tabsStore, itemsStore, tagsStore, storesBoxsStore, itemsTagsStore, itemsBoxsStore, statusKeys };
	},
	data() {
		return {
			message: "",
			loading: false,
			// answer of the assistant, the plan stays when the modal is closed to let the user edit the proposals in the tabs
			reply: null as string | null,
			rows: [] as PlanRow[],
			raw: "",
			error: "",
			nextRowId: 1,
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
			return (this.configsStore.getConfigByKey("allowed_audio_extensions") as string[]) || [];
		},
		audioMimeTypes(): string[] {
			return (this.configsStore.getConfigByKey("allowed_audio_mime_types") as string[]) || [];
		},
		maxAudioSize(): number {
			return (this.configsStore.getConfigNumber("max_size_audio_in_mb") as number) || 25;
		},
		canSend(): boolean {
			return !this.loading && !this.recording && (this.message.trim() !== "" || this.audioBlob !== null);
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
			this.reply = null;
			this.rows = [];
			this.raw = "";
			this.error = "";
			try {
				const formData = new FormData();
				// the api uses the text first, the audio is only transcribed when there is no text
				if (this.message.trim() !== "") {
					formData.append("content_ai_chat_message", this.message.trim());
				} else if (this.audioBlob) {
					formData.append("audio", this.audioBlob, this.audioName || "recording" + this.audioExtension(this.audioBlob.type));
				}
				const data = await fetchWrapper.post<ChatResponse>({ url: `${baseUrl}/ai/chat/messages`, useToken: "access", body: formData, contentFile: true });
				this.raw = JSON.stringify(data, null, 2);
				this.reply = data.message?.content ?? "";
				this.rows = (data.proposed_actions ?? []).map((action) => ({
					id: this.nextRowId++,
					action,
					applying: false,
					applied: false,
					error: "",
					tabId: null,
					createdPath: null,
				}));
				this.loadReferences();
			} catch (error) {
				const detail = error instanceof ApiError && error.data !== undefined ? JSON.stringify(error.data, null, 2) : "";
				this.error = `${this.$t("common.VAiError")}: ${getMessage(error)}${detail ? "\n" + detail : ""}`;
			} finally {
				this.loading = false;
			}
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
			void Promise.allSettled(loads);
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
			this.reply = null;
			this.rows = [];
			this.raw = "";
			this.error = "";
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

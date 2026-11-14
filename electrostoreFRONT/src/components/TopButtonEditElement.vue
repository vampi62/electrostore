<template>
	<!-- Desktop -->
	<div class="hidden md:flex flex-wrap gap-2 justify-end">
		<div v-for="(button, index) in visibleButtons" :key="index" class="relative">
			<button type="button" @click="onClick(button)"
				:disabled="button.disabled"
				:class="['px-4 py-2 rounded flex items-center', button.textColor, button.bgColor,
				button.disabled ? 'opacity-50 cursor-not-allowed' : button.hoverColor]">
				{{ $t(button.label) }}
			</button>
			<div v-show="button.loading"
				:class="['absolute inset-0 bg-opacity-90 rounded flex items-center justify-center', button.bgColor]">
				<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
			</div>
		</div>
	</div>
	<!-- Mobile -->
	<div class="md:hidden relative z-50">
		<button @click="showMobileMenu = !showMobileMenu"
			class="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600 flex items-center w-full justify-between">
			{{ $t('components.VModalTopButtonMenu') }}
			<svg
				xmlns="http://www.w3.org/2000/svg"
				class="h-5 w-5 ml-2"
				viewBox="0 0 20 20"
				fill="currentColor">
				<path
					fill-rule="evenodd"
					d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"
					clip-rule="evenodd" />
			</svg>
		</button>
		<!-- Menu -->
		<div v-if="showMobileMenu"
			class="absolute right-0 mt-2 w-48 rounded-md shadow-lg border border-gray-200">
			<div v-for="(button, index) in visibleButtons" :key="index" class="relative">
				<button type="button" @click="onClick(button)"
					:disabled="button.disabled"
					:class="['relative flex items-center justify-center w-full text-left px-4 py-2 text-sm border-b border-gray-200 last:border-b-0',
					button.textColor, button.bgColor,
					button.disabled ? 'opacity-50 cursor-not-allowed' : button.hoverColor]">
					{{ $t(button.label) }}
				</button>
				<div v-show="button.loading"
					:class="['absolute inset-0 bg-opacity-90 flex items-center justify-center', button.bgColor]">
					<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
				</div>
			</div>
		</div>
	</div>
</template>

<script lang="ts">
import { useRouter } from "vue-router";
import type { PropType } from "vue";
import type { TopButton, TopButtonType, TopButtonVisibleWhen } from "@/types/button";

interface TopButtonPreset {
	label: string;
	bgColor: string;
	hoverColor: string;
	textColor: string;
	visibleWhen: TopButtonVisibleWhen;
	event?: "buttonCreate" | "buttonUpdate" | "buttonDelete" | "buttonRestore" | "buttonHardDelete";
}

// look and behavior of the preset buttons, any of these can be overridden by the properties of the button
const PRESETS: Record<TopButtonType, TopButtonPreset> = {
	create: { label: "components.VModalTopButtonAdd", bgColor: "bg-blue-500", hoverColor: "hover:bg-blue-600", textColor: "text-white", visibleWhen: "active", event: "buttonCreate" },
	update: { label: "components.VModalTopButtonUpdate", bgColor: "bg-blue-500", hoverColor: "hover:bg-blue-600", textColor: "text-white", visibleWhen: "active", event: "buttonUpdate" },
	delete: { label: "components.VModalTopButtonDelete", bgColor: "bg-red-500", hoverColor: "hover:bg-red-600", textColor: "text-white", visibleWhen: "active", event: "buttonDelete" },
	restore: { label: "components.VModalTopButtonRestore", bgColor: "bg-green-500", hoverColor: "hover:bg-green-600", textColor: "text-white", visibleWhen: "deleted", event: "buttonRestore" },
	hardDelete: { label: "components.VModalTopButtonHardDelete", bgColor: "bg-red-700", hoverColor: "hover:bg-red-800", textColor: "text-white", visibleWhen: "deleted", event: "buttonHardDelete" },
	back: { label: "components.VModalTopButtonBack", bgColor: "bg-gray-300", hoverColor: "hover:bg-gray-400", textColor: "text-gray-800", visibleWhen: "always" },
};

const CUSTOM_PRESET: TopButtonPreset = {
	label: "",
	bgColor: "bg-gray-500",
	hoverColor: "hover:bg-gray-600",
	textColor: "text-white",
	visibleWhen: "always",
};

export default {
	name: "TopButtonEditElement",
	props: {
		// path to go back to when there is no history
		path: {
			type: String,
			default: "/",
		},
		// buttons displayed in the given order, the position of each button is chosen by the view
		// Example: [
		//   { type: "create", showCondition: isNew && canEdit, loading: edition.loading },
		//   { type: "update", showCondition: !isNew && canEdit, loading: edition.loading },
		//   { type: "restore", showCondition: canEdit },
		//   { type: "delete", showCondition: canEdit },
		//   { type: "hardDelete", showCondition: canEdit },
		//   { label: "command.TrackingStop", action: trackingStop, bgColor: "bg-orange-500", hoverColor: "hover:bg-orange-600", loading: trackingLoading },
		//   { type: "back" },
		// ]
		// - type: preset (create, update, delete, restore, hardDelete, back), without type the button is a custom one and needs a label and an action
		// - showCondition: false hides the button, this is where the permissions of the user are checked
		// - enableCondition: false disables the button and grays it out
		// - loading: shows a spinner on the button, must be linked to a boolean that indicates if the function is currently processing
		// - action: called on click, without it a preset emits its event (buttonCreate, buttonUpdate, buttonDelete, buttonRestore, buttonHardDelete)
		// - visibleWhen: active | deleted | always, the default depends on the preset (restore and hardDelete only on a soft deleted ressource, create, update and delete only on an active one)
		buttons: {
			type: Array as PropType<TopButton[]>,
			required: true,
		},
		// value of the soft delete field of the ressource (e.g. deleted_at), if it is not null the ressource is soft deleted
		// leave it undefined for a ressource without soft delete
		deletedAt: {
			type: [String, Date] as PropType<string | Date | null>,
			default: null,
		},
	},
	emits: [
		"buttonCreate",
		"buttonUpdate",
		"buttonDelete",
		"buttonRestore",
		"buttonHardDelete",
	],
	setup() {
		// router of the tab displaying the element
		const router = useRouter();
		return { router };
	},
	data() {
		return {
			showMobileMenu: false,
		};
	},
	computed: {
		isSoftDeleted(): boolean {
			return this.deletedAt !== null && this.deletedAt !== undefined;
		},
		visibleButtons() {
			return this.buttons
				.map((button) => {
					const preset = button.type ? PRESETS[button.type] : CUSTOM_PRESET;
					return {
						source: button,
						label: button.label ?? preset.label,
						bgColor: button.bgColor ?? preset.bgColor,
						hoverColor: button.hoverColor ?? preset.hoverColor,
						textColor: button.textColor ?? preset.textColor,
						visibleWhen: button.visibleWhen ?? preset.visibleWhen,
						loading: !!button.loading,
						disabled: button.enableCondition === false,
					};
				})
				.filter((button) => {
					if (button.source.showCondition === false) {
						return false;
					}
					return button.visibleWhen === "always" || (button.visibleWhen === "deleted") === this.isSoftDeleted;
				});
		},
	},
	methods: {
		onClick(button: { source: TopButton }) {
			const { source } = button;
			if (source.action) {
				source.action();
			} else if (source.type === "back") {
				this.goBack();
			} else if (source.type) {
				const event = PRESETS[source.type].event;
				if (event) {
					this.$emit(event);
				}
			}
		},
		hasHistory() {
			return !!this.router.options.history.state?.back;
		},
		previousPageIsNew() {
			const previousPage = this.router.options.history.state?.back;
			return typeof previousPage === "string" && previousPage.endsWith("/new");
		},
		goBack() {
			if (this.hasHistory() && !this.previousPageIsNew()) {
				this.router.go(-1);
			} else {
				this.router.push({ path: this.path });
			}
		},
	},
};
</script>

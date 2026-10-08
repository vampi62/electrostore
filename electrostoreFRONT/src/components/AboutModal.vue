<template>
	<div v-if="showModal" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center z-50"
		@click="$emit('closeModal')">
		<div class="bg-white p-6 rounded shadow-lg w-full max-w-md" @click.stop>
			<div class="flex justify-between items-center mb-4">
				<h2 class="text-xl">{{ $t('common.VAboutTitle') }}</h2>
				<button type="button" class="text-gray-500 hover:text-gray-700" :title="$t('common.VAiClose')"
					@click="$emit('closeModal')">
					<font-awesome-icon icon="fa-solid fa-xmark" size="lg" />
				</button>
			</div>
			<p class="text-gray-700 mb-4">{{ $t('common.VAboutDescription') }}</p>
			<dl class="text-sm mb-4">
				<div class="flex justify-between py-1 border-b border-gray-100">
					<dt class="text-gray-500">{{ $t('common.VAboutVersion') }}</dt>
					<dd class="font-mono">{{ version || $t('common.VAboutVersionUnknown') }}</dd>
				</div>
			</dl>
			<a href="https://github.com/vampi62/electrostore" target="_blank" rel="noopener noreferrer"
				class="inline-flex items-center space-x-2 text-blue-500 hover:underline">
				<font-awesome-icon icon="fa-brands fa-github" size="lg" />
				<span>{{ $t('common.VAboutGithub') }}</span>
			</a>
		</div>
	</div>
</template>

<script lang="ts">
export default {
	name: "AboutModal",
	props: {
		showModal: {
			type: Boolean,
			required: true,
		},
	},
	emits: ["closeModal"],
	data() {
		return {
			version: "",
		};
	},
	watch: {
		showModal(value: boolean) {
			if (value && !this.version) {
				this.loadVersion();
			}
		},
	},
	methods: {
		// same source as the update notification: /version.json is generated at build time
		async loadVersion() {
			if (import.meta.env.DEV) {
				this.version = "dev";
				return;
			}
			try {
				const res = await fetch(`/version.json?t=${Date.now()}`, { cache: "no-store" });
				if (res.ok) {
					this.version = (await res.json()).version ?? "";
				}
			} catch {
				this.version = "";
			}
		},
	},
};
</script>

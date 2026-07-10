<template>
	<Transition name="slide-up">
		<div v-if="showBanner" class="fixed bottom-5 left-1/2 -translate-x-1/2 z-[9999] bg-regal-noir text-white rounded-xl px-6 py-4 shadow-[0_8px_32px_rgba(0,0,0,0.3)] min-w-[380px] border-l-4 border-l-regal-marine">
			<div class="flex items-center gap-3">
				<div class="flex-1 flex flex-col gap-0.5">
					<strong>{{ $t('components.VNotificationAppUpdateNewVersionAvailable') }}</strong>
					<small class="opacity-70 text-xs">{{ $t('components.VNotificationAppUpdateReloadForImprovements') }}</small>
				</div>
				<div class="flex flex-col gap-2 lg:flex-row">
					<button class="bg-regal-marine text-white border-0 px-4 py-2 rounded-md cursor-pointer font-bold transition-colors hover:bg-regal-marine/70" @click="handleUpdate">
						{{ $t('components.VNotificationAppUpdateUpdate') }}
					</button>
					<button class="bg-regal-noir/50 text-white/60 border border-white/20 px-3 py-2 rounded-md cursor-pointer transition-all hover:text-white hover:border-white/50" @click="dismiss">
						{{ $t('components.VNotificationAppUpdateDismiss') }}
					</button>
				</div>
			</div>
		</div>
	</Transition>
</template>

<script>
import { ref, onMounted, onUnmounted } from "vue";
export default {
	name: "NotificationAppUpdate",
	data() {
		return {
			showBanner: false,
			UPDATE_CHECK_INTERVAL: 30 * 60 * 1000, // 30 minutes
			currentVersion: null,
			availableVersion: null,
			intervalId: null,
		};
	},
	async mounted() {
		this.startPolling((newVersion) => {
			this.showBanner = true;
		});
	},
	beforeUnmount() {
		this.stopPolling();
	},
	methods: {
		_sessionStateKey() {
			return "versionManager";
		},
		_saveState(updates) {
			const current = JSON.parse(sessionStorage.getItem(this._sessionStateKey()) || "{}");
			sessionStorage.setItem(this._sessionStateKey(), JSON.stringify({ ...current, ...updates }));
		},
		dismiss() {
			this.showBanner = false;
		},
		handleUpdate() {
			this.showBanner = false;
			this.forceReload();
		},
		async fetchVersion() {
			// block fetch if where in development mode to avoid caching issues
			if (import.meta.env.DEV) {
				//return { version: `dev-${Date.now()}` };
				return { version: "dev" };
			}
			try {
				const res = await fetch(`/version.json?t=${Date.now()}`, {
					cache: "no-store",
				});
				if (!res.ok) {
					return null;
				}
				return await res.json();
			} catch (e) {
				console.warn("Failed to fetch version info:", e);
				return null;
			}
		},
		async initVersion() {
			const data = await this.fetchVersion();
			const storedVersion = JSON.parse(sessionStorage.getItem(this._sessionStateKey()) || "{}");
			if (data) {
				if (storedVersion && storedVersion.currentVersion) {
					this.currentVersion = storedVersion.currentVersion;
					this.availableVersion = data.version;
					if (this.availableVersion && this.currentVersion && this.availableVersion !== this.currentVersion) {
						this._saveState({ availableVersion: data.version });
						this.showBanner = true;
					}
				} else {
					this.currentVersion = data.version;
					this.availableVersion = data.version;
					this._saveState({ currentVersion: data.version, availableVersion: data.version });
				}
			}
		},
		async checkForUpdate(onUpdateAvailable) {
			const data = await this.fetchVersion();
			if (data && this.currentVersion && data.version !== this.currentVersion) {
				this._saveState({ availableVersion: data.version });
				onUpdateAvailable(data);
			}
		},
		startPolling(onUpdateAvailable) {
			this.initVersion();
			this.intervalId = setInterval(() => {
				this.checkForUpdate(onUpdateAvailable);
			}, this.UPDATE_CHECK_INTERVAL);
		},
		stopPolling() {
			if (this.intervalId) {
				clearInterval(this.intervalId);
				this.intervalId = null;
			}
		},
		async forceReload() {
			if ("caches" in window) {
				const names = await caches.keys();
				await Promise.all(names.map((name) => caches.delete(name)));
			}
			sessionStorage.removeItem(this._sessionStateKey());
			window.location.reload();
		},
	},
};
</script>

<style scoped>
/* Animation */
.slide-up-enter-active,
.slide-up-leave-active {
	transition: all 0.3s ease;
}

.slide-up-enter-from,
.slide-up-leave-to {
	transform: translate(-50%, 100px);
	opacity: 0;
}
</style>
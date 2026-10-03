<template>
	<nav class="flex items-center justify-between gap-4 px-5 bg-gray-800 border-b-2 border-blue-400 fixed w-full top-0 h-16 z-10">
		<div class="flex items-center space-x-4 flex-shrink-0">
			<a href="/" class="text-white hover:text-blue-400" @click.prevent="openView($event, '/')"
				@auxclick.middle.prevent="openView($event, '/')">{{ $t('common.VAppHome') }}</a>
		</div>
		<NavTabs />
		<div v-if="configsStore.getConfigByKey('demo_mode') === true" class="hidden sm:block text-red-500 text-center flex-shrink-0">
			{{ $t('common.VAppDemoMode') }}
		</div>
		<button @click="showTopBar = !showTopBar"
			class="block sm:hidden flex-shrink-0 text-white hover:text-blue-400"><!-- for mobile -->
			<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"
				xmlns="http://www.w3.org/2000/svg">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
					d="M4 6h16M4 12h16m-7 6h7">
				</path>
			</svg>
		</button>
		<div class="hidden sm:flex flex-shrink-0"><!-- for desktop -->
			<div class="flex space-x-4 justify-end">
				<div class="flex items-center space-x-4">
					<button
						v-if="showPwaPrompt"
						@click="installPwa"
						class="bg-white text-[#3f51b5] hover:bg-gray-100 rounded-lg px-3 py-1.5 text-sm font-medium transition-colors flex items-center gap-2"
					>
						<font-awesome-icon icon="fa-solid fa-download" />
						<span>{{ $t('common.VAppInstall') }}</span>
					</button>
				</div>
				<a :href="'/users/' + authStore.user?.id_user" class="text-white hover:text-blue-400"
					@click.prevent="openView($event, '/users/' + authStore.user?.id_user)"
					@auxclick.middle.prevent="openView($event, '/users/' + authStore.user?.id_user)">
					{{ $t('common.VAppProfile') }}
				</a>
				<a v-if="authStore.hasPermission([1, 2])" href="/users"
					class="text-white hover:text-blue-400" @click.prevent="openView($event, '/users')"
					@auxclick.middle.prevent="openView($event, '/users')">
					{{ $t('common.VAppAdmin') }}
				</a>
				<button v-if="authStore.user" @click="authStore.logout()"
					class="cursor-pointer text-white hover:text-blue-400">
					{{ $t('common.VAppLogout') }}
				</button>
				<button @click="$emit('showAboutModal', true)"
					class="cursor-pointer text-white hover:text-blue-400 text-left">
					{{ $t('common.VAppAbout') }}
				</button>
			</div>
		</div>
	</nav>
	<div class="fixed sm:hidden top-12 w-full z-10"><!-- for mobile -->
		<div v-show="showTopBar" class="flex flex-col space-y-4 bg-gray-800 p-4">
			<div class="flex items-center space-x-4">
				<button
					v-if="showPwaPrompt"
					@click="installPwa"
					class="bg-white text-[#3f51b5] hover:bg-gray-100 rounded-lg px-3 py-1.5 text-sm font-medium transition-colors flex items-center gap-2"
				>
					<font-awesome-icon icon="fa-solid fa-download" />
					<span>{{ $t('common.VAppInstall') }}</span>
				</button>
			</div>
			<a :href="'/users/' + authStore.user?.id_user" class="text-white hover:text-blue-400"
				@click.prevent="openView($event, '/users/' + authStore.user?.id_user)">
				{{ $t('common.VAppProfile') }}
			</a>
			<a v-if="authStore.hasPermission([1, 2])" href="/users"
				class="text-white hover:text-blue-400" @click.prevent="openView($event, '/users')">
				{{ $t('common.VAppAdmin') }}
			</a>
			<button v-if="authStore.user" @click="authStore.logout()"
				class="cursor-pointer text-white hover:text-blue-400 text-left">
				{{ $t('common.VAppLogout') }}
			</button>
			<button @click="$emit('showAboutModal', true)"
				class="cursor-pointer text-white hover:text-blue-400 text-left">
				{{ $t('common.VAppAbout') }}
			</button>
			<div class="border-t-2 border-blue-400"></div>
			<ul class="mt-6 space-y-4">
				<li v-for="nav in listNavShown" :key="nav.name">
					<template v-if="!nav.enableCondition || evalCondition(nav.enableCondition)">
						<a :href="nav.path" :class="['flex items-center space-x-4 hover:text-blue-400',
							isActive(nav.path) ? 'text-blue-400' : 'text-white']" @click.prevent="openView($event, nav.path)">
							<font-awesome-icon :icon="nav.faIcon" />
							<span>{{ $t(nav.name) }}</span>
						</a>
					</template>
					<template v-else>
						<div class="flex items-center space-x-4 text-gray-500 cursor-not-allowed">
							<font-awesome-icon :icon="nav.faIcon" />
							<span>{{ $t(nav.name) }}</span>
						</div>
					</template>
				</li>
			</ul>
			<div v-if="configsStore.getConfigByKey('demo_mode') === true" class="text-red-500 text-center">
				{{ $t('common.VAppDemoMode') }}
			</div>
			<a href="https://github.com/vampi62/electrostore" class="text-white hover:text-blue-400"
				target="_blank" rel="noopener noreferrer">
				<p class="space-x-4">
					<font-awesome-icon icon="fa-brands fa-github" size="lg" />
					<span>ElectroStore</span>
				</p>
			</a>
		</div>
	</div>
	<div :class="['hidden sm:flex flex-col justify-between p-4 bg-gray-800 fixed left-0 top-16 bottom-12',
		reduceLeftSideBar ? 'w-16' : 'w-64']"><!-- for desktop -->
		<div class="flex flex-col space-y-4 overflow-x-auto no-scrollbar">
			<ul class="mt-2 space-y-4">
				<li v-for="nav in listNavShown" :key="nav.name" class="min-h-6">
					<template v-if="!nav.enableCondition || evalCondition(nav.enableCondition)">
						<a :href="nav.path" :class="['flex items-center space-x-4 hover:text-blue-400',
							isActive(nav.path) ? 'text-blue-400' : 'text-white']" @click.prevent="openView($event, nav.path)"
							@auxclick.middle.prevent="openView($event, nav.path)">
							<div class="flex items-center justify-center w-8 h-8">
								<font-awesome-icon :icon="nav.faIcon" size="lg" />
							</div>
							<span v-if="!reduceLeftSideBar" class="whitespace-nowrap">{{ $t(nav.name) }}</span>
						</a>
					</template>
					<template v-else>
						<div class="flex items-center space-x-4 text-gray-500 cursor-not-allowed">
							<div class="flex items-center justify-center w-8 h-8">
								<font-awesome-icon :icon="nav.faIcon" size="lg" />
							</div>
							<span v-if="!reduceLeftSideBar" class="whitespace-nowrap">{{ $t(nav.name) }}</span>
						</div>
					</template>
				</li>
			</ul>
		</div>
		<div v-if="configsStore.getConfigByKey('demo_mode') === true" class="text-red-500 text-center mt-4">
			{{ $t('common.VAppDemoMode') }}
		</div>
		<a href="https://github.com/vampi62/electrostore" class="block text-white hover:text-blue-400"
			target="_blank" rel="noopener noreferrer">
			<div class="text-center mt-4">
				<p class="space-x-4">
					<font-awesome-icon icon="fa-brands fa-github" size="lg" />
					<span v-if="!reduceLeftSideBar">ElectroStore</span>
				</p>
			</div>
		</a>
	</div>
	<button :class="['hidden sm:flex justify-center p-4 bg-gray-700 text-white hover:text-blue-400 fixed left-0 bottom-0 h-12',
		reduceLeftSideBar ? 'w-16' : 'w-64']" @click="reduceLeftSideBar = !reduceLeftSideBar; $emit('update:reduceLeftSideBar', reduceLeftSideBar)">
		<font-awesome-icon v-if="reduceLeftSideBar" icon="fa-solid fa-arrow-right" size="lg" />
		<font-awesome-icon v-else icon="fa-solid fa-arrow-left" size="lg" />
	</button>
</template>

<script lang="ts">
import { useAuthStore, useConfigsStore, useTabsStore } from "@/stores";
import type { PropType } from "vue";
export default {
	name: "NavBar",
	props: {
		listNav: {
			type: Array as PropType<any[]>,
			required: true,
		},
	},
	computed: {
		listNavShown() {
			return this.listNav.filter((nav) => {
				if (nav.showCondition === undefined) {
					return true;
				}
				return eval(nav.showCondition);
			});
		},
	},
	data() {
		return {
			showTopBar: false,
			reduceLeftSideBar: false,
			installEvent: null as any,
			showPwaPrompt: false,
		};
	},
	setup() {
		const authStore = useAuthStore();
		const configsStore = useConfigsStore();
		const tabsStore = useTabsStore();
		return { authStore, configsStore, tabsStore };
	},
	emits: ["update:reduceLeftSideBar", "showAboutModal"],
	mounted() {
		window.addEventListener("beforeinstallprompt", this.onBeforeInstallPrompt);
		window.addEventListener("appinstalled", this.onAppInstalled);
	},
	beforeUnmount() {
		window.removeEventListener("beforeinstallprompt", this.onBeforeInstallPrompt);
		window.removeEventListener("appinstalled", this.onAppInstalled);
	},
	methods: {
		// bare eval() in a template resolves to the (nonexistent) this.eval, not the global eval — must go through a method
		evalCondition(condition: string) {
			return eval(condition);
		},
		// the view of a menu entry is displayed in the selected tab, or in a new tab with ctrl/middle click
		openView(event: MouseEvent, path: string) {
			if (event.ctrlKey || event.metaKey || event.button === 1) {
				this.tabsStore.open(path);
			} else {
				this.tabsStore.navigate(path);
			}
			this.showTopBar = false;
		},
		isActive(path: string) {
			const activePath = (this.tabsStore.activeTab?.fullPath || "").split("?")[0];
			return activePath === path || activePath.startsWith(path + "/");
		},
		onBeforeInstallPrompt(e: Event) {
			e.preventDefault();
			this.installEvent = e;
			this.showPwaPrompt = true;
		},
		onAppInstalled() {
			this.showPwaPrompt = false;
			this.installEvent = null;
		},
		async installPwa() {
			if (!this.installEvent) {
				return;
			}
			this.installEvent.prompt();
			const { outcome } = await this.installEvent.userChoice;
			if (outcome === "accepted") {
				this.showPwaPrompt = false;
				this.installEvent = null;
			}
		},
	},
};
</script>
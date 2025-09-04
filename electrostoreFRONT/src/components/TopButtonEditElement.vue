<template>
	<div class="hidden md:flex flex-wrap gap-2 justify-end">
		<!-- Desktop -->
		<template v-for="(button, index) in optionalConfig" :key="index">
			<button type="button" @click="button.action"
				v-if="id != 'new' && storeUser?.role_user >= button.roleRequired"
				:class="['text-white px-4 py-2 rounded flex items-center',
				button.bgColor ? button.bgColor : 'bg-gray-500',
				button.hoverColor ? button.hoverColor : 'hover:bg-gray-600']">
				<span v-show="button.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t(button.label) }}
			</button>
		</template>
		<button type="button" @click="$emit('buttonSave')" v-if="id == 'new' && ((storeUser?.role_user >= mainConfig.save.roleRequired) || (mainConfig.save?.sameUserId && storeUser?.id_user == id))"
			class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
			<span v-show="mainConfig.save.loading"
				class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
			</span>
			{{ $t('components.VModalTopButtonAdd') }}
		</button>
		<button type="button" @click="$emit('buttonSave')" v-if="id != 'new' && ((storeUser?.role_user >= mainConfig.save.roleRequired) || (mainConfig.save?.sameUserId && storeUser?.id_user == id))"
			class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
			<span v-show="mainConfig.save.loading"
				class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
			</span>
			{{ $t('components.VModalTopButtonUpdate') }}
		</button>
		<button type="button" @click="$emit('buttonDelete')" v-if="id != 'new' && ((storeUser?.role_user >= mainConfig.delete.roleRequired) || (mainConfig.delete?.sameUserId && storeUser?.id_user == id))"
			class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">
			{{ $t('components.VModalTopButtonDelete') }}
		</button>
		<button @click="hasHistory() ? (previousPageIsNew() ? $router.push({ path: mainConfig.path }) : $router.go(-1)) : $router.push({ path: mainConfig.path })"
			class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">
			{{ $t('components.VModalTopButtonBack') }}
		</button>
	</div>
	<!-- Mobile -->
	<div class="md:hidden relative">
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
			class="absolute right-0 mt-2 w-48 rounded-md shadow-lg z-50 border border-gray-200">
			<template v-for="(button, index) in optionalConfig" :key="index">
				<button v-if="id != 'new' && storeUser?.role_user >= button.roleRequired" @click="button.action"
					:class="['relative flex items-center justify-center w-full text-left px-4 py-2 text-sm text-white text-gray-700 hover:bg-gray-100 border-b border-gray-200',
					button.bgColor ? button.bgColor : 'bg-gray-500',
					button.hoverColor ? button.hoverColor : 'hover:bg-gray-600']">
					<span v-show="button.loading"
						class="absolute left-4 w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
					</span>
					{{ $t(button.label) }}
				</button>
			</template>
			<button v-if="id == 'new' && ((storeUser?.role_user >= mainConfig.save.roleRequired) || (mainConfig.save?.sameUserId && storeUser?.id_user == id))"
				@click="$emit('buttonSave')"
				class="relative flex items-center justify-center w-full text-left px-4 py-2 text-sm text-white bg-blue-500 hover:bg-blue-600 border-b border-gray-200">
				<span v-show="mainConfig.save.loading"
					class="absolute left-4 w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('components.VModalTopButtonAdd') }}
			</button>
			<button v-if="id != 'new' && ((storeUser?.role_user >= mainConfig.save.roleRequired) || (mainConfig.save?.sameUserId && storeUser?.id_user == id))"
				@click="$emit('buttonSave')"
				class="relative flex items-center justify-center w-full text-left px-4 py-2 text-sm text-white bg-blue-500 hover:bg-blue-600 border-b border-gray-200">
				<span
					v-show="mainConfig.save.loading"
					class="absolute left-4 w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"
				></span>
				{{ $t('components.VModalTopButtonUpdate') }}
			</button>
			<button v-if="id != 'new' && ((storeUser?.role_user >= mainConfig.delete.roleRequired) || (mainConfig.delete?.sameUserId && storeUser?.id_user == id))"
				@click="$emit('buttonDelete')"
				class="relative flex items-center justify-center w-full text-left px-4 py-2 text-sm text-white bg-red-500 hover:bg-red-600 border-b border-gray-200">
				{{ $t('components.VModalTopButtonDelete') }}
			</button>
			<button @click="hasHistory() ? (previousPageIsNew() ? $router.push({ path: mainConfig.path }) : $router.go(-1)) : $router.push({ path: mainConfig.path })"
				class="relative flex items-center justify-center w-full text-left px-4 py-2 bg-white text-sm text-gray-700 hover:bg-gray-100">
				{{ $t('components.VModalTopButtonBack') }}
			</button>
		</div>
	</div>
</template>

<script>
export default {
	name: "TopButtonEditElement",
	props: {
		mainConfig: {
			type: Object,
			required: true,
			// main configuration object with properties:
			// path: string (default "/"),
			// save: { roleRequired: number, loading: boolean, sameUserId: boolean } // configuration for save button
			// delete: { roleRequired: number, sameUserId: boolean } // configuration for delete button
			// sameUserId: boolean (if true, the button is only visible if the user ID matches the current user ID)
			// roleRequired: number (minimum user role required to see the button)
			// loading: boolean (if true, the button shows a loading spinner) must be linked to a boolean that indicates if the function is currently processing
			default: () => ({
				path: "/",
				save: { roleRequired: 0, loading: false, sameUserId: false },
				delete: { roleRequired: 0, sameUserId: false },
			}),
		},
		id: {
			type: String,
			required: true,
			// 'new' for new elements, otherwise the ID of the element
			default: "new",
		},
		optionalConfig: {
			type: Array,
			required: false,
			// array of optional buttons with properties: label, action, roleRequired, bgColor, hoverColor, loading
			// Example: [{ label: 'components.VModalTopButtonCustom', action: customFucntionCall, roleRequired: 1, bgColor: 'bg-green-500', hoverColor: 'hover:bg-green-600', loading: false }]
			// loading is a link to a boolean that indicates if the function is currently processing
			default: () => [],
		},
		storeUser: {
			type: Object,
			required: true,
			// current store user session data
			default: null,
		},
	},
	methods: {
		hasHistory() {
			return window.history.length > 2;
		},
		previousPageIsNew() {
			const previousPage = window.history.state?.back;
			return previousPage && previousPage.endsWith("/new");
		},
	},
	emits: [
		"buttonSave",
		"buttonDelete",
	],
	data() {
		return {
			showMobileMenu: false,
		};
	},
};
</script>
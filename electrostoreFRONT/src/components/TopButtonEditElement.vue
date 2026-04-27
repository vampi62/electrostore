<template>
	<div class="hidden md:flex flex-wrap gap-2 justify-end">
		<!-- Desktop -->
		<template v-for="(button, index) in optionalConfig" :key="index">
			<div v-if="button.showCondition" class="relative">
				<button type="button" @click="button.action"
					:disabled="button.enableCondition === false"
					:class="['text-white px-4 py-2 rounded flex items-center',
					button.bgColor ? button.bgColor : 'bg-gray-500',
					button.enableCondition === false ? 'opacity-50 cursor-not-allowed' : (button.hoverColor ? button.hoverColor : 'hover:bg-gray-600')]">
					{{ $t(button.label) }}
				</button>
				<div v-show="button.loading" 
					:class="['absolute inset-0 bg-opacity-90 rounded flex items-center justify-center',
					button.bgColor ? button.bgColor : 'bg-gray-500']">
					<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
				</div>
			</div>
		</template>
		<div v-if="mainConfig.create.showCondition" class="relative">
			<button type="button" @click="$emit('buttonCreate')"
				:disabled="mainConfig.create.enableCondition === false"
				:class="['bg-blue-500 text-white px-4 py-2 rounded flex items-center',
				mainConfig.create.enableCondition === false ? 'opacity-50 cursor-not-allowed' : 'hover:bg-blue-600']">
				{{ $t('components.VModalTopButtonAdd') }}
			</button>
			<div v-show="mainConfig.create.loading" 
				class="absolute inset-0 bg-blue-500 bg-opacity-90 rounded flex items-center justify-center">
				<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
			</div>
		</div>
		<div v-if="mainConfig.update.showCondition" class="relative">
			<button type="button" @click="$emit('buttonUpdate')"
				:disabled="mainConfig.update.enableCondition === false"
				:class="['bg-blue-500 text-white px-4 py-2 rounded flex items-center',
				mainConfig.update.enableCondition === false ? 'opacity-50 cursor-not-allowed' : 'hover:bg-blue-600']">
				{{ $t('components.VModalTopButtonUpdate') }}
			</button>
			<div v-show="mainConfig.update.loading" 
				class="absolute inset-0 bg-blue-500 bg-opacity-90 rounded flex items-center justify-center">
				<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
			</div>
		</div>
		<div v-if="mainConfig.delete.showCondition" class="relative">
			<button type="button" @click="$emit('buttonDelete')"
				:disabled="mainConfig.delete.enableCondition === false"
				:class="['bg-red-500 text-white px-4 py-2 rounded',
				mainConfig.delete.enableCondition === false ? 'opacity-50 cursor-not-allowed' : 'hover:bg-red-600']">
				{{ $t('components.VModalTopButtonDelete') }}
			</button>
			<div v-show="mainConfig.delete.loading" 
				class="absolute inset-0 bg-red-500 bg-opacity-90 rounded flex items-center justify-center">
				<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
			</div>
		</div>
		<button @click="hasHistory() ? (previousPageIsNew() ? $router.push({ path: mainConfig.path }) : $router.go(-1)) : $router.push({ path: mainConfig.path })"
			class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">
			{{ $t('components.VModalTopButtonBack') }}
		</button>
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
			<template v-for="(button, index) in optionalConfig" :key="index">
				<div v-if="button.showCondition" class="relative">
					<button @click="button.action"
						:disabled="button.enableCondition === false"
						:class="['relative flex items-center justify-center w-full text-left px-4 py-2 text-sm text-white text-gray-700 border-b border-gray-200',
						button.bgColor ? button.bgColor : 'bg-gray-500',
						button.enableCondition === false ? 'opacity-50 cursor-not-allowed' : (button.hoverColor ? button.hoverColor : 'hover:bg-gray-600')]">
						{{ $t(button.label) }}
					</button>
					<div v-show="button.loading" 
						:class="['absolute inset-0 bg-opacity-90 flex items-center justify-center',
						button.bgColor ? button.bgColor : 'bg-gray-500']">
						<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
					</div>
				</div>
			</template>
			<div v-if="mainConfig.create.showCondition" class="relative">
				<button @click="$emit('buttonCreate')"
					:disabled="mainConfig.create.enableCondition === false"
					:class="['relative flex items-center justify-center w-full text-left px-4 py-2 text-sm text-white bg-blue-500 border-b border-gray-200',
					mainConfig.create.enableCondition === false ? 'opacity-50 cursor-not-allowed' : 'hover:bg-blue-600']">
					{{ $t('components.VModalTopButtonAdd') }}
				</button>
				<div v-show="mainConfig.create.loading" 
					class="absolute inset-0 bg-blue-500 bg-opacity-90 flex items-center justify-center">
					<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
				</div>
			</div>
			<div v-if="mainConfig.update.showCondition" class="relative">
				<button @click="$emit('buttonUpdate')"
					:disabled="mainConfig.update.enableCondition === false"
					:class="['relative flex items-center justify-center w-full text-left px-4 py-2 text-sm text-white bg-blue-500 border-b border-gray-200',
					mainConfig.update.enableCondition === false ? 'opacity-50 cursor-not-allowed' : 'hover:bg-blue-600']">
					{{ $t('components.VModalTopButtonUpdate') }}
				</button>
				<div v-show="mainConfig.update.loading" 
					class="absolute inset-0 bg-blue-500 bg-opacity-90 flex items-center justify-center">
					<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
				</div>
			</div>
			<button v-if="mainConfig.delete.showCondition"
				@click="$emit('buttonDelete')"
				:disabled="mainConfig.delete.enableCondition === false"
				:class="['relative flex items-center justify-center w-full text-left px-4 py-2 text-sm text-white bg-red-500 border-b border-gray-200',
				mainConfig.delete.enableCondition === false ? 'opacity-50 cursor-not-allowed' : 'hover:bg-red-600']">
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
			// create: { showCondition: boolean, enableCondition: boolean, loading: boolean } // configuration for create button
			// update: { showCondition: boolean, enableCondition: boolean, loading: boolean } // configuration for update button
			// delete: { showCondition: boolean, enableCondition: boolean, loading: boolean } // configuration for delete button
			// showCondition: boolean (if true, the button is only visible if the user has the required role, this should be handled in the parent component by checking the user's roles and passing the appropriate value)
			// loading: boolean (if true, the button shows a loading spinner) must be linked to a boolean that indicates if the function is currently processing
			default: () => ({
				path: "/",
				create: { showCondition: false, loading: false },
				update: { showCondition: false, loading: false },
				delete: { showCondition: false, loading: false },
			}),
		},
		optionalConfig: {
			type: Array,
			required: false,
			// array of optional buttons with properties: label, action, showCondition, enableCondition, bgColor, hoverColor, loading
			// Example: [{ label: 'components.VModalTopButtonCustom', action: customFucntionCall, showCondition: false, enableCondition: true, bgColor: 'bg-green-500', hoverColor: 'hover:bg-green-600', loading: false }]
			// enableCondition: boolean (if false, the button is disabled and grayed out)
			// loading is a link to a boolean that indicates if the function is currently processing
			default: () => [],
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
		"buttonCreate",
		"buttonUpdate",
		"buttonDelete",
	],
	data() {
		return {
			showMobileMenu: false,
		};
	},
};
</script>
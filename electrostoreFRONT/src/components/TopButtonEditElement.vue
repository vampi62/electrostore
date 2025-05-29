
<template>
	<div class="flex space-x-4">
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
		<button type="button" @click="$emit('button-save')" v-if="id == 'new' && ((storeUser?.role_user >= mainConfig.save.roleRequired) || (mainConfig.save?.sameUserId && storeUser?.id_user == id))"
			class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
			<span v-show="mainConfig.save.loading"
				class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
			</span>
			{{ $t('components.VModalTopButtonAdd') }}
		</button>
		<button type="button" @click="$emit('button-save')" v-if="id != 'new' && ((storeUser?.role_user >= mainConfig.save.roleRequired) || (mainConfig.save?.sameUserId && storeUser?.id_user == id))"
			class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
			<span v-show="mainConfig.save.loading"
				class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
			</span>
			{{ $t('components.VModalTopButtonUpdate') }}
		</button>
		<button type="button" @click="$emit('button-delete')" v-if="id != 'new' && ((storeUser?.role_user >= mainConfig.delete.roleRequired) || (mainConfig.delete?.sameUserId && storeUser?.id_user == id))"
			class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">
			{{ $t('components.VModalTopButtonDelete') }}
		</button>
		<RouterLink :to="mainConfig.path" class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">
			{{ $t('components.VModalTopButtonBack') }}
		</RouterLink>
	</div>
</template>

<script>
export default {
	name: "TopButtonEditElement",
	props: {
		mainConfig: {
			type: Object,
			required: true,
		},
		id: {
			type: String,
			default: "new",
		},
		optionalConfig: {
			type: Array,
			default: () => [],
		},
		storeUser: {
			type: Object,
			default: null,
		},
	},
	emits: [
		"button-save",
		"button-delete",
	],
};
</script>
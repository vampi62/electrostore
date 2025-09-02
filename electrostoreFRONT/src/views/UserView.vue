<script setup>
import { onMounted, onBeforeUnmount, ref, inject, watch } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
let userId = route.params.id;

import { useConfigsStore, useUsersStore, useCommandsStore, useProjetsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const usersStore = useUsersStore();
const commandsStore = useCommandsStore();
const projetsStore = useProjetsStore();
const authStore = useAuthStore();

if (authStore.user?.role_user !== 2 && authStore.user?.role_user !== 1 && authStore.user?.id_user !== Number(userId)) {
	addNotification({ message: "vous n'avez pas la permission d'acceder a cette page", type: "error", i18n: false });
	if (window.history.length > 1) {
		router.back();
	} else {
		router.push("/");
	}
}

async function fetchAllData() {
	if (userId !== "new") {
		usersStore.userEdition = {
			loading: true,
		};
		try {
			await usersStore.getUserById(userId);
		} catch {
			delete usersStore.users[userId];
			addNotification({ message: "user.VUserNotFound", type: "error", i18n: true });
			if (window.history.length > 1) {
				router.back();
			} else {
				router.push("/");
			}
			return;
		}
		usersStore.getProjetCommentaireByInterval(userId, 100, 0, ["projet"]);
		usersStore.getCommandCommentaireByInterval(userId, 100, 0, ["command"]);
		usersStore.getTokenByInterval(userId, 100, 0);
		usersStore.userEdition = {
			loading: false,
			nom_user: usersStore.users[userId].nom_user,
			prenom_user: usersStore.users[userId].prenom_user,
			email_user: usersStore.users[userId].email_user,
			role_user: usersStore.users[userId].role_user,
			current_mdp_user: "",
			mdp_user: "",
			confirm_mdp_user: "",
		};
	} else {
		usersStore.userEdition = {
			loading: false,
		};
		showParticipation.value = false;
		showProjetCommentaires.value = false;
		showCommandCommentaires.value = false;
		showTokens.value = false;
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	usersStore.userEdition = {
		loading: false,
	};
});

const showParticipation = ref(true);
const showProjetCommentaires = ref(true);
const showCommandCommentaires = ref(true);
const showTokens = ref(true);

const toggleParticipation = () => {
	if (userId === "new") {
		return;
	}
	showParticipation.value = !showParticipation.value;
};
const toggleProjetCommentaires = () => {
	if (userId === "new") {
		return;
	}
	showProjetCommentaires.value = !showProjetCommentaires.value;
};
const toggleCommandCommentaires = () => {
	if (userId === "new") {
		return;
	}
	showCommandCommentaires.value = !showCommandCommentaires.value;
};
const toggleTokens = () => {
	if (userId === "new") {
		return;
	}
	showTokens.value = !showTokens.value;
};

const userDeleteModalShow = ref(false);
const userTypeRole = ref([[0, t("user.VUserFilterRole0")], [1, t("user.VUserFilterRole1")], [2, t("user.VUserFilterRole2")]]);
const userSave = async() => {
	try {
		await schemaUser.value.validate(usersStore.userEdition, { abortEarly: false });
		if (userId !== "new") {
			await usersStore.updateUser(userId, { ...usersStore.userEdition });
			addNotification({ message: "user.VUserUpdated", type: "success", i18n: true });
		} else {
			await usersStore.createUser({ ...usersStore.userEdition });
			addNotification({ message: "user.VUserCreated", type: "success", i18n: true });
		}
		usersStore.userEdition.mdp_user = "";
		usersStore.userEdition.confirm_mdp_user = "";
		usersStore.userEdition.current_mdp_user = "";
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
	if (userId === "new") {
		userId = String(usersStore.userEdition.id_user);
		router.push("/users/" + userId);
	}
};
const userDelete = async() => {
	try {
		await usersStore.deleteUser(userId);
		addNotification({ message: "user.VUserDeleted", type: "success", i18n: true });
		router.push("/users");
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	userDeleteModalShow.value = false;
};

const revokeToken = async(tokenId) => {
	try {
		await usersStore.updateToken(userId, tokenId, { "revoked_reason": "Revoked by user" });
		usersStore.getTokenById(userId, tokenId);
		addNotification({ message: "user.VUserTokenRevoked", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};

const isChecked = ref(false);
const createSchema = (isChecked) => {
	return Yup.object().shape({
		nom_user: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("user.VUserNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
			.required(t("user.VUserNameRequired")),
		prenom_user: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("user.VUserFirstNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
			.required(t("user.VUserFirstNameRequired")),
		email_user: Yup.string()
			.max(configsStore.getConfigByKey("max_length_email"), t("user.VUserEmailMaxLength") + " " + configsStore.getConfigByKey("max_length_email") + t("common.VAllCaracters"))
			.required(t("user.VUserEmailRequired"))
			.email(t("user.VUserEmailInvalid")),
		mdp_user: isChecked // if isChecked is true, then mdp_user is required and must be different from current_mdp_user and contain at least 8 characters, special characters, numbers and upper and lower letters
			? Yup.string().required(t("user.VUserPasswordRequired")).notOneOf([Yup.ref("current_mdp_user"), null], t("user.VUserPasswordMatch")).matches(
				/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
				t("user.VUserPasswordComplexity"),
			)
			: Yup.string().nullable(),
		confirm_mdp_user: isChecked // if isChecked is true, then confirm_mdp_user is required and must match mdp_user and contain at least 8 characters, special characters, numbers and upper and lower letters
			? Yup.string().required(t("user.VUserConfirmPasswordRequired")).oneOf([Yup.ref("mdp_user"), null], t("user.VUserConfirmPasswordMatch")).matches(
				/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
				t("user.VUserPasswordComplexity"),
			)
			: Yup.string().nullable(),
		// current_mdp_user if always required and not nullable
		current_mdp_user: Yup.string()
			.required(t("user.VUserCurrentPasswordRequired")),
	});
};
const schemaUser = ref(createSchema(usersStore.userEdition.is_checked_custom));
watch(isChecked, (newValue) => {
	schemaUser.value = createSchema(newValue);
});

const labelTableauSession = ref([
	{ label: "user.VUserTokenCreatedDate", sortable: true, key: "created_at", type: "datetime" },
	{ label: "user.VUserTokenCreatedIP", sortable: true, key: "created_by_ip", type: "text" },
	{ label: "user.VUserTokenExpireDate", sortable: true, key: "expires_at", type: "datetime" },
	{ label: "user.VUserTokenIsRevoked", sortable: true, key: "is_revoked", type: "text" },
	{ label: "user.VUserTokenRevokedDate", sortable: true, key: "revoked_at", type: "datetime" },
	{ label: "user.VUserTokenRevokedIP", sortable: true, key: "revoked_by_ip", type: "text" },
	{ label: "user.VUserTokenRevokedReason", sortable: true, key: "revoked_reason", type: "text" },
	{ label: "user.VUserTokenActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "user.VUserTokenRevoke",
			icon: "fa-solid fa-ban",
			condition: "!rowData?.is_revoked",
			action: (row) => revokeToken(row.session_id),
			class: "bg-red-500 text-white px-2 py-1 rounded hover:bg-red-600",
		},
	] },
]);
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('user.VUserTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/users', save: { sameUserId: true, roleRequired: 1, loading: usersStore.userEdition.loading }, delete: { sameUserId: true, roleRequired: 1 } }"
			:id="userId" :store-user="authStore.user" @button-save="userSave" @button-delete="userDeleteModalShow = true"/>
	</div>
	<div :class="usersStore.users[userId] || userId == 'new' ? 'block' : 'hidden'">
		<div class="mb-6 flex justify-between flex-wrap">
			<Form :validation-schema="schemaUser" v-slot="{ errors }" @submit.prevent="" class="mb-6">
				<div class="flex flex-col text-gray-700 space-y-2">
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="nom_user">{{ $t('user.VUserName') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="nom_user" type="text" v-model="usersStore.userEdition.nom_user"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.nom_user }" />
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.nom_user || ' ' }}</span>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="prenom_user">{{ $t('user.VUserFirstName') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="prenom_user" type="text" v-model="usersStore.userEdition.prenom_user"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.prenom_user }" />
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.prenom_user || ' ' }}</span>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="email_user">{{ $t('user.VUserEmail') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="email_user" type="text" v-model="usersStore.userEdition.email_user"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.email_user }" />
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.email_user || ' ' }}</span>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="role_user">{{ $t('user.VUserRole') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="role_user" as="select"
								v-model="usersStore.userEdition.role_user"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.role_user }"
								:disabled="authStore.user?.role_user !== 2">
								<option value="" disabled :selected="!usersStore.userEdition.role_user"> -- {{ $t('user.VUserSelectRole') }} -- </option>
								<option v-for="role in userTypeRole" :key="role[0]" :value="role[0]">
									{{ role[1] }}
								</option>
							</Field>
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.role_user || ' ' }}</span>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="check">{{ $t('user.VUserRole') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="check" v-slot="{ is_checked_custom }">
								<input
									v-model="isChecked"
									v-bind="is_checked_custom"
									type="checkbox"
									:checked="isChecked"
									class="form-checkbox h-5 w-5 text-blue-600"
								/>
							</Field>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="mdp_user">{{ $t('user.VUserPassword') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="mdp_user" type="text" v-model="usersStore.userEdition.mdp_user"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.mdp_user }"
								:disabled="!isChecked" />
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.mdp_user || ' ' }}</span>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="confirm_mdp_user">{{ $t('user.VUserConfirmPassword') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="confirm_mdp_user" type="password" v-model="usersStore.userEdition.confirm_mdp_user"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.confirm_mdp_user }"
								:disabled="!isChecked" />
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.confirm_mdp_user || ' ' }}</span>
						</div>
					</div>
					<div class="flex flex-row items-start space-x-2">
						<label class="font-semibold min-w-[140px]" for="current_mdp_user">{{ $t('user.VUserCurrentPassword') }}:</label>
						<div class="flex flex-col flex-1">
							<Field name="current_mdp_user" type="password" v-model="usersStore.userEdition.current_mdp_user"
								class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
								:class="{ 'border-red-500': errors.current_mdp_user }" />
							<span class="text-red-500 h-5 w-80 text-sm">{{ errors.current_mdp_user || ' ' }}</span>
						</div>
					</div>
				</div>
			</Form>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleParticipation" class="text-xl font-semibold  bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': userId != 'new', 'cursor-not-allowed': userId == 'new' }">
				{{ $t('user.VUserParticipation') }}
			</h3>
			<div :class="showParticipation ? 'block' : 'hidden'" class="p-2">
				<div class="bg-gray-100 p-2 rounded">
					<h3 @click="toggleCommandCommentaires" class="text-xl font-semibold bg-gray-400 p-2 rounded"
						:class="{ 'cursor-pointer': userId != 'new', 'cursor-not-allowed': userId == 'new' }">
						{{ $t('user.VUserCommandsCommentaires') }} ({{ usersStore.commandsCommentaireTotalCount[userId] || 0 }})
					</h3>
					<div :class="showCommandCommentaires ? 'block' : 'hidden'" class="p-2">
						<Commentaire :meta="{ link: '/commands/', idRessource: 'id_command', contenu: 'contenu_command_commentaire', key: 'id_command_commentaire', CanEdit: false }"
							:store-data="[usersStore.commandsCommentaire[userId],usersStore.users,authStore.user,configsStore]"
							:loading="usersStore.commandsCommentaireLoading"
							:total-count="Number(usersStore.commandsCommentaireTotalCount[userId]) || 0"
							:loaded-count="Object.keys(usersStore.commandsCommentaire[userId] || {}).length"
							:fetch-function="(offset, limit) => usersStore.getCommandCommentaireByInterval(userId, limit, offset, ['command'])"
						/>
					</div>
				</div>
				<div class="bg-gray-100 p-2 rounded">
					<h3 @click="toggleProjetCommentaires" class="text-xl font-semibold bg-gray-400 p-2 rounded"
						:class="{ 'cursor-pointer': userId != 'new', 'cursor-not-allowed': userId == 'new' }">
						{{ $t('user.VUserProjetsCommentaires') }} ({{ usersStore.projetsCommentaireTotalCount[userId] || 0 }})
					</h3>
					<div :class="showProjetCommentaires ? 'block' : 'hidden'" class="p-2">
						<Commentaire :meta="{ link: '/projets/', idRessource: 'id_projet', contenu: 'contenu_projet_commentaire', key: 'id_projet_commentaire', CanEdit: false }"
							:store-data="[usersStore.projetsCommentaire[userId],usersStore.users,authStore.user,configsStore]"
							:loading="usersStore.projetsCommentaireLoading"
							:total-count="Number(usersStore.projetsCommentaireTotalCount[userId]) || 0"
							:loaded-count="Object.keys(usersStore.projetsCommentaire[userId] || {}).length"
							:fetch-function="(offset, limit) => usersStore.getProjetCommentaireByInterval(userId, limit, offset, ['projet'])"
						/>
					</div>
				</div>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleTokens" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': userId != 'new', 'cursor-not-allowed': userId == 'new' }">
				{{ $t('user.VUserTokens') }} ({{ usersStore.tokensTotalCount[userId] || 0 }})
			</h3>
			<div :class="showTokens ? 'block' : 'hidden'" class="p-2">
				<Tableau :labels="labelTableauSession" :meta="{ key: 'session_id' }"
					:store-data="[usersStore.tokens[userId]]"
					:loading="usersStore.tokensLoading"
					:total-count="Number(usersStore.tokensTotalCount[userId]) || 0"
					:loaded-count="Object.keys(usersStore.tokens[userId] || {}).length"
					:fetch-function="(offset, limit) => usersStore.getTokenByInterval(userId, limit, offset)"
					:tableau-css="{ component: 'min-h-64 max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</div>
		</div>
	</div>
	<div :class="usersStore.users[userId] || userId == 'new' ? 'hidden' : 'block'">
		<div>{{ $t('user.VUserLoading') }}</div>
	</div>
	<ModalDeleteConfirm :show-modal="userDeleteModalShow" @close-modal="userDeleteModalShow = false"
		@delete-confirmed="userDelete" :text-title="'user.VUserDeleteTitle'"
		:text-p="(authStore.user?.id_user !== Number(userId)) ? 'user.VUserDeleteTextAdmin' : 'user.VUserDeleteTextUser'"/>
</template>

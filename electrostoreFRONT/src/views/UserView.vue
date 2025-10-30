<script setup>
import { onMounted, onBeforeUnmount, ref, inject, watch } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const userId = ref(route.params.id);

import { useConfigsStore, useUsersStore, useCommandsStore, useProjetsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const usersStore = useUsersStore();
const commandsStore = useCommandsStore();
const projetsStore = useProjetsStore();
const authStore = useAuthStore();

if (authStore.user?.role_user !== 2 && authStore.user?.role_user !== 1 && authStore.user?.id_user !== Number(userId.value)) {
	addNotification({ message: "vous n'avez pas la permission d'acceder a cette page", type: "error", i18n: false });
	if (window.history.length > 1) {
		router.back();
	} else {
		router.push("/");
	}
}

async function fetchAllData() {
	if (userId.value !== "new") {
		usersStore.userEdition = {
			loading: true,
		};
		try {
			await usersStore.getUserById(userId.value);
		} catch {
			delete usersStore.users[userId.value];
			addNotification({ message: "user.VUserNotFound", type: "error", i18n: true });
			if (window.history.length > 1) {
				router.back();
			} else {
				router.push("/");
			}
			return;
		}
		usersStore.getProjetCommentaireByInterval(userId.value, 100, 0, ["projet"]);
		usersStore.getCommandCommentaireByInterval(userId.value, 100, 0, ["command"]);
		usersStore.getTokenByInterval(userId.value, 100, 0);
		usersStore.userEdition = {
			loading: false,
			id_user: usersStore.users[userId.value].id_user,
			nom_user: usersStore.users[userId.value].nom_user,
			prenom_user: usersStore.users[userId.value].prenom_user,
			email_user: usersStore.users[userId.value].email_user,
			role_user: usersStore.users[userId.value].role_user,
			current_mdp_user: "",
			mdp_user: "",
			confirm_mdp_user: "",
		};
	} else {
		usersStore.userEdition = {
			loading: false,
		};
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

const userDeleteModalShow = ref(false);
const userTypeRole = ref([[0, t("user.VUserFilterRole0")], [1, t("user.VUserFilterRole1")], [2, t("user.VUserFilterRole2")]]);
const userSave = async() => {
	try {
		createSchema(isChecked).validateSync(usersStore.userEdition, { abortEarly: false });
		if (userId.value !== "new") {
			await usersStore.updateUser(userId.value, { ...usersStore.userEdition });
			addNotification({ message: "user.VUserUpdated", type: "success", i18n: true });
		} else {
			await usersStore.createUser({ ...usersStore.userEdition });
			addNotification({ message: "user.VUserCreated", type: "success", i18n: true });
		}
		usersStore.userEdition.mdp_user = "";
		usersStore.userEdition.confirm_mdp_user = "";
		usersStore.userEdition.current_mdp_user = "";
	} catch (e) {
		if (e.inner) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
	if (userId.value === "new") {
		userId.value = String(usersStore.userEdition.id_user);
		router.push("/users/" + userId.value);
	}
};
const userDelete = async() => {
	try {
		await usersStore.deleteUser(userId.value);
		addNotification({ message: "user.VUserDeleted", type: "success", i18n: true });
		router.push("/users");
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	userDeleteModalShow.value = false;
};

const revokeToken = async(tokenId) => {
	try {
		await usersStore.updateToken(userId.value, tokenId, { "revoked_reason": "Revoked by user" });
		usersStore.getTokenById(userId.value, tokenId);
		addNotification({ message: "user.VUserTokenRevoked", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};
const isCheckedTokens = ref(false);
function watchIsCheckedTokens() {
	usersStore.getTokenByInterval(userId.value, 100, 0, isCheckedTokens.value, isCheckedTokens.value);
}
watch(isCheckedTokens, watchIsCheckedTokens);

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

const labelForm = ref([
	{ key: "nom_user", label: "user.VUserName", type: "text", condition: "edition?.id_user === session?.id_user || session?.role_user === 2" },
	{ key: "prenom_user", label: "user.VUserFirstName", type: "text", condition: "edition?.id_user === session?.id_user || session?.role_user === 2" },
	{ key: "email_user", label: "user.VUserEmail", type: "text", condition: "edition?.id_user === session?.id_user || session?.role_user === 2" },
	{ key: "role_user", label: "user.VUserRole", type: "select", options: userTypeRole, condition: "session?.role_user === 2" },
	{ key: "check", label: "user.VUserCheck", type: "checkbox", model: isChecked, condition: "edition?.id_user === session?.id_user || session?.role_user === 2" },
	{ key: "mdp_user", label: "user.VUserPassword", type: "password", condition: "(edition?.id_user === session?.id_user || session?.role_user === 2) && form[4].model" },
	{ key: "confirm_mdp_user", label: "user.VUserConfirmPassword", type: "password", condition: "(edition?.id_user === session?.id_user || session?.role_user === 2) && form[4].model" },
	{ key: "current_mdp_user", label: "user.VUserCurrentPassword", type: "password", condition: "edition?.id_user === session?.id_user || session?.role_user === 2" },
]);
const labelTableauSession = ref([
	{ label: "user.VUserTokenCreatedDate", sortable: true, key: "first_created_at", type: "datetime" },
	{ label: "user.VUserTokenLastLoginDate", sortable: true, key: "created_at", type: "datetime" },
	{ label: "user.VUserTokenCreatedIP", sortable: true, key: "created_by_ip", type: "text" },
	{ label: "user.VUserTokenExpireDate", sortable: true, key: "expires_at", type: "datetime" },
	{ label: "user.VUserTokenIsRevoked", sortable: true, key: "is_revoked", type: "text" },
	{ label: "user.VUserTokenAuthMethod", sortable: true, key: "auth_method", type: "text" },
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
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('user.VUserTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/users', save: { sameUserId: true, roleRequired: 1, loading: usersStore.userEdition.loading }, delete: { sameUserId: true, roleRequired: 1 } }"
			:id="userId" :store-user="authStore.user" @button-save="userSave" @button-delete="userDeleteModalShow = true"/>
	</div>
	<div v-if="usersStore.users[userId] || userId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="usersStore.userEdition" :store-user="authStore.user"/>
		</div>
		<CollapsibleSection title="user.VUserParticipation"
			:id-page="userId">
			<template #append-row>
				<CollapsibleSection title="user.VUserCommandsCommentaires" :disable-margin="true"
					:total-count="Number(usersStore.commandsCommentaireTotalCount[userId] || 0)" :id-page="userId">
					<template #append-row>
						<Commentaire :meta="{ link: '/commands/', idRessource: 'id_command', contenu: 'contenu_command_commentaire', key: 'id_command_commentaire', canEdit: false }"
							:store-data="[usersStore.commandsCommentaire[userId],usersStore.users,authStore.user,configsStore]"
							:loading="usersStore.commandsCommentaireLoading"
							:total-count="Number(usersStore.commandsCommentaireTotalCount[userId]) || 0"
							:loaded-count="Object.keys(usersStore.commandsCommentaire[userId] || {}).length"
							:fetch-function="(offset, limit) => usersStore.getCommandCommentaireByInterval(userId, limit, offset, ['command'])"
						/>
					</template>
				</CollapsibleSection>
				<CollapsibleSection title="user.VUserProjetsCommentaires" :disable-margin="true"
					:total-count="Number(usersStore.projetsCommentaireTotalCount[userId] || 0)" :id-page="userId">
					<template #append-row>
						<Commentaire :meta="{ link: '/projets/', idRessource: 'id_projet', contenu: 'contenu_projet_commentaire', key: 'id_projet_commentaire', canEdit: false }"
							:store-data="[usersStore.projetsCommentaire[userId],usersStore.users,authStore.user,configsStore]"
							:loading="usersStore.projetsCommentaireLoading"
							:total-count="Number(usersStore.projetsCommentaireTotalCount[userId]) || 0"
							:loaded-count="Object.keys(usersStore.projetsCommentaire[userId] || {}).length"
							:fetch-function="(offset, limit) => usersStore.getProjetCommentaireByInterval(userId, limit, offset, ['projet'])"
						/>
					</template>
				</CollapsibleSection>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="user.VUserTokens"
			:total-count="Number(usersStore.tokensTotalCount[userId] || 0)" :id-page="userId">
			<template #append-row>
				<div>
					<!-- bouton pour choisir de charger les revoked et les expired -->
					<label class="inline-flex items-center mb-4">
						<input type="checkbox" class="form-checkbox h-5 w-5 text-blue-600" v-model="isCheckedTokens">
						<span class="ml-2 text-gray-700">{{ $t('user.VUserShowExpiredAndRevokedTokens') }}</span>
					</label>
				</div>
				<Tableau :labels="labelTableauSession" :meta="{ key: 'session_id' }"
					:store-data="[usersStore.tokens[userId]]"
					:loading="usersStore.tokensLoading"
					:total-count="Number(usersStore.tokensTotalCount[userId]) || 0"
					:loaded-count="Object.keys(usersStore.tokens[userId] || {}).length"
					:fetch-function="(offset, limit) => usersStore.getTokenByInterval(userId, limit, offset, isCheckedTokens, isCheckedTokens)"
					:tableau-css="{ component: 'min-h-64 max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('user.VUserLoading') }}</div>
	</div>
	<ModalDeleteConfirm :show-modal="userDeleteModalShow" @close-modal="userDeleteModalShow = false"
		@delete-confirmed="userDelete" :text-title="'user.VUserDeleteTitle'"
		:text-p="(authStore.user?.id_user !== Number(userId)) ? 'user.VUserDeleteTextAdmin' : 'user.VUserDeleteTextUser'"/>
</template>

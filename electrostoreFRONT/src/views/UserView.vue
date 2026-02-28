<script setup>
import { onMounted, onBeforeUnmount, ref, inject, watch } from "vue";
import router from "@/router";

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

import { UserRole } from "@/enums";

if ((!authStore.hasPermission([1, 2])) && authStore.user?.id_user !== Number(userId.value)) {
	addNotification({ message: "vous n'avez pas la permission d'acceder a cette page", type: "error", i18n: false });
	if (window.history.length > 1) {
		router.back();
	} else {
		router.push("/");
	}
}

async function fetchAllData() {
	if (userId.value === "new") {
		usersStore.userEdition = {
			loading: false,
		};
	} else {
		usersStore.userEdition = {
			loading: true,
		};
		try {
			await usersStore.getUserById(userId.value);
		} catch {
			delete usersStore.users[userId.value];
			addNotification({ message: "user.NotFound", type: "error", i18n: true });
			if (window.history.length > 1) {
				router.back();
			} else {
				router.push("/");
			}
			return;
		}
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
const userTypeRole = ref({ [UserRole.User]: t("user.FilterRole0"), [UserRole.Moderator]: t("user.FilterRole1"), [UserRole.Admin]: t("user.FilterRole2") });
const userSave = async() => {
	try {
		createSchema(isChecked).validateSync(usersStore.userEdition, { abortEarly: false });
		if (userId.value === "new") {
			const newId = await usersStore.createUser({ ...usersStore.userEdition });
			addNotification({ message: "user.Created", type: "success", i18n: true });
			userId.value = String(newId);
			router.push("/users/" + userId.value);
		} else {
			await usersStore.updateUser(userId.value, { ...usersStore.userEdition });
			addNotification({ message: "user.Updated", type: "success", i18n: true });
		}
		usersStore.userEdition.mdp_user = "";
		usersStore.userEdition.confirm_mdp_user = "";
		usersStore.userEdition.current_mdp_user = "";
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const userDelete = async() => {
	try {
		await usersStore.deleteUser(userId.value);
		addNotification({ message: "user.Deleted", type: "success", i18n: true });
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
		addNotification({ message: "user.TokenRevoked", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};
const isCheckedTokens = ref(false);
function watchIsCheckedTokens() {
	usersStore.getTokenByInterval(userId.value, 100, 0, [], "", "", false, isCheckedTokens.value, isCheckedTokens.value);
}
watch(isCheckedTokens, watchIsCheckedTokens);

const isChecked = ref(false);
const createSchema = (isChecked) => {
	return Yup.object().shape({
		nom_user: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("user.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
			.required(t("user.NameRequired")),
		prenom_user: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("user.FirstNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
			.required(t("user.FirstNameRequired")),
		email_user: Yup.string()
			.max(configsStore.getConfigByKey("max_length_email"), t("user.EmailMaxLength", { count: configsStore.getConfigByKey("max_length_email") }))
			.required(t("user.EmailRequired"))
			.email(t("user.EmailInvalid")),
		mdp_user: isChecked // if isChecked is true, then mdp_user is required and must be different from current_mdp_user and =like= at least 8 characters, special characters, numbers and upper and lower letters
			? Yup.string().required(t("user.PasswordRequired")).notOneOf([Yup.ref("current_mdp_user"), null], t("user.PasswordMatch")).matches(
				/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
				t("user.PasswordComplexity"),
			)
			: Yup.string().nullable(),
		confirm_mdp_user: isChecked // if isChecked is true, then confirm_mdp_user is required and must match mdp_user and =like= at least 8 characters, special characters, numbers and upper and lower letters
			? Yup.string().required(t("user.ConfirmPasswordRequired")).oneOf([Yup.ref("mdp_user"), null], t("user.ConfirmPasswordMatch")).matches(
				/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
				t("user.PasswordComplexity"),
			)
			: Yup.string().nullable(),
		// current_mdp_user if always required and not nullable
		current_mdp_user: Yup.string()
			.required(t("user.CurrentPasswordRequired")),
	});
};

const labelForm = ref([
	{ key: "nom_user", label: "user.Name", type: "text", condition: "edition?.id_user === session?.id_user || func.hasPermission([2])" },
	{ key: "prenom_user", label: "user.FirstName", type: "text", condition: "edition?.id_user === session?.id_user || func.hasPermission([2])" },
	{ key: "email_user", label: "user.Email", type: "text", condition: "edition?.id_user === session?.id_user || func.hasPermission([2])" },
	{ key: "role_user", label: "user.Role", type: "select", options: userTypeRole, condition: "func.hasPermission([2])" },
]);
if (!authStore.isSSOUser && (authStore.user?.id_user === Number(userId.value) || authStore.hasPermission([2]))) {
	labelForm.value.push(
		{ key: "check", label: "user.Check", type: "checkbox", model: isChecked, condition: "edition?.id_user === session?.id_user || func.hasPermission([2])" },
		{ key: "mdp_user", label: "user.Password", type: "password", condition: "(edition?.id_user === session?.id_user || func.hasPermission([2])) && form[4].model" },
		{ key: "confirm_mdp_user", label: "user.ConfirmPassword", type: "password", condition: "(edition?.id_user === session?.id_user || func.hasPermission([2])) && form[4].model" },
		{ key: "current_mdp_user", label: "user.CurrentPassword", type: "password", condition: "edition?.id_user === session?.id_user || func.hasPermission([2])" },
	);
}

const labelTableauSession = ref([
	{ label: "user.TokenCreatedDate", sortable: true, key: "first_created_at", valueKey: "first_created_at", type: "datetime" },
	{ label: "user.TokenLastLoginDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "user.TokenCreatedIP", sortable: true, key: "created_by_ip", valueKey: "created_by_ip", type: "text" },
	{ label: "user.TokenExpireDate", sortable: true, key: "expires_at", valueKey: "expires_at", type: "datetime" },
	{ label: "user.TokenIsRevoked", sortable: true, key: "is_revoked", valueKey: "is_revoked", type: "text" },
	{ label: "user.TokenAuthMethod", sortable: true, key: "auth_method", valueKey: "auth_method", type: "text" },
	{ label: "user.TokenRevokedDate", sortable: true, key: "revoked_at", valueKey: "revoked_at", type: "datetime" },
	{ label: "user.TokenRevokedIP", sortable: true, key: "revoked_by_ip", valueKey: "revoked_by_ip", type: "text" },
	{ label: "user.TokenRevokedReason", sortable: true, key: "revoked_reason", valueKey: "revoked_reason", type: "text" },
	{ label: "user.TokenActions", sortable: false, key: "", type: "buttons", valueKey: "", buttons: [
		{
			label: "user.TokenRevoke",
			icon: "fa-solid fa-ban",
			condition: "!rowData?.is_revoked",
			action: (row) => revokeToken(row.session_id),
			class: "bg-red-500 text-white px-2 py-1 rounded hover:bg-red-600",
			animation: true,
		},
	] },
]);
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('user.Title') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/users', save: { sameUserId: true, roleRequired: authStore.hasPermission([1, 2]), loading: usersStore.userEdition.loading }, delete: { sameUserId: true, roleRequired: authStore.hasPermission([1, 2]) } }"
			:id="userId" :store-user="authStore.user" @button-save="userSave" @button-delete="userDeleteModalShow = true"/>
	</div>
	<div v-if="usersStore.users[userId] || userId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="usersStore.userEdition" :store-user="authStore.user"
				:store-function="{ hasPermission: (validPerm) => authStore.hasPermission(validPerm) }"/>
		</div>
		<CollapsibleSection title="user.Participation"
			:id-page="userId">
			<template #append-row>
				<CollapsibleSection title="user.CommandsCommentaires" :disable-margin="true"
					:total-count="Number(usersStore.commandsCommentaireTotalCount[userId] || 0)" :id-page="userId">
					<template #append-row>
						<Commentaire :meta="{ link: '/commands/', idRessource: 'id_command', contenu: 'contenu_command_commentaire', key: 'id_command_commentaire', canEdit: false, roleRequired: false, expand: ['command'] }"
							:store-data="[usersStore.commandsCommentaire[userId], usersStore.users]"
							:store-user="authStore.user" :store-config="configsStore"
							:loading="usersStore.commandsCommentaireLoading"
							:total-count="Number(usersStore.commandsCommentaireTotalCount[userId]) || 0"
							:fetch-function="userId !== 'new' ? (limit, offset, expand, filter, sort, clear) => usersStore.getCommandCommentaireByInterval(userId, limit, offset, expand, filter, sort, clear) : undefined"
						/>
					</template>
				</CollapsibleSection>
				<CollapsibleSection title="user.ProjetsCommentaires" :disable-margin="true"
					:total-count="Number(usersStore.projetsCommentaireTotalCount[userId] || 0)" :id-page="userId">
					<template #append-row>
						<Commentaire :meta="{ link: '/projets/', idRessource: 'id_projet', contenu: 'contenu_projet_commentaire', key: 'id_projet_commentaire', canEdit: false, roleRequired: false, expand: ['projet'] }"
							:store-data="[usersStore.projetsCommentaire[userId], usersStore.users]"
							:store-user="authStore.user" :store-config="configsStore"
							:loading="usersStore.projetsCommentaireLoading"
							:total-count="Number(usersStore.projetsCommentaireTotalCount[userId]) || 0"
							:fetch-function="userId !== 'new' ? (limit, offset, expand, filter, sort, clear) => usersStore.getProjetCommentaireByInterval(userId, limit, offset, expand, filter, sort, clear) : undefined"
						/>
					</template>
				</CollapsibleSection>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="user.Tokens"
			:total-count="Number(usersStore.tokensTotalCount[userId] || 0)" :id-page="userId">
			<template #append-row>
				<div>
					<!-- bouton pour choisir de charger les revoked et les expired -->
					<label class="inline-flex items-center mb-4">
						<input type="checkbox" class="form-checkbox h-5 w-5 text-blue-600" v-model="isCheckedTokens">
						<span class="ml-2 text-gray-700">{{ $t('user.ShowExpiredAndRevokedTokens') }}</span>
					</label>
				</div>
				<Tableau :labels="labelTableauSession" :meta="{ key: 'session_id' }"
					:store-data="[usersStore.tokens[userId]]"
					:loading="usersStore.tokensLoading"
					:total-count="Number(usersStore.tokensTotalCount[userId]) || 0"
					:fetch-function="userId !== 'new' ? (limit, offset, expand, filter, sort, clear) => usersStore.getTokenByInterval(userId, limit, offset, expand, filter, sort, clear, isCheckedTokens, isCheckedTokens) : undefined"
					:tableau-css="{ component: 'min-h-64 max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('user.Loading') }}</div>
	</div>
	<ModalDeleteConfirm :show-modal="userDeleteModalShow" @close-modal="userDeleteModalShow = false"
		:delete-action="userDelete" :text-title="'user.DeleteTitle'"
		:text-p="(authStore.user?.id_user !== Number(userId)) ? 'user.DeleteTextAdmin' : 'user.DeleteTextUser'"/>
</template>

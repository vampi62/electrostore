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
const preset = ref(route.query.preset || null);

import { useConfigsStore, useUsersStore, useCommandsStore, useProjetsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const usersStore = useUsersStore();
const commandsStore = useCommandsStore();
const projetsStore = useProjetsStore();
const authStore = useAuthStore();

const formContainer = ref(null);

import { UserRole } from "@/enums";

if ((!authStore.hasPermission([1, 2])) && authStore.user?.id_user !== Number(userId.value)) {
	addNotification({ message: t("user.noAccess"), type: "error" });
	if (window.history.length > 1) {
		router.back();
	} else {
		router.push("/");
	}
}

async function fetchAllData() {
	if (userId.value === "new") {
		loadToEdition(userId.value);
		if (preset.value) {
			preset.value.split(";").forEach((pair) => {
				const [key, value] = pair.split(":");
				if (key && value) {
					usersStore.userEdition[key] = value;
				}
			});
		}
	} else {
		usersStore.userEdition = {
			loading: true,
		};
		try {
			await usersStore.getUserById(userId.value);
		} catch {
			delete usersStore.users[userId.value];
			addNotification({ message: t("user.NotFound"), type: "error" });
			if (window.history.length > 1) {
				router.back();
			} else {
				router.push("/");
			}
			return;
		}
		loadToEdition(userId.value);
	}
}
function loadToEdition(id) {
	if (id === "new") {
		usersStore.userEdition = {
			loading: false,
		};
	} else {
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
		usersStore.userEdition.loading = true;
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("user.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			usersStore.userEdition.loading = false;
			return;
		}
		if (userId.value === "new") {
			const newId = await usersStore.createUser({ ...usersStore.userEdition });
			loadToEdition(newId);
			addNotification({ message: t("user.Created"), type: "success" });
			userId.value = String(newId);
			router.push("/users/" + userId.value);
		} else {
			await usersStore.updateUser(userId.value, { ...usersStore.userEdition });
			loadToEdition(userId.value);
			addNotification({ message: t("user.Updated"), type: "success" });
		}
		usersStore.userEdition.mdp_user = "";
		usersStore.userEdition.confirm_mdp_user = "";
		usersStore.userEdition.current_mdp_user = "";
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		usersStore.userEdition.loading = false;
	}
};
const userDelete = async() => {
	try {
		await usersStore.deleteUser(userId.value);
		addNotification({ message: t("user.Deleted"), type: "success" });
		router.push("/users");
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	userDeleteModalShow.value = false;
};

const revokeToken = async(tokenId) => {
	try {
		await usersStore.updateToken(userId.value, tokenId, { "revoked_reason": "Revoked by user" });
		usersStore.getTokenById(userId.value, tokenId);
		addNotification({ message: t("user.TokenRevoked"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const createSchema = () => {
	const edition = usersStore.userEdition;
	const shape = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.nom_user = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("user.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("user.NameRequired"));
	shape.prenom_user = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("user.FirstNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("user.FirstNameRequired"));
	shape.email_user = Yup.string()
		.max(configsStore.getConfigByKey("max_length_email"), t("user.EmailMaxLength", { count: configsStore.getConfigByKey("max_length_email") }))
		.required(t("user.EmailRequired"))
		.email(t("user.EmailInvalid"));
	if (edition?._check) {
		shape.mdp_user = Yup.string()
			.required(t("user.PasswordRequired")).notOneOf([Yup.ref("current_mdp_user"), null], t("user.PasswordMatch")).matches(
				/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
				t("user.PasswordComplexity"),
			);
		shape.confirm_mdp_user = Yup.string()
			.required(t("user.ConfirmPasswordRequired")).oneOf([Yup.ref("mdp_user"), null], t("user.ConfirmPasswordMatch")).matches(
				/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
				t("user.PasswordComplexity"),
			);
	} else {
		shape.mdp_user = Yup.string().nullable();
		shape.confirm_mdp_user = Yup.string().nullable();
	}
	shape.current_mdp_user = Yup.string()
		.required(t("user.CurrentPasswordRequired"));
	return Yup.object().shape(shape);
};

const labelForm = ref([
	{ key: "nom_user", label: "user.Name", type: "text", enableCondition: "edition?.id_user === session?.id_user || func.hasPermission([2])" },
	{ key: "prenom_user", label: "user.FirstName", type: "text", enableCondition: "edition?.id_user === session?.id_user || func.hasPermission([2])" },
	{ key: "email_user", label: "user.Email", type: "text", enableCondition: "edition?.id_user === session?.id_user || func.hasPermission([2])" },
	{ key: "role_user", label: "user.Role", type: "select", options: userTypeRole, enableCondition: "func.hasPermission([2])" },
	{ key: "_check", label: "user.Check", type: "checkbox", enableCondition: "edition?.id_user === session?.id_user || func.hasPermission([2])",
		showCondition: "!session?.isSSOUser && (edition?.id_user === session?.id_user || func.hasPermission([2]))" },
	{ key: "mdp_user", label: "user.Password", type: "password", enableCondition: "(edition?.id_user === session?.id_user || func.hasPermission([2])) && edition?._check",
		showCondition: "!session?.isSSOUser && (edition?.id_user === session?.id_user || func.hasPermission([2]))" },
	{ key: "confirm_mdp_user", label: "user.ConfirmPassword", type: "password", enableCondition: "(edition?.id_user === session?.id_user || func.hasPermission([2])) && edition?._check",
		showCondition: "!session?.isSSOUser && (edition?.id_user === session?.id_user || func.hasPermission([2]))" },
	{ key: "current_mdp_user", label: "user.CurrentPassword", type: "password", enableCondition: "edition?.id_user === session?.id_user || func.hasPermission([2])",
		showCondition: "!session?.isSSOUser && (edition?.id_user === session?.id_user || func.hasPermission([2]))" },
]);

const filterSession = ref([
	{ key: "is_revoked", disableLocalFilter: true, value: "", typeData: "bool", valueIfTrue: "true", valueIfFalse: "", preset: false,
		type: "checkbox", label: "user.ShowExpiredAndRevokedTokens", compareMethod: "==" },
]);
const labelTableauSession = ref([
	{ label: "user.TokenCreatedDate", sortable: true, key: "first_created_at", valueKey: "first_created_at", type: "datetime" },
	{ label: "user.TokenLastLoginDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "user.TokenCreatedIP", sortable: true, key: "created_by_ip", valueKey: "created_by_ip", type: "text" },
	{ label: "user.TokenExpireDate", sortable: true, key: "expires_at", valueKey: "expires_at", type: "datetime" },
	{ label: "user.TokenIsRevoked", sortable: true, key: "is_revoked", valueKey: "is_revoked", type: "bool" },
	{ label: "user.TokenAuthMethod", sortable: true, key: "auth_method", valueKey: "auth_method", type: "text" },
	{ label: "user.TokenRevokedDate", sortable: true, key: "revoked_at", valueKey: "revoked_at", type: "datetime" },
	{ label: "user.TokenRevokedIP", sortable: true, key: "revoked_by_ip", valueKey: "revoked_by_ip", type: "text" },
	{ label: "user.TokenRevokedReason", sortable: true, key: "revoked_reason", valueKey: "revoked_reason", type: "text" },
	{ label: "user.TokenActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "user.TokenRevoke",
			icon: "fa-solid fa-ban",
			showCondition: "!rowData?.is_revoked",
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
		<TopButtonEditElement
			:main-config="{ path: '/users',
				create: { sameUserId: true, showCondition: userId === 'new' && authStore.hasPermission([1, 2]), loading: usersStore.userEdition?.loading },
				update: { sameUserId: true, showCondition: userId !== 'new' && authStore.hasPermission([1, 2]), loading: usersStore.userEdition?.loading },
				delete: { sameUserId: true, showCondition: userId !== 'new' && authStore.hasPermission([1, 2]) }
			}"
			@button-create="userSave" @button-update="userSave" @button-delete="userDeleteModalShow = true"/>
	</div>
	<div v-if="usersStore.users[userId] || userId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="usersStore.userEdition" :store-user="authStore.user"
				:store-function="{ hasPermission: (validPerm) => authStore.hasPermission(validPerm) }"/>
		</div>
		<CollapsibleSection title="user.Participation" :permission="userId !=='new'">
			<template #append-row>
				<CollapsibleSection title="user.CommandsCommentaires" :disable-margin="true"
					:total-count="Number(usersStore.commandsCommentaireTotalCount[userId] || 0)" :permission="userId !=='new'">
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
					:total-count="Number(usersStore.projetsCommentaireTotalCount[userId] || 0)" :permission="userId !=='new'">
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
			:total-count="Number(usersStore.tokensTotalCount[userId] || 0)" :permission="userId !=='new'">
			<template #append-row>
				<FilterContainer class="my-4 flex gap-4" :filters="filterSession" :store-data="usersStore.tokens[userId]" />
				<Tableau :labels="labelTableauSession" :meta="{ key: 'session_id' }"
					:store-data="[usersStore.tokens[userId]]"
					:filters="filterSession"
					:loading="usersStore.tokensLoading"
					:total-count="Number(usersStore.tokensTotalCount[userId]) || 0"
					:fetch-function="userId !== 'new' ? (limit, offset, expand, filter, sort, clear) => usersStore.getTokenByInterval(userId, limit, offset, expand, filter, sort, clear) : undefined"
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

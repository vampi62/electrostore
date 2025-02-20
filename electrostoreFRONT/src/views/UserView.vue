<script setup>
import { onMounted, onBeforeUnmount, ref, computed, inject, watch } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const userId = route.params.id;

import { useConfigsStore, useUsersStore, useCommandsStore, useProjetsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const usersStore = useUsersStore();
const commandsStore = useCommandsStore();
const projetsStore = useProjetsStore();
const authStore = useAuthStore();

if (authStore.user?.role_user !== "admin" && authStore.user?.id_user !== Number(userId)) {
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
		usersStore.getProjectCommentaireByInterval(userId, 100, 0, ["projet"]);
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
const userTypeRole = ref([["admin", t("user.VUserFilterRole1")], ["user", t("user.VUserFilterRole2")]]);
const userSave = async() => {
	try {
		await schemaUser.value.validate(usersStore.userEdition, { abortEarly: false });
		await usersStore.createUser(usersStore.userEdition);
		addNotification({ message: "user.VUserCreated", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	userId = usersStore.userEdition.id_user;
	router.push("/users/" + usersStore.userEdition.id_user);
};
const userUpdate = async() => {
	try {
		await schemaUser.value.validate(usersStore.userEdition, { abortEarly: false });
		await usersStore.updateUser(userId, { ...usersStore.userEdition });
		addNotification({ message: "user.VUserUpdated", type: "success", i18n: true });
		usersStore.userEdition.mdp_user = "";
		usersStore.userEdition.confirm_mdp_user = "";
		usersStore.userEdition.current_mdp_user = "";
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
};
const userDelete = async() => {
	try {
		await usersStore.deleteUser(userId);
		addNotification({ message: "user.VUserDeleted", type: "success", i18n: true });
		router.push("/users");
	} catch (e) {
		addNotification({ message: "user.VUserDeleteError", type: "error", i18n: true });
	}
	userDeleteModalShow.value = false;
};

const revokeToken = async(tokenId) => {
	try {
		await usersStore.updateToken(userId, tokenId, { "revoked_reason": "Revoked by user" });
		usersStore.getTokenById(userId, tokenId);
		addNotification({ message: "user.VUserTokenRevoked", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: "user.VUserTokenRevokeError", type: "error", i18n: true });
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
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('user.VUserTitle') }}</h2>
		<div class="flex space-x-4">
			<button type="button" @click="userSave" v-if="userId == 'new' && authStore.user?.role_user == 'admin'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="usersStore.userEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('user.VUserAdd') }}
			</button>
			<button type="button" @click="userUpdate"
				v-else-if="userId != 'new' && authStore.user?.role_user == 'admin'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="usersStore.userEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('user.VUserUpdate') }}
			</button>
			<button type="button" @click="userDeleteModalShow = true"
				v-if="userId != 'new' && authStore.user?.role_user == 'admin'"
				class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">
				{{ $t('user.VUserDelete') }}
			</button>
			<RouterLink to="/users"
				class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">
				{{ $t('user.VUserBack') }}
			</RouterLink>
		</div>
	</div>
	<div v-if="usersStore.users[userId] || userId == 'new'">
		<div class="mb-6 flex justify-between flex-wrap">
			<Form :validation-schema="schemaUser" v-slot="{ errors }" @submit.prevent="" class="mb-6">
				<table class="table-auto text-gray-700">
					<tbody>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('user.VUserName') }}:</td>
							<td class="flex flex-col">
								<Field name="nom_user" type="text" v-model="usersStore.userEdition.nom_user"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.nom_user }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.nom_user || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('user.VUserFirstName') }}:</td>
							<td class="flex flex-col">
								<Field name="prenom_user" type="text" v-model="usersStore.userEdition.prenom_user"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.prenom_user }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.prenom_user || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('user.VUserEmail') }}:</td>
							<td class="flex flex-col">
								<Field name="email_user" type="text" v-model="usersStore.userEdition.email_user"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.email_user }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.email_user || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('user.VUserRole') }}:</td>
							<td class="flex flex-col">
								<Field name="role_user" as="select"
									v-model="usersStore.userEdition.role_user"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.role_user }"
									:disabled="authStore.user?.role_user !== 'admin'">
									<option value="" disabled :selected="!usersStore.userEdition.role_user"> -- {{ $t('user.VUserSelectRole') }} -- </option>
									<option v-for="role in userTypeRole" :key="role[0]" :value="role[0]">
										{{ role[1] }}
									</option>
								</Field>
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.role_user || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('user.VUserCheck') }}:</td>
							<td class="flex flex-col">
								<Field name="check" v-slot="{ is_checked_custom }">
									<input
										v-model="isChecked"
										v-bind="is_checked_custom"
										type="checkbox"
										:checked="isChecked"
										class="form-checkbox h-5 w-5 text-blue-600"
									/>
								</Field>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('user.VUserPassword') }}:</td>
							<td class="flex flex-col">
								<Field name="mdp_user" type="text" v-model="usersStore.userEdition.mdp_user"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.mdp_user }"
									:disabled="!isChecked" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.mdp_user || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('user.VUserConfirmPassword') }}:</td>
							<td class="flex flex-col">
								<Field name="confirm_mdp_user" type="password" v-model="usersStore.userEdition.confirm_mdp_user"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.confirm_mdp_user }"
									:disabled="!isChecked" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.confirm_mdp_user || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('user.VUserCurrentPassword') }}:</td>
							<td class="flex flex-col">
								<Field name="current_mdp_user" type="password" v-model="usersStore.userEdition.current_mdp_user"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.current_mdp_user }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.current_mdp_user || ' ' }}</span>
							</td>
						</tr>
					</tbody>
				</table>
			</Form>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleParticipation" class="text-xl font-semibold  bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': userId != 'new', 'cursor-not-allowed': userId == 'new' }">
				{{ $t('user.VUserParticipation') }}
			</h3>
			<div v-if="showParticipation" class="p-2">
				<div class="mb-6 bg-gray-100 p-2 rounded">
					<h3 @click="toggleCommandCommentaires" class="text-xl font-semibold bg-gray-400 p-2 rounded"
						:class="{ 'cursor-pointer': !usersStore.commandsCommentaireLoading && userId != 'new', 'cursor-not-allowed': userId == 'new' }">
						{{ $t('user.VUserCommandsCommentaires') }} ({{ usersStore.commandsCommentaire[userId] ?
							Object.keys(usersStore.commandsCommentaire[userId]).length : 0 }})
					</h3>
					<div v-if="!usersStore.CommandsCommentaireLoading && showCommandCommentaires" class="p-2">
						<div class="space-y-4 overflow-x-auto max-h-64 overflow-y-auto">
							<div v-for="commentaire in usersStore.commandsCommentaire[userId]"
								:key="commentaire.id_command_commentaire" class="flex flex-col border p-4 rounded-lg">
								<div class="text-sm text-gray-600">
									<span class="font-semibold">
										{{ usersStore.users[commentaire.id_user].nom_user }} {{
											usersStore.users[commentaire.id_user].prenom_user }}
									</span>
									<span class="text-xs text-gray-500">
										- {{ commentaire.date_command_commentaire }}
									</span>
								</div>
								<div class="text-sm text-gray-800 mb-2">
									<div>
										{{ commentaire.contenu_command_commentaire }}
									</div>
									<RouterLink :to="'/commands/' + commentaire.id_command"
										class="text-blue-500 hover:underline">
										{{ $t('user.VUserCommandsCommentairesLink') }}
									</RouterLink>
								</div>
							</div>
						</div>
					</div>
				</div>
				<div class="mb-6 bg-gray-100 p-2 rounded">
					<h3 @click="toggleProjetCommentaires" class="text-xl font-semibold bg-gray-400 p-2 rounded"
						:class="{ 'cursor-pointer': !usersStore.projetsCommentaireLoading && userId != 'new', 'cursor-not-allowed': userId == 'new' }">
						{{ $t('user.VUserProjetsCommentaires') }} ({{ usersStore.projetsCommentaire[userId] ?
							Object.keys(usersStore.projetsCommentaire[userId]).length : 0 }})
					</h3>
					<div v-if="!usersStore.ProjetsCommentaireLoading && showProjetCommentaires" class="p-2">
						<div class="space-y-4 overflow-x-auto max-h-64 overflow-y-auto">
							<div v-for="commentaire in usersStore.projetsCommentaire[userId]"
								:key="commentaire.id_projet_commentaire" class="flex flex-col border p-4 rounded-lg">
								<div class="text-sm text-gray-600">
									<span class="font-semibold">
										{{ usersStore.users[commentaire.id_user].nom_user }} {{
											usersStore.users[commentaire.id_user].prenom_user }}
									</span>
									<span class="text-xs text-gray-500">
										- {{ commentaire.date_projet_commentaire }}
									</span>
								</div>
								<div class="text-sm text-gray-800 mb-2">
									<div>
										{{ commentaire.contenu_projet_commentaire }}
									</div>
									<RouterLink :to="'/projets/' + commentaire.id_projet"
										class="text-blue-500 hover:underline">
										{{ $t('user.VUserProjetsCommentairesLink') }}
									</RouterLink>
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>
		<div class="mb-6 bg-gray-100 p-2 rounded">
			<h3 @click="toggleTokens" class="text-xl font-semibold bg-gray-400 p-2 rounded"
				:class="{ 'cursor-pointer': !usersStore.tokensLoading && userId != 'new', 'cursor-not-allowed': userId == 'new' }">
				{{ $t('user.VUserTokens') }} ({{ usersStore.tokens[userId] ?
					Object.keys(usersStore.tokens[userId]).length : 0 }})
			</h3>
			<div v-if="!usersStore.tokensLoading && showTokens" class="p-2">
				<div class="overflow-x-auto max-h-64 overflow-y-auto">
					<table class="min-w-full table-auto">
						<thead>
							<tr>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('user.VUserTokenCreatedDate') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('user.VUserTokenCreatedIP') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('user.VUserTokenExpireDate') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('user.VUserTokenIsRevoked') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('user.VUserTokenRevokedDate') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('user.VUserTokenRevokedIP') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('user.VUserTokenRevokedReason') }}
								</th>
								<th class="px-4 py-2 text-left bg-gray-200 sticky top-0">
									{{ $t('user.VUserTokenActions') }}
								</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="token in usersStore.tokens[userId]" :key="token.id_jwi_refresh">
								<td class="px-4 py-2 border-b border-gray-200">
									{{ token.created_at }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200">
									{{ token.created_by_ip }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200">
									{{ token.expires_at }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200">
									{{ token.is_revoked }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200">
									{{ token.revoked_at }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200">
									{{ token.revoked_by_ip }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200">
									{{ token.revoked_reason }}
								</td>
								<td class="px-4 py-2 border-b border-gray-200">
									<button type="button"
										class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600"
										@click="revokeToken(token.id_jwi_refresh)">
										{{ $t('user.VUserTokenRevoke') }}
									</button>
								</td>
							</tr>
						</tbody>
					</table>
				</div>
			</div>
		</div>
	</div>
	<div v-else>
		<div>{{ $t('user.VUserLoading') }}</div>
	</div>

	<div v-if="userDeleteModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="userDeleteModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t('user.VUserDeleteTitle') }}</h2>
			<p v-if="authStore.user?.id_user !== Number(userId)">{{ $t('user.VUserDeleteTextAdmin') }}</p>
			<p v-else>{{ $t('user.VUserDeleteTextUser') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="userDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('user.VUserDeleteConfirm') }}
				</button>
				<button type="button" @click="userDeleteModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('user.VUserDeleteCancel') }}
				</button>
			</div>
		</div>
	</div>
</template>

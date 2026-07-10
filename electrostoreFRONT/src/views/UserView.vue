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
			id_user: usersStore.users[id].id_user,
			nom_user: usersStore.users[id].nom_user,
			prenom_user: usersStore.users[id].prenom_user,
			email_user: usersStore.users[id].email_user,
			role_user: usersStore.users[id].role_user,
			current_mdp_user: "",
			mdp_user: "",
			confirm_mdp_user: "",
		};
	}
}
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

// --- Push Notifications ---
const pushSupported = typeof window !== "undefined" && "PushManager" in window && "serviceWorker" in navigator && typeof Notification !== "undefined";
const pushSubscriptionId = ref(null); // id de l'abonnement actuel dans l'API
const pushLoading = ref(false);
const pushDeviceName = ref("");
const notificationPermission = ref(typeof Notification !== "undefined" ? Notification.permission : "default");

function urlBase64ToUint8Array(base64String) {
	const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
	const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
	const rawData = window.atob(base64);
	const outputArray = new Uint8Array(rawData.length);
	for (let i = 0; i < rawData.length; ++i) {
		outputArray[i] = rawData.charCodeAt(i);
	}
	return outputArray;
}

async function checkExistingSubscription() {
	if (!pushSupported || userId.value === "new") {
		pushSubscriptionId.value = null;
		return;
	}
	notificationPermission.value = Notification.permission;
	if (Notification.permission !== "granted") {
		pushSubscriptionId.value = null;
		return;
	}
	try {
		const reg = await navigator.serviceWorker.ready;
		const sub = await reg.pushManager.getSubscription();
		if (!sub) {
			pushSubscriptionId.value = null;
			return;
		}
		// Cherche si cet endpoint est déjà enregistré côté API pour cet utilisateur
		await usersStore.getPushSubscriptionsByInterval(userId.value, 100, 0, true);
		const subs = usersStore.pushSubscriptions[userId.value] || {};
		const match = Object.values(subs).find((s) => s.endpoint === sub.endpoint);
		pushSubscriptionId.value = match ? match.id_push_subscription : null;
	} catch (e) {
		pushSubscriptionId.value = null;
	}
}

async function subscribePush() {
	if (!pushSupported) {
		addNotification({ message: t("user.PushNotSupported"), type: "error" });
		return;
	}
	pushLoading.value = true;
	try {
		const permission = await Notification.requestPermission();
		notificationPermission.value = permission;
		if (permission !== "granted") {
			addNotification({ message: t("user.PushPermissionDenied"), type: "error" });
			return;
		}
		const vapidKey = import.meta.env.VITE_VAPID_PUBLIC_KEY;
		if (!vapidKey) {
			addNotification({ message: t("user.PushSubscribeError"), type: "error" });
			return;
		}
		const reg = await navigator.serviceWorker.ready;
		const sub = await reg.pushManager.subscribe({
			userVisibleOnly: true,
			applicationServerKey: urlBase64ToUint8Array(vapidKey),
		});
		const json = sub.toJSON();
		const created = await usersStore.createPushSubscription(userId.value, {
			endpoint: json.endpoint,
			p256dh: json.keys.p256dh,
			auth: json.keys.auth,
			device_name: pushDeviceName.value || undefined,
		});
		pushSubscriptionId.value = created.id_push_subscription;
		addNotification({ message: t("user.PushSubscribed"), type: "success" });
	} catch (e) {
		addNotification({ message: t("user.PushSubscribeError"), type: "error" });
	} finally {
		pushLoading.value = false;
	}
}

async function unsubscribePush() {
	if (!pushSupported || !pushSubscriptionId.value) {
		addNotification({ message: t("user.PushNotSupported"), type: "error" });
		return;
	}
	pushLoading.value = true;
	try {
		const reg = await navigator.serviceWorker.ready;
		const sub = await reg.pushManager.getSubscription();
		if (sub) {
			await sub.unsubscribe();
		}
		await usersStore.deletePushSubscription(userId.value, pushSubscriptionId.value);
		pushSubscriptionId.value = null;
	} catch (e) {
		addNotification({ message: t("user.PushDeleteError"), type: "error" });
	} finally {
		pushLoading.value = false;
	}
}

const sendTestNotification = async() => {
	try {
		await usersStore.sendTestPushNotification(userId.value);
		addNotification({ message: t("user.PushTestSent"), type: "success" });
	} catch (e) {
		addNotification({ message: t("user.PushTestError"), type: "error" });
	}
};
const sendTestEmailNotification = async() => {
	try {
		await usersStore.sendTestEmailNotification(userId.value);
		addNotification({ message: t("user.PushTestSent"), type: "success" });
	} catch (e) {
		addNotification({ message: t("user.PushTestError"), type: "error" });
	}
};

const deletePushSubscriptionFromTable = async(subscriptionId) => {
	try {
		if (subscriptionId === pushSubscriptionId.value) {
			if (pushSupported) {
				const reg = await navigator.serviceWorker.ready;
				const sub = await reg.pushManager.getSubscription();
				if (sub) {
					await sub.unsubscribe();
				}
			}
			pushSubscriptionId.value = null;
		}
		await usersStore.deletePushSubscription(userId.value, subscriptionId);
		addNotification({ message: t("user.PushDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: t("user.PushDeleteError"), type: "error" });
	}
};

const labelTableauPushSubscriptions = ref([
	{ label: "user.PushDeviceName", sortable: false, key: "device_name", valueKey: "device_name", type: "text" },
	{ label: "user.PushEndpoint", sortable: false, key: "endpoint", valueKey: "endpoint", type: "text" },
	{ label: "user.PushCreatedAt", sortable: false, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "user.TokenActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "user.PushDeleteBtn",
			icon: "fa-solid fa-trash",
			action: (row) => deletePushSubscriptionFromTable(row.id_push_subscription),
			class: "bg-red-500 text-white px-2 py-1 rounded hover:bg-red-600",
			animation: true,
		},
	] },
]);

onMounted(() => {
	fetchAllData().then(() => checkExistingSubscription());
});</script>

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
		<CollapsibleSection title="user.PushNotifications"
			:total-count="Number(usersStore.pushSubscriptionsTotalCount[userId] || 0)" :permission="userId !== 'new'"
			v-if="configsStore.getStatusByKey('notif_webPush')">
			<template #append-row>
				<div v-if="notificationPermission === 'denied'" class="flex items-center gap-2 rounded-md bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700 mb-2">
					<i class="fa-solid fa-ban"></i>
					{{ $t('user.PushPermissionDenied') }}
				</div>
				<div v-if="pushSupported" class="flex flex-col gap-4 py-2">
					<div v-if="!pushSubscriptionId" class="flex items-end gap-3 flex-wrap">
						<div class="flex flex-col gap-1">
							<label class="text-sm font-medium text-gray-700">{{ $t('user.PushDeviceName') }}</label>
							<input v-model="pushDeviceName" type="text" maxlength="255"
								class="border border-gray-300 rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
								:placeholder="$t('user.PushDeviceName')" />
						</div>
						<button @click="subscribePush" :disabled="pushLoading"
							class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 disabled:opacity-50 text-sm">
							{{ $t('user.PushSubscribe') }}
						</button>
					</div>
					<div v-else class="flex items-center gap-4 flex-wrap">
						<span class="text-green-600 font-medium text-sm">✓ {{ $t('user.PushSubscribed') }}</span>
						<button @click="unsubscribePush" :disabled="pushLoading"
							class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600 disabled:opacity-50 text-sm">
							{{ $t('user.PushUnsubscribe') }}
						</button>
					</div>
				</div>
				<div v-else class="text-gray-500 italic text-sm py-2">
					{{ $t('user.PushNotSupported') }}
				</div>
				<div class="flex justify-end mb-2">
					<button @click="sendTestNotification"
						class="bg-yellow-500 text-white px-4 py-2 rounded hover:bg-yellow-600 text-sm">
						<i class="fa-solid fa-bell mr-1"></i>{{ $t('user.PushSendTest') }}
					</button>
					<button @click="sendTestEmailNotification"
						class="bg-yellow-500 text-white px-4 py-2 rounded hover:bg-yellow-600 text-sm">
						<i class="fa-solid fa-bell mr-1"></i>{{ $t('user.PushSendTestEmail') }}
					</button>
				</div>
				<Tableau :labels="labelTableauPushSubscriptions" :meta="{ key: 'id_push_subscription' }"
					:store-data="[usersStore.pushSubscriptions[userId]]"
					:loading="usersStore.pushSubscriptionsLoading"
					:total-count="Number(usersStore.pushSubscriptionsTotalCount[userId]) || 0"
					:fetch-function="userId !== 'new' ? (limit, offset, expand, filter, sort, clear) => usersStore.getPushSubscriptionsByInterval(userId, limit, offset, clear) : undefined"
					:tableau-css="{ component: 'min-h-32 max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
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

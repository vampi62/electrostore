import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";

import { useCommandsStore, useProjectsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
	project_comments: (store, idUser, user) => {
		store.projetsCommentaire[idUser] = {};
		for (const projetCommentaire of user.project_comments) {
			store.projetsCommentaire[idUser][projetCommentaire.id_project] = projetCommentaire;
		}
	},
	command_comments: (store, idUser, user) => {
		store.commandsCommentaire[idUser] = {};
		for (const commandCommentaire of user.command_comments) {
			store.commandsCommentaire[idUser][commandCommentaire.id_command] = commandCommentaire;
		}
	},
	tokens: (store, idUser, user) => {
		store.tokens[idUser] = {};
		for (const token of user.sessions) {
			store.tokens[idUser][token.session_id] = token;
		}
	},
	push_subscriptions: (store, idUser, user) => {
		store.pushSubscriptions[idUser] = {};
		for (const sub of user.push_subscriptions) {
			store.pushSubscriptions[idUser][sub.id_push_subscription] = sub;
		}
	},
};

function hydrateUser(store, idUser, user, expand = []) {
	store.projetsCommentaireTotalCount[idUser] = user.project_comments_count;
	store.commandsCommentaireTotalCount[idUser] = user.command_comments_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idUser, user);
		}
	}
}

const userResource = createMainResource({
	path: () => "/user",
	idField: "id_user",
	stateKey: "users",
	countKey: "usersTotalCount",
	loadingKey: "usersLoading",
	onHydrate: (store, entity, expand) => {
		hydrateUser(store, entity.id_user, entity, expand);
	},
});

const projetCommentaireResource = createNestedResource({
	path: (idUser) => `/user/${idUser}/project_comment`,
	idField: "id_project_comment",
	stateKey: "projetsCommentaire",
	countKey: "projetsCommentaireTotalCount",
	loadingKey: "projetsCommentaireLoading",
	onHydrate: (store, idUser, entity, expand) => {
		if (expand.includes("project")) {
			const projetStore = useProjectsStore();
			projetStore.projects[entity.project.id_project] = entity.project;
		}
	},
});
const commandCommentaireResource = createNestedResource({
	path: (idUser) => `/user/${idUser}/command_comment`,
	idField: "id_command_comment",
	stateKey: "commandsCommentaire",
	countKey: "commandsCommentaireTotalCount",
	loadingKey: "commandsCommentaireLoading",
	onHydrate: (store, idUser, entity, expand) => {
		if (expand.includes("command")) {
			const commandStore = useCommandsStore();
			commandStore.commands[entity.command.id_command] = entity.command;
		}
	},
});
const tokenResource = createNestedResource({
	path: (idUser) => `/user/${idUser}/sessions`,
	idField: "session_id",
	stateKey: "tokens",
	countKey: "tokensTotalCount",
	loadingKey: "tokensLoading",
});
const pushSubscriptionResource = createNestedResource({
	path: (idUser) => `/user/${idUser}/push-subscriptions`,
	idField: "id_push_subscription",
	stateKey: "pushSubscriptions",
	countKey: "pushSubscriptionsTotalCount",
	loadingKey: "pushSubscriptionsLoading",
});

export const useUsersStore = defineStore("users",{
	state: () => ({
		usersLoading: false,
		usersTotalCount: 0,
		users: {},
		userEdition: {},

		projetsCommentaireLoading: false,
		projetsCommentaireTotalCount: {},
		projetsCommentaire: {},
		projetCommentaireEdition: {},

		commandsCommentaireLoading: false,
		commandsCommentaireTotalCount: {},
		commandsCommentaire: {},
		commandCommentaireEdition: {},

		tokensLoading: false,
		tokensTotalCount: {},
		tokens: {},
		tokensEdition: {},

		pushSubscriptionsLoading: false,
		pushSubscriptionsTotalCount: {},
		pushSubscriptions: {},
	}),
	actions: {
		getUserByList: userResource.getByList,
		getUserByInterval: userResource.getByInterval,
		getUserById: userResource.getById,
		createUser: userResource.create,
		updateUser: userResource.update,
		deleteUser: userResource.remove,
		loadToEdition(id, preset = null) {
			this.userEdition[id] = {};
			if (preset) {
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.userEdition[id][key] = value;
					}
				});
			}
			if (id !== "new" && this.users[id]) {
				this.userEdition[id] = {
					loading: false,
					id_user: this.users[id].id_user,
					name_user: this.users[id].name_user,
					firstname_user: this.users[id].firstname_user,
					email_user: this.users[id].email_user,
					role_user: this.users[id].role_user,
					current_password_user: "",
					password_user: "",
					confirm_mdp_user: "",
				};
			} else {
				this.userEdition[id] = {
					loading: false,
				};
			}
			this.projetCommentaireEdition[id] = {};
			this.commandCommentaireEdition[id] = {};
			this.tokensEdition[id] = {};
			this.pushSubscriptionsEdition[id] = {};
		},
		setLoadingEdition(id, loading) {
			if (!this.userEdition[id]) {
				this.userEdition[id] = {};
			}
			this.userEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.userEdition[id];
			delete this.projetCommentaireEdition[id];
			delete this.commandCommentaireEdition[id];
			delete this.tokensEdition[id];
			delete this.pushSubscriptionsEdition[id];
		},

		getProjetCommentaireByInterval: projetCommentaireResource.getByInterval,
		getProjetCommentaireById: projetCommentaireResource.getById,
		createProjetCommentaire: projetCommentaireResource.create,
		updateProjetCommentaire: projetCommentaireResource.update,
		deleteProjetCommentaire: projetCommentaireResource.remove,

		getCommandCommentaireByInterval: commandCommentaireResource.getByInterval,
		getCommandCommentaireById: commandCommentaireResource.getById,
		createCommandCommentaire: commandCommentaireResource.create,
		updateCommandCommentaire: commandCommentaireResource.update,
		deleteCommandCommentaire: commandCommentaireResource.remove,

		getTokenByInterval: tokenResource.getByInterval,
		getTokenById: tokenResource.getById,
		updateToken: tokenResource.update,
		
		getPushSubscriptionsByInterval: pushSubscriptionResource.getByInterval,
		createPushSubscription: pushSubscriptionResource.create,
		deletePushSubscription: pushSubscriptionResource.remove,
		async sendTestPushNotification(idUser) {
			await fetchWrapper.post({
				url: `${baseUrl}/user/${idUser}/push-subscriptions/testPush`,
				useToken: "access",
			});
		},
		async sendTestEmailNotification(idUser) {
			await fetchWrapper.post({
				url: `${baseUrl}/user/${idUser}/push-subscriptions/testEmail`,
				useToken: "access",
			});
		},
	},
});

import { defineStore } from "pinia";

import { fetchWrapper, buildQuery } from "@/helpers";

import { useCommandsStore, useProjetsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
	projets_commentaires: (store, idUser, user) => {
		store.projetsCommentaire[idUser] = {};
		for (const projetCommentaire of user.projets_commentaires) {
			store.projetsCommentaire[idUser][projetCommentaire.id_projet] = projetCommentaire;
		}
	},
	commands_commentaires: (store, idUser, user) => {
		store.commandsCommentaire[idUser] = {};
		for (const commandCommentaire of user.commands_commentaires) {
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
	store.users[idUser] = user;
	store.projetsCommentaireTotalCount[idUser] = user.projets_commentaires_count;
	store.commandsCommentaireTotalCount[idUser] = user.commands_commentaires_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idUser, user);
		}
	}
}

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
		async getUserByList(idResearch = [], expand = []) {
			this.usersLoading = true;
			const paramString = buildQuery({ idResearch, expand });
			const newUserList = await fetchWrapper.get({
				url: `${baseUrl}/user?${paramString}`,
				useToken: "access",
			});
			for (const user of newUserList["data"]) {
				hydrateUser(this, user.id_user, user, expand);
			}
			this.usersLoading = false;
		},
		async getUserByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.usersLoading = true;
			if (clear) {
				this.users = {};
			}
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newUserList = await fetchWrapper.get({
				url: `${baseUrl}/user?${paramString}`,
				useToken: "access",
			});
			for (const user of newUserList["data"]) {
				hydrateUser(this, user.id_user, user, expand);
			}
			this.usersTotalCount = newUserList["pagination"]?.["total"] || 0;
			this.usersLoading = false;
			return [newUserList["pagination"]?.["nextOffset"] || 0, newUserList["pagination"]?.["hasMore"] || false];
		},
		async getUserById(id, expand = []) {
			if (!this.users[id]) {
				this.users[id] = {};
			}
			this.users[id].loading = true;
			const paramString = buildQuery({ expand });
			const user = await fetchWrapper.get({
				url: `${baseUrl}/user/${id}?${paramString}`,
				useToken: "access",
			});
			hydrateUser(this, user.id_user, user, expand);
		},
		async createUser(params) {
			const user = await fetchWrapper.post({
				url: `${baseUrl}/user`,
				useToken: "access",
				body: params,
			});
			this.users[user.id_user] = user;
			return user.id_user;
		},
		async updateUser(id, params) {
			if (params.mdp_user === "" || params.mdp_user === null) {
				delete params.mdp_user;
				delete params.confirm_mdp_user;
			}
			this.users[id] = await fetchWrapper.put({
				url: `${baseUrl}/user/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteUser(id) {
			await fetchWrapper.delete({
				url: `${baseUrl}/user/${id}`,
				useToken: "access",
			});
			delete this.users[id];
		},

		async getProjetCommentaireByInterval(idUser, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.projetsCommentaire[idUser] || clear) {
				this.projetsCommentaire[idUser] = {};
			}
			this.projetsCommentaireLoading = true;
			const projetStore = useProjetsStore();
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newProjetCommentaireList = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/projet_commentaire?${paramString}`,
				useToken: "access",
			});
			for (const projetCommentaire of newProjetCommentaireList["data"]) {
				this.projetsCommentaire[idUser][projetCommentaire.id_projet_commentaire] = projetCommentaire;
				if (expand.includes("projet")) {
					projetStore.projets[projetCommentaire.projet.id_projet] = projetCommentaire.projet;
				}
			}
			this.projetsCommentaireTotalCount[idUser] = newProjetCommentaireList["pagination"]?.["total"] || 0;
			this.projetsCommentaireLoading = false;
			return [newProjetCommentaireList["pagination"]?.["nextOffset"] || 0, newProjetCommentaireList["pagination"]?.["hasMore"] || false];
		},
		async getProjetCommentaireById(idUser, id, expand = []) {
			if (!this.projetsCommentaire[idUser]) {
				this.projetsCommentaire[idUser] = {};
			}
			if (!this.projetsCommentaire[idUser][id]) {
				this.projetsCommentaire[idUser][id] = {};
			}
			this.projetsCommentaire[idUser][id].loading = true;
			const projetStore = useProjetsStore();
			const paramString = buildQuery({ expand });
			this.projetsCommentaire[idUser][id] = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/projet_commentaire/${id}?${paramString}`,
				useToken: "access",
			});
			if (expand.includes("projet")) {
				projetStore.projets[this.projetsCommentaire[idUser][id].id_projet] = this.projetsCommentaire[idUser][id].projet;
			}
		},
		async createProjetCommentaire(idUser, params) {
			if (!this.projetsCommentaire[idUser]) {
				this.projetsCommentaire[idUser] = {};
			}
			const projetCommentaire = await fetchWrapper.post({
				url: `${baseUrl}/user/${idUser}/projet_commentaire`,
				useToken: "access",
				body: params,
			});
			this.projetsCommentaire[idUser][projetCommentaire.id_projet_commentaire] = projetCommentaire;
		},
		async updateProjetCommentaire(idUser, id, params) {
			if (!this.projetsCommentaire[idUser]) {
				this.projetsCommentaire[idUser] = {};
			}
			this.projetsCommentaire[idUser][id] = await fetchWrapper.put({
				url: `${baseUrl}/user/${idUser}/projet_commentaire/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteProjetCommentaire(idUser, id) {
			if (!this.projetsCommentaire[idUser]) {
				this.projetsCommentaire[idUser] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/user/${idUser}/projet_commentaire/${id}`,
				useToken: "access",
			});
			delete this.projetsCommentaire[idUser][id];
		},

		async getCommandCommentaireByInterval(idUser, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.commandsCommentaire[idUser] || clear) {
				this.commandsCommentaire[idUser] = {};
			}
			this.commandsCommentaireLoading = true;
			const commandStore = useCommandsStore();
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newCommandCommentaireList = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/command_commentaire?${paramString}`,
				useToken: "access",
			});
			for (const commandCommentaire of newCommandCommentaireList["data"]) {
				this.commandsCommentaire[idUser][commandCommentaire.id_command_commentaire] = commandCommentaire;
				if (expand.includes("command")) {
					commandStore.commands[commandCommentaire.command.id_command] = commandCommentaire.command;
				}
			}
			this.commandsCommentaireTotalCount[idUser] = newCommandCommentaireList["pagination"]?.["total"] || 0;
			this.commandsCommentaireLoading = false;
			return [newCommandCommentaireList["pagination"]?.["nextOffset"] || 0, newCommandCommentaireList["pagination"]?.["hasMore"] || false];
		},
		async getCommandCommentaireById(idUser, id, expand = []) {
			if (!this.commandsCommentaire[idUser]) {
				this.commandsCommentaire[idUser] = {};
			}
			if (!this.commandsCommentaire[idUser][id]) {
				this.commandsCommentaire[idUser][id] = {};
			}
			this.commandsCommentaire[idUser][id].loading = true;
			const commandStore = useCommandsStore();
			const paramString = buildQuery({ expand });
			this.commandsCommentaire[idUser][id] = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/command_commentaire/${id}?${paramString}`,
				useToken: "access",
			});
			if (expand.includes("command")) {
				commandStore.commands[this.commandsCommentaire[idUser][id].id_command] = this.commandsCommentaire[idUser][id].command;
			}
		},
		async createCommandCommentaire(idUser, params) {
			if (!this.commandsCommentaire[idUser]) {
				this.commandsCommentaire[idUser] = {};
			}
			const commandCommentaire = await fetchWrapper.post({
				url: `${baseUrl}/user/${idUser}/command_commentaire`,
				useToken: "access",
				body: params,
			});
			this.commandsCommentaire[idUser][commandCommentaire.id_command_commentaire] = commandCommentaire;
		},
		async updateCommandCommentaire(idUser, id, params) {
			if (!this.commandsCommentaire[idUser]) {
				this.commandsCommentaire[idUser] = {};
			}
			this.commandsCommentaire[idUser][id] = await fetchWrapper.put({
				url: `${baseUrl}/user/${idUser}/command_commentaire/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteCommandCommentaire(idUser, id) {
			if (!this.commandsCommentaire[idUser]) {
				this.commandsCommentaire[idUser] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/user/${idUser}/command_commentaire/${id}`,
				useToken: "access",
			});
			delete this.commandsCommentaire[idUser][id];
		},

		async getTokenByInterval(idUser, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.tokens[idUser] || clear) {
				this.tokens[idUser] = {};
			}
			this.tokensLoading = true;
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newTokenList = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/sessions?${paramString}`,
				useToken: "access",
			});
			for (const token of newTokenList["data"]) {
				this.tokens[idUser][token.session_id] = token;
			}
			this.tokensTotalCount[idUser] = newTokenList["pagination"]?.["total"] || 0;
			this.tokensLoading = false;
			return [newTokenList["pagination"]?.["nextOffset"] || 0, newTokenList["pagination"]?.["hasMore"] || false];
		},
		async getTokenById(idUser, id) {
			if (!this.tokens[idUser]) {
				this.tokens[idUser] = {};
			}
			if (!this.tokens[idUser][id]) {
				this.tokens[idUser][id] = {};
			}
			this.tokens[idUser][id].loading = true;
			this.tokens[idUser][id] = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/sessions/${id}`,
				useToken: "access",
			});
		},
		async updateToken(idUser, id, params) {
			if (!this.tokens[idUser]) {
				this.tokens[idUser] = {};
			}
			this.tokens[idUser][id] = await fetchWrapper.put({
				url: `${baseUrl}/user/${idUser}/sessions/${id}`,
				useToken: "access",
				body: params,
			});
		},

		async getPushSubscriptionsByInterval(idUser, limit = 100, offset = 0, clear = false) {
			if (!this.pushSubscriptions[idUser] || clear) {
				this.pushSubscriptions[idUser] = {};
			}
			this.pushSubscriptionsLoading = true;
			const paramString = buildQuery({ limit, offset });
			const result = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/push-subscriptions?${paramString}`,
				useToken: "access",
			});
			for (const sub of result["data"]) {
				this.pushSubscriptions[idUser][sub.id_push_subscription] = sub;
			}
			this.pushSubscriptionsTotalCount[idUser] = result["pagination"]?.["total"] || 0;
			this.pushSubscriptionsLoading = false;
			return [result["pagination"]?.["nextOffset"] || 0, result["pagination"]?.["hasMore"] || false];
		},
		async createPushSubscription(idUser, params) {
			if (!this.pushSubscriptions[idUser]) {
				this.pushSubscriptions[idUser] = {};
			}
			const sub = await fetchWrapper.post({
				url: `${baseUrl}/user/${idUser}/push-subscriptions`,
				useToken: "access",
				body: params,
			});
			this.pushSubscriptions[idUser][sub.id_push_subscription] = sub;
			return sub;
		},
		async deletePushSubscription(idUser, id) {
			if (!this.pushSubscriptions[idUser]) {
				this.pushSubscriptions[idUser] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/user/${idUser}/push-subscriptions/${id}`,
				useToken: "access",
			});
			delete this.pushSubscriptions[idUser][id];
		},
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

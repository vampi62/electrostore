import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

import { useCommandsStore, useProjetsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useUsersStore = defineStore("users",{
	state: () => ({
		usersLoading: true,
		usersTotalCount: 0,
		users: {},
		userEdition: {},

		projetsCommentaireLoading: true,
		projetsCommentaireTotalCount: {},
		projetsCommentaire: {},
		projetCommentaireEdition: {},

		commandsCommentaireLoading: true,
		commandsCommentaireTotalCount: {},
		commandsCommentaire: {},
		commandCommentaireEdition: {},

		tokensLoading: true,
		tokensTotalCount: {},
		tokens: {},
		tokensEdition: {},
	}),
	actions: {
		async getUserByList(idResearch = [], expand = []) {
			this.usersLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const paramString = [idResearchString, expandString].join("&");
			const newUserList = await fetchWrapper.get({
				url: `${baseUrl}/user?${paramString}`,
				useToken: "access",
			});
			for (const user of newUserList["data"]) {
				this.users[user.id_user] = user;
				this.projetsCommentaireTotalCount[user.id_user] = user.projets_commentaires_count;
				this.commandsCommentaireTotalCount[user.id_user] = user.commands_commentaires_count;
				if (expand.includes("projets_commentaires")) {
					this.projetsCommentaire[user.id_user] = {};
					for (const projet of user.projets_commentaires) {
						this.projetsCommentaire[user.id_user][projet.id_projet] = projet;
					}
				}
				if (expand.includes("commands_commentaires")) {
					this.commandsCommentaire[user.id_user] = {};
					for (const command of user.commands_commentaires) {
						this.commandsCommentaire[user.id_user][command.id_command] = command;
					}
				}
			}
			this.usersLoading = false;
		},
		async getUserByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.usersLoading = true;
			if (clear) {
				this.users = {};
			}
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newUserList = await fetchWrapper.get({
				url: `${baseUrl}/user?${paramString}`,
				useToken: "access",
			});
			for (const user of newUserList["data"]) {
				this.users[user.id_user] = user;
				this.projetsCommentaireTotalCount[user.id_user] = user.projets_commentaires_count;
				this.commandsCommentaireTotalCount[user.id_user] = user.commands_commentaires_count;
				if (expand.includes("projets_commentaires")) {
					this.projetsCommentaire[user.id_user] = {};
					for (const projet of user.projets_commentaires) {
						this.projetsCommentaire[user.id_user][projet.id_projet] = projet;
					}
				}
				if (expand.includes("commands_commentaires")) {
					this.commandsCommentaire[user.id_user] = {};
					for (const command of user.commands_commentaires) {
						this.commandsCommentaire[user.id_user][command.id_command] = command;
					}
				}
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
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.users[id] = await fetchWrapper.get({
				url: `${baseUrl}/user/${id}?${expandString}`,
				useToken: "access",
			});
			this.projetsCommentaireTotalCount[id] = this.users[id].projets_commentaires_count;
			this.commandsCommentaireTotalCount[id] = this.users[id].commands_commentaires_count;
			if (expand.includes("projets_commentaires")) {
				this.projetsCommentaire[id] = {};
				for (const projet of this.users[id].projets_commentaires) {
					this.projetsCommentaire[id][projet.id_projet] = projet;
				}
			}
			if (expand.includes("commands_commentaires")) {
				this.commandsCommentaire[id] = {};
				for (const command of this.users[id].commands_commentaires) {
					this.commandsCommentaire[id][command.id_command] = command;
				}
			}
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
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
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
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.projetsCommentaire[idUser][id] = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/projet_commentaire/${id}?${expandString}`,
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
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
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
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.commandsCommentaire[idUser][id] = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/command_commentaire/${id}?${expandString}`,
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
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
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
	},
});

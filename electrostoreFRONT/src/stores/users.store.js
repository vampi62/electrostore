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
			let newUserList = await fetchWrapper.get({
				url: `${baseUrl}/user?${idResearchString}&${expandString}`,
				useToken: "access",
			});
			for (const user of newUserList["data"]) {
				this.users[user.id_user] = user;
				this.projetsCommentaireTotalCount[user.id_user] = user.projets_commentaires_count;
				this.commandsCommentaireTotalCount[user.id_user] = user.commands_commentaires_count;
				if (expand.indexOf("projets_commentaires") > -1) {
					this.projetsCommentaire[user.id_user] = {};
					for (const projet of user.projets_commentaires) {
						this.projetsCommentaire[user.id_user][projet.id_projet] = projet;
					}
				}
				if (expand.indexOf("commands_commentaires") > -1) {
					this.commandsCommentaire[user.id_user] = {};
					for (const command of user.commands_commentaires) {
						this.commandsCommentaire[user.id_user][command.id_command] = command;
					}
				}
			}
			this.usersTotalCount = newUserList["count"];
			this.usersLoading = false;
		},
		async getUserByInterval(limit = 100, offset = 0, expand = []) {
			this.usersLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newUserList = await fetchWrapper.get({
				url: `${baseUrl}/user?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const user of newUserList["data"]) {
				this.users[user.id_user] = user;
				this.projetsCommentaireTotalCount[user.id_user] = user.projets_commentaires_count;
				this.commandsCommentaireTotalCount[user.id_user] = user.commands_commentaires_count;
				if (expand.indexOf("projets_commentaires") > -1) {
					this.projetsCommentaire[user.id_user] = {};
					for (const projet of user.projets_commentaires) {
						this.projetsCommentaire[user.id_user][projet.id_projet] = projet;
					}
				}
				if (expand.indexOf("commands_commentaires") > -1) {
					this.commandsCommentaire[user.id_user] = {};
					for (const command of user.commands_commentaires) {
						this.commandsCommentaire[user.id_user][command.id_command] = command;
					}
				}
			}
			this.usersTotalCount = newUserList["count"];
			this.usersLoading = false;
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
			if (expand.indexOf("projets_commentaires") > -1) {
				this.projetsCommentaire[id] = {};
				for (const projet of this.users[id].projets_commentaires) {
					this.projetsCommentaire[id][projet.id_projet] = projet;
				}
			}
			if (expand.indexOf("commands_commentaires") > -1) {
				this.commandsCommentaire[id] = {};
				for (const command of this.users[id].commands_commentaires) {
					this.commandsCommentaire[id][command.id_command] = command;
				}
			}
		},
		async createUser(params) {
			this.userEdition.loading = true;
			this.userEdition = await fetchWrapper.post({
				url: `${baseUrl}/user`,
				useToken: "access",
				body: params,
			});
			this.users[this.userEdition.id_user] = this.userEdition;
		},
		async updateUser(id, params) {
			this.userEdition.loading = true;
			if (params.mdp_user === "" || params.mdp_user === null) {
				delete params.mdp_user;
				delete params.confirm_mdp_user;
			}
			this.userEdition = await fetchWrapper.put({
				url: `${baseUrl}/user/${id}`,
				useToken: "access",
				body: params,
			});
			this.users[id] = this.userEdition;
		},
		async deleteUser(id) {
			this.userEdition.loading = true;
			this.userEdition = await fetchWrapper.delete({
				url: `${baseUrl}/user/${id}`,
				useToken: "access",
			});
			delete this.users[id];
		},

		async getProjetCommentaireByInterval(idUser, limit = 100, offset = 0, expand = []) {
			// init store
			const projetStore = useProjetsStore();
			// init list if not exist
			if (!this.projetsCommentaire[idUser]) {
				this.projetsCommentaire[idUser] = {};
			}
			// query
			this.projetsCommentaireLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newProjetCommentaireList = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/projet_commentaire?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const projetCommentaire of newProjetCommentaireList["data"]) {
				this.projetsCommentaire[idUser][projetCommentaire.id_projet_commentaire] = projetCommentaire;
				if (expand.indexOf("projet") > -1) {
					projetStore.projets[projetCommentaire.projet.id_projet] = projetCommentaire.projet;
				}
			}
			this.projetsCommentaireTotalCount[idUser] = newProjetCommentaireList["count"];
			this.projetsCommentaireLoading = false;
		},
		async getProjetCommentaireById(idUser, id, expand = []) {
			// init store
			const projetStore = useProjetsStore();
			// init list if not exist
			if (!this.projetsCommentaire[idUser]) {
				this.projetsCommentaire[idUser] = {};
			}
			if (!this.projetsCommentaire[idUser][id]) {
				this.projetsCommentaire[idUser][id] = {};
			}
			// query
			this.projetsCommentaire[idUser][id].loading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.projetsCommentaire[idUser][id] = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/projet_commentaire/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.indexOf("projet") > -1) {
				projetStore.projets[this.projetsCommentaire[idUser][id].id_projet] = this.projetsCommentaire[idUser][id].projet;
			}
		},
		async createProjetCommentaire(idUser, params) {
			this.projetCommentaireEdition.loading = true;
			this.projetCommentaireEdition = await fetchWrapper.post({
				url: `${baseUrl}/user/${idUser}/projet_commentaire`,
				useToken: "access",
				body: params,
			});
			if (!this.projetsCommentaire[idUser]) {
				this.projetsCommentaire[idUser] = {};
			}
			this.projetsCommentaire[idUser][this.projetCommentaireEdition.id_projet_commentaire] = this.projetCommentaireEdition;
		},
		async updateProjetCommentaire(idUser, id, params) {
			this.projetCommentaireEdition.loading = true;
			this.projetCommentaireEdition = await fetchWrapper.put({
				url: `${baseUrl}/user/${idUser}/projet_commentaire/${id}`,
				useToken: "access",
				body: params,
			});
			this.projetsCommentaire[idUser][id] = this.projetCommentaireEdition;
		},
		async deleteProjetCommentaire(idUser, id) {
			this.projetCommentaireEdition.loading = true;
			this.projetCommentaireEdition = await fetchWrapper.delete({
				url: `${baseUrl}/user/${idUser}/projet_commentaire/${id}`,
				useToken: "access",
			});
			delete this.projetsCommentaire[idUser][id];
		},

		async getCommandCommentaireByInterval(idUser, limit = 100, offset = 0, expand = []) {
			// init store
			const commandStore = useCommandsStore();
			// init list if not exist
			if (!this.commandsCommentaire[idUser]) {
				this.commandsCommentaire[idUser] = {};
			}
			// query
			this.commandsCommentaireLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newCommandCommentaireList = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/command_commentaire?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const commandCommentaire of newCommandCommentaireList["data"]) {
				this.commandsCommentaire[idUser][commandCommentaire.id_command_commentaire] = commandCommentaire;
				if (expand.indexOf("command") > -1) {
					commandStore.commands[commandCommentaire.command.id_command] = commandCommentaire.command;
				}
			}
			this.commandsCommentaireTotalCount[idUser] = newCommandCommentaireList["count"];
			this.commandsCommentaireLoading = false;
		},
		async getCommandCommentaireById(idUser, id, expand = []) {
			// init store
			const commandStore = useCommandsStore();
			// init list if not exist
			if (!this.commandsCommentaire[idUser]) {
				this.commandsCommentaire[idUser] = {};
			}
			if (!this.commandsCommentaire[idUser][id]) {
				this.commandsCommentaire[idUser][id] = {};
			}
			// query
			this.commandsCommentaire[idUser][id].loading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.commandsCommentaire[idUser][id] = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/command_commentaire/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.indexOf("command") > -1) {
				commandStore.commands[this.commandsCommentaire[idUser][id].id_command] = this.commandsCommentaire[idUser][id].command;
			}
		},
		async createCommandCommentaire(idUser, params) {
			this.commandCommentaireEdition.loading = true;
			this.commandCommentaireEdition = await fetchWrapper.post({
				url: `${baseUrl}/user/${idUser}/command_commentaire`,
				useToken: "access",
				body: params,
			});
			if (!this.commandsCommentaire[idUser]) {
				this.commandsCommentaire[idUser] = {};
			}
			this.commandsCommentaire[idUser][this.commandCommentaireEdition.id_command_commentaire] = this.commandCommentaireEdition;
		},
		async updateCommandCommentaire(idUser, id, params) {
			this.commandCommentaireEdition.loading = true;
			this.commandCommentaireEdition = await fetchWrapper.put({
				url: `${baseUrl}/user/${idUser}/command_commentaire/${id}`,
				useToken: "access",
				body: params,
			});
			this.commandsCommentaire[idUser][id] = this.commandCommentaireEdition;
		},
		async deleteCommandCommentaire(idUser, id) {
			this.commandCommentaireEdition.loading = true;
			this.commandCommentaireEdition = await fetchWrapper.delete({
				url: `${baseUrl}/user/${idUser}/command_commentaire/${id}`,
				useToken: "access",
			});
			delete this.commandsCommentaire[idUser][id];
		},

		async getTokenByInterval(idUser, limit = 100, offset = 0) {
			this.tokensLoading = true;
			let newTokenList = await fetchWrapper.get({
				url: `${baseUrl}/user/${idUser}/token?limit=${limit}&offset=${offset}`,
				useToken: "access",
			});
			if (!this.tokens[idUser]) {
				this.tokens[idUser] = {};
			}
			for (const token of newTokenList["data"]) {
				this.tokens[idUser][token.id_jwi_refresh] = token;
			}
			this.tokensTotalCount[idUser] = newTokenList["count"];
			this.tokensLoading = false;
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
				url: `${baseUrl}/user/${idUser}/token/${id}`,
				useToken: "access",
			});
		},
		async updateToken(idUser, id, params) {
			this.tokensEdition.loading = true;
			this.tokensEdition = await fetchWrapper.put({
				url: `${baseUrl}/user/${idUser}/token/${id}`,
				useToken: "access",
				body: params,
			});
		},
	},
});

import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

import { useCommandsStore, useProjetsStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useUsersStore = defineStore('users',{
    state: () => ({
        usersLoading: true,
        usersTotalCount: 0,
        users: {},
        userEdition: {},

        projetsCommentaireLoading: true,
        projetsCommentaireTotalCount: {},
        projetsCommentaire: {},
        projectCommentaireEdition: {},

        commandsCommentaireLoading: true,
        commandsCommentaireTotalCount: {},
        commandsCommentaire: {},
        commandCommentaireEdition: {},

        tokensLoading: true,
        tokensTotalCount: {},
        tokens: {},
        tokensEdition: {}
    }),
    actions: {
        async getUserByList(idResearch = [], expand = []) {
            this.usersLoading = true;
            const idResearchString = idResearch.join(',');
            const expandString = expand.join(',');
            let newUserList = await fetchWrapper.get({
                url: `${baseUrl}/user?&idResearch=${idResearchString}&expand=${expandString}`,
                useToken: "access"
            });
            for (const user of newUserList['data']) {
                this.users[user.id_user] = user;
                this.projetsCommentaireTotalCount[user.id_user] = user.projets_commentaires_count;
                this.commandsCommentaireTotalCount[user.id_user] = user.commands_commentaires_count;
                if (expand.indexOf("projets_commentaires") > -1) {
                    this.projetsCommentaire[user.id_user] = {};
                    for (const projet of user.projets) {
                        this.projetsCommentaire[user.id_user][projet.id_projet] = projet;
                    }
                }
                if (expand.indexOf("commands_commentaires") > -1) {
                    this.commandsCommentaire[user.id_user] = {};
                    for (const command of user.commands) {
                        this.commandsCommentaire[user.id_user][command.id_command] = command;
                    }
                }
            }
            this.usersTotalCount = newUserList['count'];
            this.usersLoading = false;
        },
        async getUserByInterval(limit = 100, offset = 0, expand = []) {
            this.usersLoading = true;
            const expandString = expand.join(',');
            let newUserList = await fetchWrapper.get({
                url: `${baseUrl}/user?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const user of newUserList['data']) {
                this.users[user.id_user] = user;
                this.projetsCommentaireTotalCount[user.id_user] = user.projets_commentaires_count;
                this.commandsCommentaireTotalCount[user.id_user] = user.commands_commentaires_count;
                if (expand.indexOf("projets_commentaires") > -1) {
                    this.projetsCommentaire[user.id_user] = {};
                    for (const projet of user.projets) {
                        this.projetsCommentaire[user.id_user][projet.id_projet] = projet;
                    }
                }
                if (expand.indexOf("commands_commentaires") > -1) {
                    this.commandsCommentaire[user.id_user] = {};
                    for (const command of user.commands) {
                        this.commandsCommentaire[user.id_user][command.id_command] = command;
                    }
                }
            }
            this.usersTotalCount = newUserList['count'];
            this.usersLoading = false;
        },
        async getUserById(id, expand = []) {
            this.users[id] = { loading: true };
            const expandString = expand.join(',');
            this.users[id] = await fetchWrapper.get({
                url: `${baseUrl}/user/${id}?expand=${expandString}`,
                useToken: "access"
            });
            this.projetsCommentaireTotalCount[id] = this.users[id].projets_commentaires_count;
            this.commandsCommentaireTotalCount[id] = this.users[id].commands_commentaires_count;
            if (expand.indexOf("projets_commentaires") > -1) {
                this.projetsCommentaire[id] = {};
                for (const projet of this.users[id].projets) {
                    this.projetsCommentaire[id][projet.id_projet] = projet;
                }
            }
            if (expand.indexOf("commands_commentaires") > -1) {
                this.commandsCommentaire[id] = {};
                for (const command of this.users[id].commands) {
                    this.commandsCommentaire[id][command.id_command] = command;
                }
            }
        },
        async createUser(params) {
            this.userEdition.loading = true;
            this.userEdition = await fetchWrapper.post({
                url: `${baseUrl}/user`,
                useToken: "access",
                body: params
            });
            this.users[this.userEdition.id_user] = this.userEdition;
        },
        async updateUser(id, params) {
            this.userEdition.loading = true;
            this.userEdition = await fetchWrapper.put({
                url: `${baseUrl}/user/${id}`,
                useToken: "access",
                body: params
            });
            this.users[id] = this.userEdition;
        },
        async deleteUser(id) {
            this.userEdition.loading = true;
            this.userEdition = await fetchWrapper.delete({
                url: `${baseUrl}/user/${id}`,
                useToken: "access"
            });
            delete this.users[id];
        },

        async getProjectCommentaireByInterval(idUser, limit = 100, offset = 0, expand = []) {
            // init store
            const projetStore = useProjetsStore();
            // init list if not exist
            if (!this.projetsCommentaire[idUser]) {
                this.projetsCommentaire[idUser] = {};
            }
            // query
            this.projetsCommentaireLoading = true;
            const expandString = expand.join(',');
            let newProjectCommentaireList = await fetchWrapper.get({
                url: `${baseUrl}/user/${idUser}/projetcommentaire?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const projectCommentaire of newProjectCommentaireList['data']) {
                this.projetsCommentaire[idUser][projectCommentaire.id_projet_commentaire] = projectCommentaire;
                if (expand.indexOf("projet") > -1) {
                    projetStore.projets[projectCommentaire.id_projet] = projectCommentaire.projets;
                }
            }
            this.projetsCommentaireTotalCount[idUser] = newProjectCommentaireList['count'];
            this.projetsCommentaireLoading = false;
        },
        async getProjectCommentaireById(idUser, id, expand = []) {
            // init store
            const projetStore = useProjetsStore();
            // init list if not exist
            if (!this.projetsCommentaire[idUser]) {
                this.projetsCommentaire[idUser] = {};
            }
            // query
            this.projetsCommentaire[idUser][id] = { loading: true };
            const expandString = expand.join(',');
            this.projetsCommentaire[idUser][id] = await fetchWrapper.get({
                url: `${baseUrl}/user/${idUser}/projetcommentaire/${id}?expand=${expandString}`,
                useToken: "access"
            });
            if (expand.indexOf("projet") > -1) {
                projetStore.projets[this.projetsCommentaire[idUser][id].id_projet] = this.projetsCommentaire[idUser][id].projets;
            }
        },
        async createProjectCommentaire(idUser, params) {
            this.projectCommentaireEdition = { loading: true };
            this.projectCommentaireEdition = await fetchWrapper.post({
                url: `${baseUrl}/user/${idUser}/projetcommentaire`,
                useToken: "access",
                body: params
            });
            if (!this.projetsCommentaire[idUser]) {
                this.projetsCommentaire[idUser] = {};
            }
            this.projetsCommentaire[idUser][this.projectCommentaireEdition.id_projet_commentaire] = this.projectCommentaireEdition;
        },
        async updateProjectCommentaire(idUser, id, params) {
            this.projectCommentaireEdition = { loading: true };
            this.projectCommentaireEdition = await fetchWrapper.put({
                url: `${baseUrl}/user/${idUser}/projetcommentaire/${id}`,
                useToken: "access",
                body: params
            });
            this.projetsCommentaire[idUser][id] = this.projectCommentaireEdition;
        },
        async deleteProjectCommentaire(idUser, id) {
            this.projectCommentaireEdition = { loading: true };
            this.projectCommentaireEdition = await fetchWrapper.delete({
                url: `${baseUrl}/user/${idUser}/projetcommentaire/${id}`,
                useToken: "access"
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
            const expandString = expand.join(',');
            let newCommandCommentaireList = await fetchWrapper.get({
                url: `${baseUrl}/user/${idUser}/commandcommentaire?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const commandCommentaire of newCommandCommentaireList['data']) {
                this.commandsCommentaire[idUser][commandCommentaire.id_command_commentaire] = commandCommentaire;
                if (expand.indexOf("command") > -1) {
                    commandStore.commands[commandCommentaire.id_command] = commandCommentaire.commands;
                }
            }
            this.commandsCommentaireTotalCount[idUser] = newCommandCommentaireList['count'];
            this.commandsCommentaireLoading = false;
        },
        async getCommandCommentaireById(idUser, id, expand = []) {
            // init store
            const commandStore = useCommandsStore();
            // init list if not exist
            if (!this.commandsCommentaire[idUser]) {
                this.commandsCommentaire[idUser] = {};
            }
            // query
            this.commandsCommentaire[idUser][id] = { loading: true };
            const expandString = expand.join(',');
            this.commandsCommentaire[idUser][id] = await fetchWrapper.get({
                url: `${baseUrl}/user/${idUser}/commandcommentaire/${id}?expand=${expandString}`,
                useToken: "access"
            });
            if (expand.indexOf("command") > -1) {
                commandStore.commands[this.commandsCommentaire[idUser][id].id_command] = this.commandsCommentaire[idUser][id].commands;
            }
        },
        async createCommandCommentaire(idUser, params) {
            this.commandCommentaireEdition = { loading: true };
            this.commandCommentaireEdition = await fetchWrapper.post({
                url: `${baseUrl}/user/${idUser}/commandcommentaire`,
                useToken: "access",
                body: params
            });
            if (!this.commandsCommentaire[idUser]) {
                this.commandsCommentaire[idUser] = {};
            }
            this.commandsCommentaire[idUser][this.commandCommentaireEdition.id_command_commentaire] = this.commandCommentaireEdition;
        },
        async updateCommandCommentaire(idUser, id, params) {
            this.commandCommentaireEdition = { loading: true };
            this.commandCommentaireEdition = await fetchWrapper.put({
                url: `${baseUrl}/user/${idUser}/commandcommentaire/${id}`,
                useToken: "access",
                body: params
            });
            this.commandsCommentaire[idUser][id] = this.commandCommentaireEdition;
        },
        async deleteCommandCommentaire(idUser, id) {
            this.commandCommentaireEdition = { loading: true };
            this.commandCommentaireEdition = await fetchWrapper.delete({
                url: `${baseUrl}/user/${idUser}/commandcommentaire/${id}`,
                useToken: "access"
            });
            delete this.commandsCommentaire[idUser][id];
        },

        async getTokenByInterval(idUser, limit = 100, offset = 0) {
            this.tokensLoading = true;
            let newTokenList = await fetchWrapper.get({
                url: `${baseUrl}/user/${idUser}/token?limit=${limit}&offset=${offset}`,
                useToken: "access"
            });
            this.tokens[idUser] = {};
            for (const token of newTokenList['data']) {
                this.tokens[idUser][token.id_token] = token;
            }
            this.tokensTotalCount[idUser] = newTokenList['count'];
            this.tokensLoading = false;
        },
        async updateToken(idUser, id, params) {
            this.tokensEdition = { loading: true };
            this.tokensEdition = await fetchWrapper.put({
                url: `${baseUrl}/user/${idUser}/token/${id}`,
                useToken: "access",
                body: params
            });
            this.tokens[idUser][id] = this.tokensEdition;
        }
    }
});

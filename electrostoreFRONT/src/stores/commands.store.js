import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

import { useUsersStore, useItemsStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useCommandsStore = defineStore({
    id: 'commands',
    state: () => ({
        commandsLoading: true,
        commandsTotalCount: 0,
        commands: {},
        commandEdition: {},

        commentairesTotalCount: {},
        commentairesLoading: false,
        commentaires: {},
        commentaireEdition: {},

        documentsTotalCount: {},
        documentsLoading: false,
        documents: {},
        documentEdition: {},

        itemsTotalCount: {},
        itemsLoading: false,
        items: {},
        itemEdition: {}
    }),
    actions: {
        async getCommandByList(idResearch = [], expand = []) {
            this.commandsLoading = true;
            const idResearchString = idResearch.join(',');
            const expandString = expand.join(',');
            let newCommandList = await fetchWrapper.get({
                url: `${baseUrl}/command?&idResearch=${idResearchString}&expand=${expandString}`,
                useToken: "access"
            });
            for (const command of newCommandList['data']) {
                this.commands[command.id_command] = command;
                this.commentairesTotalCount[command.id_command] = command.commands_commentaires_count;
                this.documentsTotalCount[command.id_command] = command.commands_documents_count;
                this.itemsTotalCount[command.id_command] = command.commands_items_count;
                if (expand.indexOf("commands_commentaires") > -1) {
                    this.commentaires[command.id_command] = {};
                    for (const commentaire of command.commands_commentaires) {
                        this.commentaires[command.id_command][commentaire.id_command_commentaire] = commentaire;
                    }
                }
                if (expand.indexOf("commands_documents") > -1) {
                    this.documents[command.id_command] = {};
                    for (const document of command.commands_documents) {
                        this.documents[command.id_command][document.id_command_document] = document;
                    }
                }
                if (expand.indexOf("commands_items") > -1) {
                    this.items[command.id_command] = {};
                    for (const item of command.commands_items) {
                        this.items[command.id_command][item.id_item] = item;
                    }
                }
            }
            this.commandsTotalCount = newCommandList['count'];
            this.commandsLoading = false;
        },
        async getCommandByInterval(limit = 100, offset = 0, expand = []) {
            this.commandsLoading = true;
            const expandString = expand.join(',');
            let newCommandList = await fetchWrapper.get({
                url: `${baseUrl}/command?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const command of newCommandList['data']) {
                this.commands[command.id_command] = command;
                this.commentairesTotalCount[command.id_command] = command.commands_commentaires_count;
                this.documentsTotalCount[command.id_command] = command.commands_documents_count;
                this.itemsTotalCount[command.id_command] = command.commands_items_count;
                if (expand.indexOf("commands_commentaires") > -1) {
                    this.commentaires[command.id_command] = {};
                    for (const commentaire of command.commands_commentaires) {
                        this.commentaires[command.id_command][commentaire.id_command_commentaire] = commentaire;
                    }
                }
                if (expand.indexOf("commands_documents") > -1) {
                    this.documents[command.id_command] = {};
                    for (const document of command.commands_documents) {
                        this.documents[command.id_command][document.id_command_document] = document;
                    }
                }
                if (expand.indexOf("commands_items") > -1) {
                    this.items[command.id_command] = {};
                    for (const item of command.commands_items) {
                        this.items[command.id_command][item.id_item] = item;
                    }
                }
            }
            this.commandsTotalCount = newCommandList['count'];
            this.commandsLoading = false;
        },
        async getCommandById(id, expand = []) {
            this.commands[id] = { loading: true };
            const expandString = expand.join(',');
            this.commands[id] = await fetchWrapper.get({
                url: `${baseUrl}/command/${id}?expand=${expandString}`,
                useToken: "access"
            });
            this.commentairesTotalCount[id] = this.commands[id].commands_commentaires_count;
            this.documentsTotalCount[id] = this.commands[id].commands_documents_count;
            this.itemsTotalCount[id] = this.commands[id].commands_items_count;
            if (expand.indexOf("commands_commentaires") > -1) {
                this.commentaires[id] = {};
                for (const commentaire of this.commands[id].commands_commentaires) {
                    this.commentaires[id][commentaire.id_command_commentaire] = commentaire;
                }
            }
            if (expand.indexOf("commands_documents") > -1) {
                this.documents[id] = {};
                for (const document of this.commands[id].commands_documents) {
                    this.documents[id][document.id_command_document] = document;
                }
            }
            if (expand.indexOf("commands_items") > -1) {
                this.items[id] = {};
                for (const item of this.commands[id].commands_items) {
                    this.items[id][item.id_item] = item;
                }
            }
        },
        async createCommand(params) {
            this.commandEdition.loading = true;
            this.commandEdition = await fetchWrapper.post({
                url: `${baseUrl}/command`,
                useToken: "access",
                body: params
            });
            this.commands[this.commandEdition.id_command] = this.commandEdition;
        },
        async updateCommand(id, params) {
            this.commandEdition.loading = true;
            this.commandEdition = await fetchWrapper.put({
                url: `${baseUrl}/command/${id}`,
                useToken: "access",
                body: params
            });
            this.commands[id] = params;
        },
        async deleteCommand(id) {
            this.commandEdition.loading = true;
            this.commandEdition = await fetchWrapper.delete({
                url: `${baseUrl}/command/${id}`,
                useToken: "access"
            });
            delete this.commands[id];
        },

        async getCommentaireByInterval(idCommand, limit = 100, offset = 0, expand = []) {
            const userStore = useUsersStore();
            if (!this.commentaires[idCommand]) {
                this.commentaires[idCommand] = {};
            }
            this.commentairesLoading = true;
            const expandString = expand.join(',');
            let newCommentaireList = await fetchWrapper.get({
                url: `${baseUrl}/command/${idCommand}/commentaire?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const commentaire of newCommentaireList['data']) {
                this.commentaires[idCommand][commentaire.id_command_commentaire] = commentaire;
                if (expand.indexOf("user") > -1) {
                    userStore.users[commentaire.id_user] = commentaire.user
                }
            }
            this.commentairesTotalCount[idCommand] = newCommentaireList['count'];
            this.commentairesLoading = false;
        },
        async getCommentaireById(idCommand, id, expand = []) {
            const userStore = useUsersStore();
            if (!this.commentaires[idCommand]) {
                this.commentaires[idCommand] = {};
            }
            this.commentaires[idCommand][id] = { loading: true };
            const expandString = expand.join(',');
            this.commentaires[idCommand][id] = await fetchWrapper.get({
                url: `${baseUrl}/command/${idCommand}/commentaire/${id}?expand=${expandString}`,
                useToken: "access"
            });
            if (expand.indexOf("user") > -1) {
                userStore.users[this.commentaires[idCommand][id].id_user] = this.commentaires[idCommand][id].user
            }
        },
        async createCommentaire(idCommand, params) {
            this.commentaireEdition = { loading: true };
            this.commentaireEdition = await fetchWrapper.post({
                url: `${baseUrl}/command/${idCommand}/commentaire`,
                useToken: "access",
                body: params
            });
            if (!this.commentaires[idCommand]) {
                this.commentaires[idCommand] = {};
            }
            this.commentaires[idCommand][this.commentaireEdition.id_command_commentaire] = this.commentaireEdition;
        },
        async updateCommentaire(idCommand, id, params) {
            this.commentaireEdition = { loading: true };
            this.commentaireEdition = await fetchWrapper.put({
                url: `${baseUrl}/command/${idCommand}/commentaire/${id}`,
                useToken: "access",
                body: params
            });
            this.commentaires[idCommand][id] = this.commentaireEdition;
        },
        async deleteCommentaire(idCommand, id) {
            this.commentaireEdition = { loading: true };
            this.commentaireEdition = await fetchWrapper.delete({
                url: `${baseUrl}/command/${idCommand}/commentaire/${id}`,
                useToken: "access"
            });
            delete this.commentaires[idCommand][id];
        },

        async getDocumentByInterval(idCommand, limit = 100, offset = 0, expand = []) {
            this.documentsLoading = true;
            if (!this.documents[idCommand]) {
                this.documents[idCommand] = {};
            }
            const expandString = expand.join(',');
            let newDocumentList = await fetchWrapper.get({
                url: `${baseUrl}/command/${idCommand}/document?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const document of newDocumentList['data']) {
                this.documents[idCommand][document.id_command_document] = document;
            }
            this.documentsTotalCount[idCommand] = newDocumentList['count'];
            this.documentsLoading = false;
        },
        async getDocumentById(idCommand, id, expand = []) {
            if (!this.documents[idCommand]) {
                this.documents[idCommand] = {};
            }
            this.documents[idCommand][id] = { loading: true };
            const expandString = expand.join(',');
            this.documents[idCommand][id] = await fetchWrapper.get({
                url: `${baseUrl}/command/${idCommand}/document/${id}?expand=${expandString}`,
                useToken: "access"
            });
        },
        async createDocument(idCommand, params) {
            this.documentEdition = { loading: true };
            const formData = new FormData();
            formData.append('name_command_document', params.name_command_document);
            formData.append('document', params.document);
            this.documentEdition = await fetchWrapper.post({
                url: `${baseUrl}/command/${idCommand}/document`,
                useToken: "access",
                body: formData,
                contentFile: true
            });
            if (!this.documents[idCommand]) {
                this.documents[idCommand] = {};
            }
            this.documents[idCommand][this.documentEdition.id_command_document] = this.documentEdition;
        },
        async updateDocument(idCommand, id, params) {
            this.documentEdition = { loading: true };
            const formData = new FormData();
            if (params.name_command_document) { formData.append('name_command_document', params.name_command_document) }
            if (params.document) { formData.append('document', params.document) }
            this.documentEdition = await fetchWrapper.put({
                url: `${baseUrl}/command/${idCommand}/document/${id}`,
                useToken: "access",
                body: formData,
                contentFile: true
            });
            this.documents[idCommand][id] = this.documentEdition;
        },
        async deleteDocument(idCommand, id) {
            this.documentEdition = { loading: true };
            this.documentEdition = await fetchWrapper.delete({
                url: `${baseUrl}/command/${idCommand}/document/${id}`,
                useToken: "access"
            });
            delete this.documents[idCommand][id];
        },
        async downloadDocument(idCommand, id) {
            return await fetchWrapper.image({
                url: `${baseUrl}/command/${idCommand}/document/${id}/download`,
                useToken: "access"
            });
        },

        async getItemByList(idCommand, idResearch = [], expand = []) {
            const itemStore = useItemsStore();
            if (!this.items[idCommand]) {
                this.items[idCommand] = {};
            }
            this.itemsLoading = true;
            const idResearchString = idResearch.join(',');
            const expandString = expand.join(',');
            let newItemList = await fetchWrapper.get({
                url: `${baseUrl}/command/${idCommand}/item?&idResearch=${idResearchString}&expand=${expandString}`,
                useToken: "access"
            });
            for (const item of newItemList['data']) {
                this.items[idCommand][item.id_item] = item;
                if (expand.indexOf("item") > -1) {
                    itemStore.items[item.id_item] = item.item
                }
            }
            this.itemsTotalCount[idCommand] = newItemList['count'];
            this.itemsLoading = false;
        },
        async getItemByInterval(idCommand, limit = 100, offset = 0, expand = []) {
            const itemStore = useItemsStore();
            if (!this.items[idCommand]) {
                this.items[idCommand] = {};
            }
            this.itemsLoading = true;
            const expandString = expand.join(',');
            let newItemList = await fetchWrapper.get({
                url: `${baseUrl}/command/${idCommand}/item?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const item of newItemList['data']) {
                this.items[idCommand][item.id_item] = item;
                if (expand.indexOf("item") > -1) {
                    itemStore.items[item.id_item] = item.item
                }
            }
            this.itemsTotalCount[idCommand] = newItemList['count'];
            this.itemsLoading = false;
        },
        async getItemById(idCommand, id, expand = []) {
            const itemStore = useItemsStore();
            if (!this.items[idCommand]) {
                this.items[idCommand] = {};
            }
            this.items[idCommand][id] = { loading: true };
            const expandString = expand.join(',');
            this.items[idCommand][id] = await fetchWrapper.get({
                url: `${baseUrl}/command/${idCommand}/item/${id}?expand=${expandString}`,
                useToken: "access"
            });
            if (expand.indexOf("item") > -1) {
                itemStore.items[commentaire.id_user] = this.commentaires[idCommand][id].user
            }
        },
        async createItem(idCommand, params) {
            this.itemEdition = { loading: true };
            this.itemEdition = await fetchWrapper.post({
                url: `${baseUrl}/command/${idCommand}/item`,
                useToken: "access",
                body: params
            });
            if (!this.items[idCommand]) {
                this.items[idCommand] = {};
            }
            this.items[idCommand][this.itemEdition.id_item] = this.itemEdition;
        },
        async updateItem(idCommand, id, params) {
            this.itemEdition = { loading: true };
            this.itemEdition = await fetchWrapper.put({
                url: `${baseUrl}/command/${idCommand}/item/${id}`,
                useToken: "access",
                body: params
            });
            this.items[idCommand][id] = params;
        },
        async deleteItem(idCommand, id) {
            this.itemEdition = { loading: true };
            this.itemEdition = await fetchWrapper.delete({
                url: `${baseUrl}/command/${idCommand}/item/${id}`,
                useToken: "access"
            });
            delete this.items[idCommand][id];
        },
        async createItemBulk(idCommand, params) {
            this.itemEdition = { loading: true };
            this.itemEdition = await fetchWrapper.post({
                url: `${baseUrl}/command/${idCommand}/item/bulk`,
                useToken: "access",
                body: params
            });
            if (!this.items[idCommand]) {
                this.items[idCommand] = {};
            }
            for (const item of this.itemEdition["valide"]) {
                this.items[idCommand][item.id_item] = item;
            }
        }
    }
});

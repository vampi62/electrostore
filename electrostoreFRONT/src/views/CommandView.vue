<script setup>
import { ref, computed } from 'vue';
import { useRoute } from 'vue-router';
import { useCommandsStore, useUsersStore, useItemsStore, useAuthStore } from '@/stores';

const route = useRoute();
const commandId = route.params.id;

// Accès au store pour récupérer les données de la commande
const commandsStore = useCommandsStore();
const usersStore = useUsersStore();
const itemStore = useItemsStore();
const authStore = useAuthStore();
commandsStore.getCommandById(commandId);
commandsStore.getCommentaireByInterval(commandId, 100, 0, ['user']);
commandsStore.getDocumentByInterval(commandId);
commandsStore.getItemByInterval(commandId, 100, 0, ['item']);

// États pour gérer l'affichage des sections
const showDocuments = ref(true);
const showItems = ref(true);
const showCommentaires = ref(true);

// Fonctions pour basculer l'affichage des sections
const toggleDocuments = () => {
    showDocuments.value = !showDocuments.value;
};
const toggleItems = () => {
    showItems.value = !showItems.value;
};
const toggleCommentaires = () => {
    showCommentaires.value = !showCommentaires.value;
};

// document 
const documentModalShow = ref(false);
const documentModalData = ref({ id_command_document: null, name_command_document: '', document: null, isEdit: false });
const documentOpenAddModal = () => {
    documentModalData.value = { name_command_document: '', document: null, isEdit: false };
    documentModalShow.value = true;
}
const documentOpenEditModal = (doc) => {
    documentModalData.value = { id_command_document: doc.id_command_document, name_command_document: doc.name_command_document, document: null, isEdit: true };
    documentModalShow.value = true;
}
const documentSave = () => {
    if (documentModalData.value.isEdit) {
        commandsStore.updateDocument(commandId, documentModalData.value.id_command_document, documentModalData.value);
    } else {
        commandsStore.createDocument(commandId, documentModalData.value);
    }
    documentModalShow.value = false;
}
const documentDelete = (doc) => {
    commandsStore.deleteDocument(commandId, doc.id_command_document);
}
const handleFileUpload = (e) => {
    documentModalData.value.document = e.target.files[0];
}
const documentDownload = async (fileContent) => {
    const file = await commandsStore.downloadDocument(commandId, fileContent.id_command_document);
    const url = window.URL.createObjectURL(new Blob([file]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', fileContent.name_command_document + '.' + fileContent.type_command_document);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
const documentView = async (fileContent) => {
    const file = await commandsStore.downloadDocument(commandId, fileContent.id_command_document);
    const blob = new Blob([file], { type: getMimeType(fileContent.type_command_document) });
    const url = window.URL.createObjectURL(blob);

    if (['pdf', 'png', 'jpg', 'jpeg', 'gif', 'bmp'].includes(fileContent.type_command_document)) {
        // Ouvrir directement dans une nouvelle fenêtre
        window.open(url, '_blank');
    } else if (['doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx', 'txt'].includes(fileContent.type_command_document)) {
        // Télécharger automatiquement pour les formats éditables
        const a = document.createElement('a');
        a.href = url;
        a.download = fileContent.name || `document.${fileContent.type_command_document}`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    } else {
        alert('Type de fichier non pris en charge');
    }
}
const getMimeType = (type) => {
    const mimeTypes = {
        'pdf': 'application/pdf',
        'doc': 'application/msword',
        'docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        'xls': 'application/vnd.ms-excel',
        'xlsx': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        'ppt': 'application/vnd.ms-powerpoint',
        'pptx': 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
        'txt': 'text/plain',
        'png': 'image/png',
        'jpg': 'image/jpeg',
        'jpeg': 'image/jpeg',
        'gif': 'image/gif',
        'bmp': 'image/bmp'
    };

    return mimeTypes[type] || 'application/octet-stream';
}

// item
const filterText = ref('');
const itemModalShow = ref(false);
const itemModalData = ref({ id_item: null, qte_command_item: 1, prix_command_item: 0, isEdit: false });
const itemOpenAddModal = () => {
    itemModalShow.value = true;
    itemStore.getItemByInterval();
}
const itemSave = (idItem) => {
    if (itemModalData.value.isEdit) {
        commandsStore.updateItem(commandId, itemModalData.value.id_item, itemModalData.value);
    } else {
        itemModalData.value.id_item = idItem;
        commandsStore.createItem(commandId, itemModalData.value);
    }
}
const itemDelete = (item) => {
    commandsStore.deleteItem(commandId, item.id_item);
}
const filteredItems = computed(() => {
    return filterText.value
        ? Object.values(itemStore.items).filter(item => item.nom_item.toLowerCase().includes(filterText.value.toLowerCase()))
        : itemStore.items;
});

// commentaire
const commentaireModalShow = ref(false);
const commentaireModalData = ref({});
const commentaireFormNew = ref('');
const commentaireEdit = (commentaire) => {
    commentaire.editionTmp = commentaire.contenu_command_commentaire;
    commentaire.edition = true;
}
const commentaireSave = async (commentaire = null) => {
    if (commentaire === null) {
        if (commentaireFormNew.value.trim() !== '') {
            await commandsStore.createCommentaire(commandId, {
                contenu_command_commentaire: commentaireFormNew.value
            });
            commentaireFormNew.value = '';
        }
    } else {
        if (commentaire.editionTmp !== null && commentaire.editionTmp.trim() !== '' && commentaire.editionTmp !== commentaire.contenu_command_commentaire) {
            await commandsStore.updateCommentaire(commandId, commentaire.id_command_commentaire, {
                contenu_command_commentaire: commentaire.editionTmp
            });
        }
        commentaire.edition = false;
    }
}
const commentaireDelete = () => {
    commandsStore.deleteCommentaire(commandId, commentaireModalData.value.id_command_commentaire);
    commentaireModalShow.value = false;
}
const commentaireDeleteOpenModal = (commentaire) => {
    commentaireModalData.value = commentaire;
    commentaireModalShow.value = true;
}
</script>
<template>
    <h2 class="text-2xl font-bold mb-4">Commande</h2>
    <div v-if="!commandsStore.commands[commandId].loading">
        <div v-if="commandsStore.commands[commandId]" class="mb-6">
            <p class="text-gray-700"><span class="font-semibold">ID: </span>{{
                commandsStore.commands[commandId].id_command }}</p>
            <p class="text-gray-700"><span class="font-semibold">Prix: </span>{{
                commandsStore.commands[commandId].prix_command }}</p>
            <p class="text-gray-700"><span class="font-semibold">URL: </span>{{
                commandsStore.commands[commandId].url_command }}</p>
            <p class="text-gray-700"><span class="font-semibold">Status: </span> {{
                commandsStore.commands[commandId].status_command }}</p>
            <p class="text-gray-700"><span class="font-semibold">Date: </span>{{
                commandsStore.commands[commandId].date_command }}</p>
            <p class="text-gray-700"><span class="font-semibold">Date de Livraison: </span>{{
                commandsStore.commands[commandId].date_livraison_command }}</p>
        </div>
        <div v-if="!commandsStore.documentsLoading" class="mb-6">
            <h3 @click="toggleDocuments" class="text-xl font-semibold cursor-pointer mb-2">
                Documents ({{ commandsStore.documents[commandId] ?
                    Object.keys(commandsStore.documents[commandId]).length : 0 }})
            </h3>
            <div v-if="showDocuments">
                <button @click="documentOpenAddModal" class="bg-blue-500 text-white px-4 py-2 rounded mb-4">
                    Ajouter un document
                </button>
                <div class="overflow-x-auto max-h-64 overflow-y-auto">
                    <table class="min-w-full table-auto">
                        <thead>
                            <tr>
                                <th class="px-4 py-2 text-left bg-gray-200 sticky top-0">Nom</th>
                                <th class="px-4 py-2 text-left bg-gray-200 sticky top-0">Type</th>
                                <th class="px-4 py-2 text-left bg-gray-200 sticky top-0">Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="document in commandsStore.documents[commandId]"
                                :key="document.id_command_document">
                                <td class="px-4 py-2 border-b border-gray-200">{{ document.name_command_document }}</td>
                                <td class="px-4 py-2 border-b border-gray-200">{{ document.type_command_document }}</td>
                                <td class="px-4 py-2 border-b border-gray-200 flex space-x-2">
                                    <button @click="documentOpenEditModal(document)" class="text-blue-500">Editer</button>
                                    <button @click="documentView(document)" class="text-green-500">Afficher</button>
                                    <button @click="documentDownload(document)" class="text-yellow-500">Telecharger</button>
                                    <button @click="documentDelete(document)" class="text-red-500">Supprimer</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
        <div v-if="!commandsStore.itemsLoading" class="mb-6">
            <h3 @click="toggleItems" class="text-xl font-semibold cursor-pointer mb-2">
                Items ({{ commandsStore.items[commandId] ? Object.keys(commandsStore.items[commandId]).length : 0 }})
            </h3>
            <div v-if="showItems">
                <button @click="itemOpenAddModal" class="bg-blue-500 text-white px-4 py-2 rounded-md">Ajouter Item</button>
                <div class="overflow-x-auto max-h-64 overflow-y-auto">
                    <table class="min-w-full table-auto">
                        <thead>
                            <tr>
                                <th class="px-4 py-2 text-left bg-gray-200 sticky top-0">Nom Item</th>
                                <th class="px-4 py-2 text-left bg-gray-200 sticky top-0">Quantité</th>
                                <th class="px-4 py-2 text-left bg-gray-200 sticky top-0">Prix</th>
                                <th class="px-4 py-2 text-left bg-gray-200 sticky top-0">Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="item in commandsStore.items[commandId]" :key="item.id_item">
                                <td class="px-4 py-2 border-b border-gray-200">{{ itemStore.items[item.id_item].nom_item
                                    }}
                                </td>
                                <td class="px-4 py-2 border-b border-gray-200">{{ item.qte_command_item }}</td>
                                <td class="px-4 py-2 border-b border-gray-200">{{ item.prix_command_item }}</td>
                                <td class="px-4 py-2 border-b border-gray-200">
                                    <button @click="itemDelete(item)"
                                        class="bg-red-500 text-white px-2 py-1 rounded">Retirer</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
        <div v-if="!commandsStore.commentairesLoading" class="mb-6">
            <h3 @click="toggleCommentaires" class="text-xl font-semibold cursor-pointer mb-2">
                Commentaires ({{ commandsStore.commentaires[commandId] ?
                    Object.keys(commandsStore.commentaires[commandId]).length : 0 }})
            </h3>
            <div v-if="showCommentaires">
                <!-- Zone de saisie de commentaire -->
                <div class="flex items-center space-x-4">
                    <input v-model="commentaireFormNew" type="text" placeholder="Ajouter un commentaire..."
                        class="w-full p-2 border rounded-lg">
                    <button @click="commentaireSave(null)"
                        class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
                        Publier
                    </button>
                </div>
                <!-- Affichage des commentaires existants -->
                <div class="space-y-4 overflow-x-auto max-h-64 overflow-y-auto">
                    <div v-for="commentaire in commandsStore.commentaires[commandId]"
                        :key="commentaire.id_command_commentaire" class="flex flex-col border p-4 rounded-lg">
                        <div :class="{
                            'text-right': commentaire.id_user === authStore.user.id_user,
                            'text-left': commentaire.id_user !== authStore.user.id_user
                        }" class="text-sm text-gray-600">
                            <span class="font-semibold">
                                {{ usersStore.users[commentaire.id_user].nom_user }} {{
                                    usersStore.users[commentaire.id_user].prenom_user }}
                            </span>
                            <span class="text-xs text-gray-500">
                                - {{ commentaire.date_command_commentaire }}
                            </span>
                        </div>
                        <div class="text-center text-gray-800 mb-2">
                            <template v-if="commentaire.edition">
                                <textarea v-model="commentaire.editionTmp"
                                    class="w-full p-2 border rounded-lg"></textarea>
                                <div class="flex justify-end space-x-2 mt-2">
                                    <button @click="commentaireSave(commentaire)"
                                        class="px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600">
                                        Valider
                                    </button>
                                    <button @click="commentaire.edition = false"
                                        class="px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
                                        Annuler
                                    </button>
                                </div>
                            </template>
                            <template v-else>
                                {{ commentaire.contenu_command_commentaire }}
                            </template>
                        </div>

                        <!-- Boutons modifier/supprimer si conditions remplies -->
                        <template v-if="!commentaire.edition">
                            <div v-if="commentaire.id_user === authStore.user.id_user || authStore.user.role === 'admin'"
                                class="flex justify-end space-x-2">
                                <button @click="commentaireEdit(commentaire)"
                                    class="px-3 py-1 bg-yellow-400 text-white rounded-lg hover:bg-yellow-500">
                                    Modifier
                                </button>
                                <button @click="commentaireDeleteOpenModal(commentaire)"
                                    class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
                                    Supprimer
                                </button>
                            </div>
                        </template>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <div v-else>
        <div>chargement</div>
    </div>

    <div v-if="documentModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center">
        <div class="bg-white p-6 rounded shadow-lg w-96">
            <h2 class="text-xl mb-4">{{ documentModalData.isEdit ? 'Editer' : 'Ajouter' }} un document</h2>
            <input v-model="documentModalData.name_command_document" type="text" placeholder="Nom du document"
                class="w-full p-2 mb-4 border rounded" />
            <input @change="handleFileUpload" type="file" class="w-full p-2 mb-4" />
            <div class="flex justify-end space-x-2">
                <button @click="documentModalShow = false" class="px-4 py-2 bg-gray-300 rounded">Annuler</button>
                <button @click="documentSave" class="px-4 py-2 bg-blue-500 text-white rounded">Enregistrer</button>
            </div>
        </div>
    </div>
    <div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center">
        <div class="bg-white rounded-lg shadow-lg w-3/4 p-6">
            <div class="flex justify-between items-center border-b pb-3">
                <h2 class="text-2xl font-semibold">Liste des Items</h2>
                <button @click="itemModalShow = false" class="text-gray-500 hover:text-gray-700">&times;</button>
            </div>

            <!-- Filtres -->
            <div class="my-4 flex gap-4">
                <input type="text" v-model="filterText" placeholder="Filtrer par nom" class="border p-2 rounded w-full">
            </div>

            <!-- Tableau Items -->
            <div class="overflow-y-auto max-h-96 min-h-96">
                <table class="min-w-full bg-white border border-gray-200">
                    <thead class="bg-gray-100 sticky top-0">
                        <tr>
                            <th class="px-4 py-2 border-b">Nom Item</th>
                            <th class="px-4 py-2 border-b">Prix</th>
                            <th class="px-4 py-2 border-b">Commander</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in filteredItems" :key="item.id_item">
                            <td class="px-4 py-2 border-b">{{ item.nom_item }}</td>
                            <td class="px-4 py-2 border-b">{{ item.prix_item }}</td>
                            <td class="px-4 py-2 border-b">
                                <div v-if="commandsStore.items[commandId][item.id_item]">
                                    <button @click="itemDelete(item)"
                                        class="bg-red-500 text-white px-2 py-1 rounded">Retirer</button>
                                </div>
                                <div v-else>
                                    <button @click="itemSave(item.id_item)"
                                        class="bg-green-500 text-white px-2 py-1 rounded">Ajouter</button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
    <div v-if="commentaireModalShow" class="fixed inset-0 flex items-center justify-center bg-black bg-opacity-50">
        <div class="bg-white p-6 rounded-lg shadow-lg">
            <h2 class="text-lg font-semibold">Confirmer la suppression</h2>
            <p>Voulez-vous vraiment supprimer ce commentaire ?</p>
            <div class="flex justify-end space-x-4 mt-4">
                <button @click="commentaireDelete()"
                    class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
                    Oui, supprimer
                </button>
                <button @click="commentaireModalShow = false"
                    class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
                    Annuler
                </button>
            </div>
        </div>
    </div>
</template>
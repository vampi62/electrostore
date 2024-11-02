<script setup>
import { useI18n } from 'vue-i18n';
const { t } = useI18n();

import { useItemsStore } from '@/stores';
const itemsStore = useItemsStore();

function getTotalQuantity(itembox) {
    if (!itembox) {
        return 0;
    }
    return itembox.reduce((total, box) => total + box.qte_itembox, 0);
}


itemsStore.getAll();
</script>

<template>
    <div>
        <h2>inventaire</h2>
    </div>
    <table class="table">
        <thead>
            <tr>
                <th>id</th>
                <th>nom</th>
                <th>seuil</th>
                <th>datasheet</th>
                <th>description</th>
                <th>img</th>
                <th>quantité</th>
                <th></th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            <tr v-if="!itemsStore.items.loading" v-for="item in itemsStore.items" :key="item.id_item">
                <td>{{ item.id_item }}</td>
                <td>{{ item.nom_item }}</td>
                <td>{{ item.seuil_min_item }}</td>
                <td>{{ item.datasheet_item }}</td>
                <td>{{ item.description_item }}</td>
                <td>
                    <div v-if="item.id_img">
                        {{  getImages(item.id_img) }}
                        <img :src="images[item.id_img]" v-if="images[item.id_img]" :alt="'Image de ' + item.name" width="100" />
                        <span v-else>Chargement...</span>
                    </div>
                    <div v-else>
                        Aucune image
                    </div>
                </td>
                <td>{{ getTotalQuantity(item.itembox) }}</td>
                <td>
                    <button class="btn btn-primary">modifier</button>
                    <button class="btn btn-danger">supprimer</button>
                </td>
            </tr>
        </tbody>
    </table>
</template>

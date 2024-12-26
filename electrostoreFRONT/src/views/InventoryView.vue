<script setup>
import { onMounted, ref } from 'vue';
import { useItemsStore } from '@/stores';
const itemsStore = useItemsStore();

const offset = ref(0);
const limit = 100;
function getTotalQuantity(itembox) {
    if (!itembox) {
        return 0;
    }
    return itembox.reduce((total, box) => total + box.qte_item_box, 0);
}

function handleScroll(event) {
    console.log(event);
    const bottom = event.target.scrollHeight - event.target.scrollTop === event.target.clientHeight;
    if (bottom) {
        if (itemsStore.items.length < itemsStore.TotalCount) {
            offset.value += limit;
            itemsStore.getItemByInterval(limit, offset.value, ["item_boxs"]);
        }
    }
}

onMounted(() => {
    itemsStore.getItemByInterval(limit, offset.value, ["item_boxs"]);
});
</script>

<template>
    <div>
        <h2>inventaire</h2>
    </div>
    <div @scroll="handleScroll">
    <table class="min-w-full border-collapse border border-gray-300">
        <thead class="bg-gray-100">
            <tr>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">id</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">nom</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">seuil</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">datasheet</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">description
                </th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">img</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">quantité</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">edit</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">supprimer</th>
            </tr>
        </thead>
        <tbody>
            <template v-if="!itemsStore.itemsLoading">
                <tr v-for="item in itemsStore.items" :key="item.id_item" class="even:bg-gray-50">
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ item.id_item }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ item.nom_item }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ item.seuil_min_item }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ item.datasheet }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ item.description_item }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                        <div v-if="item.id_img">
                            <img v-if="itemsStore.imagesURL[item.id_img]" :src="itemsStore.imagesURL[item.id_img]" alt="Image"
                                class="w-16 h-16 object-cover rounded" />
                            <span v-else class="w-16 h-16 object-cover rounded">Chargement...</span>
                        </div>
                        <div v-else>
                            <img src="../assets/nopicture.webp" alt="Image"
                                class="w-16 h-16 object-cover rounded" />
                        </div>
                    </td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ getTotalQuantity(item.item_boxs)
                        }}
                    </td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                        <button class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm">
                            Modifier
                        </button>
                    </td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                        <button class="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm ml-2">
                            Supprimer
                        </button>
                    </td>
                </tr>
            </template>
            <template v-else>
                <div>chargement</div>
            </template>
        </tbody>
    </table>
    </div>
</template>

<script setup>
import { onMounted, ref, computed } from 'vue';
import { useItemsStore } from '@/stores';
const itemsStore = useItemsStore();

async function fetchData() {
    itemsStore.getItemByInterval(limit, offset.value, ["item_boxs"]);
}
onMounted(() => {
    fetchData();
});

const sort = ref({ key: '', order: 'asc' });
function changeSort(key) {
    if (sort.value.key === key) {
        sort.value.order = sort.value.order === 'asc' ? 'desc' : 'asc';
    } else {
        sort.value.key = key;
        sort.value.order = 'asc';
    }
}

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
const filter = ref([
    { key: 'nom_item', value: '', type: 'text', label: 'url', compareMethod: 'contain' },
    { key: 'seuil_min_item', value: '', type: 'number', label: 'seuil minimum', compareMethod: '>=' },
    { key: 'seuil_min_item', value: '', type: 'number', label: 'seuil maximum', compareMethod: '<=' },
    { key: 'qte_item_box', subPath: 'item_boxs', value: '', type: 'number', label: 'stock minimum', compareMethod: '>=' },
    { key: 'qte_item_box', subPath: 'item_boxs', value: '', type: 'number', label: 'stock maximum', compareMethod: '<=' },
]);
const filteredItems = computed(() => {
    return Object.values(itemsStore.items).filter(element => {
        return filter.value.every(f => {
            if (f.value) {
                if (f.subPath) {
                    if (f.compareMethod === '=') {
                        return element[f.subPath].some(subElement => subElement[f.key] === f.value);
                    } else if (f.compareMethod === '>=') {
                        return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) >= f.value;
                    } else if (f.compareMethod === '<=') {
                        return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) <= f.value;
                    }
                } else {
                    if (f.compareMethod === '=') {
                        return element[f.key] === f.value;
                    } else if (f.compareMethod === '>=') {
                        return element[f.key] >= f.value;
                    } else if (f.compareMethod === '<=') {
                        return element[f.key] <= f.value;
                    } else if (f.compareMethod === 'contain') {
                        return element[f.key].includes(f.value);
                    }
                }
            }
            return true;
        });
    });
});
const sortedItems = computed(() => {
    if (sort.value.key) {
        return Object.values(filteredItems.value).sort((a, b) => {
            if (sort.value.order === 'asc') {
                return a[sort.value.key] > b[sort.value.key] ? 1 : -1;
            } else {
                return a[sort.value.key] < b[sort.value.key] ? 1 : -1;
            }
        });
    }
    return filteredItems.value;
});
</script>

<template>
    <div>
        <h2>inventaire</h2>
    </div>
    <div>
        <button class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm">
            Ajouter
        </button>
        <div>
            <div class="flex flex-wrap">
                <template v-for="f in filter">
                    <label class="text-sm text-gray-700 ml-2">{{ f.label }}</label>
                    <template v-if="f.type === 'select'">
                        <select v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2">
                            <option value=""></option>
                            <template v-if="f.options">
                                <option v-for="option in f.options" :key="option[0]" :value="option[0]">{{ option[1] }}
                                </option>
                            </template>
                        </select>
                    </template>
                    <template v-else-if="f.type === 'date'">
                        <input type="date" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
                    </template>
                    <template v-else-if="f.type === 'number'">
                        <input type="number" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
                    </template>
                    <template v-else-if="f.type === 'text'">
                        <input type="text" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
                    </template>
                </template>
            </div>
        </div>
    </div>
    <table class="min-w-full border-collapse border border-gray-300">
        <thead class="bg-gray-100">
            <tr>
                <th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
                    @click="changeSort('nom_item')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">nom</span>
                        <template v-if="sort.key === 'nom_item'">
                            <template v-if="sort.order === 'asc'">
                                <font-awesome-icon icon="fa-solid fa-sort-up" class="ml-2" />
                            </template>
                            <template v-else>
                                <font-awesome-icon icon="fa-solid fa-sort-down" class="ml-2" />
                            </template>
                        </template>
                        <template v-else>
                            <font-awesome-icon icon="fa-solid fa-sort" class="ml-2" />
                        </template>
                    </div>
                </th>
                <th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
                    @click="changeSort('seuil_min_item')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">seuil</span>
                        <template v-if="sort.key === 'seuil_min_item'">
                            <template v-if="sort.order === 'asc'">
                                <font-awesome-icon icon="fa-solid fa-sort-up" class="ml-2" />
                            </template>
                            <template v-else>
                                <font-awesome-icon icon="fa-solid fa-sort-down" class="ml-2" />
                            </template>
                        </template>
                        <template v-else>
                            <font-awesome-icon icon="fa-solid fa-sort" class="ml-2" />
                        </template>
                    </div>
                </th>
                <th
                    class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">description</span>
                    </div>
                </th>
                <th
                    class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">img</span>
                    </div>
                </th>
                <th
                    class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">quantité</span>
                    </div>
                </th>
                <th
                    class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
                    <div class="flex justify-between items-center">
                        <span class="flex-1"></span>
                    </div>
                </th>
            </tr>
        </thead>
        <tbody>
            <template v-if="!itemsStore.itemsLoading">
                <RouterLink v-for="item in sortedItems" :key="item.id_item" :to="'/inventory/' + item.id_item"
                    custom v-slot="{ navigate }">
                    <tr @click="navigate" class=" transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ item.nom_item }}</td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ item.seuil_min_item }}
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ item.description_item }}
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                            <div v-if="item.id_img">
                                <img v-if="itemsStore.imagesURL[item.id_img]" :src="itemsStore.imagesURL[item.id_img]"
                                    alt="Image" class="w-16 h-16 object-cover rounded" />
                                <span v-else class="w-16 h-16 object-cover rounded">Chargement...</span>
                            </div>
                            <div v-else>
                                <img src="../assets/nopicture.webp" alt="Image"
                                    class="w-16 h-16 object-cover rounded" />
                            </div>
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{
                            getTotalQuantity(item.item_boxs)
                            }}
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-center text-sm text-gray-700">
                            <button class="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm">
                                Supprimer
                            </button>
                        </td>
                    </tr>
                </RouterLink>
            </template>
            <template v-else>
                <div>chargement</div>
            </template>
        </tbody>
    </table>
</template>

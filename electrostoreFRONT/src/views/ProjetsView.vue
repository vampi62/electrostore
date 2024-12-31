<script setup>
import { onMounted, ref, computed } from 'vue';
import { useProjetsStore } from '@/stores';
const projetsStore = useProjetsStore();

async function fetchData() {
    projetsStore.getProjetByInterval(100, 0);
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
const filter = ref([
    { key: 'status_projet', value: '', type: 'select', options: [['en attente', 'en attente'], ['en cours', 'en cours'], ['terminée', 'terminée']], label: 'status', compareMethod: '=' },
    { key: 'date_projet', value: '', type: 'date', label: 'date', compareMethod: '>=' },
    { key: 'nom_projet', value: '', type: 'text', label: 'nom', compareMethod: 'contain' },
    { key: 'url_projet', value: '', type: 'text', label: 'url', compareMethod: 'contain' },
    { key: 'date_fin_projet', value: '', type: 'date', label: 'date_cloture', compareMethod: '>=' }
]);
const filteredProjets = computed(() => {
    return Object.values(projetsStore.projets).filter(element => {
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
const sortedProjets = computed(() => {
    if (sort.value.key) {
        return Object.values(filteredProjets.value).sort((a, b) => {
            if (sort.value.order === 'asc') {
                return a[sort.value.key] > b[sort.value.key] ? 1 : -1;
            } else {
                return a[sort.value.key] < b[sort.value.key] ? 1 : -1;
            }
        });
    }
    return filteredProjets.value;
});
</script>

<template>
    <div>
        <h2>liste des projets</h2>
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
                    @click="changeSort('nom_projet')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">nom</span>
                        <template v-if="sort.key === 'nom_projet'">
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
                <th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
                    @click="changeSort('url_projet')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">url</span>
                        <template v-if="sort.key === 'url_projet'">
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
                    @click="changeSort('status_projet')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">status</span>
                        <template v-if="sort.key === 'status_projet'">
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
                    @click="changeSort('date_projet')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">date</span>
                        <template v-if="sort.key === 'date_projet'">
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
                    @click="changeSort('date_fin_projet')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">date_fin</span>
                        <template v-if="sort.key === 'date_fin_projet'">
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
                        <span class="flex-1"></span>
                    </div>
                </th>
            </tr>
        </thead>
        <tbody>
            <template v-if="!projetsStore.projets.projetsLoading">
                <RouterLink v-for="projet in sortedProjets" :key="projet.id_projet"
                    :to="'/projets/' + projet.id_projet" custom v-slot="{ navigate }">
                    <tr @click="navigate" class=" transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.nom_projet }}</td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.description_projet
                            }}
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.url_projet }}</td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.status_projet }}
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.date_projet }}</td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.date_fin_projet }}
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-center text-sm text-gray-700">
                            <button class="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm">
                                Supprimer
                            </button>
                        </td>
                    </tr>
                </RouterLink>
            </template>
        </tbody>
    </table>
</template>

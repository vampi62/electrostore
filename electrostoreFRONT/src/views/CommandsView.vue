<script setup>
import { onMounted, ref, computed, inject } from 'vue';


const { addNotification } = inject('useNotification');




import { useI18n } from 'vue-i18n';
const { t } = useI18n();





import { useCommandsStore, useItemsStore } from '@/stores';
const commandsStore = useCommandsStore();
const itemsStore = useItemsStore();

async function fetchData() {
    let allItems = [];
    let offset = 0;
    const limit = 100;
    do {
        await commandsStore.getCommandByInterval(limit, offset, ['commands_items']);
        offset += limit;
    } while (offset < commandsStore.commandsTotalCount)
    await itemsStore.getItemByList(allItems);
    filter.value[6].options = Object.values(itemsStore.items).map(item => [item.id_item, item.nom_item]);
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
    { key: 'status_command', value: '', type: 'select', options: [['En attente', t('VCommandsFilterStatus1')], ['En cours', t('VCommandsFilterStatus2')], ['Terminée', t('VCommandsFilterStatus3')], ['Annulée', t('VCommandsFilterStatus4')]], label: t('VCommandsFilterStatus'), compareMethod: '=' },
    { key: 'date_command', value: '', type: 'date', label: t('VCommandsFilterDate'), compareMethod: '>=' },
    { key: 'url_command', value: '', type: 'text', label: t('VCommandsFilterURL'), compareMethod: 'contain' },
    { key: 'prix_command', value: '', type: 'number', label: t('VCommandsFilterPriceMin'), compareMethod: '>=' },
    { key: 'prix_command', value: '', type: 'number', label: t('VCommandsFilterPriceMax'), compareMethod: '<=' },
    { key: 'date_livraison_command', value: '', type: 'date', label: t('VCommandsFilterDateL'), compareMethod: '>=' },
    { key: 'id_item', subPath: 'commands_items', value: '', type: 'select', options: Object.values(itemsStore.items).map(item => [item.id_item, item.nom_item]), label: t('VCommandsFilterItem'), compareMethod: '=' }
]);
const filteredCommands = computed(() => {
    return Object.values(commandsStore.commands).filter(element => {
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
const sortedCommands = computed(() => {
    if (sort.value.key) {
        return Object.values(filteredCommands.value).sort((a, b) => {
            if (sort.value.order === 'asc') {
                return a[sort.value.key] > b[sort.value.key] ? 1 : -1;
            } else {
                return a[sort.value.key] < b[sort.value.key] ? 1 : -1;
            }
        });
    }
    return filteredCommands.value;
});
</script>

<template>
    <div>
        <h2>{{ $t('VCommandsTitle') }}</h2>
    </div>
    <div>
        <div
            class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
            <RouterLink :to="'/commands/new'">
                {{ $t('VCommandsAdd') }}
            </RouterLink>
        </div>
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
                    @click="changeSort('status_command')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">{{ $t('VCommandsTabStatus') }}</span>
                        <template v-if="sort.key === 'status_command'">
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
                    @click="changeSort('date_command')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">{{ $t('VCommandsTabDate') }}</span>
                        <template v-if="sort.key === 'date_command'">
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
                    @click="changeSort('url_command')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">{{ $t('VCommandsTabURL') }}</span>
                        <template v-if="sort.key === 'url_command'">
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
                    @click="changeSort('prix_command')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">{{ $t('VCommandsTabPrix') }}</span>
                        <template v-if="sort.key === 'prix_command'">
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
                        <span class="flex-1">{{ $t('VCommandsTabItemList') }}</span>
                    </div>
                </th>
                <th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
                    @click="changeSort('date_livraison_command')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">{{ $t('VCommandsTabDateL') }}</span>
                        <template v-if="sort.key === 'date_livraison_command'">
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
            </tr>
        </thead>
        <tbody>
            <template v-if="!commandsStore.commandsLoading">
                <RouterLink v-for="command in sortedCommands" :key="command.id_command"
                    :to="'/commands/' + command.id_command" custom v-slot="{ navigate }">
                    <tr @click="navigate" class=" transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ command.status_command }}
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ command.date_command }}
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ command.url_command }}
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ command.prix_command }}
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                            <ul>
                                <li v-for="item in commandsStore.items[command.id_command]" :key="item.id_item">
                                    {{ item.id_item }} - {{ itemsStore.items[item.id_item]?.nom_item }}
                                </li>
                            </ul>
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{
                            command.date_livraison_command
                        }}</td>
                    </tr>
                </RouterLink>
            </template>
        </tbody>
    </table>
</template>
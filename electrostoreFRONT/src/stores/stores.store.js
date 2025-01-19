import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

import { useTagsStore, useItemsStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useStoresStore = defineStore({
    id: 'stores',
    state: () => ({
        storesLoading: true,
        storesTotalCount: 0,
        stores: {},
        storeEdition: {},

        boxsLoading: true,
        boxsTotalCount: {},
        boxs: {},
        boxEdition: {},

        ledsLoading: true,
        ledsTotalCount: {},
        leds: {},
        ledEdition: {},

        tagsStoreLoading: true,
        tagsStoreTotalCount: {},
        tagsStore: {},
        tagStoreEdition: {},

        boxItemsLoading: true,
        boxItemsTotalCount: {},
        boxItems: {},
        boxItemEdition: {},

        boxTagsLoading: true,
        boxTagsTotalCount: {},
        boxTags: {},
        boxTagEdition: {}
    }),
    actions: {
        async getStoreByList(idResearch = [], expand = []) {
            this.storesLoading = true;
            const idResearchString = idResearch.join(',');
            const expandString = expand.join(',');
            let newStoreList = await fetchWrapper.get({
                url: `${baseUrl}/store?&idResearch=${idResearchString}&expand=${expandString}`,
                useToken: "access"
            });
            for (const store of newStoreList['data']) {
                this.stores[store.id_store] = store;
                this.boxsTotalCount[store.id_store] = store.boxs_count;
                this.ledsTotalCount[store.id_store] = store.leds_count;
                this.tagsStoreTotalCount[store.id_store] = store.stores_tags_count;
                if (expand.indexOf("boxs") > -1) {
                    this.boxs[store.id_store] = {};
                    for (const box of store.boxs) {
                        this.boxs[store.id_store][box.id_box] = box;
                    }
                }
                if (expand.indexOf("leds") > -1) {
                    this.leds[store.id_store] = {};
                    for (const led of store.leds) {
                        this.leds[store.id_store][led.id_led] = led;
                    }
                }
                if (expand.indexOf("stores_tags") > -1) {
                    this.tagsStore[store.id_store] = {};
                    for (const tag of store.tagsStore) {
                        this.tagsStore[store.id_store][tag.id_tag] = tag;
                    }
                }
            }
            this.storesTotalCount = newStoreList['count'];
            this.storesLoading = false;
        },
        async getStoreByInterval(limit = 100, offset = 0, expand = []) {
            this.storesLoading = true;
            const expandString = expand.join(',');
            let newStoreList = await fetchWrapper.get({
                url: `${baseUrl}/store?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const store of newStoreList['data']) {
                this.stores[store.id_store] = store;
                this.boxsTotalCount[store.id_store] = store.boxs_count;
                this.ledsTotalCount[store.id_store] = store.leds_count;
                this.tagsStoreTotalCount[store.id_store] = store.stores_tags_count;
                if (expand.indexOf("boxs") > -1) {
                    this.boxs[store.id_store] = {};
                    for (const box of store.boxs) {
                        this.boxs[store.id_store][box.id_box] = box;
                    }
                }
                if (expand.indexOf("leds") > -1) {
                    this.leds[store.id_store] = {};
                    for (const led of store.leds) {
                        this.leds[store.id_store][led.id_led] = led;
                    }
                }
                if (expand.indexOf("stores_tags") > -1) {
                    this.tagsStore[store.id_store] = {};
                    for (const tag of store.tagsStore) {
                        this.tagsStore[store.id_store][tag.id_tag] = tag;
                    }
                }
            }
            this.storesTotalCount = newStoreList['count'];
            this.storesLoading = false;
        },
        async getStoreById(id, expand = []) {
            this.stores[id] = { loading: true };
            const expandString = expand.join(',');
            this.stores[id] = await fetchWrapper.get({
                url: `${baseUrl}/store/${id}?expand=${expandString}`,
                useToken: "access"
            });
            this.boxsTotalCount[id] = this.stores[id].boxs_count;
            this.ledsTotalCount[id] = this.stores[id].leds_count;
            this.tagsStoreTotalCount[id] = this.stores[id].stores_tags_count;
            if (expand.indexOf("boxs") > -1) {
                this.boxs[id] = {};
                for (const box of this.stores[id].boxs) {
                    this.boxs[id][box.id_box] = box;
                }
            }
            if (expand.indexOf("leds") > -1) {
                this.leds[id] = {};
                for (const led of this.stores[id].leds) {
                    this.leds[id][led.id_led] = led;
                }
            }
            if (expand.indexOf("stores_tags") > -1) {
                this.tagsStore[id] = {};
                for (const tag of this.stores[id].tagsStore) {
                    this.tagsStore[id][tag.id_tag] = tag;
                }
            }
        },
        async createStore(params) {
            this.storeEdition.loading = true;
            this.storeEdition = await fetchWrapper.post({
                url: `${baseUrl}/store`,
                useToken: "access",
                body: params
            });
            this.stores[this.storeEdition.id_store] = this.storeEdition;
        },
        async updateStore(id, params) {
            this.storeEdition.loading = true;
            this.storeEdition = await fetchWrapper.put({
                url: `${baseUrl}/store/${id}`,
                useToken: "access",
                body: params
            });
            this.stores[id] = this.storeEdition;
        },
        async deleteStore(id) {
            this.storeEdition.loading = true;
            this.storeEdition = await fetchWrapper.delete({
                url: `${baseUrl}/store/${id}`,
                useToken: "access"
            });
            delete this.stores[id];
        },

        async getBoxByInterval(idStore, limit = 100, offset = 0, expand = []) {
            // init list if not exist
            if (!this.boxs[idStore]) {
                this.boxs[idStore] = {};
            }
            this.boxsLoading = true;
            const expandString = expand.join(',');
            let newBoxList = await fetchWrapper.get({
                url: `${baseUrl}/store/${idStore}/box?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const box of newBoxList['data']) {
                this.boxs[idStore][box.id_box] = box;
                this.boxItemsTotalCount[box.id_box] = box.item_boxs_count;
                this.boxTagsTotalCount[box.id_box] = box.box_tags_count;
                if (expand.indexOf("item_boxs") > -1) {
                    this.boxItems[box.id_box] = {};
                    for (const item of box.item_boxs) {
                        this.boxItems[box.id_box][item.id_item] = item;
                    }
                }
                if (expand.indexOf("box_tags") > -1) {
                    this.boxTags[box.id_box] = {};
                    for (const tag of box.box_tags) {
                        this.boxTags[box.id_box][tag.id_tag] = tag;
                    }
                }
            }
            this.boxsTotalCount = newBoxList['count'];
            this.boxsLoading = false;
        },
        async getBoxById(idStore, id, expand = []) {
            // init list if not exist
            if (!this.boxs[idStore]) {
                this.boxs[idStore] = {};
            }
            this.boxs[idStore][id] = { loading: true };
            const expandString = expand.join(',');
            this.boxs[idStore][id] = await fetchWrapper.get({
                url: `${baseUrl}/store/${idStore}/box/${id}?expand=${expandString}`,
                useToken: "access"
            });
            this.boxItemsTotalCount[id] = this.boxs[idStore][id].item_boxs_count;
            this.boxTagsTotalCount[id] = this.boxs[idStore][id].box_tags_count;
            if (expand.indexOf("item_boxs") > -1) {
                this.boxItems[id] = {};
                for (const item of this.boxs[idStore][id].item_boxs) {
                    this.boxItems[id][item.id_item] = item;
                }
            }
            if (expand.indexOf("box_tags") > -1) {
                this.boxTags[id] = {};
                for (const tag of this.boxs[idStore][id].box_tags) {
                    this.boxTags[id][tag.id_tag] = tag;
                }
            }
        },
        async createBox(idStore, params) {
            this.boxEdition = { loading: true };
            this.boxEdition = await fetchWrapper.post({
                url: `${baseUrl}/store/${idStore}/box`,
                useToken: "access",
                body: params
            });
            if (!this.boxs[idStore]) {
                this.boxs[idStore] = {};
            }
            this.boxs[idStore][this.boxEdition.id_box] = this.boxEdition;
        },
        async updateBox(idStore, id, params) {
            this.boxEdition = { loading: true };
            this.boxEdition = await fetchWrapper.put({
                url: `${baseUrl}/store/${idStore}/box/${id}`,
                useToken: "access",
                body: params
            });
            this.boxs[idStore][id] = this.boxEdition;
        },
        async deleteBox(idStore, id) {
            this.boxEdition = { loading: true };
            this.boxEdition = await fetchWrapper.delete({
                url: `${baseUrl}/store/${idStore}/box/${id}`,
                useToken: "access"
            });
            delete this.boxs[idStore][id];
        },
        async createBoxBulk(idStore, params) {
            this.boxEdition = { loading: true };
            this.boxEdition = await fetchWrapper.post({
                url: `${baseUrl}/store/${idStore}/box/bulk`,
                useToken: "access",
                body: params
            });
            if (!this.boxs[idStore]) {
                this.boxs[idStore] = {};
            }
            for (const box of this.boxEdition['data']) {
                this.boxs[idStore][box.id_box] = box;
            }
        },
        async updateBoxBulk(idStore, params) {
            this.boxEdition = { loading: true };
            this.boxEdition = await fetchWrapper.put({
                url: `${baseUrl}/store/${idStore}/box/bulk`,
                useToken: "access",
                body: params
            });
            if (!this.boxs[idStore]) {
                this.boxs[idStore] = {};
            }
            for (const box of this.boxEdition['data']) {
                this.boxs[idStore][box.id_box] = box;
            }
        },
        async deleteBoxBulk(idStore, params) {
            this.boxEdition = { loading: true };
            this.boxEdition = await fetchWrapper.delete({
                url: `${baseUrl}/store/${idStore}/box/bulk`,
                useToken: "access",
                body: params
            });
            for (const id of this.boxs["valide"]) {
                delete this.boxs[idStore][id];
            }
        },
        async showBoxById(idStore, id, params) {
            await fetchWrapper.post({
                url: `${baseUrl}/store/${idStore}/box/${id}/show`,
                useToken: "access",
                body: params
            });
        },

        async getLedByInterval(idStore, limit = 100, offset = 0) {
            // init list if not exist
            if (!this.leds[idStore]) {
                this.leds[idStore] = {};
            }
            this.ledsLoading = true;
            let newLedList = await fetchWrapper.get({
                url: `${baseUrl}/store/${idStore}/led?limit=${limit}&offset=${offset}`,
                useToken: "access"
            });
            for (const led of newLedList['data']) {
                this.leds[idStore][led.id_led] = led;
            }
            this.ledsTotalCount = newLedList['count'];
            this.ledsLoading = false;
        },
        async getLedById(idStore, id) {
            // init list if not exist
            if (!this.leds[idStore]) {
                this.leds[idStore] = {};
            }
            this.leds[idStore][id] = { loading: true };
            this.leds[idStore][id] = await fetchWrapper.get({
                url: `${baseUrl}/store/${idStore}/led/${id}`,
                useToken: "access"
            });
        },
        async createLed(idStore, params) {
            this.ledEdition = { loading: true };
            this.ledEdition = await fetchWrapper.post({
                url: `${baseUrl}/store/${idStore}/led`,
                useToken: "access",
                body: params
            });
            if (!this.leds[idStore]) {
                this.leds[idStore] = {};
            }
            this.leds[idStore][this.ledEdition.id_led] = this.ledEdition;
        },
        async updateLed(idStore, id, params) {
            this.ledEdition = { loading: true };
            this.ledEdition = await fetchWrapper.put({
                url: `${baseUrl}/store/${idStore}/led/${id}`,
                useToken: "access",
                body: params
            });
            this.leds[idStore][id] = this.ledEdition;
        },
        async deleteLed(idStore, id) {
            this.ledEdition = { loading: true };
            this.ledEdition = await fetchWrapper.delete({
                url: `${baseUrl}/store/${idStore}/led/${id}`,
                useToken: "access"
            });
            delete this.leds[idStore][id];
        },
        async createLedBulk(idStore, params) {
            this.ledEdition = { loading: true };
            this.ledEdition = await fetchWrapper.post({
                url: `${baseUrl}/store/${idStore}/led/bulk`,
                useToken: "access",
                body: params
            });
            if (!this.leds[idStore]) {
                this.leds[idStore] = {};
            }
            for (const led of this.ledEdition["valide"]) {
                this.leds[idStore][led.id_led] = led;
            }
        },
        async updateLedBulk(idStore, params) {
            this.ledEdition = { loading: true };
            this.ledEdition = await fetchWrapper.put({
                url: `${baseUrl}/store/${idStore}/led/bulk`,
                useToken: "access",
                body: params
            });
            if (!this.leds[idStore]) {
                this.leds[idStore] = {};
            }
            for (const led of this.ledEdition["valide"]) {
                this.leds[idStore][led.id_led] = led;
            }
        },
        async deleteLedBulk(idStore, params) {
            this.ledEdition = { loading: true };
            this.ledEdition = await fetchWrapper.delete({
                url: `${baseUrl}/store/${idStore}/led/bulk`,
                useToken: "access",
                body: params
            });
            for (const id of this.ledEdition["valide"]) {
                delete this.leds[idStore][id];
            }
        },
        async showLedById(idStore, id, params) {
            await fetchWrapper.post({
                url: `${baseUrl}/store/${idStore}/led/${id}/show`,
                useToken: "access",
                body: params
            });
        },

        async getTagStoreByInterval(idStore, limit = 100, offset = 0, expand = []) {
            // init list if not exist
            const tagsStore = useTagsStore();
            if (!this.tagsStore[idStore]) {
                this.tagsStore[idStore] = {};
            }
            this.tagsStoreLoading = true;
            const expandString = expand.join(',');
            let newTagList = await fetchWrapper.get({
                url: `${baseUrl}/store/${idStore}/tag?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const tag of newTagList['data']) {
                this.tagsStore[idStore][tag.id_tag] = tag;
                if (expand.indexOf("tag") > -1) {
                    tagsStore.tags[tag.id_tag] = tag.tag;
                }
            }
            this.tagsStoreTotalCount = newTagList['count'];
            this.tagsStoreLoading = false;
        },
        async getTagStoreById(idStore, id, expand = []) {
            // init list if not exist
            const tagsStore = useTagsStore();
            if (!this.tagsStore[idStore]) {
                this.tagsStore[idStore] = {};
            }
            this.tagsStore[idStore][id] = { loading: true };
            const expandString = expand.join(',');
            this.tagsStore[idStore][id] = await fetchWrapper.get({
                url: `${baseUrl}/store/${idStore}/tag/${id}?expand=${expandString}`,
                useToken: "access"
            });
            if (expand.indexOf("tag") > -1) {
                tagsStore.tags[id] = this.tagsStore[idStore][id].tag;
            }
        },
        async createTagStore(idStore, params) {
            this.tagStoreEdition = { loading: true };
            this.tagStoreEdition = await fetchWrapper.post({
                url: `${baseUrl}/store/${idStore}/tag`,
                useToken: "access",
                body: params
            });
            if (!this.tagsStore[idStore]) {
                this.tagsStore[idStore] = {};
            }
            this.tagsStore[idStore][this.tagStoreEdition.id_tag] = this.tagStoreEdition;
        },
        async deleteTagStore(idStore, id) {
            this.tagStoreEdition = { loading: true };
            this.tagStoreEdition = await fetchWrapper.delete({
                url: `${baseUrl}/store/${idStore}/tag/${id}`,
                useToken: "access"
            });
            delete this.tagsStore[idStore][id];
        },
        async createTagStoreBulk(idStore, params) {
            this.tagStoreEdition = { loading: true };
            this.tagStoreEdition = await fetchWrapper.post({
                url: `${baseUrl}/store/${idStore}/tag/bulk`,
                useToken: "access",
                body: params
            });
            if (!this.tagsStore[idStore]) {
                this.tagsStore[idStore] = {};
            }
            for (const tag of this.tagStoreEdition["valide"]) {
                this.tagsStore[idStore][tag.id_tag] = tag;
            }
        },
        async deleteTagStoreBulk(idStore, params) {
            this.tagStoreEdition = { loading: true };
            this.tagStoreEdition = await fetchWrapper.delete({
                url: `${baseUrl}/store/${idStore}/tag/bulk`,
                useToken: "access",
                body: params
            });
            for (const id of this.tagStoreEdition["valide"]) {
                delete this.tagsStore[idStore][id];
            }
        },

        async getBoxItemByList(idBox, idResearch = [], expand = []) {
            // init list if not exist
            const itemsStore = useItemsStore();
            if (!this.boxItems[idBox]) {
                this.boxItems[idBox] = {};
            }
            this.boxItemsLoading = true;
            const idResearchString = idResearch.join(',');
            const expandString = expand.join(',');
            let newItemList = await fetchWrapper.get({
                url: `${baseUrl}/box/${idBox}/item?&idResearch=${idResearchString}&expand=${expandString}`,
                useToken: "access"
            });
            for (const item of newItemList['data']) {
                this.boxItems[idBox][item.id_item] = item;
                if (expand.indexOf("item") > -1) {
                    itemsStore.items[item.id_item] = item.item;
                }
            }
            this.boxItemsTotalCount[idBox] = newItemList['count'];
            this.boxItemsLoading = false;
        },
        async getBoxItemByInterval(idBox, limit = 100, offset = 0, expand = []) {
            // init list if not exist
            const itemsStore = useItemsStore();
            if (!this.boxItems[idBox]) {
                this.boxItems[idBox] = {};
            }
            this.boxItemsLoading = true;
            const expandString = expand.join(',');
            let newItemList = await fetchWrapper.get({
                url: `${baseUrl}/box/${idBox}/item?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const item of newItemList['data']) {
                this.boxItems[idBox][item.id_item] = item;
                if (expand.indexOf("item") > -1) {
                    itemsStore.items[item.id_item] = item.item;
                }
            }
            this.boxItemsTotalCount = newItemList['count'];
            this.boxItemsLoading = false;
        },
        async getBoxItemById(idBox, id, expand = []) {
            // init list if not exist
            const itemsStore = useItemsStore();
            if (!this.boxItems[idBox]) {
                this.boxItems[idBox] = {};
            }
            this.boxItems[idBox][id] = { loading: true };
            const expandString = expand.join(',');
            this.boxItems[idBox][id] = await fetchWrapper.get({
                url: `${baseUrl}/box/${idBox}/item/${id}?expand=${expandString}`,
                useToken: "access"
            });
            if (expand.indexOf("item") > -1) {
                itemsStore.items[id] = this.boxItems[idBox][id].item;
            }
        },
        async createBoxItem(idBox, params) {
            this.boxItemEdition = { loading: true };
            this.boxItemEdition = await fetchWrapper.post({
                url: `${baseUrl}/box/${idBox}/item`,
                useToken: "access",
                body: params
            });
            if (!this.boxItems[idBox]) {
                this.boxItems[idBox] = {};
            }
            this.boxItems[idBox][this.boxItemEdition.id_item] = this.boxItemEdition;
        },
        async updateBoxItem(idBox, id, params) {
            this.boxItemEdition = { loading: true };
            this.boxItemEdition = await fetchWrapper.put({
                url: `${baseUrl}/box/${idBox}/item/${id}`,
                useToken: "access",
                body: params
            });
            this.boxItems[idBox][id] = this.boxItemEdition;
        },
        async deleteBoxItem(idBox, id) {
            this.boxItemEdition = { loading: true };
            this.boxItemEdition = await fetchWrapper.delete({
                url: `${baseUrl}/box/${idBox}/item/${id}`,
                useToken: "access"
            });
            delete this.boxItems[idBox][id];
        },

        async getBoxTagByInterval(idBox, limit = 100, offset = 0, expand = []) {
            // init list if not exist
            const tagsStore = useTagsStore();
            if (!this.boxTags[idBox]) {
                this.boxTags[idBox] = {};
            }
            this.boxTagsLoading = true;
            const expandString = expand.join(',');
            let newTagList = await fetchWrapper.get({
                url: `${baseUrl}/box/${idBox}/tag?limit=${limit}&offset=${offset}&expand=${expandString}`,
                useToken: "access"
            });
            for (const tag of newTagList['data']) {
                this.boxTags[idBox][tag.id_tag] = tag;
                if (expand.indexOf("tag") > -1) {
                    tagsStore.tags[tag.id_tag] = tag.tag;
                }
            }
            this.boxTagsTotalCount = newTagList['count'];
            this.boxTagsLoading = false;
        },
        async getBoxTagById(idBox, id, expand = []) {
            // init list if not exist
            const tagsStore = useTagsStore();
            if (!this.boxTags[idBox]) {
                this.boxTags[idBox] = {};
            }
            this.boxTags[idBox][id] = { loading: true };
            const expandString = expand.join(',');
            this.boxTags[idBox][id] = await fetchWrapper.get({
                url: `${baseUrl}/box/${idBox}/tag/${id}?expand=${expandString}`,
                useToken: "access"
            });
            if (expand.indexOf("tag") > -1) {
                tagsStore.tags[id] = this.boxTags[idBox][id].tag;
            }
        },
        async createBoxTag(idBox, params) {
            this.boxTagEdition = { loading: true };
            this.boxTagEdition = await fetchWrapper.post({
                url: `${baseUrl}/box/${idBox}/tag`,
                useToken: "access",
                body: params
            });
            if (!this.boxTags[idBox]) {
                this.boxTags[idBox] = {};
            }
            this.boxTags[idBox][this.boxTagEdition.id_tag] = this.boxTagEdition;
        },
        async deleteBoxTag(idBox, id) {
            this.boxTagEdition = { loading: true };
            this.boxTagEdition = await fetchWrapper.delete({
                url: `${baseUrl}/box/${idBox}/tag/${id}`,
                useToken: "access"
            });
            delete this.boxTags[idBox][id];
        },
        async createBoxTagBulk(idBox, params) {
            this.boxTagEdition = { loading: true };
            this.boxTagEdition = await fetchWrapper.post({
                url: `${baseUrl}/box/${idBox}/tag/bulk`,
                useToken: "access",
                body: params
            });
            if (!this.boxTags[idBox]) {
                this.boxTags[idBox] = {};
            }
            for (const tag of this.boxTagEdition["valide"]) {
                this.boxTags[idBox][tag.id_tag] = tag;
            }
        },
        async deleteBoxTagBulk(idBox, params) {
            this.boxTagEdition = { loading: true };
            this.boxTagEdition = await fetchWrapper.delete({
                url: `${baseUrl}/box/${idBox}/tag/bulk`,
                useToken: "access",
                body: params
            });
            for (const id of this.boxTagEdition["valide"]) {
                delete this.boxTags[idBox][id];
            }
        }
    }
});

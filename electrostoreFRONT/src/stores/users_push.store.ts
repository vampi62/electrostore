import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import type { components } from "@/types/api";

type ReadUserPushSubscriptionDto = components["schemas"]["ReadUserPushSubscriptionDto"];
type CreateUserPushSubscriptionDto = components["schemas"]["CreateUserPushSubscriptionDtoByUserId"];

const userPushSubscriptionResource = createResource<{ readBasic: ReadUserPushSubscriptionDto, create: CreateUserPushSubscriptionDto }>({
	compositeKey: ["id_user", "id_user_push_subscription"],
	path: [
		{ keys: ["id_user"], url: ({ id_user }) => `/user/${id_user}/push-subscriptions` },
	],
	entityPath: [
		{ keys: ["id_user", "id_user_push_subscription"], url: ({ id_user, id_user_push_subscription }) => `/user/${id_user}/push-subscriptions/${id_user_push_subscription}` },
	],
	stateKey: "userPushSubscriptions",
	countKey: "userPushSubscriptionsTotalCount",
	loadingKey: "userPushSubscriptionsLoading",
});

// Records are indexed by the composite key "<id_user>:<id_user_push_subscription>"
export const useUsersPushStore = defineStore("usersPush", {
	state: () => ({
		userPushSubscriptionsLoading: false,
		// indexed by scope: "id_user=3"
		userPushSubscriptionsTotalCount: {} as Record<string, number>,
		userPushSubscriptions: {} as Record<string, ReadUserPushSubscriptionDto>,
	}),
	getters: {
		// the records of a user, indexed by the rest of the key
		userPushSubscriptionsByUser: (state) => (id: string) => userPushSubscriptionResource.slice(state.userPushSubscriptions, { id_user: id }),
		userPushSubscriptionsCountByUser: (state) => (id: string) => state.userPushSubscriptionsTotalCount[userPushSubscriptionResource.toScopeKey({ id_user: id })] ?? 0,
	},
	actions: {
		getUserPushSubscriptionByInterval: userPushSubscriptionResource.getByInterval,
		getUserPushSubscriptionById: userPushSubscriptionResource.getById,
		createUserPushSubscription: userPushSubscriptionResource.create,
		deleteUserPushSubscription: userPushSubscriptionResource.remove,
		hydrateUserPushSubscriptions: userPushSubscriptionResource.hydrate,
	},
});

import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import type { components } from "@/types/api";

type SessionDto = components["schemas"]["SessionDto"];
type UpdateAccessTokenDto = components["schemas"]["UpdateAccessTokenDto"];

const userSessionResource = createResource<{ readBasic: SessionDto, update: UpdateAccessTokenDto }>({
	compositeKey: ["id_user", "session_id"],
	path: [
		{ keys: ["id_user"], url: ({ id_user }) => `/user/${id_user}/sessions` },
	],
	entityPath: [
		{ keys: ["id_user", "session_id"], url: ({ id_user, session_id }) => `/user/${id_user}/sessions/${session_id}` },
	],
	stateKey: "userSessions",
	countKey: "userSessionsTotalCount",
	loadingKey: "userSessionsLoading",
});

// Records are indexed by the composite key "<id_user>:<session_id>"
export const useUsersSessionsStore = defineStore("usersSessions", {
	state: () => ({
		userSessionsLoading: false,
		// indexed by scope: "id_user=3"
		userSessionsTotalCount: {} as Record<string, number>,
		userSessions: {} as Record<string, SessionDto>,
	}),
	getters: {
		// the records of a user, indexed by the rest of the key
		userSessionsByUser: (state) => (id: string) => userSessionResource.slice(state.userSessions, { id_user: id }),
		userSessionsCountByUser: (state) => (id: string) => state.userSessionsTotalCount[userSessionResource.toScopeKey({ id_user: id })] ?? 0,
	},
	actions: {
		getUserSessionByInterval: userSessionResource.getByInterval,
		getUserSessionById: userSessionResource.getById,
		updateUserSession: userSessionResource.update,
		hydrateUserSessions: userSessionResource.hydrate,
	},
});

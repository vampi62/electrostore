import { defineStore } from "pinia";

import { fetchWrapper, createResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useCommandsCommentsStore, useProjectsCommentsStore, useUsersSessionsStore, useUsersPushStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Edition } from "@/types/resource";
import type { components } from "@/types/api";
type ReadUserDto = components["schemas"]["ReadUserDto"];
type ReadExtendedUserDto = components["schemas"]["ReadExtendedUserDto"];
type CreateUserDto = components["schemas"]["CreateUserDto"];
type UpdateUserDto = components["schemas"]["UpdateUserDto"];

type SessionDto = components["schemas"]["SessionDto"];
type ReadUserPushSubscriptionDto = components["schemas"]["ReadUserPushSubscriptionDto"];

// the user is expanded with sessions and push subscriptions, which are not part of the generated extended dto
type ExpandedUserDto = ReadExtendedUserDto & {
	sessions?: SessionDto[] | null;
	push_subscriptions?: ReadUserPushSubscriptionDto[] | null;
};
// the edition form also carries the password confirmation, which is never sent to the api
// unset_check is the checkbox of the form that enables the password fields
type UserEdition = Edition<{ create: CreateUserDto, update: UpdateUserDto }> & { confirm_password_user?: string; unset_check?: boolean };

const baseUrl = `${import.meta.env.VITE_API_URL}`;

// What an expanded user brings along is handed to the store of each nested resource
const EXPAND_HANDLERS: Record<string, (idUser: string, data: ExpandedUserDto) => void> = {
	project_comments: (idUser, data) => useProjectsCommentsStore().hydrateProjectComments({ id_user: idUser }, data.project_comments ?? []),
	command_comments: (idUser, data) => useCommandsCommentsStore().hydrateCommandComments({ id_user: idUser }, data.command_comments ?? []),
	tokens: (idUser, data) => useUsersSessionsStore().hydrateUserSessions({ id_user: idUser }, (data.sessions ?? []).map((session) => ({ ...session, id_user: session.id_user ?? Number(idUser) }))),
	push_subscriptions: (idUser, data) => useUsersPushStore().hydrateUserPushSubscriptions({ id_user: idUser }, data.push_subscriptions ?? []),
};

function hydrateUser(idUser: string, user: ExpandedUserDto, expand: string[] = []) {
	const scope = { id_user: idUser };
	useProjectsCommentsStore().hydrateProjectComments(scope, undefined, user.project_comments_count);
	useCommandsCommentsStore().hydrateCommandComments(scope, undefined, user.command_comments_count);
	for (const key of expand) {
		EXPAND_HANDLERS[key]?.(idUser, user);
	}
}

const userResource = createResource<{ readExtended: ExpandedUserDto, readBasic: ReadUserDto, create: CreateUserDto, update: UpdateUserDto }>({
	compositeKey: ["id_user"],
	generatedField: "id_user",
	path: [{ keys: [], url: () => "/user" }],
	entityPath: [{ keys: ["id_user"], url: ({ id_user }) => `/user/${id_user}` }],
	stateKey: "users",
	countKey: "usersTotalCount",
	loadingKey: "usersLoading",
	editionKey: "userEdition",
	onHydrate: (store: StoreGeneric, entity: ExpandedUserDto, expand) => {
		if (entity.id_user === undefined) {
			return;
		}
		hydrateUser(String(entity.id_user), entity, expand);
	},
});

export const useUsersStore = defineStore("users", {
	state: () => ({
		usersLoading: false,
		usersTotalCount: 0,
		users: {} as Record<string, ReadUserDto>,
		userEdition: {} as Record<string, UserEdition>,
	}),
	actions: {
		getUserByInterval: userResource.single.getByInterval,
		getUserById: userResource.single.getById,
		createUser: userResource.single.create,
		getAvailableNewUserId: userResource.single.getAvailableNewId,
		updateUser: userResource.single.update,
		deleteUser: userResource.single.remove,
		loadToEdition(id: string, preset: string | null = null) {
			if (!isNewId(id) && this.users[id]) {
				this.userEdition[id] = {
					loading: false,
					name_user: this.users[id].name_user ?? undefined,
					firstname_user: this.users[id].firstname_user ?? undefined,
					email_user: this.users[id].email_user ?? undefined,
					role_user: this.users[id].role_user,
					current_password_user: "",
					password_user: "",
					confirm_password_user: "",
				};
			} else {
				this.userEdition[id] = {
					loading: false,
				};
				userResource.single.loadEditionPreset.call(this, id, preset);
			}
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.userEdition[id]) {
				this.userEdition[id] = {};
			}
			this.userEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.userEdition[id];
		},
		async saveAllChanges(id: string) {
			let realId = id;
			if (isNewId(id)) {
				realId = await this.createUser(this.userEdition[id] as CreateUserDto);
			} else {
				await this.updateUser(id, this.userEdition[id]);
			}
			return realId;
		},

		async sendTestPushNotification(id: string) {
			await fetchWrapper.post({
				url: `${baseUrl}/user/${id}/push-subscriptions/testPush`,
				useToken: "access",
			});
		},
		async sendTestEmailNotification(id: string) {
			await fetchWrapper.post({
				url: `${baseUrl}/user/${id}/push-subscriptions/testEmail`,
				useToken: "access",
			});
		},
	},
});

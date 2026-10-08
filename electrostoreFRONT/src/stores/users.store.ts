import { defineStore } from "pinia";

import { fetchWrapper, createMainResource, createNestedResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useCommandsStore, useProjectsStore, useEquipementsStore } from "@/stores";

import type { components } from "@/types/api";
type ReadUserDto = components["schemas"]["ReadUserDto"];
type ReadCommandCommentDto = components["schemas"]["ReadCommandCommentDto"];
type ReadProjectCommentDto = components["schemas"]["ReadProjectCommentDto"];
type ReadEquipementCommentDto = components["schemas"]["ReadEquipementCommentDto"];
type SessionDto = components["schemas"]["SessionDto"];
type ReadUserPushSubscriptionDto = components["schemas"]["ReadUserPushSubscriptionDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS: Record<string, (store: any, idUser: any, data: any) => void> = {
	project_comments: (store, idUser, user) => {
		store.projectsComment[idUser] = {};
		for (const projectComment of user.project_comments) {
			store.projectsComment[idUser][projectComment.id_project] = projectComment;
		}
	},
	command_comments: (store, idUser, user) => {
		store.commandsComment[idUser] = {};
		for (const commandComment of user.command_comments) {
			store.commandsComment[idUser][commandComment.id_command] = commandComment;
		}
	},
	tokens: (store, idUser, user) => {
		store.tokens[idUser] = {};
		for (const token of user.sessions) {
			store.tokens[idUser][token.session_id] = token;
		}
	},
	push_subscriptions: (store, idUser, user) => {
		store.pushSubscriptions[idUser] = {};
		for (const sub of user.push_subscriptions) {
			store.pushSubscriptions[idUser][sub.id_user_push_subscription] = sub;
		}
	},
};

function hydrateUser(store: any, idUser: string, user: any, expand: string[] = []) {
	store.projectsCommentTotalCount[idUser] = user.project_comments_count;
	store.commandsCommentTotalCount[idUser] = user.command_comments_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idUser, user);
		}
	}
}

const userResource = createMainResource({
	path: () => "/user",
	idField: "id_user",
	stateKey: "users",
	countKey: "usersTotalCount",
	loadingKey: "usersLoading",
	editionKey: "userEdition",
	onHydrate: (store, entity, expand) => {
		hydrateUser(store, entity.id_user, entity, expand);
	},
});

const projectCommentResource = createNestedResource({
	path: (idUser) => `/user/${idUser}/project_comment`,
	idField: "id_project_comment",
	stateKey: "projectsComment",
	countKey: "projectsCommentTotalCount",
	loadingKey: "projectsCommentLoading",
	editionKey: "projectCommentEdition",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("project")) {
			const projectStore = useProjectsStore();
			projectStore.projects[entity.project.id_project] = entity.project;
		}
	},
});
const commandCommentResource = createNestedResource({
	path: (idUser) => `/user/${idUser}/command_comment`,
	idField: "id_command_comment",
	stateKey: "commandsComment",
	countKey: "commandsCommentTotalCount",
	loadingKey: "commandsCommentLoading",
	editionKey: "commandCommentEdition",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("command")) {
			const commandStore = useCommandsStore();
			commandStore.commands[entity.command.id_command] = entity.command;
		}
	},
});
const tokenResource = createNestedResource({
	path: (idUser) => `/user/${idUser}/sessions`,
	idField: "session_id",
	stateKey: "tokens",
	countKey: "tokensTotalCount",
	loadingKey: "tokensLoading",
});
const pushSubscriptionResource = createNestedResource({
	path: (idUser) => `/user/${idUser}/push-subscriptions`,
	idField: "id_user_push_subscription",
	stateKey: "pushSubscriptions",
	countKey: "pushSubscriptionsTotalCount",
	loadingKey: "pushSubscriptionsLoading",
});
const equipementCommentResource = createNestedResource({
	path: (idUser) => `/user/${idUser}/equipement_comment`,
	idField: "id_equipement_comment",
	stateKey: "equipementsComment",
	countKey: "equipementsCommentTotalCount",
	loadingKey: "equipementsCommentLoading",
	editionKey: "equipementCommentEdition",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("equipement")) {
			const equipementsStore = useEquipementsStore();
			equipementsStore.equipements[entity.equipement.id_equipement] = entity.equipement;
		}
	},
});

export const useUsersStore = defineStore("users",{
	state: () => ({
		usersLoading: false,
		usersTotalCount: 0,
		users: {} as Record<string, ReadUserDto>,
		userEdition: {} as Record<string, any>,

		projectsCommentLoading: false,
		projectsCommentTotalCount: {} as Record<string, number>,
		projectsComment: {} as Record<string, ReadProjectCommentDto>,
		projectCommentEdition: {} as Record<string, any>,

		commandsCommentLoading: false,
		commandsCommentTotalCount: {} as Record<string, number>,
		commandsComment: {} as Record<string, ReadCommandCommentDto>,
		commandCommentEdition: {} as Record<string, any>,

		tokensLoading: false,
		tokensTotalCount: {} as Record<string, number>,
		tokens: {} as Record<string, SessionDto>,
		tokensEdition: {} as Record<string, any>,

		pushSubscriptionsLoading: false,
		pushSubscriptionsTotalCount: {} as Record<string, number>,
		pushSubscriptions: {} as Record<string, ReadUserPushSubscriptionDto>,

		equipementsCommentLoading: false,
		equipementsCommentTotalCount: {} as Record<string, number>,
		equipementsComment: {} as Record<string, ReadEquipementCommentDto>,
		equipementCommentEdition: {} as Record<string, any>,
	}),
	actions: {
		getUserByList: userResource.getByList,
		getUserByInterval: userResource.getByInterval,
		getUserById: userResource.getById,
		createUser: userResource.create,
		getAvailableNewUserId: userResource.getAvailableNewId,
		updateUser: userResource.update,
		deleteUser: userResource.remove,
		loadToEdition(id: string, preset = null) {
			if (!isNewId(id) && this.users[id]) {
				this.userEdition[id] = {
					loading: false,
					id_user: this.users[id].id_user,
					name_user: this.users[id].name_user,
					firstname_user: this.users[id].firstname_user,
					email_user: this.users[id].email_user,
					role_user: this.users[id].role_user,
					current_password_user: "",
					password_user: "",
					confirm_password_user: "",
				};
			} else {
				this.userEdition[id] = {
					loading: false,
				};
				userResource.loadEditionPreset.call(this, id, preset);
			}
			this.projectCommentEdition[id] = {};
			this.commandCommentEdition[id] = {};
			this.tokensEdition[id] = {};
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.userEdition[id]) {
				this.userEdition[id] = {};
			}
			this.userEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.userEdition[id];
			delete this.projectCommentEdition[id];
			delete this.commandCommentEdition[id];
			delete this.tokensEdition[id];
		},
		async saveAllChanges(id: string) {
			let realId = id;
			if (isNewId(id)) {
				realId = await this.createUser(this.userEdition[id]);
				this.copyProjectCommentAllId(id, realId);
				this.copyCommandCommentAllId(id, realId);
			} else {
				await this.updateUser(id, this.userEdition[id]);
			}
			await Promise.all([
				this.pushProjectCommentChange(realId),
				this.pushCommandCommentChange(realId),
			]);
			return realId;
		},

		getProjectCommentByInterval: projectCommentResource.getByInterval,
		getProjectCommentById: projectCommentResource.getById,
		createProjectComment: projectCommentResource.create,
		updateProjectComment: projectCommentResource.update,
		deleteProjectComment: projectCommentResource.remove,
		getAvailableNewProjectCommentId: projectCommentResource.getAvailableNewId,
		valideProjectCommentEditionById: projectCommentResource.valideEditionById,
		copyProjectCommentPerId: projectCommentResource.copyPerId,
		copyProjectCommentAllId: projectCommentResource.copyAllId,
		pushProjectCommentChange: projectCommentResource.pushChange,

		getCommandCommentByInterval: commandCommentResource.getByInterval,
		getCommandCommentById: commandCommentResource.getById,
		createCommandComment: commandCommentResource.create,
		updateCommandComment: commandCommentResource.update,
		deleteCommandComment: commandCommentResource.remove,
		getAvailableNewCommandCommentId: commandCommentResource.getAvailableNewId,
		valideCommandCommentEditionById: commandCommentResource.valideEditionById,
		copyCommandCommentPerId: commandCommentResource.copyPerId,
		copyCommandCommentAllId: commandCommentResource.copyAllId,
		pushCommandCommentChange: commandCommentResource.pushChange,

		getTokenByInterval: tokenResource.getByInterval,
		getTokenById: tokenResource.getById,
		updateToken: tokenResource.update,

		getPushSubscriptionsByInterval: pushSubscriptionResource.getByInterval,
		createPushSubscription: pushSubscriptionResource.create,
		deletePushSubscription: pushSubscriptionResource.remove,
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

		getEquipementCommentByInterval: equipementCommentResource.getByInterval,
		getEquipementCommentById: equipementCommentResource.getById,
	},
});

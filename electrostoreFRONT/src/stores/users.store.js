import { defineStore } from "pinia";

import { fetchWrapper, createMainResource, createNestedResource } from "@/helpers";

import { useCommandsStore, useProjectsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
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

function hydrateUser(store, idUser, user, expand = []) {
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
	readyKey: "projectCommentReady",
	onHydrate: (store, idUser, entity, expand) => {
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
	readyKey: "commandCommentReady",
	onHydrate: (store, idUser, entity, expand) => {
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

export const useUsersStore = defineStore("users",{
	state: () => ({
		usersLoading: false,
		usersTotalCount: 0,
		users: {},
		userEdition: {},

		projectsCommentLoading: false,
		projectsCommentTotalCount: {},
		projectsComment: {},
		projectCommentEdition: {},
		projectCommentReady: {},

		commandsCommentLoading: false,
		commandsCommentTotalCount: {},
		commandsComment: {},
		commandCommentEdition: {},
		commandCommentReady: {},

		tokensLoading: false,
		tokensTotalCount: {},
		tokens: {},
		tokensEdition: {},

		pushSubscriptionsLoading: false,
		pushSubscriptionsTotalCount: {},
		pushSubscriptions: {},
	}),
	actions: {
		getUserByList: userResource.getByList,
		getUserByInterval: userResource.getByInterval,
		getUserById: userResource.getById,
		createUser: userResource.create,
		updateUser: userResource.update,
		deleteUser: userResource.remove,
		loadToEdition(id, preset = null) {
			this.userEdition[id] = {};
			if (preset) {
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.userEdition[id][key] = value;
					}
				});
			}
			if (id !== "new" && this.users[id]) {
				this.userEdition[id] = {
					loading: false,
					id_user: this.users[id].id_user,
					name_user: this.users[id].name_user,
					firstname_user: this.users[id].firstname_user,
					email_user: this.users[id].email_user,
					role_user: this.users[id].role_user,
					current_password_user: "",
					password_user: "",
					confirm_mdp_user: "",
				};
			} else {
				this.userEdition[id] = {
					loading: false,
				};
			}
			this.projectCommentEdition[id] = {};
			this.projectCommentReady[id] = {};
			this.commandCommentEdition[id] = {};
			this.commandCommentReady[id] = {};
			this.tokensEdition[id] = {};
			this.pushSubscriptionsEdition[id] = {};
		},
		setLoadingEdition(id, loading) {
			if (!this.userEdition[id]) {
				this.userEdition[id] = {};
			}
			this.userEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.userEdition[id];
			delete this.projectCommentEdition[id];
			delete this.projectCommentReady[id];
			delete this.commandCommentEdition[id];
			delete this.commandCommentReady[id];
			delete this.tokensEdition[id];
			delete this.pushSubscriptionsEdition[id];
		},
		async saveAllChanges(id) {
			let realId = id;
			if (id === "new") {
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
		async sendTestPushNotification(idUser) {
			await fetchWrapper.post({
				url: `${baseUrl}/user/${idUser}/push-subscriptions/testPush`,
				useToken: "access",
			});
		},
		async sendTestEmailNotification(idUser) {
			await fetchWrapper.post({
				url: `${baseUrl}/user/${idUser}/push-subscriptions/testEmail`,
				useToken: "access",
			});
		},
	},
});

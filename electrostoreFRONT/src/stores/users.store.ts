import { defineStore } from "pinia";

import { fetchWrapper, createMainResource, createNestedResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useCommandsStore, useProjectsStore, useEquipementsStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Edition } from "@/types/resource";
import type { components } from "@/types/api";
type ReadUserDto = components["schemas"]["ReadUserDto"];
type ReadExtendedUserDto = components["schemas"]["ReadExtendedUserDto"];
type CreateUserDto = components["schemas"]["CreateUserDto"];
type UpdateUserDto = components["schemas"]["UpdateUserDto"];

type ReadProjectCommentDto = components["schemas"]["ReadProjectCommentDto"];
type ReadExtendedProjectCommentDto = components["schemas"]["ReadExtendedProjectCommentDto"];
type CreateProjectCommentByProjectDto = components["schemas"]["CreateProjectCommentByProjectDto"];
type UpdateProjectCommentDto = components["schemas"]["UpdateProjectCommentDto"];

type ReadCommandCommentDto = components["schemas"]["ReadCommandCommentDto"];
type ReadExtendedCommandCommentDto = components["schemas"]["ReadExtendedCommandCommentDto"];
type CreateCommandCommentByCommandDto = components["schemas"]["CreateCommandCommentByCommandDto"];
type UpdateCommandCommentDto = components["schemas"]["UpdateCommandCommentDto"];

type ReadEquipementCommentDto = components["schemas"]["ReadEquipementCommentDto"];
type ReadExtendedEquipementCommentDto = components["schemas"]["ReadExtendedEquipementCommentDto"];
type CreateEquipementCommentByEquipementDto = components["schemas"]["CreateEquipementCommentByEquipementDto"];
type UpdateEquipementCommentDto = components["schemas"]["UpdateEquipementCommentDto"];

type SessionDto = components["schemas"]["SessionDto"];
type UpdateAccessTokenDto = components["schemas"]["UpdateAccessTokenDto"];

type ReadUserPushSubscriptionDto = components["schemas"]["ReadUserPushSubscriptionDto"];
type CreateUserPushSubscriptionDto = components["schemas"]["CreateUserPushSubscriptionDtoByUserId"];

// the user is expanded with sessions and push subscriptions, which are not part of the generated extended dto
type ExpandedUserDto = ReadExtendedUserDto & {
	sessions?: SessionDto[] | null;
	push_subscriptions?: ReadUserPushSubscriptionDto[] | null;
};
// the edition form also carries the password confirmation, which is never sent to the api
// unset_check is the checkbox of the form that enables the password fields
type UserEdition = Edition<{ create: CreateUserDto, update: UpdateUserDto }> & { confirm_password_user?: string; unset_check?: boolean };

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS: Record<string, (store: StoreGeneric, idUser: string, data: ExpandedUserDto) => void> = {
	project_comments: (store, idUser, user) => {
		store.projectsComment[idUser] = {};
		for (const projectComment of user.project_comments ?? []) {
			if (!projectComment.id_project) {
				continue;
			}
			store.projectsComment[idUser][projectComment.id_project] = projectComment;
		}
	},
	command_comments: (store, idUser, user) => {
		store.commandsComment[idUser] = {};
		for (const commandComment of user.command_comments ?? []) {
			if (!commandComment.id_command) {
				continue;
			}
			store.commandsComment[idUser][commandComment.id_command] = commandComment;
		}
	},
	tokens: (store, idUser, user) => {
		store.tokens[idUser] = {};
		for (const token of user.sessions ?? []) {
			if (!token.session_id) {
				continue;
			}
			store.tokens[idUser][token.session_id] = token;
		}
	},
	push_subscriptions: (store, idUser, user) => {
		store.pushSubscriptions[idUser] = {};
		for (const sub of user.push_subscriptions ?? []) {
			if (!sub.id_user_push_subscription) {
				continue;
			}
			store.pushSubscriptions[idUser][sub.id_user_push_subscription] = sub;
		}
	},
};

function hydrateUser(store: StoreGeneric, idUser: string, user: ExpandedUserDto, expand: string[] = []) {
	store.projectsCommentTotalCount[idUser] = user.project_comments_count;
	store.commandsCommentTotalCount[idUser] = user.command_comments_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idUser, user);
		}
	}
}

const userResource = createMainResource<{ readExtended: ExpandedUserDto, readBasic: ReadUserDto, create: CreateUserDto, update: UpdateUserDto }>({
	path: () => "/user",
	idField: "id_user",
	stateKey: "users",
	countKey: "usersTotalCount",
	loadingKey: "usersLoading",
	editionKey: "userEdition",
	onHydrate: (store, entity: ExpandedUserDto, expand) => {
		if (entity.id_user === undefined) {
			return;
		}
		hydrateUser(store, String(entity.id_user), entity, expand);
	},
});

const projectCommentResource = createNestedResource<{ readExtended: ReadExtendedProjectCommentDto, readBasic: ReadProjectCommentDto, create: CreateProjectCommentByProjectDto, update: UpdateProjectCommentDto }>({
	path: (idUser) => `/user/${idUser}/project_comment`,
	idField: "id_project_comment",
	stateKey: "projectsComment",
	countKey: "projectsCommentTotalCount",
	loadingKey: "projectsCommentLoading",
	editionKey: "projectCommentEdition",
	onHydrate: (store, entity: ReadExtendedProjectCommentDto, expand) => {
		if (expand.includes("project") && entity.project?.id_project) {
			const projectStore = useProjectsStore();
			projectStore.projects[entity.project.id_project] = entity.project;
		}
	},
});
const commandCommentResource = createNestedResource<{ readExtended: ReadExtendedCommandCommentDto, readBasic: ReadCommandCommentDto, create: CreateCommandCommentByCommandDto, update: UpdateCommandCommentDto }>({
	path: (idUser) => `/user/${idUser}/command_comment`,
	idField: "id_command_comment",
	stateKey: "commandsComment",
	countKey: "commandsCommentTotalCount",
	loadingKey: "commandsCommentLoading",
	editionKey: "commandCommentEdition",
	onHydrate: (store, entity: ReadExtendedCommandCommentDto, expand) => {
		if (expand.includes("command") && entity.command?.id_command) {
			const commandStore = useCommandsStore();
			commandStore.commands[entity.command.id_command] = entity.command;
		}
	},
});
const tokenResource = createNestedResource<{ readBasic: SessionDto, update: UpdateAccessTokenDto }>({
	path: (idUser) => `/user/${idUser}/sessions`,
	idField: "session_id",
	stateKey: "tokens",
	countKey: "tokensTotalCount",
	loadingKey: "tokensLoading",
});
const pushSubscriptionResource = createNestedResource<{ readBasic: ReadUserPushSubscriptionDto, create: CreateUserPushSubscriptionDto }>({
	path: (idUser) => `/user/${idUser}/push-subscriptions`,
	idField: "id_user_push_subscription",
	stateKey: "pushSubscriptions",
	countKey: "pushSubscriptionsTotalCount",
	loadingKey: "pushSubscriptionsLoading",
});
const equipementCommentResource = createNestedResource<{ readExtended: ReadExtendedEquipementCommentDto, readBasic: ReadEquipementCommentDto, create: CreateEquipementCommentByEquipementDto, update: UpdateEquipementCommentDto }>({
	path: (idUser) => `/user/${idUser}/equipement_comment`,
	idField: "id_equipement_comment",
	stateKey: "equipementsComment",
	countKey: "equipementsCommentTotalCount",
	loadingKey: "equipementsCommentLoading",
	editionKey: "equipementCommentEdition",
	onHydrate: (store, entity: ReadExtendedEquipementCommentDto, expand) => {
		if (expand.includes("equipement") && entity.equipement?.id_equipement) {
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
		userEdition: {} as Record<string, UserEdition>,

		projectsCommentLoading: false,
		projectsCommentTotalCount: {} as Record<string, number>,
		projectsComment: {} as Record<string, Record<string, ReadProjectCommentDto>>,
		projectCommentEdition: {} as Record<string, Record<string, Edition<{ create: CreateProjectCommentByProjectDto, update: UpdateProjectCommentDto }>>>,

		commandsCommentLoading: false,
		commandsCommentTotalCount: {} as Record<string, number>,
		commandsComment: {} as Record<string, Record<string, ReadCommandCommentDto>>,
		commandCommentEdition: {} as Record<string, Record<string, Edition<{ create: CreateCommandCommentByCommandDto, update: UpdateCommandCommentDto }>>>,

		tokensLoading: false,
		tokensTotalCount: {} as Record<string, number>,
		tokens: {} as Record<string, Record<string, SessionDto>>,
		tokensEdition: {} as Record<string, Record<string, Edition<{ update: UpdateAccessTokenDto }>>>,

		pushSubscriptionsLoading: false,
		pushSubscriptionsTotalCount: {} as Record<string, number>,
		pushSubscriptions: {} as Record<string, Record<string, ReadUserPushSubscriptionDto>>,

		equipementsCommentLoading: false,
		equipementsCommentTotalCount: {} as Record<string, number>,
		equipementsComment: {} as Record<string, Record<string, ReadEquipementCommentDto>>,
		equipementCommentEdition: {} as Record<string, Record<string, Edition<{ create: CreateEquipementCommentByEquipementDto, update: UpdateEquipementCommentDto }>>>,
	}),
	actions: {
		getUserByList: userResource.getByList,
		getUserByInterval: userResource.getByInterval,
		getUserById: userResource.getById,
		createUser: userResource.create,
		getAvailableNewUserId: userResource.getAvailableNewId,
		updateUser: userResource.update,
		deleteUser: userResource.remove,
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
				realId = await this.createUser(this.userEdition[id] as CreateUserDto);
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

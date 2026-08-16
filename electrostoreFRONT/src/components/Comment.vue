<template>
	<Form v-if="meta.canEdit" :validation-schema="schemaComment" v-slot="{ errors }">
		<div class="flex items-center space-x-4">
			<Field :name="meta.contenu" type="text" v-model="commentFormNew"
				:placeholder="$t('components.VModalCommentPlaceholder')"
				class="w-full p-2 border rounded-lg"
				:class="{ 'border-red-500': errors[meta.contenu] }" />
			<div class="relative">
				<button type="button" @click="commentCreate(commentFormNew)"
					class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600"
					:disabled="createLoading">
					{{ $t('components.VModalCommentAdd') }}
				</button>
				<div v-if="createLoading"
					class="absolute inset-0 bg-blue-500 bg-opacity-90 rounded-lg flex items-center justify-center">
					<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
				</div>
			</div>
		</div>
	</Form>
	<div class="space-y-4 overflow-x-auto max-h-96 overflow-y-auto" @scroll="loadNext">
		<div v-for="comment in storeData[0]"
			:key="comment[meta.key]" class="flex flex-col border p-4 rounded-lg">
			<div :class="{
				'text-right': meta.canEdit && comment.id_user === storeUser.id_user,
				'text-left': meta.canEdit && comment.id_user !== storeUser.id_user
			}" class="text-sm text-gray-600">
				<span class="font-semibold">
					{{ storeData[1][comment.id_user]?.name_user }} {{
						storeData[1][comment.id_user]?.firstname_user }}
				</span>
				<span class="text-xs text-gray-500">
					- {{ new Date(comment.created_at).toLocaleString() }} - {{ new Date(comment.updated_at).toLocaleString() }}
				</span>
			</div>
			<div v-if="meta.canEdit" class="text-center text-gray-800 mb-2">
				<template v-if="comment.tmp && meta.canEdit">
					<Form :validation-schema="schemaComment" v-slot="{ errors }">
						<Field :name="meta.contenu" type="text"
							v-model="comment.tmp[meta.contenu]"
							class="w-full p-2 border rounded-lg"
							:class="{ 'border-red-500': errors[meta.contenu] }" />
						<div class="flex justify-end space-x-2 mt-2">
							<div class="relative">
								<button type="button" @click="commentUpdate(comment.tmp)"
									class="px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600">
									{{ $t('components.VModalCommentSave') }}
								</button>
								<div v-if="comment.tmp.loading"
									class="absolute inset-0 bg-green-500 bg-opacity-90 rounded-lg flex items-center justify-center">
									<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
								</div>
							</div>
							<button type="button" @click="comment.tmp = null"
								class="px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
								{{ $t('components.VModalCommentCancel') }}
							</button>
						</div>
					</Form>
				</template>
				<template v-else>
					<div :class="{
						'text-right': comment.id_user === storeUser.id_user,
						'text-left': comment.id_user !== storeUser.id_user
					}">
						{{ comment[meta.contenu] }}
					</div>
					<div v-if="meta.canEdit && (comment.id_user === storeUser.id_user || meta.roleRequired)"
						class="flex justify-end space-x-2">
						<button type="button" @click="comment.tmp = { ...comment }"
							class="px-3 py-1 bg-yellow-400 text-white rounded-lg hover:bg-yellow-500">
							{{ $t('components.VModalCommentEdit') }}
						</button>
						<button type="button" @click="selectedComment = comment[meta.key]; deleteModalShow = true"
							class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
							{{ $t('components.VModalCommentDelete') }}
						</button>
					</div>
				</template>
			</div>
			<div v-if="!meta.canEdit" class="text-sm text-gray-800 mb-2">
				<div>
					{{ comment[meta.contenu] }}
				</div>
				<RouterLink :to="meta.link + comment[meta.idRessource]"
					class="text-blue-500 hover:underline">
					{{ $t('components.VModalCommentLink') }}
				</RouterLink>
			</div>
		</div>
		<div v-if="loading" class="text-center">
			{{ $t('components.VModalCommentLoading') }}
		</div>
	</div>

	<ModalDeleteConfirm :show-modal="deleteModalShow" @close-modal="deleteModalShow = false"
		:delete-action="commentDelete" :text-title="texteModalDelete?.textTitle"
		:text-p="texteModalDelete?.textP"/>
</template>

<script>
import { inject, defineAsyncComponent } from "vue";
import { Form, Field } from "vee-validate";
import * as Yup from "yup";
export default {
	name: "Comment",
	props: {
		storeData: {
			type: Array,
			required: true,
			// This should be an array containing:
			// [0] - store with all comments
			// [1] - store with all users
			default: () => [],
		},
		storeFunction: {
			type: Object,
			required: false,
			// This should contain functions for create, update, and delete operations
			default: () => ({
				create: () => {},
				update: () => {},
				delete: () => Promise.resolve(),
			}),
		},
		storeUser: {
			type: Object,
			default: () => ({}),
			// This should be an object containing the user session data
		},
		storeConfig: {
			type: Object,
			default: () => ({}),
			// This should be an object containing the configuration store, used to get max length for validation
		},
		meta: {
			type: Object,
			required: true,
			// This should contain metadata about the comment, such as:
			// - key: unique identifier for the comment
			// - contenu: the content field of the comment
			// - canEdit: boolean indicating if the user can edit comments
			// - idRessource: identifier for the resource linked to the comment
			// - link: URL for the resource linked to the comment
			// - roleRequired: boolean indicating if a specific role is required to edit/delete all comments (not just the user's own comments)
			default: () => ({
				key: "id_comment",
				contenu: "content_comment",
				canEdit: false,
				idRessource: "id_ressource",
				link: "/ressource/",
				roleRequired: false,
			}),
		},
		loading: {
			type: Boolean,
			default: true,
			// Indicates if the component is loading data
		},
		totalCount: {
			type: Number,
			default: 0,
		},
		fetchFunction: {
			type: Function,
			default: () => { 
				return [0, false];
			},
		},
		listFetchFunction: {
			type: Array,
			default: () => [],
			// This should be an array of functions to refetch related lists when a comment is created, updated, or deleted
		},
		texteModalDelete: {
			type: Object,
			required: false,
			// This should contain the text for the delete confirmation modal
			// the text will be translated using $t so it should be a key from the translation files
			// Example: { textTitle: "page.VModalCommentDeleteTitle", textP: "page.VModalCommentDeleteP" }
			default: () => ({
				textTitle: "common.VALLMissingTranslateLink",
				textP: "common.VALLMissingTranslateLink",
			}),
		},
	},
	components: {
		Form,
		Field,
		ModalDeleteConfirm: defineAsyncComponent(() => import("@/components/ModalDeleteConfirm.vue")),
	},
	async created() {
		await this.refetchData();
	},
	setup() {
		const { addNotification } = inject("useNotification"); 
		return {
			addNotification,
		};
	},
	data() {
		return {
			commentFormNew: "",
			selectedComment: null,
			deleteModalShow: false,
			createLoading: false,
			nextOffset: 0,
			hasMore: true,
			isInitializing: true,
		};
	},
	computed: {
		schemaComment() {
			return Yup.object().shape({
				[this.meta.contenu]: Yup.string()
					.max(this.storeConfig.getConfigByKey("max_length_comment"), this.$t("components.VModalCommentMaxLength") + " " + this.storeConfig.getConfigByKey("max_length_comment") + this.$t("common.VAllCaracters"))
					.required(this.$t("components.VModalCommentRequired")),
			});
		},
	},
	methods: {
		async commentCreate(comment) {
			this.createLoading = true;
			try {
				this.schemaComment.validateSync({ [this.meta.contenu]: comment }, { abortEarly: false });
				await this.storeFunction.create({
					[this.meta.contenu]: comment,
				});
				this.addNotification({
					type: "success",
					message: this.$t("components.VModalCommentCreateSuccess"),
				});
				this.commentFormNew = "";
			} catch (e) {
				this.addNotification({ message: e, type: "error" });
			} finally {
				this.createLoading = false;
			}
		},
		async commentUpdate(comment) {
			comment.loading = true;
			try {
				this.schemaComment.validateSync(comment, { abortEarly: false });
				await this.storeFunction.update(comment[this.meta.key], {
					[this.meta.contenu]: comment[this.meta.contenu],
				});
				this.addNotification({
					type: "success",
					message: this.$t("components.VModalCommentUpdateSuccess"),
				});
				comment = null;
			} catch (e) {
				this.addNotification({ message: e, type: "error" });
				return;
			}
		},
		async commentDelete() {
			await this.storeFunction.delete(this.selectedComment)
				.then(() => {
					this.addNotification({
						type: "success",
						message: this.$t("components.VModalCommentDeleteSuccess"),
					});
				})
				.catch((e) => {
					this.addNotification({ message: e, type: "error" });
				});
			this.deleteModalShow = false;
		},
		async loadNext(e) {
			if (this.totalCount === 0 || this.loading || !this.hasMore) {
				return;
			}
			if (e.target.scrollTop + e.target.clientHeight >= e.target.scrollHeight - 10) {
				if (this.totalCount === this.nextOffset) {
					return;
				}
				[this.nextOffset, this.hasMore] = await this.fetchFunction(this.nextOffset, 100, this.meta?.expand || []);
			}
		},
		async refetchData() {
			// Reset l'état et refetch les données depuis le début
			this.nextOffset = 0;
			this.hasMore = true;
			let intervalOffset = this.nextOffset;
			[this.nextOffset, this.hasMore] = await this.fetchFunction(100, 0, this.meta?.expand || []);
			await this.refetchListData(intervalOffset, this.nextOffset);
		},
		async refetchListData(minOffset, maxOffset) {
			for (let index = 0; index < this.listFetchFunction.length; index++) {
				if (this.listFetchFunction[index]) {
					await this.listFetchFunction[index](minOffset, maxOffset);
				}
			}
		},
	},
};
</script>
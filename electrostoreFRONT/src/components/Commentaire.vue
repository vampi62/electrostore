<template>
	<Form v-if="meta.CanEdit" :validation-schema="schemaCommentaire" v-slot="{ errors }">
		<div class="flex items-center space-x-4">
			<Field :name="meta.contenu" type="text" v-model="commentaireFormNew"
				:placeholder="$t('components.VModalCommentairePlaceholder')"
				class="w-full p-2 border rounded-lg"
				:class="{ 'border-red-500': errors[meta.contenu] }" />
			<button type="button" @click="commentaireCreate(commentaireFormNew)"
				class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
				{{ $t('components.VModalCommentaireAdd') }}
			</button>
		</div>
	</Form>
	<div class="space-y-4 overflow-x-auto max-h-96 overflow-y-auto" @scroll="loadNext">
		<div v-for="commentaire in storeData[0]"
			:key="commentaire[meta.key]" class="flex flex-col border p-4 rounded-lg">
			<div :class="{
				'text-right': meta.CanEdit && commentaire.id_user === storeData[2].id_user,
				'text-left': meta.CanEdit && commentaire.id_user !== storeData[2].id_user
			}" class="text-sm text-gray-600">
				<span class="font-semibold">
					{{ storeData[1][commentaire.id_user].nom_user }} {{
						storeData[1][commentaire.id_user].prenom_user }}
				</span>
				<span class="text-xs text-gray-500">
					- {{ commentaire.created_at }} - {{ commentaire.updated_at }}
				</span>
			</div>
			<div v-if="meta.CanEdit" class="text-center text-gray-800 mb-2">
				<template v-if="commentaire.tmp && meta.CanEdit">
					<Form :validation-schema="schemaCommentaire" v-slot="{ errors }">
						<Field :name="meta.contenu" type="text"
							v-model="commentaire.tmp[meta.contenu]"
							class="w-full p-2 border rounded-lg"
							:class="{ 'border-red-500': errors[meta.contenu] }" />
						<div class="flex justify-end space-x-2 mt-2">
							<button type="button" @click="commentaireUpdate(commentaire.tmp)"
								class="px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600">
								{{ $t('components.VModalCommentaireSave') }}
							</button>
							<button type="button" @click="commentaire.tmp = null"
								class="px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
								{{ $t('components.VModalCommentaireCancel') }}
							</button>
						</div>
					</Form>
				</template>
				<template v-else>
					<div :class="{
						'text-right': commentaire.id_user === storeData[2].id_user,
						'text-left': commentaire.id_user !== storeData[2].id_user
					}">
						{{ commentaire[meta.contenu] }}
					</div>
					<div v-if="meta.CanEdit && (commentaire.id_user === storeData[2].id_user || storeData[2].role_user === 1 || storeData[2].role_user === 2)"
						class="flex justify-end space-x-2">
						<button type="button" @click="commentaire.tmp = { ...commentaire }"
							class="px-3 py-1 bg-yellow-400 text-white rounded-lg hover:bg-yellow-500">
							{{ $t('components.VModalCommentaireEdit') }}
						</button>
						<button type="button" @click="selectedCommentaire = commentaire[meta.key]; deleteModalShow = true"
							class="px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600">
							{{ $t('components.VModalCommentaireDelete') }}
						</button>
					</div>
				</template>
			</div>
			<div v-if="!meta.CanEdit" class="text-sm text-gray-800 mb-2">
				<div>
					{{ commentaire[meta.contenu] }}
				</div>
				<RouterLink :to="meta.link + commentaire[meta.idRessource]"
					class="text-blue-500 hover:underline">
					{{ $t('components.VModalCommentaireLink') }}
				</RouterLink>
			</div>
		</div>
		<div v-if="loading" class="text-center">
			{{ $t('components.VModalCommentaireLoading') }}
		</div>
	</div>

	<ModalDeleteConfirm :show-modal="deleteModalShow" @close-modal="deleteModalShow = false"
		@delete-confirmed="commentaireDelete()" :text-title="texteModalDelete?.textTitle"
		:text-p="texteModalDelete?.textP"/>
</template>

<script>
import { inject } from "vue";
import { Form, Field } from "vee-validate";
import * as Yup from "yup";
import ModalDeleteConfirm from "@/components/ModalDeleteConfirm.vue";
export default {
	name: "Commentaire",
	props: {
		storeData: {
			type: Array,
			required: true,
			// This should be an array containing:
			// [0] - store with all commentaires
			// [1] - store with all users
			// [2] - current user session data
			// [3] - store with configuration data
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
		meta: {
			type: Object,
			required: true,
			// This should contain metadata about the comment, such as:
			// - key: unique identifier for the comment
			// - contenu: the content field of the comment
			// - CanEdit: boolean indicating if the user can edit comments
			// - idRessource: identifier for the resource linked to the comment
			// - link: URL for the resource linked to the comment
			default: () => ({
				key: "id_commentaire",
				contenu: "contenu_commentaire",
				CanEdit: false,
				idRessource: "id_ressource",
				link: "/ressource/",
			}),

		},
		loading: {
			type: Boolean,
			default: true,
			// Indicates if the component is loading data
		},
		loadedCount: {
			type: Number,
			default: 0,
		},
		totalCount: {
			type: Number,
			default: 0,
		},
		fetchFunction: {
			type: Function,
			default: () => {},
		},
		texteModalDelete: {
			type: Object,
			required: false,
			// This should contain the text for the delete confirmation modal
			// the text will be translated using $t so it should be a key from the translation files
			// Example: { textTitle: "page.VModalCommentaireDeleteTitle", textP: "page.VModalCommentaireDeleteP" }
			default: () => ({
				textTitle: "common.VALLMissingTranslateLink",
				textP: "common.VALLMissingTranslateLink",
			}),
		},
	},
	components: {
		Form,
		Field,
		ModalDeleteConfirm,
	},
	setup() {
		const { addNotification } = inject("useNotification"); 
		return {
			addNotification,
		};
	},
	data() {
		return {
			commentaireFormNew: "",
			selectedCommentaire: null,
			deleteModalShow: false,
		};
	},
	computed: {
		schemaCommentaire() {
			return Yup.object().shape({
				[this.meta.contenu]: Yup.string()
					.max(this.storeData[3].getConfigByKey("max_length_commentaire"), this.$t("components.VModalCommentaireMaxLength") + " " + this.storeData[3].getConfigByKey("max_length_commentaire") + this.$t("common.VAllCaracters"))
					.required(this.$t("components.VModalCommentaireRequired")),
			});
		},
	},
	methods: {
		commentaireCreate(commentaire) {
			try {
				this.schemaCommentaire.validateSync({ [this.meta.contenu]: commentaire }, { abortEarly: false });
				this.storeFunction.create({
					[this.meta.contenu]: commentaire,
				});
				this.addNotification({
					type: "success",
					message: this.$t("components.VModalCommentaireCreateSuccess"),
				});
				this.commentaireFormNew = "";
			} catch (e) {
				e.inner.forEach((error) => {
					this.addNotification({
						type: "error",
						message: error.message,
					});
				});
			}
		},
		commentaireUpdate(commentaire) {
			try {
				this.schemaCommentaire.validateSync(commentaire, { abortEarly: false });
				this.storeFunction.update(commentaire[this.meta.key], {
					[this.meta.contenu]: commentaire[this.meta.contenu],
				});
				this.addNotification({
					type: "success",
					message: this.$t("components.VModalCommentaireUpdateSuccess"),
				});
				commentaire = null;
			} catch (e) {
				e.inner.forEach((error) => {
					this.addNotification({
						type: "error",
						message: error.message,
					});
				});
				return;
			}
		},
		commentaireDelete() {
			this.storeFunction.delete(this.selectedCommentaire)
				.then(() => {
					this.addNotification({
						type: "success",
						message: this.$t("components.VModalCommentaireDeleteSuccess"),
					});
				})
				.catch((e) => {
					e.inner.forEach((error) => {
						this.addNotification({
							type: "error",
							message: error.message,
						});
					});
				});
			this.deleteModalShow = false;
		},
		async loadNext(e) {
			if (this.totalCount === 0) {
				return;
			}
			if (this.loading) {
				return;
			}
			if (e.target.scrollTop + e.target.clientHeight >= e.target.scrollHeight - 10) {
				if (this.totalCount === this.loadedCount) {
					return;
				}
				await this.fetchFunction(this.loadedCount + 100, this.loadedCount);
			}
		},
	},
};
</script>
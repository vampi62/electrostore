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
	<div class="space-y-4 overflow-x-auto max-h-96 overflow-y-auto" :ref="`HTMLContainerCommentaires-${this.$.uid}`">
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

	<ModalDeleteConfirm :showModal="deleteModalShow" @closeModal="deleteModalShow = false"
		@deleteConfirmed="commentaireDelete()" :textTitle="texteModalDelete?.textTitle"
		:textP="texteModalDelete?.textP"/>
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
		},
		storeFunction: {
			type: Object,
			required: false,
		},
		meta: {
			type: Object,
			required: true,
		},
		loading: {
			type: Boolean,
			default: true,
		},
		texteModalDelete: {
			type: Object,
			required: false,
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
	},
};
</script>
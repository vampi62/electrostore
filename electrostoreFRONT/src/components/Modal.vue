<script>
export default {
  name: 'Modal',
  props: {
    show: {
      type: Boolean,
      required: true
    },
    title: {
      type: String,
      default: 'Modal'
    },
    initialData: {
      type: Object,
      default: () => ([])
    }
  },
  data() {
    return {
      formData: { ...this.initialData }
    }
  },
  methods: {
    close() {
      this.$emit('close')
    },
    submit() {
      this.$emit('submit', this.formData)
    }
  },
  watch: {
    initialData: {
      handler(newVal) {
        this.formData = { ...newVal }
      },
      deep: true
    }
  }
}
</script>


<template>
  <div v-if="show" @click="close" class="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full flex items-center justify-center">
    <div @click.stop class="bg-white p-5 rounded-lg shadow-xl w-1/2">
      <h3 class="text-lg font-bold mb-4">{{ title }}</h3>
      <form @submit.prevent="submit">
        <div v-for="initialData" class="mb-4" >
          <label class="block text-gray-700 text-sm font-bold mb-2" for="nom">
            Nom
          </label>
          <input v-model="formData.nom_camera" class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline" id="nom" type="text" required>
        </div>

        <div class="flex items-center justify-between">
          <button class="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline" type="submit">
            Enregistrer
          </button>
          <button @click="close" class="bg-gray-500 hover:bg-gray-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline" type="button">
            Annuler
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
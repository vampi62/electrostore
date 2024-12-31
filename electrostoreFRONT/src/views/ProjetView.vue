<script setup>

import { useRoute } from 'vue-router';

const route = useRoute();
const projetId = route.params.id;

</script>

<template>
    <div>
        <h2>projet {{ projetId }}</h2>
    </div>
</template>

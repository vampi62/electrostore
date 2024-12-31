<script setup>

import { useRoute } from 'vue-router';

const route = useRoute();
const itemId = route.params.id;

</script>

<template>
    <div>
        <h2>Item {{ itemId }}</h2>
    </div>
</template>

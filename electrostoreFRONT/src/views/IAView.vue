<script setup>

import { useRoute } from 'vue-router';

const route = useRoute();
const iaId = route.params.id;

</script>

<template>
    <div>
        <h2>IA {{ iaId }}</h2>
    </div>
</template>

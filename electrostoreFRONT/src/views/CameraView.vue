<script setup>

import { useRoute } from 'vue-router';

const route = useRoute();
const cameraId = route.params.id;

</script>

<template>
    <div>
        <h2>camera {{ cameraId }}</h2>
    </div>
</template>

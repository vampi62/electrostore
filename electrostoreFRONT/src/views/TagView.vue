<script setup>

import { useRoute } from 'vue-router';

const route = useRoute();
const tagId = route.params.id;

</script>

<template>
    <div>
        <h2>tag {{ tagId }}</h2>
    </div>
</template>

<script setup>
import { useRoute } from 'vue-router';
const route = useRoute();
const userId = route.params.id;
</script>

<template>
    <div>
        <h2>utilisateur</h2>
    </div>
    <div>
        <h1>User Details</h1>
        <p>User ID: {{ userId }}</p>
    </div>
</template>

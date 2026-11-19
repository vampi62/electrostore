<template>
	<span class="font-semibold sm:min-w-[150px]">{{ field.label ? $t(field.label) : field.text }}</span>
</template>

<script setup lang="ts">
import type { FormLabel } from "@/types/form";

defineProps<{ field: FormLabel }>();
</script>

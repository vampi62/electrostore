<template>
	<span class="font-semibold sm:min-w-[150px]">{{ field.label ? $t(field.label) : value }}</span>
</template>

<script setup lang="ts">
import type { FormLabel } from "@/types/form";

defineProps<{ field: FormLabel; value?: unknown }>();
</script>

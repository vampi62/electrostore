<template>
	<div class="flex space-x-2">
		<span>{{ displayValue }}</span>
	</div>
</template>

<script setup lang="ts">
import { computed, toValue } from "vue";
import type { FormLabel } from "@/types/form";

const props = defineProps<{ field: FormLabel }>();

const displayValue = computed(() => toValue(props.field.value));
</script>

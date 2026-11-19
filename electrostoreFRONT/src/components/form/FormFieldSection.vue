<template>
	<h2 class="text-lg font-bold w-full pt-4">{{ $t(field.label) }}</h2>
	<hr class="w-full border-gray-300 mb-2" />
	<!-- Empty div to force the next line to take the full width -->
	<div class="w-full"></div>
</template>

<script setup lang="ts">
import type { FormLabel } from "@/types/form";

defineProps<{ field: FormLabel }>();
</script>

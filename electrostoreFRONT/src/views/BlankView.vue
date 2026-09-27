<script setup>
import { useViewScroll } from "@/composables";

useViewScroll(false);
</script>

<template>
	<div class="flex flex-1 flex-col items-center justify-center gap-4 text-gray-500">
		<font-awesome-icon icon="fa-solid fa-box" size="3x" />
		<p class="text-center">{{ $t('common.VAppTabBlankHint') }}</p>
	</div>
</template>

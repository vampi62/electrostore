<script setup>
import { onMounted, computed } from "vue";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useConfigsStore } from "@/stores";
const configsStore = useConfigsStore();

onMounted(() => {
	configsStore.getHealth();
	configsStore.getConfig();
});

const services = computed(() => [
	{ key: "api_status", label: t("health.ServiceApi"), type: "string" },
	{ key: "db_connected", label: t("health.ServiceDb"), type: "bool" },
	{ key: "mqtt_connected", label: t("health.ServiceMqtt"), type: "bool" },
	{ key: "kafka_connected", label: t("health.ServiceKafka"), type: "bool" },
	{ key: "ia_status", label: t("health.ServiceIa"), type: "string" },
	{ key: "ia_training_in_progress", label: t("health.ServiceIaTrain"), type: "int" },
	{ key: "notif_status", label: t("health.ServiceNotif"), type: "string" },
	{ key: "cron_status", label: t("health.ServiceCron"), type: "string" },
	{ key: "worker_status", label: t("health.ServiceWorker"), type: "string" },
]);

const configGroups = computed(() => [
	{
		label: t("health.ConfigGroupGeneral"),
		keys: [
			{ key: "demo_mode", label: t("health.ConfigDemoMode"), type: "bool" },
			{ key: "max_size_document_in_mb", label: t("health.ConfigMaxDocSize"), type: "value", unit: "MB" },
		],
	},
	{
		label: t("health.ConfigGroupLimits"),
		keys: [
			{ key: "max_length_name", label: t("health.ConfigMaxName"), type: "value" },
			{ key: "max_length_description", label: t("health.ConfigMaxDesc"), type: "value" },
			{ key: "max_length_url", label: t("health.ConfigMaxUrl"), type: "value" },
			{ key: "max_length_commentaire", label: t("health.ConfigMaxComment"), type: "value" },
			{ key: "max_length_email", label: t("health.ConfigMaxEmail"), type: "value" },
			{ key: "max_length_ip", label: t("health.ConfigMaxIp"), type: "value" },
			{ key: "max_length_reason", label: t("health.ConfigMaxReason"), type: "value" },
			{ key: "max_length_status", label: t("health.ConfigMaxStatus"), type: "value" },
			{ key: "max_length_type", label: t("health.ConfigMaxType"), type: "value" },
		],
	},
	{
		label: t("health.ConfigGroupMedia"),
		keys: [
			{ key: "allowed_image_extensions", label: t("health.ConfigImgExt"), type: "list" },
			{ key: "allowed_document_extensions", label: t("health.ConfigDocExt"), type: "list" },
		],
	},
	{
		label: t("health.ConfigGroupSSO"),
		keys: [
			{ key: "sso_available_providers", label: t("health.ConfigSsoProviders"), type: "sso" },
		],
	},
]);

function statusBadge(key, type) {
	const value = configsStore.getStatusByKey(key);
	if (type === "bool") {
		return value ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";
	}
	if (type === "int") {
		return value > 0 ? "bg-yellow-100 text-yellow-800" : "bg-green-100 text-green-800";
	}
	const v = (value || "").toLowerCase();
	if (["healthy", "ok", "online", "running"].includes(v)) {
		return "bg-green-100 text-green-800";
	}
	if (["unknown", ""].includes(v) || !v) {
		return "bg-gray-100 text-gray-600";
	}
	return "bg-red-100 text-red-800";
}

function statusLabel(key, type) {
	const value = configsStore.getStatusByKey(key);
	if (type === "bool") {
		return value ? t("health.StatusTrue") : t("health.StatusFalse");
	}
	if (type === "int") {
		return value > 0 ? t("health.StatusInProgress", value) : t("health.StatusIdle");
	}
	return value || t("health.StatusUnknown");
}

function configValue(key, type, unit) {
	const value = configsStore.getConfigByKey(key);
	if (type === "bool") {
		return null;
	}
	if (type === "list") {
		return Array.isArray(value) ? value : [];
	}
	if (type === "sso") {
		return Array.isArray(value) ? value : [];
	}
	return unit ? `${value} ${unit}` : value;
}

function configBool(key) {
	return configsStore.getConfigByKey(key);
}

const isStatusLoading = computed(() => configsStore.status?.loading);
const isConfigLoading = computed(() => configsStore.configs?.loading);
</script>

<template>
	<div class="max-w-5xl mx-auto">
		<h2 class="text-2xl font-bold mb-6">{{ $t('health.Title') }}</h2>

		<!-- Services Status -->
		<section class="mb-8">
			<div class="flex items-center justify-between mb-3">
				<h3 class="text-lg font-semibold text-gray-700">{{ $t('health.SectionStatus') }}</h3>
				<button type="button" @click="configsStore.getHealth()"
					class="flex items-center gap-1 text-sm px-3 py-1 rounded bg-gray-100 hover:bg-gray-200 text-gray-700">
					<i class="fa-solid fa-arrows-rotate" :class="{ 'animate-spin': isStatusLoading }"></i>
					{{ $t('health.Refresh') }}
				</button>
			</div>

			<div v-if="isStatusLoading" class="flex items-center gap-2 text-gray-500 py-4">
				<span class="w-5 h-5 border-2 border-gray-400 border-t-transparent rounded-full animate-spin"></span>
				{{ $t('health.Loading') }}
			</div>

			<div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
				<div v-for="service in services" :key="service.key"
					class="flex items-center justify-between bg-white border border-gray-200 rounded-lg px-4 py-3 shadow-sm">
					<span class="font-medium text-gray-700">{{ service.label }}</span>
					<span class="px-2 py-0.5 rounded text-sm font-semibold" :class="statusBadge(service.key, service.type)">
						{{ statusLabel(service.key, service.type) }}
					</span>
				</div>
			</div>
		</section>

		<!-- Configuration -->
		<section>
			<div class="flex items-center justify-between mb-3">
				<h3 class="text-lg font-semibold text-gray-700">{{ $t('health.SectionConfig') }}</h3>
				<button type="button" @click="configsStore.getConfig()"
					class="flex items-center gap-1 text-sm px-3 py-1 rounded bg-gray-100 hover:bg-gray-200 text-gray-700">
					<i class="fa-solid fa-arrows-rotate" :class="{ 'animate-spin': isConfigLoading }"></i>
					{{ $t('health.Refresh') }}
				</button>
			</div>

			<div v-if="isConfigLoading" class="flex items-center gap-2 text-gray-500 py-4">
				<span class="w-5 h-5 border-2 border-gray-400 border-t-transparent rounded-full animate-spin"></span>
				{{ $t('health.Loading') }}
			</div>

			<div v-else class="flex flex-col gap-4">
				<div v-for="group in configGroups" :key="group.label"
					class="bg-white border border-gray-200 rounded-lg shadow-sm overflow-hidden">
					<div class="px-4 py-2 bg-gray-50 border-b border-gray-200">
						<h4 class="font-semibold text-gray-600 text-sm uppercase tracking-wide">{{ group.label }}</h4>
					</div>
					<div class="divide-y divide-gray-100">
						<div v-for="item in group.keys" :key="item.key"
							class="flex items-center justify-between px-4 py-2.5">
							<span class="text-gray-700 text-sm">{{ item.label }}</span>

							<!-- Bool -->
							<span v-if="item.type === 'bool'"
								class="px-2 py-0.5 rounded text-sm font-semibold"
								:class="configBool(item.key) ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-600'">
								{{ configBool(item.key) ? $t('health.StatusTrue') : $t('health.StatusFalse') }}
							</span>

							<!-- Simple value -->
							<span v-else-if="item.type === 'value'"
								class="text-sm font-mono bg-gray-100 text-gray-700 px-2 py-0.5 rounded">
								{{ configValue(item.key, item.type, item.unit) }}
							</span>

							<!-- List of extensions -->
							<div v-else-if="item.type === 'list'"
								class="flex flex-wrap gap-1 justify-end max-w-xs">
								<span v-for="ext in configValue(item.key, item.type)" :key="ext"
									class="text-xs bg-blue-50 text-blue-700 border border-blue-200 px-1.5 py-0.5 rounded font-mono">
									{{ ext }}
								</span>
							</div>

							<!-- SSO providers -->
							<div v-else-if="item.type === 'sso'" class="flex flex-col gap-1 items-end">
								<span v-if="configValue(item.key, item.type).length === 0"
									class="text-sm text-gray-400 italic">{{ $t('health.ConfigSsoNone') }}</span>
								<div v-for="provider in configValue(item.key, item.type)" :key="provider.provider"
									class="flex items-center gap-2 text-sm bg-gray-50 border border-gray-200 px-2 py-1 rounded">
									<img v-if="provider.icon_url" :src="provider.icon_url" :alt="provider.display_name" class="h-4 w-4" />
									<span class="font-medium">{{ provider.display_name }}</span>
									<span class="text-gray-400 text-xs font-mono">({{ provider.provider }})</span>
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>
		</section>
	</div>
</template>

<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref, inject, computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import * as Yup from "yup";

import { useViewScroll, useTabGuard } from "@/composables";
import type { useNotification } from "@/composables";
import type FormContainer from "@/components/FormContainer.vue";
import { downloadFile, viewFile, isNewId } from "@/utils";
import CommandStatus from "@/enums/CommandStatus";
import TrackingStatus from "@/enums/TrackingStatus";
import TrackingSubStatus from "@/enums/TrackingSubStatus";
import { useConfigsStore, useCommandsStore, useCommandsCommentsStore, useCommandsDocumentsStore, useCommandsHistoryStore, useItemsCommandsStore, useUsersStore, useItemsStore, useCarriersStore, useAuthStore } from "@/stores";

import type { FilterLabel } from "@/types/filter";
import type { FormLabel } from "@/types/form";
import type { TableauLabel, TableauRowData } from "@/types/tableau";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";
import type { RoadMapHistoryEntry } from "@/types/roadmap";

const { addNotification } = inject("useNotification") as ReturnType<typeof useNotification>;
const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const configsStore = useConfigsStore();
const commandsStore = useCommandsStore();
const commandCommentsStore = useCommandsCommentsStore();
const commandDocumentsStore = useCommandsDocumentsStore();
const commandHistoryStore = useCommandsHistoryStore();
const itemCommandsStore = useItemsCommandsStore();
const usersStore = useUsersStore();
const itemsStore = useItemsStore();
const carriersStore = useCarriersStore();
const authStore = useAuthStore();

const commandId = ref(route.params.id as string);
const preset = ref(typeof route.query.preset === "string" ? route.query.preset : null);

// every new element has its own edition space in the store, so several tabs can create an element at the same time
if (isNewId(commandId.value)) {
	commandId.value = commandsStore.getAvailableNewCommandId();
}

const formContainer = ref<InstanceType<typeof FormContainer> | null>(null);

async function fetchAllData() {
	if (isNewId(commandId.value)) {
		if (Object.keys(carriersStore.carriers).length === 0) {
			carriersStore.getCarrierByInterval(200, 0, [], [], {});
		}
		commandsStore.loadToEdition(commandId.value, preset.value);
	} else {
		commandsStore.setLoadingEdition(commandId.value, true);
		try {
			await commandsStore.getCommandById(commandId.value, ["carrier"]);
		} catch {
			delete commandsStore.commands[commandId.value];
			addNotification({ message: t("command.NotFound"), type: "error" });
			router.push("/commands");
			return;
		}
		commandsStore.loadToEdition(commandId.value);
		commandHistoryStore.getCommandHistoryByInterval({ id_command: commandId.value });
		usersStore.users[authStore.user.id_user] = authStore.user; // avoids undefined user when the current user posts first comment
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	commandsStore.clearEdition(commandId.value);
});

// commande
const commandDeleteModalShow = ref(false);
const commandStatusOptions = {
	[CommandStatus.Created]: t("command.Status0"),
	[CommandStatus.Processing]: t("command.Status1"),
	[CommandStatus.InTransit]: t("command.Status2"),
	[CommandStatus.Delivered]: t("command.Status3"),
	[CommandStatus.Cancelled]: t("command.Status4"),
	[CommandStatus.Returned]: t("command.Status5"),
	[CommandStatus.Failed]: t("command.Status6"),
	[CommandStatus.Unknown]: t("command.Status7"),
	[CommandStatus.Archived]: t("command.Status8"),
};
const trackingStatusOptions = {
	[TrackingStatus.NotFound]: t("command.TrackingStatus0"),
	[TrackingStatus.InfoReceived]: t("command.TrackingStatus1"),
	[TrackingStatus.InTransit]: t("command.TrackingStatus2"),
	[TrackingStatus.Expired]: t("command.TrackingStatus3"),
	[TrackingStatus.AvailableForPickup]: t("command.TrackingStatus4"),
	[TrackingStatus.OutForDelivery]: t("command.TrackingStatus5"),
	[TrackingStatus.DeliveryFailure]: t("command.TrackingStatus6"),
	[TrackingStatus.Delivered]: t("command.TrackingStatus7"),
	[TrackingStatus.Exception]: t("command.TrackingStatus8"),
	[TrackingStatus.Unknown]: t("command.TrackingStatus9"),
};
const trackingSubStatusOptions = {
	[TrackingSubStatus.NotFound_Other]: t("command.TrackingSubStatus0"),
	[TrackingSubStatus.NotFound_InvalidCode]: t("command.TrackingSubStatus1"),
	[TrackingSubStatus.InfoReceived]: t("command.TrackingSubStatus2"),
	[TrackingSubStatus.InTransit_PickedUp]: t("command.TrackingSubStatus3"),
	[TrackingSubStatus.InTransit_Other]: t("command.TrackingSubStatus4"),
	[TrackingSubStatus.InTransit_Departure]: t("command.TrackingSubStatus5"),
	[TrackingSubStatus.InTransit_Arrival]: t("command.TrackingSubStatus6"),
	[TrackingSubStatus.InTransit_CustomsProcessing]: t("command.TrackingSubStatus7"),
	[TrackingSubStatus.InTransit_CustomsReleased]: t("command.TrackingSubStatus8"),
	[TrackingSubStatus.InTransit_CustomsRequiringInformation]: t("command.TrackingSubStatus9"),
	[TrackingSubStatus.Expired_Other]: t("command.TrackingSubStatus10"),
	[TrackingSubStatus.AvailableForPickup_Other]: t("command.TrackingSubStatus11"),
	[TrackingSubStatus.OutForDelivery_Other]: t("command.TrackingSubStatus12"),
	[TrackingSubStatus.DeliveryFailure_Other]: t("command.TrackingSubStatus13"),
	[TrackingSubStatus.DeliveryFailure_NoBody]: t("command.TrackingSubStatus14"),
	[TrackingSubStatus.DeliveryFailure_Security]: t("command.TrackingSubStatus15"),
	[TrackingSubStatus.DeliveryFailure_Rejected]: t("command.TrackingSubStatus16"),
	[TrackingSubStatus.DeliveryFailure_InvalidAddress]: t("command.TrackingSubStatus17"),
	[TrackingSubStatus.Delivered_Other]: t("command.TrackingSubStatus18"),
	[TrackingSubStatus.Exception_Other]: t("command.TrackingSubStatus19"),
	[TrackingSubStatus.Exception_Returning]: t("command.TrackingSubStatus20"),
	[TrackingSubStatus.Exception_Returned]: t("command.TrackingSubStatus21"),
	[TrackingSubStatus.Exception_NoBody]: t("command.TrackingSubStatus22"),
	[TrackingSubStatus.Exception_Security]: t("command.TrackingSubStatus23"),
	[TrackingSubStatus.Exception_Damage]: t("command.TrackingSubStatus24"),
	[TrackingSubStatus.Exception_Rejected]: t("command.TrackingSubStatus25"),
	[TrackingSubStatus.Exception_Delayed]: t("command.TrackingSubStatus26"),
	[TrackingSubStatus.Exception_Lost]: t("command.TrackingSubStatus27"),
	[TrackingSubStatus.Exception_Destroyed]: t("command.TrackingSubStatus28"),
	[TrackingSubStatus.Exception_Cancel]: t("command.TrackingSubStatus29"),
};

// roadmap commande
const commandRoadmapSteps = [
	{ id: CommandStatus.Created, name: "Created" },
	{ id: CommandStatus.Processing, name: "Processing" },
	{ id: CommandStatus.InTransit, name: "InTransit" },
	{ id: CommandStatus.Delivered, name: "Delivered" },
	{ id: CommandStatus.Cancelled, name: "Cancelled" },
	{ id: CommandStatus.Returned, name: "Returned" },
	{ id: CommandStatus.Failed, name: "Failed" },
	{ id: CommandStatus.Unknown, name: "Unknown" },
	{ id: CommandStatus.Archived, name: "Archived" },
];
const commandRoadmapStepColors = {
	Created: { completed: "bg-gray-300 text-gray-700", current: "bg-gray-400 text-gray-800", pending: "bg-gray-100 text-gray-500", border: "border-gray-400", badge: "bg-gray-200 text-gray-800", text: "text-gray-700", historyBorder: "border-gray-400" },
	Processing: { completed: "bg-blue-400 text-white", current: "bg-blue-500 text-white", pending: "bg-blue-100 text-blue-600", border: "border-blue-500", badge: "bg-blue-200 text-blue-900", text: "text-blue-700", historyBorder: "border-blue-500" },
	InTransit: { completed: "bg-cyan-400 text-white", current: "bg-cyan-500 text-white", pending: "bg-cyan-100 text-cyan-600", border: "border-cyan-500", badge: "bg-cyan-200 text-cyan-900", text: "text-cyan-700", historyBorder: "border-cyan-500" },
	Delivered: { completed: "bg-green-400 text-white", current: "bg-green-500 text-white", pending: "bg-green-100 text-green-600", border: "border-green-500", badge: "bg-green-200 text-green-900", text: "text-green-700", historyBorder: "border-green-500" },
	Cancelled: { completed: "bg-red-400 text-white", current: "bg-red-500 text-white", pending: "bg-red-100 text-red-600", border: "border-red-500", badge: "bg-red-200 text-red-900", text: "text-red-700", historyBorder: "border-red-500" },
	Returned: { completed: "bg-orange-400 text-white", current: "bg-orange-500 text-white", pending: "bg-orange-100 text-orange-600", border: "border-orange-500", badge: "bg-orange-200 text-orange-900", text: "text-orange-700", historyBorder: "border-orange-500" },
	Failed: { completed: "bg-rose-400 text-white", current: "bg-rose-500 text-white", pending: "bg-rose-100 text-rose-600", border: "border-rose-500", badge: "bg-rose-200 text-rose-900", text: "text-rose-700", historyBorder: "border-rose-500" },
	Unknown: { completed: "bg-gray-300 text-gray-700", current: "bg-gray-400 text-gray-800", pending: "bg-gray-100 text-gray-500", border: "border-gray-400", badge: "bg-gray-200 text-gray-800", text: "text-gray-700", historyBorder: "border-gray-400" },
	Archived: { completed: "bg-purple-400 text-white", current: "bg-purple-500 text-white", pending: "bg-purple-100 text-purple-600", border: "border-purple-500", badge: "bg-purple-200 text-purple-900", text: "text-purple-700", historyBorder: "border-purple-500" },
};
const commandCurrentStep = computed(() => {
	const status = commandsStore.commandEdition[commandId.value]?.status_command;
	if (status === null || status === undefined) {
		return 0;
	}
	const idx = commandRoadmapSteps.findIndex((s) => s.id === Number(status));
	return Math.max(idx, 0);
});

// roadmap tracking
const trackingRoadmapSteps = [
	{ id: TrackingStatus.NotFound, name: "NotFound" },
	{ id: TrackingStatus.InfoReceived, name: "InfoReceived" },
	{ id: TrackingStatus.InTransit, name: "InTransit" },
	{ id: TrackingStatus.Expired, name: "Expired" },
	{ id: TrackingStatus.AvailableForPickup, name: "AvailableForPickup" },
	{ id: TrackingStatus.OutForDelivery, name: "OutForDelivery" },
	{ id: TrackingStatus.DeliveryFailure, name: "DelivereyFailure" },
	{ id: TrackingStatus.Delivered, name: "Delivered" },
	{ id: TrackingStatus.Exception, name: "Exception" },
	{ id: TrackingStatus.Unknown, name: "Unknown" },
];
const trackingRoadmapStepColors = {
	InfoReceived: { completed: "bg-gray-300 text-gray-700", current: "bg-gray-400 text-gray-800", pending: "bg-gray-100 text-gray-500", border: "border-gray-400", badge: "bg-gray-200 text-gray-800", text: "text-gray-700", historyBorder: "border-gray-400" },
	PickedUp: { completed: "bg-blue-300 text-blue-900", current: "bg-blue-400 text-white", pending: "bg-blue-50 text-blue-500", border: "border-blue-400", badge: "bg-blue-200 text-blue-900", text: "text-blue-600", historyBorder: "border-blue-400" },
	Departure: { completed: "bg-cyan-300 text-cyan-900", current: "bg-cyan-400 text-white", pending: "bg-cyan-50 text-cyan-500", border: "border-cyan-400", badge: "bg-cyan-200 text-cyan-900", text: "text-cyan-600", historyBorder: "border-cyan-400" },
	Arrival: { completed: "bg-teal-300 text-teal-900", current: "bg-teal-400 text-white", pending: "bg-teal-50 text-teal-500", border: "border-teal-400", badge: "bg-teal-200 text-teal-900", text: "text-teal-600", historyBorder: "border-teal-400" },
	AvailableForPickup: { completed: "bg-yellow-300 text-yellow-900", current: "bg-yellow-400 text-yellow-900", pending: "bg-yellow-50 text-yellow-600", border: "border-yellow-400", badge: "bg-yellow-200 text-yellow-900", text: "text-yellow-700", historyBorder: "border-yellow-400" },
	OutForDelivery: { completed: "bg-orange-300 text-orange-900", current: "bg-orange-400 text-white", pending: "bg-orange-50 text-orange-500", border: "border-orange-400", badge: "bg-orange-200 text-orange-900", text: "text-orange-600", historyBorder: "border-orange-400" },
	Delivered: { completed: "bg-green-400 text-white", current: "bg-green-500 text-white", pending: "bg-green-100 text-green-600", border: "border-green-500", badge: "bg-green-200 text-green-900", text: "text-green-700", historyBorder: "border-green-500" },
	Returning: { completed: "bg-amber-300 text-amber-900", current: "bg-amber-400 text-white", pending: "bg-amber-50 text-amber-500", border: "border-amber-400", badge: "bg-amber-200 text-amber-900", text: "text-amber-600", historyBorder: "border-amber-400" },
	Returned: { completed: "bg-red-300 text-red-900", current: "bg-red-400 text-white", pending: "bg-red-50 text-red-500", border: "border-red-400", badge: "bg-red-200 text-red-900", text: "text-red-600", historyBorder: "border-red-400" },
	Unknown: { completed: "bg-gray-400 text-white", current: "bg-gray-500 text-white", pending: "bg-gray-100 text-gray-500", border: "border-gray-500", badge: "bg-gray-300 text-gray-800", text: "text-gray-600", historyBorder: "border-gray-500" },
};
const trackingCurrentStep = computed(() => {
	const status = commandsStore.commands[commandId.value]?.last_status_command;
	if (status === null || status === undefined) {
		return 0;
	}
	const idx = trackingRoadmapSteps.findIndex((s) => s.id === Number(status));
	return Math.max(idx, 0);
});
const trackingHistory = computed(() => {
	const result: Record<string, RoadMapHistoryEntry[]> = {};
	const historyData = commandHistoryStore.commandHistoryByCommand(commandId.value);
	if (!historyData) {
		return result;
	}
	for (const entry of Object.values(historyData)) {
		const status = entry.status_command_history;
		if (status !== null && status !== undefined) {
			if (!result[status]) {
				result[status] = [];
			}
			const subStatus = entry.sub_status_command_history;
			result[status].push({
				action: entry.description_command_history || (subStatus === undefined ? "" : trackingSubStatusOptions[subStatus]) || "",
				date: entry.event_time_utc || entry.created_at,
				comment: entry.location_command_history || "",
			});
		}
	}
	return result;
});
const commandSave = async() => {
	try {
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("command.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			commandsStore.setLoadingEdition(commandId.value, false);
			return;
		}
		const id = await commandsStore.saveAllChanges(commandId.value);
		commandsStore.loadToEdition(id);
		if (isNewId(commandId.value)) {
			addNotification({ message: t("command.Created"), type: "success" });
			const tempId = commandId.value;
			commandId.value = String(id);
			commandsStore.clearEdition(tempId);
			router.push("/commands/" + commandId.value);
		} else {
			addNotification({ message: t("command.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		commandsStore.setLoadingEdition(commandId.value, false);
	}
};
const commandDelete = async() => {
	try {
		await commandsStore.deleteCommand(commandId.value);
		addNotification({ message: t("command.Deleted"), type: "success" });
		router.push("/commands");
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	commandDeleteModalShow.value = false;
};

// tracking
const trackingActivateLoading = ref(false);
const trackingStopLoading = ref(false);
const trackingResumeLoading = ref(false);
const trackingDeleteLoading = ref(false);
const trackingRefreshLoading = ref(false);

const trackingActivate = async() => {
	trackingActivateLoading.value = true;
	try {
		await commandsStore.updateCommand(commandId.value, { is_tracking_requested: true });
		commandsStore.loadToEdition(commandId.value);
		addNotification({ message: t("command.TrackingActivated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		trackingActivateLoading.value = false;
	}
};
const trackingStop = async() => {
	trackingStopLoading.value = true;
	try {
		await commandsStore.updateCommand(commandId.value, { is_tracking_requested: false });
		commandsStore.loadToEdition(commandId.value);
		addNotification({ message: t("command.TrackingStopped"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		trackingStopLoading.value = false;
	}
};
const trackingResume = async() => {
	trackingResumeLoading.value = true;
	try {
		await commandsStore.updateCommand(commandId.value, { is_tracking_requested: true });
		commandsStore.loadToEdition(commandId.value);
		addNotification({ message: t("command.TrackingResumed"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		trackingResumeLoading.value = false;
	}
};
const trackingDelete = async() => {
	trackingDeleteLoading.value = true;
	try {
		await commandsStore.updateCommand(commandId.value, { tracking_number_command: "" });
		commandsStore.loadToEdition(commandId.value);
		addNotification({ message: t("command.TrackingDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		trackingDeleteLoading.value = false;
	}
};
const trackingRefresh = async() => {
	trackingRefreshLoading.value = true;
	try {
		await commandsStore.getCommandById(commandId.value);
		commandsStore.loadToEdition(commandId.value);
		addNotification({ message: t("command.TrackingRefreshed"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		trackingRefreshLoading.value = false;
	}
};
const trackingOptionalConfig = computed(() => {
	const ed = commandsStore.commandEdition[commandId.value];
	const command = commandsStore.commands[commandId.value];
	const base = !isNewId(commandId.value) && !!ed?.tracking_number_command && !!ed?.id_carrier;
	return [
		{
			label: "command.TrackingActivate",
			action: trackingActivate,
			showCondition: base && !ed?.is_tracking_requested,
			bgColor: "bg-green-500",
			hoverColor: "hover:bg-green-600",
			loading: trackingActivateLoading.value,
		},
		{
			label: "command.TrackingStop",
			action: trackingStop,
			showCondition: base && !!command?.is_tracking_validated && !!command?.is_active,
			bgColor: "bg-orange-500",
			hoverColor: "hover:bg-orange-600",
			loading: trackingStopLoading.value,
		},
		{
			label: "command.TrackingResume",
			action: trackingResume,
			showCondition: base && !!command?.is_tracking_validated && !command?.is_active,
			bgColor: "bg-green-500",
			hoverColor: "hover:bg-green-600",
			loading: trackingResumeLoading.value,
		},
		{
			label: "command.TrackingDelete",
			action: trackingDelete,
			showCondition: base && !!command?.is_tracking_validated,
			bgColor: "bg-red-500",
			hoverColor: "hover:bg-red-600",
			loading: trackingDeleteLoading.value,
		},
		{
			label: "command.TrackingRefresh",
			action: trackingRefresh,
			showCondition: base && !!command?.is_tracking_validated && !!command?.is_active,
			bgColor: "bg-blue-500",
			hoverColor: "hover:bg-blue-600",
			loading: trackingRefreshLoading.value,
		},
	];
});

// document
const documentAddModalShow = ref(false);
const documentAdd = async(files: { name: string; document: File }[]) => {
	for (const file of files) {
		const documentModalData = { name_command_document: file.name, document: file.document, type_command_document: file.document.type, created_at: new Date() };
		const newId = commandDocumentsStore.getAvailableNewCommandDocumentId({ id_command: commandId.value });
		commandDocumentsStore.commandDocumentEditionByCommand(commandId.value)[newId] = documentModalData;
		try {
			schemaAddDocument.validateSync(documentModalData, { abortEarly: false });
			commandDocumentsStore.valideCommandDocumentEditionByKey({ id_command: commandId.value, id_command_document: newId }, "created", true);
			delete commandDocumentsStore.commandDocumentEditionByCommand(commandId.value)[newId];
			addNotification({ message: t("command.DocumentAdded"), type: "success" });
		} catch (e) {
			addNotification({ message: e, type: "error" });
		}
	}
	documentAddModalShow.value = false;
};
const documentEdit = (row: TableauRowData) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		commandDocumentsStore.valideCommandDocumentEditionByKey({ id_command: commandId.value, id_command_document: row.id_command_document },
			commandDocumentsStore.commandDocumentReadyByCommand(commandId.value)?.[row.id_command_document]?.status === "created" ? "created" : "modified");
		delete commandDocumentsStore.commandDocumentEditionByCommand(commandId.value)[row.id_command_document];
		addNotification({ message: t("command.DocumentUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const documentRestore = (row: TableauRowData) => {
	try {
		delete commandDocumentsStore.commandDocumentReadyByCommand(commandId.value)[row.id_command_document];
		addNotification({ message: t("command.DocumentRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const documentDelete = (row: TableauRowData) => {
	try {
		commandDocumentsStore.valideCommandDocumentEditionByKey({ id_command: commandId.value, id_command_document: row.id_command_document }, "deleted");
		addNotification({ message: t("command.DocumentDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const documentDownload = async(fileContent: TableauRowData) => {
	const file = await commandDocumentsStore.downloadCommandDocument(commandId.value, fileContent.id_command_document);
	downloadFile(file, { keyName: fileContent.name_command_document, keyType: fileContent.type_command_document });
};
const documentView = async(fileContent: TableauRowData) => {
	const file = await commandDocumentsStore.downloadCommandDocument(commandId.value, fileContent.id_command_document);
	if (viewFile(file, { keyName: fileContent.name_command_document, keyType: fileContent.type_command_document })) {
		addNotification({ message: t("command.DocumentOpenInNewTab"), type: "success" });
	} else {
		addNotification({ message: t("command.DocumentNotSupported"), type: "error" });
	}
};

// item
const itemModalShow = ref(false);
const itemSave = (row: TableauRowData) => {
	try {
		schemaItem.validateSync(row, { abortEarly: false });
		itemCommandsStore.valideItemCommandEditionByKey({ id_command: commandId.value, id_item: row.id_item },
			itemCommandsStore.itemCommandsByCommand(commandId.value)?.[row.id_item] ? "modified" : "created");
		delete itemCommandsStore.itemCommandEditionByCommand(commandId.value)[row.id_item];
		addNotification({ message: t("command.ItemUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const itemDelete = (row: TableauRowData) => {
	try {
		if (itemCommandsStore.itemCommandReadyByCommand(commandId.value)?.[row.id_item]?.status === "created") {
			delete itemCommandsStore.itemCommandReadyByCommand(commandId.value)[row.id_item];
		} else {
			itemCommandsStore.valideItemCommandEditionByKey({ id_command: commandId.value, id_item: row.id_item }, "deleted");
		}
		addNotification({ message: t("command.ItemDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const itemRestore = (row: TableauRowData) => {
	try {
		delete itemCommandsStore.itemCommandReadyByCommand(commandId.value)[row.id_item];
		addNotification({ message: t("command.ItemRestored"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const filterItem = ref<FilterLabel[]>([
	{ key: "reference_name_item", value: "", type: "text", label: "", placeholder: t("command.ItemFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

const createSchema = () => {
	const edition = commandsStore.commandEdition[commandId.value];
	const shape: Record<string, Yup.AnySchema> = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.price_command = Yup.number()
		.nullable()
		.optional()
		.min(0, t("command.PriceMin"))
		.typeError(t("command.PriceNumber"));
	shape.url_command = Yup.string()
		.nullable()
		.optional()
		.max(configsStore.getConfigNumber("max_length_url"), t("command.UrlMaxLength", { count: configsStore.getConfigNumber("max_length_url") }))
		.url(t("command.UrlInvalid"));
	shape.date_command = Yup.date()
		.typeError(t("command.DateInvalid"))
		.required(t("command.DateRequired"));
	shape.status_command = Yup.mixed()
		.required(t("command.StatusRequired"));
	shape.date_delivery_command = Yup.date()
		.nullable()
		.optional();
	shape.tracking_number_command = Yup.string()
		.max(configsStore.getConfigNumber("max_length_name"), t("command.TrackingNumberMaxLength", { count: configsStore.getConfigNumber("max_length_name") }))
		.nullable()
		.optional();
	return Yup.object().shape(shape);
};

const schemaAddDocument = Yup.object().shape({
	name_command_document: Yup.string()
		.max(configsStore.getConfigNumber("max_length_name"), t("command.DocumentNameMaxLength", { count: configsStore.getConfigNumber("max_length_name") }))
		.required(t("command.DocumentNameRequired")),
	document: Yup.mixed()
		.required(t("command.DocumentRequired"))
		.test("fileSize", t("command.DocumentSize", { count: configsStore.getConfigNumber("max_size_document_in_mb") }), (value) => !value || (value as File).size <= (Number(configsStore.getConfigNumber("max_size_document_in_mb"))) * 1024 * 1024),
});
const schemaEditDocument = Yup.object().shape({
	name_command_document: Yup.string()
		.max(configsStore.getConfigNumber("max_length_name"), t("command.DocumentNameMaxLength", { count: configsStore.getConfigNumber("max_length_name") }))
		.required(t("command.DocumentNameRequired")),
});
const schemaItem = Yup.object().shape({
	quantity_command_item: Yup.number()
		.required(t("command.ItemQuantityRequired"))
		.typeError(t("command.ItemQuantityNumber"))
		.min(1, t("command.ItemQuantityMin")),
	price_command_item: Yup.number()
		.required(t("command.ItemPriceRequired"))
		.typeError(t("command.ItemPriceNumber"))
		.min(1, t("command.ItemPriceMin")),
});

const labelForm = computed<FormLabel[]>(() => [
	{ key: "price_command", label: "command.Price", type: "number" },
	{ key: "url_command", label: "command.Url", type: "text" },
	{ key: "date_command", label: "command.Date", type: "datetime-local" },
	{ key: "status_command", label: "command.Status", type: "select", typeData: "number", options: commandStatusOptions },
	{ key: "date_delivery_command", label: "command.DeliveryDate", type: "datetime-local" },
	{ key: "tracking_number_command", label: "command.TrackingNumber", type: "text" },
	{ key: "id_carrier", label: "command.Carrier", type: "fetch-select", fetchFunction: (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) =>
		carriersStore.getCarrierByInterval(limit, offset, expand, filter, sort, clear),
	fetchStore: carriersStore.carriers, fetchValueKey: "id_carrier", fetchStoreKey: "name",
	},
	{ key: "is_tracking_requested", label: "command.IsTrackingRequested", type: "checkbox", enableCondition: "false" },
	{ key: "is_tracking_validated", label: "command.IsTrackingValidated", type: "checkbox", enableCondition: "false" },
	{ key: "is_active", label: "command.IsActive", type: "checkbox", enableCondition: "false" },
	{ key: "shipper_address_command", label: "command.ShipperAddress", type: "readonly" },
	{ key: "recipient_address_command", label: "command.RecipientAddress", type: "readonly" },
	{ key: "last_status_command", label: "command.LastStatus", type: "readonly" },
]);
const labelTableauDocument = ref<TableauLabel[]>([
	{ label: "command.DocumentName", sortable: true, key: "name_command_document", valueKey: "name_command_document", type: "text", canEdit: true },
	{ label: "command.DocumentType", sortable: true, key: "type_command_document", valueKey: "type_command_document", type: "text" },
	{ label: "command.DocumentDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "command.DocumentActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_command_document && ready?.status !== 'deleted'",
			action: (row) => {
				commandDocumentsStore.commandDocumentEditionByCommand(commandId.value)[row.id_command_document] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_command_document",
			action: (row) => {
				delete commandDocumentsStore.commandDocumentEditionByCommand(commandId.value)[row.id_command_document];
			},
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_command_document",
			action: (row) => documentEdit(commandDocumentsStore.commandDocumentEditionByCommand(commandId.value)[row.id_command_document]),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-eye",
			action: (row) => documentView(row),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-download",
			action: (row) => documentDownload(row),
			class: "px-3 py-1 bg-yellow-500 text-white rounded-lg hover:bg-yellow-600",
			animation: true,
		},
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row) => documentRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row) => documentDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauItem = ref<TableauLabel[]>([
	{ label: "command.ItemName", sortable: true, key: "Item.reference_name_item", sourceKey: "id_item", type: "text",
		storeRessourceId: 1, valueKey: "reference_name_item" },

	{ label: "command.ItemQuantity", sortable: true, key: "quantity_command_item", valueKey: "quantity_command_item", type: "number", canEdit: true },
	{ label: "command.ItemPrice", sortable: true, key: "price_command_item", valueKey: "price_command_item", type: "number", canEdit: true },
	{ label: "command.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_item && ready?.status !== 'deleted'",
			action: (row) => {
				itemCommandsStore.itemCommandEditionByCommand(commandId.value)[row.id_item] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_item",
			action: (row) => itemSave(itemCommandsStore.itemCommandEditionByCommand(commandId.value)[row.id_item]),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_item",
			action: (row) => {
				delete itemCommandsStore.itemCommandEditionByCommand(commandId.value)[row.id_item];
			},
			class: "px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500",
		},
		{
			label: "",
			showCondition: "ready?.status === 'deleted'",
			icon: "fa-solid fa-rotate-left",
			action: (row) => itemRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			showCondition: "ready?.status !== 'deleted'",
			icon: "fa-solid fa-trash",
			action: (row) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauModalItem = ref<TableauLabel[]>([
	{ label: "command.ItemName", sortable: true, key: "reference_name_item", valueKey: "reference_name_item", type: "text" },

	{ label: "command.ItemQuantity", sortable: true, key: "Item.quantity_command_item", sourceKey: "id_item", type: "text",
		storeRessourceId: 1, valueKey: "quantity_command_item", canEdit: true },

	{ label: "command.ItemPrice", sortable: true, key: "Item.price_command_item", sourceKey: "id_item", type: "text",
		storeRessourceId: 1, valueKey: "price_command_item", canEdit: true },

	{ label: "command.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "!ready?.status && store[1]?.[rowData.id_item] === undefined && !edition?.id_item",
			action: (row) => {
				itemCommandsStore.itemCommandEditionByCommand(commandId.value)[row.id_item] = { price_command_item: 1, quantity_command_item: 1, id_item: row.id_item };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!ready?.status && store[1]?.[rowData.id_item] && !edition?.id_item",
			action: (row) => {
				itemCommandsStore.itemCommandEditionByCommand(commandId.value)[row.id_item] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_item",
			action: (row) => itemSave(itemCommandsStore.itemCommandEditionByCommand(commandId.value)[row.id_item]),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_item",
			action: (row) => {
				delete itemCommandsStore.itemCommandEditionByCommand(commandId.value)[row.id_item];
			},
			class: "px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500",
		},
		{
			label: "",
			icon: "fa-solid fa-rotate-left",
			showCondition: "ready?.status === 'deleted'",
			action: (row) => itemRestore(row),
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "(store[1]?.[rowData.id_item] || ready?.status === 'created') && ready?.status !== 'deleted'",
			action: (row) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
useViewScroll(true);
useTabGuard(commandsStore, () => commandId.value, "commandEdition", "commands", [
	{ key: "documentReady", get: (id) => commandDocumentsStore.commandDocumentReadyByCommand(id), set: (id, entries) => commandDocumentsStore.setCommandDocumentStagedSlice("ready", { id_command: id }, entries) },
	{ key: "itemReady", get: (id) => itemCommandsStore.itemCommandReadyByCommand(id), set: (id, entries) => itemCommandsStore.setItemCommandStagedSlice("ready", { id_command: id }, entries) },
]);
</script>
<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('command.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/commands',
				create: { showCondition: isNewId(commandId) && authStore.hasPermission([0, 1, 2]), loading: commandsStore.commandEdition[commandId]?.loading },
				update: { showCondition: !isNewId(commandId) && authStore.hasPermission([0, 1, 2]), loading: commandsStore.commandEdition[commandId]?.loading },
				delete: { showCondition: !isNewId(commandId) && authStore.hasPermission([0, 1, 2]) }
			}"
			:optional-config="trackingOptionalConfig"
			@button-create="commandSave" @button-update="commandSave" @button-delete="commandDeleteModalShow = true"/>
	</div>
	<div v-if="commandsStore.commands[commandId] || isNewId(commandId)" class="w-full">
		<RoadMap v-if="!isNewId(commandId)"
			:steps="commandRoadmapSteps"
			:current-step="commandCurrentStep"
			:step-colors="commandRoadmapStepColors"
			mode="horizontal-bottom"
		/>
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-edition="commandsStore.commandEdition[commandId]" :store-data="commandsStore.commands[commandId]" />
			<RoadMap
				v-if="!isNewId(commandId) && commandsStore.commands[commandId]?.last_status_command !== null && commandsStore.commands[commandId]?.last_status_command !== undefined"
				:steps="trackingRoadmapSteps"
				:current-step="trackingCurrentStep"
				:step-colors="trackingRoadmapStepColors"
				:history="trackingHistory"
				mode="vertical-right"
			/>
		</div>
		<CollapsibleSection title="command.Documents"
			:total-count="commandDocumentsStore.commandDocumentsCountByCommand(commandId)">
			<template #append-row>
				<button type="button" @click="documentAddModalShow = true"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('command.AddDocument') }}
				</button>
				<Tableau :labels="labelTableauDocument" :meta="{ key: 'id_command_document' }"
					:store-data="[commandDocumentsStore.commandDocumentsByCommand(commandId)]"
					:store-edition="commandDocumentsStore.commandDocumentEditionByCommand(commandId)"
					:store-ready="commandDocumentsStore.commandDocumentReadyByCommand(commandId)"
					:schema="schemaEditDocument"
					:loading="commandDocumentsStore.commandDocumentsLoading"
					:total-count="commandDocumentsStore.commandDocumentsCountByCommand(commandId)"
					:fetch-function="!isNewId(commandId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => commandDocumentsStore.getCommandDocumentByInterval({ id_command: commandId }, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="command.Items"
			:total-count="itemCommandsStore.itemCommandsCountByCommand(commandId)">
			<template #append-row>
				<button type="button" @click="itemModalShow = true"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('command.AddItem') }}
				</button>
				<Tableau :labels="labelTableauItem" :meta="{ key: 'id_item', expand: ['item'] }"
					:store-data="[itemCommandsStore.itemCommandsByCommand(commandId),itemsStore.items]"
					:store-edition="itemCommandsStore.itemCommandEditionByCommand(commandId)"
					:store-ready="itemCommandsStore.itemCommandReadyByCommand(commandId)"
					:schema="schemaItem"
					:loading="itemCommandsStore.itemCommandsLoading"
					:total-count="itemCommandsStore.itemCommandsCountByCommand(commandId)"
					:fetch-function="!isNewId(commandId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => itemCommandsStore.getItemCommandByInterval({ id_command: commandId }, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="command.Comments"
			:total-count="commandCommentsStore.commandCommentsCountByCommand(commandId)" :permission="!isNewId(commandId)">
			<template #append-row>
				<Comment :meta="{ contenu: 'content_command_comment', key: 'id_command_comment', canEdit: true, roleRequired: authStore.hasPermission([1, 2]), expand: ['user'] }"
					:store-data="[commandCommentsStore.commandCommentsByCommand(commandId), usersStore.users]"
					:store-user="authStore.user" :store-config="configsStore"
					:store-edition="commandCommentsStore.commandCommentEditionByCommand(commandId)"
					:store-function="{
						create: (data: Record<string, string>) => commandCommentsStore.createCommandComment({ id_command: commandId }, { content_command_comment: data.content_command_comment }),
						update: (id: string, data: Record<string, string>) => commandCommentsStore.updateCommandComment({ id_command: commandId, id_command_comment: id }, { content_command_comment: data.content_command_comment }),
						delete: (id: string) => commandCommentsStore.deleteCommandComment({ id_command: commandId, id_command_comment: id }),
					}"
					:loading="commandCommentsStore.commandCommentsLoading" :texte-modal-delete="{ textTitle: 'command.CommentDeleteTitle', textP: 'command.CommentDeleteText' }"
					:total-count="commandCommentsStore.commandCommentsCountByCommand(commandId)"
					:fetch-function="!isNewId(commandId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => commandCommentsStore.getCommandCommentByInterval({ id_command: commandId }, limit, offset, expand, filter, sort, clear) : undefined"
				/>
			</template>
		</CollapsibleSection>
	</div>
	<div v-else>
		<div>{{ $t('command.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="commandDeleteModalShow" @close-modal="commandDeleteModalShow = false"
		:delete-action="commandDelete" :text-title="'command.DeleteTitle'"
		:text-p="'command.DeleteText'"/>

	<ModalMultipleFiles
		:show-modal="documentAddModalShow"
		@close-modal="documentAddModalShow = false"
		@files-saved="documentAdd"
		file-type="document"
	/>

	<div v-if="itemModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center"
		@click="itemModalShow = false">
		<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('command.ItemTitle') }}</h2>
				<button type="button" @click="itemModalShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<FilterContainer class="my-4 flex gap-4" :filters="filterItem" :store-data="itemsStore.items" />

			<Tableau :labels="labelTableauModalItem" :meta="{ key: 'id_item' }"
				:store-data="[itemsStore.items,itemCommandsStore.itemCommandsByCommand(commandId)]"
				:store-edition="itemCommandsStore.itemCommandEditionByCommand(commandId)"
				:store-ready="itemCommandsStore.itemCommandReadyByCommand(commandId)"
				:filters="filterItem"
				:loading="itemCommandsStore.itemCommandsLoading" :schema="schemaItem"
				:total-count="Number(itemsStore.itemsTotalCount || 0)"
				:fetch-function="!isNewId(commandId) ? (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => itemsStore.getItemByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>
<script setup>
import { onMounted, onBeforeUnmount, ref, inject, computed } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const commandId = ref(route.params.id);
const preset = ref(route.query.preset || null);

import { downloadFile, viewFile } from "@/utils";

import CommandStatus from "@/enums/CommandStatus";
import TrackingStatus from "@/enums/TrackingStatus";
import TrackingSubStatus from "@/enums/TrackingSubStatus";

import { useConfigsStore, useCommandsStore, useUsersStore, useItemsStore, useCarriersStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const commandsStore = useCommandsStore();
const usersStore = useUsersStore();
const itemsStore = useItemsStore();
const carriersStore = useCarriersStore();
const authStore = useAuthStore();

const formContainer = ref(null);

async function fetchAllData() {
	if (Object.keys(carriersStore.carriers).length === 0) {
		carriersStore.getCarrierByInterval(200, 0, "", "", false);
	}
	if (commandId.value === "new") {
		loadToEdition(commandId.value);
		if (preset.value) {
			preset.value.split(";").forEach((pair) => {
				const [key, value] = pair.split(":");
				if (key && value) {
					commandsStore.commandEdition[key] = value;
				}
			});
		}
	} else {
		commandsStore.commandEdition = {
			loading: true,
		};
		try {
			await commandsStore.getCommandById(commandId.value);
		} catch {
			delete commandsStore.commands[commandId.value];
			addNotification({ message: t("command.NotFound"), type: "error" });
			router.push("/commands");
			return;
		}
		loadToEdition(commandId.value);
		commandsStore.getHistoryByInterval(commandId.value);
		usersStore.users[authStore.user.id_user] = authStore.user; // avoids undefined user when the current user posts first comment
	}
}
function loadToEdition(id) {
	if (id === "new") {
		commandsStore.commandEdition = {
			loading: false,
			is_tracking_requested: false,
			is_tracking_validated: false,
			is_active: true,
			tracking_number: "",
		};
	} else {
		commandsStore.commandEdition = {
			prix_command: commandsStore.commands[id].prix_command,
			url_command: commandsStore.commands[id].url_command,
			status_command: commandsStore.commands[id].status_command,
			date_command: commandsStore.commands[id].date_command,
			date_livraison_command: commandsStore.commands[id].date_livraison_command,
			tracking_number: commandsStore.commands[id].tracking_number,
			id_carrier: commandsStore.commands[id].id_carrier,
			is_tracking_requested: commandsStore.commands[id].is_tracking_requested,
			is_tracking_validated: commandsStore.commands[id].is_tracking_validated,
			is_active: commandsStore.commands[id].is_active,
			shipper_adress: commandsStore.commands[id].shipper_adress,
			recipient_adress: commandsStore.commands[id].recipient_adress,
			last_status: commandsStore.commands[id].last_status,
			loading: false,
		};
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	commandsStore.commandEdition = {
		loading: false,
	};
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
	const status = commandsStore.commandEdition?.status_command;
	if (status === null || status === undefined) {
		return 0;
	}
	const idx = commandRoadmapSteps.findIndex((s) => s.id === Number(status));
	return idx >= 0 ? idx : 0;
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
	const status = commandsStore.commandEdition?.last_status;
	if (status === null || status === undefined) {
		return 0;
	}
	const idx = trackingRoadmapSteps.findIndex((s) => s.id === Number(status));
	return idx >= 0 ? idx : 0;
});
const trackingHistory = computed(() => {
	const result = {};
	const historyData = commandsStore.history[commandId.value];
	if (!historyData) {
		return result;
	}
	for (const entry of Object.values(historyData)) {
		if (entry.status !== null && entry.status !== undefined) {
			if (!result[entry.status]) {
				result[entry.status] = [];
			}
			result[entry.status].push({
				action: entry.description || entry.sub_status || "",
				date: entry.event_time_utc || entry.created_at,
				comment: entry.location || "",
			});
		}
	}
	return result;
});
const carrierOptions = computed(() =>
	Object.fromEntries(
		Object.values(carriersStore.carriers)
			.filter((c) => c && c.id_carrier)
			.map((c) => [c.id_carrier, c.name ?? `Carrier #${c.id_carrier}`]),
	),
);
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
			commandsStore.commandEdition.loading = false;
			return;
		}
		if (commandId.value === "new") {
			const newId = await commandsStore.createCommand({ ...commandsStore.commandEdition });
			loadToEdition(newId);
			addNotification({ message: t("command.Created"), type: "success" });
			commandId.value = String(newId);
			router.push("/commands/" + commandId.value);
		} else {
			await commandsStore.updateCommand(commandId.value, { ...commandsStore.commandEdition });
			loadToEdition(commandId.value);
			addNotification({ message: t("command.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		commandsStore.commandEdition.loading = false;
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
		loadToEdition(commandId.value);
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
		loadToEdition(commandId.value);
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
		loadToEdition(commandId.value);
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
		await commandsStore.updateCommand(commandId.value, { tracking_number: "" });
		loadToEdition(commandId.value);
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
		loadToEdition(commandId.value);
		addNotification({ message: t("command.TrackingRefreshed"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		trackingRefreshLoading.value = false;
	}
};
const trackingOptionalConfig = computed(() => {
	const ed = commandsStore.commandEdition;
	const base = commandId.value !== "new" && !!ed?.tracking_number && !!ed?.id_carrier;
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
			showCondition: base && !!ed?.is_tracking_validated && !!ed?.is_active,
			bgColor: "bg-orange-500",
			hoverColor: "hover:bg-orange-600",
			loading: trackingStopLoading.value,
		},
		{
			label: "command.TrackingResume",
			action: trackingResume,
			showCondition: base && !!ed?.is_tracking_validated && !ed?.is_active,
			bgColor: "bg-green-500",
			hoverColor: "hover:bg-green-600",
			loading: trackingResumeLoading.value,
		},
		{
			label: "command.TrackingDelete",
			action: trackingDelete,
			showCondition: base && !!ed?.is_tracking_validated,
			bgColor: "bg-red-500",
			hoverColor: "hover:bg-red-600",
			loading: trackingDeleteLoading.value,
		},
		{
			label: "command.TrackingRefresh",
			action: trackingRefresh,
			showCondition: base && !!ed?.is_tracking_validated && !!ed?.is_active,
			bgColor: "bg-blue-500",
			hoverColor: "hover:bg-blue-600",
			loading: trackingRefreshLoading.value,
		},
	];
});

// document
const documentAddModalShow = ref(false);
const documentDeleteModalShow = ref(false);
const documentModalData = ref({ id_command_document: null, name_command_document: "", document: null });
const documentDeleteOpenModal = (doc) => {
	documentModalData.value = doc;
	documentDeleteModalShow.value = true;
};
const documentAdd = async(files) => {
	for (const file of files) {
		documentModalData.value = { name_command_document: file.name, document: file.document };
		try {
			schemaAddDocument.validateSync(documentModalData.value, { abortEarly: false });
			await commandsStore.createDocument(commandId.value, documentModalData.value);
			addNotification({ message: t("command.DocumentAdded"), type: "success" });
		} catch (e) {
			addNotification({ message: e, type: "error" });
		}
	}
	documentAddModalShow.value = false;
};
const documentEdit = async(row) => {
	try {
		schemaEditDocument.validateSync(row, { abortEarly: false });
		await commandsStore.updateDocument(commandId.value, row.id_command_document, row);
		delete commandsStore.documentEdition[row.id_command_document];
		addNotification({ message: t("command.DocumentUpdated"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const documentDelete = async() => {
	try {
		await commandsStore.deleteDocument(commandId.value, documentModalData.value.id_command_document);
		addNotification({ message: t("command.DocumentDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	documentDeleteModalShow.value = false;
};
const documentDownload = async(fileContent) => {
	const file = await commandsStore.downloadDocument(commandId.value, fileContent.id_command_document);
	downloadFile(file, { keyName: fileContent.name_command_document, keyType: fileContent.type_command_document });
};
const documentView = async(fileContent) => {
	const file = await commandsStore.downloadDocument(commandId.value, fileContent.id_command_document);
	if (viewFile(file, { keyName: fileContent.name_command_document, keyType: fileContent.type_command_document })) {
		addNotification({ message: t("command.DocumentOpenInNewTab"), type: "success" });
	} else {
		addNotification({ message: t("command.DocumentNotSupported"), type: "error" });
	}
};

// item
const itemModalShow = ref(false);
const itemSave = async(item) => {
	if (commandsStore.items[commandId.value][item.id_item]) {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await commandsStore.updateItem(commandId.value, item.tmp.id_item, item.tmp);
			item.tmp = null;
			addNotification({ message: t("command.ItemUpdated"), type: "success" });
		} catch (e) {
			addNotification({ message: e, type: "error" });
			return;
		}
	} else {
		try {
			schemaItem.validateSync(item.tmp, { abortEarly: false });
			await commandsStore.createItem(commandId.value, item.tmp);
			item.tmp = null;
			addNotification({ message: t("command.ItemAdded"), type: "success" });
		} catch (e) {
			addNotification({ message: e, type: "error" });
			return;
		}
	}
};
const itemDelete = async(item) => {
	try {
		await commandsStore.deleteItem(commandId.value, item.id_item);
		addNotification({ message: t("command.ItemDeleted"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const filterItem = ref([
	{ key: "reference_name_item", value: "", type: "text", label: "", placeholder: t("command.ItemFilterPlaceholder"), compareMethod: "=like=", class: "w-full" },
]);

const createSchema = () => {
	const edition = commandsStore.commandEdition;
	const shape = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.prix_command = Yup.number()
		.nullable()
		.optional()
		.min(0, t("command.PriceMin"))
		.typeError(t("command.PriceNumber"));
	shape.url_command = Yup.string()
		.nullable()
		.optional()
		.max(configsStore.getConfigByKey("max_length_url"), t("command.UrlMaxLength", { count: configsStore.getConfigByKey("max_length_url") }))
		.url(t("command.UrlInvalid"));
	shape.date_command = Yup.date()
		.typeError(t("command.DateInvalid"))
		.required(t("command.DateRequired"));
	shape.status_command = Yup.mixed()
		.required(t("command.StatusRequired"));
	shape.date_livraison_command = Yup.date()
		.nullable()
		.optional();
	shape.tracking_number = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("command.TrackingNumberMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.nullable()
		.optional();
	return Yup.object().shape(shape);
};

const schemaAddDocument = Yup.object().shape({
	name_command_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("command.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("command.DocumentNameRequired")),
	document: Yup.mixed()
		.required(t("command.DocumentRequired"))
		.test("fileSize", t("command.DocumentSize", { count: configsStore.getConfigByKey("max_size_document_in_mb") }), (value) => !value || value?.size <= (Number(configsStore.getConfigByKey("max_size_document_in_mb"))) * 1024 * 1024),
});
const schemaEditDocument = Yup.object().shape({
	name_command_document: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("command.DocumentNameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("command.DocumentNameRequired")),
});
const schemaItem = Yup.object().shape({
	qte_command_item: Yup.number()
		.required(t("command.ItemQuantityRequired"))
		.typeError(t("command.ItemQuantityNumber"))
		.min(1, t("command.ItemQuantityMin")),
	prix_command_item: Yup.number()
		.required(t("command.ItemPriceRequired"))
		.typeError(t("command.ItemPriceNumber"))
		.min(1, t("command.ItemPriceMin")),
});

const labelForm = computed(() => [
	{ key: "prix_command", label: "command.Price", type: "number" },
	{ key: "url_command", label: "command.Url", type: "text" },
	{ key: "date_command", label: "command.Date", type: "datetime-local" },
	{ key: "status_command", label: "command.Status", type: "select", typeData: "number", options: commandStatusOptions },
	{ key: "date_livraison_command", label: "command.DeliveryDate", type: "datetime-local" },
	{ key: "tracking_number", label: "command.TrackingNumber", type: "text" },
	{ key: "id_carrier", label: "command.Carrier", type: "fetch-select", fetchFunction: (limit, offset, expand, filter, sort, clear) => 
		carriersStore.getCarrierByInterval(limit, offset, filter, sort, clear),
	fetchStore: carriersStore.carriers, fetchValueKey: "id_carrier", fetchStoreKey: "name",
	},
	{ key: "is_tracking_requested", label: "command.IsTrackingRequested", type: "computed" },
	{ key: "is_tracking_validated", label: "command.IsTrackingValidated", type: "computed" },
	{ key: "is_active", label: "command.IsActive", type: "computed" },
	{ key: "shipper_adress", label: "command.ShipperAddress", type: "computed" },
	{ key: "recipient_adress", label: "command.RecipientAddress", type: "computed" },
	{ key: "last_status", label: "command.LastStatus", type: "computed" },
]);
const labelTableauDocument = ref([
	{ label: "command.DocumentName", sortable: true, key: "name_command_document", valueKey: "name_command_document", type: "text", canEdit: true },
	{ label: "command.DocumentType", sortable: true, key: "type_command_document", valueKey: "type_command_document", type: "text" },
	{ label: "command.DocumentDate", sortable: true, key: "created_at", valueKey: "created_at", type: "datetime" },
	{ label: "command.DocumentActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_command_document",
			action: (row) => {
				commandsStore.documentEdition[row.id_command_document] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_command_document",
			action: (row) => {
				delete commandsStore.documentEdition[row.id_command_document];
			},
			class: "px-3 py-1 bg-gray-500 text-white rounded-lg hover:bg-gray-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_command_document",
			action: (row) => documentEdit(commandsStore.documentEdition[row.id_command_document]),
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
			icon: "fa-solid fa-trash",
			action: (row) => documentDeleteOpenModal(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
		},
	] },
]);
const labelTableauItem = ref([
	{ label: "command.ItemName", sortable: true, key: "Item.reference_name_item", sourceKey: "id_item", type: "text", 
		storeRessourceId: 1, valueKey: "reference_name_item" },

	{ label: "command.ItemQuantity", sortable: true, key: "qte_command_item", valueKey: "qte_command_item", type: "number", canEdit: true },
	{ label: "command.ItemPrice", sortable: true, key: "prix_command_item", valueKey: "prix_command_item", type: "number", canEdit: true },
	{ label: "command.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "!edition?.id_item",
			action: (row) => {
				commandsStore.itemEdition[row.id_item] = { ...row };
			},
			type: "button",
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_item",
			action: (row) => itemSave(commandsStore.itemEdition[row.id_item]),
			type: "button",
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_item",
			action: (row) => {
				delete commandsStore.itemEdition[row.id_item];
			},
			type: "button",
			class: "px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			action: (row) => itemDelete(row),
			type: "button",
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
const labelTableauModalItem = ref([
	{ label: "command.ItemName", sortable: true, key: "reference_name_item", valueKey: "reference_name_item", type: "text" },

	{ label: "command.ItemQuantity", sortable: true, key: "Item.qte_command_item", sourceKey: "id_item", type: "text", 
		storeRessourceId: 1, valueKey: "qte_command_item", canEdit: true },

	{ label: "command.ItemPrice", sortable: true, key: "Item.prix_command_item", sourceKey: "id_item", type: "text", 
		storeRessourceId: 1, valueKey: "prix_command_item", canEdit: true },

	{ label: "command.ItemActions", sortable: false, key: "", type: "buttons", buttons: [
		{
			label: "",
			icon: "fa-solid fa-plus",
			showCondition: "store[1]?.[rowData.id_item] === undefined && !edition?.id_item",
			action: (row) => {
				commandsStore.itemEdition[row.id_item] = { prix_command_item: 1, qte_command_item: 1, id_item: row.id_item };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-edit",
			showCondition: "store[1]?.[rowData.id_item] && !edition?.id_item",
			action: (row) => {
				commandsStore.itemEdition[row.id_item] = { ...row };
			},
			class: "px-3 py-1 bg-blue-500 text-white rounded-lg hover:bg-blue-600",
		},
		{
			label: "",
			icon: "fa-solid fa-save",
			showCondition: "edition?.id_item",
			action: (row) => itemSave(commandsStore.itemEdition[row.id_item]),
			class: "px-3 py-1 bg-green-500 text-white rounded-lg hover:bg-green-600",
			animation: true,
		},
		{
			label: "",
			icon: "fa-solid fa-times",
			showCondition: "edition?.id_item",
			action: (row) => {
				delete commandsStore.itemEdition[row.id_item];
			},
			class: "px-3 py-1 bg-gray-400 text-white rounded-lg hover:bg-gray-500",
		},
		{
			label: "",
			icon: "fa-solid fa-trash",
			showCondition: "store[1]?.[rowData.id_item]",
			action: (row) => itemDelete(row),
			class: "px-3 py-1 bg-red-500 text-white rounded-lg hover:bg-red-600",
			animation: true,
		},
	] },
]);
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>
<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('command.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/commands',
				create: { showCondition: commandId === 'new' && authStore.hasPermission([0, 1, 2]), loading: commandsStore.commandEdition?.loading },
				update: { showCondition: commandId !== 'new' && authStore.hasPermission([0, 1, 2]), loading: commandsStore.commandEdition?.loading },
				delete: { showCondition: commandId !== 'new' && authStore.hasPermission([0, 1, 2]) }
			}"
			:optional-config="trackingOptionalConfig"
			@button-create="commandSave" @button-update="commandSave" @button-delete="commandDeleteModalShow = true"/>
	</div>
	<div v-if="commandsStore.commands[commandId] || commandId == 'new'" class="w-full">
		<RoadMap v-if="commandId !== 'new'"
			:steps="commandRoadmapSteps"
			:current-step="commandCurrentStep"
			:step-colors="commandRoadmapStepColors"
			mode="horizontal-bottom"
		/>
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="commandsStore.commandEdition" />
			<RoadMap
				v-if="commandId !== 'new' && commandsStore.commandEdition?.last_status !== null && commandsStore.commandEdition?.last_status !== undefined"
				:steps="trackingRoadmapSteps"
				:current-step="trackingCurrentStep"
				:step-colors="trackingRoadmapStepColors"
				:history="trackingHistory"
				mode="vertical-right"
			/>
		</div>
		<CollapsibleSection title="command.Documents"
			:total-count="Number(commandsStore.documentsTotalCount[commandId] || 0)" :permission="commandId !=='new'">
			<template #append-row>
				<button type="button" @click="documentAddModalShow = true"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('command.AddDocument') }}
				</button>
				<Tableau :labels="labelTableauDocument" :meta="{ key: 'id_command_document' }"
					:store-data="[commandsStore.documents[commandId]]"
					:store-edition="commandsStore.documentEdition"
					:schema="schemaEditDocument"
					:loading="commandsStore.documentsLoading"
					:total-count="Number(commandsStore.documentsTotalCount[commandId] || 0)"
					:fetch-function="commandId !== 'new' ? (limit, offset, expand, filter, sort, clear) => commandsStore.getDocumentByInterval(commandId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="command.Items"
			:total-count="Number(commandsStore.itemsTotalCount[commandId] || 0)" :permission="commandId !=='new'">
			<template #append-row>
				<button type="button" @click="itemModalShow = true"
					class="bg-blue-500 text-white px-4 py-2 rounded mb-4 hover:bg-blue-600">
					{{ $t('command.AddItem') }}
				</button>
				<Tableau :labels="labelTableauItem" :meta="{ key: 'id_item', expand: ['item'] }"
					:store-data="[commandsStore.items[commandId],itemsStore.items]"
					:store-edition="commandsStore.itemEdition"
					:schema="schemaItem"
					:loading="commandsStore.itemsLoading"
					:total-count="Number(commandsStore.itemsTotalCount[commandId] || 0)"
					:fetch-function="commandId !== 'new' ? (limit, offset, expand, filter, sort, clear) => commandsStore.getItemByInterval(commandId, limit, offset, expand, filter, sort, clear) : undefined"
					:tableau-css="{ component: 'max-h-64', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
				/>
			</template>
		</CollapsibleSection>
		<CollapsibleSection title="command.Commentaires"
			:total-count="Number(commandsStore.commentairesTotalCount[commandId] || 0)" :permission="commandId !=='new'">
			<template #append-row>
				<Commentaire :meta="{ contenu: 'contenu_command_commentaire', key: 'id_command_commentaire', canEdit: true, roleRequired: authStore.hasPermission([1, 2]), expand: ['user'] }"
					:store-data="[commandsStore.commentaires[commandId], usersStore.users]"
					:store-user="authStore.user" :store-config="configsStore"
					:store-function="{ create: (data) => commandsStore.createCommentaire(commandId, data), update: (id, data) => commandsStore.updateCommentaire(commandId, id, data), delete: (id) => commandsStore.deleteCommentaire(commandId, id) }"
					:loading="commandsStore.commentairesLoading" :texte-modal-delete="{ textTitle: 'command.CommentDeleteTitle', textP: 'command.CommentDeleteText' }"
					:total-count="Number(commandsStore.commentairesTotalCount[commandId] || 0)"
					:fetch-function="commandId !== 'new' ? (limit, offset, expand, filter, sort, clear) => commandsStore.getCommentaireByInterval(commandId, limit, offset, expand, filter, sort, clear) : undefined"
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

	<ModalDeleteConfirm :show-modal="documentDeleteModalShow" @close-modal="documentDeleteModalShow = false"
		:delete-action="documentDelete" :text-title="'command.DocumentDeleteTitle'"
		:text-p="'command.DocumentDeleteText'"/>

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
				:store-data="[itemsStore.items,commandsStore.items[commandId]]"
				:store-edition="commandsStore.itemEdition"
				:filters="filterItem"
				:loading="commandsStore.itemsLoading" :schema="schemaItem"
				:total-count="Number(itemsStore.itemsTotalCount || 0)"
				:fetch-function="commandId !== 'new' ? (limit, offset, expand, filter, sort, clear) => itemsStore.getItemByInterval(limit, offset, expand, filter, sort, clear) : undefined"
				:tableau-css="{ component: 'flex-1 overflow-y-auto', tr: 'transition duration-150 ease-in-out hover:bg-gray-200 even:bg-gray-10' }"
			/>
		</div>
	</div>
</template>
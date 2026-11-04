// Actions proposed by the assistant (see ProposedActionDto of the api): nothing is applied until the user validates them.
// Each type knows the tab that lets the user review it and how to describe it in the table of the proposal.

export interface AiProposedAction {
	action_type: string | null;
	payload: unknown;
}

export type AiRefKind = "item" | "tag" | "box" | "store";

// one line of the description of an action: either a plain value or a reference to an existing record
export interface AiActionDetail {
	labelKey: string;
	value?: string;
	ref?: { kind: AiRefKind, id: string };
}

interface AiActionDefinition {
	labelKey: string;
	// the resource is created by the form of this view: once saved, the tab shows the created record (/inventory/12)
	createdIn?: string;
	// tab opened to review the action
	path: (payload: Record<string, unknown>) => string;
	details: (payload: Record<string, unknown>) => AiActionDetail[];
}

const text = (value: unknown) => (value === undefined || value === null || value === "" ? undefined : String(value));
const ref = (labelKey: string, kind: AiRefKind, id: unknown): AiActionDetail => ({ labelKey, ref: { kind, id: String(id) } });
const plain = (labelKey: string, value: unknown): AiActionDetail[] => {
	const shown = text(value);
	return shown === undefined ? [] : [{ labelKey, value: shown }];
};

// A new element is opened with its form prefilled, the values are encoded one by one because ":" and ";" separate the fields of a preset
function newPath(base: string, fields: Record<string, unknown>) {
	const preset = Object.entries(fields)
		.filter(([, value]) => text(value) !== undefined)
		.map(([field, value]) => `${field}:${encodeURIComponent(String(value))}`)
		.join(";");
	return `${base}/new?preset=${encodeURIComponent(preset)}`;
}

const DEFINITIONS: Record<string, AiActionDefinition> = {
	create_item: {
		labelKey: "common.VAiActionCreateItem",
		createdIn: "/inventory",
		path: (payload) => newPath("/inventory", {
			reference_name_item: payload.reference_name_item,
			friendly_name_item: payload.friendly_name_item,
			threshold_min_item: payload.threshold_min_item,
			description_item: payload.description_item,
		}),
		details: (payload) => [
			...plain("common.VAiFieldReference", payload.reference_name_item),
			...plain("common.VAiFieldName", payload.friendly_name_item),
			...plain("common.VAiFieldThresholdMin", payload.threshold_min_item),
			...plain("common.VAiFieldDescription", payload.description_item),
		],
	},
	create_tag: {
		labelKey: "common.VAiActionCreateTag",
		createdIn: "/tags",
		path: (payload) => newPath("/tags", { name_tag: payload.name_tag, weight_tag: payload.weight_tag }),
		details: (payload) => [
			...plain("common.VAiFieldName", payload.name_tag),
			...plain("common.VAiFieldWeight", payload.weight_tag),
		],
	},
	create_box: {
		labelKey: "common.VAiActionCreateBox",
		path: (payload) => `/stores/${String(payload.id_store)}`,
		details: (payload) => [
			ref("common.VAiFieldStore", "store", payload.id_store),
			...plain("common.VAiFieldPosition", `(${String(payload.xstart_box)}, ${String(payload.ystart_box)}) → (${String(payload.xend_box)}, ${String(payload.yend_box)})`),
		],
	},
	attach_tag: {
		labelKey: "common.VAiActionAttachTag",
		path: (payload) => `/inventory/${String(payload.id_item)}`,
		details: (payload) => [
			ref("common.VAiFieldItem", "item", payload.id_item),
			ref("common.VAiFieldTag", "tag", payload.id_tag),
		],
	},
	move_item_stock: {
		labelKey: "common.VAiActionMoveStock",
		path: (payload) => `/inventory/${String(payload.id_item)}`,
		details: (payload) => [
			ref("common.VAiFieldItem", "item", payload.id_item),
			ref("common.VAiFieldBox", "box", payload.id_box),
			...plain("common.VAiFieldQuantity", payload.quantity_item_box),
			...plain("common.VAiFieldThresholdMax", payload.threshold_max_item_item_box),
		],
	},
};

const asRecord = (payload: unknown) => (payload && typeof payload === "object" ? payload as Record<string, unknown> : {});

export function aiActionDefinition(action: AiProposedAction): AiActionDefinition | undefined {
	return action.action_type ? DEFINITIONS[action.action_type] : undefined;
}

export function aiActionPath(action: AiProposedAction): string | undefined {
	return aiActionDefinition(action)?.path(asRecord(action.payload));
}

export function aiActionDetails(action: AiProposedAction): AiActionDetail[] {
	return aiActionDefinition(action)?.details(asRecord(action.payload)) ?? [];
}

// the tab of a created element has left the "new" form for the saved record
export function isAiActionSaved(action: AiProposedAction, tabPath: string): boolean {
	const base = aiActionDefinition(action)?.createdIn;
	return !!base && new RegExp(String.raw`^${base}/\d+(\?|$)`).test(tabPath);
}

// ids of the existing records an action refers to, to display their names
export function aiActionRefs(action: AiProposedAction): Array<{ kind: AiRefKind, id: string }> {
	return aiActionDetails(action).flatMap((detail) => (detail.ref ? [detail.ref] : []));
}

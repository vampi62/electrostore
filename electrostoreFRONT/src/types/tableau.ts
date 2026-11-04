import type { StateTree } from "pinia";

export interface TableauRessourcePrint {
	from: "ressource" | "link" | "text";
	valueKey?: string;
	text?: string;
}

export type TableauRowData = StateTree;

export interface TableauButton {
	label?: string;
	icon?: string;
	class?: string;
	animation?: boolean;
	showCondition?: string;
	enableCondition?: string;
	action: (row: TableauRowData) => unknown;
}

export interface TableauLabel {
	key: string;
	label: string;
	type?: string;
	sortable?: boolean;
	showCondition?: string;
	valueKey?: string;
	sourceKey?: string;
	storeLinkId?: number;
	storeRessourceId?: number;
	storeLinkKeyJoinSource?: string;
	storeLinkKeyJoinRessource?: string;
	ressourcePrint?: TableauRessourcePrint[];
	buttons?: TableauButton[];
	condition?: string;
	// type: "enum", { [value]: label }
	options?: Record<string | number, string>;
	canEdit?: boolean;
	fieldUrl?: string;
	placeholder?: string;
	// card view only: where the column is printed in the card, defaults to the first "image" column
	// as image and the first "text" column as title, the other columns are printed as "label: value"
	cardRole?: "image" | "title" | "subtitle" | "hidden";
}

export interface TableauMeta {
	key: string;
	path?: string;
	// open the path of a row in a new tab instead of the current one
	newTab?: boolean;
	sort?: string;
	sortOrder?: "asc" | "desc";
	preventClear?: boolean;
	expand?: string[];
	saveState?: boolean;
	stateKey?: string;
	linkEditionKey?: string;
	// show the table/card switch (read only cards, canEdit is ignored in card view)
	cardView?: boolean;
}

export type TableauViewMode = "table" | "card";

export interface TableauCss {
	component?: string;
	table?: string;
	thead?: string;
	th?: string;
	tbody?: string;
	tr?: string;
	td?: string;
}

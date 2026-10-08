export interface TableauRessourcePrint {
	from: "ressource" | "link" | "text";
	valueKey?: string;
	text?: string;
}

export interface TableauButton {
	label?: string;
	icon?: string;
	class?: string;
	type?: string;
	animation?: boolean;
	showCondition?: string;
	enableCondition?: string;
	action?: (row: TableauRowData) => any;
	[key: string]: any;
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
	options?: Record<string, any>;
	canEdit?: boolean;
	fieldUrl?: string;
	placeholder?: string;
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
}

export interface TableauCss {
	component?: string;
	table?: string;
	thead?: string;
	th?: string;
	tbody?: string;
	tr?: string;
	td?: string;
}

export type TableauRowData = Record<string, any>;

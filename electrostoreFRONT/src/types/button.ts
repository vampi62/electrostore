// state of one of the fixed buttons of a view (create, update, delete)
export interface ActionButton {
	showCondition?: boolean;
	enableCondition?: boolean;
	loading?: boolean;
}

// optional button of a view
export interface TopButton extends ActionButton {
	// translation key for the label
	label: string;
	bgColor?: string;
	hoverColor?: string;
	action?: () => unknown;
}

export interface TopButtonMainConfig {
	// path to go back to when there is no history
	path: string;
	create: ActionButton;
	update: ActionButton;
	delete: ActionButton;
}

// preset buttons known by the component: they provide the label, the colors and the event emitted
export type TopButtonType = "create" | "update" | "delete" | "restore" | "hardDelete" | "back";

// when a button is visible, depending on the soft delete state of the ressource (see the deletedAt prop of the component)
// - active: the ressource is not soft deleted
// - deleted: the ressource is soft deleted
// - always: whatever the state
export type TopButtonVisibleWhen = "active" | "deleted" | "always";

// button of a view, displayed in the order of the array given to the component
export interface TopButton {
	// preset of the button, when omitted the button is a custom one (label and action are then required)
	type?: TopButtonType;
	// translation key for the label (overrides the one of the preset)
	label?: string;
	// tailwind classes (override the ones of the preset)
	bgColor?: string;
	hoverColor?: string;
	textColor?: string;
	// overrides the default of the preset (always for a custom button)
	visibleWhen?: TopButtonVisibleWhen;
	// if false, the button is hidden (default true), this is where the permissions of the user are checked
	showCondition?: boolean;
	// if false, the button is disabled and grayed out (default true)
	enableCondition?: boolean;
	// if true, the button shows a loading spinner
	loading?: boolean;
	// called on click, replaces the event emitted by a preset
	action?: () => unknown;
}

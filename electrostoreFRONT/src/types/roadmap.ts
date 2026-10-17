export type RoadMapMode = "horizontal-top" | "horizontal-bottom" | "horizontal-left" | "horizontal-right" | "vertical-top" | "vertical-bottom" | "vertical-left" | "vertical-right";

export interface RoadMapStep {
	id: number;
	name: string;
}

// tailwind classes of a step
export interface RoadMapStepColor {
	completed: string;
	current: string;
	pending: string;
	border: string;
	badge: string;
	text: string;
	historyBorder: string;
}

export interface RoadMapHistoryEntry {
	action: string;
	date?: string | Date | null;
	user?: string;
	comment?: string;
}

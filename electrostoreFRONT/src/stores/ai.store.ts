import { defineStore } from "pinia";

import type { AiProposedAction } from "@/helpers";

// one proposed action of the assistant, nothing is applied until the user validates it (in a tab or from the table)
export interface PlanRow {
	id: number;
	action: AiProposedAction;
	applying: boolean;
	applied: boolean;
	error: string;
	// tab opened to edit the proposal, and the page of the record once created from the table
	tabId: string | null;
	createdPath: string | null;
}

// one message of the exchange, sent back to the api to let the assistant rework its proposals
export interface AiHistoryMessage {
	role: "user" | "assistant";
	content: string;
}

// The plan of the assistant lives in a store (and not in the modal) to let the navbar tell the user that some proposals are waiting for a validation
export const useAiStore = defineStore("ai", {
	state: () => ({
		// answer of the assistant, null before the first one
		reply: null as string | null,
		rows: [] as PlanRow[],
		raw: "",
		nextRowId: 1,
		history: [] as AiHistoryMessage[],
	}),
	getters: {
		// proposals not validated yet (a pending one, being edited in a tab or in error)
		pendingCount: (state) => state.rows.filter((row) => !row.applied).length,
	},
	actions: {
		removeRow(id: number) {
			const index = this.rows.findIndex((row) => row.id === id);
			if (index !== -1) {
				this.rows.splice(index, 1);
			}
		},
		reset() {
			this.reply = null;
			this.rows = [];
			this.raw = "";
			this.history = [];
		},
	},
});

import type { MaybeRef } from "vue";
import type { StateTree } from "pinia";
import type { Schema } from "yup";
import type { FetchFunction } from "@/types/fetch";

// yup schema returned by the schemaBuilder of a form
export type FormSchema = Schema;

// helper functions of a view used in the conditions of the labels (func.hasPermission([2]))
export type FormFunction = Record<string, (...args: never[]) => unknown>;

// option of a select / multi-checkbox: the label, or the label with its state
export type FormOption = string | { label: string; disabled?: boolean; hidden?: boolean };

export interface FormLabel {
	// key in the storeData for the field
	key: string;
	// translation key for the label
	label: string;
	text?: string;
	// input type, e.g. 'text', 'number', 'select', 'checkbox', 'password', 'textarea', 'computed', 'custom'
	type?: string;
	// JavaScript expressions evaluated to enable / show the field
	enableCondition?: string;
	showCondition?: string;
	typeData?: string;
	placeholder?: string;
	// displayed by type: "computed", a computed can be given
	value?: MaybeRef<string | number | boolean | null>;
	loading?: boolean;
	// select inputs: { [value]: option }
	options?: Record<string | number, FormOption>;
	sort?: "asc" | "desc";
	// textarea inputs
	rows?: number;
	// type: "fetch-select", fetchStore holds the elements to select, fetchStoreKey is the field displayed and fetchValueKey the field stored
	fetchStore?: StateTree;
	fetchStoreKey?: string;
	fetchValueKey?: string;
	fetchFunction?: FetchFunction;
}

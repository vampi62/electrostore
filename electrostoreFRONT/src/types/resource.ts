import type { StoreGeneric } from "pinia";

export interface EntityTypes {
	readExtended?: unknown;
	readBasic?: unknown;
	create?: unknown;
	update?: unknown;
	createBulk?: unknown;
	updateBulk?: unknown;
	deleteBulk?: unknown;
}

export type Body<T extends EntityTypes, K extends keyof EntityTypes> =
	T extends Record<K, infer B> ? B : never;

export type IdKey<T extends EntityTypes> = Extract<keyof T["readExtended"], string> | Extract<keyof T["readBasic"], string>;

export interface MainResourceOptions<T extends EntityTypes> {
	path: () => string;
	idField: IdKey<T>
	countKey: string;
	stateKey: string;
	loadingKey: string;
	editionKey?: string;
	onHydrate?: (store: StoreGeneric, entity: T["readExtended"], expand: string[]) => void;
}

export interface NestedResourceOptions<T extends EntityTypes> {
	path: (idParentResource: string) => string;
	idField: IdKey<T>;
	countKey: string;
	stateKey: string;
	loadingKey: string;
	editionKey?: string;
	readyKey?: string;
	onHydrate?: (store: StoreGeneric, entity: T["readExtended"], expand: string[]) => void;
}

type PartialBody<B> = [B] extends [object] ? Partial<B> : unknown;

// Staged change of an entity: its fields must be valid fields of the create or update dto of EntityTypes.
// Usage: Record<string, Ready<{ create: CreateXDto, update: UpdateXDto }>>
export type Ready<T extends Pick<EntityTypes, "create" | "update">> = {
	status?: "created" | "modified" | "deleted";
	isFormData?: boolean;
	pushChange?: boolean;
} & PartialBody<NonNullable<T["create"]>> & PartialBody<NonNullable<T["update"]>>;

export type Edition<T extends Pick<EntityTypes, "create" | "update">> = {
	isFormData?: boolean;
	loading?: boolean;
} & PartialBody<NonNullable<T["create"]>> & PartialBody<NonNullable<T["update"]>>;

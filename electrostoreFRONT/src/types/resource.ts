import type { StoreGeneric } from "pinia";

export interface EntityTypes {
	readExtended: unknown;
	readBasic?: unknown;
	create?: unknown;
	update?: unknown;
	createBulk?: unknown;
	updateBulk?: unknown;
	deleteBulk?: unknown;
}

export type Body<T extends EntityTypes, K extends keyof EntityTypes> =
	T extends Record<K, infer B> ? B : never;

export type IdKey<T extends EntityTypes> = Extract<keyof T["readExtended"], string>;

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

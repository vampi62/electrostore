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

// Values of some fields of a composite key. Complete, it identifies one record; partial, it selects a slice of them.
// Any field can be given to reach a route (e.g. id_store for a box), the ones of the DTO are suggested.
export type KeyParts<T extends EntityTypes> = Partial<Record<IdKey<T>, string>> & Partial<Record<string, string>>;

// One way to reach a resource. Several routes can serve the same one (/item/{id_item}/tag and /tag/{id_tag}/item):
// the first whose key fields are all given is used.
export interface ResourceRoute<T extends EntityTypes> {
	keys: string[];
	url: (parts: KeyParts<T>) => string;
}

// Resource identified by several fields (junction table): records are stored under the joined key, e.g. "3:12" for { id_item: 3, id_tag: 12 }.
// countKey is indexed by scope ("id_item=3"), the others (stateKey, editionKey, readyKey) by composite key.
export interface ResourceOptions<T extends EntityTypes> {
	compositeKey: Array<IdKey<T>>;
	// Fields only the urls need (e.g. id_store to reach a box): they are not part of the scope of the records and of their count
	routeOnlyFields?: string[];
	// Key field the api assigns when the record is created: it is a temporary "new-x" id until then
	generatedField?: IdKey<T>;
	path: Array<ResourceRoute<T>>;
	entityPath: Array<ResourceRoute<T>>;
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

// The generated multipart dtos type the files as string (binary), the fields K hold a File.
export type WithFile<T, K extends keyof T> = { [P in keyof T]: P extends K ? Exclude<T[P], string> | File : T[P] };

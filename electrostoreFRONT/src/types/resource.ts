export interface MainResourceOptions {
	path: () => string;
	idField: string;
	countKey: string;
	stateKey: string;
	loadingKey: string;
	editionKey?: string;
	onHydrate?: (store: any, entity: any, expand: string[], externalParam: any[]) => void;
}

export interface NestedResourceOptions {
	path: (idParentResource: any) => string;
	idField: string;
	countKey: string;
	stateKey: string;
	loadingKey: string;
	editionKey?: string;
	readyKey?: string;
	onHydrate?: (store: any, entity: any, expand: string[], externalParam: any[]) => void;
}

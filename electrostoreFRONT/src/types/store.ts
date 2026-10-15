export type ShowParams = Record<string, number>;

export interface StoreGridFunction {
	showLedById: (id: string, data: ShowParams) => Promise<unknown>;
	showBoxById: (id: string, data: ShowParams) => Promise<unknown>;
}

export type StoreGridElementType = "led" | "box" | "border";

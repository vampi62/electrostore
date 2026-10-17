export interface CommentMeta {
	key: string;
	contenu: string;
	canEdit: boolean;
	idRessource?: string;
	link?: string;
	roleRequired?: boolean;
	expand?: string[];
}

export interface CommentStoreFunction {
	create: (data: Record<string, string>) => Promise<unknown>;
	update: (id: string, data: Record<string, string>) => Promise<unknown>;
	delete: (id: string) => Promise<unknown>;
}

export interface CommentDeleteText {
	textTitle: string;
	textP: string;
}

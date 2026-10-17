import type { FetchFunction } from "@/types/fetch";
import type { TableauCss, TableauLabel, TableauMeta } from "@/types/tableau";

// fields of a tag used by the component Tags
export interface TagsMeta {
	// field holding the name of a tag
	keyName: string;
	// field holding the weight of a tag, the tags are sorted by weight
	keyPoids: string;
}

// configuration of the Tableau used to pick the tags
export interface TagsTableauModal {
	label: TableauLabel[];
	meta: TableauMeta;
	css?: TableauCss;
	loading: boolean;
	fetchFunction: FetchFunction;
	totalCount: number;
}

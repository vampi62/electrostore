import type { RSQLFilter, RSQLSort } from "@/types/rsql";

export type TokenType = "access" | "refresh" | null;

export interface RequestParams {
	url: string;
	body?: unknown;
	useToken?: TokenType;
	contentFile?: boolean;
}

export interface SorterDto {
	field: string;
	order: "asc" | "desc";
}

export interface FilterDto {
	field: string;
	search_type: string;
	value: string;
}

export interface PaginationDto {
	limit: number;
	offset: number;
	total: number;
	next_offset?: number;
	has_more?: boolean;
}

export interface PaginatedResponseDto<T> {
	data: T[];
	pagination?: PaginationDto;
	filters?: FilterDto[];
	sort?: SorterDto[];
}

export interface ErrorDetail {
	field: string;
	message: string;
}

export interface ReadBulkDto<T> {
	valide: T[];
	error: ErrorDetail[];
}

// [next offset, has more] returned by the fetch functions of the stores
export type FetchPage = [number, boolean];

// fetch function of a store given to a component to load a page of data (Tableau, Comment, Filter, FormContainer)
export type FetchFunction = (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => FetchPage | Promise<FetchPage>;

// called by a component with the interval [minOffset, maxOffset] of the data it has just loaded, to load the data linked to it
export type ListFetchFunction = (minOffset: number, maxOffset: number) => unknown;

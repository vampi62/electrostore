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
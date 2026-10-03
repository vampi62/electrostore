export class ApiError extends Error {
	status: number;
	data: any;

	constructor(message: string, status = 0, data: any = null) {
		super(message);
		this.name = "ApiError";
		this.status = status;
		this.data = data;
	}
}

// ASP.NET validation errors: { errors: { Field: ["msg", ...] } }
function formatValidationErrors(errors: unknown): string {
	if (!errors || typeof errors !== "object") {
		return "";
	}
	return Object.values(errors as Record<string, unknown>)
		.flatMap((v) => (Array.isArray(v) ? v : [v]))
		.filter((v) => typeof v === "string" && v)
		.join("\n");
}

// Extract a displayable message from an API response body ({ error }, { details }, ProblemDetails...)
export function extractApiMessage(data: any, fallback = ""): string {
	if (!data) {
		return fallback;
	}
	if (typeof data === "string") {
		return data;
	}
	for (const candidate of [data.error, data.details, data.detail, data.message]) {
		if (typeof candidate === "string" && candidate) {
			return candidate;
		}
		if (candidate && typeof candidate === "object") {
			const nested = extractApiMessage(candidate);
			if (nested) {
				return nested;
			}
		}
	}
	return formatValidationErrors(data.errors) || data.title || fallback;
}

// Convert anything thrown (ApiError, Error, string, API body...) into a displayable string
export function getMessage(e: unknown, fallback = "Unknown error"): string {
	if (typeof e === "string") {
		return e || fallback;
	}
	if (e instanceof ApiError || e instanceof Error) {
		return e.message || fallback;
	}
	return extractApiMessage(e, fallback);
}

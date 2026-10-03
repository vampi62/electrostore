// Check if the given ID is a new ID reserved in the store (e.g., "new-1", "new-2", etc.)
export function isNewId(id: string): boolean {
	return typeof id === "string" && id.startsWith("new");
}

/**
 * Check if the given ID is a new ID reserved in the store (e.g., "new-1", "new-2", etc.)
 * @param {string} id - The ID to check.
 * @returns {boolean} True if the ID is a new ID, false otherwise.
 */
export function isNewId(id) {
	return typeof id === "string" && id.startsWith("new");
}

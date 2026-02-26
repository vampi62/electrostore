/**
 * Get extension's mimetype
 * @param {string} date - file extension
 * @returns {string} - mimetype
 */
export function toLowerCaseWithoutAccents(str) {
	return str
		.normalize("NFD")
		.replace(/[\u0300-\u036f]/g, "")
		.toLowerCase();
}
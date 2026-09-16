/**
 * Get extension's mimetype
 * @param {string} date - file extension
 * @returns {string} - mimetype
 */
export function toLowerCaseWithoutAccents(str) {
	return str
		.normalize("NFD")
		.replaceAll(/[\u0300-\u036f]/g, "")
		.toLowerCase();
}
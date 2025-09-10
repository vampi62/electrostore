/**
 * Get extension's mimetype
 * @param {string}   - file extension
 * @returns {string} - mimetype
 */
export function getMimeType(type) {
	const mimeTypes = {
		"pdf": "application/pdf",
		"doc": "application/msword",
		"docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
		"xls": "application/vnd.ms-excel",
		"xlsx": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
		"ppt": "application/vnd.ms-powerpoint",
		"pptx": "application/vnd.openxmlformats-officedocument.presentationml.presentation",
		"txt": "text/plain",
		"png": "image/png",
		"jpg": "image/jpeg",
		"jpeg": "image/jpeg",
		"gif": "image/gif",
		"bmp": "image/bmp",
	};
	return mimeTypes[type] || "application/octet-stream";
}
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
/**
 * Get extension from mimetype
 * @param {string}   - mimetype
 * @returns {string} - extension
 */
export function getExtension(mimeType) {
	const mimeTypes = {
		"application/pdf": "pdf",
		"application/msword": "doc",
		"application/vnd.openxmlformats-officedocument.wordprocessingml.document": "docx",
		"application/vnd.ms-excel": "xls",
		"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": "xlsx",
		"application/vnd.ms-powerpoint": "ppt",
		"application/vnd.openxmlformats-officedocument.presentationml.presentation": "pptx",
		"text/plain": "txt",
		"image/png": "png",
		"image/jpeg": "jpg",
		"image/gif": "gif",
		"image/bmp": "bmp",
	};
	return mimeTypes[mimeType] || "bin";
}

import { getExtension } from "@/utils/mimeTypes.js";

export function downloadFile(fileData, fileInfo) {
	let url;
	if (fileData instanceof Blob) {
		url = URL.createObjectURL(new Blob([fileData], { type: fileInfo["keyType"] }));
	} else {
		url = fileData;
	}
	const link = document.createElement("a");
	link.href = url;
	link.setAttribute("download", fileInfo["keyName"] + "." + getExtension(fileInfo["keyType"]));
	document.body.appendChild(link);
	link.click();
	link.remove();
}

export function viewFile(fileData, fileInfo) {
	let url;
	if (fileData instanceof Blob) {
		url = URL.createObjectURL(new Blob([fileData], { type: fileInfo["keyType"] }));
	} else {
		url = fileData;
	}
	if (["pdf", "png", "jpg", "jpeg", "gif", "bmp"].includes(getExtension(fileInfo["keyType"]))) {
		window.open(url, "_blank");
	} else if (["doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt"].includes(getExtension(fileInfo["keyType"]))) {
		const a = document.createElement("a");
		a.href = url;
		a.setAttribute("download", fileInfo["keyName"] + "." + getExtension(fileInfo["keyType"]));
		document.body.appendChild(a);
		a.click();
		a.remove();
	} else {
		return false;
	}
	return true;
}
// Lowercase a string and strip accents, for accent-insensitive comparisons/search
export function toLowerCaseWithoutAccents(str: string): string {
	return str
		.normalize("NFD")
		.replaceAll(/[̀-ͯ]/g, "")
		.toLowerCase();
}

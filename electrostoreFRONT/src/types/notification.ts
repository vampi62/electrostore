// API error body: the message is in "error" or "message", as a string or a list of strings
export interface NotificationErrorBody {
	error?: string | string[];
	message?: string | string[];
}

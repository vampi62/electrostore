/// <reference lib="webworker" />
import { precacheAndRoute, cleanupOutdatedCaches } from "workbox-precaching";

declare const self: ServiceWorkerGlobalScope & { __WB_MANIFEST: any };

// Injecté automatiquement par vite-plugin-pwa
precacheAndRoute(self.__WB_MANIFEST);
cleanupOutdatedCaches();

// Gestion des notifications push
self.addEventListener("push", (event: PushEvent) => {
	if (!event.data) {
		return;
	}

	let data: any;
	try {
		data = event.data.json();
	} catch {
		data = { title: "ElectroStore", body: event.data.text() };
	}

	const title = data.title || "ElectroStore";
	const options = {
		body: data.body || "",
		icon: "/pwa/android-192x192.png",
		badge: "/pwa/android-192x192.png",
		data: data.data || {},
		requireInteraction: false,
		tag: "electrostore-notification",
		vibrate: [100, 50, 100],
	} as NotificationOptions;

	event.waitUntil(self.registration.showNotification(title, options));
});

// Clic sur une notification push → ouvrir/focus l'app
self.addEventListener("notificationclick", (event: NotificationEvent) => {
	event.notification.close();

	const url = event.notification.data?.url || "/";

	event.waitUntil(
		self.clients.matchAll({ type: "window", includeUncontrolled: true }).then((clientList) => {
			for (const client of clientList) {
				if (client.url === url && "focus" in client) {
					return (client as WindowClient).focus();
				}
			}
			if (self.clients.openWindow) {
				return self.clients.openWindow(url);
			}
		}),
	);
});

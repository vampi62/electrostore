import { precacheAndRoute, cleanupOutdatedCaches } from "workbox-precaching";

// Injecté automatiquement par vite-plugin-pwa
precacheAndRoute(self.__WB_MANIFEST);
cleanupOutdatedCaches();

// Gestion des notifications push
self.addEventListener("push", (event) => {
	if (!event.data) {
		return;
	}

	let data;
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
	};

	event.waitUntil(self.registration.showNotification(title, options));
});

// Clic sur une notification push → ouvrir/focus l'app
self.addEventListener("notificationclick", (event) => {
	event.notification.close();

	const url = event.notification.data?.url || "/";

	event.waitUntil(
		self.clients.matchAll({ type: "window", includeUncontrolled: true }).then((clientList) => {
			for (const client of clientList) {
				if (client.url === url && "focus" in client) {
					return client.focus();
				}
			}
			if (self.clients.openWindow) {
				return self.clients.openWindow(url);
			}
		}),
	);
});

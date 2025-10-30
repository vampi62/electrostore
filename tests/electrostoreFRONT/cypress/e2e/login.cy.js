/* eslint-disable no-undef */
describe("Login Page", () => {
	beforeEach(() => {
		// Force the browser language to English
		cy.on('window:before:load', (win) => {
			Object.defineProperty(win.navigator, 'language', {
				value: 'en-US'
			});
			Object.defineProperty(win.navigator, 'languages', {
				value: ['en-US', 'en']
			});
			Object.defineProperty(win.navigator, 'userLanguage', {
				value: 'en-US'
			});
		});
		
		// Set localStorage to force English language for the application
		cy.window().then((win) => {
			win.localStorage.setItem('i18nextLng', 'en');
		});

		// intercept the API calls GET /api/config
		cy.intercept("GET", "**/api/config", {
			statusCode: 200,
			body: {
				"smtp_enabled": false,
				"mqtt_connected": false,
				"ia_service_status": false,
				"max_length_url": 150,
				"max_length_commentaire": 455,
				"max_length_description": 500,
				"max_length_name": 50,
				"max_length_type": 50,
				"max_length_email": 100,
				"max_length_ip": 50,
				"max_length_reason": 50,
				"max_length_status": 50,
				"max_size_document_in_mb": 5,
			},
		}).as("getConfig");
		
		// Visit the login page before each test
		cy.visit("/login");
	});

	it("displays the login form", () => {
		// Check that the login form elements are visible
		cy.get("form").should("be.visible");
		cy.get("input[type=\"email\"]").should("be.visible");
		cy.get("input[type=\"password\"]").should("be.visible");
		cy.get("button[type=\"submit\"]").should("be.visible");
	});

	it("shows validation errors for empty fields", () => {
		// Submit the form without entering any data
		cy.get("button[type=\"submit\"]").click();

		// Check that validation errors are displayed
		cy.get("form").contains("Email is required").should("be.visible");
		cy.get("form").contains("Password is required").should("be.visible");
	});

	it("shows error message for invalid credentials", () => {
		// Mock the API response for invalid credentials
		cy.intercept("POST", "**/api/auth/login", {
			statusCode: 401,
			body: {
				message: "Invalid email or password",
			},
		}).as("loginRequest");

		// Fill in the form with invalid credentials
		cy.get("input[type=\"email\"]").type("invalid@example.com");
		cy.get("input[type=\"password\"]").type("wrongpassword");
		cy.get("button[type=\"submit\"]").click();

		// Wait for the API request to complete
		cy.wait("@loginRequest");

		// Check that the error message is displayed
		cy.get("form").contains("Incorrect credentials").should("be.visible");
	});

	it("redirects to dashboard after successful login", () => {
		// Mock the API response for successful login
		cy.intercept("POST", "**/api/auth/login", {
			statusCode: 200,
			body: {
				token: "fake-jwt-token",
				expire_date_token: new Date(Date.now() + 3600000).toISOString(), // 1 hour from now
				refresh_token: "fake-refresh-token",
				expire_date_refresh_token: new Date(Date.now() + 86400000).toISOString(), // 1 day from now
				user: {
					id_user: 1,
					email_user: "admin@example.com",
					nom_user: "Admin",
					prenom_user: "User",
					role_user: "admin",
				},
			},
		}).as("loginRequest");
		cy.intercept("GET", "**/api/item", {
			statusCode: 200,
			body: [],
			headers: {
				"Content-Type": "application/json",
				"X-Total-Count": "0",
			},
		}).as("getItems");

		// Fill in the form with valid credentials
		cy.get("input[type=\"email\"]").type("admin@example.com");
		cy.get("input[type=\"password\"]").type("password123");
		cy.get("button[type=\"submit\"]").click();

		// Wait for the API request to complete
		cy.wait("@loginRequest");

		// Check that we are redirected to the dashboard
		cy.url().should("include", "/inventory");
	});

	it("allows navigation to forgot password page", () => {
		// Check if there's a "Forgot password" link and click it
		cy.contains("Forgot password ?").then((link) => {
			if (link && link.length > 0) {
				cy.wrap(link).click();
				cy.url().should("include", "/forgot-password");
			}
		});
	});

	// Performance benchmark test
	it("loads the login page quickly (performance benchmark)", () => {
		// Measure page load time
		cy.window().then((win) => {
			// Utiliser l'API Performance moderne
			const navigationEntries = win.performance.getEntriesByType('navigation');
			let pageLoadTime;
			
			if (navigationEntries.length > 0) {
				// Utiliser PerformanceNavigationTiming API (moderne)
				pageLoadTime = navigationEntries[0].loadEventEnd - navigationEntries[0].startTime;
			} else {
				// Fallback pour les navigateurs qui ne supportent pas PerformanceNavigationTiming
				cy.log('Navigation Timing API non supportée, utilisation d\'une alternative');
				pageLoadTime = win.performance.now(); // Temps écoulé depuis la création de la page
			}
			
			// Log the performance data
			cy.log(`[BENCHMARK] Page load time: ${pageLoadTime}ms`);
			
			// Assert that the page loads in a reasonable time
			expect(pageLoadTime).to.be.lessThan(3000); // 3 seconds is a reasonable upper limit
		});
	});
});
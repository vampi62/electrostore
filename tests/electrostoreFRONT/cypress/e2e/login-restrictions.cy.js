/* eslint-disable no-undef */
const ssoProviders = [{ provider: "authentik", display_name: "Authentik", icon_url: "https://example.com/icon.png" }];

function visitLoginWithConfig(config, path = "/login") {
	cy.intercept("GET", "**/api/config", {
		statusCode: 200,
		body: { demo_mode: false, sso_available_providers: ssoProviders, ...config },
	}).as("getConfig");
	cy.intercept("GET", "**/api/status", { statusCode: 200, body: { api_status: "healthy", notif_smtp: true } }).as("getStatus");
	cy.visit(path);
	cy.wait("@getConfig");
}

describe("Login restrictions from the api config", () => {
	describe("password login disabled", () => {
		it("only shows the SSO login", () => {
			visitLoginWithConfig({ password_login_enabled: false });

			cy.get("input[type=\"password\"]").should("not.exist");
			cy.get("button[type=\"submit\"]").should("not.exist");
			cy.contains("Forgot password ?").should("not.exist");
			cy.contains("Authentik").should("be.visible");
		});

		it("redirects the forgot and reset password pages to the login", () => {
			visitLoginWithConfig({ password_login_enabled: false }, "/forgot-password");
			cy.url().should("include", "/login");

			visitLoginWithConfig({ password_login_enabled: false }, "/reset-password");
			cy.url().should("include", "/login");
		});
	});

	describe("sign up disabled", () => {
		it("hides the register link and redirects the register page", () => {
			visitLoginWithConfig({ sign_up_enabled: false });

			cy.get("a[href=\"/register\"]").should("not.exist");
			cy.get("input[type=\"password\"]").should("be.visible");

			visitLoginWithConfig({ sign_up_enabled: false }, "/register");
			cy.url().should("include", "/login");
		});
	});
});

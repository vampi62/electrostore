# REST API Documentation

## Authentication

The API uses JWT (JSON Web Tokens) for authentication. Two types of tokens are used:

- **Access Token**: Valid for 24 hours.
- **Refresh Token**: Valid for 7 days.

Tokens must be included in the `Authorization` header of requests in the format `Bearer <token>`.

## Routes

### Routes Without Token

- **Registration**
  - `POST /api/user`
  - Allows a new user to register.

- **Login**
  - `POST /api/auth/login`
  - Allows a user to log in and receive an `access token` and a `refresh token`.

- **Get SSO Authentication URL** ('if A SSO method is configured')
  - `GET /api/auth/{sso_method}/url`
  - Retrieves the authentication URL for the specified SSO method (e.g., Authentik).

- **SSO Callback** ('if A SSO method is configured')
  - `POST /api/auth/{sso_method}/callback`
  - Handles the SSO callback and returns a JWT.

- **Forgot Password**
  - `POST /api/auth/forgot-password`
  - Sends an email with a reset code valid for 1 hour.
  - Requires an SMTP server to be configured; otherwise, it returns an error.

- **Reset Password**
  - `POST /api/auth/reset-password`
  - Allows resetting the password using the code received by email.

### Route Using the Refresh Token

- **Refresh Token**
  - `POST /api/auth/refresh-token`
  - Uses the `refresh token` to obtain a new pair of tokens.
  - The `refresh token` must be placed in the `Authorization` header.

### Routes Requiring an Access Token

All other routes require a valid `access token`. Some routes may also require a minimum user role.

## Swagger

Swagger documentation is available to explore and test the API endpoints. You can access it [here](https://vampi62.github.io/electrostore/openapi/).

# Installation Guide

## Overview

ElectroStore provides an automated configuration generator that creates all necessary files for Docker deployment. This is the recommended installation method.

**Configuration Generator**: [https://vampi62.github.io/electrostore/docs/generator/](https://vampi62.github.io/electrostore/docs/generator/index.html)

## Quick Start

1. Open the [configuration generator](https://vampi62.github.io/electrostore/docs/generator/index.html)
2. Fill in the configuration options (see details below)
3. Click "Generate files"
4. Download the generated ZIP archive containing all configuration files
5. Extract the archive to your server
6. Run the setup script: `./setup.sh` (Linux/macOS) or `.\setup.ps1` (Windows)
7. Start the services: `docker-compose up -d`

## Configuration Generator Options

### URLs and CORS

**API URL**  
The public URL where your API will be accessible.
- Example: `http://localhost:5000` or `https://api.electrostore.com`
- Used for internal service communication and CORS configuration

**Frontend URL**  
The public URL where your web interface will be accessible.
- Example: `http://localhost:8080` or `https://electrostore.com`
- Automatically added to CORS allowed origins

**Other Allowed Origins (CORS)**  
Additional domains that should be allowed to access the API (one per line).
- Example: `https://mobile.electrostore.com`
- Useful for mobile apps or additional frontends

### Docker Image Version

Select the ElectroStore version to deploy:

- **latest**: Development version using images from the `main` branch (features under development)
- **local**: Build from source code on your machine (requires local repository clone)

### Reverse Proxy (Traefik)

**Use Traefik**  
Enable this option if you're using Traefik as a reverse proxy with Docker labels.

When enabled, configure:
- **Traefik Docker Network**: Docker network used by Traefik (default: `traefik`)
- **Entrypoint**: Traefik entrypoint (e.g., `web` for HTTP or `websecure` for HTTPS)
- **Middlewares**: Comma-separated list of middlewares (e.g., `redirect-https@file,compress@file`)
- **Enable TLS/HTTPS**: Automatically configure HTTPS with cert resolver
- **Cert Resolver**: Certificate resolver name configured in Traefik (e.g., `letsencrypt`)

When disabled, configure:
- **API Port**: Port to expose the API (default: `5000`)
- **Frontend Port**: Port to expose the web interface (default: `8080`)

### Database (MariaDB)

**Include MariaDB in Docker**  
Enable to deploy a containerized MariaDB instance. The database will be automatically configured with secure default values.

When disabled, configure external database:
- **Host**: Database server address
- **Port**: Database port (default: `3306`)
- **Database Name**: Name of the database
- **User**: Database username
- **Password**: Database password

### MQTT Broker (Mosquitto)

**Include Mosquitto in Docker**  
Enable to deploy a containerized MQTT broker for ESP module communication. The broker will be automatically configured with secure default values.

When disabled, configure external MQTT broker:
- **Host**: MQTT broker address
- **Port**: MQTT broker port (default: `1883`)
- **User**: MQTT username
- **Password**: MQTT password

### File Storage (S3)

**Enable S3 storage**  
Enable to store images and documents in S3-compatible storage. When disabled, files are stored in a local Docker volume.

**Use integrated Garage S3**  
Enable to deploy a containerized Garage S3 instance. Two separate buckets will be automatically created: one for the API service and one for the IA service, each with its own access keys.

Configure bucket names:
- **API Bucket Name**: Bucket for API files (default: `electrostore-api`)
- **IA Bucket Name**: Bucket for AI training data (default: `electrostore-ia`)

When using external S3 service (AWS, MinIO, etc.), configure separately for API and IA services:
- **Endpoint**: S3 endpoint URL
- **Access Key**: S3 access key ID
- **Secret Key**: S3 secret access key
- **Bucket**: Bucket name
- **Region**: S3 region (e.g., `us-east-1`)
- **Use HTTPS**: Enable secure connection

### Notifications

#### Email Service (SMTP)

**Enable email sending**  
Enable to send email notifications and password reset emails.

Configure:
- **SMTP Host**: SMTP server address (e.g., `smtp.gmail.com`)
- **SMTP Port**: SMTP server port (default: `587`)
- **SMTP User**: SMTP username
- **SMTP Password**: SMTP password
- **"From" Address**: Sender email address (e.g., `noreply@electrostore.com`)

#### Web Push Notifications (VAPID)

**Enable Web Push Notifications**  
Enable browser push notifications for the Progressive Web App (PWA).

Generate VAPID keys with: `npx web-push generate-vapid-keys`

Configure:
- **Subject**: Contact address (mailto: or https:) identifying the application owner
- **VAPID Public Key**: Public key from `web-push` output (also set as `VITE_VAPID_PUBLIC_KEY` in frontend)
- **VAPID Private Key**: Private key (keep secret, server-side only)

### Parcel Tracking

**Enable 17track parcel tracking**  
Enable automatic delivery status updates via the 17track API.

Configure:
- **17track API Key**: API key from [17track developer portal](https://www.17track.net/en/api)

### Secrets Management (HashiCorp Vault)

**Use HashiCorp Vault**  
Enable to retrieve secrets from Vault instead of storing them in plain text.

Configure:
- **Vault URL**: Vault server address (default: `http://vault:8200`)
- **Vault Token**: Token with appropriate permissions
- **Secrets Path**: Path in Vault where secrets are stored (default: `electrostore`)
- **Mount Point**: Mount point of the KV secrets engine (default: `secret`)
- **Vault Container Name**: Docker container name (default: `vault`)

### JWT Configuration

Configure JSON Web Token authentication:
- **JWT Secret Key**: Random secret key (click "Generate" for a secure key)
- **Issuer**: Token issuer identifier (default: `ElectroStoreAPI`)
- **Audience**: Token audience identifier (default: `ElectroStoreClient`)
- **Validity Duration**: Token validity in days (default: `1`)

### OAuth Configuration

**Add OAuth provider**  
Configure Single Sign-On (SSO) authentication providers (e.g., Google, GitHub, Authentik).

For each provider, configure:
- **Provider Name**: Internal identifier
- **Client ID**: OAuth client ID from provider
- **Client Secret**: OAuth client secret
- **Authority**: OAuth authorization URL
- **Redirect URI**: Callback URL (format: `{frontend-url}/auth/callback`)
- **Scope**: Requested OAuth scopes (e.g., `openid profile email`)
- **Display Name**: Name shown to users
- **Icon URL**: Provider logo URL
- **Group Mapping**: Map OAuth groups to ElectroStore roles (User, Moderator, Admin)

## Generated Files

The generator creates:

- **docker-compose.yml**: Complete Docker Compose configuration
- **config/api/appsettings.json**: API service configuration
- **config/ia/appsettings.json**: AI service configuration
- **config/notif/appsettings.json**: Notification service configuration
- **config/cron/appsettings.json**: CRON service configuration
- **config/worker/appsettings.json**: WORKER service configuration
- **.env**: Environment variables for Docker Compose
- **setup.sh** / **setup.ps1**: Automated setup script for Linux/macOS or Windows
- **config/garage/garage.toml**: Garage S3 configuration (if enabled)
- **config/mosquitto/mosquitto.conf**: Mosquitto MQTT configuration (if enabled)
- **config/mosquitto/mosquitto.passwd**: Mosquitto password file (if enabled)

## Deployment

### Linux / macOS

```bash
# Extract the generated archive
unzip electrostore-config.zip
cd electrostore-config

# Make the setup script executable
chmod +x setup.sh

# Run the setup script (configures Garage S3 and MQTT if enabled)
./setup.sh
```

### Windows (PowerShell)

```powershell
# Extract the generated archive
Expand-Archive electrostore-config.zip
cd electrostore-config

# Run the setup script (configures Garage S3 and MQTT if enabled)
.\setup.ps1

# Start all services
docker-compose up -d
```

## First Login

After deployment, access the web interface at your configured frontend URL (e.g., `http://localhost:8080`).

**Default credentials:**
- **Username**: `admin@localhost.local`
- **Password**: `Admin@1234`

**Important**: Change the administrator password immediately after first login.

## Troubleshooting

### Check service status

```bash
docker-compose ps
```

### View service logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f electrostoreAPI
```

### Restart services

```bash
docker-compose restart
```

### Rebuild services (for local version)

```bash
docker-compose up -d --build
```
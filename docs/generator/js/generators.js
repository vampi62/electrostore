// Generate docker-compose.yml file
function generateDockerCompose(config) {
    let compose = `# docker-compose.yml for ElectroStore
# This file uses environment variables defined in the .env file
# To use this file, create a .env file at the project root
# Default values are provided with the syntax \${VAR:-default_value}

version: '3.8'

services:`;

    // API Backend
    compose += `
  api:
    image: ghcr.io/vampi62/electrostore/api:\${API_VERSION:-latest}
    container_name: electrostore-api
    cap_add:
      - CHOWN
    cap_drop:
      - ALL
    read_only: true
    security_opt:
      - no-new-privileges:true
    tmpfs:
      - /tmp
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 1G`;
    
    if (config.useTraefik) {
        const apiRule = generateTraefikRule(config.apiUrlObj);
        const entrypoint = config.traefik.tlsEnable ? config.traefik.tlsEntrypoint : config.traefik.entrypoint;
        
        compose += `
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.api.rule=${apiRule}"
      - "traefik.http.routers.api.entrypoints=${entrypoint}"
      - "traefik.http.services.api.loadbalancer.server.port=80"`;
      
        if (config.traefik.middlewares) {
            compose += `
      - "traefik.http.routers.api.middlewares=${config.traefik.middlewares}"`;
        }
        
        if (config.traefik.tlsEnable) {
            compose += `
      - "traefik.http.routers.api.tls.certresolver=${config.traefik.certResolver}"`;
        }
    } else {
        compose += `
    ports:
      - "\${API_PORT:-${config.apiPort}}:80"`;
    }
    
    compose += `
    environment:
      - ASPNETCORE_ENVIRONMENT=\${ENVIRONMENT:-Production}`;

    compose += `
    depends_on:`;
    
    if (config.useMariaDB) compose += `\n      - mariadb`;
    if (config.useMQTT) compose += `\n      - mqtt`;
    if (config.enableS3 && config.useS3) compose += `\n      - garage`;
    
    compose += `
    volumes:
      - ./config/appsettings.json:/app/config/appsettings.json:ro`;
    
    if (!config.enableS3) {
        compose += `\n      - api-wwwroot:/app/wwwroot`;
    }
    
    compose += `
    networks:
      - electrostore`;
    
    if (config.useTraefik) {
        compose += `\n      - ${config.traefik.network}`;
    }
    
    compose += `
    restart: unless-stopped
`;

    // Frontend
    compose += `
  frontend:
    image: ghcr.io/vampi62/electrostore/front:\${FRONTEND_VERSION:-latest}
    container_name: electrostore-frontend
    cap_drop:
      - ALL
    security_opt:
      - no-new-privileges:true
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 1G
    environment:
      - VUE_API_URL=${config.apiUrlObj.toString()}`;
    
    if (config.useTraefik) {
        const frontendRule = generateTraefikRule(config.frontUrlObj);
        const entrypoint = config.traefik.tlsEnable ? config.traefik.tlsEntrypoint : config.traefik.entrypoint;
        
        compose += `
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.frontend.rule=${frontendRule}"
      - "traefik.http.routers.frontend.entrypoints=${entrypoint}"
      - "traefik.http.services.frontend.loadbalancer.server.port=80"`;
      
        if (config.traefik.middlewares) {
            compose += `
      - "traefik.http.routers.frontend.middlewares=${config.traefik.middlewares}"`;
        }
        
        if (config.traefik.tlsEnable) {
            compose += `
      - "traefik.http.routers.frontend.tls=true"
      - "traefik.http.routers.frontend.tls.certresolver=${config.traefik.certResolver}"`;
        }
    } else {
        compose += `
    ports:
      - "\${FRONTEND_PORT:-${config.frontendPort}}:80"`;
    }
    
    compose += `
    depends_on:
      - api
    networks:
      - electrostore`;
    
    if (config.useTraefik) {
        compose += `\n      - ${config.traefik.network}`;
    }
    
    compose += `
    restart: unless-stopped
`;

    // Service IA
    compose += `
  ia:
    image: ghcr.io/vampi62/electrostore/ia:\${IA_VERSION:-latest}
    container_name: electrostore-ia
    cap_add:
      - CHOWN
    cap_drop:
      - ALL
    security_opt:
      - no-new-privileges:true
    read_only: true
    tmpfs:
      - /tmp
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G`;
    
    compose += `
    depends_on:`;
    
    if (config.useMariaDB) compose += `\n      - mariadb`;
    if (config.enableS3 && config.useS3) compose += `\n      - garage`;
    
    if (!config.enableS3) {
        compose += `
    volumes:
      - api-wwwroot:/data
      - ./config/appsettings.json:/app/config/appsettings.json:ro`;
    }
    compose += `
    networks:
      - electrostore
    restart: unless-stopped
`;

    // MariaDB
    if (config.useMariaDB) {
        compose += `
  mariadb:
    image: mariadb:\${MARIADB_VERSION:-11.7.2}
    container_name: electrostore-mariadb
    environment:
      - MYSQL_ROOT_PASSWORD=\${MARIADB_ROOT_PASSWORD:-changeme_root_password}
      - MYSQL_DATABASE=\${MARIADB_DATABASE:-electrostore_db}
      - MYSQL_USER=\${MARIADB_USER:-electrostore_user}
      - MYSQL_PASSWORD=\${MARIADB_PASSWORD:-changeme_password}
    volumes:
      - mariadb-data:/var/lib/mysql
    networks:
      - electrostore
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "healthcheck.sh", "--connect", "--innodb_initialized"]
      interval: 10s
      timeout: 5s
      retries: 5
`;
    }

    // MQTT
    if (config.useMQTT) {
        compose += `
  mqtt:
    image: eclipse-mosquitto:\${MQTT_VERSION:-2.0.20}
    container_name: electrostore-mqtt
    ports:
      - "1883:1883"
    volumes:
      - ./config/mosquitto/mosquitto.conf:/mosquitto/config/mosquitto.conf:ro
      - ./config/mosquitto/mosquitto.passwd:/mosquitto/config/mosquitto.passwd:ro
      - mqtt-data:/mosquitto/data
    networks:
      - electrostore
    restart: unless-stopped
`;
    }

    // S3 Garage
    if (config.enableS3 && config.useS3) {
        compose += `
  garage:
    image: dxflrs/garage:\${GARAGE_VERSION:-v2.0.0}
    container_name: electrostore-garage
    ports:
      - "3900:3900"
      - "3901:3901"
      - "3902:3902"
    volumes:
      - ./config/garage/garage.toml:/etc/garage.toml:ro
      - garage-data:/data
      - garage-meta:/meta
    networks:
      - electrostore
    restart: unless-stopped
`;
    }

    // Networks et Volumes
    compose += `
networks:
  electrostore:
    driver: bridge`;
    
    if (config.useTraefik) {
        compose += `
  ${config.traefik.network}:
    external: true`;
    }

if (!config.enableS3 || config.useMQTT || config.useMariaDB || 
    (config.enableS3 && config.useS3) || (config.useVault && config.vault.integrated)) {
    compose += `
volumes:`;
}

    if (!config.enableS3) compose += `\n  api-wwwroot:`;
    if (config.useMariaDB) compose += `\n  mariadb-data:`;
    if (config.useMQTT) compose += `\n  mqtt-data:`;
    if (config.enableS3 && config.useS3) compose += `\n  garage-data:\n  garage-meta:`;
    if (config.useVault && config.vault.integrated) compose += `\n  vault-data:`;

    return compose;
}

// Generate appsettings.json
function generateAppsettings(config) {
    const settings = {
        "Logging": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft.AspNetCore": "Warning"
            }
        },
        "AllowedHosts": "*"
    };

    let connectionString;
    if (config.useMariaDB) {
        connectionString = `Server=mariadb;Port=3306;Database=${config.mariadb.database};User=${config.mariadb.user};Password=
        ${config.useVault ? '{{vault:mariadb_password}}' : config.mariadb.password};`;
    } else {
        const db = config.mariadbExternal;
        connectionString = `Server=${db.host};Port=${db.port};Database=${db.database};User=${db.user};Password=
        ${config.useVault ? '{{vault:mariadb_password}}' : db.password};`;
    }
    
    settings.ConnectionStrings = {
        "DefaultConnection": connectionString
    };

    if (config.useMQTT) {
        settings.MQTT = {
            "Server": "mqtt",
            "Port": 1883,
            "Username": config.mqtt.user,
            "Password": config.useVault ? "{{vault:mqtt_password}}" : config.mqtt.password
        };
    } else {
        const mqtt = config.mqttExternal;
        settings.MQTT = {
            "Server": mqtt.host,
            "Port": parseInt(mqtt.port),
            "Username": mqtt.user,
            "Password": config.useVault ? "{{vault:mqtt_password}}" : config.mqtt.password
        };
    }

    if (config.enableS3) {
        if (config.useS3) {
            settings.S3 = {
                "Enable": true,
                "Endpoint": "http://garage:3900",
                "AccessKey": config.useVault ? "{{vault:s3_access_key}}" : config.s3.accessKey,
                "SecretKey": config.useVault ? "{{vault:s3_secret_key}}" : config.s3.secretKey,
                "Bucket": config.s3.bucket,
                "Region": config.s3.region,
                "Secure": false
            };
        } else if (config.s3External) {
            settings.S3 = {
                "Enable": true,
                "Endpoint": config.s3External.endpoint,
                "AccessKey": config.useVault ? "{{vault:s3_access_key}}" : config.s3External.accessKey,
                "SecretKey": config.useVault ? "{{vault:s3_secret_key}}" : config.s3External.secretKey,
                "Bucket": config.s3External.bucket,
                "Region": config.s3External.region,
                "Secure": config.s3External.secure
            };
        }
    } else {
        settings.S3 = {
            "Enable": false
        };
    }

    if (config.enableSMTP && config.smtp) {
        settings.SMTP = {
            "Enable": true,
            "Host": config.smtp.host,
            "Port": parseInt(config.smtp.port),
            "Username": config.smtp.user,
            "Password": config.useVault ? "{{vault:smtp_password}}" : config.smtp.password,
            "From": config.smtp.from
        };
    } else {
        settings.SMTP = {
            "Enable": false
        };
    }

    settings.Jwt = {
        "Key": config.useVault ? "{{vault:jwt_key}}" : config.jwt.key,
        "Issuer": config.jwt.issuer,
        "Audience": config.jwt.audience,
        "ExpireDays": parseInt(config.jwt.expireDays)
    };

    if (config.oauthProviders.length > 0) {
        settings.OAuth = {};
        config.oauthProviders.forEach(provider => {
            const name = toSnakeCase(provider.displayName);
            settings.OAuth[toCamelCase(provider.displayName)] = {
                "ClientId": config.useVault ? `{{vault:oauth_${name}_client_id}}` : provider.clientId,
                "ClientSecret": config.useVault ? `{{vault:oauth_${name}_client_secret}}` : provider.clientSecret,
                "Authority": provider.authority,
                "RedirectUri": provider.redirectUri,
                "Scope": provider.scope,
                "GroupMapping": {
                    "User": provider.groupMapping.user,
                    "Moderator": provider.groupMapping.moderator,
                    "Admin": provider.groupMapping.admin
                },
                "DisplayName": provider.displayName,
                "IconUrl": provider.iconUrl
            };
        });
    }

    settings.FrontendUrl = config.frontUrl;
    settings.AllowedOrigins = config.allowedOrigins;

    return JSON.stringify(settings, null, 2);
}

// Generate .env file
function generateEnvFile(config) {
    let env = `# .env file for ElectroStore\n`;
    env += `# Generated on ${new Date().toLocaleDateString('en-US')}\n`;
    env += `# This file contains environment variables used by docker-compose.yml\n\n`;
    
    env += `# General configuration\n`;
    env += `PROJECT_NAME=electrostore\n`;
    env += `ENVIRONMENT=${config.appVersion == 'latest' ? 'Development' : 'Production'}\n\n`;
    
    env += `# Docker image versions\n`;
    env += `API_VERSION=${config.appVersion}\n`;
    env += `FRONTEND_VERSION=${config.appVersion}\n`;
    env += `IA_VERSION=${config.appVersion}\n`;
    if (config.useMariaDB) {
        env += `MARIADB_VERSION=11.7.2\n`;
    }
    if (config.useMQTT) {
        env += `MQTT_VERSION=2.0.20\n`;
    }
    if (config.enableS3 && config.useS3) {
        env += `GARAGE_VERSION=v2.0.0\n`;
    }
    env += `\n`;
    
    if (!config.useTraefik) {
        env += `# Ports\n`;
        env += `API_PORT=${config.apiPort}\n`;
        env += `FRONTEND_PORT=${config.frontendPort}\n\n`;
    }
    
    if (config.useMariaDB) {
        env += `# MariaDB (integrated service)\n`;
        env += `MARIADB_DATABASE=${config.mariadb.database}\n`;
        env += `MARIADB_USER=${config.mariadb.user}\n`;
        env += `MARIADB_PASSWORD=${config.mariadb.password}\n`;
        env += `MARIADB_ROOT_PASSWORD=${config.mariadb.rootPassword}\n\n`;
    }
    
    if (config.useMQTT) {
        env += `# MQTT (integrated service)\n`;
        env += `MQTT_USER=${config.mqtt.user}\n`;
        env += `MQTT_PASSWORD=${config.mqtt.password}\n\n`;
    }
    
    if (config.enableS3 && config.useS3) {
        env += `# S3 Garage (integrated service)\n`;
        env += `S3_ACCESS_KEY=${config.s3.accessKey}\n`;
        env += `S3_SECRET_KEY=${config.s3.secretKey}\n`;
        env += `S3_BUCKET=${config.s3.bucket}\n`;
        env += `S3_REGION=${config.s3.region}\n\n`;
    }
    
    if (config.useVault) {
        env += `# HashiCorp Vault\n`;
        env += `VAULT_TOKEN=${config.vault.token}\n`;
        if (config.vault.integrated) {
            env += `VAULT_VERSION=1.18\n`;
        }
        env += `\n`;
    }
    
    return env;
}

// Generate setup.sh script
function generateSetupScript(config) {
    let script = `#!/bin/bash
# ElectroStore Configuration Script
# Generated on ${new Date().toLocaleDateString('en-US')}

set -e

echo "====================================="
echo "ElectroStore Configuration"
echo "====================================="
echo ""

`;

    if (config.useVault && config.vault.integrated) {
        script += `# Vault Configuration
echo "Configuring HashiCorp Vault..."

echo "Starting Vault..."
docker compose up -d vault

echo "Waiting for Vault to start (5 seconds)..."
sleep 5

echo "Enabling KV v2 engine..."
docker exec electrostore-vault vault secrets enable -version=2 -path=secret kv || echo "KV engine already enabled"

echo "Storing secrets in Vault..."
`;

        script += `docker exec electrostore-vault vault kv put ${config.vault.path} mariadb_password="
        ${config.useMariaDB ? config.mariadb.password : config.mariadbExternal.password}"
`;

        script += `docker exec electrostore-vault vault kv patch ${config.vault.path} mqtt_password="
        ${config.useMQTT ? config.mqtt.password : config.mqttExternal.password}
`;

        if (config.enableSMTP && config.smtp) {
            script += `docker exec electrostore-vault vault kv patch ${config.vault.path} smtp_password="
            ${config.smtp.password}"
`;
        }

        script += `docker exec electrostore-vault vault kv patch ${config.vault.path} jwt_key="
        ${config.jwt.key}"

echo "Vault configuration completed"
echo ""

`;
    }

    if (config.enableS3 && config.useS3) {
        script += `# S3 Garage Configuration
echo "Configuring Garage S3..."

echo "Starting Garage..."
docker compose up -d garage

echo "Waiting for Garage to start (10 seconds)..."
sleep 10

echo "Configuring Garage cluster..."
docker exec electrostore-garage /garage status || true
docker exec electrostore-garage /garage layout assign -z dc1 -c 1 \$(docker exec electrostore-garage garage status | grep -oP '[0-9a-f]{16}' | head -n 1) || true
docker exec electrostore-garage /garage layout apply --version 1

echo "Creating S3 access keys..."

docker exec electrostore-garage /garage key import --name electrostore-key ${config.s3.accessKey} ${config.s3.secretKey}
GARAGE_ACCESS_KEY="${config.s3.accessKey}"
GARAGE_SECRET_KEY="${config.s3.secretKey}"

echo "Access Key: \$GARAGE_ACCESS_KEY"
echo "Secret Key: \$GARAGE_SECRET_KEY"

echo "Creating bucket ${config.s3.bucket}..."
docker exec electrostore-garage /garage bucket create ${config.s3.bucket}

echo "Assigning permissions..."
docker exec electrostore-garage /garage bucket allow --read --write ${config.s3.bucket} --key electrostore-key
`;

        if (config.useVault) {
            script += `
echo "Storing S3 keys in Vault..."
docker exec electrostore-vault vault kv patch ${config.vault.path} s3_access_key="\$GARAGE_ACCESS_KEY" s3_secret_key="\$GARAGE_SECRET_KEY"
`;
        }

        script += `
rm /tmp/garage_keys.txt

echo "Garage configuration completed"
echo ""

`;
    }

    if (config.useMQTT) {
        script += `# MQTT Configuration
echo "Configuring Mosquitto MQTT..."

echo "Hashing MQTT password with temporary container..."
docker run --rm -v "./config/mosquitto:/temp" eclipse-mosquitto:\${MQTT_VERSION:-2.0.20} sh -c "cp /temp/mosquitto.passwd /tmp/mosquitto.passwd && mosquitto_passwd -U /tmp/mosquitto.passwd && cat /tmp/mosquitto.passwd > /temp/mosquitto.passwd"

echo "Starting Mosquitto..."
docker compose up -d mqtt

echo "MQTT configuration completed"
echo ""

`;
    }

    script += `# Start all services
echo "Starting all services..."
docker compose up -d
`;

/*     if (config.useTraefik) {
        script += `
echo "Checking and creating Traefik network if needed..."
docker network inspect ${config.traefik.network} >/dev/null 2>&1 || docker network create ${config.traefik.network}

echo "Connecting containers to Traefik network..."
docker network connect ${config.traefik.network} electrostore-api 2>/dev/null || echo "API déjà connecté au réseau Traefik"
docker network connect ${config.traefik.network} electrostore-frontend 2>/dev/null || echo "Frontend déjà connecté au réseau Traefik"
`;
    } */

    script += `
echo ""
echo "====================================="
echo "Configuration completed!"
echo "====================================="
echo ""
echo "Application access:"
echo "${'Frontend: ' + config.frontUrlObj.toString()}"
echo "${'API: ' + config.apiUrlObj.toString()}"
echo ""
`;

    if (config.enableS3 && config.useS3) {
        script += `echo "S3 Garage:"
echo "  Endpoint: http://localhost:3900"
echo "  Bucket: ${config.s3.bucket}"
echo "  Access Key: (see .env)"
echo "  Secret Key: (see .env)"
echo ""
`;
    }

    if (config.useMQTT) {
        script += `echo "MQTT:"
echo "  Host: localhost:1883"
echo "  User: ${config.mqtt.user}"
echo "  Password: (see .env)"
echo ""
`;
    }

    return script;
}

// Generate Garage configuration file
function generateGarageConfig(config) {
    const rpcSecret = generateHexaKey(64);
    return `metadata_dir = "/meta"
data_dir = "/data"
db_engine = "sqlite"
replication_factor = 1

rpc_bind_addr = "[::]:3901"
rpc_public_addr = "127.0.0.1:3901"
rpc_secret = "${rpcSecret}"

[s3_api]
s3_region = "${config.s3.region}"
api_bind_addr = "[::]:3900"
root_domain = ".s3.garage.localhost"

[s3_web]
bind_addr = "[::]:3902"
root_domain = ".web.garage.localhost"
index = "index.html"
`;
}

// Generate Mosquitto configuration file
function generateMosquittoConfig(config) {
    return `listener 1883
persistence true
persistence_location /mosquitto/data/
password_file /mosquitto/config/mosquitto.passwd
allow_anonymous false
`;
}

// Generate Mosquitto password file
function generateMosquittoPasswd(config) {
    return `${config.mqtt.user}:${config.mqtt.password}
`;
}

// Generate README file
function generateReadme() {
    return `# ElectroStore - Docker Configuration

This file contains all necessary files to deploy ElectroStore with Docker.

## File Structure

- \`docker-compose.yml\` : Docker Compose configuration
- \`.env\` : Environment variables
- \`setup.sh\` : Automatic configuration script
- \`config/appsettings.json\` : API configuration

## Installation

1. Make sure Docker and Docker Compose are installed
2. Make the script executable:
   \`\`\`bash
   chmod +x setup.sh
   \`\`\`
3. Run the configuration script:
   \`\`\`bash
   ./setup.sh
   \`\`\`

The script will automatically configure:
- Garage S3 (if enabled): bucket creation and access keys
- MQTT (if enabled): configuration and password hashing
- All services will be started automatically

## Application Access

After startup, the application will be accessible at the configured URLs.
Check the setup.sh script output for details.

## Support

For more information, visit: https://github.com/vampi62/electrostore
`;
}

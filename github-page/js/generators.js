// Génération du fichier docker-compose.yml
function generateDockerCompose(config) {
    let compose = `# docker-compose.yml pour ElectroStore
# Ce fichier utilise des variables d'environnement définies dans le fichier .env
# Pour utiliser ce fichier, créez un fichier .env à la racine du projet
# Les valeurs par défaut sont fournies avec la syntaxe \${VAR:-valeur_par_defaut}

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
      - electrostore
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
      - electrostore
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
      - ./config/mosquitto.conf:/mosquitto/config/mosquitto.conf:ro
      - ./config/mosquitto.passwd:/mosquitto/config/mosquitto.passwd:ro
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
      - ./config/garage.toml:/etc/garage.toml:ro
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

if (!config.enableS3 || config.useMQTT || config.useMariaDB || (config.enableS3 && config.useS3)) {
    compose += `
volumes:`;
}

    if (!config.enableS3) {
        compose += `\n  api-wwwroot:`;
    }
    if (config.useMariaDB) compose += `\n  mariadb-data:`;
    if (config.useMQTT) compose += `\n  mqtt-data:`;
    if (config.enableS3 && config.useS3) {
        compose += `\n  garage-data:\n  garage-meta:`;
    }
    
    return compose;
}

// Génération du appsettings.json
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
        connectionString = `Server=mariadb;Port=3306;Database=${config.mariadb.database};User=${config.mariadb.user};Password=${config.mariadb.password};`;
    } else {
        const db = config.mariadbExternal;
        connectionString = `Server=${db.host};Port=${db.port};Database=${db.database};User=${db.user};Password=${db.password};`;
    }
    
    settings.ConnectionStrings = {
        "DefaultConnection": connectionString
    };

    if (config.useMQTT) {
        settings.MQTT = {
            "Server": "mqtt",
            "Port": 1883,
            "Username": config.mqtt.user,
            "Password": config.mqtt.password
        };
    } else {
        const mqtt = config.mqttExternal;
        settings.MQTT = {
            "Server": mqtt.host,
            "Port": parseInt(mqtt.port),
            "Username": mqtt.user,
            "Password": mqtt.password
        };
    }

    if (config.enableS3) {
        if (config.useS3) {
            settings.S3 = {
                "Enable": true,
                "Endpoint": "http://garage:3900",
                "AccessKey": config.s3.accessKey,
                "SecretKey": config.s3.secretKey,
                "Bucket": config.s3.bucket,
                "Region": config.s3.region,
                "Secure": false
            };
        } else if (config.s3External) {
            settings.S3 = {
                "Enable": true,
                "Endpoint": config.s3External.endpoint,
                "AccessKey": config.s3External.accessKey,
                "SecretKey": config.s3External.secretKey,
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
            "Host": config.smtp.host,
            "Port": parseInt(config.smtp.port),
            "Username": config.smtp.user,
            "Password": config.smtp.password,
            "From": config.smtp.from
        };
    }

    settings.Jwt = {
        "Key": config.jwt.key,
        "Issuer": config.jwt.issuer,
        "Audience": config.jwt.audience,
        "ExpireDays": parseInt(config.jwt.expireDays)
    };

    if (config.oauthProviders.length > 0) {
        settings.OAuth = {};
        config.oauthProviders.forEach(provider => {
            settings.OAuth[provider.displayName] = {
                "ClientId": provider.clientId,
                "ClientSecret": provider.clientSecret,
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

// Génération du .env
function generateEnvFile(config) {
    let env = `# Fichier .env pour ElectroStore\n`;
    env += `# Généré le ${new Date().toLocaleDateString('fr-FR')}\n`;
    env += `# Ce fichier contient les variables d'environnement utilisées par docker-compose.yml\n\n`;
    
    env += `# Configuration générale\n`;
    env += `PROJECT_NAME=electrostore\n`;
    env += `ENVIRONMENT=${config.appVersion == 'latest' ? 'Development' : 'Production'}\n\n`;
    
    env += `# Versions des images Docker\n`;
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
        env += `# MariaDB (service intégré)\n`;
        env += `MARIADB_DATABASE=${config.mariadb.database}\n`;
        env += `MARIADB_USER=${config.mariadb.user}\n`;
        env += `MARIADB_PASSWORD=${config.mariadb.password}\n`;
        env += `MARIADB_ROOT_PASSWORD=${config.mariadb.rootPassword}\n\n`;
    }
    
    if (config.useMQTT) {
        env += `# MQTT (service intégré)\n`;
        env += `MQTT_USER=${config.mqtt.user}\n`;
        env += `MQTT_PASSWORD=${config.mqtt.password}\n\n`;
    }
    
    if (config.enableS3 && config.useS3) {
        env += `# S3 Garage (service intégré)\n`;
        env += `S3_ACCESS_KEY=${config.s3.accessKey}\n`;
        env += `S3_SECRET_KEY=${config.s3.secretKey}\n`;
        env += `S3_BUCKET=${config.s3.bucket}\n`;
        env += `S3_REGION=${config.s3.region}\n\n`;
    }
    
    return env;
}

// Génération du script setup.sh
function generateSetupScript(config) {
    let script = `#!/bin/bash
# Script de configuration ElectroStore
# Généré le ${new Date().toLocaleDateString('fr-FR')}

set -e

echo "====================================="
echo "Configuration ElectroStore"
echo "====================================="
echo ""

`;

    if (config.enableS3 && config.useS3) {
        script += `# Configuration S3 Garage
echo "Configuration de Garage S3..."

echo "Démarrage de Garage..."
docker compose up -d garage

echo "Attente du démarrage de Garage (10 secondes)..."
sleep 10

echo "Configuration du cluster Garage..."
docker exec electrostore-garage /garage status || true
docker exec electrostore-garage /garage layout assign -z dc1 -c 1 \$(docker exec electrostore-garage garage status | grep -oP '[0-9a-f]{16}' | head -n 1) || true
docker exec electrostore-garage /garage layout apply --version 1

echo "Création des clés d'accès S3..."
docker exec electrostore-garage /garage key create electrostore-key > /tmp/garage_keys.txt
GARAGE_ACCESS_KEY=\$(grep 'Key ID' /tmp/garage_keys.txt | awk '{print \$3}')
GARAGE_SECRET_KEY=\$(grep 'Secret key' /tmp/garage_keys.txt | awk '{print \$3}')

echo "Access Key: \$GARAGE_ACCESS_KEY"
echo "Secret Key: \$GARAGE_SECRET_KEY"

echo "Création du bucket ${config.s3.bucket}..."
docker exec electrostore-garage /garage bucket create ${config.s3.bucket}

echo "Attribution des permissions..."
docker exec electrostore-garage /garage bucket allow --read --write ${config.s3.bucket} --key electrostore-key

echo "Mise à jour du fichier .env..."
sed -i "s/S3_ACCESS_KEY=.*/S3_ACCESS_KEY=\$GARAGE_ACCESS_KEY/" .env
sed -i "s/S3_SECRET_KEY=.*/S3_SECRET_KEY=\$GARAGE_SECRET_KEY/" .env

echo "Mise à jour du fichier de configuration appsettings.json..."
sed -i "s/\"AccessKey\": \".*\"/\"AccessKey\": \"$GARAGE_ACCESS_KEY\"/" config/appsettings.json
sed -i "s/\"SecretKey\": \".*\"/\"SecretKey\": \"$GARAGE_SECRET_KEY\"/" config/appsettings.json

rm /tmp/garage_keys.txt

echo "Configuration Garage terminée"
echo ""

`;
    }

    if (config.useMQTT) {
        script += `# Configuration MQTT
echo "Configuration de Mosquitto MQTT..."

echo "Hashage du mot de passe MQTT avec un conteneur temporaire..."
docker run --rm -v "./config:/temp" eclipse-mosquitto:\${MQTT_VERSION:-2.0.20} sh -c "cp /temp/mosquitto.passwd /tmp/mosquitto.passwd && mosquitto_passwd -U /tmp/mosquitto.passwd && cat /tmp/mosquitto.passwd > /temp/mosquitto.passwd"

echo "Démarrage de Mosquitto..."
docker compose up -d mqtt

echo "Configuration MQTT terminée"
echo ""

`;
    }

    script += `# Démarrer tous les services
echo "Démarrage de tous les services..."
docker compose up -d

echo ""
echo "====================================="
echo "Configuration terminée !"
echo "====================================="
echo ""
echo "Accès à l'application :"
echo "${'Frontend: ' + config.frontUrlObj.toString()}"
echo "${'API: ' + config.apiUrlObj.toString()}"
echo ""
`;

    if (config.enableS3 && config.useS3) {
        script += `echo "S3 Garage:"
echo "  Endpoint: http://localhost:3900"
echo "  Bucket: ${config.s3.bucket}"
echo "  Access Key: (voir .env)"
echo "  Secret Key: (voir .env)"
echo ""
`;
    }

    if (config.useMQTT) {
        script += `echo "MQTT:"
echo "  Host: localhost:1883"
echo "  User: ${config.mqtt.user}"
echo "  Password: (voir .env)"
echo ""
`;
    }

    return script;
}

// Génération du fichier de configuration Garage
function generateGarageConfig(config) {
    const rpcSecret = generateGarageRpcSecret();
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

// Génération du fichier de configuration Mosquitto
function generateMosquittoConfig(config) {
    return `listener 1883
persistence true
persistence_location /mosquitto/data/
password_file /mosquitto/config/mosquitto.passwd
allow_anonymous false
`;
}

// Génération du fichier de mots de passe Mosquitto
function generateMosquittoPasswd(config) {
    return `${config.mqtt.user}:${config.mqtt.password}
`;
}

// Génération du fichier README
function generateReadme() {
    return `# ElectroStore - Configuration Docker

Ce fichier contient tous les fichiers nécessaires pour déployer ElectroStore avec Docker.

## Structure des fichiers

- \`docker-compose.yml\` : Configuration Docker Compose
- \`.env\` : Variables d'environnement
- \`setup.sh\` : Script de configuration automatique
- \`config/appsettings.json\` : Configuration de l'API

## Installation

1. Assurez-vous que Docker et Docker Compose sont installés
2. Rendez le script exécutable :
   \`\`\`bash
   chmod +x setup.sh
   \`\`\`
3. Exécutez le script de configuration :
   \`\`\`bash
   ./setup.sh
   \`\`\`

Le script configurera automatiquement :
- Garage S3 (si activé) : création du bucket et des clés d'accès
- MQTT (si activé) : configuration et hashage du mot de passe
- Tous les services seront démarrés automatiquement

## Accès à l'application

Après le démarrage, l'application sera accessible aux URLs configurées.
Consultez la sortie du script setup.sh pour les détails.

## Support

Pour plus d'informations, consultez : https://github.com/vampi62/electrostore
`;
}

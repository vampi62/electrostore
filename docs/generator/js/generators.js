// Generate docker-compose.yml file
function generateDockerCompose(config) {
    let compose = `# docker-compose.yml for ElectroStore
# This file uses environment variables defined in the .env file
# To use this file, create a .env file at the project root
# Default values are provided with the syntax \${VAR:-default_value}

version: '3.8'

services:`;

    // API Backend
    if (config.appVersion === 'local') {
        compose += `
  api:
    build:
      context: ./electrostore/electrostoreAPI
      dockerfile: Dockerfile`;
    } else {
        compose += `
  api:
    image: ghcr.io/vampi62/electrostore/api:\${API_VERSION:-latest}`;
    }
    compose += `
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
      - "traefik.http.services.api.loadbalancer.server.port=8080"`;
      
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
      - "\${API_PORT:-${config.apiPort}}:5000"`;
    }
    
    compose += `
    environment:
      - ASPNETCORE_ENVIRONMENT=\${ENVIRONMENT:-Production}`;

    compose += `
    depends_on:
      kafka:
        condition: service_healthy`;
    
    if (config.useMariaDB) compose += `\n      mariadb\n        condition: service_healthy`;
    if (config.useMQTT) compose += `\n      mqtt\n        condition: service_started`;
    if (config.enableS3 && config.useS3) compose += `\n      garage\n        condition: service_healthy`;
    
    compose += `
    volumes:
      - ./config/api/appsettings.json:/app/config/appsettings.json:ro`;
    
    if (!config.enableS3) {
        compose += `\n      - api-wwwroot:/app/wwwroot`;
    }
    
    compose += `
    networks:
      electrostore:
        aliases:
          - electrostoreAPI`;
    
    if (config.useTraefik) {
        compose += `\n      - ${config.traefik.network}`;
    }
    
    compose += `
    restart: unless-stopped
`;

    // Frontend
    if (config.appVersion === 'local') {
        compose += `
  frontend:
    build:
      context: ./electrostore/electrostoreFRONT
      dockerfile: Dockerfile`;
    } else {
        compose += `
  frontend:
    image: ghcr.io/vampi62/electrostore/front:\${FRONTEND_VERSION:-latest}`;
    }
    compose += `
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
    if (config.enableVapid && config.vapid) {
        compose += `
      - VUE_VAPID_PUBLIC_KEY=${config.vapid.publicKey}`;
    }
      
    
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
    if (config.appVersion === 'local') {
        compose += `
  ia:
    build:
      context: ./electrostore/electrostoreIA
      dockerfile: Dockerfile`;
    } else {
        compose += `
  ia:
    image: ghcr.io/vampi62/electrostore/ia:\${IA_VERSION:-latest}`;
    }
    compose += `
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
    depends_on:
      kafka
        condition: service_healthy
      api:
        condition: service_healthy`;
    
    if (config.useMariaDB) compose += `\n      - mariadb\n        condition: service_healthy`;
    if (config.enableS3 && config.useS3) compose += `\n      - garage\n        condition: service_healthy`;
    
    compose += `
    volumes:
      - ./config/ia/appsettings.json:/app/config/appsettings.json:ro`;
    if (!config.enableS3) {
        compose += `\n      - ia-models:/app/models`;
    }
    compose += `
    networks:
      electrostore:
        aliases:
          - electrostoreIA
    restart: unless-stopped
`;

    // NOTIF Service
    if (config.appVersion === 'local') {
        compose += `
  notif:
    build:
      context: ./electrostore/electrostoreNOTIF
      dockerfile: Dockerfile`;
    } else {
        compose += `
  notif:
    image: ghcr.io/vampi62/electrostore/notif:\${NOTIF_VERSION:-latest}`;
    }
    compose += `
    container_name: electrostore-notif
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
          cpus: '1.0'
          memory: 512M
    depends_on:
      kafka:
        condition: service_healthy
      api:
        condition: service_healthy
    volumes:
      - ./config/notif/appsettings.json:/app/config/appsettings.json:ro
    networks:
      electrostore:
        aliases:
          - electrostoreNOTIF
    restart: unless-stopped
`;

    // CRON Service
    if (config.appVersion === 'local') {
        compose += `
  cron:
    build:
      context: ./electrostore/electrostoreCRON
      dockerfile: Dockerfile`;
    } else {
        compose += `
  cron:
    image: ghcr.io/vampi62/electrostore/cron:\${CRON_VERSION:-latest}`;
    }
    compose += `
    container_name: electrostore-cron
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
          cpus: '1.0'
          memory: 256M
    depends_on:
      kafka:
        condition: service_healthy
      api:
        condition: service_healthy
    volumes:
      - ./config/cron/appsettings.json:/app/config/appsettings.json:ro
    networks:
      electrostore:
        aliases:
          - electrostoreCRON
    restart: unless-stopped
`;

    // WORKER Service
    if (config.appVersion === 'local') {
        compose += `
  worker:
    build:
      context: ./electrostore/electrostoreWORKER
      dockerfile: Dockerfile`;
    } else {
        compose += `
  worker:
    image: ghcr.io/vampi62/electrostore/worker:\${WORKER_VERSION:-latest}`;
    }
        compose += `
    container_name: electrostore-worker
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
          cpus: '1.0'
          memory: 256M`;

    compose += `
    depends_on:
      kafka:
        condition: service_healthy
      api:
        condition: service_healthy`;

    if (config.useMQTT) compose += `\n      mqtt\n        condition: service_started`;

    compose += `
    volumes:
      - ./config/worker/appsettings.json:/app/config/appsettings.json:ro
    networks:
      electrostore:
        aliases:
          - electrostoreWORKER
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
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 15s
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
      - mqtt-logs:/mosquitto/log
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
      - "3903:3903"
    volumes:
      - ./config/garage/garage.toml:/etc/garage.toml:ro
      - garage-data:/data
      - garage-meta:/meta
    networks:
      - electrostore
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "/garage", "-c", "/etc/garage.toml", "status"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 15s
`;
    }

    // Kafka
    compose += `
  kafka:
    image: apache/kafka:\${KAFKA_VERSION:-4.2.1}
    container_name: electrostore-kafka
    environment:
      - KAFKA_NODE_ID=1
      - KAFKA_PROCESS_ROLES=broker,controller
      - KAFKA_LISTENERS=PLAINTEXT://:9092,CONTROLLER://:9093
      - KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://kafka:9092
      - KAFKA_CONTROLLER_LISTENER_NAMES=CONTROLLER
      - KAFKA_LISTENER_SECURITY_PROTOCOL_MAP=CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT
      - KAFKA_CONTROLLER_QUORUM_VOTERS=1@kafka:9093
      - KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1
      - KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR=1
      - KAFKA_TRANSACTION_STATE_LOG_MIN_ISR=1
      - KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS=0
      - KAFKA_NUM_PARTITIONS=3
      - KAFKA_AUTO_CREATE_TOPICS_ENABLE=true
      - KAFKA_CREATE_TOPICS="notification-requests:3:1,cronjob-events:3:1,ia-requests:3:1,cron-parcel-tracking:3:1,ia-status:3:1"
    volumes:
      - kafka-data:/var/lib/kafka/data
      - kafka-config:/mnt/shared/config
      - kafka-secrets:/etc/kafka/secrets
    networks:
      - electrostore
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "/opt/kafka/bin/kafka-broker-api-versions.sh --bootstrap-server localhost:9092 || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 15s
`;

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

    compose += `
volumes:
  kafka-data:
  kafka-config:
  kafka-secrets:`;

    if (!config.enableS3) compose += `\n  api-wwwroot:\n  ia-models:`;
    if (config.useMariaDB) compose += `\n  mariadb-data:`;
    if (config.useMQTT) compose += `\n  mqtt-data:\n  mqtt-logs:`;
    if (config.enableS3 && config.useS3) compose += `\n  garage-data:\n  garage-meta:`;

    return compose;
}

// Generate appsettings.json for API service
function generateApiAppsettings(config) {
    const settings = {
        "Logging": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft.AspNetCore": "Warning"
            }
        },
        "Kestrel": {
            "Endpoints": {
                "Grpc": {
                    "Url": "http://0.0.0.0:5001",
                    "Protocols": "Http2"
                },
                "Http": {
                    "Url": "http://0.0.0.0:5000",
                    "Protocols": "Http1"
                }
            }
        },
        "AllowedHosts": "*"
    };

    let connectionString;
    if (config.useMariaDB) {
        connectionString = `Server=mariadb;Port=3306;Database=${config.mariadb.database};User=${config.mariadb.user};Password=${config.useVault ? '{{vault:mariadb_password}}' : config.mariadb.password};`;
    } else {
        const db = config.mariadbExternal;
        connectionString = `Server=${db.host};Port=${db.port};Database=${db.database};User=${db.user};Password=${config.useVault ? '{{vault:mariadb_password}}' : db.password};`;
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
                "Endpoint": "garage:3900",
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

    settings.Kafka = {
        "BootstrapServers": "kafka:9092"
    };

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

    if (config.useVault) {
        settings.Vault = {
            "Enable": true,
            "Addr": config.vault.addr,
            "Token": config.vault.token,
            "Path": config.vault.path,
            "MountPoint": config.vault.mountPoint
        };
    } else {
        settings.Vault = {
            "Enable": false
        };
    }

    settings.IAServiceGrpcUrl = "http://electrostoreIA:5001";
    settings.IAServiceHealthUrl = "http://electrostoreIA:5000/health";
    settings.NotifServiceHealthUrl = "http://electrostoreNOTIF:5000/health";
    settings.CRONServiceHealthUrl = "http://electrostoreCRON:5000/health";
    settings.WORKERServiceHealthUrl = "http://electrostoreWORKER:5000/health";
    settings.DemoMode = false;
    settings.FrontendUrl = config.frontUrl;
    settings.AllowedOrigins = config.allowedOrigins;

    return JSON.stringify(settings, null, 2);
}

// Generate appsettings.json for IA service
function generateIaAppsettings(config) {
    const settings = {
        "Logging": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft.AspNetCore": "Warning",
                "Microsoft.ML": "Warning"
            }
        },
        "Kestrel": {
            "Endpoints": {
                "Grpc": {
                    "Url": "http://0.0.0.0:5001",
                    "Protocols": "Http2"
                },
                "Http": {
                    "Url": "http://0.0.0.0:5000",
                    "Protocols": "Http1"
                }
            }
        }
    };

    settings.Kafka = {
        "BootstrapServers": "kafka:9092",
        "ConsumerGroupId": "ia-service"
    };

    settings.ApiServiceGrpcUrl = "http://electrostoreAPI:5001";

    if (config.useVault) {
        settings.Vault = {
            "Enable": true,
            "Addr": config.vault.addr,
            "Token": config.vault.token,
            "Path": config.vault.path,
            "MountPoint": config.vault.mountPoint
        };
    } else {
        settings.Vault = {
            "Enable": false,
            "Addr": "http://vault:8200",
            "Token": "",
            "Path": "",
            "MountPoint": "secret"
        };
    }

    if (config.enableS3) {
        if (config.useS3) {
            settings.S3 = {
                "Enable": true,
                "Endpoint": "garage:3900",
                "AccessKey": config.useVault ? "{{vault:s3_ia_access_key}}" : config.s3Ia.accessKey,
                "SecretKey": config.useVault ? "{{vault:s3_ia_secret_key}}" : config.s3Ia.secretKey,
                "Bucket": config.s3Ia.bucket,
                "Region": config.s3Ia.region,
                "Secure": false
            };
        } else if (config.s3IaExternal) {
            settings.S3 = {
                "Enable": true,
                "Endpoint": config.s3IaExternal.endpoint,
                "AccessKey": config.useVault ? "{{vault:s3_ia_access_key}}" : config.s3IaExternal.accessKey,
                "SecretKey": config.useVault ? "{{vault:s3_ia_secret_key}}" : config.s3IaExternal.secretKey,
                "Bucket": config.s3IaExternal.bucket,
                "Region": config.s3IaExternal.region,
                "Secure": config.s3IaExternal.secure
            };
        }
    } else {
        settings.S3 = { "Enable": false };
    }

    settings.DefaultEpochs = 10;
    settings.DefaultBatchSize = 32;

    return JSON.stringify(settings, null, 2);
}

// Generate appsettings.json for NOTIF service
function generateNotifAppsettings(config) {
    const settings = {
        "Logging": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft": "Warning"
            }
        },
        "Kestrel": {
            "Endpoints": {
                "Http": {
                    "Url": "http://0.0.0.0:5000",
                    "Protocols": "Http1"
                }
            }
        }
    };

    settings.Kafka = {
        "BootstrapServers": "kafka:9092",
        "ConsumerGroupId": "notif-service"
    };

    settings.ApiServiceGrpcUrl = "http://electrostoreAPI:5001";

    if (config.useVault) {
        settings.Vault = {
            "Enable": true,
            "Addr": config.vault.addr,
            "Token": config.vault.token,
            "Path": config.vault.path,
            "MountPoint": config.vault.mountPoint
        };
    } else {
        settings.Vault = {
            "Enable": false,
            "Addr": "http://vault:8200",
            "Token": "",
            "Path": "",
            "MountPoint": "secret"
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

    if (config.enableVapid && config.vapid) {
        settings.VAPID = {
            "Subject": config.vapid.subject,
            "PublicKey": config.vapid.publicKey,
            "PrivateKey": config.useVault ? "{{vault:vapid_private_key}}" : config.vapid.privateKey
        };
    } else {
        settings.VAPID = {
            "Subject": "mailto:admin@electrostore.local",
            "PublicKey": "",
            "PrivateKey": ""
        };
    }

    return JSON.stringify(settings, null, 2);
}

// Generate appsettings.json for CRON service
function generateCronAppsettings(config) {
    const settings = {
        "Logging": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft.AspNetCore": "Warning",
                "Quartz": "Warning"
            }
        },
        "Kestrel": {
            "Endpoints": {
                "Http": {
                    "Url": "http://0.0.0.0:5000",
                    "Protocols": "Http1"
                }
            }
        }
    };

    settings.Kafka = {
        "BootstrapServers": "kafka:9092",
        "CronConsumerGroupId": "cron-service-events"
    };

    if (config.enableTrack17) {
        settings.Track17 = {
            "ApiKey": config.useVault ? "{{vault:track17_api_key}}" : (config.track17ApiKey || "")
        };
    } else {
        settings.Track17 = { "ApiKey": "" };
    }

    settings.ApiServiceGrpcUrl = "http://electrostoreAPI:5001";
    settings.CronRefreshIntervalMinutes = 60;

    if (config.useVault) {
        settings.Vault = {
            "Enable": true,
            "Addr": config.vault.addr,
            "Token": config.vault.token,
            "Path": config.vault.path,
            "MountPoint": config.vault.mountPoint
        };
    } else {
        settings.Vault = {
            "Enable": false,
            "Addr": "http://vault:8200",
            "Token": "",
            "Path": "",
            "MountPoint": "secret"
        };
    }

    return JSON.stringify(settings, null, 2);
}

// Generate appsettings.json for WORKER service
function generateWorkerAppsettings(config) {
    const settings = {
        "Logging": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft.AspNetCore": "Warning"
            }
        },
        "Kestrel": {
            "Endpoints": {
                "Http": {
                    "Url": "http://0.0.0.0:5000",
                    "Protocols": "Http1"
                }
            }
        }
    };

    settings.Kafka = {
        "BootstrapServers": "kafka:9092",
        "ConsumerGroupId": "worker-service"
    };

    if (config.useMQTT) {
        settings.Mqtt = {
            "Host": "mqtt",
            "Port": "1883",
            "Username": config.mqtt.user,
            "Password": config.useVault ? "{{vault:mqtt_password}}" : config.mqtt.password,
            "ClientId": "electrostore-worker"
        };
    } else {
        const mqtt = config.mqttExternal;
        settings.Mqtt = {
            "Host": mqtt.host,
            "Port": mqtt.port,
            "Username": mqtt.user,
            "Password": config.useVault ? "{{vault:mqtt_password}}" : mqtt.password,
            "ClientId": "electrostore-worker"
        };
    }

    settings.ApiServiceGrpcUrl = "http://electrostoreAPI:5001";

    if (config.useVault) {
        settings.Vault = {
            "Enable": true,
            "Addr": config.vault.addr,
            "Token": config.vault.token,
            "Path": config.vault.path,
            "MountPoint": config.vault.mountPoint
        };
    } else {
        settings.Vault = {
            "Enable": false,
            "Addr": "http://vault:8200",
            "Token": "",
            "Path": "",
            "MountPoint": "secret"
        };
    }

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
    env += `NOTIF_VERSION=${config.appVersion}\n`;
    env += `CRON_VERSION=${config.appVersion}\n`;
    env += `WORKER_VERSION=${config.appVersion}\n`;
    env += `KAFKA_VERSION=4.2.1\n`;
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

    if (config.enableVapid && config.vapid) {
        env += `# VAPID (Web Push Notifications)\n`;
        env += `VAPID_PUBLIC_KEY=${config.vapid.publicKey}\n`;
        env += `VAPID_PRIVATE_KEY=${config.vapid.privateKey}\n\n`;
    }
    
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
        env += `# S3 Garage - API service (integrated service)\n`;
        env += `S3_ACCESS_KEY=${config.s3.accessKey}\n`;
        env += `S3_SECRET_KEY=${config.s3.secretKey}\n`;
        env += `S3_BUCKET=${config.s3.bucket}\n`;
        env += `S3_REGION=${config.s3.region}\n\n`;
        env += `# S3 Garage - IA service (integrated service)\n`;
        env += `S3_IA_ACCESS_KEY=${config.s3Ia.accessKey}\n`;
        env += `S3_IA_SECRET_KEY=${config.s3Ia.secretKey}\n`;
        env += `S3_IA_BUCKET=${config.s3Ia.bucket}\n\n`;
        env += `S3_IA_REGION=${config.s3Ia.region}\n\n`;
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
    if (config.appVersion === 'local') {
        script += `# Clone repository
echo "Cloning ElectroStore repository..."
if [ -d "electrostore" ]; then
    echo "Repository already exists, skipping clone..."
else
    git clone https://github.com/vampi62/electrostore.git
fi
echo ""

    `;
    }

    if (config.useVault) {
        script += `# Vault Configuration
echo "Configuring HashiCorp Vault..."

export VAULT_TOKEN='${config.vault.token}'

echo "Enabling KV v2 engine..."
docker exec -e VAULT_TOKEN="$VAULT_TOKEN" ${config.vault.containerName} vault secrets enable -version=2 -path=${config.vault.mountPoint} kv || echo "KV engine already enabled"

echo "Storing secrets in Vault..."
`;

        script += `docker exec -e VAULT_TOKEN="$VAULT_TOKEN" ${config.vault.containerName} vault kv put ${config.vault.mountPoint}/${config.vault.path} mariadb_password='${escapeVaultSecret(config.useMariaDB ? config.mariadb.password : config.mariadbExternal.password)}'
`;

        script += `docker exec -e VAULT_TOKEN="$VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} mqtt_password='${escapeVaultSecret(config.useMQTT ? config.mqtt.password : config.mqttExternal.password)}'
`;

        if (config.enableSMTP && config.smtp) {
            script += `docker exec -e VAULT_TOKEN="$VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} smtp_password='${escapeVaultSecret(config.smtp.password)}'
`;
        }

        if (config.enableVapid && config.vapid) {
            script += `docker exec -e VAULT_TOKEN="$VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} vapid_private_key='${escapeVaultSecret(config.vapid.privateKey)}'
`;
        }

        script += `docker exec -e VAULT_TOKEN="$VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} jwt_key='${escapeVaultSecret(config.jwt.key)}'
`;

        if (config.oauthProviders.length > 0) {
            config.oauthProviders.forEach(provider => {
                const name = toSnakeCase(provider.displayName);
                script += `docker exec -e VAULT_TOKEN="$VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} oauth_${name}_client_id='${escapeVaultSecret(provider.clientId)}'
`;
                script += `docker exec -e VAULT_TOKEN="$VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} oauth_${name}_client_secret='${escapeVaultSecret(provider.clientSecret)}'
`;
            });
        }

        script += `
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
GARAGE_NODE_ID=$(docker exec electrostore-garage /garage status | grep -oP '[0-9a-f]{16}' | head -n 1)
GARAGE_CAPACITY=10000000000 # 10GB, adjust as needed
docker exec electrostore-garage /garage layout assign $GARAGE_NODE_ID -z dc1 --capacity $GARAGE_CAPACITY || true
docker exec electrostore-garage /garage layout apply --version 1

echo "Creating S3 access keys..."
GARAGE_API_ACCESS_KEY="${config.s3.accessKey}"
GARAGE_API_SECRET_KEY="${config.s3.secretKey}"
GARAGE_IA_ACCESS_KEY="${config.s3Ia.accessKey}"
GARAGE_IA_SECRET_KEY="${config.s3Ia.secretKey}"

echo "API Access Key: \$GARAGE_API_ACCESS_KEY"
echo "API Secret Key: \$GARAGE_API_SECRET_KEY"
echo "IA Access Key: \$GARAGE_IA_ACCESS_KEY"
echo "IA Secret Key: \$GARAGE_IA_SECRET_KEY"

echo "Creating bucket ${config.s3.bucket} for API service..."
docker exec electrostore-garage /garage key import -n electrostore-key --yes $GARAGE_API_ACCESS_KEY $GARAGE_API_SECRET_KEY
docker exec electrostore-garage /garage bucket create ${config.s3.bucket}
docker exec electrostore-garage /garage bucket allow --read --write ${config.s3.bucket} --key electrostore-key

echo "Creating bucket ${config.s3Ia.bucket} for IA service..."
docker exec electrostore-garage /garage key import -n electrostore-ia-key --yes $GARAGE_IA_ACCESS_KEY $GARAGE_IA_SECRET_KEY
docker exec electrostore-garage /garage bucket create ${config.s3Ia.bucket}
docker exec electrostore-garage /garage bucket allow --read --write ${config.s3Ia.bucket} --key electrostore-ia-key
`;

        if (config.useVault) {
            script += `
echo "Storing S3 keys in Vault..."
docker exec -e VAULT_TOKEN="$VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} s3_access_key="$GARAGE_API_ACCESS_KEY" s3_secret_key="$GARAGE_API_SECRET_KEY" s3_ia_access_key="$GARAGE_IA_ACCESS_KEY" s3_ia_secret_key="$GARAGE_IA_SECRET_KEY"
`;
        }

        script += `
#rm /tmp/garage_keys.txt

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

// Generate setup.ps1 script for Windows
function generateSetupScriptWindows(config) {
    let script = `# ElectroStore Configuration Script (Windows)
# Generated on ${new Date().toLocaleDateString('en-US')}

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "ElectroStore Configuration" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

`;
    if (config.appVersion === 'local') {
        script += `# Clone repository
Write-Host "Cloning ElectroStore repository..." -ForegroundColor Yellow
if (Test-Path "electrostore") {
    Write-Host "Repository already exists, skipping clone..." -ForegroundColor Yellow
} else {
    git clone https://github.com/vampi62/electrostore.git
}
Write-Host ""

`;
    }

    if (config.useVault) {
        script += `# Vault Configuration
Write-Host "Configuring HashiCorp Vault..." -ForegroundColor Yellow

$env:VAULT_TOKEN = '${config.vault.token}'

Write-Host "Enabling KV v2 engine..."
docker exec -e VAULT_TOKEN="$env:VAULT_TOKEN" ${config.vault.containerName} vault secrets enable -version=2 -path=${config.vault.mountPoint} kv
if ($LASTEXITCODE -ne 0) { Write-Host "KV engine already enabled" }

Write-Host "Storing secrets in Vault..."
`;

        script += `docker exec -e VAULT_TOKEN="$env:VAULT_TOKEN" ${config.vault.containerName} vault kv put ${config.vault.mountPoint}/${config.vault.path} mariadb_password='${escapeVaultSecret(config.useMariaDB ? config.mariadb.password : config.mariadbExternal.password)}'
`;

        script += `docker exec -e VAULT_TOKEN="$env:VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} mqtt_password='${escapeVaultSecret(config.useMQTT ? config.mqtt.password : config.mqttExternal.password)}'
`;

        if (config.enableSMTP && config.smtp) {
            script += `docker exec -e VAULT_TOKEN="$env:VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} smtp_password='${escapeVaultSecret(config.smtp.password)}'
`;
        }

        if (config.enableVapid && config.vapid) {
            script += `docker exec -e VAULT_TOKEN="$env:VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} vapid_private_key='${escapeVaultSecret(config.vapid.privateKey)}'
`;
        }

        script += `docker exec -e VAULT_TOKEN="$env:VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} jwt_key='${escapeVaultSecret(config.jwt.key)}'
`;

        if (config.oauthProviders.length > 0) {
            config.oauthProviders.forEach(provider => {
                const name = toSnakeCase(provider.displayName);
                script += `docker exec -e VAULT_TOKEN="$env:VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} oauth_${name}_client_id='${escapeVaultSecret(provider.clientId)}'
`;
                script += `docker exec -e VAULT_TOKEN="$env:VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} oauth_${name}_client_secret='${escapeVaultSecret(provider.clientSecret)}'
`;
            });
        }

        script += `
Write-Host "Vault configuration completed" -ForegroundColor Green
Write-Host ""

`;
    }

    if (config.enableS3 && config.useS3) {
        script += `# S3 Garage Configuration
Write-Host "Configuring Garage S3..." -ForegroundColor Yellow

Write-Host "Starting Garage..."
docker compose up -d garage

Write-Host "Waiting for Garage to start (10 seconds)..."
Start-Sleep -Seconds 10

Write-Host "Configuring Garage cluster..."
$GARAGE_NODE_ID = (docker exec electrostore-garage /garage status) | Select-String -Pattern '[0-9a-f]{16}' | ForEach-Object { $_.Matches[0].Value } | Select-Object -First 1
$GARAGE_CAPACITY = 10000000000 # 10GB, adjust as needed
docker exec electrostore-garage /garage layout assign $GARAGE_NODE_ID -z dc1 --capacity $GARAGE_CAPACITY
docker exec electrostore-garage /garage layout apply --version 1

Write-Host "Creating S3 access keys..."
$GARAGE_API_ACCESS_KEY = "${config.s3.accessKey}"
$GARAGE_API_SECRET_KEY = "${config.s3.secretKey}"
$GARAGE_IA_ACCESS_KEY = "${config.s3Ia.accessKey}"
$GARAGE_IA_SECRET_KEY = "${config.s3Ia.secretKey}"

Write-Host "API Access Key: $GARAGE_API_ACCESS_KEY"
Write-Host "API Secret Key: $GARAGE_API_SECRET_KEY"
Write-Host "IA Access Key: $GARAGE_IA_ACCESS_KEY"
Write-Host "IA Secret Key: $GARAGE_IA_SECRET_KEY"

Write-Host "Creating bucket ${config.s3.bucket} for API service..."
docker exec electrostore-garage /garage key import -n electrostore-key --yes $GARAGE_API_ACCESS_KEY $GARAGE_API_SECRET_KEY
docker exec electrostore-garage /garage bucket create ${config.s3.bucket}
docker exec electrostore-garage /garage bucket allow --read --write ${config.s3.bucket} --key electrostore-key

Write-Host "Creating bucket ${config.s3Ia.bucket} for IA service..."
docker exec electrostore-garage /garage key import -n electrostore-ia-key --yes $GARAGE_IA_ACCESS_KEY $GARAGE_IA_SECRET_KEY
docker exec electrostore-garage /garage bucket create ${config.s3Ia.bucket}
docker exec electrostore-garage /garage bucket allow --read --write ${config.s3Ia.bucket} --key electrostore-ia-key
`;

        if (config.useVault) {
            script += `
Write-Host "Storing S3 keys in Vault..."
docker exec -e VAULT_TOKEN="$env:VAULT_TOKEN" ${config.vault.containerName} vault kv patch ${config.vault.mountPoint}/${config.vault.path} s3_access_key="$GARAGE_API_ACCESS_KEY" s3_secret_key="$GARAGE_API_SECRET_KEY" s3_ia_access_key="$GARAGE_IA_ACCESS_KEY" s3_ia_secret_key="$GARAGE_IA_SECRET_KEY"
`;
        }

        script += `
Write-Host "Garage configuration completed" -ForegroundColor Green
Write-Host ""

`;
    }

    if (config.useMQTT) {
        script += `# MQTT Configuration
Write-Host "Configuring Mosquitto MQTT..." -ForegroundColor Yellow

Write-Host "Hashing MQTT password with temporary container..."
$MqttConfigPath = Join-Path $PWD "config/mosquitto"
docker run --rm -v "\${MqttConfigPath}:/temp" eclipse-mosquitto:2.0.20 sh -c "cp /temp/mosquitto.passwd /tmp/mosquitto.passwd && mosquitto_passwd -U /tmp/mosquitto.passwd && cat /tmp/mosquitto.passwd > /temp/mosquitto.passwd"

Write-Host "Starting Mosquitto..."
docker compose up -d mqtt

Write-Host "MQTT configuration completed" -ForegroundColor Green
Write-Host ""

`;
    }

    script += `# Start all services
Write-Host "Starting all services..."
docker compose up -d

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Configuration completed!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Application access:"
Write-Host "Frontend: ${'${config.frontUrlObj.toString()}'}"
Write-Host "API: ${'${config.apiUrlObj.toString()}'}"
Write-Host ""
`;

    if (config.enableS3 && config.useS3) {
        script += `Write-Host "S3 Garage:"
Write-Host "  Endpoint: http://localhost:3900"
Write-Host "  Bucket: ${config.s3.bucket}"
Write-Host "  Access Key: (see .env)"
Write-Host "  Secret Key: (see .env)"
Write-Host ""
`;
    }

    if (config.useMQTT) {
        script += `Write-Host "MQTT:"
Write-Host "  Host: localhost:1883"
Write-Host "  User: ${config.mqtt.user}"
Write-Host "  Password: (see .env)"
Write-Host ""
`;
    }

    return script;
}

// Generate README file
function generateReadme() {
    return `# ElectroStore - Docker Configuration

This file contains all necessary files to deploy ElectroStore with Docker.

## File Structure

- \`docker-compose.yml\` : Docker Compose configuration
- \`.env\` : Environment variables
- \`setup.sh\` : Automatic configuration script
- \`config/api/appsettings.json\` : API configuration
- \`config/ia/appsettings.json\` : IA service configuration
- \`config/notif/appsettings.json\` : Notification service configuration
- \`config/cron/appsettings.json\` : CRON service configuration
- \`config/worker/appsettings.json\` : WORKER service configuration

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

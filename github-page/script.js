// Variables globales
let oauthProvidersCount = 0;

// Initialisation au chargement
document.addEventListener('DOMContentLoaded', function() {
    initializeForm();
});

// Gestion du formulaire
document.getElementById('configForm').addEventListener('submit', function(e) {
    e.preventDefault();
    generateFiles();
});

// Initialisation du formulaire
function initializeForm() {
    // Initialiser les sections visibles
    toggleSection('mariadb');
    toggleSection('mqtt');
}

// Basculer l'affichage des sections selon les checkboxes
function toggleSection(section) {
    console.log(`Toggling section: ${section}`);
    console.log(`use${section.charAt(0).toUpperCase() + section.slice(1)}`);
    const checkbox = document.getElementById(`use${section.charAt(0).toUpperCase() + section.slice(1)}`);
    const isChecked = checkbox ? checkbox.checked : false;
    
    switch(section) {
        case 'traefik':
            document.getElementById('section-traefik').style.display = isChecked ? 'block' : 'none';
            document.getElementById('section-ports').style.display = isChecked ? 'none' : 'block';
            break;
        case 'mariadb':
            document.getElementById('section-mariadb-integrated').style.display = isChecked ? 'block' : 'none';
            document.getElementById('section-mariadb-external').style.display = isChecked ? 'none' : 'block';
            break;
        case 'mqtt':
            document.getElementById('section-mqtt-integrated').style.display = isChecked ? 'block' : 'none';
            document.getElementById('section-mqtt-external').style.display = isChecked ? 'none' : 'block';
            break;
        case 's3':
            document.getElementById('section-s3-integrated').style.display = isChecked ? 'block' : 'none';
            document.getElementById('section-s3-external').style.display = isChecked ? 'none' : 'block';
            break;
    }
}

// Basculer l'activation S3
function toggleS3Enable() {
    const enabled = document.getElementById('enableS3').checked;
    document.getElementById('section-s3-choice').style.display = enabled ? 'block' : 'none';
    
    if (enabled) {
        // Si S3 est activé, afficher la section appropriée selon le toggle useS3
        toggleSection('s3');
    } else {
        // Si S3 est désactivé, masquer toutes les sections S3
        document.getElementById('section-s3-integrated').style.display = 'none';
        document.getElementById('section-s3-external').style.display = 'none';
    }
}

// Basculer SMTP
function toggleSMTP() {
    const enabled = document.getElementById('enableSMTP').checked;
    document.getElementById('section-smtp').style.display = enabled ? 'block' : 'none';
}

// Basculer Traefik TLS
function toggleTraefikTLS() {
    const enabled = document.getElementById('traefikTlsEnable').checked;
    document.getElementById('section-traefik-tls').style.display = enabled ? 'block' : 'none';
}

// Génération de mot de passe aléatoire
function generatePassword(fieldId, length = 32) {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?';
    let password = '';
    
    for (let i = 0; i < length; i++) {
        password += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    
    document.getElementById(fieldId).value = password;
}

// Ajouter un provider OAuth
function addOAuthProvider() {
    oauthProvidersCount++;
    const providerId = `oauth-${oauthProvidersCount}`;
    
    const providerHtml = `
        <div class="oauth-provider" id="${providerId}">
            <div class="oauth-provider-header">
                <h4>Provider OAuth #${oauthProvidersCount}</h4>
                <button type="button" class="btn-remove" onclick="removeOAuthProvider('${providerId}')">Supprimer</button>
            </div>
            
            <div class="form-group">
                <label>Nom d'affichage</label>
                <input type="text" name="${providerId}_displayName" placeholder="Google, GitHub, Azure AD...">
            </div>
            
            <div class="form-group">
                <label>Client ID</label>
                <input type="text" name="${providerId}_clientId">
            </div>
            
            <div class="form-group">
                <label>Client Secret</label>
                <input type="password" name="${providerId}_clientSecret">
            </div>
            
            <div class="form-group">
                <label>Authority</label>
                <input type="text" name="${providerId}_authority" placeholder="https://accounts.google.com">
            </div>
            
            <div class="form-group">
                <label>Redirect URI</label>
                <input type="text" name="${providerId}_redirectUri" placeholder="/signin-oauth">
            </div>
            
            <div class="form-group">
                <label>Scope</label>
                <input type="text" name="${providerId}_scope" placeholder="openid profile email">
            </div>
            
            <div class="form-group">
                <label>Icon URL</label>
                <input type="text" name="${providerId}_iconUrl" placeholder="https://example.com/icon.png">
            </div>
            
            <div class="form-group">
                <label>Mapping des groupes</label>
                <div class="oauth-group-mapping">
                    <div class="form-group">
                        <label>User</label>
                        <input type="text" name="${providerId}_groupUser" placeholder="users">
                    </div>
                    <div class="form-group">
                        <label>Moderator</label>
                        <input type="text" name="${providerId}_groupModerator" placeholder="moderators">
                    </div>
                    <div class="form-group">
                        <label>Admin</label>
                        <input type="text" name="${providerId}_groupAdmin" placeholder="admins">
                    </div>
                </div>
            </div>
        </div>
    `;
    
    document.getElementById('oauthProviders').insertAdjacentHTML('beforeend', providerHtml);
}

// Supprimer un provider OAuth
function removeOAuthProvider(providerId) {
    document.getElementById(providerId).remove();
}

// Réinitialisation du formulaire
function resetForm() {
    document.getElementById('configForm').reset();
    document.getElementById('oauthProviders').innerHTML = '';
    oauthProvidersCount = 0;
    document.getElementById('result').classList.add('hidden');
    initializeForm();
}


// Génération des fichiers
function generateFiles() {
    const formData = new FormData(document.getElementById('configForm'));
    const config = collectConfig(formData);
    console.log('Configuration collectée :', config);
    
    const dockerCompose = generateDockerCompose(config);
    const appsettings = generateAppsettings(config);
    const envFile = generateEnvFile(config);
    
    document.getElementById('dockerCompose').textContent = dockerCompose;
    document.getElementById('appsettingsFile').textContent = appsettings;
    document.getElementById('envFile').textContent = envFile;
    
    const appUrl = config.useTraefik 
        ? `http://${config.traefikFrontDomain}`
        : `http://localhost:${config.frontendPort}`;
    document.getElementById('appUrl').textContent = appUrl;
    
    document.getElementById('result').classList.remove('hidden');
    document.getElementById('result').scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// Collecter toutes les données du formulaire
function collectConfig(formData) {
    const config = {
        // Services intégrés
        useTraefik: document.getElementById('useTraefik').checked,
        useMariaDB: document.getElementById('useMariadb').checked,
        useMQTT: document.getElementById('useMqtt').checked,
        enableS3: document.getElementById('enableS3').checked,
        useS3: document.getElementById('useS3').checked,
        enableSMTP: document.getElementById('enableSMTP').checked,
        
        // OAuth Providers
        oauthProviders: []
    };
    
    // Traefik configuration avancée
    if (config.useTraefik) {
        config.traefik = {
            entrypoint: formData.get('traefikEntrypoint') || 'web',
            middlewares: formData.get('traefikMiddlewares') || '',
            tlsEnable: document.getElementById('traefikTlsEnable') ? document.getElementById('traefikTlsEnable').checked : false
        };
        
        if (config.traefik.tlsEnable) {
            config.traefik.certResolver = formData.get('traefikCertResolver') || 'letsencrypt';
            config.traefik.tlsEntrypoint = formData.get('traefikTlsEntrypoint') || 'websecure';
        }
    }
    
    // MariaDB
    if (config.useMariaDB) {
        config.mariadb = {
            database: formData.get('mariadbDatabase') || 'electrostore_db',
            user: formData.get('mariadbUser') || 'electrostore_user',
            password: formData.get('mariadbPassword') || generateRandomPassword(32),
            rootPassword: formData.get('mariadbRootPassword') || generateRandomPassword(32)
        };
    } else {
        config.mariadbExternal = {
            host: formData.get('mariadbExternalHost'),
            port: formData.get('mariadbExternalPort') || '3306',
            database: formData.get('mariadbExternalDatabase'),
            user: formData.get('mariadbExternalUser'),
            password: formData.get('mariadbExternalPassword')
        };
    }
    
    // MQTT
    if (config.useMQTT) {
        config.mqtt = {
            user: formData.get('mqttUser') || 'electrostore_mqtt',
            password: formData.get('mqttPassword') || generateRandomPassword(32)
        };
    } else {
        config.mqttExternal = {
            host: formData.get('mqttExternalHost'),
            port: formData.get('mqttExternalPort') || '1883',
            user: formData.get('mqttExternalUser'),
            password: formData.get('mqttExternalPassword')
        };
    }
    
    // S3
    if (config.enableS3) {
        if (config.useS3) {
            config.s3 = {
                accessKey: formData.get('s3AccessKey') || generateRandomPassword(20),
                secretKey: formData.get('s3SecretKey') || generateRandomPassword(40),
                bucket: formData.get('s3Bucket') || 'electrostore-images',
                region: formData.get('s3Region') || 'garage'
            };
        } else {
            config.s3External = {
                endpoint: formData.get('s3ExternalEndpoint'),
                accessKey: formData.get('s3ExternalAccessKey'),
                secretKey: formData.get('s3ExternalSecretKey'),
                bucket: formData.get('s3ExternalBucket'),
                region: formData.get('s3ExternalRegion') || 'us-east-1',
                secure: document.getElementById('s3ExternalSecure') ? document.getElementById('s3ExternalSecure').checked : true
            };
        }
    }
    
    // SMTP
    if (config.enableSMTP) {
        config.smtp = {
            host: formData.get('smtpHost'),
            port: formData.get('smtpPort') || '587',
            user: formData.get('smtpUser'),
            password: formData.get('smtpPassword'),
            from: formData.get('smtpFrom')
        };
    }
    
    // JWT
    config.jwt = {
        key: formData.get('jwtKey'),
        issuer: formData.get('jwtIssuer') || 'ElectroStoreAPI',
        audience: formData.get('jwtAudience') || 'ElectroStoreClient',
        expireDays: formData.get('jwtExpireDays') || '7'
    };
    
    // URLs
    config.apiUrl = formData.get('apiUrl');
    config.frontUrl = formData.get('frontUrl');
    
    // CORS: Ajouter automatiquement l'URL du frontend
    const additionalOrigins = formData.get('allowedOrigins')?.split('\n').filter(o => o.trim()) || [];
    config.allowedOrigins = [config.frontUrl, ...additionalOrigins].filter(o => o);
    
    // Extraire les domaines pour Traefik depuis les URLs
    if (config.useTraefik) {
        try {
            const apiUrlObj = new URL(config.apiUrl || 'http://api.electrostore.local');
            const frontUrlObj = new URL(config.frontUrl || 'http://electrostore.local');
            config.traefikApiDomain = apiUrlObj.hostname;
            config.traefikFrontDomain = frontUrlObj.hostname;
        } catch (e) {
            // Fallback si les URLs ne sont pas valides
            config.traefikApiDomain = 'api.electrostore.local';
            config.traefikFrontDomain = 'electrostore.local';
        }
    } else {
        // Extraire les ports des URLs si pas de Traefik
        try {
            const apiUrlObj = new URL(config.apiUrl || 'http://localhost:5000');
            const frontUrlObj = new URL(config.frontUrl || 'http://localhost:8080');
            config.apiPort = apiUrlObj.port || '5000';
            config.frontendPort = frontUrlObj.port || '8080';
            config.iaPort = formData.get('iaPort') || '8001';
        } catch (e) {
            config.apiPort = '5000';
            config.frontendPort = '8080';
            config.iaPort = '8001';
        }
    }
    
    // OAuth Providers
    for (let i = 1; i <= oauthProvidersCount; i++) {
        const providerId = `oauth-${i}`;
        const displayName = formData.get(`${providerId}_displayName`);
        
        if (displayName) {
            config.oauthProviders.push({
                displayName,
                clientId: formData.get(`${providerId}_clientId`),
                clientSecret: formData.get(`${providerId}_clientSecret`),
                authority: formData.get(`${providerId}_authority`),
                redirectUri: formData.get(`${providerId}_redirectUri`),
                scope: formData.get(`${providerId}_scope`),
                iconUrl: formData.get(`${providerId}_iconUrl`),
                groupMapping: {
                    user: formData.get(`${providerId}_groupUser`),
                    moderator: formData.get(`${providerId}_groupModerator`),
                    admin: formData.get(`${providerId}_groupAdmin`)
                }
            });
        }
    }
    
    return config;
}

// Générer une règle Traefik complète à partir d'une URL
function generateTraefikRule(url) {
    try {
        const urlObj = new URL(url);
        let rules = [];
        
        // Toujours ajouter le Host
        rules.push(`Host(\`${urlObj.hostname}\`)`);
        
        // Ajouter le port si différent de 80/443
        const port = urlObj.port;
        if (port && port !== '80' && port !== '443') {
            rules.push(`ClientIP(\`0.0.0.0/0\`)`);
        }
        
        // Ajouter le PathPrefix si un chemin est spécifié
        const path = urlObj.pathname;
        if (path && path !== '/' && path !== '') {
            rules.push(`PathPrefix(\`${path}\`)`);
        }
        
        return rules.join(' && ');
    } catch (e) {
        return `Host(\`localhost\`)`;
    }
}

// Génération du fichier docker-compose.yml
function generateDockerCompose(config) {
    let compose = `version: '3.8'

services:`;

    // API Backend
    compose += `
  api:
    build:
      context: ./electrostoreAPI
      dockerfile: Dockerfile
    container_name: electrostore-api`;
    
    if (config.useTraefik) {
        const apiRule = generateTraefikRule(config.apiUrl);
        const entrypoint = config.traefik.tlsEnable ? config.traefik.tlsEntrypoint : config.traefik.entrypoint;
        
        compose += `
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.api.rule=${apiRule}"
      - "traefik.http.routers.api.entrypoints=${entrypoint}"
      - "traefik.http.services.api.loadbalancer.server.port=80"`;
      
        // Middlewares optionnels
        if (config.traefik.middlewares) {
            compose += `
      - "traefik.http.routers.api.middlewares=${config.traefik.middlewares}"`;
        }
        
        // TLS si activé
        if (config.traefik.tlsEnable) {
            compose += `
      - "traefik.http.routers.api.tls.certresolver=${config.traefik.certResolver}"`;
        }
    } else {
        compose += `
    ports:
      - "${config.apiPort}:80"`;
    }
    
    compose += `
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:`;
    
    if (config.useMariaDB) compose += `\n      - mariadb`;
    if (config.useMQTT) compose += `\n      - mqtt`;
    if (config.enableS3 && config.useS3) compose += `\n      - garage`;
    
    compose += `
    volumes:
      - ./electrostoreAPI/config/appsettings.json:/app/config/appsettings.json:ro`;
    
    if (!config.enableS3) {
        compose += `\n      - api-wwwroot:/app/wwwroot`;
    }
    
    compose += `
    networks:
      - electrostore-network`;
    
    if (config.useTraefik) {
        compose += `
      - traefik`;
    }
    
    compose += `
    restart: unless-stopped
`;

    // Frontend
    compose += `
  frontend:
    build:
      context: ./electrostoreFRONT
      dockerfile: Dockerfile
      args:
        - VITE_API_URL=${config.apiUrl || 'http://localhost:5000'}
    container_name: electrostore-frontend`;
    
    if (config.useTraefik) {
        const frontendRule = generateTraefikRule(config.frontUrl);
        const entrypoint = config.traefik.tlsEnable ? config.traefik.tlsEntrypoint : config.traefik.entrypoint;
        
        compose += `
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.frontend.rule=${frontendRule}"
      - "traefik.http.routers.frontend.entrypoints=${entrypoint}"
      - "traefik.http.services.frontend.loadbalancer.server.port=80"`;
      
        // Middlewares optionnels
        if (config.traefik.middlewares) {
            compose += `
      - "traefik.http.routers.frontend.middlewares=${config.traefik.middlewares}"`;
        }
        
        // TLS si activé
        if (config.traefik.tlsEnable) {
            compose += `
      - "traefik.http.routers.frontend.tls=true"
      - "traefik.http.routers.frontend.tls.certresolver=${config.traefik.certResolver}"`;
        }
    } else {
        compose += `
    ports:
      - "${config.frontendPort}:80"`;
    }
    
    compose += `
    depends_on:
      - api
    networks:
      - electrostore-network`;
    
    if (config.useTraefik) {
        compose += `
      - traefik`;
    }
    
    compose += `
    restart: unless-stopped
`;

    // Service IA
    compose += `
  ia:
    build:
      context: ./electrostoreIA
      dockerfile: Dockerfile
    container_name: electrostore-ia`;
    
    if (!config.useTraefik) {
        compose += `
    ports:
      - "${config.iaPort}:8000"`;
    }
    
    compose += `
    depends_on:`;
    
    if (config.useMariaDB) compose += `\n      - mariadb`;
    if (config.enableS3 && config.useS3) compose += `\n      - garage`;
    
    compose += `
    volumes:
      - ia-models:/app/models
    networks:
      - electrostore-network`;
    
    if (config.useTraefik) {
        compose += `
      - traefik`;
    }
    
    compose += `
    restart: unless-stopped
`;

    // MariaDB si activé
    if (config.useMariaDB) {
        compose += `
  mariadb:
    image: mariadb:11
    container_name: electrostore-mariadb
    environment:
      - MYSQL_ROOT_PASSWORD=${config.mariadb.rootPassword}
      - MYSQL_DATABASE=${config.mariadb.database}
      - MYSQL_USER=${config.mariadb.user}
      - MYSQL_PASSWORD=${config.mariadb.password}
    volumes:
      - mariadb-data:/var/lib/mysql
    networks:
      - electrostore-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "healthcheck.sh", "--connect", "--innodb_initialized"]
      interval: 10s
      timeout: 5s
      retries: 5
`;
    }

    // MQTT si activé
    if (config.useMQTT) {
        compose += `
  mqtt:
    image: eclipse-mosquitto:2
    container_name: electrostore-mqtt
    ports:
      - "1883:1883"
      - "9001:9001"
    volumes:
      - ./MQTTCONF:/mosquitto/config
      - mqtt-data:/mosquitto/data
    networks:
      - electrostore-network
    restart: unless-stopped
`;
    }

    // S3 Garage si activé
    if (config.enableS3 && config.useS3) {
        compose += `
  garage:
    image: dxflrs/garage:v0.9
    container_name: electrostore-garage
    ports:
      - "3900:3900"
      - "3901:3901"
      - "3902:3902"
    environment:
      - GARAGE_RPC_SECRET=${config.s3.secretKey}
    volumes:
      - garage-data:/data
      - garage-meta:/meta
    networks:
      - electrostore-network
    restart: unless-stopped
`;
    }

    // Networks et Volumes
    compose += `
networks:
  electrostore-network:
    driver: bridge`;
    
    if (config.useTraefik) {
        compose += `
  traefik:
    external: true`;
    }

    compose += `

volumes:
  ia-models:`;
    
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

    // ConnectionStrings
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

    // MQTT
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

    // S3
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

    // SMTP
    if (config.enableSMTP && config.smtp) {
        settings.SMTP = {
            "Host": config.smtp.host,
            "Port": parseInt(config.smtp.port),
            "Username": config.smtp.user,
            "Password": config.smtp.password,
            "From": config.smtp.from
        };
    };

    // JWT
    settings.JWT = {
        "Key": config.jwt.key,
        "Issuer": config.jwt.issuer,
        "Audience": config.jwt.audience,
        "ExpireDays": parseInt(config.jwt.expireDays)
    };

    // OAuth
    if (config.oauthProviders.length > 0) {
        settings.OAuth = {
            "Providers": config.oauthProviders.map(provider => ({
                "DisplayName": provider.displayName,
                "ClientId": provider.clientId,
                "ClientSecret": provider.clientSecret,
                "Authority": provider.authority,
                "RedirectUri": provider.redirectUri,
                "Scope": provider.scope,
                "IconUrl": provider.iconUrl,
                "GroupMapping": {
                    "User": provider.groupMapping.user,
                    "Moderator": provider.groupMapping.moderator,
                    "Admin": provider.groupMapping.admin
                }
            }))
        };
    }

    // URLs
    settings.Urls = {
        "API": config.apiUrl,
        "Frontend": config.frontUrl
    };

    // CORS
    settings.AllowedOrigins = config.allowedOrigins;

    return JSON.stringify(settings, null, 2);
}

// Génération du .env
function generateEnvFile(config) {
    let env = `# Généré le ${new Date().toLocaleDateString('fr-FR')}\n\n`;
    
    env += `# Configuration générale\n`;
    env += `PROJECT_NAME=electrostore\n`;
    env += `ENVIRONMENT=Production\n\n`;
    
    if (!config.useTraefik) {
        env += `# Ports\n`;
        env += `API_PORT=${config.apiPort}\n`;
        env += `FRONTEND_PORT=${config.frontendPort}\n`;
        env += `IA_PORT=${config.iaPort}\n\n`;
    } else {
        env += `# Traefik Domains\n`;
        env += `API_DOMAIN=${config.traefikApiDomain}\n`;
        env += `FRONTEND_DOMAIN=${config.traefikFrontDomain}\n\n`;
    }
    
    if (config.useMariaDB) {
        env += `# MariaDB\n`;
        env += `MARIADB_DATABASE=${config.mariadb.database}\n`;
        env += `MARIADB_USER=${config.mariadb.user}\n`;
        env += `MARIADB_PASSWORD=${config.mariadb.password}\n`;
        env += `MARIADB_ROOT_PASSWORD=${config.mariadb.rootPassword}\n\n`;
    }
    
    if (config.useMQTT) {
        env += `# MQTT\n`;
        env += `MQTT_USER=${config.mqtt.user}\n`;
        env += `MQTT_PASSWORD=${config.mqtt.password}\n\n`;
    }
    
    if (config.useS3) {
        env += `# S3 Garage\n`;
        env += `S3_ACCESS_KEY=${config.s3.accessKey}\n`;
        env += `S3_SECRET_KEY=${config.s3.secretKey}\n`;
        env += `S3_BUCKET=${config.s3.bucket}\n`;
        env += `S3_REGION=${config.s3.region}\n\n`;
    }
    
    return env;
}

// Copier dans le presse-papiers
function copyToClipboard(elementId) {
    const text = document.getElementById(elementId).textContent;
    navigator.clipboard.writeText(text).then(() => {
        const button = event.target;
        const originalText = button.textContent;
        button.textContent = '✅ Copié !';
        button.style.background = '#059669';
        
        setTimeout(() => {
            button.textContent = originalText;
            button.style.background = '';
        }, 2000);
    });
}

// Télécharger le fichier
function downloadFile(filename, elementId) {
    const text = document.getElementById(elementId).textContent;
    const blob = new Blob([text], { type: 'text/plain' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
}

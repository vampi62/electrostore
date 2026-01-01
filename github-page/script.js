// Variables globales
let oauthProvidersCount = 0;

// Initialisation au chargement
document.addEventListener('DOMContentLoaded', function() {
    initializeForm();
    loadGitHubTags();
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

// Charger les tags depuis GitHub
async function loadGitHubTags() {
    try {
        const response = await fetch('https://api.github.com/repos/vampi62/electrostore/tags');
        if (!response.ok) {
            console.warn('Impossible de récupérer les tags GitHub');
            return;
        }
        
        const tags = await response.json();
        
        // Créer les options pour le sélecteur
        const options = tags.map(tag => 
            `<option value="${tag.name}">${tag.name}</option>`
        ).join('');
        
        // Ajouter les options au sélecteur de version
        const select = document.getElementById('appVersion');
        if (select) {
            // Conserver l'option "latest" et ajouter les tags
            select.innerHTML = '<option value="latest">latest (développement)</option>' + options;
        }
    } catch (error) {
        console.error('Erreur lors du chargement des tags GitHub:', error);
    }
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

// générer un mot de passe aléatoire
function generateRandomPassword(length) {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?';
    let password = '';
    for (let i = 0; i < length; i++) {
        password += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return password;
}


// Génération des fichiers
function generateFiles() {
    const formData = new FormData(document.getElementById('configForm'));
    const config = collectConfig(formData);
    console.log('Configuration collectée :', config);
    
    const dockerCompose = generateDockerCompose(config);
    const appsettings = generateAppsettings(config);
    const envFile = generateEnvFile(config);
    const setupScript = generateSetupScript(config);
    
    document.getElementById('dockerCompose').textContent = dockerCompose;
    document.getElementById('appsettingsFile').textContent = appsettings;
    document.getElementById('envFile').textContent = envFile;
    document.getElementById('setupScript').textContent = setupScript;
    
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
        appVersion: formData.get('appVersion') || 'latest',
        
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
                bucket: formData.get('s3Bucket') || 'electrostore',
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
      - "\${API_PORT:-${config.apiPort}}:80"`;
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
      - ./config/appsettings.json:/app/config/appsettings.json:ro`;
    
    if (!config.enableS3) {
        compose += `\n      - api-wwwroot:/app/wwwroot`;
    }
    
    compose += `
    networks:
      - electrostore`;
    
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
    image: ghcr.io/vampi62/electrostore/front:\${FRONTEND_VERSION:-latest}
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
      - "\${FRONTEND_PORT:-${config.frontendPort}}:80"`;
    }
    
    compose += `
    depends_on:
      - api
    networks:
      - electrostore`;
    
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
    image: ghcr.io/vampi62/electrostore/ia:\${IA_VERSION:-latest}
    container_name: electrostore-ia`;
    
    if (!config.useTraefik) {
        compose += `
    ports:
      - "\${IA_PORT:-${config.iaPort}}:8000"`;
    }
    
    compose += `
    depends_on:`;
    
    if (config.useMariaDB) compose += `\n      - mariadb`;
    if (config.enableS3 && config.useS3) compose += `\n      - garage`;
    
    compose += `
    volumes:
      - ia-models:/app/models
    networks:
      - electrostore`;
    
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

    // MQTT si activé
    if (config.useMQTT) {
        compose += `
  mqtt:
    image: eclipse-mosquitto:\${MQTT_VERSION:-2.0.20}
    container_name: electrostore-mqtt
    ports:
      - "1883:1883"
      - "9001:9001"
    volumes:
      - ./MQTTCONF:/mosquitto/config:ro
      - mqtt-data:/mosquitto/data
    networks:
      - electrostore
    restart: unless-stopped
`;
    }

    // S3 Garage si activé
    if (config.enableS3 && config.useS3) {
        compose += `
  garage:
    image: dxflrs/garage:\${GARAGE_VERSION:-v0.9}
    container_name: electrostore-garage
    ports:
      - "3900:3900"
      - "3901:3901"
      - "3902:3902"
    environment:
      - GARAGE_RPC_SECRET=\${S3_SECRET_KEY:-changeme_s3_secret_key}
    volumes:
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
    settings.Jwt = {
        "Key": config.jwt.key,
        "Issuer": config.jwt.issuer,
        "Audience": config.jwt.audience,
        "ExpireDays": parseInt(config.jwt.expireDays)
    };

    // OAuth
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

    // URLs
    settings.FrontendUrl = config.frontUrl;

    // CORS
    settings.AllowedOrigins = config.allowedOrigins;

    return JSON.stringify(settings, null, 2);
}

// Génération du .env
function generateEnvFile(config) {
    let env = `# Fichier .env pour ElectroStore\n`;
    env += `# Généré le ${new Date().toLocaleDateString('fr-FR')}\n`;
    env += `# Ce fichier contient les variables d'environnement utilisées par docker-compose.yml\n`;
    env += `# IMPORTANT: Ne pas versionner ce fichier dans Git (ajouter à .gitignore)\n\n`;
    
    env += `# Configuration générale\n`;
    env += `PROJECT_NAME=electrostore\n`;
    env += `ENVIRONMENT=Production\n\n`;
    
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
        env += `GARAGE_VERSION=v0.9\n`;
    }
    env += `\n`;
    
    env += `# URLs\n`;
    env += `API_URL=${config.apiUrl || 'http://localhost:5000'}\n`;
    env += `FRONTEND_URL=${config.frontUrl || 'http://localhost:8080'}\n\n`;
    
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

    // Configuration S3 Garage si activé
    if (config.enableS3 && config.useS3) {
        script += `# Configuration S3 Garage
echo "Configuration de Garage S3..."

# Démarrer uniquement le service garage
docker-compose up -d garage

echo "Attente du démarrage de Garage (10 secondes)..."
sleep 10

# Configurer le cluster Garage
echo "Configuration du cluster Garage..."
docker exec electrostore-garage garage status || true
docker exec electrostore-garage garage layout assign -z dc1 -c 1 \$(docker exec electrostore-garage garage status | grep -oP '[0-9a-f]{16}' | head -n 1) || true

# Créer un utilisateur et obtenir les clés
echo "Création des clés d'accès S3..."
docker exec electrostore-garage garage key new electrostore-key > /tmp/garage_keys.txt
GARAGE_ACCESS_KEY=\$(grep 'Key ID' /tmp/garage_keys.txt | awk '{print \$3}')
GARAGE_SECRET_KEY=\$(grep 'Secret key' /tmp/garage_keys.txt | awk '{print \$3}')

echo "Access Key: \$GARAGE_ACCESS_KEY"
echo "Secret Key: \$GARAGE_SECRET_KEY"

# Créer le bucket
echo "Création du bucket ${config.s3.bucket}..."
docker exec electrostore-garage garage bucket create ${config.s3.bucket}

# Donner les permissions
echo "Attribution des permissions..."
docker exec electrostore-garage garage bucket allow --read --write ${config.s3.bucket} --key electrostore-key

# Mettre à jour le fichier .env avec les vraies clés
echo "Mise à jour du fichier .env..."
sed -i "s/S3_ACCESS_KEY=.*/S3_ACCESS_KEY=\$GARAGE_ACCESS_KEY/" .env
sed -i "s/S3_SECRET_KEY=.*/S3_SECRET_KEY=\$GARAGE_SECRET_KEY/" .env

rm /tmp/garage_keys.txt

echo "✅ Configuration Garage terminée"
echo ""

`;
    }

    // Configuration MQTT si activé
    if (config.useMQTT) {
        script += `# Configuration MQTT
echo "Configuration de Mosquitto MQTT..."

# Créer le dossier MQTTCONF s'il n'existe pas
mkdir -p MQTTCONF

# Créer le fichier mosquitto.conf
cat > MQTTCONF/mosquitto.conf << 'EOF'
listener 1883
persistence true
persistence_location /mosquitto/data/
password_file /mosquitto/config/mosquitto.passwd
allow_anonymous false
EOF

# Créer le fichier mosquitto.passwd avec le mot de passe en clair
cat > MQTTCONF/mosquitto.passwd << 'EOF'
${config.mqtt.user}:${config.mqtt.password}
EOF

echo "Fichiers MQTT créés dans MQTTCONF/"

# Démarrer MQTT pour hasher le mot de passe
echo "Démarrage de Mosquitto..."
docker-compose up -d mqtt

echo "Attente du démarrage de Mosquitto (5 secondes)..."
sleep 5

# Hasher le mot de passe
echo "Hashage du mot de passe..."
docker exec electrostore-mqtt mosquitto_passwd -U /mosquitto/config/mosquitto.passwd

# Copier le fichier hashé
docker cp electrostore-mqtt:/mosquitto/config/mosquitto.passwd MQTTCONF/mosquitto.passwd

# Redémarrer MQTT
echo "Redémarrage de Mosquitto avec le mot de passe hashé..."
docker-compose restart mqtt

echo "✅ Configuration MQTT terminée"
echo ""

`;
    }

    script += `# Démarrer tous les services
echo "Démarrage de tous les services..."
docker-compose up -d

echo ""
echo "====================================="
echo "✅ Configuration terminée !"
echo "====================================="
echo ""
echo "Accès à l'application :"
echo "${config.useTraefik ? 'Frontend: http://' + config.traefikFrontDomain : 'Frontend: http://localhost:' + config.frontendPort}"
echo "${config.useTraefik ? 'API: http://' + config.traefikApiDomain : 'API: http://localhost:' + config.apiPort}"
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

// Télécharger tous les fichiers dans un ZIP
async function downloadAllAsZip() {
    // Importer JSZip dynamiquement depuis CDN
    if (typeof JSZip === 'undefined') {
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/jszip/3.10.1/jszip.min.js';
        document.head.appendChild(script);
        
        await new Promise((resolve) => {
            script.onload = resolve;
        });
    }
    
    const zip = new JSZip();
    
    // Ajouter les fichiers principaux
    zip.file('docker-compose.yml', document.getElementById('dockerCompose').textContent);
    zip.file('.env', document.getElementById('envFile').textContent);
    zip.file('setup.sh', document.getElementById('setupScript').textContent);
    
    // Créer le dossier config et ajouter appsettings.json
    zip.folder('config');
    zip.file('config/appsettings.json', document.getElementById('appsettingsFile').textContent);
    
    // Ajouter un README
    const readme = `# ElectroStore - Configuration Docker

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
    zip.file('README.md', readme);
    
    // Générer le ZIP
    const content = await zip.generateAsync({ type: 'blob' });
    
    // Télécharger
    const url = window.URL.createObjectURL(content);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'electrostore-config.zip';
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
}

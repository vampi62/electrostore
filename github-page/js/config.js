// Variables globales
let oauthProvidersCount = 0;

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
        expireDays: formData.get('jwtExpireDays') || '1'
    };
    
    // URLs
    config.apiUrl = formData.get('apiUrl');
    config.frontUrl = formData.get('frontUrl');
    
    // CORS: Ajouter automatiquement l'URL du frontend
    const additionalOrigins = formData.get('allowedOrigins')?.split('\n').filter(o => o.trim()) || [];
    config.allowedOrigins = [config.frontUrl, ...additionalOrigins].filter(o => o);
    
    // Extraire les URLs complètes pour Traefik
    if (config.useTraefik) {
        try {
            config.apiUrlObj = new URL(config.apiUrl || 'http://api.electrostore.local');
            config.frontUrlObj = new URL(config.frontUrl || 'http://electrostore.local');
        } catch (e) {
            // Fallback si les URLs ne sont pas valides
            config.apiUrlObj = new URL('http://api.electrostore.local');
            config.frontUrlObj = new URL('http://electrostore.local');
        }
    } else {
        // Extraire les ports des URLs si pas de Traefik
        try {
            const apiUrlObj = new URL(config.apiUrl || 'http://localhost:5000');
            const frontUrlObj = new URL(config.frontUrl || 'http://localhost:8080');
            config.apiPort = apiUrlObj.port || '5000';
            config.frontendPort = frontUrlObj.port || '8080';
        } catch (e) {
            config.apiPort = '5000';
            config.frontendPort = '8080';
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

// Générer un mot de passe aléatoire
function generateRandomPassword(length) {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?';
    let password = '';
    for (let i = 0; i < length; i++) {
        password += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return password;
}

function returnUrlString(urlObj) {
    return urlObj.protocol + '//' + urlObj.hostname + (urlObj.port ? `:${urlObj.port}` : '') + urlObj.pathname;
}
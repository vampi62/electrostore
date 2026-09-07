// Global variables
let oauthProvidersCount = 0;

// Collect all form data
function collectConfig(formData) {
    const config = {
        // Integrated services
        useTraefik: document.getElementById('useTraefik').checked,
        useMariaDB: document.getElementById('useMariadb').checked,
        useMQTT: document.getElementById('useMqtt').checked,
        enableS3: document.getElementById('enableS3').checked,
        useS3: document.getElementById('useS3').checked,
        enableSMTP: document.getElementById('enableSMTP').checked,
        enableVapid: document.getElementById('enableVapid').checked,
        useVault: document.getElementById('useVault').checked,
        appVersion: formData.get('appVersion') || 'latest',
        
        // OAuth Providers
        oauthProviders: []
    };
    
    // Traefik configuration
    if (config.useTraefik) {
        config.traefik = {
            network: formData.get('traefikNetwork') || 'traefik',
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
            password: formData.get('mqttExternalPassword'),
            containerName: formData.get('mqttExternalContainerName') || 'mosquitto'
        };
    }
    
    // S3
    if (config.enableS3) {
        if (config.useS3) {
            config.s3 = {
                accessKey: 'GK' + generateRandomHexaKey(24),
                secretKey: generateRandomHexaKey(64),
                bucket: formData.get('s3Bucket') || 'electrostore-api',
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
            from: formData.get('smtpFrom'),
            enableSsl: document.getElementById('smtpEnableSsl') ? document.getElementById('smtpEnableSsl').checked : true
        };
    }

    // VAPID (Web Push)
    if (config.enableVapid) {
        config.vapid = {
            subject: formData.get('vapidSubject') || 'mailto:admin@electrostore.local',
            publicKey: formData.get('vapidPublicKey'),
            privateKey: formData.get('vapidPrivateKey')
        };
    }
    
    // Vault
    if (config.useVault) {
        config.vault = {
            addr: formData.get('vaultAddr') || 'http://vault:8200',
            token: formData.get('vaultToken'),
            path: formData.get('vaultPath') || 'electrostore',
            mountPoint: formData.get('vaultMountPoint') || 'secret',
            containerName: formData.get('vaultContainerName') || 'vault'
        };
    }

    // LLM chat assistant
    config.enableLlm = document.getElementById('enableLlm').checked;
    if (config.enableLlm) {
        config.llm = {
            baseUrl: formData.get('llmBaseUrl') || 'http://ollama:11434',
            model: formData.get('llmModel') || 'llama3.1',
            endpoint: formData.get('llmEndpoint') || 'api/chat',
            apiKey: formData.get('llmApiKey') || '',
            systemPrompt: formData.get('llmSystemPrompt') || DEFAULT_SYSTEM_PROMPT
        };
    }

    // Speech-to-text
    config.enableStt = document.getElementById('enableStt').checked;
    if (config.enableStt) {
        config.stt = {
            baseUrl: formData.get('sttBaseUrl') || 'http://whisper:9000',
            model: formData.get('sttModel') || 'whisper-1',
            apiKey: formData.get('sttApiKey') || ''
        };
    }

    // JWT
    config.jwt = {
        key: formData.get('jwtKey') || generateRandomPassword(64),
        issuer: formData.get('jwtIssuer') || 'ElectroStoreAPI',
        audience: formData.get('jwtAudience') || 'ElectroStoreClient',
        expireDays: formData.get('jwtExpireDays') || '1'
    };

    config.aes = {
        key: formData.get('aesKey') || generateRandomHexaKey(64)
    };
    
    // URLs
    config.apiUrl = formData.get('apiUrl');
    config.frontUrl = formData.get('frontUrl');
    
    // CORS: allowed origins
    const additionalOrigins = formData.get('allowedOrigins')?.split('\n').filter(o => o.trim()) || [];
    config.allowedOrigins = [config.frontUrl, ...additionalOrigins].filter(o => o);

    // Application language
    config.appLanguage = formData.get('appLanguage') || 'en';

    // File Validation
    config.fileValidation = {
        maxDocumentSizeMB: parseInt(document.getElementById('maxDocumentSizeMB').value) || 10,
        maxImageSizeMB: parseInt(document.getElementById('maxImageSizeMB').value) || 8,
        maxAudioSizeMB: parseInt(document.getElementById('maxAudioSizeMB').value) || 25,
        allowedImageMimeTypes: {},
        allowedDocumentMimeTypes: {},
        allowedAudioMimeTypes: {}
    };
    document.querySelectorAll('#allowedImageMimeTypes .mime-type-row').forEach(row => {
        const mime = row.querySelector('.mime-type-input').value.trim();
        let ext = row.querySelector('.mime-ext-input').value.trim();
        if (ext && !ext.startsWith('.')) ext = '.' + ext;
        if (mime && ext) config.fileValidation.allowedImageMimeTypes[mime] = ext;
    });
    document.querySelectorAll('#allowedDocumentMimeTypes .mime-type-row').forEach(row => {
        const mime = row.querySelector('.mime-type-input').value.trim();
        let ext = row.querySelector('.mime-ext-input').value.trim();
        if (ext && !ext.startsWith('.')) ext = '.' + ext;
        if (mime && ext) config.fileValidation.allowedDocumentMimeTypes[mime] = ext;
    });
    document.querySelectorAll('#allowedAudioMimeTypes .mime-type-row').forEach(row => {
        const mime = row.querySelector('.mime-type-input').value.trim();
        let ext = row.querySelector('.mime-ext-input').value.trim();
        if (ext && !ext.startsWith('.')) ext = '.' + ext;
        if (mime && ext) config.fileValidation.allowedAudioMimeTypes[mime] = ext;
    });
    
    if (config.useTraefik) {
        try {
            config.apiUrlObj = new URL(config.apiUrl || 'http://api.electrostore.local');
            config.frontUrlObj = new URL(config.frontUrl || 'http://electrostore.local');
        } catch (e) {
            config.apiUrlObj = new URL('http://api.electrostore.local');
            config.frontUrlObj = new URL('http://electrostore.local');
        }
    } else {
        try {
            config.apiUrlObj = new URL(config.apiUrl || 'http://localhost:5000');
            config.frontUrlObj = new URL(config.frontUrl || 'http://localhost:8080');
            config.apiPort = document.getElementById('apiPort')?.value || config.apiUrlObj.port || '5000';
            config.frontendPort = document.getElementById('frontendPort')?.value || config.frontUrlObj.port || '8080';
        } catch (e) {
            config.apiUrlObj = new URL('http://localhost:5000');
            config.frontUrlObj = new URL('http://localhost:8080');
            config.apiPort = document.getElementById('apiPort')?.value || '5000';
            config.frontendPort = document.getElementById('frontendPort')?.value || '8080';
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

// Generate random password
function generateRandomPassword(length) {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@&_+-';
    let password = '';
    for (let i = 0; i < length; i++) {
        password += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return password;
}

// Generate valid hexadecimal key
function generateRandomHexaKey(length) {
    const chars = '0123456789abcdef';
    let result = '';
    for (let i = 0; i < length; i++) {
        result += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return result;
}

// Generate complete Traefik rule from URL
function generateTraefikRule(urlObj) {
    try {
        let rules = [];
        
        rules.push(`Host(\`${urlObj.hostname}\`)`);
        
        const port = urlObj.port;
        if (port && port !== '80' && port !== '443') {
            rules.push(`ClientIP(\`0.0.0.0/0\`)`);
        }
        
        const path = urlObj.pathname;
        if (path && path !== '/' && path !== '') {
            rules.push(`PathPrefix(\`${path}\`)`);
        }
        
        return rules.join(' && ');
    } catch (e) {
        return `Host(\`localhost\`)`;
    }
}

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

// Génération de mot de passe aléatoire dans un champ
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

// Initialisation du formulaire
function initializeForm() {
    // Initialiser les sections visibles
    toggleSection('mariadb');
    toggleSection('mqtt');
}

// Génération des fichiers - Point d'entrée principal
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

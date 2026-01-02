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

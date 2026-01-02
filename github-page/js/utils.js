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
    zip.file('README.md', generateReadme());
    
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

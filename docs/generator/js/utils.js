// Copy to clipboard
function copyToClipboard(elementId) {
    const text = document.getElementById(elementId).textContent;
    navigator.clipboard.writeText(text).then(() => {
        const button = event.target;
        const originalText = button.textContent;
        button.textContent = 'Copied!';
        button.style.background = '#059669';
        
        setTimeout(() => {
            button.textContent = originalText;
            button.style.background = '';
        }, 2000);
    });
}

// Show/Hide password field
function togglePasswordVisibility(fieldId) {
    const field = document.getElementById(fieldId);
    const button = event.target;
    
    if (field.type === 'password') {
        field.type = 'text';
        button.textContent = 'Hide';
    } else {
        field.type = 'password';
        button.textContent = 'Show';
    }
}

// Convert string to camelCase
function toCamelCase(str) {
  return str.replace(/(?:^\w|[A-Z]|\b\w)/g, function(word, index) {
    return index === 0 ? word.toLowerCase() : word.toUpperCase();
  }).replace(/\s+/g, '');
}

// Convert string to snake_case
function toSnakeCase(str) {
    return str.replace(/\s+/g, '_').toLowerCase();
}

// Download file
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

// Download all files as ZIP
async function downloadAllAsZip() {
    // Dynamically import JSZip from CDN
    if (typeof JSZip === 'undefined') {
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/jszip/3.10.1/jszip.min.js';
        document.head.appendChild(script);
        
        await new Promise((resolve) => {
            script.onload = resolve;
        });
    }
    
    const zip = new JSZip();
    
    // Add main files
    zip.file('docker-compose.yml', document.getElementById('dockerCompose').textContent);
    zip.file('.env', document.getElementById('envFile').textContent);
    zip.file('setup.sh', document.getElementById('setupScript').textContent);
    
    // Create config folder and add appsettings.json
    zip.folder('config');
    zip.file('config/appsettings.json', document.getElementById('appsettingsFile').textContent);
    
    // Add garage.toml if present
    const garageConfigSection = document.getElementById('garageConfigSection');
    if (!garageConfigSection.classList.contains('hidden')) {
        zip.folder('config/garage');
        zip.file('config/garage/garage.toml', document.getElementById('garageConfigFile').textContent);
    }
    
    // Add MQTT files if present
    const mosquittoConfigSection = document.getElementById('mosquittoConfigSection');
    if (!mosquittoConfigSection.classList.contains('hidden')) {
        zip.folder('config/mosquitto');
        zip.file('config/mosquitto/mosquitto.conf', document.getElementById('mosquittoConfigFile').textContent);
        zip.file('config/mosquitto/mosquitto.passwd', document.getElementById('mosquittoPasswdFile').textContent);
    }
    
    // Add README
    zip.file('README.md', generateReadme());
    
    // Generate ZIP
    const content = await zip.generateAsync({ type: 'blob' });
    
    // Download
    const url = window.URL.createObjectURL(content);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'electrostore.zip';
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
}

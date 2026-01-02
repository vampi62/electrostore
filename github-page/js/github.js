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

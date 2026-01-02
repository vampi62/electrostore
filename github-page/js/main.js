// Point d'entrée principal de l'application
// Ce fichier initialise l'application au chargement de la page

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

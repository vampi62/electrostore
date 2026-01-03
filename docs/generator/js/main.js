// Main application entry point
// This file initializes the application on page load

// Initialization on load
document.addEventListener('DOMContentLoaded', function() {
    initializeForm();
    loadGitHubTags();
    document.getElementById('jwtKey').value = generateRandomPassword(64);
});

// Form handling
document.getElementById('configForm').addEventListener('submit', function(e) {
    e.preventDefault();
    generateFiles();
});

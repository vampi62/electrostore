// Load tags from GitHub
async function loadGitHubTags() {
    try {
        const response = await fetch('https://api.github.com/repos/vampi62/electrostore/tags');
        if (!response.ok) {
            console.warn('Unable to fetch GitHub tags');
            return;
        }
        
        const tags = await response.json();
        
        // Create options for selector
        const options = tags.map(tag => 
            `<option value="${tag.name}">${tag.name}</option>`
        ).join('');
        
        // Add options to version selector
        const select = document.getElementById('appVersion');
        if (select) {
            // Keep "latest" option and add tags
            select.innerHTML = '<option value="latest">latest (développement)</option>' + options;
        }
    } catch (error) {
        console.error('Error loading GitHub tags:', error);
    }
}

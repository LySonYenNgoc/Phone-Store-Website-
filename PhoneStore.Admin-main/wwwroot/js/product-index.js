// Product Index Filter and Search functionality
console.log('Loading product-index.js...');

document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing Product Index filters...');
    
    // Cache DOM elements
    const elements = {
        searchTerm: document.getElementById('searchTerm'),
        category: document.getElementById('category'),
        color: document.getElementById('color'),
        sortBy: document.getElementById('sortBy'),
        publishStatus: document.getElementById('publishStatus')
    };
    
    // Verify elements exist
    console.log('Elements found:', {
        searchTerm: !!elements.searchTerm,
        category: !!elements.category,
        color: !!elements.color,
        sortBy: !!elements.sortBy,
        publishStatus: !!elements.publishStatus
    });
    
    let typingTimer;
    const doneTypingInterval = 500;

    // Add event listeners for search input (with debounce)
    if (elements.searchTerm) {
        elements.searchTerm.addEventListener('input', function() {
            console.log('Search input changed:', this.value);
            clearTimeout(typingTimer);
            typingTimer = setTimeout(applyFilters, doneTypingInterval);
        });
    }

    // Add event listeners for dropdowns (immediate)
    ['category', 'color', 'sortBy', 'publishStatus'].forEach(key => {
        const element = elements[key];
        if (element) {
            element.addEventListener('change', function() {
                console.log(`${key} changed to:`, this.value);
                applyFilters();
            });
        }
    });

    console.log('Event listeners initialized successfully');
});

function applyFilters() {
    console.log('applyFilters() called');
    
    const searchTermValue = document.getElementById('searchTerm')?.value || '';
    const categoryValue = document.getElementById('category')?.value || '';
    const colorValue = document.getElementById('color')?.value || '';
    const sortByValue = document.getElementById('sortBy')?.value || '';
    const publishStatusValue = document.getElementById('publishStatus')?.value || '';

    console.log('Current filter values:', {
        searchTerm: searchTermValue,
        category: categoryValue,
        color: colorValue,
        sortBy: sortByValue,
        publishStatus: publishStatusValue
    });

    const url = `/Product/Index?searchTerm=${encodeURIComponent(searchTermValue)}&category=${encodeURIComponent(categoryValue)}&color=${encodeURIComponent(colorValue)}&sortBy=${encodeURIComponent(sortByValue)}&publishStatus=${encodeURIComponent(publishStatusValue)}`;
    console.log('Navigating to:', url);
    
    window.location.href = url;
}

// Backward compatibility
function filterProducts() {
    console.log('filterProducts() called (backward compatibility)');
    applyFilters();
}

function testManualFilter() {
    console.log('Manual test - navigating with hardcoded params');
    window.location.href = '/Product/Index?searchTerm=iPhone&sortBy=name';
}

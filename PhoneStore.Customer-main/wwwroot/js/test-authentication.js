// Test Authentication Flow for PhoneStore Customer
// Open browser console and run these tests

console.log('🔧 PhoneStore Customer Authentication Test Suite');

// Test 1: Check if authentication elements are present
function testAuthenticationElements() {
    console.log('\n📋 Test 1: Authentication Elements');

    // Check if user dropdown exists (when logged in)
    const userDropdown = document.querySelector('[data-user-dropdown]');
    const loginButton = document.querySelector('a[href*="Login"]');
    const registerButton = document.querySelector('a[href*="Register"]');

    if (userDropdown) {
        console.log('✅ User dropdown found - User is logged in');
        console.log('👤 Username:', userDropdown.textContent.trim());
    } else if (loginButton && registerButton) {
        console.log('✅ Login/Register buttons found - User is not logged in');
    } else {
        console.log('❌ Authentication elements not found');
    }
}

// Test 2: Check form validation on current page
function testFormValidation() {
    console.log('\n📋 Test 2: Form Validation');

    const forms = document.querySelectorAll('form');
    if (forms.length === 0) {
        console.log('ℹ️ No forms found on current page');
        return;
    }

    forms.forEach((form, index) => {
        console.log(`📝 Form ${index + 1}:`, form.id || 'unnamed');

        // Check for required inputs
        const requiredInputs = form.querySelectorAll('input[required]');
        console.log(`  📊 Required fields: ${requiredInputs.length}`);

        // Check for validation functions
        const onsubmit = form.getAttribute('onsubmit');
        if (onsubmit) {
            console.log(`  ✅ Validation function: ${onsubmit}`);
        }

        // Check for CSRF token
        const csrfToken = form.querySelector('input[name="__RequestVerificationToken"]');
        if (csrfToken) {
            console.log('  🔒 CSRF protection: ✅');
        } else {
            console.log('  ⚠️ CSRF protection: Missing');
        }
    });
}

// Test 3: Check responsive design
function testResponsiveDesign() {
    console.log('\n📋 Test 3: Responsive Design');

    const viewport = {
        width: window.innerWidth,
        height: window.innerHeight
    };

    console.log(`📱 Viewport: ${viewport.width}x${viewport.height}`);

    // Check for mobile menu
    const mobileMenu = document.querySelector('[data-mobile-menu]');
    if (viewport.width < 768) {
        if (mobileMenu) {
            console.log('✅ Mobile menu found for small screen');
        } else {
            console.log('⚠️ Mobile menu not found for small screen');
        }
    } else {
        console.log('💻 Desktop view detected');
    }

    // Check for responsive classes
    const responsiveElements = document.querySelectorAll('[class*="sm:"], [class*="md:"], [class*="lg:"]');
    console.log(`📊 Responsive elements: ${responsiveElements.length}`);
}

// Test 4: Check JavaScript functionality
function testJavaScriptFunctionality() {
    console.log('\n📋 Test 4: JavaScript Functionality');

    // Check for password toggle
    const passwordToggles = document.querySelectorAll('[onclick*="togglePassword"]');
    if (passwordToggles.length > 0) {
        console.log(`✅ Password toggles: ${passwordToggles.length} found`);
    }

    // Check for validation functions
    const validationFunctions = [];
    if (typeof validateForm === 'function') validationFunctions.push('validateForm');
    if (typeof validateResetForm === 'function') validationFunctions.push('validateResetForm');
    if (typeof validateLoginForm === 'function') validationFunctions.push('validateLoginForm');

    if (validationFunctions.length > 0) {
        console.log(`✅ Validation functions: ${validationFunctions.join(', ')}`);
    } else {
        console.log('ℹ️ No validation functions found on current page');
    }
}

// Test 5: Security checks
function testSecurity() {
    console.log('\n📋 Test 5: Security Checks');

    // Check for HTTPS (in production)
    const isHTTPS = location.protocol === 'https:';
    console.log(`🔒 HTTPS: ${isHTTPS ? '✅' : '⚠️ (Use HTTPS in production)'}`);

    // Check for secure cookies (only visible server-side)
    const cookies = document.cookie;
    console.log(`🍪 Client cookies: ${cookies ? 'Present' : 'None visible'}`);

    // Check for XSS protection
    const metaCSP = document.querySelector('meta[http-equiv="Content-Security-Policy"]');
    if (metaCSP) {
        console.log('🛡️ CSP header: ✅');
    }
}

// Test 6: Navigation tests
function testNavigation() {
    console.log('\n📋 Test 6: Navigation Tests');

    const authLinks = {
        login: document.querySelector('a[href*="Login"]'),
        register: document.querySelector('a[href*="Register"]'),
        profile: document.querySelector('a[href*="Profile"]'),
        orders: document.querySelector('a[href*="Orders"]'),
        logout: document.querySelector('a[href*="Logout"]')
    };

    Object.entries(authLinks).forEach(([name, element]) => {
        if (element) {
            console.log(`✅ ${name.charAt(0).toUpperCase() + name.slice(1)} link: Found`);
        } else {
            console.log(`ℹ️ ${name.charAt(0).toUpperCase() + name.slice(1)} link: Not available`);
        }
    });
}

// Run all tests
function runAllTests() {
    console.log('🚀 Starting PhoneStore Authentication Tests...\n');

    testAuthenticationElements();
    testFormValidation();
    testResponsiveDesign();
    testJavaScriptFunctionality();
    testSecurity();
    testNavigation();

    console.log('\n✅ Test suite completed!');
    console.log('💡 To test specific functionality:');
    console.log('   - Try registering a new account');
    console.log('   - Test login/logout flow');
    console.log('   - Check password reset');
    console.log('   - Test responsive design by resizing window');
}

// Auto-run tests
runAllTests();

// Export functions for manual testing
window.PhoneStoreTests = {
    runAllTests,
    testAuthenticationElements,
    testFormValidation,
    testResponsiveDesign,
    testJavaScriptFunctionality,
    testSecurity,
    testNavigation
};

console.log('\n🎯 Manual testing available via window.PhoneStoreTests object');

// Access Denied Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Kiểm tra nếu không có history thì ẩn nút back
    if (window.history.length <= 1) {
        const backButton = document.querySelector('button[onclick="window.history.back()"]');
        if (backButton) {
            backButton.style.display = 'none';
        }
    }

    // Thêm hiệu ứng hover cho icon
    const icon = document.querySelector('.access-denied-icon');
    if (icon) {
        icon.addEventListener('mouseenter', function() {
            this.style.transform = 'scale(1.1)';
        });

        icon.addEventListener('mouseleave', function() {
            this.style.transform = 'scale(1.0)';
        });
    }

    // Auto focus vào nút back sau 500ms
    setTimeout(function() {
        const backButton = document.querySelector('button[onclick="window.history.back()"]');
        if (backButton && backButton.style.display !== 'none') {
            backButton.focus();
        }
    }, 500);

    // Thêm keyboard navigation
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            // ESC key - quay lại trang trước
            if (window.history.length > 1) {
                window.history.back();
            }
        } else if (e.key === 'Enter' && e.ctrlKey) {
            // Ctrl + Enter - về dashboard
            const dashboardLink = document.querySelector('a[href*="Dashboard"]');
            if (dashboardLink) {
                dashboardLink.click();
            }
        }
    });

    // Console log cho debugging
    console.log('Access Denied page loaded successfully');
    console.log('History length:', window.history.length);
});

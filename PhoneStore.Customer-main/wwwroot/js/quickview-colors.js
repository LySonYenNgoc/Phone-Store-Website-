// QuickView Color Functions
function updateQuickViewColors(colors, images) {
    const colorSection = document.getElementById('quickViewColorSection');
    const colorsContainer = document.getElementById('quickViewColors');

    if (!colorSection || !colorsContainer) return;

    if (colors && colors.length > 0) {
        colorSection.classList.remove('hidden');
        colorsContainer.innerHTML = '';

        // Store images for color filtering
        window.quickViewImageData = {
            allImages: images,
            currentImages: images
        };

        colors.forEach((color, index) => {
            const colorButton = document.createElement('button');
            colorButton.type = 'button';
            colorButton.className = `color-option w-8 h-8 rounded-full border-2 transition-all hover:scale-110 ${index === 0 ? 'border-gray-900 ring-2 ring-gray-900 ring-offset-2' : 'border-gray-300'}`;
            colorButton.style.backgroundColor = color.hexCode;
            colorButton.title = color.name;
            colorButton.dataset.colorId = color.id;
            colorButton.dataset.colorName = color.name;

            colorButton.onclick = () => selectQuickViewColor(color.id, colorButton);
            colorsContainer.appendChild(colorButton);
        });
    } else {
        colorSection.classList.add('hidden');
    }
}

function selectQuickViewColor(colorId, buttonElement) {
    // Update active state
    const allColorButtons = document.querySelectorAll('#quickViewColors .color-option');
    allColorButtons.forEach(btn => {
        btn.classList.remove('border-gray-900', 'ring-2', 'ring-gray-900', 'ring-offset-2');
        btn.classList.add('border-gray-300');
    });

    buttonElement.classList.remove('border-gray-300');
    buttonElement.classList.add('border-gray-900', 'ring-2', 'ring-gray-900', 'ring-offset-2');

    // Filter images by color
    if (window.quickViewImageData) {
        const colorImages = window.quickViewImageData.allImages.filter(img => 
            img.colorId === colorId || img.colorId === parseInt(colorId)
        );
        
        // If no images for this color, show all images
        const imagesToShow = colorImages.length > 0 ? colorImages : window.quickViewImageData.allImages;
        
        // Update main image and thumbnails
        const mainImageEl = document.getElementById('quickViewMainImage');
        const thumbnailsEl = document.getElementById('quickViewThumbnails');
        
        if (imagesToShow.length > 0 && mainImageEl) {
            const firstImageUrl = imagesToShow[0].imageUrl || imagesToShow[0];
            mainImageEl.src = firstImageUrl;
            mainImageEl.onerror = function() {
                this.src = 'https://via.placeholder.com/400x400/EEE/999?text=No+Image';
            };
        }
        
        // Update thumbnails
        if (thumbnailsEl) {
            thumbnailsEl.innerHTML = '';
            imagesToShow.forEach((imageObj, index) => {
                const thumb = document.createElement('div');
                thumb.className = `quick-view-thumbnail w-16 h-16 bg-gray-100 rounded-lg overflow-hidden cursor-pointer border-2 ${index === 0 ? 'border-primary-500 active' : 'border-transparent'} hover:border-primary-300 transition-all`;

                const img = document.createElement('img');
                const thumbImageUrl = imageObj.imageUrl || imageObj;
                img.src = thumbImageUrl;
                img.alt = buttonElement.dataset.colorName || 'Product Image';
                img.className = 'w-full h-full object-cover';
                img.onerror = function() {
                    this.src = 'https://via.placeholder.com/100x100/EEE/999?text=No+Image';
                };

                thumb.appendChild(img);
                thumb.onclick = () => changeQuickViewImage(thumbImageUrl, thumb);
                thumbnailsEl.appendChild(thumb);
            });
        }
    }
}

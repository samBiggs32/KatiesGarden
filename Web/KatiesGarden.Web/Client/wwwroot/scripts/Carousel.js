
// Initialize a single carousel
window.initializeCarousel = function (id) {
    try {
        console.log(`Initializing carousel: ${id}`);
        const carouselElement = document.getElementById(id);

        if (!carouselElement) {
            console.warn(`Carousel element with ID "${id}" not found in the DOM`);
            return false;
        }

        // For Bootstrap 4
        $(carouselElement).carousel({
            interval: 5000,
            pause: 'hover',
            wrap: true
        });

        console.log(`Carousel ${id} initialized successfully`);
        return true;
    } catch (error) {
        console.error(`Error initializing carousel ${id}:`, error);
        return false;
    }
};
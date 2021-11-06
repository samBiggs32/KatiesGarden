const citymap = {
    Milverton: {
        center: { lat: 51.0233371428786, lng: - 3.2553684238706695 },
        size: 1438,
    },    
};

function initMap() {
    const map = new google.maps.Map(document.getElementById("map"), {
        zoom: 14,
        center: { lat: 51.023371428786, lng: - 3.2553684238706695},        
        mapTypeId: "roadmap",
    });

    for (const city in citymap) {
        // Add the circle for this city to the map.
        const cityCircle = new google.maps.Circle({
            strokeColor: "#4287f5",
            strokeOpacity: 0.8,
            strokeWeight: 2,
            fillColor: "#4287f5",
            fillOpacity: 0.35,
            map,
            center: citymap[city].center,
            radius: Math.sqrt(citymap[city].size) * 30,
        });
    }
}

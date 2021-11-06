var map;

function initMap() {
    map = new google.maps.Map(document.getElementById("map"), {
        zoom: 15,
        center: { lat: 51.0233371428786, lng: - 3.2553684238706695},        
        mapTypeId: "terrain",
    });  
}

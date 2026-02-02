var mapInstance = null;
var markersLayer = null;

window.leafletMap = {
    initialize: function (elementId, stores) {
        // If map is already initialized, just update data or return
        if (mapInstance) {
            this.updateMarkers(stores);
            return;
        }

        var mapContainer = document.getElementById(elementId);
        if (!mapContainer) return;

        // Initialize map centered on a default location (e.g., Philippines mostly central)
        // Adjust center and zoom based on your actual data or preference
        mapInstance = L.map(elementId).setView([12.8797, 121.7740], 6);

        // Add OpenStreetMap tiles
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap'
        }).addTo(mapInstance);

        markersLayer = L.layerGroup().addTo(mapInstance);

        this.updateMarkers(stores);

        // Force a resize calculation after a short delay to ensure correct rendering if container was hidden
        setTimeout(() => {
            mapInstance.invalidateSize();
        }, 100);
    },

    updateMarkers: function (stores) {
        if (!mapInstance || !markersLayer) return;

        markersLayer.clearLayers();

        var bounds = L.latLngBounds();
        var hasValidMarkers = false;

        stores.forEach(store => {
            if (store.gpsLatitude && store.gpsLongitude) {
                var color = store.storeHeadId ? '#4caf50' : '#f44336'; // Green for assigned, Red for unassigned
                var fillColor = store.storeHeadId ? '#4caf50' : '#f44336';

                var marker = L.circleMarker([store.gpsLatitude, store.gpsLongitude], {
                    radius: 8,
                    fillColor: fillColor,
                    color: "#fff",
                    weight: 1,
                    opacity: 1,
                    fillOpacity: 0.8
                });

                var popupContent = `
                    <div style="font-family: sans-serif;">
                        <h4 style="margin: 0 0 5px 0;">${store.storeName}</h4>
                        <div style="font-size: 0.9em; margin-bottom: 5px;"><strong>Code:</strong> ${store.storeCode}</div>
                        <div style="font-size: 0.9em; margin-bottom: 5px;"><strong>Status:</strong> ${store.status}</div>
                        <div style="font-size: 0.9em;">
                            <strong>Head:</strong> ${store.storeHeadName || '<span style="color: red;">Unassigned</span>'}
                        </div>
                    </div>
                `;

                marker.bindPopup(popupContent);
                markersLayer.addLayer(marker);

                bounds.extend(marker.getLatLng());
                hasValidMarkers = true;
            }
        });

        // Fit bounds if we have markers
        if (hasValidMarkers) {
            mapInstance.fitBounds(bounds, { padding: [50, 50] });
        }
    },

    destroy: function () {
        if (mapInstance) {
            mapInstance.remove();
            mapInstance = null;
            markersLayer = null;
        }
    }
};

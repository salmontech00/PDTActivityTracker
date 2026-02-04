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

                // Create a custom SVG pin icon
                var iconHtml = `
                    <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <path d="M12 21C16.5 17.5 19 14.1667 19 11C19 7.13401 15.866 4 12 4C8.13401 4 5 7.13401 5 11C5 14.1667 7.5 17.5 12 21Z" fill="${color}" stroke="#fff" stroke-width="1.5"/>
                        <circle cx="12" cy="11" r="2.5" fill="#fff"/>
                    </svg>
                `;

                var customIcon = L.divIcon({
                    html: iconHtml,
                    className: 'custom-pin-icon',
                    iconSize: [32, 32],
                    iconAnchor: [16, 32],
                    popupAnchor: [0, -32]
                });

                var marker = L.marker([store.gpsLatitude, store.gpsLongitude], {
                    icon: customIcon
                });

                var popupContent = `
                    <div style="font-family: sans-serif; min-width: 150px;">
                        <h4 style="margin: 0 0 5px 0;">${store.storeName}</h4>
                        <div style="font-size: 0.9em; margin-bottom: 5px;"><strong>Code:</strong> ${store.storeCode}</div>
                        <div style="font-size: 0.9em; margin-bottom: 5px;"><strong>Status:</strong> ${store.status}</div>
                        <div style="font-size: 0.9em; margin-bottom: 5px;">
                            <strong>Store Head:</strong> ${store.storeHeadName || '<span style="color: red;">Unassigned</span>'}
                        </div>
                        <div style="font-size: 0.9em;">
                            <strong>Area Head:</strong> ${store.areaHeadName || '<span style="color: red;">Unassigned</span>'}
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

    focusMarker: function (lat, lng, zoom) {
        if (!mapInstance) return;
        mapInstance.setView([lat, lng], zoom || 15);

        if (markersLayer) {
            markersLayer.eachLayer(function (layer) {
                if (layer instanceof L.Marker) {
                    var pos = layer.getLatLng();
                    if (pos.lat === lat && pos.lng === lng) {
                        layer.openPopup();
                    }
                }
            });
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

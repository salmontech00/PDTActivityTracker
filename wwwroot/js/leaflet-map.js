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
                // Determine Mode based on data properties
                // Mode A: Store Head Dashboard (Explicit 'isMyStore' boolean present)
                // Mode B: Admin Assignment (Original logic checking assignments)

                var isStoreHeadMode = typeof store.isMyStore === 'boolean';
                var isStoreHeadMode = typeof store.isMyStore === 'boolean';
                var color, opacity, zIndex, pulseHtml, statusBadge;
                var clickAction = null;

                if (isStoreHeadMode) {
                    // --- STORE HEAD MODE ---
                    var isMyStore = store.isMyStore;
                    color = isMyStore ? '#4caf50' : '#9e9e9e'; // Green vs Gray
                    opacity = isMyStore ? 1 : 0.6;
                    zIndex = isMyStore ? 1000 : 1;
                    pulseHtml = isMyStore ? '<div class="pin-pulse"></div>' : '';
                    statusBadge = `<span style="background: ${isMyStore ? '#e8f5e9' : '#f5f5f5'}; color: ${isMyStore ? '#2e7d32' : '#9e9e9e'}; padding: 2px 6px; border-radius: 4px; font-size: 0.8em; font-weight: bold;">${isMyStore ? 'My Store' : 'No Access'}</span>`;
                    clickAction = isMyStore ? 'dashboard' : null;
                } else {
                    // --- ADMIN MODE (Legacy/Default) ---
                    // Red = Unassigned, Green = Assigned
                    var isAssigned = store.storeHeadId != null;
                    color = isAssigned ? '#4caf50' : '#f44336';
                    opacity = 1;
                    zIndex = 100;
                    pulseHtml = ''; // No pulse for admin view

                    var storeHeadName = store.storeHeadName || '<span style="color: red;">Unassigned</span>';
                    var areaHeadName = store.areaHeadName || '<span style="color: red;">Unassigned</span>';

                    statusBadge = `
                        <div style="font-size: 0.9em; margin-bottom: 5px;"><strong>Status:</strong> ${store.status}</div>
                        <div style="font-size: 0.9em; margin-bottom: 5px;"><strong>Store Head:</strong> ${storeHeadName}</div>
                        <div style="font-size: 0.9em;"><strong>Area Head:</strong> ${areaHeadName}</div>
                    `;
                    clickAction = 'popup_only';
                }

                // Render SVG Pin
                var iconHtml = `
                    <div style="position: relative; width: 32px; height: 32px;">
                        ${pulseHtml}
                        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" style="position: relative; z-index: 2;">
                            <path d="M12 21C16.5 17.5 19 14.1667 19 11C19 7.13401 15.866 4 12 4C8.13401 4 5 7.13401 5 11C5 14.1667 7.5 17.5 12 21Z" fill="${color}" stroke="#fff" stroke-width="1.5"/>
                            <circle cx="12" cy="11" r="2.5" fill="#fff"/>
                        </svg>
                    </div>
                `;

                var customIcon = L.divIcon({
                    html: iconHtml,
                    className: 'custom-pin-icon',
                    iconSize: [32, 32],
                    iconAnchor: [16, 32],
                    popupAnchor: [0, -32]
                });

                var marker = L.marker([store.gpsLatitude, store.gpsLongitude], {
                    icon: customIcon,
                    opacity: opacity,
                    zIndexOffset: zIndex,
                    interactive: isStoreHeadMode ? isMyStore : true
                });

                // Popup Content
                var popupContent = `
                    <div style="font-family: sans-serif; min-width: 150px;">
                        <h4 style="margin: 0 0 5px 0; color: #616161;">${store.storeName}</h4>
                        <div style="margin-bottom: 5px;">
                             ${isStoreHeadMode ? statusBadge : ''}
                        </div>
                        <div style="font-size: 0.9em; margin-bottom: 5px;"><strong>Code:</strong> ${store.storeCode}</div>
                         ${!isStoreHeadMode ? statusBadge : ''}
                        ${(isStoreHeadMode && clickAction === 'dashboard') ? `<div style="font-size: 0.8em; color: #4caf50; margin-top: 5px;">Click to view dashboard</div>` : ''}
                    </div>
                `;

                marker.bindPopup(popupContent);

                // Click Event
                marker.on('click', function (e) {
                    if (clickAction === 'dashboard') {
                        // Store Head: Open Dashboard
                        DotNet.invokeMethodAsync('PDTActivityTracker', 'OnStorePinClicked', store.id);
                        mapInstance.setView(e.latlng, 15);
                    }
                    // Admin: Default popup behavior (handled by Leaflet auto-open)
                });

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

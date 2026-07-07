var mapInstance = null;
var markersLayer = null;
var userLocationMarker = null;

window.leafletMap = {
    initialize: function (elementId, stores, options) {
        var mapContainer = document.getElementById(elementId);
        if (!mapContainer) return;

        // If map is already initialized but attached to a different/dead container
        if (mapInstance && mapInstance.getContainer() !== mapContainer) {
            this.destroy();
        }

        // If map is already initialized and valid, just update data
        if (mapInstance) {
            this.updateMarkers(stores, options);
            if (options && options.focusOn) {
                this.focusMarker(options.focusOn.lat, options.focusOn.lng, options.focusOn.zoom);
            }
            return;
        }

        // Determine initial view center and zoom
        var initialCenter = [12.8797, 121.7740];
        var initialZoom = 6;
        if (options && options.focusOn) {
            initialCenter = [options.focusOn.lat, options.focusOn.lng];
            initialZoom = options.focusOn.zoom || 18;
        }

        // Initialize map centered on the determined location
        mapInstance = L.map(elementId).setView(initialCenter, initialZoom);

        // Add OpenStreetMap tiles
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap'
        }).addTo(mapInstance);

        markersLayer = L.layerGroup().addTo(mapInstance);

        this.updateMarkers(stores, options);

        if (options && options.focusOn) {
            // Open popup and trigger flash animation
            this.focusMarker(options.focusOn.lat, options.focusOn.lng, options.focusOn.zoom);
        }

        // Force a resize calculation after a short delay to ensure correct rendering if container was hidden
        setTimeout(() => {
            if (mapInstance) {
                mapInstance.invalidateSize();
            }
        }, 100);
    },

    updateMarkers: function (stores, options) {
        if (!mapInstance || !markersLayer) return;

        // Default options
        options = options || { fitBounds: true };
        if (options.fitBounds === undefined) options.fitBounds = true;

        markersLayer.clearLayers();

        var bounds = L.latLngBounds();
        var hasValidMarkers = false;

        stores.forEach(store => {
            if (store.gpsLatitude && store.gpsLongitude) {
                // Determine Mode based on data properties
                // Mode A: Store Head Dashboard (Explicit 'isMyStore' boolean present)
                // Mode B: Admin Assignment (Original logic checking assignments)

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
                var alertDotHtml = (isStoreHeadMode && store.hasAlert)
                    ? `<div class="alert-dot" style="position: absolute; top: 1px; right: 1px; width: 10px; height: 10px; background: #f44336; border: 2px solid #fff; border-radius: 50%; z-index: 10; animation: alertPulse 1.2s ease-in-out infinite;"></div>`
                    : '';

                var iconHtml = `
                    <div style="position: relative; width: 32px; height: 32px;">
                        ${pulseHtml}
                        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" style="position: relative; z-index: 2;">
                            <path d="M12 21C16.5 17.5 19 14.1667 19 11C19 7.13401 15.866 4 12 4C8.13401 4 5 7.13401 5 11C5 14.1667 7.5 17.5 12 21Z" fill="${color}" stroke="#fff" stroke-width="1.5"/>
                            <circle cx="12" cy="11" r="2.5" fill="#fff"/>
                        </svg>
                        ${alertDotHtml}
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
                    <div style="font-family: sans-serif; min-width: 150px; max-width: 250px; word-break: break-word; overflow-wrap: anywhere;">
                        <h4 style="margin: 0 0 5px 0; color: #616161;">${store.storeName}</h4>
                        <div style="margin-bottom: 5px;">
                             ${isStoreHeadMode ? statusBadge : ''}
                        </div>
                        <div style="font-size: 0.9em; margin-bottom: 5px;"><strong>Code:</strong> ${store.storeCode}</div>
                        ${isStoreHeadMode && store.storeHeadName ? `<div style="font-size: 0.9em; margin-bottom: 5px;"><strong>Store Head:</strong> ${store.storeHeadName}</div>` : ''}
                        ${isStoreHeadMode && store.areaHeadName ? `<div style="font-size: 0.9em; margin-bottom: 5px;"><strong>Area Head:</strong> ${store.areaHeadName}</div>` : ''}
                         ${!isStoreHeadMode ? statusBadge : ''}
                        ${(isStoreHeadMode && clickAction === 'dashboard') ? `<div style="font-size: 0.8em; color: #4caf50; margin-top: 5px;">Click to view progress</div>` : ''}
                    </div>
                `;

                marker.bindPopup(popupContent);

                // Click Event
                marker.on('click', function (e) {
                    if (clickAction === 'dashboard') {
                        // Use dynamic callback name if provided
                        var callback = (options && options.callbackName) ? options.callbackName : 'OnStorePinClicked';

                        // Use per-instance reference if provided to avoid cross-device/instance interference
                        if (options && options.dotNetRef) {
                            options.dotNetRef.invokeMethodAsync(callback, store.id);
                        } else {
                            // Fallback to static call
                            DotNet.invokeMethodAsync('PDTActivityTracker', callback, store.id);
                        }
                    }
                    // Admin: Default popup behavior (handled by Leaflet auto-open)
                });

                marker.storeId = store.id;
                markersLayer.addLayer(marker);

                bounds.extend(marker.getLatLng());
                hasValidMarkers = true;
            }
        });

        // Fit bounds if we have markers and requested
        if (hasValidMarkers && options.fitBounds) {
            var fitOptions = { padding: [50, 50] };
            if (options.maxZoom) fitOptions.maxZoom = options.maxZoom;
            mapInstance.fitBounds(bounds, fitOptions);
        }
    },

    focusMarker: function (lat, lng, zoom) {
        if (!mapInstance) return;
        mapInstance.flyTo([lat, lng], zoom || 15, { animate: true, duration: 1.8 });

        if (markersLayer) {
            markersLayer.eachLayer(function (layer) {
                if (layer instanceof L.Marker) {
                    var pos = layer.getLatLng();
                    if (Math.abs(pos.lat - lat) < 0.00001 && Math.abs(pos.lng - lng) < 0.00001) {
                        layer.openPopup();
                        if (layer._icon) {
                            var pinElement = layer._icon.querySelector('svg');
                            layer._icon.style.zIndex = 99999;
                            if (pinElement) {
                                pinElement.classList.remove('focused-marker-flash');
                                void pinElement.offsetWidth; // force reflow
                                pinElement.classList.add('focused-marker-flash');
                                setTimeout(() => {
                                    if (pinElement) {
                                        pinElement.classList.remove('focused-marker-flash');
                                    }
                                    if (layer._icon) {
                                        layer._icon.style.zIndex = '';
                                    }
                                }, 1500);
                            }
                        }
                    }
                }
            });
        }
    },

    setUserLocation: function (lat, lng) {
        if (!mapInstance) return;

        if (userLocationMarker) {
            userLocationMarker.setLatLng([lat, lng]);
        } else {
            userLocationMarker = L.circleMarker([lat, lng], {
                radius: 10,
                fillColor: "#2196F3",
                color: "#fff",
                weight: 2,
                opacity: 1,
                fillOpacity: 0.8
            }).addTo(mapInstance);

            userLocationMarker.bindPopup("Your Current Location");
        }
    },

    personnelMarkers: {},

    setPersonnelLocation: function (userId, lat, lng, name) {
        if (!mapInstance) return;

        if (this.personnelMarkers[userId]) {
            this.personnelMarkers[userId].setLatLng([lat, lng]);
            this.personnelMarkers[userId].getPopup().setContent(`
                <div style="font-family: sans-serif;">
                    <strong style="color: #2196F3;">Live Tracking: ${name}</strong><br/>
                    <small style="color: #666;">GPS: ${lat.toFixed(6)}, ${lng.toFixed(6)}</small>
                </div>
            `);
        } else {
            var marker = L.circleMarker([lat, lng], {
                radius: 10,
                fillColor: "#2196F3",
                color: "#fff",
                weight: 3,
                opacity: 1,
                fillOpacity: 1,
                className: 'personnel-marker-pulse'
            }).addTo(mapInstance);

            marker.bindPopup(`
                <div style="font-family: sans-serif;">
                    <strong style="color: #2196F3;">Live Tracking: ${name}</strong><br/>
                    <small style="color: #666;">GPS: ${lat.toFixed(6)}, ${lng.toFixed(6)}</small>
                </div>
            `);

            this.personnelMarkers[userId] = marker;
        }
    },


    destroy: function () {
        if (mapInstance) {
            mapInstance.remove();
            mapInstance = null;
            markersLayer = null;
            userLocationMarker = null;
            this.personnelMarkers = {};
        }
    },

    removeAlertFromMarker: function (storeId) {
        if (!markersLayer) return;
        markersLayer.eachLayer(function (layer) {
            if (layer._icon && layer.storeId === storeId) {
                var dot = layer._icon.querySelector('.alert-dot');
                if (dot) dot.remove();
            }
        });
    }
};

window.themeStorage = {
    setTheme: function (theme) {
        localStorage.setItem('theme', theme);
        applyTheme(theme);
    },
    getTheme: function () {
        return localStorage.getItem('theme') || 'Light';
    },
    // Keep backward compatibility
    setDarkMode: function (isDarkMode) {
        const theme = isDarkMode ? 'Dark' : 'Light';
        localStorage.setItem('theme', theme);
        applyTheme(theme);
    },
    getDarkMode: function () {
        const theme = localStorage.getItem('theme') || 'Light';
        return theme === 'Dark';
    }
};

function applyTheme(theme) {
    document.documentElement.classList.remove('theme-lightest', 'theme-light', 'theme-dark');
    document.documentElement.classList.add(`theme-${theme.toLowerCase()}`);
}

// Immediately apply theme on load to prevent flash
(function () {
    const theme = localStorage.getItem('theme') || 'Light';
    applyTheme(theme);
})();

// Camera Helpers
window.cameraInterop = {
    startVideo: async function (videoElementId, facingMode = 'environment') {
        const video = document.getElementById(videoElementId);
        if (!video) return false;

        try {
            // Stop any existing stream first
            if (video.srcObject) {
                const tracks = video.srcObject.getTracks();
                tracks.forEach(track => track.stop());
            }

            const constraints = {
                video: {
                    facingMode: facingMode
                },
                audio: false
            };

            const stream = await navigator.mediaDevices.getUserMedia(constraints);
            video.srcObject = stream;
            video.setAttribute('playsinline', ''); // Required for iOS
            await video.play();
            return true;
        } catch (err) {
            console.error("Camera access error:", err);
            return false;
        }
    },
    stopVideo: function (videoElementId) {
        const video = document.getElementById(videoElementId);
        if (video && video.srcObject) {
            const stream = video.srcObject;
            const tracks = stream.getTracks();
            tracks.forEach(track => track.stop());
            video.srcObject = null;
        }
    },
    hasFlash: async function (videoElementId) {
        const video = document.getElementById(videoElementId);
        if (!video || !video.srcObject) return false;

        const track = video.srcObject.getVideoTracks()[0];
        if (!track) return false;

        try {
            const capabilities = track.getCapabilities();
            return !!capabilities.torch;
        } catch (err) {
            console.warn("Could not check for flash capability:", err);
            return false;
        }
    },
    toggleFlash: async function (videoElementId, enable) {
        const video = document.getElementById(videoElementId);
        if (!video || !video.srcObject) return false;

        const track = video.srcObject.getVideoTracks()[0];
        if (!track) return false;

        try {
            await track.applyConstraints({
                advanced: [{ torch: enable }]
            });
            return true;
        } catch (err) {
            console.error("Error toggling flash:", err);
            return false;
        }
    },
    capturePhoto: function (videoElementId, canvasElementId, watermarkText) {
        const video = document.getElementById(videoElementId);
        const canvas = document.getElementById(canvasElementId);
        if (!video || !canvas) return null;

        let width = video.videoWidth;
        let height = video.videoHeight;

        console.log(`Capture: ${width}x${height}, Watermark: ${watermarkText}`);

        if (width === 0 || height === 0) return null;

        const maxDimension = 1024;
        if (width > height) {
            if (width > maxDimension) {
                height = Math.round(height * (maxDimension / width));
                width = maxDimension;
            }
        } else {
            if (height > maxDimension) {
                width = Math.round(width * (maxDimension / height));
                height = maxDimension;
            }
        }

        canvas.width = width;
        canvas.height = height;
        const context = canvas.getContext('2d');
        context.drawImage(video, 0, 0, width, height);

        if (watermarkText) {
            const padding = 10;
            const fontSize = Math.max(12, Math.round(height * 0.03));
            context.font = `bold ${fontSize}px Arial, sans-serif`;

            const lines = watermarkText.split('|').map(s => s.trim());
            const lineHeight = fontSize * 1.2;
            const boxHeight = (lines.length * lineHeight) + (padding * 2);

            context.fillStyle = 'rgba(0, 0, 0, 0.6)';
            context.fillRect(0, height - boxHeight, width, boxHeight);

            context.fillStyle = 'white';
            context.textBaseline = 'top';
            lines.forEach((line, index) => {
                context.fillText(line, padding, height - boxHeight + padding + (index * lineHeight));
            });
            console.log("Watermark applied.");
        }

        return canvas.toDataURL('image/jpeg', 0.8);
    },
    getCurrentLocation: function () {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject("Geolocation is not supported by this browser.");
                return;
            }
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude
                    });
                },
                (error) => {
                    let msg = "Unknown error";
                    switch (error.code) {
                        case error.PERMISSION_DENIED: msg = "Location permission denied."; break;
                        case error.POSITION_UNAVAILABLE: msg = "Location information is unavailable."; break;
                        case error.TIMEOUT: msg = "Location request timed out."; break;
                    }
                    reject(msg);
                },
                { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
            );
        });
    }
};
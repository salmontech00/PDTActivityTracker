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
    capturePhoto: function (videoElementId, canvasElementId) {
        const video = document.getElementById(videoElementId);
        const canvas = document.getElementById(canvasElementId);
        if (!video || !canvas) return null;

        let width = video.videoWidth;
        let height = video.videoHeight;
        const maxDimension = 1024;

        // Calculate new dimensions while maintaining aspect ratio
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

        const context = canvas.getContext('2d');
        canvas.width = width;
        canvas.height = height;
        context.drawImage(video, 0, 0, width, height);

        return canvas.toDataURL('image/jpeg', 0.8);
    }
};
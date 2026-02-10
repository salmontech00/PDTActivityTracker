window.themeStorage = {
    setTheme: function (theme) {
        localStorage.setItem('theme', theme);
        applyTheme(theme);
    },
    getTheme: function () {
        return localStorage.getItem('theme') || 'Lightest';
    },
    // Keep backward compatibility
    setDarkMode: function (isDarkMode) {
        const theme = isDarkMode ? 'Dark' : 'Lightest';
        localStorage.setItem('theme', theme);
        applyTheme(theme);
    },
    getDarkMode: function () {
        const theme = localStorage.getItem('theme') || 'Lightest';
        return theme === 'Dark';
    }
};

function applyTheme(theme) {
    document.documentElement.classList.remove('theme-lightest', 'theme-light', 'theme-dark');
    document.documentElement.classList.add(`theme-${theme.toLowerCase()}`);
}

// Immediately apply theme on load to prevent flash
(function () {
    const theme = localStorage.getItem('theme') || 'Lightest';
    applyTheme(theme);
})();

// Camera Helpers
window.cameraInterop = {
    startVideo: async function (videoElementId) {
        const video = document.getElementById(videoElementId);
        if (!video) return false;

        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'environment' },
                audio: false
            });
            video.srcObject = stream;
            video.play();
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
    capturePhoto: function (videoElementId, canvasElementId) {
        const video = document.getElementById(videoElementId);
        const canvas = document.getElementById(canvasElementId);
        if (!video || !canvas) return null;

        const context = canvas.getContext('2d');
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        context.drawImage(video, 0, 0, canvas.width, canvas.height);

        return canvas.toDataURL('image/jpeg', 0.8);
    }
};
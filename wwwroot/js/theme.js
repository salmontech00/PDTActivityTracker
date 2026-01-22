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
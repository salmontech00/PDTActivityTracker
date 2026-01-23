using Microsoft.JSInterop;

namespace PDTActivityTracker.Components.Services
{
    public enum ThemeMode
    {
        Lightest,
        Light,
        Dark
    }

    public class ThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        private ThemeMode _currentTheme = ThemeMode.Light; // Changed default from Lightest to Light
        private bool _initialized = false;

        public event Action? OnThemeChanged;

        public ThemeMode CurrentTheme => _currentTheme;

        public bool IsDarkMode => _currentTheme == ThemeMode.Dark;

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;

            try
            {
                var storedTheme = await _jsRuntime.InvokeAsync<string>("themeStorage.getTheme");
                if (Enum.TryParse<ThemeMode>(storedTheme, out var theme))
                {
                    _currentTheme = theme;
                }
                else
                {
                    _currentTheme = ThemeMode.Light; // Changed default to Light
                }
            }
            catch
            {
                _currentTheme = ThemeMode.Light; // Changed default to Light
            }

            _initialized = true;
        }

        public async Task CycleThemeAsync()
        {
            _currentTheme = _currentTheme switch
            {
                ThemeMode.Lightest => ThemeMode.Light,
                ThemeMode.Light => ThemeMode.Dark,
                ThemeMode.Dark => ThemeMode.Lightest,
                _ => ThemeMode.Light // Changed default to Light
            };

            try
            {
                await _jsRuntime.InvokeVoidAsync("themeStorage.setTheme", _currentTheme.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save theme preference: {ex.Message}");
            }

            OnThemeChanged?.Invoke();
        }

        public async Task SetThemeAsync(ThemeMode theme)
        {
            if (_currentTheme == theme) return;

            _currentTheme = theme;

            try
            {
                await _jsRuntime.InvokeVoidAsync("themeStorage.setTheme", _currentTheme.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save theme preference: {ex.Message}");
            }

            OnThemeChanged?.Invoke();
        }

        // Keep backward compatibility
        public async Task ToggleDarkModeAsync()
        {
            await CycleThemeAsync();
        }
    }
}
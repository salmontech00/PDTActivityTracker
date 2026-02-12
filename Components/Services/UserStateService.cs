using Blazored.SessionStorage;

namespace PDTActivityTracker.Components.Services
{
    public class UserStateService(ISessionStorageService sessionStorage)
    {
        private readonly ISessionStorageService _sessionStorage = sessionStorage;

        public bool IsAuthenticated { get; private set; } = false;
        public string? Username { get; private set; }
        public string? Fullname { get; private set; }
        public string? Role { get; private set; }
        public int UserId { get; private set; }

        public event Action? DashboardRefreshRequested;
        public void NotifyDashboardRefresh() => DashboardRefreshRequested?.Invoke();

        public async Task InitializeAsync()
        {
            try
            {
                var username = await _sessionStorage.GetItemAsync<string>("username");
                if (!string.IsNullOrWhiteSpace(username))
                {
                    IsAuthenticated = true;
                    Username = username;

                    var fullname = await _sessionStorage.GetItemAsync<string>("fullname");
                    Fullname = fullname;

                    var role = await _sessionStorage.GetItemAsync<string>("role");
                    Role = role;

                    var userId = await _sessionStorage.GetItemAsync<int>("userId");
                    UserId = userId;
                }
            }
            catch (InvalidOperationException)
            {
                // JS interop not available during prerendering
            }
        }

        public async Task Login(string username)
        {
            IsAuthenticated = true;
            Username = username;
            try { await _sessionStorage.SetItemAsync("username", username); } catch { }
        }

        public async Task Logout()
        {
            IsAuthenticated = false;
            Username = null;
            Fullname = null;
            Role = null;
            UserId = 0;
            try
            {
                await _sessionStorage.RemoveItemAsync("username");
                await _sessionStorage.RemoveItemAsync("fullname");
                await _sessionStorage.RemoveItemAsync("role");
                await _sessionStorage.RemoveItemAsync("userId");
            }
            catch { }
        }

        public async Task SetUsername(string username)
        {
            Username = username;
            try { await _sessionStorage.SetItemAsync("username", username); } catch { }
        }

        public async Task SetFullname(string fullname)
        {
            Fullname = fullname;
            try { await _sessionStorage.SetItemAsync("fullname", fullname); } catch { }
        }

        public async Task SetRole(string role)
        {
            Role = role;
            try { await _sessionStorage.SetItemAsync("role", role); } catch { }
        }

        public async Task SetUserId(int userId)
        {
            UserId = userId;
            try { await _sessionStorage.SetItemAsync("userId", userId); } catch { }
        }
    }
}
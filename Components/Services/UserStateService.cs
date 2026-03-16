using Blazored.SessionStorage;

namespace PDTActivityTracker.Components.Services
{
    public class UserStateService(ISessionStorageService sessionStorage, DashboardRefreshService dashboardRefreshService)
    {
        private readonly ISessionStorageService _sessionStorage = sessionStorage;
        private readonly DashboardRefreshService _dashboardRefreshService = dashboardRefreshService;

        public bool IsAuthenticated { get; private set; } = false;
        public string? Username { get; private set; }
        public string? Fullname { get; private set; }
        public string? Role { get; private set; }
        public int UserId { get; private set; }
        public int SelectedStoreId { get; private set; }


        public string? LastValidPage { get; private set; }

        public event Action? DashboardRefreshRequested;
        public void NotifyDashboardRefresh()
        {
            DashboardRefreshRequested?.Invoke();
            _dashboardRefreshService.NotifyRefresh(SelectedStoreId);
        }

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

                    var selectedStoreId = await _sessionStorage.GetItemAsync<int>("selectedStoreId");
                    SelectedStoreId = selectedStoreId;

                    var lastValidPage = await _sessionStorage.GetItemAsync<string>("lastValidPage");
                    LastValidPage = lastValidPage;
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
            LastValidPage = null;
            try
            {
                await _sessionStorage.RemoveItemAsync("username");
                await _sessionStorage.RemoveItemAsync("fullname");
                await _sessionStorage.RemoveItemAsync("role");
                await _sessionStorage.RemoveItemAsync("userId");
                await _sessionStorage.RemoveItemAsync("selectedStoreId");
                await _sessionStorage.RemoveItemAsync("lastValidPage");
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

        public async Task SetSelectedStoreId(int storeId)
        {
            SelectedStoreId = storeId;
            try { await _sessionStorage.SetItemAsync("selectedStoreId", storeId); } catch { }
        }

        public async Task SetLastValidPage(string url)
        {
            LastValidPage = url;
            try { await _sessionStorage.SetItemAsync("lastValidPage", url); } catch { }
        }

        public string GetDashboardRoute()
        {
            return Role switch
            {
                "Store Head" => "/store-head/map",
                "Area Head" => "/area-head/map",
                "AVP" => "/avp/dashboard",
                "Admin" => "/",
                _ => "/login"
            };
        }
    }
}
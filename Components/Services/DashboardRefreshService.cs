namespace PDTActivityTracker.Components.Services
{
    public class DashboardRefreshService
    {
        public event Action<int?>? OnRefreshRequested;
        public event Action<int, double, double, string>? OnLocationUpdated;

        public void NotifyRefresh(int? storeId = null)
        {
            OnRefreshRequested?.Invoke(storeId);
        }

        public void NotifyLocationUpdate(int userId, double lat, double lng, string fullname)
        {
            OnLocationUpdated?.Invoke(userId, lat, lng, fullname);
        }
    }
}

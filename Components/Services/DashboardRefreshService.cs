namespace PDTActivityTracker.Components.Services
{
    public class DashboardRefreshService
    {
        public event Action<int?>? OnRefreshRequested;

        public void NotifyRefresh(int? storeId = null)
        {
            OnRefreshRequested?.Invoke(storeId);
        }
    }
}

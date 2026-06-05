using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PDTActivityTracker.Model;
using PDTActivityTracker.Components.Model;

namespace PDTActivityTracker.Components.Services
{
    public static class ActivityProgressHelper
    {
        public class ActivityProgressResult
        {
            public int ActivityId { get; set; }
            public int TotalTasks { get; set; }
            public int CompletedTasks { get; set; }
            public double CompletionPercentage { get; set; }
            public DateTime? LastCategoryCompletedAt { get; set; }
            public DateTime? CheckInTime { get; set; }
            public bool IsFullyCompleted { get; set; }
            public string StatusLabel { get; set; } = "Not Started";
            public string StatusColor { get; set; } = "Default";
        }

        public static async Task<Dictionary<int, ActivityProgressResult>> CalculateProgressForActivitiesAsync(
            IEnumerable<StoreDailyActivity> activities,
            AppDbContext db)
        {
            var results = new Dictionary<int, ActivityProgressResult>();
            if (activities == null || !activities.Any()) return results;

            var categories = await db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync();
            var allChecklistItems = await db.ChecklistItems.AsNoTracking().ToListAsync();

            var activityIds = activities.Select(a => a.Id).ToList();
            var allCurrentProgresses = await db.TaskProgresses
                .AsNoTracking()
                .Where(tp => activityIds.Contains(tp.StoreDailyActivityId))
                .ToListAsync();

            var storeIds = activities.Select(a => a.StoreId).Distinct().ToList();

            // Load historical progresses to accurately track rolling-window weekly and monthly tasks
            var allHistoricalProgresses = await db.TaskProgresses
                .AsNoTracking()
                .Include(tp => tp.StoreDailyActivity)
                .Where(tp => tp.StoreDailyActivity != null
                          && storeIds.Contains(tp.StoreDailyActivity.StoreId)
                          && tp.IsCompleted
                          && tp.CompletedAt != null)
                .ToListAsync();

            foreach (var activity in activities)
            {
                var storeId = activity.StoreId;
                var activityDate = activity.Date.Date;
                var today = activityDate;
                var sevenDaysAgo = today.AddDays(-7);
                var oneMonthAgo = today.AddMonths(-1);

                var checklistItems = allChecklistItems.Where(i => IsTaskAssignedToStore(i, storeId)).ToList();
                var currentProgresses = allCurrentProgresses.Where(tp => tp.StoreDailyActivityId == activity.Id).ToList();

                var allPeriodProgress = allHistoricalProgresses
                    .Where(tp => tp.StoreDailyActivity!.StoreId == storeId
                              && tp.StoreDailyActivity.Date <= activityDate)
                    .ToList();

                var categoryProgresses = categories.Select(c =>
                {
                    var catItems = checklistItems.Where(i => i.CategoryId == c.Id).ToList();
                    int catCompleted = 0;
                    foreach (var item in catItems)
                    {
                        bool done = false;
                        if (item.Frequency == "Daily")
                            done = currentProgresses.Any(tp => tp.ChecklistItemId == item.Id && tp.IsCompleted);
                        else if (item.Frequency == "Weekly")
                            done = allPeriodProgress.Any(tp => tp.ChecklistItemId == item.Id
                                && tp.CompletedAt.HasValue && tp.CompletedAt.Value.Date > sevenDaysAgo);
                        else if (item.Frequency == "Monthly")
                            done = allPeriodProgress.Any(tp => tp.ChecklistItemId == item.Id
                                && tp.CompletedAt.HasValue && tp.CompletedAt.Value.Date > oneMonthAgo);
                        if (done) catCompleted++;
                    }

                    DateTime? completedAt = null;
                    if (catCompleted >= catItems.Count && catItems.Count > 0)
                    {
                        var timestamps = catItems.Select(item =>
                        {
                            if (item.Frequency == "Daily")
                                return currentProgresses.FirstOrDefault(tp => tp.ChecklistItemId == item.Id && tp.IsCompleted)?.CompletedAt;
                            if (item.Frequency == "Weekly")
                                return allPeriodProgress.FirstOrDefault(tp => tp.ChecklistItemId == item.Id && tp.CompletedAt.HasValue && tp.CompletedAt.Value.Date > sevenDaysAgo)?.CompletedAt;
                            if (item.Frequency == "Monthly")
                                return allPeriodProgress.FirstOrDefault(tp => tp.ChecklistItemId == item.Id && tp.CompletedAt.HasValue && tp.CompletedAt.Value.Date > oneMonthAgo)?.CompletedAt;
                            return null;
                        }).Where(d => d.HasValue && d.Value > DateTime.MinValue).Select(d => d!.Value).ToList();

                        if (timestamps.Any()) completedAt = timestamps.Max();
                    }

                    return new
                    {
                        CategoryId = c.Id,
                        TotalTasks = catItems.Count,
                        CompletedTasks = catCompleted,
                        CompletedAt = completedAt,
                        IsCompleted = catCompleted >= catItems.Count && catItems.Count > 0
                    };
                }).Where(c => c.TotalTasks > 0).ToList();

                // Handle sequence logic exactly like StoreHeadLanding and AreaHeadMap
                int completedCount = 0;
                bool stopCounting = false;
                foreach (var catProg in categoryProgresses)
                {
                    if (stopCounting) break;
                    completedCount += catProg.CompletedTasks;
                    if (catProg.CompletedTasks < catProg.TotalTasks)
                    {
                        stopCounting = true;
                    }
                }

                int totalTasksCount = checklistItems.Count;
                double completionPercentage = totalTasksCount > 0 ? (completedCount * 100.0 / totalTasksCount) : 0;
                bool isFullyCompleted = categoryProgresses.Any() && categoryProgresses.All(c => c.IsCompleted);

                DateTime? lastCategoryCompletedAt = isFullyCompleted
                    ? (categoryProgresses
                        .Where(c => c.IsCompleted && c.CompletedAt.HasValue)
                        .Select(c => c.CompletedAt!.Value)
                        .DefaultIfEmpty()
                        .Max() is DateTime dt && dt > DateTime.MinValue ? dt : (DateTime?)null)
                    : null;

                // Identify Check In Time
                var checkInCat = categories.FirstOrDefault(c => c.Name.ToLower() == "check in");
                DateTime? checkInTime = null;
                if (checkInCat != null)
                {
                    var checkInItemsIds = checklistItems.Where(i => i.CategoryId == checkInCat.Id).Select(i => i.Id).ToList();
                    var checkInDone = currentProgresses
                        .Where(tp => checkInItemsIds.Contains(tp.ChecklistItemId) && tp.IsCompleted && tp.CompletedAt.HasValue)
                        .OrderBy(tp => tp.CompletedAt)
                        .ToList();
                    
                    if (checkInDone.Count == checkInItemsIds.Count && checkInItemsIds.Any())
                    {
                        checkInTime = checkInDone.Last().CompletedAt;
                    }
                }

                // Determine final Status Label and Color
                string statusLabel = "Not Started";
                string statusColor = "Default";

                if (isFullyCompleted)
                {
                    statusLabel = "Completed";
                    statusColor = "Success";
                }
                else if (activityDate < DateTime.Today)
                {
                    statusLabel = "Incomplete";
                    statusColor = "Error";
                }
                else if (checkInTime.HasValue || completedCount > 0)
                {
                    statusLabel = "In Progress";
                    statusColor = "Warning";
                }

                results[activity.Id] = new ActivityProgressResult
                {
                    ActivityId = activity.Id,
                    TotalTasks = totalTasksCount,
                    CompletedTasks = completedCount,
                    CompletionPercentage = completionPercentage,
                    LastCategoryCompletedAt = lastCategoryCompletedAt,
                    CheckInTime = checkInTime,
                    IsFullyCompleted = isFullyCompleted,
                    StatusLabel = statusLabel,
                    StatusColor = statusColor
                };
            }

            return results;
        }

        private static bool IsTaskAssignedToStore(ChecklistItem task, int storeId)
        {
            if (task.TaskType != "Manpower") return true;
            if (string.IsNullOrWhiteSpace(task.SelectedStores)) return false;
            try
            {
                var ids = JsonSerializer.Deserialize<List<int>>(task.SelectedStores);
                return ids != null && ids.Contains(storeId);
            }
            catch { return false; }
        }
    }
}

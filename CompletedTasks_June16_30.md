### June 30, 2026 (Today)

#### **Replaced MudDatePicker and MudDateRangePicker with Custom Dialog-based Glass Calendars on Mobile**
* **Repository:** `PDTActivityTracker` (Commit: `bd6cdf8`)
* **Files:** 
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [GlassCalendarWrapperDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/GlassCalendarWrapperDialog.razor)
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [GlassDateRangeWrapperDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/GlassDateRangeWrapperDialog.razor)
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [GlassCalendarDemoDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/GlassCalendarDemoDialog.razor)
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [AssignStoreHeadDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/AssignStoreHeadDialog.razor)
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [AddUser.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/AddUser.razor)
  * `PDTActivityTracker` → `Components/Pages/AVP` → [AVPMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/AVP/AVPMonitoring.razor)
  * `PDTActivityTracker` → `Components/Pages/StoreHead` → [MonitoringOwnStore.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/StoreHead/MonitoringOwnStore.razor)
  * `PDTActivityTracker` → `Components/Pages/AreaHead` → [AreaHeadMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/AreaHead/AreaHeadMonitoring.razor)
  * `PDTActivityTracker` → `Components/Pages/AreaHead` → [AreaHeadWeeklyReport.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/AreaHead/AreaHeadWeeklyReport.razor)
  * `PDTActivityTracker` → `Components/Pages/Admin` → [ReportsMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Admin/ReportsMonitoring.razor)
  * `PDTActivityTracker` → `wwwroot/css` → [pulse.css](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/wwwroot/css/pulse.css)
* **The Fix:** Removed native MudBlazor popover calendars on mobile viewports due to layout overflow and sizing bugs.
* **Technical Details:**
  * Implemented `GlassCalendarWrapperDialog.razor` and `GlassDateRangeWrapperDialog.razor` containing a dedicated `GlassCalendarDemoDialog.razor` instance.
  * Hidden default MudBlazor popover pickers (`MudDatePicker` and `MudDateRangePicker`) in desktop views via `.date-picker-desktop` classes and replaced them in mobile views (`.date-picker-mobile`) with read-only `MudTextField` elements displaying formatting wrappers.
  * Modified `OnRangeChanged` in `GlassDateRangeWrapperDialog.razor` to execute `MudDialog.Close(DialogResult.Ok(...))` immediately when both start and end date boundaries are filled.
  * Overrode `.mud-dialog-content` padding with `0 !important` inside wrapper styles to allow the custom calendar to occupy the full dialog width.

---

### June 29, 2026

#### **Fixed Dropdown and Date Picker Sizing Overlaps (PDT Activity Tracker)**
* **Repository:** `PDTActivityTracker` (Commit: `1aba60a`)
* **Files:** 
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [AssignStoreHeadDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/AssignStoreHeadDialog.razor)
  * `PDTActivityTracker` → `Components/Pages/Admin` → [ReportsMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Admin/ReportsMonitoring.razor)
  * `PDTActivityTracker` → `Components/Pages/AVP` → [AVPMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/AVP/AVPMonitoring.razor)
  * `PDTActivityTracker` → `Components/Pages/AreaHead` → [AreaHeadMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/AreaHead/AreaHeadMonitoring.razor)
  * `PDTActivityTracker` → `Components/Pages/AreaHead` → [AreaHeadWeeklyReport.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/AreaHead/AreaHeadWeeklyReport.razor)
  * `PDTActivityTracker` → `Components/Pages/StoreHead` → [MonitoringOwnStore.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/StoreHead/MonitoringOwnStore.razor)
  * `PDTActivityTracker` → `wwwroot/css` → [pulse.css](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/wwwroot/css/pulse.css)
* **The Fix:** Input dropdown elements and date pickers overlapping dialog edges on 375px screens.
* **Technical Details:**
  * Added media query rule breaks at `max-width: 480px` and `max-width: 375px` to configure input elements, calendar components, and dropdown select parameters to scale and prevent overlapping the boundaries of Dialog structures.
  * Enforced specific base dimensions on `AssignStoreHeadDialog.razor` date selectors to display consistently on standard screen layouts.

#### **Fixed Classification Logic & Load Reports Filter Sync (AB Assortment)**
* **Repository:** `AB Assortment` (Commits: `4c3e49a`, `8f5e4cc`, `b441532`, `7fc97b9`, `47f501f`)
* **Files:** 
  * `AB Assortment` → `backend/controllers` → [skuController.js](file:///d:/Assortment%20Bible/assortment-bible-dev/backend/controllers/skuController.js)
  * `AB Assortment` → `frontend/src/Pages` → [SkuCategory.jsx](file:///d:/Assortment%20Bible/assortment-bible-dev/frontend/src/Pages/SkuCategory.jsx)
  * `AB Assortment` → `frontend/src/Pages` → [StoresCategory.jsx](file:///d:/Assortment%20Bible/assortment-bible-dev/frontend/src/Pages/StoresCategory.jsx)
  * `AB Assortment` → `frontend/src/Pages` → [Report.jsx](file:///d:/Assortment%20Bible/assortment-bible-dev/frontend/src/Pages/Report.jsx)
* **The Fix:** Inconsistent classification parsing (Wing, Specialty, Core) during report loading.
* **Technical Details:**
  * Overhauled filtering in `skuController.js` to ensure selecting "Wing" queries only Wing SKUs, selecting "Specialty" queries only Specialty SKUs, selecting "Wing+Specialty" queries Wing, Specialty, and Wing+Specialty, and selecting "Core" queries all Core items.
  * Corrected category data loading synchronization inside `SkuCategory.jsx` to resolve asynchronous race conditions on full page reload.
  * Mapped timezone date offset calculations in backend queries to match local filter constraints.

#### **Implemented Dirty-Filter Checks & Dynamic Limits on Excel Export (AB Assortment)**
* **Repository:** `AB Assortment` (Commits: `8d247ea`, `aeafbe6`, `50238bb`)
* **Files:** 
  * `AB Assortment` → `frontend/src/Pages` → [Report.jsx](file:///d:/Assortment%20Bible/assortment-bible-dev/frontend/src/Pages/Report.jsx)
* **The Fix:** Users exporting stale reports after changing search criteria without reloading, and memory overflow during large exports.
* **Technical Details:**
  * Added filter-state event listeners in `Report.jsx` to toggle dirty states and block spreadsheet exports when query values differ from the loaded state, highlighting the "Load Report" button.
  * Added an export boundary select dropdown (10, 100, and 1000 items limit) and restricted the summary counters to the size of the returned set.

---

### June 26, 2026

#### **Standardized Excel Import Validation & SKU Template Naming (AB Assortment)**
* **Repository:** `AB Assortment` (Commits: `4c3e49a`, `8f5e4cc`)
* **Files:** 
  * `AB Assortment` → `frontend/public` → `AB_Update_SKU_Template.xlsx`
  * `AB Assortment` → `frontend/src/Pages` → [SkuCategory.jsx](file:///d:/Assortment%20Bible/assortment-bible-dev/frontend/src/Pages/SkuCategory.jsx)
* **The Fix:** Silent upload failures when column configurations or template labels were mismatching.
* **Technical Details:**
  * Modified `AB_Update_SKU_Template.xlsx` default headings, renaming columns from "LARGE SMR" to "SMR LARGE" and "PREMIUM SMR" to "SMR PREMIUM".
  * Integrated parsing checks in `SkuCategory.jsx` to output diagnostic error sheets listing cell numbers and column positions that failed verification during spreadsheet imports.

---

### June 25, 2026

#### **Investigated LDAP Database Connection Link Failures (PDT Activity Tracker)**
* **Repository:** `PDTActivityTracker` (Diag session)
* **Files:** 
  * `PDTActivityTracker` → `Components/Services` → [LdapService.cs](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Services/LdapService.cs)
* **The Fix:** Resolving database connection errors preventing user login attempts.
* **Technical Details:**
  * Analyzed system trace output and confirmed DB connection failures are due to server-side accounts policy disabling user credentials on the target host (`CWBSY0011` for user `ITD66978SE` on host `192.168.0.40`).

---

### June 23, 2026

#### **Implemented Soft Delete for Checklist Items & Dynamic Recalculations**
* **Repository:** `PDTActivityTracker` (Commit: `6624009`)
* **Files:** 
  * `PDTActivityTracker` → `Components/Pages/Admin` → [Checklist.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Admin/Checklist.razor)
  * `PDTActivityTracker` → `Components/Pages/Admin` → [ReportsMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Admin/ReportsMonitoring.razor)
  * `PDTActivityTracker` → `Components/Pages/StoreHead` → [StoreActivityPlanner.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/StoreHead/StoreActivityPlanner.razor)
  * `PDTActivityTracker` → `Components/Pages/StoreHead` → [StoreHeadLanding.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/StoreHead/StoreHeadLanding.razor)
  * `PDTActivityTracker` → `Components/Model` → [ChecklistItem.cs](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Model/ChecklistItem.cs)
  * `PDTActivityTracker` → `Components/Services` → [ActivityProgressHelper.cs](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Services/ActivityProgressHelper.cs)
  * `PDTActivityTracker` → `Program.cs`
* **The Fix:** Deleting a checklist item broke historical records and miscalculated completions.
* **Technical Details:**
  * Modified `ChecklistItem.cs` to add a nullable boolean deleted field.
  * Updated `ActivityProgressHelper.cs` to filter out soft-deleted checklist items when recalculating daily task totals and progress metrics.
  * Implemented soft delete callbacks in `Checklist.razor` that delete items without destroying past completed daily activity records.

---

### June 19, 2026

#### **Added Version Brand Label & Custom Lightbox Dialog**
* **Repository:** `PDTActivityTracker` (Commit: `3c0a91e`)
* **Files:** 
  * `PDTActivityTracker` → `Components/Pages` → [Login.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Login.razor)
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [PhotoGalleryDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/PhotoGalleryDialog.razor)
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [IssueDetailDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/IssueDetailDialog.razor)
* **The Fix:** Corrected photo zoom-out scaling and image size enlargement layout behavior; added brand version tracking tags.
* **Technical Details:** 
  * Added a version-tracking label (`v1.0`) inside `Login.razor` styled to dynamically align below the main brand logo.
  * Configured `PhotoGalleryDialog.razor` image elements to trigger a customized lightbox dialog that displays zoomed images.
  * Overwrote image size boundaries in `IssueDetailDialog.razor` to scale within card dimensions when viewport width transitions.

---

### June 18, 2026

#### **Standardized Notifications & Fixed Pagination/Review Button Scaling**
* **Repository:** `PDTActivityTracker` (Commits: `69117b8`, `963cea4`, `a899ea7`)
* **Files:** 
  * `PDTActivityTracker` → `Components/Layout` → [MainLayout.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Layout/MainLayout.razor)
  * `PDTActivityTracker` → `Program.cs`
  * `PDTActivityTracker` → `wwwroot/css` → [pulse.css](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/wwwroot/css/pulse.css)
  * `PDTActivityTracker` → `Components/Pages/AVP` → [AVPMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/AVP/AVPMonitoring.razor)
  * `PDTActivityTracker` → `Components/Pages/Admin` → [ReportsMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Admin/ReportsMonitoring.razor)
  * `PDTActivityTracker` → `Components/Pages/Admin` → [StoreAssignment.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Admin/StoreAssignment.razor)
  * `PDTActivityTracker` → `Components/Pages/Admin` → [Users.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Admin/Users.razor)
  * `PDTActivityTracker` → `Components/Pages/AreaHead` → [AreaHeadMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/AreaHead/AreaHeadMonitoring.razor)
  * `PDTActivityTracker` → `Components/Pages/StoreHead` → [MonitoringOwnStore.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/StoreHead/MonitoringOwnStore.razor)
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [ActivityDetailDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/ActivityDetailDialog.razor)
* **The Fix:** Resolved notification styling inconsistencies, corrected pagination overflow boundaries on mobile pages, and adjusted feedback/review button sizes.
* **Technical Details:**
  * Added default toaster configurations inside `Program.cs` and matching CSS overrides inside `pulse.css` to unify snackbar positioning, background gradients, and exit durations.
  * Refactored pagination blocks in AVP and Area Head dashboards to scale responsively and wrap cleanly under list templates.
  * Resized evaluation buttons and review controls inside AVP dashboards to fit screen configurations.

---

### June 17, 2026

#### **Fixed Image Boundaries & Store Head Dialog Layouts**
* **Repository:** `PDTActivityTracker` (Commits: `fcb636c`, `29484ae`)
* **Files:** 
  * `PDTActivityTracker` → `Components/Pages/Admin` → [ReportsMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Admin/ReportsMonitoring.razor)
  * `PDTActivityTracker` → `Components/Pages/AreaHead` → [AreaHeadMonitoring.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/AreaHead/AreaHeadMonitoring.razor)
  * `PDTActivityTracker` → `Components/Pages/AreaHead` → [AreaHeadWeeklyReport.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/AreaHead/AreaHeadWeeklyReport.razor)
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [IssueDetailDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/IssueDetailDialog.razor)
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [AssignStoreHeadDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/AssignStoreHeadDialog.razor)
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [ConfirmDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/ConfirmDialog.razor)
  * `PDTActivityTracker` → `Components/Pages/StoreHead` → [MonitoringOwnStore.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/StoreHead/MonitoringOwnStore.razor)
  * `PDTActivityTracker` → `Components/Pages/StoreHead` → [StoreActivityPlanner.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/StoreHead/StoreActivityPlanner.razor)
  * `PDTActivityTracker` → `Components/Pages/AreaHead` → [AreaHeadMap.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/AreaHead/AreaHeadMap.razor)
* **The Fix:** Corrected photo scaling overflows in store panels, resized the clear filters button, and fixed calendar layouts on store assignments.
* **Technical Details:**
  * Injected CSS constraints inside `pulse.css` to limit upload item preview heights and keep images within card containers.
  * Resized spacing and margin properties around the `AssignStoreHeadDialog.razor` date range selection wrappers to maintain layout symmetry.

---

### June 16, 2026

#### **Fine-tuned Input Alignment and Borders**
* **Repository:** `PDTActivityTracker` (Design alignments)
* **Files:** 
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [ActivityDetailDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/ActivityDetailDialog.razor)
  * `PDTActivityTracker` → `Components/Pages/Dialogs` → [AddEditStoreDialog.razor](file:///c:/Users/itis11.ta/source/repos/PDTActivityTracker/Components/Pages/Dialogs/AddEditStoreDialog.razor)
* **The Fix:** Visual alignment and boundary adjustments across active dialog panels.
* **Technical Details:**
  * Standardized dialogue panel borders, margins, and padding rules on `AddEditStoreDialog.razor` and `ActivityDetailDialog.razor` input elements.

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Blazored.SessionStorage;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using OfficeOpenXml;
using PDTActivityTracker.Components;
using PDTActivityTracker.Components.Services;
using PDTActivityTracker.Model;
using PDTActivityTracker.Components.Model;
using MudBlazor;


var builder = WebApplication.CreateBuilder(args);

#pragma warning disable CS0618
ExcelPackage.License.SetNonCommercialPersonal("TuitionValidation");
#pragma warning restore CS0618

builder.Services.AddMudServices(config =>
{
    // Desktop keeps default bottom-left; mobile centering is handled via CSS
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4500;  // 4.5s — enough time to read errors on mobile
    config.SnackbarConfiguration.HideTransitionDuration = 300;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddScoped<PDTActivityTracker.Components.Services.ThemeService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

builder.Services.AddScoped<UserStateService>();
builder.Services.AddSingleton<DashboardRefreshService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ExportService>();

builder.Services.Configure<LdapSettings>(builder.Configuration.GetSection("LdapSettings"));
builder.Services.AddScoped<LdapService>();

builder.Services.AddHttpClient();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DatabaseConnectionString"))
);

var app = builder.Build();

await EnsureDefaultAdminAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task EnsureDefaultAdminAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

    using var db = await dbFactory.CreateDbContextAsync();

    await db.Database.EnsureCreatedAsync();

    // Ensure new tables and columns exist (EnsureCreated only works if DB is empty)
    await db.Database.ExecuteSqlRawAsync(@"
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Areas]') AND type in (N'U'))
        BEGIN
            DROP TABLE [Areas];
        END

        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[AccessTags]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [AccessTags] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [Name] nvarchar(max) NOT NULL,
                [Description] nvarchar(max) NULL,
                CONSTRAINT [PK_AccessTags] PRIMARY KEY ([Id])
            );
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Tags' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            ALTER TABLE [Users] ADD [Tags] nvarchar(max) NULL;
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'EmployeeNumber' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            ALTER TABLE [Users] ADD [EmployeeNumber] nvarchar(max) NULL;
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Designation' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'Position' AND Object_ID = Object_ID(N'Users'))
            BEGIN
                EXEC sp_rename 'Users.Position', 'Designation', 'COLUMN';
            END
            ELSE
            BEGIN
                ALTER TABLE [Users] ADD [Designation] nvarchar(max) NULL;
            END
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Status' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            ALTER TABLE [Users] ADD [Status] nvarchar(max) NULL;
            EXEC('UPDATE [Users] SET [Status] = ''Active''');
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'CurrentLatitude' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            ALTER TABLE [Users] ADD [CurrentLatitude] float NULL;
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'CurrentLongitude' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            ALTER TABLE [Users] ADD [CurrentLongitude] float NULL;
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'LastLocationUpdate' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            ALTER TABLE [Users] ADD [LastLocationUpdate] datetime2 NULL;
        END

        IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'Area' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            ALTER TABLE [Users] DROP COLUMN [Area];
        END

        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Categories]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [Categories] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [Name] nvarchar(max) NOT NULL,
                [Icon] nvarchar(max) NOT NULL,
                [ColorClass] nvarchar(max) NOT NULL,
                [SortOrder] int NOT NULL,
                CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
            );
        END

        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ChecklistItems]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [ChecklistItems] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [TaskName] nvarchar(max) NULL,
                [Description] nvarchar(max) NULL,
                [CategoryId] int NOT NULL,
                [Section] nvarchar(max) NULL,
                [IsPhotoRequired] bit NOT NULL,
                [NumberOfPhotos] int NOT NULL,
                [IsMandatory] bit NOT NULL,
                [Frequency] nvarchar(max) NOT NULL,
                [IsRemarksRequired] bit NOT NULL,
                [SortOrder] int NOT NULL,
                CONSTRAINT [PK_ChecklistItems] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_ChecklistItems_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
            );
        END
        ELSE
        BEGIN
            -- Migration if ChecklistItems already exists but has string Category
            IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'Category' AND Object_ID = Object_ID(N'ChecklistItems'))
            BEGIN
                -- Add CategoryId column if it doesn't exist
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'CategoryId' AND Object_ID = Object_ID(N'ChecklistItems'))
                BEGIN
                    ALTER TABLE [ChecklistItems] ADD [CategoryId] int NULL;
                END
            END
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'IsDeleted' AND Object_ID = Object_ID(N'ChecklistItems'))
        BEGIN
            ALTER TABLE [ChecklistItems] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Section' AND Object_ID = Object_ID(N'ChecklistItems'))
        BEGIN
            ALTER TABLE [ChecklistItems] ADD [Section] nvarchar(max) NULL;
        END

        -- Make TaskName nullable for existing tables
        IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'TaskName' AND Object_ID = Object_ID(N'ChecklistItems') AND is_nullable = 0)
        BEGIN
            ALTER TABLE [ChecklistItems] ALTER COLUMN [TaskName] nvarchar(max) NULL;
        END

        -- Create Stores Table
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Stores]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [Stores] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [StoreCode] nvarchar(max) NOT NULL,
                [StoreName] nvarchar(max) NOT NULL,
                [Location] nvarchar(max) NOT NULL,
                [Status] nvarchar(max) NULL,
                [GPSLatitude] float NULL,
                [GPSLongitude] float NULL,
                [ContactPhone] nvarchar(max) NULL,
                [ContactEmail] nvarchar(max) NULL,
                [IsDeleted] bit NOT NULL DEFAULT 0,
                [StoreHeadId] int NULL,
                [AreaHeadId] int NULL,
                CONSTRAINT [PK_Stores] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_Stores_Users_StoreHeadId] FOREIGN KEY ([StoreHeadId]) REFERENCES [Users] ([Id]),
                CONSTRAINT [FK_Stores_Users_AreaHeadId] FOREIGN KEY ([AreaHeadId]) REFERENCES [Users] ([Id])
            );
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'IsDeleted' AND Object_ID = Object_ID(N'Stores'))
            BEGIN
                ALTER TABLE [Stores] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
            END
        END

        -- Create StoreAssignmentLogs Table
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[StoreAssignmentLogs]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [StoreAssignmentLogs] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [StoreId] int NOT NULL,
                [UserId] int NULL,
                [Role] nvarchar(max) NOT NULL,
                [Action] nvarchar(max) NOT NULL,
                [Timestamp] datetime2 NOT NULL,
                [Remarks] nvarchar(max) NULL,
                CONSTRAINT [PK_StoreAssignmentLogs] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_StoreAssignmentLogs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_StoreAssignmentLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
            );
        END

        -- Create ActivityLogs Table
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ActivityLogs]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [ActivityLogs] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [UserId] int NULL,
                [StoreId] int NULL,
                [Action] nvarchar(max) NOT NULL,
                [EntityType] nvarchar(max) NULL,
                [EntityId] int NULL,
                [Details] nvarchar(max) NULL,
                [Timestamp] datetime2 NOT NULL,
                [IPAddress] nvarchar(max) NULL,
                CONSTRAINT [PK_ActivityLogs] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_ActivityLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]),
                CONSTRAINT [FK_ActivityLogs_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id])
            );
        END

        -- Create TaskPhotos Table
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[TaskPhotos]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [TaskPhotos] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [UploadedById] int NULL,
                [StoreId] int NULL,
                [ChecklistItemId] int NULL,
                [CategoryId] int NULL,
                [Description] nvarchar(max) NULL,
                [UploadedAt] datetime2 NOT NULL,
                [ImageData] varbinary(max) NULL,
                [ImageMimeType] nvarchar(max) NULL,
                [Latitude] float NULL,
                [Longitude] float NULL,
                [CaptureTimestamp] datetime2 NULL,
                CONSTRAINT [PK_TaskPhotos] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_TaskPhotos_Users_UploadedById] FOREIGN KEY ([UploadedById]) REFERENCES [Users] ([Id]),
                CONSTRAINT [FK_TaskPhotos_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]),
                CONSTRAINT [FK_TaskPhotos_ChecklistItems_ChecklistItemId] FOREIGN KEY ([ChecklistItemId]) REFERENCES [ChecklistItems] ([Id]),
                CONSTRAINT [FK_TaskPhotos_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id])
            );
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Latitude' AND Object_ID = Object_ID(N'TaskPhotos'))
            BEGIN
                ALTER TABLE [TaskPhotos] ADD [Latitude] float NULL;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Longitude' AND Object_ID = Object_ID(N'TaskPhotos'))
            BEGIN
                ALTER TABLE [TaskPhotos] ADD [Longitude] float NULL;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'CaptureTimestamp' AND Object_ID = Object_ID(N'TaskPhotos'))
            BEGIN
                ALTER TABLE [TaskPhotos] ADD [CaptureTimestamp] datetime2 NULL;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'StoreDailyActivityId' AND Object_ID = Object_ID(N'TaskPhotos'))
            BEGIN
                ALTER TABLE [TaskPhotos] ADD [StoreDailyActivityId] int NULL;
                -- Optional: Add constraint if it doesn't exist
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TaskPhotos_StoreDailyActivities_StoreDailyActivityId')
                BEGIN
                    ALTER TABLE [TaskPhotos] ADD CONSTRAINT [FK_TaskPhotos_StoreDailyActivities_StoreDailyActivityId] 
                    FOREIGN KEY ([StoreDailyActivityId]) REFERENCES [StoreDailyActivities] ([Id]);
                END
            END
        END

        -- Create Notifications Table
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Notifications]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [Notifications] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [UserId] int NOT NULL,
                [Message] nvarchar(max) NOT NULL,
                [Type] nvarchar(max) NOT NULL,
                [IsRead] bit NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [RelatedEntityId] int NULL,
                [RelatedEntityType] nvarchar(max) NULL,
                CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
            );
        END

        -- Create Issues Table
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Issues]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [Issues] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [StoreId] int NOT NULL,
                [Title] nvarchar(max) NOT NULL,
                [Description] nvarchar(max) NULL,
                [Priority] nvarchar(max) NOT NULL,
                [Status] nvarchar(max) NOT NULL,
                [ReportedById] int NULL,
                [ReportedAt] datetime2 NOT NULL,
                [ResolvedAt] datetime2 NULL,
                CONSTRAINT [PK_Issues] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_Issues_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_Issues_Users_ReportedById] FOREIGN KEY ([ReportedById]) REFERENCES [Users] ([Id])
            );
        END
        ELSE
        BEGIN
            IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'ReportedById' AND Object_ID = Object_ID(N'Issues') AND is_nullable = 0)
            BEGIN
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Issues_Users_ReportedById')
                BEGIN
                    ALTER TABLE [Issues] DROP CONSTRAINT [FK_Issues_Users_ReportedById];
                END
                ALTER TABLE [Issues] ALTER COLUMN [ReportedById] int NULL;
                ALTER TABLE [Issues] ADD CONSTRAINT [FK_Issues_Users_ReportedById] FOREIGN KEY ([ReportedById]) REFERENCES [Users] ([Id]);
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'ImageData' AND Object_ID = Object_ID(N'Issues'))
            BEGIN
                ALTER TABLE [Issues] ADD [ImageData] varbinary(max) NULL;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'ImageMimeType' AND Object_ID = Object_ID(N'Issues'))
            BEGIN
                ALTER TABLE [Issues] ADD [ImageMimeType] nvarchar(max) NULL;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AreaHeadRemark' AND Object_ID = Object_ID(N'Issues'))
            BEGIN
                ALTER TABLE [Issues] ADD [AreaHeadRemark] nvarchar(max) NULL;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'ReviewedById' AND Object_ID = Object_ID(N'Issues'))
            BEGIN
                ALTER TABLE [Issues] ADD [ReviewedById] int NULL;
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Issues_Users_ReviewedById')
                BEGIN
                    ALTER TABLE [Issues] ADD CONSTRAINT [FK_Issues_Users_ReviewedById] 
                    FOREIGN KEY ([ReviewedById]) REFERENCES [Users] ([Id]);
                END
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Latitude' AND Object_ID = Object_ID(N'Issues'))
            BEGIN
                ALTER TABLE [Issues] ADD [Latitude] float NULL;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Longitude' AND Object_ID = Object_ID(N'Issues'))
            BEGIN
                ALTER TABLE [Issues] ADD [Longitude] float NULL;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'CaptureTime' AND Object_ID = Object_ID(N'Issues'))
            BEGIN
                ALTER TABLE [Issues] ADD [CaptureTime] datetime2 NULL;
            END
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'QuantityExpected' AND Object_ID = Object_ID(N'ChecklistItems'))
        BEGIN
            ALTER TABLE [ChecklistItems] ADD [QuantityExpected] int NULL;
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'TaskType' AND Object_ID = Object_ID(N'ChecklistItems'))
        BEGIN
            ALTER TABLE [ChecklistItems] ADD [TaskType] nvarchar(max) NULL;
        END
        
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Position' AND Object_ID = Object_ID(N'ChecklistItems'))
        BEGIN
            ALTER TABLE [ChecklistItems] ADD [Position] nvarchar(max) NULL;
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'SelectedStores' AND Object_ID = Object_ID(N'ChecklistItems'))
        BEGIN
            ALTER TABLE [ChecklistItems] ADD [SelectedStores] nvarchar(max) NULL;
        END

        -- Create StoreDailyActivities Table
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[StoreDailyActivities]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [StoreDailyActivities] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [StoreId] int NOT NULL,
                [Date] datetime2 NOT NULL,
                [CurrentCategoryId] int NOT NULL,
                [IsCompleted] bit NOT NULL,
                [CompletedAt] datetime2 NULL,
                [StartedById] int NULL,
                CONSTRAINT [PK_StoreDailyActivities] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_StoreDailyActivities_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_StoreDailyActivities_Categories_CurrentCategoryId] FOREIGN KEY ([CurrentCategoryId]) REFERENCES [Categories] ([Id]),
                CONSTRAINT [FK_StoreDailyActivities_Users_StartedById] FOREIGN KEY ([StartedById]) REFERENCES [Users] ([Id])
            );
        END
        ELSE
        BEGIN
            IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'StartedById' AND Object_ID = Object_ID(N'StoreDailyActivities') AND is_nullable = 0)
            BEGIN
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_StoreDailyActivities_Users_StartedById')
                BEGIN
                    ALTER TABLE [StoreDailyActivities] DROP CONSTRAINT [FK_StoreDailyActivities_Users_StartedById];
                END
                ALTER TABLE [StoreDailyActivities] ALTER COLUMN [StartedById] int NULL;
                ALTER TABLE [StoreDailyActivities] ADD CONSTRAINT [FK_StoreDailyActivities_Users_StartedById] FOREIGN KEY ([StartedById]) REFERENCES [Users] ([Id]);
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AreaHeadRemark' AND Object_ID = Object_ID(N'StoreDailyActivities'))
            BEGIN
                ALTER TABLE [StoreDailyActivities] ADD [AreaHeadRemark] nvarchar(max) NULL;
            END
            if (NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AreaHeadReviewedAt' AND Object_ID = Object_ID(N'StoreDailyActivities')))
            BEGIN
                ALTER TABLE [StoreDailyActivities] ADD [AreaHeadReviewedAt] datetime2 NULL;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AreaHeadId' AND Object_ID = Object_ID(N'StoreDailyActivities'))
            BEGIN
                ALTER TABLE [StoreDailyActivities] ADD [AreaHeadId] int NULL;
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_StoreDailyActivities_Users_AreaHeadId')
                BEGIN
                    ALTER TABLE [StoreDailyActivities] ADD CONSTRAINT [FK_StoreDailyActivities_Users_AreaHeadId] 
                    FOREIGN KEY ([AreaHeadId]) REFERENCES [Users] ([Id]);
                END
            END
        END

        -- Create TaskProgresses Table
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[TaskProgresses]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [TaskProgresses] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [StoreDailyActivityId] int NOT NULL,
                [ChecklistItemId] int NOT NULL,
                [IsCompleted] bit NOT NULL,
                [ActualQuantity] int NULL,
                [Remarks] nvarchar(max) NULL,
                [CompletedAt] datetime2 NULL,
                [Latitude] float NULL,
                [Longitude] float NULL,
                CONSTRAINT [PK_TaskProgresses] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_TaskProgresses_StoreDailyActivities_StoreDailyActivityId] FOREIGN KEY ([StoreDailyActivityId]) REFERENCES [StoreDailyActivities] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_TaskProgresses_ChecklistItems_ChecklistItemId] FOREIGN KEY ([ChecklistItemId]) REFERENCES [ChecklistItems] ([Id])
            );
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'PositionActuals' AND Object_ID = Object_ID(N'TaskProgresses'))
            BEGIN
                ALTER TABLE [TaskProgresses] ADD [PositionActuals] nvarchar(max) NULL;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'PositionExpected' AND Object_ID = Object_ID(N'TaskProgresses'))
            BEGIN
                ALTER TABLE [TaskProgresses] ADD [PositionExpected] nvarchar(max) NULL;
            END
        END
    ");

    // Seed Categories if empty or not fully updated
    var expectedCategories = new List<Category>
    {
        new Category { Name = "Check In", Icon = Icons.Material.Filled.Login, ColorClass = "category-checkin", SortOrder = 0 },
        new Category { Name = "Opening", Icon = Icons.Material.Filled.DoorFront, ColorClass = "category-opening", SortOrder = 1 },
        new Category { Name = "Asset", Icon = Icons.Material.Filled.Devices, ColorClass = "category-asset", SortOrder = 2 },
        new Category { Name = "Human Resource", Icon = Icons.Material.Filled.Groups, ColorClass = "category-hr", SortOrder = 3 },
        new Category { Name = "Inventory", Icon = Icons.Material.Filled.Inventory, ColorClass = "category-inventory", SortOrder = 4 },
        new Category { Name = "Marketing", Icon = Icons.Material.Filled.Campaign, ColorClass = "category-marketing", SortOrder = 5 },
        new Category { Name = "Expenses & Repair", Icon = Icons.Material.Filled.Build, ColorClass = "category-expenses", SortOrder = 6 },
        new Category { Name = "Check Out", Icon = Icons.Material.Filled.Logout, ColorClass = "category-checkout", SortOrder = 7 }
    };

    if (!await db.Categories.AnyAsync())
    {
        foreach (var cat in expectedCategories)
        {
            db.Categories.Add(cat);
        }
        await db.SaveChangesAsync();
    }
    else
    {
        // Update existing categories if names match, or add missing ones
        var existingCategories = await db.Categories.ToListAsync();
        foreach (var expected in expectedCategories)
        {
            var existing = existingCategories.FirstOrDefault(c => c.Name == expected.Name);
            if (existing != null)
            {
                existing.Icon = expected.Icon;
                existing.ColorClass = expected.ColorClass;
                existing.SortOrder = expected.SortOrder;
            }
            else
            {
                // Special case: Rename "Closing" to "Check Out" if it exists
                var closing = existingCategories.FirstOrDefault(c => c.Name == "Closing");
                if (closing != null && expected.Name == "Check Out")
                {
                    closing.Name = "Check Out";
                    closing.Icon = expected.Icon;
                    closing.ColorClass = expected.ColorClass;
                    closing.SortOrder = expected.SortOrder;
                }
                else
                {
                    db.Categories.Add(expected);
                }
            }
        }
        await db.SaveChangesAsync();
    }

    // Data Migration for ChecklistItems if necessary
    // We check if the legacy 'Category' column exists and has data
    bool hasLegacyData = false;
    using (var cmd = db.Database.GetDbConnection().CreateCommand())
    {
        cmd.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE Name = 'Category' AND Object_ID = Object_ID('ChecklistItems')";
        if (db.Database.GetDbConnection().State != System.Data.ConnectionState.Open) await db.Database.GetDbConnection().OpenAsync();
        var count = (int)(cmd.ExecuteScalar() ?? 0);
        if (count > 0)
        {
            cmd.CommandText = "SELECT COUNT(*) FROM ChecklistItems WHERE CategoryId IS NULL AND Category IS NOT NULL";
            hasLegacyData = (int)(cmd.ExecuteScalar() ?? 0) > 0;
        }
    }

    if (hasLegacyData)
    {
        var categoriesInDb = await db.Categories.ToListAsync();
        foreach (var cat in categoriesInDb)
        {
            await db.Database.ExecuteSqlRawAsync("UPDATE ChecklistItems SET CategoryId = @p0 WHERE Category = @p1 AND CategoryId IS NULL", cat.Id, cat.Name);
        }

        // Set default CategoryId for those that don't match any seeded name
        var firstCatId = categoriesInDb.First().Id;
        await db.Database.ExecuteSqlRawAsync("UPDATE ChecklistItems SET CategoryId = @p0 WHERE CategoryId IS NULL", firstCatId);

        // Finally drop the old column and make CategoryId non-nullable
        await db.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE [ChecklistItems] ALTER COLUMN [CategoryId] int NOT NULL;
                ALTER TABLE [ChecklistItems] DROP COLUMN [Category];
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ChecklistItems_Categories_CategoryId')
                BEGIN
                    ALTER TABLE [ChecklistItems] ADD CONSTRAINT [FK_ChecklistItems_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE;
                END
            ");
    }
    else
    {
        // Even if categories exist, check if we still need to migrate string Category column
        bool hasLegacyColumn = false;
        using (var cmd = db.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE Name = 'Category' AND Object_ID = Object_ID('ChecklistItems')";
            if (db.Database.GetDbConnection().State != System.Data.ConnectionState.Open) await db.Database.GetDbConnection().OpenAsync();
            hasLegacyColumn = (int)(cmd.ExecuteScalar() ?? 0) > 0;
        }

        if (hasLegacyColumn)
        {
            var categoriesInDb = await db.Categories.ToListAsync();
            foreach (var cat in categoriesInDb)
            {
                await db.Database.ExecuteSqlRawAsync("UPDATE ChecklistItems SET CategoryId = @p0 WHERE Category = @p1", cat.Id, cat.Name);
            }

            // Set default CategoryId for those that don't match
            var firstCatId = categoriesInDb.First().Id;
            await db.Database.ExecuteSqlRawAsync("UPDATE ChecklistItems SET CategoryId = @p0 WHERE CategoryId IS NULL", firstCatId);

            await db.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE [ChecklistItems] ALTER COLUMN [CategoryId] int NOT NULL;
                ALTER TABLE [ChecklistItems] DROP COLUMN [Category];
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ChecklistItems_Categories_CategoryId')
                BEGIN
                    ALTER TABLE [ChecklistItems] ADD CONSTRAINT [FK_ChecklistItems_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE;
                END
            ");
        }
    }

    if (!await db.Users.AnyAsync())
    {
        var hashedPassword = HashPassword("admin123");

        var defaultAdmin = new PDTActivityTracker.Components.Model.User
        {
            Username = "admin",
            Password = hashedPassword,
            Email = "015mhikebertillo@gmail.com",
            Role = "Admin",
            Fullname = "System Administrator",
            EmployeeNumber = "ADMIN-001",
            Designation = "System Administrator",
            Status = "Active"
        };

        db.Users.Add(defaultAdmin);
        await db.SaveChangesAsync();

        Console.WriteLine("Default admin user created!");
        Console.WriteLine("Username: admin");
        Console.WriteLine("Password: admin123");
    }
}

static string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(password);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}

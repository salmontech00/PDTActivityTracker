using System.Security.Cryptography;
using System.Text;
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

builder.Services.AddMudServices();
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddScoped<PDTActivityTracker.Components.Services.ThemeService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<UserStateService>();
builder.Services.AddScoped<IEmailService, EmailService>();

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

        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[AccessTags]') AND type in (N'U'))
        BEGIN
            DROP TABLE [AccessTags];
        END

        IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'Tags' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            ALTER TABLE [Users] DROP COLUMN [Tags];
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

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Section' AND Object_ID = Object_ID(N'ChecklistItems'))
        BEGIN
            ALTER TABLE [ChecklistItems] ADD [Section] nvarchar(max) NULL;
        END

        -- Make TaskName nullable for existing tables
        IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'TaskName' AND Object_ID = Object_ID(N'ChecklistItems') AND is_nullable = 0)
        BEGIN
            ALTER TABLE [ChecklistItems] ALTER COLUMN [TaskName] nvarchar(max) NULL;
        END
    ");

    // Seed Categories if empty
    if (!await db.Categories.AnyAsync())
    {
        var defaultCategories = new List<Category>
        {
            new Category { Name = "Opening", Icon = Icons.Material.Filled.DoorFront, ColorClass = "category-opening", SortOrder = 0 },
            new Category { Name = "Asset", Icon = Icons.Material.Filled.Devices, ColorClass = "category-asset", SortOrder = 1 },
            new Category { Name = "Human Resource", Icon = Icons.Material.Filled.Groups, ColorClass = "category-hr", SortOrder = 2 },
            new Category { Name = "Inventory", Icon = Icons.Material.Filled.Inventory, ColorClass = "category-inventory", SortOrder = 3 },
            new Category { Name = "Marketing", Icon = Icons.Material.Filled.Campaign, ColorClass = "category-marketing", SortOrder = 4 },
            new Category { Name = "Expenses & Repair", Icon = Icons.Material.Filled.Build, ColorClass = "category-expenses", SortOrder = 5 },
            new Category { Name = "Closing", Icon = Icons.Material.Filled.DoorBack, ColorClass = "category-closing", SortOrder = 6 }
        };

        foreach (var cat in defaultCategories)
        {
            db.Categories.Add(cat);
        }
        await db.SaveChangesAsync();

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
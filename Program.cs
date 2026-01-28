using System.Security.Cryptography;
using System.Text;
using Blazored.SessionStorage;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using OfficeOpenXml;
using PDTActivityTracker.Components;
using PDTActivityTracker.Components.Services;
using PDTActivityTracker.Model;


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

        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ChecklistItems]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [ChecklistItems] (
                [Id] int NOT NULL IDENTITY(1, 1),
                [TaskName] nvarchar(max) NULL,
                [Description] nvarchar(max) NULL,
                [Category] nvarchar(max) NOT NULL,
                [Section] nvarchar(max) NULL,
                [IsPhotoRequired] bit NOT NULL,
                [NumberOfPhotos] int NOT NULL,
                [IsMandatory] bit NOT NULL,
                [Frequency] nvarchar(max) NOT NULL,
                [IsRemarksRequired] bit NOT NULL,
                [SortOrder] int NOT NULL,
                CONSTRAINT [PK_ChecklistItems] PRIMARY KEY ([Id])
            );
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
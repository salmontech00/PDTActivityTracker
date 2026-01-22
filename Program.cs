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
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[AccessTags]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [AccessTags] (
                [Id] int NOT NULL IDENTITY,
                [Name] nvarchar(max) NOT NULL,
                [Description] nvarchar(max) NULL,
                CONSTRAINT [PK_AccessTags] PRIMARY KEY ([Id])
            );
        END

        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Areas]') AND type in (N'U'))
        BEGIN
            CREATE TABLE [Areas] (
                [Id] int NOT NULL IDENTITY,
                [Name] nvarchar(max) NOT NULL,
                [Type] nvarchar(max) NOT NULL,
                CONSTRAINT [PK_Areas] PRIMARY KEY ([Id])
            );
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Position' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            ALTER TABLE [Users] ADD [Position] nvarchar(max) NULL;
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Area' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            ALTER TABLE [Users] ADD [Area] nvarchar(max) NULL;
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Tags' AND Object_ID = Object_ID(N'Users'))
        BEGIN
            ALTER TABLE [Users] ADD [Tags] nvarchar(max) NULL;
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
            Fullname = "System Administrator"
        };

        db.Users.Add(defaultAdmin);
        await db.SaveChangesAsync();

        Console.WriteLine("Default admin user created!");
        Console.WriteLine("Username: admin");
        Console.WriteLine("Password: admin123");
    }

    // Seed default Access Tags if empty
    if (!await db.AccessTags.AnyAsync())
    {
        db.AccessTags.AddRange(new List<PDTActivityTracker.Components.Model.AccessTag>
        {
            new() { Name = "Read Only", Description = "View only access" },
            new() { Name = "Full Access", Description = "Complete system access" },
            new() { Name = "Data Entry", Description = "Can add/edit basic records" }
        });
        await db.SaveChangesAsync();
        Console.WriteLine("Default Access Tags seeded.");
    }
}

static string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(password);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}
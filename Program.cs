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

    if (!await db.Users.AnyAsync())
    {
        var hashedPassword = HashPassword("admin123");

        var defaultAdmin = new PDTActivityTracker.Components.Model.User
        {
            Username = "admin",
            Password = hashedPassword,
            Email = "admin@tuition.local",
            Role = "Admin",
            Fullname = "System Administrator"
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
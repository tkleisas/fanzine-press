using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FanzinePress.Web.Data;
using FanzinePress.Web.Models;
using FanzinePress.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FanzinePressDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=fanzinepress.db"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<FanzinePressDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

builder.Services.AddRazorPages(options =>
{
    // Allow anonymous access to account pages and image API; require auth for everything else
    options.Conventions.AllowAnonymousToFolder("/Account");
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllers();
builder.Services.AddSingleton<PdfService>();

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FanzinePressDbContext>();
    db.Database.Migrate();

    // Migrate existing files from wwwroot/uploads/ into DB
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var uploadsDir = Path.Combine(env.WebRootPath, "uploads");
    if (Directory.Exists(uploadsDir))
    {
        var migrated = false;

        // Migrate photo images
        var photosToMigrate = db.Photos.Where(p => p.ImageData == null).ToList();
        foreach (var photo in photosToMigrate)
        {
            var filePath = Path.Combine(uploadsDir, photo.FileName);
            if (File.Exists(filePath))
            {
                photo.ImageData = File.ReadAllBytes(filePath);
                photo.ImageContentType = GetContentType(photo.FileName);
                migrated = true;
            }
        }

        // Migrate ad images
        var adsToMigrate = db.Ads.Where(a => a.ImageFileName != null && a.ImageData == null).ToList();
        foreach (var ad in adsToMigrate)
        {
            var filePath = Path.Combine(uploadsDir, ad.ImageFileName!);
            if (File.Exists(filePath))
            {
                ad.ImageData = File.ReadAllBytes(filePath);
                ad.ImageContentType = GetContentType(ad.ImageFileName!);
                migrated = true;
            }
        }

        // Migrate title images
        var issuesToMigrate = db.Issues.Where(i => i.TitleImageFileName != null && i.TitleImageData == null).ToList();
        foreach (var issue in issuesToMigrate)
        {
            var filePath = Path.Combine(uploadsDir, issue.TitleImageFileName!);
            if (File.Exists(filePath))
            {
                issue.TitleImageData = File.ReadAllBytes(filePath);
                issue.TitleImageContentType = GetContentType(issue.TitleImageFileName!);
                migrated = true;
            }
        }

        if (migrated)
        {
            db.SaveChanges();
            Console.WriteLine("Migrated existing image files into database.");
        }
    }

    // Bootstrap roles and admin user
    await BootstrapIdentityAsync(scope.ServiceProvider, db, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static string GetContentType(string fileName)
{
    var ext = Path.GetExtension(fileName).ToLowerInvariant();
    return ext switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".svg" => "image/svg+xml",
        _ => "application/octet-stream"
    };
}

static async Task BootstrapIdentityAsync(IServiceProvider services, FanzinePressDbContext db, IConfiguration configuration)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // Ensure roles exist
    foreach (var roleName in new[] { Roles.Admin, Roles.Editor })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // If no admin exists, create one from configuration / env vars
    var anyAdmin = (await userManager.GetUsersInRoleAsync(Roles.Admin)).Any();
    if (!anyAdmin)
    {
        var adminEmail = configuration["FanzinePress:AdminEmail"]
            ?? Environment.GetEnvironmentVariable("FANZINE_ADMIN_EMAIL")
            ?? "admin@fanzinepress.local";
        var adminPassword = configuration["FanzinePress:AdminPassword"]
            ?? Environment.GetEnvironmentVariable("FANZINE_ADMIN_PASSWORD")
            ?? "ChangeMe123!";

        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "Administrator"
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.Admin);
                Console.WriteLine($"Created admin user: {adminEmail}");
                existing = admin;
            }
            else
            {
                Console.WriteLine("Failed to create admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else if (!await userManager.IsInRoleAsync(existing, Roles.Admin))
        {
            await userManager.AddToRoleAsync(existing, Roles.Admin);
        }

        // Assign any orphan issues (no owner) to this admin
        if (existing != null)
        {
            var orphans = db.Issues.Where(i => i.OwnerId == null).ToList();
            if (orphans.Count > 0)
            {
                foreach (var issue in orphans)
                {
                    issue.OwnerId = existing.Id;
                }
                db.SaveChanges();
                Console.WriteLine($"Assigned {orphans.Count} orphan issue(s) to admin.");
            }
        }
    }
}

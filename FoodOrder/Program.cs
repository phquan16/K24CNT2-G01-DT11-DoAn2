using FoodOrder.Data;
using FoodOrder.Models;
using FoodOrder.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

    var builder = WebApplication.CreateBuilder(args);

    // 1. EF Core + SQL Server (Sử dụng chuỗi kết nối "Default" đã khai báo ở appsettings.json)
    builder.Services.AddDbContext<FoodOrderContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

    // 2. Cấu hình Identity cho nhân viên (Admin, Waiter, Kitchen, Cashier). Khách quét QR không cần đăng nhập.
    builder.Services
        .AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5; // Sai 5 lần sẽ khóa tạm thời
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Khóa trong 5 phút
        })
        .AddEntityFrameworkStores<FoodOrderContext>()
        .AddDefaultTokenProviders(); // Hỗ trợ đặt lại mật khẩu

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

    // 3. Đăng ký Service nghiệp vụ 
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<ITableSessionService, TableSessionService>();
    builder.Services.AddScoped<IMenuService, MenuService>();

// 4. Các dịch vụ mở rộng
    builder.Services.AddSignalR(); 
    builder.Services.AddSwaggerGen(); 
    builder.Services.AddControllersWithViews();

var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication(); // BẮT BUỘC đứng TRƯỚC UseAuthorization
    app.UseAuthorization();

    // Route dành cho khu vực Admin (phải đứng trước route default)
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    // Route mặc định
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
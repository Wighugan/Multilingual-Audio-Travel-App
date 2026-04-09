using Microsoft.EntityFrameworkCore;
using TravelApp.WebAdmin.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thêm hỗ trợ Controller
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

// 1. CẤU HÌNH CORS (Bắt buộc để HTML có thể lấy được dữ liệu từ API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
/*    app.UseSwagger();
    app.UseSwaggerUI();*/
}

app.UseCors("AllowAll");
app.UseDefaultFiles(); // Tự động tìm file index.html
app.UseStaticFiles();  // Cho phép đọc file tĩnh (ảnh, css)

app.UseAuthorization();
app.MapControllers();

app.Run();
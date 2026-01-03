using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Banco de Dados (SQLite)
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configuração de Autenticação por Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login"; // Rota de login
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // Para você não ter que logar todo dia
    });

// 3. Configuração de Sessão (Importante para o Admin)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2); // Tempo que o GM pode ficar inativo
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. Serviços Essenciais
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 5. Pipeline de Execução
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS é recomendado para produção
    app.UseHsts();
}

// Ordem correta dos Middlewares
app.UseStaticFiles();
app.UseRouting();

app.UseSession(); // Sessão deve vir antes da Autenticação/Autorização

app.UseAuthentication(); // Habilita o reconhecimento de quem está logado
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
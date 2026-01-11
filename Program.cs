using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using PortalDMPlace.Functions;
using PortalDMPlace.Enumerators;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Banco de Dados (SQLite)
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configuração de Autenticação por Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login"; // Onde o site manda se não estiver logado
        options.AccessDeniedPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.Cookie.Name = "DMPlaceAuth"; // Nome do cookie no navegador
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

builder.Services.AddScoped<HelpersFunctions>();
builder.Services.AddScoped<AccountObject>();

var app = builder.Build();

// --- RITUAL DE INICIALIZAÇÃO DO BANCO ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DataContext>();
        
        // Garante que o banco e as tabelas existam (roda as migrations pendentes)
        context.Database.Migrate();

        // Verifica se já existe um desenvolvedor para não duplicar
        if (!context.Accounts.Any(a => a.AccountType == AccountType.Developer))
        {
            var devAccount = new Account
            {
                Username = "Desenvolvedor",
                // Hash fixo para "Dev123!" gerado uma única vez
                HashPassword = BCrypt.Net.BCrypt.HashPassword("Dev123!"),
                Name = "O Criador",
                CreatedAt = DateTime.Now,
                BirthDate = new DateTime(2000, 1, 1),
                AccountType = AccountType.Developer
            };

            context.Accounts.Add(devAccount);
            context.SaveChanges();
            
            Console.WriteLine("✅ A conta do Criador foi forjada com sucesso!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro ao iniciar a realidade: {ex.Message}");
    }
}

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

// 2. Chame o seu Middleware customizado AQUI
app.UseMiddleware<UserSessionMiddleware>();

// 1. Rota Padrão (HomeController)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 2. Rota Dinâmica para Campanhas (CampanhaController)
app.MapControllerRoute(
    name: "campanha",
    pattern: "C/{slug}/{action=Index}/{id?}",
    defaults: new { controller = "Campanha" });

app.Run();
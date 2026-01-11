using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;

namespace PortalDMPlace.Functions
{
    public class UserSessionMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context, DataContext dbContext, AccountObject accountObject)
        {
            // 1. Otimização: Não roda se for arquivo estático (CSS, JS, Imagens)
            if (context.Request.Path.StartsWithSegments("/img") || 
                context.Request.Path.StartsWithSegments("/css") || 
                context.Request.Path.StartsWithSegments("/js"))
            {
                await _next(context);
                return;
            }

            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdClaim, out int userId))
                {
                    // Verificação: Só vai no banco se o objeto estiver vazio
                    if (accountObject.Id == 0) 
                    {
                        var user = await dbContext.Accounts
                            .AsNoTracking()
                            .FirstOrDefaultAsync(a => a.Id == userId);

                        if (user != null)
                        {
                            Console.WriteLine($"[UserSessionMiddleware] Carregando dados do usuário {user.Username} (ID: {user.Id}) na sessão.");
                            accountObject.LoadUserData(user);
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
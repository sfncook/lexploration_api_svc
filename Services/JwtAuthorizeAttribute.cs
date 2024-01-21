using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SalesBotApi.Models;


public class JwtAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (token != null)
        {
            JwtPayload jwtPayload = JwtValidator.ValidateAndDecodeToken(token, "foo-bar-001");
            if (jwtPayload != null)
            {
                context.HttpContext.Items["UserData"] = jwtPayload;
                await next();
            }
            else
            {
                context.Result = new UnauthorizedResult();
            }
        }
        else
        {
            context.Result = new UnauthorizedResult();
        }
    }
}

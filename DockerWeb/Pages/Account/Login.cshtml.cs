using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DockerWeb.Helper;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using System.ComponentModel;
using System;
using Microsoft.Extensions.Options;

namespace DockerWeb.Pages.Account
{
    [BindProperties]
    public class LoginModel : PageModel
    {
        //readonly User _user;

        [DisplayName("UserName"), Required, StringLength(20, MinimumLength = 3)]
        public string UserName { get; set; }
        [DisplayName("Password"), Required, StringLength(30, MinimumLength = 3), DataType(DataType.Password)]
        public string Password { get; set; }

        //public LoginModel(IOptions<User> options)
        //{
        //    _user = options.Value;
        //}

        public async Task OnGetAsync()
        {
            // Clear the existing external cookie
            #region 先清除Cookkie

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            #endregion
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                if (!(UserName == Model.User.UserName && Password == Model.User.Password))
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }

                #region snippet1

                #region 使用JwtClaimTypes

                var claimsIdentity = new ClaimsIdentity("Cookie", JwtClaimTypes.Name, JwtClaimTypes.Role);
                claimsIdentity.AddClaim(new Claim(JwtClaimTypes.Name, Model.User.UserName));

                #endregion

                #region 微软内置的ClaimTypes，暂注释

                //var claims = new List<Claim>
                //{
                //    new Claim(JwtClaimTypes.Name, user.Email),
                //    new Claim("FullName", user.FullName),
                //    new Claim(ClaimTypes.Role, "Administrator"),
                //};

                //var claimsIdentity = new ClaimsIdentity(
                //    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                #endregion

                //下列配置，会覆盖掉以前通过 Cookie设置选项 的配置
                var authProperties = new AuthenticationProperties
                {
                    //AllowRefresh = <bool>,
                    // Refreshing the authentication session should be allowed.

                    // 指定过期时间
                    ExpiresUtc = DateTime.UtcNow.AddHours(12),//DateTimeOffset.UtcNow.AddHours(12),
                    // The time at which the authentication ticket expires. A 
                    // value set here overrides the ExpireTimeSpan option of 
                    // CookieAuthenticationOptions set with AddCookie.

                    // 持久保存
                    IsPersistent = true,
                    // Whether the authentication session is persisted across 
                    // multiple requests. Required when setting the 
                    // ExpireTimeSpan option of CookieAuthenticationOptions 
                    // set with AddCookie. Also required when setting 
                    // ExpiresUtc.

                    //IssuedUtc =  DateTimeOffset.UtcNow,
                    // The time at which the authentication ticket was issued.

                    //RedirectUri = <string>
                    // The full path or absolute URI to be used as an http 
                    // redirect response value.
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
                #endregion

                return LocalRedirect(Url.GetLocalUrl(returnUrl));
            }

            // Something failed. Redisplay the form.
            return Page();
        }

    }

}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DockerWeb.Helper;
using DockerWeb.Model;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.RecurringJobExtensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DockerWeb.DB;
using Microsoft.AspNetCore.DataProtection.Repositories;
using DockerWeb.OtherCS;

namespace DockerWeb
{
    public class Startup
    {
        //public Startup(IConfiguration configuration)
        //{
        //    Configuration = configuration;

        //    var builder = new ConfigurationBuilder();
        //    builder.SetBasePath(Directory.GetCurrentDirectory());
        //    builder.AddJsonFile("now.json");
        //    var configRoot = builder.Build();

        //    User userEntity = new User();
        //    configRoot.GetSection("env").Bind(userEntity);
        //}

        public Startup(IConfiguration configuration)//(IHostingEnvironment env)
        {
            //var builder = new ConfigurationBuilder()
            //    .SetBasePath(env.ContentRootPath)
            //    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            //    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            //    .AddJsonFile("now.json")
            //    .AddEnvironmentVariables();
            //Configuration = builder.Build();

            //var builder = new ConfigurationBuilder()
            //    .AddEnvironmentVariables();
            //Configuration = builder.Build();

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            var connectionString = Configuration.GetSection("HangfireConnection").Value;

            #region Redis

            //var redis = StackExchange.Redis.ConnectionMultiplexer.Connect(Configuration.GetSection("Redis").Value);
            //services.AddDataProtection().PersistKeysToRedis(redis)
            //    .AddKeyManagementOptions(options => options.XmlEncryptor = new NullXmlEncryptor());

            #endregion

            #region DataBase

            services.AddEntityFrameworkNpgsql().AddDbContext<CoreDataContext>(options => options.UseNpgsql(connectionString));


            // custom entity framework key repository
            services.AddSingleton<IXmlRepository, DataProtectionKeyRepository>();

            // this part is needed in 2.0
            // I'll update the post once/if I find a better way to get the repository instance
            var built = services.BuildServiceProvider();
            services.AddDataProtection().AddKeyManagementOptions(options =>
            {
                options.XmlRepository = built.GetService<IXmlRepository>();
                options.XmlEncryptor = new NullXmlEncryptor();
            });

            #endregion

            User.UserName = Configuration.GetSection("UserName").Value;
            User.Password = Configuration.GetSection("Password").Value;

            TwilioHelper.Init(Configuration.GetSection("TwilioAccountSid").Value, Configuration.GetSection("TwilioAuthToken").Value, Configuration.GetSection("TwilioFromPhoneNumber").Value);

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddMvc()
                .AddRazorPagesOptions(option =>
                {
                    option.Conventions.AuthorizeFolder("/").AllowAnonymousToPage("/Index").AllowAnonymousToPage("/Privacy").AllowAnonymousToPage("/Account/Login");
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
                .AddCookie();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            #region 创建文件

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "recurringjob.json"), Configuration.GetSection("recurringjob").Value);

            #endregion

            services.AddHangfire(x =>
                {
                    x.UsePostgreSqlStorage(connectionString);

                    x.UseRecurringJob("recurringjob.json");

                    x.UseDefaultActivator();
                });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseMvc();

            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                Queues = new[] { "default", "apis", "jobs" }
                //WorkerCount = 5 //默认值：Environment.ProcessorCount * 5
            });
            app.UseHangfireDashboard("/jobs", new DashboardOptions()
            {
                Authorization = new[] { new CustomAuthorizeFilter() }
            });

        }
    }
}

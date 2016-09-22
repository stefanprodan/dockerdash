using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DockerDash
{
    public partial class Startup
    {
        private IHostingEnvironment _env;
        private string jwtKey;
        private string user;
        private string password;

        public Startup(IHostingEnvironment env)
        {
            _env = env;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            jwtKey = Configuration["JWTKey"];
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            services.AddSignalR(options =>
            {
                options.Hubs.EnableDetailedErrors = true;
            });

            if (_env.IsDevelopment())
            {
                user = Configuration["User"];
                password = Configuration["Password"];

                services.Configure<DockerHost>(Configuration.GetSection("DockerHostDev"));
            }
            else
            {
                var dockerAddress = Environment.GetEnvironmentVariable("DOCKER_REMOTE_API");
                if (string.IsNullOrEmpty(dockerAddress))
                {
                    //services.Configure<DockerHost>(Configuration.GetSection("DockerHostTest"));
                    throw new Exception("DOCKER_REMOTE_API environment variable not found");
                }
                else
                {
                    services.Configure<DockerHost>(dockerHost =>
                    {
                        dockerHost.Uri = dockerAddress;
                    });
                }

                user = Environment.GetEnvironmentVariable("DOCKERDASH_USER");
                if (string.IsNullOrEmpty(user))
                {
                    //user = Configuration["User"];
                    throw new Exception("DOCKERDASH_USER environment variable not found");
                }

                password = Environment.GetEnvironmentVariable("DOCKERDASH_PASSWORD");
                if (string.IsNullOrEmpty(password))
                {
                    //password = Configuration["Password"];
                    throw new Exception("DOCKERDASH_PASSWORD environment variable not found");
                }
            }

            services.AddSingleton<DockerService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, DockerService dockerService)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // SignalR hack: get token from QueryString and create Authorization header
            app.Use(async (context, next) =>
            {
                if (string.IsNullOrWhiteSpace(context.Request.Headers["Authorization"]))
                {
                    if (context.Request.QueryString.HasValue)
                    {
                        var token = context.Request.QueryString.Value
                            .Split('&')
                            .SingleOrDefault(x => x.Contains("authorization"))?.Split('=')[1];

                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            context.Request.Headers.Add("Authorization", new[] { $"Bearer {token}" });
                        }
                    }
                }
                await next.Invoke();
            });

            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseSignalR();

            ConfigureAuth(app);

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapRoute(
                    "DeepLink",
                    "{*pathInfo}",
                    defaults: new { controller = "Home", action = "Index" });
            });

            dockerService.MonitorEvents();
        }
    }
}

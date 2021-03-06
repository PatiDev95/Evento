
using Evento.Core.Repositories;
using Evento.Infrastructure.Mappers;
using Evento.Infrastructure.Repositories;
using Evento.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Evento.Infrastructure.Settings;
using Microsoft.IdentityModel.Logging;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;
using Autofac;
using IApplicationLifetime = Microsoft.Extensions.Hosting.IHostApplicationLifetime;
using Evento.Api.Framework;

namespace Evento
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IContainer Conteiner { get; set; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true;
            });
            services.AddMemoryCache();
            services.AddAuthorization(x => x.AddPolicy("HasAdminRole", p=> p.RequireRole("admin")));
            // services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITicketService, TicketService>();
            services.AddScoped<IDataInitializer, DataInitializer>();
            services.AddSingleton(AutoMapperConfig.Initialize());
            services.AddSingleton<IJwtHandler, JwtHandler>();
            services.Configure<JwtSettings>(Configuration.GetSection("JwtSettings"));
            services.Configure<AppSettings>(Configuration.GetSection("app"));


            services.AddLogging(a =>
            {
                a.AddDebug();
                a.AddConfiguration(Configuration.GetSection("Logging"));
                a.AddNLog();
            });

            // Pobranie appsetingsow (appsetings.json)
            var jwtSettingsSection = Configuration.GetSection(typeof(JwtSettings).Name);

            // Zmapowanie appsetings�w na klase AppSettings
            var jwtSettings = jwtSettingsSection.Get<JwtSettings>();

            var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidIssuer = jwtSettings.Issuer
                };
            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<EventRepository>().As<IEventRepository>().InstancePerLifetimeScope();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApplicationLifetime applicationLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseErrorHandler();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            SeedData(app);
            applicationLifetime.ApplicationStopped.Register(() => Conteiner.Dispose()); 
        }

        private void SeedData(IApplicationBuilder app)
        {
            var appSettingsSection = Configuration.GetSection(typeof(AppSettings).Name);
            var settings = appSettingsSection.Get<AppSettings>();
            if(settings.SeedData)
            {
                var dataInitializer = app.ApplicationServices.GetService<IDataInitializer>();
                dataInitializer.SeedAsync();
            }
            
        }
    }
}

using Marten;
using Marten.Events.Daemon.Resiliency;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Weasel.Postgresql;

namespace AspNetCoreWithMarten
{    
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }
      #region sample_StartupConfigureServices

        public void ConfigureServices(IServiceCollection services)
        {
            // This is the absolute, simplest way to integrate Marten into your
            // .Net Core application with Marten's default configuration
            services.AddMarten(options =>
            {
                // Establish the connection string to your Marten database
                options.Connection(Configuration.GetConnectionString("Marten"));

                // If we're running in development mode, let Marten just take care
                // of all necessary schema building and patching behind the scenes
                if (Environment.IsDevelopment())
                {
                    options.AutoCreateSchemaObjects = AutoCreate.All;
                }
            });
        }
        // and other methods we don't care about right now...

        #endregion

      
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", context =>
                {
                    return context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}

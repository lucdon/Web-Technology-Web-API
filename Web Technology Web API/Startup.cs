using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;

public class Startup {
    public IConfiguration configuration { get; }

    public Startup(IConfiguration configuration) {
        this.configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services) {
        Console.WriteLine("ConfigureServices");
        services.AddCors();
        services.AddMvc();

        services.AddSingleton<IMongoDbService, MongoDbService>(mdbs => new MongoDbService(new MongoClientSettings {
            Server = new MongoServerAddress("localhost", 27017),
            ServerSelectionTimeout = TimeSpan.FromSeconds(3)
        }, "webTechDB"));
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
        Console.WriteLine("Configure");
        if (env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
        } else {

            app.UseForwardedHeaders(new ForwardedHeadersOptions {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
        }

        // allow cross origin requests
        app.UseCors(
            options => options.WithOrigins("http://localhost:8080").AllowAnyMethod().AllowAnyHeader()
        );

        app.UseMvc();
    }
}
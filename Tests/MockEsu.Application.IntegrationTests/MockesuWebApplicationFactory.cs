﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MockEsu.Infrastructure.Data;
using System.Diagnostics;

namespace MockEsu.Application.IntegrationTests;

internal class MockesuWebApplicationFactory : WebApplicationFactory<Program>
{
    const string TestConnectionString = "Server=localhost;Port=5433;Database=MockEsu;User ID=postgres;Password=testtest;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseNpgsql(TestConnectionString);
            });
        });
        //CreateSqlScriptForConnection(schemaOnly: false);

        base.ConfigureWebHost(builder);
    }

    private void CreateSqlScriptForConnection(bool schemaOnly)
    {
        string fileName = @"..\..\..\dump-test-database.sql";

        string server = "localhost";
        string port = "5433";
        string database = "MockEsu";
        string userId = "postgres";
        string password = "testtest";

        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = @"D:\Progs\pgAdmin 4\runtime\pg_dump.exe"; ///TODO: change pg_dump to environment
        psi.RedirectStandardOutput = true;
        psi.Arguments = $"--host={server} --port={port} --username={userId} --no-password --dbname={database} --no-owner";
        if (schemaOnly)
            psi.Arguments += "--schema-only";
        psi.UseShellExecute = false;
        psi.Environment["PGPASSWORD"] = password;

        Process process = Process.Start(psi);

        using (StreamReader reader = process.StandardOutput)
        {
            string result = reader.ReadToEnd();
            File.WriteAllText(fileName, result);
        }

        process.WaitForExit();
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PASopa.Commands;
using PASopa.Commands.Handlers;

namespace PASopa
{
    static class Program
    {
        static void Main(string[] args)
        {
            BuildCommandLine()
                .UseHelp()
                .UseHost(HostBuilder)
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            var root = new Root("pasopa");
            return new CommandLineBuilder(root);
        }

        private static void HostBuilder(IHostBuilder builder)
        {
            builder.UseCommandHandler<Test, TestHandler>();
            builder.UseCommandHandler<UnPack, UnpackHandler>();
            builder.UseCommandHandler<Make, MakeHandler>();
            builder.ConfigureServices(ConfigureServices);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole());
        }
    }
}

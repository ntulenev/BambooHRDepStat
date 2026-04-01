using Abstractions;

using BambooHR.Reporting.Utility;

using Infrastructure;

using Logic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Models;

using var cts = new CancellationTokenSource();

var builder = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureServices((hostContext, services) =>
    {
        var options = hostContext.Configuration
            .GetRequiredSection(BambooHrOptions.SectionName)
            .Get<BambooHrOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{BambooHrOptions.SectionName}' is missing.");
        options.Validate();

        _ = services.AddSingleton(options);
        _ = services.AddSingleton(TimeProvider.System);
        _ = services.AddSingleton<IApplication, Application>();
        _ = services.AddSingleton<ILoadingNotifier, ConsoleLoadingNotifier>();
        _ = services.AddSingleton<IAvailabilityWindowProvider, AvailabilityWindowProvider>();
        _ = services.AddSingleton<IEmployeeProfileDirectoryLoader, EmployeeProfileDirectoryLoader>();
        _ = services.AddSingleton<IHierarchyTopologyBuilder, HierarchyTopologyBuilder>();
        _ = services.AddSingleton<IEmployeeAvailabilityResolver, EmployeeAvailabilityResolver>();
        _ = services.AddSingleton<IHierarchyAnalytics, HierarchyAnalytics>();
        _ = services.AddSingleton<IHierarchyReportBuilder, HierarchyReportBuilder>();
        _ = services.AddSingleton<IConsoleReportRenderer, ConsoleReportWriter>();
        _ = services.AddSingleton<HtmlReportFileStore>();
        _ = services.AddSingleton<HtmlContentComposer>();
        _ = services.AddSingleton<HtmlReportLauncher>();
        _ = services.AddSingleton<IHtmlReportRenderer, HtmlReportRenderer>();
        _ = services.AddSingleton<PdfReportFileStore>();
        _ = services.AddSingleton<PdfContentComposer>();
        _ = services.AddSingleton<IPdfReportRenderer, PdfReportRenderer>();
        _ = services.AddSingleton<IReportWriter, CompositeReportWriter>();
        _ = services.AddSingleton(_ =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(
                    $"https://{options.Organization}.bamboohr.com",
                    UriKind.Absolute)
            };

            return client;
        });
        _ = services.AddSingleton<IBambooHrClient, BambooHrClient>();
    });
var host = builder.Build();
using var scope = host.Services.CreateScope();
var app = scope.ServiceProvider.GetRequiredService<IApplication>();
await app.RunAsync(args, cts.Token).ConfigureAwait(false);

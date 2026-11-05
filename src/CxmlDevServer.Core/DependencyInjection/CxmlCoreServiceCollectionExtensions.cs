using Cxml.Protocol.Documents;
using Cxml.Protocol.Parsing;
using Cxml.Protocol.Validation;
using Cxml.Protocol.Writing;
using CxmlDevServer.Core.Catalog;
using CxmlDevServer.Core.Common;
using CxmlDevServer.Core.Handlers;
using CxmlDevServer.Core.Messaging;
using CxmlDevServer.Core.Options;
using CxmlDevServer.Core.Pipeline;
using CxmlDevServer.Core.Pipeline.Steps;
using CxmlDevServer.Core.PunchOut;
using CxmlDevServer.Core.Rules;
using CxmlDevServer.Core.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CxmlDevServer.Core.DependencyInjection;

/// <summary>
/// Registers the Core services and offers the extension seams: <c>AddCxmlTransaction</c>,
/// <c>AddDocumentRule</c> and <c>AddPipelineStep</c>.
/// </summary>
public static class CxmlCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the protocol objects, the inbound pipeline with its built-in steps, rules and handlers
    /// (PunchOutSetupRequest, OrderRequest, ProfileRequest) and the PunchOut, catalog and template services.
    /// The persistence layer (<see cref="Abstractions.ICxmlDbContext"/>) and logging must be registered separately.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration; options are bound from its <c>Cxml</c> section.</param>
    public static IServiceCollection AddCxmlDevServerCore(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.Configure<CxmlServerOptions>(configuration.GetSection(CxmlServerOptions.SectionName));
        return services.AddCxmlDevServerCore();
    }

    /// <summary>Registers the Core services with options configured in code.</summary>
    public static IServiceCollection AddCxmlDevServerCore(this IServiceCollection services, Action<CxmlServerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return services.AddCxmlDevServerCore();
    }

    private static IServiceCollection AddCxmlDevServerCore(this IServiceCollection services)
    {
        services.AddOptions<CxmlServerOptions>();
        services.TryAddSingleton(TimeProvider.System);

        // Protocol: the payload registry collects every parser/writer registered through the seams below.
        services.TryAddSingleton(sp =>
        {
            var registry = CxmlPayloadRegistry.CreateDefault();
            foreach (var parser in sp.GetServices<ICxmlPayloadParser>())
            {
                registry.Register(parser);
            }

            foreach (var writer in sp.GetServices<ICxmlPayloadWriter>())
            {
                registry.Register(writer);
            }

            return registry;
        });
        services.TryAddSingleton(sp => new CxmlReader(sp.GetRequiredService<CxmlPayloadRegistry>()));
        services.TryAddSingleton(sp => new CxmlWriter(sp.GetRequiredService<CxmlPayloadRegistry>()));
        services.TryAddSingleton<CxmlDtdValidator>();

        // Application services.
        services.TryAddScoped<SupplierIdentityService>();
        services.TryAddScoped<CatalogQueryService>();
        services.TryAddScoped<PunchOutOrderMessageBuilder>();
        services.TryAddScoped<PunchOutService>();
        services.TryAddScoped<PunchOutReturnReceiver>();
        services.TryAddScoped<IMessageTemplateService, MessageTemplateService>();
        services.TryAddScoped<CxmlInboundProcessor>();

        // Pipeline, in the order they run (Order decides, registration order does not).
        services.AddPipelineStep<FinalizeStep>();
        services.AddPipelineStep<CaptureStep>();
        services.AddPipelineStep<ParseStep>();
        services.AddPipelineStep<DtdValidateStep>();
        services.AddPipelineStep<AuthenticateStep>();
        services.AddPipelineStep<ScenarioStep>();
        services.AddPipelineStep<RuleValidateStep>();
        services.AddPipelineStep<DispatchStep>();

        // Rules.
        services.AddDocumentRule<TimestampFormatRule>();
        services.AddDocumentRule<OrderDateFormatRule>();
        services.AddDocumentRule<PayloadIdRule>();
        services.AddDocumentRule<BrowserFormPostUrlRule>();
        services.AddDocumentRule<BuyerCookieRule>();
        services.AddDocumentRule<OrderIdRule>();
        services.AddDocumentRule<OrderTotalRule>();
        services.AddDocumentRule<LineNumbersUniqueRule>();
        services.AddDocumentRule<CurrencyCodeRule>();
        services.AddDocumentRule<CountryCodeRule>();
        services.AddDocumentRule<DeploymentModeRule>();

        // Transactions: ProfileRequest goes through the same seam as any transaction added later.
        services.AddCxmlTransaction<PunchOutSetupHandler>();
        services.AddCxmlTransaction<OrderRequestHandler>();
        services.AddCxmlTransaction<ProfileRequestHandler>();
        return services;
    }

    /// <summary>
    /// Registers a transaction whose payload model is already known to the payload registry: just the handler
    /// that answers requests named <see cref="ICxmlRequestHandler.RequestName"/>.
    /// </summary>
    public static IServiceCollection AddCxmlTransaction<THandler>(this IServiceCollection services)
        where THandler : class, ICxmlRequestHandler
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ICxmlRequestHandler, THandler>());
        return services;
    }

    /// <summary>
    /// Registers a new transaction: a payload codec (parser and writer in one class) and the handler that
    /// answers requests carrying the payload.
    /// </summary>
    /// <typeparam name="TPayload">The payload model (documents the transaction; the codec produces it).</typeparam>
    /// <typeparam name="TCodec">Parser and writer for <typeparamref name="TPayload"/>.</typeparam>
    /// <typeparam name="THandler">The request handler.</typeparam>
    public static IServiceCollection AddCxmlTransaction<TPayload, TCodec, THandler>(this IServiceCollection services)
        where TPayload : class, ICxmlPayload
        where TCodec : class, ICxmlPayloadParser, ICxmlPayloadWriter
        where THandler : class, ICxmlRequestHandler
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICxmlPayloadParser, TCodec>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICxmlPayloadWriter, TCodec>());
        return services.AddCxmlTransaction<THandler>();
    }

    /// <summary>Registers a new transaction with separate parser and writer classes.</summary>
    /// <typeparam name="TPayload">The payload model.</typeparam>
    /// <typeparam name="TParser">The parser.</typeparam>
    /// <typeparam name="TWriter">The writer.</typeparam>
    /// <typeparam name="THandler">The request handler.</typeparam>
    public static IServiceCollection AddCxmlTransaction<TPayload, TParser, TWriter, THandler>(this IServiceCollection services)
        where TPayload : class, ICxmlPayload
        where TParser : class, ICxmlPayloadParser
        where TWriter : class, ICxmlPayloadWriter
        where THandler : class, ICxmlRequestHandler
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICxmlPayloadParser, TParser>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICxmlPayloadWriter, TWriter>());
        return services.AddCxmlTransaction<THandler>();
    }

    /// <summary>Registers a business rule that runs for inbound documents.</summary>
    public static IServiceCollection AddDocumentRule<TRule>(this IServiceCollection services)
        where TRule : class, IDocumentRule
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentRule, TRule>());
        return services;
    }

    /// <summary>Registers a custom step of the inbound pipeline; its <see cref="ICxmlPipelineStep.Order"/> decides where it runs.</summary>
    public static IServiceCollection AddPipelineStep<TStep>(this IServiceCollection services)
        where TStep : class, ICxmlPipelineStep
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ICxmlPipelineStep, TStep>());
        return services;
    }
}

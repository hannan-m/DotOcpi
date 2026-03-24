using DotOcpi.Registry;
using DotOcpi.Security;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi;

/// <summary>
/// Builder for configuring DotOcpi services. Returned by
/// <see cref="DotOcpiServiceCollectionExtensions.AddDotOcpi(IServiceCollection, Action{DotOcpiOptions})"/>.
/// </summary>
public sealed class DotOcpiBuilder
{
    /// <summary>
    /// The service collection being configured.
    /// </summary>
    public IServiceCollection Services { get; }

    internal DotOcpiBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Registers <see cref="InMemoryTokenStore"/> as the <see cref="ITokenStore"/> implementation.
    /// Suitable for development and single-instance deployments. State is lost on restart.
    /// </summary>
    public DotOcpiBuilder AddInMemoryTokenStore()
    {
        Services.AddSingleton<ITokenStore, InMemoryTokenStore>();
        return this;
    }

    /// <summary>
    /// Registers a custom <see cref="ITokenStore"/> implementation.
    /// </summary>
    public DotOcpiBuilder AddTokenStore<TStore>()
        where TStore : class, ITokenStore
    {
        Services.AddSingleton<ITokenStore, TStore>();
        return this;
    }

    /// <summary>
    /// Registers <see cref="InMemoryCpoRegistry"/> as the <see cref="ICpoRegistry"/> implementation.
    /// Suitable for development and single-instance deployments. State is lost on restart.
    /// </summary>
    public DotOcpiBuilder AddInMemoryCpoRegistry()
    {
        Services.AddSingleton<ICpoRegistry, InMemoryCpoRegistry>();
        return this;
    }

    /// <summary>
    /// Registers a custom <see cref="ICpoRegistryStore"/> for persistent CPO connection storage.
    /// The in-memory registry is still used as a runtime cache; the store is loaded at startup.
    /// </summary>
    public DotOcpiBuilder AddCpoRegistryStore<TStore>()
        where TStore : class, ICpoRegistryStore
    {
        Services.AddSingleton<ICpoRegistryStore, TStore>();
        return this;
    }

    /// <summary>
    /// Registers a custom <see cref="ITokenProtector"/> implementation for encrypting
    /// outbound CPO tokens at rest. Overrides the default <see cref="PlaintextTokenProtector"/>
    /// (and the Data Protection-backed implementation registered by <c>AddAspNetCoreServer()</c>).
    /// </summary>
    public DotOcpiBuilder AddTokenProtector<TProtector>()
        where TProtector : class, ITokenProtector
    {
        Services.AddSingleton<ITokenProtector, TProtector>();
        return this;
    }
}

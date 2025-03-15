using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

namespace WsdlExMachina.Generator;

/// <summary>
/// Extension methods for registering SOAP clients with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a SOAP client to the service collection.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type.</typeparam>
    /// <typeparam name="TClient">The client implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">The SOAP service endpoint URL.</param>
    /// <param name="configureClient">Optional action to configure the HttpClient.</param>
    /// <param name="configureBuilder">Optional action to configure the HttpClientBuilder.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddSoapClient<TInterface, TClient>(
        this IServiceCollection services,
        string endpoint,
        Action<HttpClient>? configureClient = null,
        Action<IHttpClientBuilder>? configureBuilder = null)
        where TClient : class, TInterface
        where TInterface : class
    {
        // Register the client options
        services.AddSingleton(new SoapClientOptions { Endpoint = endpoint });

        // Register the HTTP client
        var builder = services.AddHttpClient<TInterface, TClient>((serviceProvider, client) =>
        {
            // Configure base address if endpoint is a base URL
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                client.BaseAddress = uri;
            }

            // Apply additional configuration
            configureClient?.Invoke(client);
        });

        // Apply additional builder configuration
        configureBuilder?.Invoke(builder);

        return services;
    }

    /// <summary>
    /// Adds a SOAP client to the service collection with Polly policies.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type.</typeparam>
    /// <typeparam name="TClient">The client implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">The SOAP service endpoint URL.</param>
    /// <param name="configurePolly">Optional action to configure the Polly policies.</param>
    /// <param name="configureClient">Optional action to configure the HttpClient.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddSoapClientWithPolly<TInterface, TClient>(
        this IServiceCollection services,
        string endpoint,
        Action<PollyPolicyOptions>? configurePolly = null,
        Action<HttpClient>? configureClient = null)
        where TClient : class, TInterface
        where TInterface : class
    {
        // Default policy options
        var policyOptions = new PollyPolicyOptions
        {
            RetryCount = 3,
            TimeoutSeconds = 30
        };

        // Apply custom configuration
        configurePolly?.Invoke(policyOptions);

        return services.AddSoapClient<TInterface, TClient>(
            endpoint,
            configureClient,
            builder =>
            {
                // Add timeout policy
                builder.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromSeconds(policyOptions.TimeoutSeconds)));

                // Add retry policy
                builder.AddTransientHttpErrorPolicy(policy => policy
                    .WaitAndRetryAsync(
                        policyOptions.RetryCount,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(retryAttempt, 2))));
            });
    }
}

/// <summary>
/// Options for configuring Polly policies.
/// </summary>
public class PollyPolicyOptions
{
    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Options for configuring a SOAP client.
/// </summary>
public class SoapClientOptions
{
    /// <summary>
    /// Gets or sets the SOAP service endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
}

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using DotOcpi.Security;
using DotOcpi.Simulator.Charging;
using DotOcpi.Simulator.Endpoints;
using DotOcpi.Simulator.Models;
using DotOcpi.Simulator.Push;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DotOcpi.Simulator;

/// <summary>
/// In-memory OCPI-compliant CPO test server. Serves version discovery,
/// credentials exchange, module data, and simulates realistic charging behavior.
/// </summary>
[UnconditionalSuppressMessage(
    "AOT",
    "IL2026:RequiresUnreferencedCode",
    Justification = "Test utility — never published as NativeAOT. Uses reflection-based JSON for flexibility."
)]
[UnconditionalSuppressMessage(
    "AOT",
    "IL3050:RequiresDynamicCode",
    Justification = "Test utility — never published as NativeAOT."
)]
public sealed class OcpiCpoSimulator : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly CpoSimulatorConfiguration _config;
    private readonly SimulatorState _state;
    private readonly PushEngine _pushEngine;
    private readonly CancellationTokenSource _cts = new();
    private Task? _chargingTickTask;

    private OcpiCpoSimulator(
        WebApplication app,
        CpoSimulatorConfiguration config,
        SimulatorState state,
        PushEngine pushEngine,
        string tokenA
    )
    {
        _app = app;
        _config = config;
        _state = state;
        _pushEngine = pushEngine;
        TokenA = tokenA;
    }

    /// <summary>The server configuration.</summary>
    public CpoSimulatorConfiguration Config => _config;

    /// <summary>The pre-shared Token A for initial registration.</summary>
    public string TokenA { get; }

    /// <summary>The base URL of the test server.</summary>
    public Uri BaseUrl => new(_app.Urls.First());

    /// <summary>Returns the Token B issued during the last credentials exchange.</summary>
    public string GetIssuedTokenB() =>
        _state.DefaultConnection?.IssuedTokenB
        ?? throw new InvalidOperationException("No credentials exchange has occurred yet.");

    /// <summary>Returns the Token C received from the eMSP during the last credentials exchange.</summary>
    public string? GetReceivedTokenC() => _state.DefaultConnection?.ReceivedTokenC;

    /// <summary>Tokens received from eMSP via PUT /tokens, keyed by uid.</summary>
    public IReadOnlyDictionary<string, JsonElement> ReceivedTokens => _state.ReceivedTokens;

    /// <summary>Returns a snapshot of all HTTP requests received by this server.</summary>
    public IReadOnlyList<RecordedRequest> GetReceivedRequests() => [.. _state.RequestHistory];

    /// <summary>Clears the request history.</summary>
    public void ClearRequestHistory() => _state.RequestHistory.Clear();

    /// <summary>Waits for all pending async push operations to complete.</summary>
    public Task WaitForPendingCallbacksAsync() => _pushEngine.WaitForPendingAsync(TimeSpan.FromSeconds(5));

    /// <summary>Returns all active sessions (typed mode only).</summary>
    public IReadOnlyList<SessionState> GetActiveSessions() =>
        _state.Sessions.Values.Where(s => s.Phase == SessionPhase.Active).ToList();

    /// <summary>Gets a session by ID (typed mode only).</summary>
    public SessionState? GetSession(string sessionId) => _state.Sessions.TryGetValue(sessionId, out var s) ? s : null;

    /// <summary>Sets an EVSE's status (typed mode, for test setup).</summary>
    public void SetEvseStatus(string locationId, string evseUid, EvseStatus status)
    {
        var key = $"{locationId}:{evseUid}";
        if (_state.Evses.TryGetValue(key, out var evse))
        {
            evse.Update(status);
        }
    }

    /// <summary>Creates and starts a new test CPO server.</summary>
    public static async Task<OcpiCpoSimulator> CreateAsync(Action<CpoSimulatorConfiguration>? configure = null)
    {
        var config = new CpoSimulatorConfiguration();
        configure?.Invoke(config);

        var tokenA = TokenGenerator.Generate();
        var state = new SimulatorState();
        var pushEngine = new PushEngine(config, state);

        // Initialize typed mode state
        if (!config.IsLegacyMode)
        {
            state.SetLocations(config.LocationSpecs);
            state.SetTariffs(config.TariffSpecs);
            state.InitializeEvses(config.LocationSpecs);
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        var app = builder.Build();
        var server = new OcpiCpoSimulator(app, config, state, pushEngine, tokenA);

        // Handle unhandled exceptions (e.g., simulated timeouts) as 500
        app.Use(
            async (ctx, next) =>
            {
                try
                {
                    await next(ctx).ConfigureAwait(false);
                }
                catch (TaskCanceledException) when (!ctx.RequestAborted.IsCancellationRequested)
                {
                    ctx.Response.StatusCode = 500;
                }
            }
        );

        RequestRecordingMiddleware.Use(app, state);
        server.MapEndpoints(app);

        await app.StartAsync().ConfigureAwait(false);

        // Start charging tick timer for typed mode
        if (!config.IsLegacyMode)
            server.StartChargingTick();

        return server;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);

        if (_chargingTickTask is not null)
        {
            try
            {
                await _chargingTickTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            catch
            { /* Timer cancelled via CTS */
            }
        }

        await _pushEngine.DisposeAsync().ConfigureAwait(false);
        await _app.StopAsync().ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
        _state.Dispose();
        _cts.Dispose();
    }

    // ── Endpoint Mapping ────────────────────────────────────────

    private void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var state = _state;
        var config = _config;
        var tokenA = TokenA;
        var self = this;

        endpoints.MapGet(
            "/ocpi/versions",
            (HttpContext ctx) => VersionsHandler.HandleVersions(ctx, state, config, self.BaseUrl)
        );

        endpoints.MapGet(
            "/ocpi/versions/{versionId}",
            (HttpContext ctx, string versionId) =>
                VersionsHandler.HandleVersionDetail(ctx, versionId, state, config, self.BaseUrl)
        );

        endpoints.MapPost(
            "/ocpi/credentials",
            (HttpContext ctx) => CredentialsHandler.HandlePost(ctx, state, config, self.BaseUrl, tokenA)
        );
        endpoints.MapPut(
            "/ocpi/credentials",
            (HttpContext ctx) => CredentialsHandler.HandlePut(ctx, state, config, self.BaseUrl)
        );
        endpoints.MapDelete("/ocpi/credentials", (HttpContext ctx) => CredentialsHandler.HandleDelete(ctx, state));

        // Module endpoints — both flat (2.0/2.1.1) and party-prefixed (2.2+) patterns
        MapModule(endpoints, "locations", (ctx) => LocationsHandler.HandleGetList(ctx, state, config));
        MapModule(endpoints, "tariffs", (ctx) => TariffsHandler.HandleGetList(ctx, state, config));
        MapModule(endpoints, "sessions", (ctx) => SessionsHandler.HandleGetList(ctx, state, config));
        MapModule(endpoints, "cdrs", (ctx) => CdrsHandler.HandleGetList(ctx, state, config));

        // Single-item GET
        MapModuleItem(
            endpoints,
            "locations/{itemId}",
            (ctx, itemId) => LocationsHandler.HandleGetSingle(ctx, itemId, state, config)
        );
        MapModuleItem(
            endpoints,
            "tariffs/{itemId}",
            (ctx, itemId) => TariffsHandler.HandleGetSingle(ctx, itemId, state, config)
        );
        MapModuleItem(
            endpoints,
            "sessions/{itemId}",
            (ctx, itemId) => SessionsHandler.HandleGetSingle(ctx, itemId, state, config)
        );
        MapModuleItem(
            endpoints,
            "cdrs/{itemId}",
            (ctx, itemId) => CdrsHandler.HandleGetSingle(ctx, itemId, state, config)
        );

        // EVSE/Connector sub-resources
        endpoints.MapGet(
            "/ocpi/locations/{locationId}/{evseUid}",
            (HttpContext ctx, string locationId, string evseUid) =>
                LocationsHandler.HandleGetEvse(ctx, locationId, evseUid, state, config)
        );
        endpoints.MapGet(
            "/ocpi/{countryCode}/{partyId}/locations/{locationId}/{evseUid}",
            (HttpContext ctx, string locationId, string evseUid) =>
                LocationsHandler.HandleGetEvse(ctx, locationId, evseUid, state, config)
        );
        endpoints.MapGet(
            "/ocpi/locations/{locationId}/{evseUid}/{connectorId}",
            (HttpContext ctx, string locationId, string evseUid, string connectorId) =>
                LocationsHandler.HandleGetConnector(ctx, locationId, evseUid, connectorId, state, config)
        );
        endpoints.MapGet(
            "/ocpi/{countryCode}/{partyId}/locations/{locationId}/{evseUid}/{connectorId}",
            (HttpContext ctx, string locationId, string evseUid, string connectorId) =>
                LocationsHandler.HandleGetConnector(ctx, locationId, evseUid, connectorId, state, config)
        );

        // Tokens
        MapModulePut(
            endpoints,
            "tokens/{tokenUid}",
            (ctx, tokenUid) => TokensHandler.HandlePut(ctx, tokenUid, state, config)
        );
        MapModulePatch(
            endpoints,
            "tokens/{tokenUid}",
            (ctx, tokenUid) => TokensHandler.HandlePatch(ctx, tokenUid, state, config)
        );
        MapModulePost(
            endpoints,
            "tokens/{tokenUid}/authorize",
            (ctx, tokenUid) => TokensHandler.HandleAuthorize(ctx, tokenUid, state, config)
        );

        // Commands
        Action<string, string?, object?> onSessionEvent = HandleSessionEvent;
        string[] commandTypes =
        [
            "START_SESSION",
            "STOP_SESSION",
            "RESERVE_NOW",
            "UNLOCK_CONNECTOR",
            "CANCEL_RESERVATION",
        ];
        foreach (var cmd in commandTypes)
        {
            var captured = onSessionEvent;
            endpoints.MapPost(
                $"/ocpi/commands/{cmd}",
                (HttpContext ctx) => CommandsHandler.HandleCommand(ctx, state, config, captured)
            );
            endpoints.MapPost(
                $"/ocpi/{{countryCode}}/{{partyId}}/commands/{cmd}",
                (HttpContext ctx) => CommandsHandler.HandleCommand(ctx, state, config, captured)
            );
        }

        // Charging profiles
        MapModulePut(
            endpoints,
            "charging_profiles/{sessionId}",
            (ctx, sessionId) => ChargingProfilesHandler.HandlePut(ctx, sessionId, state, config)
        );
        MapModuleDelete(
            endpoints,
            "charging_profiles/{sessionId}",
            (ctx, sessionId) => ChargingProfilesHandler.HandleDelete(ctx, sessionId, state, config)
        );
        MapModuleItem(
            endpoints,
            "charging_profiles/{sessionId}",
            (ctx, sessionId) => ChargingProfilesHandler.HandleGet(ctx, sessionId, state, config)
        );
    }

    private static void MapModule(IEndpointRouteBuilder endpoints, string path, Func<HttpContext, Task> handler)
    {
        endpoints.MapGet($"/ocpi/{path}", handler);
        endpoints.MapGet($"/ocpi/{{countryCode}}/{{partyId}}/{path}", handler);
    }

    private static void MapModuleWithParam(
        IEndpointRouteBuilder endpoints,
        string method,
        string path,
        Func<HttpContext, string, Task> handler
    )
    {
        var paramName = path.Split('/')[^1].Trim('{', '}');
        // For sub-paths like tokens/{tokenUid}/authorize — param is not the last segment
        if (path.Contains('/') && !path.EndsWith('}'))
        {
            var parts = path.Split('/');
            paramName = parts.First(p => p.StartsWith('{') && p.EndsWith('}')).Trim('{', '}');
        }

        RequestDelegate handler2 = ctx =>
        {
            var value = ctx.GetRouteValue(paramName)?.ToString() ?? "";
            return handler(ctx, value);
        };

        var flatPath = $"/ocpi/{path}";
        var partyPath = $"/ocpi/{{countryCode}}/{{partyId}}/{path}";

        switch (method)
        {
            case "GET":
                endpoints.MapGet(flatPath, handler2);
                endpoints.MapGet(partyPath, handler2);
                break;
            case "POST":
                endpoints.MapPost(flatPath, handler2);
                endpoints.MapPost(partyPath, handler2);
                break;
            case "PUT":
                endpoints.MapPut(flatPath, handler2);
                endpoints.MapPut(partyPath, handler2);
                break;
            case "PATCH":
                endpoints.MapPatch(flatPath, handler2);
                endpoints.MapPatch(partyPath, handler2);
                break;
            case "DELETE":
                endpoints.MapDelete(flatPath, handler2);
                endpoints.MapDelete(partyPath, handler2);
                break;
        }
    }

    private static void MapModuleItem(
        IEndpointRouteBuilder endpoints,
        string path,
        Func<HttpContext, string, Task> handler
    ) => MapModuleWithParam(endpoints, "GET", path, handler);

    private static void MapModulePost(
        IEndpointRouteBuilder endpoints,
        string path,
        Func<HttpContext, string, Task> handler
    ) => MapModuleWithParam(endpoints, "POST", path, handler);

    private static void MapModulePut(
        IEndpointRouteBuilder endpoints,
        string path,
        Func<HttpContext, string, Task> handler
    ) => MapModuleWithParam(endpoints, "PUT", path, handler);

    private static void MapModulePatch(
        IEndpointRouteBuilder endpoints,
        string path,
        Func<HttpContext, string, Task> handler
    ) => MapModuleWithParam(endpoints, "PATCH", path, handler);

    private static void MapModuleDelete(
        IEndpointRouteBuilder endpoints,
        string path,
        Func<HttpContext, string, Task> handler
    ) => MapModuleWithParam(endpoints, "DELETE", path, handler);

    // ── Session event handling ───────────────────────────────────

    private void HandleSessionEvent(string eventType, string? id, object? data)
    {
        switch (eventType)
        {
            case "COMMAND_CALLBACK" when id is not null && data is string status:
                _pushEngine.EnqueueCommandCallback(id, status, _config.CommandCallbackDelay);
                break;

            case "SESSION_STARTED" when id is not null:
                // Push will happen on next tick
                break;

            case "SESSION_STOPPED" when id is not null && data is not null:
                _pushEngine.EnqueueCdrPush(data);
                break;
        }
    }

    // ── Charging tick timer ──────────────────────────────────────

    private void StartChargingTick()
    {
        _chargingTickTask = Task.Run(async () =>
        {
            var ct = _cts.Token;
            using var timer = new PeriodicTimer(_config.ChargingTickInterval);
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                TickActiveSessions();
            }
        });
    }

    private void TickActiveSessions()
    {
        foreach (var session in _state.Sessions.Values)
        {
            if (session.Phase != SessionPhase.Active)
                continue;

            var reachedTarget = ChargingSimulation.Tick(
                session,
                _config.DefaultChargingProfile,
                _config.ChargingTickInterval
            );

            // Build model under the session lock to prevent torn reads after Tick releases it
            if (_config.PushEnabled && _config.PushSessionUpdates)
            {
                var conn = _state.DefaultConnection;
                var version = conn?.NegotiatedVersion ?? _config.SupportedVersions[0];
                var loc = _state.FindLocation(session.LocationId) ?? LocationSpec.Default;
                var model = session.WithLock(_ =>
                    VersionModelBuilder.BuildSession(
                        version,
                        session,
                        _config.CpoIdentity,
                        _config.EmspIdentity,
                        loc,
                        _config.CpoCurrency
                    )
                );
                _pushEngine.EnqueueSessionPush(session.SessionId, model, "PATCH");
            }

            if (reachedTarget && _config.AutoStopOnTargetKwh)
            {
                // Build CDR model and finalize session under a single lock acquisition
                var cdrConn = _state.DefaultConnection;
                var cdrVersion = cdrConn?.NegotiatedVersion ?? _config.SupportedVersions[0];
                var cdrLoc = _state.FindLocation(session.LocationId) ?? LocationSpec.Default;
                var cdr = session.WithLock(s =>
                {
                    s.Phase = SessionPhase.Completed;
                    s.EndTime = DateTimeOffset.UtcNow;
                    s.LastUpdated = DateTimeOffset.UtcNow;

                    return VersionModelBuilder.BuildCdr(
                        cdrVersion,
                        session,
                        _config.CpoIdentity,
                        _config.EmspIdentity,
                        cdrLoc,
                        _config.CpoCurrency,
                        _config.DefaultVatRate
                    );
                });

                var evseKey = $"{session.LocationId}:{session.EvseUid}";
                if (_state.Evses.TryGetValue(evseKey, out var evseState))
                {
                    evseState.ClearSession(EvseStatus.Available);
                }

                lock (_config.Cdrs)
                {
                    _config.Cdrs.Add(cdr);
                }

                _pushEngine.EnqueueCdrPush(cdr);
            }
        }
    }
}

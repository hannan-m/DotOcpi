using System.Text.Json;
using DotOcpi;
using DotOcpi.AspNetCore;
using DotOcpi.AspNetCore.Routing;
using DotOcpi.Client;
using DotOcpi.Client.Internal;
using DotOcpi.Client.Sync;
using DotOcpi.Models.V2_2_1;
using DotOcpi.Modules;
using DotOcpi.Registration;
using DotOcpi.Registry;
using DotOcpi.Sample.Handlers;
using DotOcpi.Sample.Services;
using DotOcpi.Security;
using DotOcpi.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── Dashboard URL ────────────────────────────────────────────
builder.WebHost.UseUrls("http://localhost:5050");

// ── Logging ──────────────────────────────────────────────────
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("DotOcpi.Sample", LogLevel.Information);
builder.Logging.AddFilter("DotOcpi.Client.Sync", LogLevel.Information);

// ── Razor Pages + OpenAPI ─────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "DotOcpi Sample — Internal API",
            Version = "v1",
            Description = "Backend API endpoints for the DotOcpi eMSP dashboard",
        }
    );
});

// ── DotOcpi ──────────────────────────────────────────────────
builder
    .Services.AddDotOcpi(options =>
    {
        options.SupportedVersions = [OcpiVersion.V2_2_1, OcpiVersion.V2_1_1];
        options.DefaultEmspIdentity = new PartyIdentity("NL", "MSP");
    })
    .AddInMemoryTokenStore()
    .AddInMemoryCpoRegistry()
    .AddClient()
    .AddAspNetCoreServer()
    .AddSyncHandler<SampleSyncHandler>()
    .AddPullSync(opts =>
    {
        opts.DefaultInterval = TimeSpan.FromSeconds(30);
        opts.EnabledModules = ["locations", "tariffs"];
        opts.MaxJitter = TimeSpan.FromSeconds(5);
    });

// SSRF override for localhost demo
builder
    .Services.AddHttpClient(OcpiHttpClientConfiguration.HttpClientName)
    .ConfigurePrimaryHttpMessageHandler(static () =>
        new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5), MaxConnectionsPerServer = 20 }
    );

// ── Application Services ─────────────────────────────────────
builder.Services.AddSingleton<InMemoryDataStore>();
builder.Services.AddSingleton<MetricsCollector>();
builder.Services.AddSingleton<SseEventService>();

// CPO Simulator (auto-starts test CPOs)
builder.Services.AddSingleton<CpoSimulator>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CpoSimulator>());

// Auto-registration service
builder.Services.AddHostedService<AutoRegistrationService>();

// ── Command History ──────────────────────────────────────────
builder.Services.AddSingleton<CommandHistory>();

// ── OCPI Handlers ────────────────────────────────────────────
builder.Services.AddSingleton<ILocationsReceiver, SampleLocationsReceiver>();
builder.Services.AddSingleton<ISessionsReceiver, SampleSessionsReceiver>();
builder.Services.AddSingleton<ICdrsReceiver, SampleCdrsReceiver>();
builder.Services.AddSingleton<ITariffsReceiver, SampleTariffsReceiver>();
builder.Services.AddSingleton<ITokensAuthorizer, SampleTokensAuthorizer>();
builder.Services.AddSingleton<ITokensSender, SampleTokensSender>();
builder.Services.AddSingleton<ICommandsCallback, SampleCommandsCallback>();
builder.Services.AddSingleton<IChargingProfilesCallback, SampleChargingProfilesCallback>();

// ── Outbound Token Provider ──────────────────────────────────
builder.Services.AddSingleton<SampleTokenProvider>();
builder.Services.AddSingleton<IOutboundTokenProvider>(sp => sp.GetRequiredService<SampleTokenProvider>());

var app = builder.Build();

// ── Static Files + Routing ───────────────────────────────────
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DotOcpi Sample API v1"));
app.UseRouting();
app.MapRazorPages();
app.MapAllOcpiEndpoints();

// ── API: SSE Events ──────────────────────────────────────────

app.MapGet(
    "/api/events",
    async (SseEventService sse, HttpContext ctx) =>
    {
        ctx.Response.ContentType = "text/event-stream";
        ctx.Response.Headers.CacheControl = "no-cache";
        ctx.Response.Headers.Connection = "keep-alive";

        var clientId = sse.AddClient(ctx.Response.Body);
        try
        {
            await ctx.Response.WriteAsync($"event: connected\ndata: {{\"clientId\":\"{clientId}\"}}\n\n");
            await ctx.Response.Body.FlushAsync();

            // Keep connection alive until client disconnects
            await Task.Delay(Timeout.Infinite, ctx.RequestAborted);
        }
        catch (OperationCanceledException) { }
        finally
        {
            sse.RemoveClient(clientId);
        }
    }
);

// ── API: Metrics ─────────────────────────────────────────────

app.MapGet(
    "/api/metrics",
    (ICpoRegistry registry, InMemoryDataStore store, MetricsCollector metrics) =>
        Results.Ok(
            new
            {
                cpoCount = registry.GetAll().Count,
                totalSyncs = metrics.TotalSyncs,
                totalErrors = metrics.TotalErrors,
                dataStore = new
                {
                    locations = store.Locations.Count,
                    sessions = store.Sessions.Count,
                    tariffs = store.Tariffs.Count,
                    cdrs = store.Cdrs.Count,
                },
            }
        )
);

// ── API: CPO Registration ────────────────────────────────────

app.MapPost(
    "/api/cpos/register",
    async (
        HttpContext ctx,
        IVersionDiscovery versionDiscovery,
        ICredentialsClient credentialsClient,
        ICpoRegistry registry,
        ITokenStore tokenStore,
        SampleTokenProvider tokenProvider
    ) =>
    {
        var f = await ctx.Request.ReadFormAsync();
        var versionsUrl = f["versionsUrl"].ToString();
        var tokenA = f["tokenA"].ToString();

        if (string.IsNullOrEmpty(versionsUrl) || string.IsNullOrEmpty(tokenA))
            return Results.Content(
                "<span class='badge badge-error'>versionsUrl and tokenA are required.</span>",
                "text/html"
            );

        try
        {
            var versions = await versionDiscovery.GetVersionsAsync(versionsUrl, tokenA);
            var negotiated = VersionNegotiator.Negotiate(versions.Select(v => v.Version));
            if (negotiated is null)
                return Results.BadRequest("No mutually supported OCPI version.");

            var versionEntry = versions.First(v => v.Version == negotiated.Value.ToVersionString());
            var detail = await versionDiscovery.GetVersionDetailAsync(versionEntry.Url, tokenA);

            var credentialsUrl = detail
                .Endpoints.First(e => string.Equals(e.Identifier, "credentials", StringComparison.OrdinalIgnoreCase))
                .Url;

            var ourTokenB = TokenGenerator.Generate();
            var ourTokenBHash = TokenHasher.Hash(ourTokenB);
            var credentials = CredentialsHelper.Build(ourTokenB, negotiated.Value);

            var cpoResponse = await credentialsClient.PostCredentialsAsync(
                credentialsUrl,
                tokenA,
                negotiated.Value,
                credentials
            );

            await tokenStore.StoreAsync(ourTokenBHash, TokenPurpose.TokenB, "NL:MSP");

            var moduleEndpoints = detail
                .Endpoints.Where(e => !string.Equals(e.Identifier, "credentials", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(e => e.Identifier, e => e.Url, StringComparer.OrdinalIgnoreCase);

            var now = DateTimeOffset.UtcNow;
            var connection = new CpoConnection
            {
                CpoCountryCode = cpoResponse.CountryCode,
                CpoPartyId = cpoResponse.PartyId,
                EmspCountryCode = "NL",
                EmspPartyId = "MSP",
                Version = negotiated.Value,
                ModuleEndpoints = moduleEndpoints,
                TokenBHash = ourTokenBHash,
                CpoVersionsUrl = versionsUrl,
                EmspVersionsUrl = "https://emsp.example.com/ocpi/versions",
                Status = ConnectionStatus.Connected,
                CreatedAt = now,
                UpdatedAt = now,
            };

            registry.AddOrUpdate(connection);
            tokenProvider.StoreToken(connection.ConnectionKey, cpoResponse.Token);

            return Results.Content(
                $"<span class='badge badge-success'>Registered {connection.ConnectionKey} (OCPI {negotiated.Value.ToVersionString()})</span>",
                "text/html"
            );
        }
        catch (Exception ex)
        {
            return Results.Content($"<span class='badge badge-error'>Failed: {ex.Message}</span>", "text/html");
        }
    }
);

// ── API: Commands ────────────────────────────────────────────

app.MapPost(
    "/api/commands/start-session",
    async (
        HttpContext ctx,
        IOcpiClient ocpiClient,
        ICpoRegistry registry,
        IOcpiSyncService syncService,
        CommandHistory cmdHistory
    ) =>
    {
        var f = await ctx.Request.ReadFormAsync();
        var cpoId = f["cpoId"].ToString();
        var locationId = f["locationId"].ToString();
        var evseUid = f["evseUid"].ToString();
        var tokenUid = f["tokenUid"].ToString();

        if (string.IsNullOrEmpty(cpoId))
            cpoId = "DE:CPO";
        if (string.IsNullOrEmpty(locationId))
            locationId = "LOC1";
        if (string.IsNullOrEmpty(evseUid))
            evseUid = "DE*CPO*E001";
        if (string.IsNullOrEmpty(tokenUid))
            tokenUid = "TOKEN001";

        try
        {
            var connection = registry.FindByConnectionKey(cpoId);
            var version = connection?.Version ?? OcpiVersion.V2_2_1;

            // Build version-appropriate command model
            object command = version.UsesPartyIdInUrls()
                ? new StartSession
                {
                    ResponseUrl = "https://emsp.example.com/ocpi/2.2.1/commands/CORR-001",
                    Token = new Token
                    {
                        CountryCode = new CiString("NL"),
                        PartyId = new CiString("MSP"),
                        Uid = new CiString(tokenUid),
                        Type = TokenType.RFID,
                        ContractId = new CiString("NL-MSP-000001"),
                        Issuer = "DotOcpi Sample eMSP",
                        Valid = true,
                        Whitelist = WhitelistType.ALWAYS,
                        LastUpdated = DateTimeOffset.UtcNow,
                    },
                    LocationId = new CiString(locationId),
                    EvseUid = new CiString(evseUid),
                }
                : new DotOcpi.Models.V2_1_1.StartSession
                {
                    ResponseUrl = "https://emsp.example.com/ocpi/2.1.1/commands/CORR-001",
                    Token = new DotOcpi.Models.V2_1_1.Token
                    {
                        Uid = tokenUid,
                        Type = DotOcpi.Models.V2_1_1.TokenType.RFID,
                        AuthId = "NL-MSP-000001",
                        Issuer = "DotOcpi Sample eMSP",
                        Valid = true,
                        Whitelist = DotOcpi.Models.V2_1_1.WhitelistType.ALWAYS,
                        LastUpdated = DateTimeOffset.UtcNow,
                    },
                    LocationId = locationId,
                    EvseUid = evseUid,
                };

            var result = await ocpiClient.Commands.SendStartSessionAsync(cpoId, command);
            var status = result.IsSuccess ? "ACCEPTED" : "FAILED";
            cmdHistory.Add(cpoId, "START_SESSION", status);

            // Sync sessions so the new session appears in the data store
            if (result.IsSuccess)
            {
                await Task.Delay(300); // Brief delay for CPO to create session
                await syncService.SyncModuleFromCpoAsync(cpoId, "sessions");
            }

            return Results.Content(
                result.IsSuccess
                    ? "<div class='result-box'><span class='badge badge-success'>ACCEPTED</span> Session starting — live progress below</div>"
                    : $"<div class='result-box'><span class='badge badge-error'>FAILED</span> {result.StatusMessage}</div>",
                "text/html"
            );
        }
        catch (Exception ex)
        {
            cmdHistory.Add(cpoId, "START_SESSION", "ERROR");
            return Results.Content(
                $"<div class='result-box'><span class='badge badge-error'>ERROR</span> {ex.Message}</div>",
                "text/html"
            );
        }
    }
);

app.MapPost(
    "/api/commands/stop-session",
    async (
        HttpContext ctx,
        IOcpiClient ocpiClient,
        ICpoRegistry registry,
        IOcpiSyncService syncService,
        CommandHistory cmdHistory
    ) =>
    {
        var f = await ctx.Request.ReadFormAsync();
        var cpoId = f["cpoId"].ToString();
        var sessionId = f["sessionId"].ToString();

        if (string.IsNullOrEmpty(cpoId))
            cpoId = "DE:CPO";
        if (string.IsNullOrEmpty(sessionId))
            sessionId = "SES001";

        try
        {
            var connection = registry.FindByConnectionKey(cpoId);
            var version = connection?.Version ?? OcpiVersion.V2_2_1;

            object command = version.UsesPartyIdInUrls()
                ? new StopSession
                {
                    ResponseUrl = "https://emsp.example.com/ocpi/2.2.1/commands/CORR-002",
                    SessionId = new CiString(sessionId),
                }
                : new DotOcpi.Models.V2_1_1.StopSession
                {
                    ResponseUrl = "https://emsp.example.com/ocpi/2.1.1/commands/CORR-002",
                    SessionId = sessionId,
                };

            var result = await ocpiClient.Commands.SendStopSessionAsync(cpoId, command);
            var status = result.IsSuccess ? "ACCEPTED" : "FAILED";
            cmdHistory.Add(cpoId, "STOP_SESSION", status);

            // Sync sessions + CDRs so the completed status and CDR appear
            if (result.IsSuccess)
            {
                await Task.Delay(300);
                await syncService.SyncModuleFromCpoAsync(cpoId, "sessions");
                await syncService.SyncModuleFromCpoAsync(cpoId, "cdrs");
            }

            return Results.Content(
                result.IsSuccess
                    ? "<div class='result-box'><span class='badge badge-success'>ACCEPTED</span> Session stopped — CDR generated</div>"
                    : $"<div class='result-box'><span class='badge badge-error'>FAILED</span> {result.StatusMessage}</div>",
                "text/html"
            );
        }
        catch (Exception ex)
        {
            cmdHistory.Add(cpoId, "STOP_SESSION", "ERROR");
            return Results.Content(
                $"<div class='result-box'><span class='badge badge-error'>ERROR</span> {ex.Message}</div>",
                "text/html"
            );
        }
    }
);

// ── API: Per-EVSE live session (for inline display on Locations page) ─

app.MapGet(
    "/api/evse-session",
    (HttpContext ctx, ICpoRegistry registry, InMemoryDataStore store) =>
    {
        var cpoId = ctx.Request.Query["cpoId"].ToString();
        var evseUid = ctx.Request.Query["evseUid"].ToString();
        if (string.IsNullOrEmpty(cpoId) || string.IsNullOrEmpty(evseUid))
            return Results.Content("", "text/html");

        var version = registry.FindByConnectionKey(cpoId)?.Version ?? OcpiVersion.V2_2_1;
        var opts = OcpiJsonOptions.GetOptions(version);

        // Find active or recently completed sessions for this EVSE
        var matchingSessions = store
            .Sessions.Where(kv => kv.Key.StartsWith(cpoId + ":"))
            .Select(kv =>
            {
                var json = JsonSerializer.Serialize(kv.Value, opts.GetTypeInfo(kv.Value.GetType()));
                return JsonDocument.Parse(json).RootElement.Clone();
            })
            .Where(ses =>
            {
                var uid = ExtractEvseUid(ses);
                if (uid != evseUid)
                    return false;
                var status = ses.TryGetProperty("status", out var s) ? s.GetString() : null;
                return status is "ACTIVE" or "PENDING";
            })
            .ToList();

        if (matchingSessions.Count == 0)
            return Results.Content("", "text/html");

        var sb = new System.Text.StringBuilder();
        foreach (var ses in matchingSessions)
        {
            var id = ses.TryGetProperty("id", out var idP) ? idP.GetString() : "—";
            var status = ses.TryGetProperty("status", out var stP) ? stP.GetString() : "—";
            var kwh = ses.TryGetProperty("kwh", out var kwhP)
                ? kwhP.GetDecimal().ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
                : "0.0";
            var cost = "—";
            if (ses.TryGetProperty("total_cost", out var costP))
            {
                if (costP.ValueKind == JsonValueKind.Number)
                    cost =
                        costP.GetDecimal().ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " EUR";
                else if (costP.ValueKind == JsonValueKind.Object && costP.TryGetProperty("excl_vat", out var excl))
                    cost = excl.GetDecimal().ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " EUR";
            }
            var start = ses.TryGetProperty("start_date_time", out var startP) ? startP.GetString() : "—";

            sb.Append("<div class='live-session-panel'>");
            sb.Append("<div class='live-session-header'>");
            sb.Append("<span class='badge badge-active'><span class='badge-dot'></span> CHARGING</span>");
            sb.Append("<span class='mono text-sm'>").Append(id).Append("</span>");
            sb.Append("</div>");

            sb.Append("<div class='live-session-stats'>");
            sb.Append("<div class='live-stat'><div class='live-stat-value'>")
                .Append(kwh)
                .Append("</div><div class='live-stat-label'>kWh</div></div>");
            sb.Append("<div class='live-stat'><div class='live-stat-value'>")
                .Append(cost)
                .Append("</div><div class='live-stat-label'>Cost</div></div>");
            sb.Append("<div class='live-stat'><div class='live-stat-value text-sm'>")
                .Append(start)
                .Append("</div><div class='live-stat-label'>Started</div></div>");
            sb.Append("</div>");

            sb.Append(
                "<form hx-post='/api/commands/stop-session' hx-target='closest .evse-live-session' hx-swap='innerHTML' style='margin-top:0.5rem'>"
            );
            sb.Append("<input type='hidden' name='cpoId' value='").Append(cpoId).Append("'/>");
            sb.Append("<input type='hidden' name='sessionId' value='").Append(id).Append("'/>");
            sb.Append("<button type='submit' class='btn btn-sm btn-danger'>■ Stop Session</button>");
            sb.Append("</form>");
            sb.Append("</div>");
        }

        return Results.Content(sb.ToString(), "text/html");
    }
);

// ── API: Sessions Table (HTMX partial for real-time refresh) ─

app.MapGet(
    "/api/sessions-table",
    (HttpContext ctx, ICpoRegistry registry, InMemoryDataStore store) =>
    {
        var statusFilter = ctx.Request.Query["status"].ToString();
        var sessions = store
            .Sessions.Select(kv =>
            {
                var cpoKey = kv.Key.Split(':')[0] + ":" + kv.Key.Split(':')[1];
                var version = registry.FindByConnectionKey(cpoKey)?.Version ?? OcpiVersion.V2_2_1;
                var opts = OcpiJsonOptions.GetOptions(version);
                var json = JsonSerializer.Serialize(kv.Value, opts.GetTypeInfo(kv.Value.GetType()));
                return (CpoKey: cpoKey, Data: JsonDocument.Parse(json).RootElement.Clone());
            })
            .Where(x =>
                string.IsNullOrEmpty(statusFilter)
                || (
                    x.Data.TryGetProperty("status", out var s)
                    && string.Equals(s.GetString(), statusFilter, StringComparison.OrdinalIgnoreCase)
                )
            )
            .ToList();

        var sb = new System.Text.StringBuilder();
        sb.Append("<div class='table-wrap'><table><thead><tr>");
        sb.Append(
            "<th>ID</th><th>CPO</th><th>Location</th><th>EVSE</th><th>Status</th><th>kWh</th><th>Start</th><th>Cost</th><th>Actions</th>"
        );
        sb.Append("</tr></thead><tbody>");

        if (sessions.Count == 0)
        {
            sb.Append(
                "<tr><td colspan='9'><div class='empty-state'><div class='empty-state-icon'>▶</div><div class='empty-state-text'>No sessions tracked yet.</div></div></td></tr>"
            );
        }
        else
        {
            foreach (var (cpoKey, ses) in sessions)
            {
                var id = ses.TryGetProperty("id", out var idP) ? idP.GetString() : "—";
                var locId = ExtractLocationId(ses);
                var evseUid = ExtractEvseUid(ses);
                var status = ses.TryGetProperty("status", out var stP) ? stP.GetString() : "—";
                var kwh = ses.TryGetProperty("kwh", out var kwhP)
                    ? kwhP.GetDecimal().ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
                    : "—";
                var start = ses.TryGetProperty("start_date_time", out var startP) ? startP.GetString() : "—";
                var cost = "—";
                if (ses.TryGetProperty("total_cost", out var costP))
                {
                    if (costP.ValueKind == JsonValueKind.Number)
                        cost =
                            costP.GetDecimal().ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                            + " EUR";
                    else if (costP.ValueKind == JsonValueKind.Object && costP.TryGetProperty("excl_vat", out var excl))
                        cost =
                            excl.GetDecimal().ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                            + " EUR";
                }
                var badge = status switch
                {
                    "ACTIVE" => "badge-active",
                    "COMPLETED" => "badge-completed",
                    "PENDING" => "badge-warning",
                    _ => "badge-info",
                };

                sb.Append("<tr><td class='mono'>").Append(id).Append("</td>");
                sb.Append("<td><span class='badge badge-info'>").Append(cpoKey).Append("</span></td>");
                sb.Append("<td class='mono'>").Append(locId).Append("</td>");
                sb.Append("<td class='mono text-sm'>").Append(evseUid).Append("</td>");
                sb.Append("<td><span class='badge ")
                    .Append(badge)
                    .Append("'><span class='badge-dot'></span> ")
                    .Append(status)
                    .Append("</span></td>");
                sb.Append("<td class='mono'>").Append(kwh).Append("</td>");
                sb.Append("<td class='mono text-sm text-muted'>").Append(start).Append("</td>");
                sb.Append("<td class='mono'>").Append(cost).Append("</td>");

                if (status == "ACTIVE")
                {
                    sb.Append("<td><form hx-post='/api/commands/stop-session' hx-target='#stop-r-")
                        .Append(id)
                        .Append("' hx-swap='innerHTML' style='display:inline'>");
                    sb.Append("<input type='hidden' name='cpoId' value='").Append(cpoKey).Append("'/>");
                    sb.Append("<input type='hidden' name='sessionId' value='").Append(id).Append("'/>");
                    sb.Append("<button type='submit' class='btn btn-sm btn-danger'>■ Stop</button>");
                    sb.Append("</form><span id='stop-r-").Append(id).Append("'></span></td>");
                }
                else
                {
                    sb.Append("<td></td>");
                }

                sb.Append("</tr>");
            }
        }

        sb.Append("</tbody></table></div>");
        return Results.Content(sb.ToString(), "text/html");
    }
);

// ── API: Command History ─────────────────────────────────────

app.MapGet(
    "/api/commands/history",
    (CommandHistory cmdHistory) =>
    {
        var records = cmdHistory.GetRecent();
        var html = string.Join(
            "",
            records.Select(r =>
            {
                var badgeClass =
                    r.Result == "ACCEPTED" ? "badge-success"
                    : r.Result == "ERROR" ? "badge-error"
                    : "badge-warning";
                return $"<tr><td class='mono text-sm'>{r.Time:HH:mm:ss}</td><td><span class='badge badge-info'>{r.CpoId}</span></td><td>{r.CommandType}</td><td><span class='badge {badgeClass}'>{r.Result}</span></td></tr>";
            })
        );

        if (string.IsNullOrEmpty(html))
            html =
                "<tr><td colspan='4' class='text-center text-muted' style='padding: 1.5rem;'>No commands sent yet</td></tr>";

        return Results.Content(html, "text/html");
    }
);

// ── API: Sync ────────────────────────────────────────────────

app.MapPost(
    "/api/sync/{cpoId}/{module}",
    async (string cpoId, string module, IOcpiSyncService syncService) =>
    {
        await syncService.SyncModuleFromCpoAsync(cpoId, module);
        return Results.Ok();
    }
);

app.MapPost(
    "/api/sync/{cpoId}",
    async (string cpoId, IOcpiSyncService syncService) =>
    {
        await syncService.SyncFromCpoAsync(cpoId);
        return Results.Ok();
    }
);

app.MapPost(
    "/api/sync/all",
    async (ICpoRegistry registry, IOcpiSyncService syncService) =>
    {
        foreach (var cpo in registry.GetAll())
            await syncService.SyncFromCpoAsync(cpo.ConnectionKey);
        return Results.Ok();
    }
);

app.Run();
return;

// ── Helpers for multi-version session property extraction ────

// V2.2+: session.location_id (flat). V2.0/V2.1.1: session.location.id (embedded).
static string ExtractLocationId(JsonElement ses)
{
    if (ses.TryGetProperty("location_id", out var locId))
        return locId.GetString() ?? "—";
    if (
        ses.TryGetProperty("location", out var loc)
        && loc.ValueKind == JsonValueKind.Object
        && loc.TryGetProperty("id", out var embeddedId)
    )
        return embeddedId.GetString() ?? "—";
    return "—";
}

// V2.2+: session.evse_uid (flat). V2.0/V2.1.1: session.location.evses[0].uid (embedded).
static string ExtractEvseUid(JsonElement ses)
{
    if (ses.TryGetProperty("evse_uid", out var uid))
        return uid.GetString() ?? "—";
    if (
        ses.TryGetProperty("location", out var loc)
        && loc.ValueKind == JsonValueKind.Object
        && loc.TryGetProperty("evses", out var evses)
        && evses.ValueKind == JsonValueKind.Array
        && evses.GetArrayLength() > 0
        && evses[0].TryGetProperty("uid", out var evseUid)
    )
        return evseUid.GetString() ?? "—";
    return "—";
}

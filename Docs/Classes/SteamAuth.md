### Class
`Fougerite.Tools.SteamAPITools` / `Fougerite.Tools.SteamTicketInfo` / `Fougerite.Tools.SteamTicketValidator` /
`Fougerite.Tools.SteamWebValidation` / `Fougerite.Tools.TrustedSteamIDs` / `Fougerite.Tools.SteamUserRegistry`

### Description
These classes implement Fougerite's extended Steam authentication pipeline, used by `Hooks`/`ConnectionAcceptor`
when a client connects. Native (built-in) Steam authentication only ever validates **Rust** tickets; anything
else - most notably RustBuster clients, which authenticate with the free **Spacewar** AppID - gets rejected by
native auth and only reaches Fougerite through the `On_SteamDeny` hook. This pipeline lets you (via the
`SteamAuthMode` option in `Fougerite.cfg`) decide what happens to those rejected connections instead of leaving
it entirely to a plugin's `SteamDenyEvent.ForceAllow`.

The overall flow (see `Hooks.cs`/`ConnectionAcceptor`):
1. The client's raw session ticket is parsed with `SteamAPITools.TryParseTicket`.
2. If it's a Rust ticket, native auth already decides; Fougerite just records it in `SteamUserRegistry`.
3. If it's a Spacewar ticket and the configured `SteamAuthMode` needs it, `SteamTicketValidator.Begin` kicks off
   an asynchronous Steam Web API call and the result is awaited without blocking the main thread.
4. When native auth calls `SteamDeny` (or the Web API result is ready), `SteamTicketValidator.Evaluate` decides,
   based on the mode, the parsed ticket and the Web API result, whether the player is let in.

### SteamAPITools
Static helpers for parsing/validating ticket bytes - no network calls, pure data inspection:
- `RustAppId` (252490) / `SpacewarAppId` (480) / `SteamWebApiBaseUrl` - well-known constants.
- `TryParseTicket(byte[] ticket, out SteamTicketInfo info)` - parses the binary layout of a Steam session
  ticket (GC section + ownership ticket + signature). Does **not** verify the signature - a forged ticket with
  a sane layout parses successfully. Use `SteamTicketValidator`/the Web API for real verification.
- `IsPlausibleTicket(SteamTicketInfo info, ulong claimedSteamId, uint expectedAppId, out string reason)` -
  every *offline* sanity check that can be performed without contacting Steam (SteamID/AppID match, individual
  account, exact layout, non-blank signature, plausible issue/expiry timestamps). Backs
  `SteamAuthMode.SteamAccountsUnverified`. Rejects broken emulators/lazy forgeries, but cannot stop a carefully
  crafted forgery since the signature itself is never checked.
- `IsIndividualSteamId(ulong steamId)` - true for a public, individual, desktop-instance SteamID64.
- `GetIssuedTicket(byte[] ticket, SteamTicketInfo info)` - trims the trailing buffer padding some clients
  append after the ticket, returning the ticket exactly as Steam issued it. This is required before sending it
  to the Web API, otherwise genuine tickets could be rejected (see `SteamTicketValidator.Begin` below).
- `ToHex(byte[] data)` - uppercase hex encoding (the format the Steam Web API expects for tickets).

`SteamTicketInfo` (returned by `TryParseTicket`) exposes: `SteamId`, `AppId`, `Version`, `HasSessionHeader`,
`HasSignature`, `GcToken`, `SessionGeneratedUtc`, `OwnershipIssuedUtc`, `OwnershipExpiresUtc`, `IsLayoutExact`,
`IsSignatureBlank`, `TicketLength`, `TrailingBytes`.

### SteamAuthMode (enum, `Fougerite.cfg` -> `SteamAuthMode`)
Controls what happens to a connection that native Steam auth rejects. Each mode's safety rating below reflects
how hard it is for a forged/cracked/emulated client to slip through - **not** how many legitimate players it
admits:
- `RustOnly` (1) - **Most Safe.** Enforces strict native Steam authentication for Rust (252490). Completely
  blocks all Spacewar, emulated or cracked clients; every rejected connection stays rejected.
- `RustOwners` (2) - **High Safety.** Allows Spacewar tickets, but uses the Steam Web API to verify the account
  actually owns paid Rust (needs a Web API key + the player's game details to be public). A SteamID listed in
  `TrustedSteamIDs` is admitted even when it doesn't own Rust.
- `SteamPaidAccounts` (3) - **High Safety.** Allows Spacewar players whose ticket is verified through the
  Steam Web API and whose Steam account is not limited, meaning it has spent at least 5 USD on Steam (needs a
  Web API key). This keeps out freshly-made alt accounts **without** requiring Rust ownership. Private
  profiles are fine, but the player must have set up a Steam Community profile at least once. A SteamID
  listed in `TrustedSteamIDs` is admitted even when its account is limited.
- `SteamAccounts` (4) - **Medium Safety.** Verifies via the Web API that the Spacewar ticket belongs to a
  legitimate Steam account, but does not require Rust ownership or a non-limited account (needs a Web API
  key). **This is the most recommended setting to use with RustBuster.**
- `SteamAccountsUnverified` (5) - **Low Safety.** Performs basic offline checks
  (`SteamAPITools.IsPlausibleTicket`) to filter out sloppy emulators. No network call, fastest Steam-only mode,
  but bypassable by well-forged tickets spoofing any SteamID.
- `Legacy` (0) - **Very Low Safety.** Performs no ticket verification on its own, delegating access decisions
  entirely to plugins (via `SteamDenyEvent.ForceAllow`). This is the original pre-`SteamAuthMode` behaviour.
- `AllowAll` (6) - **Zero Safety.** Disables all checks and allows anyone to connect, including clients without
  Steam at all.

`RustOwners` and `SteamPaidAccounts` each need a Steam profile requirement the player might not have set up
(public game details / an existing Community profile); the startup log warns about whichever is configured so
the requirement isn't a surprise when players get rejected.

### SteamTicketValidator
Static class that drives the Steam Web API calls and the final admit/deny decision:
- `IsWebApiConfigured` - whether `Bootstrap.SteamWebAPIKey` is set.
- `ShouldValidate(SteamAuthMode mode)` - whether the given mode needs a Web API call for a Spacewar ticket.
- `ReadWebApiTicket(ClientConnection cc, uLink.BitStream loginData, SteamAuthMode mode)` - reads an optional
  `ISteamUser.GetAuthTicketForWebApi` ticket a client appended after its session ticket. Must be called right
  after `ClientConnection.ReadConnectionData`.
- `Begin(ClientConnection cc, SteamTicketInfo ticketInfo, byte[] webApiTicket, SteamAuthMode mode)` - starts an
  asynchronous `AuthenticateUserTicket` (+ `GetOwnedGames`/community-profile check when required) call on the
  `ThreadPool` and returns a `SteamWebValidation` immediately. A genuine, freshly-issued ticket can briefly make
  Steam answer `101 Invalid ticket`, so an invalid-ticket response is retried for about 5 seconds while Steam
  finishes registering the ticket, instead of failing the connection outright.
- `Get(ClientConnection cc)` - returns the in-flight/completed validation for a connection, or `null`.
- `Evaluate(SteamAuthMode mode, ClientConnection cc, SteamTicketInfo ticket, SteamWebValidation web, out string reason)`
  - applies the mode and returns whether the player may join, plus a human-readable `reason` (useful for
  logging why someone was let in/kicked).
- `WebApiTicketIdentity` / `WebApiTicketMagic` / `WebApiTicketVersion` / `MaxWebApiTicketLength` - protocol
  constants for the optional Web API ticket extension in the connection data.

`SteamWebValidation` (returned by `Begin`) exposes the live/completed state of one validation: `ClaimedSteamId`,
`AppId`, `RequireRustOwnership`, `RequirePaidAccount` (set for `SteamPaidAccounts`), `IsLimitedAccount`
(nullable, true when the account never spent 5 USD on Steam), `Attempts`, `TrimmedBytes` (padding bytes
removed from the client's ticket before sending it to Steam), `VerifiedSteamId`, `OwnerSteamId`, `VacBanned`,
`PublisherBanned`, `OwnsRust` (nullable), `LastHttpStatus`, `Status` (`SteamWebValidationStatus`), `Detail`,
`IsCompleted`, `IsGenuineSteamAccount`, `IsApiFailure`. `SteamWebValidationStatus` values: `Pending`,
`Verified`, `InvalidTicket`, `SteamIdMismatch`, `NotRustOwner`, `OwnershipPrivate`, `LimitedAccount`,
`AccountStatusUnknown`, `ApiError`, `ApiKeyRejected`, `TimedOut`.

### TrustedSteamIDs
Lazy singleton (`GetInstance()`, same pattern as `PermissionSystem`/`Web`/etc.) backed by
`Save\TrustedSteamIDs.json`, holding SteamIDs that are admitted by `SteamTicketValidator.Evaluate` under
`SteamAuthMode.RustOwners`/`SteamAuthMode.SteamPaidAccounts` without needing to pass that mode's Steam Web API
check (Rust ownership / non-limited account). Not used by any other mode - `RustOnly` doesn't call the Web
API at all, and the other modes don't need this kind of override.
The JSON file is just a flat array of SteamID64 numbers and can be edited by hand while the server is stopped:
- `GetInstance()` - returns the singleton instance.
- `Contains(ulong steamId)` - whether the SteamID is trusted.
- `Add(ulong steamId)` / `Remove(ulong steamId)` - add/remove a SteamID, persisting the change to disk.
- `GetAll()` - a snapshot `List<ulong>` of every trusted SteamID.
- `Reload()` - reloads the list from disk, discarding unsaved in-memory changes (there shouldn't be any, since
  `Add`/`Remove` save immediately).
This is also documented in `Fougerite.cfg` right under the `SteamAuthMode` table.
Exposed to Python, JS and Lua plugins as the global `TrustedSteamIDs` variable, same as `PermissionSystem`.

### SteamUserRegistry
A simple static, thread-safe registry of SteamIDs that have been verified this session (by native auth for
Rust, or by the pipeline above for Spacewar):
- `SteamAppId` (nested enum): `None` (-1), `SpaceWars` (480), `Rust` (252490).
- `Add(ulong userID, SteamAppId appId)` / `Remove(ulong userID)` / `Contains(ulong userID)`.
- `GetType(ulong userID)` - the `SteamAppId` a user was verified under, or `SteamAppId.None`.
- `GetShallowCopy()` - snapshot `Dictionary<ulong, SteamAppId>` of every verified user.

### Example - C# (checking whether a connected player authenticated as Spacewar)
```csharp
public void OnPlayerConnected(Player player)
{
    SteamUserRegistry.SteamAppId appId = SteamUserRegistry.GetType(player.UID);
    if (appId == SteamUserRegistry.SteamAppId.SpaceWars)
    {
        Logger.Log($"{player.Name} connected via a RustBuster/Spacewar ticket.");
    }
}
```

### Example - C# (logging the policy verdict/Web API result on SteamDeny)
```csharp
public void OnSteamDeny(SteamDenyEvent e)
{
    // In any mode other than Legacy, e.ForceAllow already reflects e.PolicyAllowed by the time this fires -
    // this just demonstrates reading the parsed ticket/Web API result for logging/auditing purposes.
    Logger.Log($"SteamDeny ({e.Mode}): {e.PolicyReason}");
    if (e.WebValidation != null)
    {
        Logger.Log($"Web API result: {e.WebValidation}");
    }
}
```

See also: [`On_SteamDeny`](../Hooks/Player/On_SteamDeny.md) · [`On_PlayerApproval`](../Hooks/Player/On_PlayerApproval.md) ·
[`FougeriteCfg`](../FougeriteCfg.md)

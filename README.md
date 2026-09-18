# Zeymera Scoreboard

A kiosk-style digital scoreboard for billiards, built on Blazor Server.

## Projects

| Project | What it is |
|---|---|
| `Zeymera.Scoreboard.Client` | The scoreboard display itself. Blazor Server app (net10.0), state persisted via EF Core/SQLite. This is what you run on the machine driving the screen. |
| `Zeymera.Scoreboard.Api` | ASP.NET Core minimal API backed by PostgreSQL (EF Core) for players, teams, matches and match stats. |

## Running the scoreboard

```
cd Zeymera.Scoreboard.Client
dotnet run
```

Opens at `http://localhost:5288` by default (see `Properties/launchSettings.json`). On first run it creates `wwwroot/Db/scoreboard.db` (SQLite) and seeds a default game state.

Click anywhere on the board to enter fullscreen kiosk mode.

## Keyboard shortcuts

| Key | Action |
|---|---|
| `C` | Show/hide the controls overlay |
| `T` | Start/stop the shot clock |
| `R` | Reset the shot clock |
| `1` / `2` | Set active player |
| `P` | Open the player picker for whichever player is currently active |
| `+` / `-` | Adjust the current-points counter |

## Remote control input

The scoreboard accepts the same commands from three sources, all funneled through `Home.razor`'s `ApplyCommand`:

1. **Keyboard**, as above.
2. **WebSocket** — a control app connects to `ws://<host>:<port>/ws` and sends a JSON envelope as a UTF-8 text frame: `{"command":"...", "payload": ...}`. For backward compatibility the endpoint also still accepts a bare command name with no payload as plain text, e.g. just `IncrementPoints`.

   **Command reference** (`command` is a `ScoreboardCommand` enum name, case-insensitive):

   | Payload | Commands |
   |---|---|
   | *(none)* | `ToggleControls`, `ToggleShotClock`, `ResetShotClock`, `SelectPlayer1`, `SelectPlayer2`, `IncrementPoints`, `DecrementPoints`, `IncrementPlayer1Score`, `DecrementPlayer1Score`, `IncrementPlayer2Score`, `DecrementPlayer2Score`, `IncrementInning`, `ClearRosterPlayer1`, `ClearRosterPlayer2`, `EndGame`, `NewGame` |
   | string | `RenamePlayer1`, `RenamePlayer2` — the free-typed name |
   | number | `SetMatchTarget` — the target score; `SelectRosterPlayer1`, `SelectRosterPlayer2` — a roster player's `Id` to link to that slot |
   | object | `UpsertPlayer` — see below |

   ```json
   {"command":"IncrementPoints"}
   {"command":"RenamePlayer1","payload":"Hayri"}
   {"command":"SetMatchTarget","payload":40}
   {"command":"SelectRosterPlayer1","payload":7}
   ```

   Every score/inning change saves immediately — there's no separate "commit" step. `Inning` follows one rule: it auto-increments whenever Player 1 (the white ball) scores (`IncrementPlayer1Score`), and otherwise only moves via the manual `IncrementInning` correction. It never decrements — a mis-scored point is fixed with `DecrementPlayer1Score`/`DecrementPlayer2Score`, not by rolling the inning back.

   `UpsertPlayer` adds or updates a row in the local `player` roster (see "Player roster" below) — the photo is optional and only needs sending when it changes:
   ```json
   {"command":"UpsertPlayer","payload":{"id":7,"nickname":"Hayri","name":"Hayri Ozler","photoBase64":"<base64 jpeg bytes>","photoExtension":"jpg"}}
   ```

   **C# example** (MAUI or any .NET client, using `System.Net.WebSockets.ClientWebSocket`):
   ```csharp
   private ClientWebSocket _socket = new();

   private async Task ConnectAsync(string host) =>
       await _socket.ConnectAsync(new Uri($"ws://{host}:5288/ws"), CancellationToken.None);

   private async Task SendCommandAsync(string command, object? payload = null)
   {
       var envelope = payload is null ? new { command } : new { command, payload };
       var json = JsonSerializer.Serialize(envelope);
       await _socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
   }

   // usage:
   await SendCommandAsync("SelectPlayer1");
   await SendCommandAsync("RenamePlayer1", "Hayri");
   await SendCommandAsync("SetMatchTarget", 40);
   ```

   **Receiving state** — the instant a client connects, before it sends anything, and again after **every** subsequent change to the board (from any source — this client, another connected client, the keyboard, the on-screen controls), the server pushes:
   ```json
   {"type":"state","state":{"player1DisplayName":"Hayri","player2DisplayName":"Player 2","player1Score":13,"player2Score":11,"player1Avg":1.857,"player2Avg":1.571,"player1HighRun":5,"player2HighRun":3,"activePlayer":2,"currentPoints":0,"inning":7,"matchTarget":25,"shotClockActive":false,"shotClockSeconds":40}}
   ```
   Display names are already resolved (roster `Nickname`, falling back to `Name`, falling back to the free-typed name, falling back to `"Player 1"`/`"Player 2"`) — no need to separately resolve `Player1Id`/`Player2Id` against the roster. A well-behaved client should treat this push, not its own sent commands, as the source of truth for what to render — check `type == "state"` on every received message and refresh the UI from `state`.

   Quick test from a browser console **on the scoreboard page itself** (a live circuit needs to be open for anything to receive the command — a bare `curl`/script connection with no rendered page won't do anything):
   ```js
   const ws = new WebSocket("ws://localhost:5288/ws");
   ws.onopen = () => console.log("connected");
   ws.onmessage = (e) => console.log("received:", e.data); // state snapshot, on connect and after every change
   ws.send(JSON.stringify({ command: "RenamePlayer1", payload: "Hayri" }));
   ws.send("IncrementPoints");
   ```
   The sync-dot on the board reflects whether a control connection is currently active.
3. **Bluetooth (Web Bluetooth)** — "Pair remote" in the controls overlay connects to a BLE peripheral over `wwwroot/js/bluetooth.js`. This only works against a **custom** GATT service; browsers block the standard HID-over-GATT profile, so a generic BLE pedal/keyboard won't pair this way (pair it at the OS level instead — it'll fire keydown events, which are already wired up). The remote sends the exact same envelope as the WebSocket channel (bare command name or the JSON form). Note a full player photo is a lot of data for typical BLE notify MTU sizes — that payload is realistically better suited to the WebSocket channel unless your firmware chunks/reassembles large messages. The service/characteristic UUIDs in `bluetooth.js` are placeholders (Nordic UART Service convention) — replace them with your device's actual UUIDs.

## Player roster

The `player` table (SQLite, same database as the scoreboard state) is a local roster independent of `Zeymera.Scoreboard.Api` — `Id` is auto-increment, plus `Nickname`, `Name`, and `PhotoPath` (a relative path under `wwwroot/Players/`; the photo bytes live on disk, not in the DB). The remote `UpsertPlayer` command may still pass an explicit `id` (e.g. to match a MAUI app's own id scheme) — EF/SQLite only auto-assign when the value is left at its default, so both paths work side by side.

- **`/players`** — a page (linked from the board's controls overlay) to add/edit/delete roster players by hand, with a photo upload.
- **Assigning a roster player to the board** — press `P` (or use the "Choose Player" button per slot in the controls overlay) to open a picker dialog listing the roster; picking one links that slot to the player's `Id` (`ScoreboardState.Player1Id`/`Player2Id`). Once linked, the board displays that player's `Nickname` (falling back to `Name` if the nickname is empty) and their photo next to the name, instead of the free-typed `Player1Name`/`Player2Name`. "Use manual name instead" in the dialog unlinks it and reverts to the free-text name. Typing a number into the free-text name field is also treated as a lookup — if a roster player has that Id, that player is linked instead of using the digits as a literal name.
- **`UpsertPlayer`** over WebSocket/Bluetooth (see above) adds/updates roster entries remotely — e.g. from the MAUI companion app.

## Match history

Ending a match is two separate steps, both in the controls overlay:

- **"End Game"** snapshots the current game into the `match_result` table — date/time played, both players' display names (a copy, not just a `PlayerId` reference, so history stays readable even if that roster player is later renamed or deleted), scores, averages, high runs, innings played, the match target, and the winner. It does **not** reset the board — the final score stays on screen (e.g. for a photo/announcement).
- **"New Game"** resets the board for the next match. It does **not** save anything — press "End Game" first if the current match's stats should be kept. `MatchTarget` is preserved across "New Game" (it's treated as a match-format setting, not per-game state).

View history at **`/matches`**, linked from the controls overlay, with the winner's name highlighted.

Both are also `ScoreboardCommand`s (`EndGame`, `NewGame`), so either can be triggered remotely over WebSocket/Bluetooth as well as the on-screen buttons. Neither has a confirmation dialog — both fire immediately, locally or remotely.

The match target itself (`Current.MatchTarget`, default 20 on a brand-new database) can be edited directly in the controls overlay, or set remotely with `SetMatchTarget` (see above).

## Known gaps

- `Services/WebSocketService.cs` in the Client project is an older outbound `ClientWebSocket` stub, superseded by the inbound `/ws` endpoint + `ScoreboardCommandHub` design above; currently unused.
- No formal EF Core migrations — schema changes are patched in at startup in `Program.cs` (`CREATE TABLE IF NOT EXISTS` / `EnsureColumn`) since `Database.EnsureCreated()` is a no-op once the db file exists. Fine for now, but if the schema keeps growing, switching to real migrations would remove the need for this pattern.
- The `/ws` state broadcast (see "Remote control input" above) does **not** fire on the shot clock's per-tick countdown (every 100ms) — only on discrete state changes (a command applied, the clock naturally expiring, etc.), to avoid flooding connected clients. A control app won't see the shot clock counting down live, only when it starts/stops/resets/expires.
- Bluetooth is receive-only from the board's perspective — the board (as BLE *central*) never opens a *write* channel back to the paired peripheral, so the state broadcast above only reaches WebSocket clients, not a connected Bluetooth remote. Would need a specific writable GATT characteristic on the remote's firmware to support that.

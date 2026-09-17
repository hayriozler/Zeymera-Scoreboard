# Zeymera Scoreboard

A kiosk-style digital scoreboard for billiards, built on Blazor Server.

## Projects

| Project | What it is |
|---|---|
| `Zeymera.Scoreboard.Client` | The scoreboard display itself. Blazor Server app (net10.0), state persisted via EF Core/SQLite. This is what you run on the machine driving the screen. |
| `Zeymera.Scoreboard.Api` | ASP.NET Core minimal API backed by PostgreSQL (EF Core) for players, teams, matches and match stats. |
| `xxZeymera.Scoreboard.Clientxx` | Earlier prototype of the client, using localStorage/JS for persistence instead of EF Core. Kept for reference; not built or run anymore. |

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
| `Enter` | Commit current points to the active player and hand off the turn |

## Remote control input

The scoreboard accepts the same commands from three sources, all funneled through `Home.razor`'s `ApplyCommand`:

1. **Keyboard**, as above.
2. **WebSocket** — a control app connects to `ws://<host>:<port>/ws/control` and sends a JSON envelope as a UTF-8 text frame:
   ```json
   {"command":"IncrementPoints"}
   {"command":"RenamePlayer1","payload":"Hayri"}
   ```
   `command` is one of the `ScoreboardCommand` enum names (case-insensitive): `ToggleControls`, `ToggleShotClock`, `ResetShotClock`, `SelectPlayer1`, `SelectPlayer2`, `IncrementPoints`, `DecrementPoints`, `CommitPoints`, `RenamePlayer1`, `RenamePlayer2`, `UpsertPlayer`, `FinishMatch`. `payload` is optional: a plain string for the two rename commands, a structured object for `UpsertPlayer` (see below), and ignored by every other command. For backward compatibility the endpoint also still accepts a bare command name with no payload, e.g. just the text `IncrementPoints`.

   `UpsertPlayer` adds or updates a row in the local `player` roster (see "Player roster" below) — the photo is optional and only needs sending when it changes:
   ```json
   {"command":"UpsertPlayer","payload":{"id":7,"nickname":"Hayri","name":"Hayri Ozler","photoBase64":"<base64 jpeg bytes>","photoExtension":"jpg"}}
   ```

   Quick test from a browser console **on the scoreboard page itself** (a live circuit needs to be open for anything to receive the command — a bare `curl`/script connection with no rendered page won't do anything):
   ```js
   const ws = new WebSocket("ws://localhost:5288/ws/control");
   ws.onopen = () => console.log("connected");
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

"Finish Match" in the controls overlay (confirms before proceeding) snapshots the current game into the `match_result` table and resets the board for a new match. Each row keeps: date/time played, both players' display names (a copy, not just a `PlayerId` reference — history stays readable even if that roster player is later renamed or deleted), scores, averages, high runs, innings played, the match target, and the winner. View it at **`/matches`**, linked from the controls overlay, with the winner's name highlighted.

`FinishMatch` is also one of the `ScoreboardCommand`s, so it can be triggered remotely over WebSocket/Bluetooth as well as the on-screen button — remote triggers skip the local confirmation dialog (the calling app is expected to confirm on its own end).

## Known gaps

- `Services/WebSocketService.cs` in the Client project is an older outbound `ClientWebSocket` stub, superseded by the inbound `/ws/control` endpoint + `ScoreboardCommandHub` design above; currently unused.
- No formal EF Core migrations — schema changes are patched in at startup in `Program.cs` (`CREATE TABLE IF NOT EXISTS` / `EnsureColumn`) since `Database.EnsureCreated()` is a no-op once the db file exists. Fine for now, but if the schema keeps growing, switching to real migrations would remove the need for this pattern.

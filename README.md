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
| `Enter` | Commit current points to the active player and hand off the turn |

## Remote control input

The scoreboard accepts the same commands from three sources, all funneled through `Home.razor`'s `ApplyCommand`:

1. **Keyboard**, as above.
2. **WebSocket** — a control app connects to `ws://<host>:<port>/ws` and sends a JSON envelope as a UTF-8 text frame:
   ```json
   {"command":"IncrementPoints"}
   {"command":"RenamePlayer1","payload":"Hayri"}
   {"command":"SetMatchTarget","payload":40}
   ```
   `command` is one of the `ScoreboardCommand` enum names (case-insensitive): `ToggleControls`, `ToggleShotClock`, `ResetShotClock`, `SelectPlayer1`, `SelectPlayer2`, `IncrementPoints`, `DecrementPoints`, `CommitPoints`, `RenamePlayer1`, `RenamePlayer2`, `UpsertPlayer`, `EndGame`, `NewGame`, `SetMatchTarget`. `payload` is optional: a plain string for the two rename commands, a number for `SetMatchTarget`, a structured object for `UpsertPlayer` (see below), and ignored by every other command. For backward compatibility the endpoint also still accepts a bare command name with no payload, e.g. just the text `IncrementPoints`.

   `UpsertPlayer` adds or updates a row in the local `player` roster (see "Player roster" below) — the photo is optional and only needs sending when it changes:
   ```json
   {"command":"UpsertPlayer","payload":{"id":7,"nickname":"Hayri","name":"Hayri Ozler","photoBase64":"<base64 jpeg bytes>","photoExtension":"jpg"}}
   ```

   **The instant a client connects**, before it sends anything, the server pushes a one-time state snapshot so the control app's UI can reflect the live game immediately instead of starting blank/stale:
   ```json
   {"type":"state","state":{"player1DisplayName":"Hayri","player2DisplayName":"Player 2","player1Score":13,"player2Score":11,"player1Avg":1.857,"player2Avg":1.571,"player1HighRun":5,"player2HighRun":3,"activePlayer":2,"currentPoints":0,"inning":7,"matchTarget":25,"shotClockActive":false,"shotClockRemaining":40,"shotClockSeconds":40}}
   ```
   Display names are already resolved (roster `Nickname`, falling back to `Name`, falling back to the free-typed name, falling back to `"Player 1"`/`"Player 2"`) — no need to separately resolve `Player1Id`/`Player2Id` against the roster. This is a one-time snapshot at connect time only, not a live subscription — the client won't be pushed further updates as the game progresses after that.

   Quick test from a browser console **on the scoreboard page itself** (a live circuit needs to be open for anything to receive the command — a bare `curl`/script connection with no rendered page won't do anything):
   ```js
   const ws = new WebSocket("ws://localhost:5288/ws");
   ws.onopen = () => console.log("connected");
   ws.onmessage = (e) => console.log("received:", e.data); // first message is the state snapshot above
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
- The `/ws` state snapshot (see "Remote control input" above) is sent **once**, at connect time only. A control app that stays connected through the rest of the game currently has to infer state changes from the commands it itself sent — it won't be pushed live updates when the shot clock ticks, or when some other input source (keyboard, another control app) changes the score. Broadcasting state to all connected controllers on every change would be the natural next step if that's needed.

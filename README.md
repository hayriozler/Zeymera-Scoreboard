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
| `+` / `-` | Adjust the current-points counter |
| `Enter` | Commit current points to the active player and hand off the turn |

## Remote control input

The scoreboard accepts the same commands from three sources, all funneled through `Home.razor`'s `ApplyCommand`:

1. **Keyboard**, as above.
2. **WebSocket** — a control app can connect to `ws://<host>:<port>/ws/control` and send one of the `ScoreboardCommand` enum names as a plain UTF-8 text frame:
   `ToggleControls`, `ToggleShotClock`, `ResetShotClock`, `SelectPlayer1`, `SelectPlayer2`, `IncrementPoints`, `DecrementPoints`, `CommitPoints`.

   Quick test from a browser console on the scoreboard page:
   ```js
   const ws = new WebSocket("ws://localhost:5288/ws/control");
   ws.onopen = () => console.log("connected");
   ws.send("IncrementPoints");
   ```
   The sync-dot on the board reflects whether a control connection is currently active.
3. **Bluetooth (Web Bluetooth)** — "Pair remote" in the controls overlay connects to a BLE peripheral over `wwwroot/js/bluetooth.js`. This only works against a **custom** GATT service; browsers block the standard HID-over-GATT profile, so a generic BLE pedal/keyboard won't pair this way (pair it at the OS level instead — it'll fire keydown events, which are already wired up). The remote is expected to notify the same command names as the WebSocket channel, UTF-8 encoded. The service/characteristic UUIDs in `bluetooth.js` are placeholders (Nordic UART Service convention) — replace them with your device's actual UUIDs.

## Known gaps

- The player-picker dropdown (`_players` in `Home.razor`) is wired to call `Zeymera.Scoreboard.Api` but not populated yet.
- `Services/WebSocketService.cs` in the Client project is an older outbound `ClientWebSocket` stub, superseded by the inbound `/ws/control` endpoint + `ScoreboardCommandHub` design above; currently unused.

using System.Net.WebSockets;
using System.Text;
using Zeymera.Scoreboard.Client.Models;
using Zeymera.Scoreboard.Client.Services;

namespace Zeymera.Scoreboard.Client.Endpoints;

public static class ScoreboardWebSocketEndpoint
{
    private const int MaxControlMessageBytes = 16 * 1024 * 1024;

    public static void MapScoreboardWebSocket(this IEndpointRouteBuilder app)
    {
        app.Map("/ws", async (HttpContext context, ScoreboardCommandHub hub, ILogger<Program> logger) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            logger.LogInformation("WS connected: {RemoteIp}", remoteIp);
            hub.ControllerConnected(socket, remoteIp);
            try
            {
                var buffer = new byte[8192];
                while (socket.State == WebSocketState.Open)
                {
                    using var messageStream = new MemoryStream();
                    WebSocketReceiveResult result;
                    var closed = false;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                            closed = true;
                            break;
                        }

                        messageStream.Write(buffer, 0, result.Count);
                        if (messageStream.Length > MaxControlMessageBytes)
                        {
                            logger.LogWarning("Message from {RemoteIp} exceeded {MaxBytes} bytes - closing connection", remoteIp, MaxControlMessageBytes);
                            await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "message too large", CancellationToken.None);
                            closed = true;
                            break;
                        }
                    } while (!result.EndOfMessage);

                    if (closed)
                    {
                        logger.LogInformation("WS closed by {RemoteIp}", remoteIp);
                        break;
                    }

                    var seq = hub.NextSequence();
                    var text = Encoding.UTF8.GetString(messageStream.ToArray());
                    logger.LogInformation("[#{Seq}] RECV from {RemoteIp}: {Text}", seq, remoteIp, text);

                    var messages = ScoreboardCommandParser.TryParse(text);
                    if (messages.Count > 0)
                    {
                        logger.LogInformation("[#{Seq}] Parsed {Count} command(s) from {RemoteIp}: {Commands}", seq, messages.Count, remoteIp, string.Join(", ", messages.Select(m => m.Command)));
                        hub.Publish(messages);
                    }
                    else
                    {
                        logger.LogWarning("[#{Seq}] Could not parse any command from {RemoteIp}: {Text}", seq, remoteIp, text);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("WS connection from {RemoteIp} cancelled (dropped without a close handshake)", remoteIp);
            }
            catch (WebSocketException ex)
            {
                logger.LogWarning(ex, "WS connection from {RemoteIp} ended abruptly", remoteIp);
            }
            finally
            {
                hub.ControllerDisconnected(socket);
                logger.LogInformation("WS disconnected: {RemoteIp}", remoteIp);
            }
        });
    }
}

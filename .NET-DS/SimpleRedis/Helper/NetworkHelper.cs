using System.Net.Sockets;

namespace SimpleRedis.Helper;

public static class NetworkHelper
{
    /// <summary>
    /// 通过 Socket.Poll 等待数据可读，支持超时
    /// </summary>
    public static async Task<bool> PollForDataAsync(TcpClient tcpClient, int timeoutMs)
    {
        var socket = tcpClient.Client;
        if (socket == null) return false;

        var tcs = new TaskCompletionSource<bool>();

        using var cts = new CancellationTokenSource(timeoutMs);
        _ = Task.Run(() =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (socket.Poll(100, SelectMode.SelectRead))
                    {
                        tcs.TrySetResult(true);
                        return;
                    }
                }
                tcs.TrySetResult(false);
            }
            catch
            {
                tcs.TrySetResult(false);
            }
        }, cts.Token);

        return await tcs.Task;
    }

    /// <summary>
    /// 通过 Socket.Select 等待数据可读，支持超时
    /// </summary>
    public static async Task<bool> SelectForDataAsync(TcpClient tcpClient, int timeoutMs)
    {
        var socket = tcpClient.Client;
        if (socket == null) return false;

        var tcs = new TaskCompletionSource<bool>();

        using var cts = new CancellationTokenSource(timeoutMs);
        _ = Task.Run(() =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var readList = new List<Socket> { socket };
                    Socket.Select(readList, null, null, 100);
                    if (readList.Count > 0)
                    {
                        tcs.TrySetResult(true);
                        return;
                    }
                }
                tcs.TrySetResult(false);
            }
            catch
            {
                tcs.TrySetResult(false);
            }
        }, cts.Token);

        return await tcs.Task;
    }
}

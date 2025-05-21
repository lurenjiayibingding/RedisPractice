using System.Net.Sockets;

namespace SimpleRedis.Helper
{
    /// <summary>
    /// 网络帮助类
    /// </summary>
    public class NetworkHelper
    {
        /// <summary>
        /// 简单等待流可读
        /// 只是简单的轮询stream.DataAvailable状态，会浪费CPU资源，并且未设置超时策略
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static async Task SimpleWaitForStreamToBeReadableAsync(NetworkStream stream)
        {
            while (!stream.DataAvailable)
            {
                await Task.Delay(100);
            }
        }

        /// <summary>
        /// 通过Socket.Poll来确定响应流是否可读取
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="timeOuts"></param>
        /// <returns></returns>
        public static async Task<bool> StockPollWaitForStreamToBeReadable(TcpClient tcpClient, int timeOuts)
        {
            var socket = tcpClient.Client;
            var tcl = new TaskCompletionSource<bool>();

            using var cts = new CancellationTokenSource(timeOuts);
            _ = Task.Run(() =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        if (socket.Poll(100, SelectMode.SelectRead))
                        {
                            tcl.TrySetResult(true);
                            return;
                        }
                    }

                    cts.Token.ThrowIfCancellationRequested();
                    throw new TimeoutException("等待Socket可读超时取消");
                }
                catch (Exception ex)
                {
                    tcl.TrySetException(ex);
                    throw new Exception("等待Socket可读发生异常");
                }
            });

            return await tcl.Task;
        }

        /// <summary>
        /// 通过Socket.Select来确定响应流是否可读取
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="timeOuts"></param>
        /// <returns></returns>
        public static async Task<bool> StockSelectWaitForStreamToBeReadable(TcpClient tcpClient, int timeOuts)
        {
            var socket = tcpClient.Client;
            var tcl = new TaskCompletionSource<bool>();

            using var cts = new CancellationTokenSource(timeOuts);
            _ = Task.Run(() =>
            {
                try
                {
                    var readList = new List<Socket> { socket };
                    var writeList = new List<Socket>();
                    var errorList = new List<Socket>();

                    while (!cts.Token.IsCancellationRequested)
                    {
                        Socket.Select(readList, writeList, errorList, 100);
                        if (readList.Count > 0)
                        {
                            tcl.TrySetResult(true);
                            return;
                        }
                    }
                    cts.Token.ThrowIfCancellationRequested();
                    throw new TimeoutException("等待Socket可读超时取消");
                }
                catch (Exception ex)
                {
                    tcl.TrySetException(ex);
                    throw new Exception("等待Socket可读发生异常");
                }
            });

            return await tcl.Task;
        }

        ///// <summary>
        ///// 监听Socket来确定响应流是否可读取
        ///// </summary>
        ///// <param name="tcpClient"></param>
        ///// <param name="timeOuts"></param>
        ///// <returns></returns>
        //public static async Task<bool> StockSelectWaitForStreamToBeReadable2(TcpClient tcpClient, int timeOuts)
        //{
        //    var socket = tcpClient.Client;
        //    bool isReadable = false;

        //    try
        //    {
        //        using var cts = new CancellationTokenSource(timeOuts);
        //        await Task.Run(() =>
        //        {
        //            var readList = new List<Socket> { socket };
        //            var writeList = new List<Socket>();
        //            var errorList = new List<Socket>();

        //            while (!cts.Token.IsCancellationRequested)
        //            {
        //                Socket.Select(readList, writeList, errorList, 100);
        //                if (readList.Count > 0)
        //                {
        //                    isReadable = true;
        //                    return;
        //                }
        //            }
        //            throw new TimeoutException("等待Socket可读超时取消");
        //        });

        //        return isReadable;
        //    }
        //    catch (AggregateException ex)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}
    }
}
﻿using System.Threading.Tasks;
using DotNetty.Transport.Channels;

namespace Base.Network;

internal abstract class BaseSocketClient<TSocketClient, TData> : ISocketClient, IChannelEvent
    where TSocketClient : class, ISocketClient
{
    public BaseSocketClient(string ip, int port, TcpSocketCientEvent<TSocketClient, TData> clientEvent)
    {
        Ip = ip;
        Port = port;
        _clientEvent = clientEvent;
    }

    protected TcpSocketCientEvent<TSocketClient, TData> _clientEvent { get; }
    protected IChannel _channel { get; set; }

    public virtual void OnChannelActive(IChannelHandlerContext ctx)
    {
        _clientEvent.OnClientStarted?.Invoke(this as TSocketClient);
    }

    public void OnChannelInactive(IChannel channel)
    {
        _clientEvent.OnClientClose(this as TSocketClient);
    }

    public void OnException(IChannel channel, Exception exception)
    {
        _clientEvent.OnException?.Invoke(exception);
        Close();
    }

    public abstract void OnChannelReceive(IChannelHandlerContext ctx, object msg);

    public string Ip { get; }

    public int Port { get; }

    public void Close()
    {
        _channel.CloseAsync();
    }

    public abstract Task Send(byte[] bytes);
    public abstract Task Send(string msgStr);

    protected void PackException(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _clientEvent.OnException?.Invoke(ex);
        }
    }

    public void SetChannel(IChannel channel)
    {
        _channel = channel;
    }
}
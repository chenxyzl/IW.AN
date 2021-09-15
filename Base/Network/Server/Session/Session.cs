using Akka.Actor;
using Base.Network.Server.Interfaces;
using Base.Network.Shared.Interfaces;
using DotNetty.Common.Utilities;
using System;
using System.Collections.Generic;
using Base.Alg;

namespace Base.Network.Server
{

    #region delegates

    /// <summary>
    /// The delegate of message received handler from client connection.
    /// </summary>
    /// <param name="client">The client instance.</param>
    /// <param name="reader">The buffer reader of received message.</param>
    public delegate void MessageReceivedHandler(IClient client, IBufferReader reader);

    /// <summary>
    /// The delegate of client disconnected handler connection.
    /// </summary>
    /// <param name="client">The instance of disconnected client.</param>
    public delegate void ClientDisconnectedHandler(IClient client);

    #endregion

    /// <summary>
    /// This class is responsible for represents the client connection.
    /// </summary>
    public abstract class Session<T> : IClient where T : BaseActor
    {
        #region private members
        /// <summary>
        /// The callback of message received handler implementation.
        /// </summary>
        protected MessageReceivedHandler _messageReceivedHandler;

        /// <summary>
        /// The callback of client disconnected handler implementation.
        /// </summary>
        protected ClientDisconnectedHandler _clientDisconnectedHandler;

        #endregion

        #region properties

        /// <inheritdoc/>
        public ushort Id { get; set; }

        /// <inheritdoc/>
        public abstract string IpAddress { get; }

        /// <inheritdoc/>
		public abstract bool IsConnected { get; }

        /// <inheritdoc/>
		public T Actor { get; }

        #endregion

        /// <inheritdoc/>
        public abstract void SendMessage(IBufferWriter writer);

        /// <inheritdoc/>
        public virtual void Disconnect()
        {

        }
    }
}

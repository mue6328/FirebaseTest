using FM.IceLink;
using FM.IceLink.WebSync4;
using System;

namespace FirebaseTest11
{
    /// <summary>
    /// Signalling
    /// 
    /// Provides concrete implementation for DoJoinAsync.
    /// 
    /// See the Advanced Topics Manual Signalling Guide for more info.
    /// </summary>
    public class AutoSignalling : Signalling
    {
        public AutoSignalling(string serverUrl, string name, string chatName, string sessionId, Function1<PeerClient, Connection> createConnection, Action2<string, string> onReceivedText)
            : base(serverUrl, name, chatName, sessionId, createConnection, onReceivedText)
        { }

        protected override void DefineChannels()
        {
            SessionChannel = $"/{SessionId}";
            MetadataChannel = $"{SessionChannel}/metadata";
        }

        /// <summary>
        /// Handles subscription to the user and session channels and promise resolution/rejection. </summary>
        /// <param name="promise"> The connection promise created by JoinAsync method. </param>
        protected override void DoJoinAsync(Promise<object> promise)
        {
            BindUserMetadata(UserIdKey, UserId).Then(new Function1<Object, Future<Object>>((o) =>
            {
                return BindUserMetadata(UserNameKey, UserName);
            })).Then(new Function1<Object, Future<Object>>((o) =>
            {
                return SubscribeToSessionChannel();
            }))
            .Then((o) =>
            {
                if (promise.State == FutureState.Pending)
                {
                    promise.Resolve(o);
                }
            })
            .Fail((e) =>
            {
                if (promise.State == FutureState.Pending)
                {
                    promise.Reject(e);
                }
            });
        }

        private Future<object> SubscribeToSessionChannel()
        {
            Promise<object> promise = new Promise<object>();
            try
            {
                ClientExtensions.JoinConference(Client, new JoinConferenceArgs(SessionChannel)
                {
                    OnSuccess = (o) =>
                    {
                        promise.Resolve(o);
                    },
                    OnFailure = (e) =>
                    {
                        promise.Reject(e.Exception);
                    },
                    OnRemoteClient = (remoteClient) =>
                    {
                        Connection connection = CreateConnection(remoteClient);
                        Connections.Add(connection);
                        return connection;
                    }
                });
            }
            catch (Exception e)
            {
                promise.Reject(e);
            }

            return promise;
        }

        public override void Reconnect(PeerClient remoteClient, Connection connection)
        {
            ClientExtensions.ReconnectRemoteClient(Client, remoteClient, connection);
        }

        public override void RenegotiateSessionChannel(Connection connection)
        {
            ClientExtensions.Renegotiate(Client, SessionChannel, connection);
        }
    }
}

using System;
using FM.IceLink;
using FM.IceLink.WebSync4;
using FM.WebSync;
using Newtonsoft.Json;

namespace FirebaseTest11
{
    /// <summary>
    /// Signalling
    /// 
    /// Provides abstract method DoJoinAsync.
    /// Provides concrete methods JoinAsync and LeaveAsync.
    /// 
    /// See the Advanced Topics Manual Signalling Guide for more info.
    /// </summary>
    public abstract class Signalling
    {
        protected string SessionId;
        protected string ServerUrl;
        protected string UserName;
        protected string UserId;
        protected string chatname;
        protected string SessionChannel;
        protected string MetadataChannel;
        protected string UserIdKey = "userId";
        protected string UserNameKey = "userName";
        protected string TextMessageKey = "textMsg";
        protected Function1<PeerClient, Connection> CreateConnection;
        protected Action2<string, string> OnReceivedText;
        protected Client Client;
        protected ConnectionCollection Connections;

        public Signalling(string serverUrl, string name, string chatName, string sessionId, Function1<PeerClient, Connection> createConnection, Action2<string, string> onReceivedText)
        {
            ServerUrl = serverUrl;
            SessionId = sessionId;
            UserName = name;
            chatname = chatName;
            CreateConnection = createConnection;
            UserId = Guid.NewGuid().ToString();
            OnReceivedText = onReceivedText;

            DefineChannels();
        }

        protected abstract void DefineChannels();

        private void CloseAllConnections()
        {
            foreach (Connection c in Connections.Values)
            {
                c.Close();
            }
            Connections.RemoveAll();
        }

        protected Future<object> BindUserMetadata(string k, string v)
        {
            Promise<object> promise = new Promise<object>();
            try
            {
                Client.Bind(new BindArgs(new Record(k, Serializer.SerializeString(v)))
                {
                    OnSuccess = (o) =>
                    {
                        promise.Resolve(null);
                    },
                    OnFailure = (e) =>
                    {
                        promise.Reject(e.Exception);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return promise;
        }

        protected Future<object> UnbindUserMetadata(string k)
        {
            Promise<object> promise = new Promise<object>();
            try
            {
                Client.Unbind(new UnbindArgs(k)
                {
                    OnSuccess = (o) =>
                    {
                        promise.Resolve(null);
                    },
                    OnFailure = (e) =>
                    {
                        promise.Reject(e.Exception);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return promise;
        }

        protected Future<object> UnsubscribeFromChannel(string channel)
        {
            Promise<object> promise = new Promise<object>();
            try
            {
                ClientExtensions.LeaveConference(Client, new LeaveConferenceArgs(channel)
                {
                    OnSuccess = (o) =>
                    {
                        promise.Resolve(o);
                    },
                    OnFailure = (e) =>
                    {
                        promise.Reject(e.Exception);
                    }
                });
            }
            catch (Exception e)
            {
                promise.Reject(e);
            }

            return promise;
        }

        /// <summary>
        /// Initiate the WebSync Client and provides ConnectArgs including
        /// OnSuccess and OnFailure callbacks. The OnSuccess callback will
        /// call to the abstract DoJoinAsync method, while the OnFailure
        /// callback simply rejects the Promise around successful connection.
        /// </summary>
        /// <returns> Future </returns>
        public virtual Future<object> JoinAsync()
        {
            Promise<object> promise = new Promise<object>();
            try
            {
                //Create the signalling client and connect
                Connections = new ConnectionCollection();
                Client = new Client(ServerUrl); 
                Client.Connect(new ConnectArgs() // WebSync 서버에 연결
                {
                    OnSuccess = (p) =>
                    {
                        SubscribeToMetadataChannel()
                        .Then(new Action1<Object>((o) =>
                        {
                            DoJoinAsync(promise);
                        }));
                    },
                    OnFailure = (e) =>
                    {
                        if (promise.State == FutureState.Pending)
                        {
                            promise.Reject(e.Exception);
                        }
                    },
                    OnStreamFailure = (streamFailureArgs) =>
                    {
                        CloseAllConnections();
                    },
                    /*
                        Used to override the client's default backoff.
                        By default the backoff doubles after each failure.
                        For example purposes that gets too long.
                        Add a comment to this line
                     */
                    RetryBackoff = (args) =>
                    {
                        return 1000; // milliseconds
                    }
                });
            }
            catch (Exception ex)
            {
                if (promise.State == FutureState.Pending)
                {
                    promise.Reject(ex);
                }
            }
            return promise;
        }

        // 모든 연결을 닫고 클라이언트 접속을 끊음
        public virtual Future<object> LeaveAsync()
        {
            Promise<object> promise = new Promise<object>();

            CloseAllConnections();

            try
            {
                Client.Disconnect(new DisconnectArgs()
                {
                    OnComplete = (p) =>
                    {
                        promise.Resolve(null);
                    }
                });
            }
            catch (Exception e)
            {
                if (promise.State == FutureState.Pending)
                {
                    promise.Reject(e);
                }
            }

            return promise;
        }

        private Future<object> SubscribeToMetadataChannel()
        {
            Promise<object> promise = new Promise<object>();
            try
            {
                Client.Subscribe(new SubscribeArgs(MetadataChannel)
                {
                    OnSuccess = (o) =>
                    {
                        promise.Resolve(null);
                    },
                    OnFailure = (e) =>
                    {
                        promise.Reject(e.Exception);
                    },
                    OnReceive = (receiveArgs) =>
                    {
                        if (!receiveArgs.WasSentByMe)
                        {
                            Message m = JsonConvert.DeserializeObject<Message>(receiveArgs.DataJson);
                            OnReceivedText(m.userName, m.textMsg);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                promise.Reject(ex);
            }
            return promise;
        }

        public void WriteLine(string message)
        {
            Message m = new Message()
            {
                userName = UserName,
                textMsg = message
            };
            Client.Publish(new PublishArgs(MetadataChannel, JsonConvert.SerializeObject(m)));
        }

        public Data GetData()
        {
            Data data = new Data()
            {
                chatName = chatname
            };

            return data;
        }

        protected abstract void DoJoinAsync(Promise<Object> promise);
        public abstract void Reconnect(PeerClient remoteClient, Connection connection);
        public abstract void RenegotiateSessionChannel(Connection connection);
    }

    class Message
    {
        public string userName { get; set; }
        public string textMsg { get; set; }
    }

    public class Data
    {
        public string chatName { get; set; }
    }
}

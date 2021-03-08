using System;
using FM.WebSync;
using FM.IceLink;
using FM.IceLink.WebSync4;

namespace FirebaseTest11
{
    /// <summary>
    /// Signalling
    /// 
    /// Provides concrete implementation for DoJoinAsync.
    /// 
    /// See the Advanced Topics Manual Signalling Guide for more info.
    /// </summary>
    public class ManualSignalling : Signalling
    {
        protected string OfferTag = "offer";
        protected string AnswerTag = "answer";
        protected string CandidateTag = "candidate";
        private string UserChannel;

        protected Connection ConnectionInUserChannel;

        public ManualSignalling(string serverUrl, string name, string chatName, string sessionId, Function1<PeerClient, Connection> createConnection, Action2<string, string> onReceivedText)
            : base(serverUrl, name, chatName, sessionId, createConnection, onReceivedText)
        { }

        protected override void DefineChannels()
        {
            UserChannel = $"/user/{UserId}";
            SessionChannel = $"/manual-signalling/{SessionId}";
            MetadataChannel = $"{SessionChannel}/metadata";
        }

        private string RemoteUserChannel(string remoteUserId)
        {
            return "/user/" + remoteUserId;
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
                return SubscribeToUserChannel();
                //return null;
            }))
            .Then(new Function1<Object, Future<Object>>((o) =>
            {
                return SubscribeToSessionChannel();
            }))
            .Then((o) =>
            {
                if (promise.State == FutureState.Pending)
                {
                    promise.Resolve(null);
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

        private Future<object> SubscribeToUserChannel()
        {

            Promise<object> promise = new Promise<object>();
            try
            {
                Client.Subscribe(new SubscribeArgs(UserChannel)
                {
                    OnSuccess = (o) =>
                    {
                        promise.Resolve(null);
                    },
                    OnFailure = (e) =>
                    {
                        promise.Reject(e.Exception);
                    },
                    OnReceive = (subscribeReceiveArgs) =>
                    {
                        try
                        {
                            string RemoteClientId = subscribeReceiveArgs.Client.ClientId.ToString();

                            Record Value;
                            if (subscribeReceiveArgs.PublishingClient.BoundRecords.TryGetValue(UserIdKey, out Value))
                            {
                                string RemoteUserId = Serializer.DeserializeString(Value.ValueJson);
                                ConnectionInUserChannel = Connections.GetByExternalId(RemoteClientId);
                                if (subscribeReceiveArgs.Tag.Equals(CandidateTag))
                                {
                                    if (ConnectionInUserChannel == null)
                                    {
                                        ConnectionInUserChannel = CreateConnectionAndWireOnLocalCandidate(new PeerClient(subscribeReceiveArgs.PublishingClient.ClientId.ToString(), subscribeReceiveArgs.PublishingClient.BoundRecords), RemoteUserId);
                                        ConnectionInUserChannel.ExternalId = RemoteClientId;
                                        Connections.Add(ConnectionInUserChannel);
                                    }

                                    Log.Info("Received candidate from remote peer.");
                                    ConnectionInUserChannel.AddRemoteCandidate(Candidate.FromJson(subscribeReceiveArgs.DataJson)).Fail((e) =>
                                    {
                                        Log.Error("Could not process candidate from remote peer.", e);
                                    });
                                }
                                else if (subscribeReceiveArgs.Tag.Equals(OfferTag))
                                {
                                    Log.Info("Received offer from remote peer.");

                                    if (ConnectionInUserChannel == null)
                                    {
                                        ConnectionInUserChannel = CreateConnectionAndWireOnLocalCandidate(new PeerClient(subscribeReceiveArgs.PublishingClient.ClientId.ToString(), subscribeReceiveArgs.PublishingClient.BoundRecords), RemoteUserId);
                                        ConnectionInUserChannel.ExternalId = RemoteClientId;
                                        Connections.Add(ConnectionInUserChannel);

                                        ConnectionInUserChannel.SetRemoteDescription(SessionDescription.FromJson(subscribeReceiveArgs.DataJson))
                                        .Then(new Function1<SessionDescription, Future<SessionDescription>>((offer) =>
                                        {
                                            return ConnectionInUserChannel.CreateAnswer();
                                        }))
                                        .Then(new Function1<SessionDescription, Future<SessionDescription>>((answer) =>
                                        {
                                            return ConnectionInUserChannel.SetLocalDescription(answer);
                                        }))
                                        .Then((answer) =>
                                        {
                                            try
                                            {
                                                Client.Publish(new PublishArgs(RemoteUserChannel(RemoteUserId), answer.ToJson(), AnswerTag)
                                                {
                                                    OnSuccess = (a) =>
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
                                        })
                                        .Fail((e) =>
                                        {
                                            Log.Error("Could not process offer from remote peer.", e);
                                        });
                                    }

                                }
                                else if (subscribeReceiveArgs.Tag.Equals(AnswerTag))
                                {
                                    if (ConnectionInUserChannel != null)
                                    {
                                        Log.Info("Received answer from remote peer");
                                        ConnectionInUserChannel.SetRemoteDescription(SessionDescription.FromJson(subscribeReceiveArgs.DataJson)).Fail((e) =>
                                        {
                                            Log.Error("Could not process answer from remote peer.", e);
                                        });
                                    }
                                    else
                                    {
                                        Log.Error("Received answer, but connection does not exist!");
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(e.Message);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return promise;
        }

        /// <summary>
        /// Handles subscribing to the session channel and connecting to subscribed clients.
        /// 
        /// See the Subscribing to the Session Channel and Connecting to Subscribed Clients sections
        /// of the Advanced Topics Manual Signalling Guide for more info.
        /// </summary>
        /// <returns> Future </returns>
        private Future<object> SubscribeToSessionChannel()
        {

            Promise<object> promise = new Promise<object>();
            try
            {
                SubscribeArgs SubscribeArgs = new SubscribeArgs(SessionChannel)
                {
                    OnSuccess = (args) =>
                    {
                        promise.Resolve(null);
                    },
                    OnFailure = (e) =>
                    {
                        promise.Reject(e.Exception);
                    }
                };

                FM.WebSync.Subscribers.SubscribeArgsExtensions.SetOnClientSubscribe(SubscribeArgs, (o) =>
                {

                    string RemoteClientId = o.Client.ClientId.ToString();

                    Record Value;
                    if (o.SubscribedClient.BoundRecords.TryGetValue(UserIdKey, out Value))
                    {
                        string RemoteUserId = Serializer.DeserializeString(Value.ValueJson);
                        var Con = CreateConnectionAndWireOnLocalCandidate(new PeerClient(o.SubscribedClient.ClientId.ToString(), o.SubscribedClient.BoundRecords), RemoteUserId);
                        Con.ExternalId = RemoteClientId;
                        Connections.Add(Con);

                        Con.CreateOffer().Then(new Function1<SessionDescription, Future<SessionDescription>>((offer) =>
                        {
                            return Con.SetLocalDescription(offer);
                        }))
                        .Then((offer) =>
                        {
                            try
                            {
                                Client.Publish(new PublishArgs(RemoteUserChannel(RemoteUserId), offer.ToJson(), OfferTag)
                                {
                                    OnSuccess = (PublishSuccessArgs) =>
                                    {
                                        Log.Info("Published offer to remote peer.");
                                    },
                                    OnFailure = (e) =>
                                    {
                                        Log.Error("Could not publish offer to remote peer.", e.Exception);
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Message);
                            }
                        });
                    }
                });

                FM.WebSync.Subscribers.SubscribeArgsExtensions.SetOnClientUnsubscribe(SubscribeArgs, (o) =>
                {

                    string RemoteClientId = o.Client.ClientId.ToString();

                    Connection Con = Connections.GetById(RemoteClientId);
                    if (Con != null)
                    {
                        Connections.Remove(Con);
                        Con.Close();
                    }
                });

                Client.Subscribe(SubscribeArgs);
            }
            catch (Exception ex)
            {
                Log.Error("Could not subscribe to session channel.", ex);
            }
            return promise;
        }

        private Connection CreateConnectionAndWireOnLocalCandidate(PeerClient remoteClient, string remoteUserId)
        {
            Connection Con = CreateConnection.Invoke(remoteClient);

            Con.OnLocalCandidate += (Connection, Candidate) =>
            {
                try
                {
                    Client.Publish(new PublishArgs(RemoteUserChannel(remoteUserId), Candidate.ToJson(), CandidateTag)
                    {
                        OnSuccess = (PublishSuccessArgs) =>
                        {
                            Log.Info("Published candidate to remote peer.");
                        },
                        OnFailure = (e) =>
                        {
                            Log.Error("Could not publish candidate to remote peer.", e.Exception);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.Error("Could not publish candidate to remote peer.");
                }
            };

            return Con;
        }

        public override void Reconnect(PeerClient remoteClient, Connection connection)
        {
            throw new NotImplementedException();
        }

        public override void RenegotiateSessionChannel(Connection connection)
        {
            throw new NotImplementedException();
        }
    }
}

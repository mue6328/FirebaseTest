using Android.Content;
using Android.Media.Projection;
using Android.Views;
using FM.IceLink;
using FM.IceLink.Android;
using FM.IceLink.WebSync4;
using System;
using System.Collections.Generic;

namespace FirebaseTest11
{
    public class App
    {
        // This flag determines the signalling mode used.
        // Note that Manual and Auto signalling do not Interop.
        private static bool SIGNAL_MANUALLY = false;
        private Signalling _Signalling;

        private IOnReceivedTextListener _TextListener;
        private static Future<DtlsCertificate> _CertificatePromise;
        private static DtlsCertificate _Certificate;

        public string SessionId
        { get; set; }

        public string Name
        { get; set; }

        public string ChatName
        { get; set; }

        public bool EnableAudioSend
        { get; set; }

        public bool EnableAudioReceive
        { get; set; }

        public bool EnableVideoSend
        { get; set; }

        public bool EnableVideoReceive
        { get; set; }

        public bool EnableScreenShare
        { get; set; }

        public MediaProjection MediaProjection
        { get; set; }

        private IceServer[] _IceServers = new IceServer[]
        {
            new IceServer("stun:turn.frozenmountain.com:3478"),
            //NB: url "turn:turn.icelink.fm:443" implies that the relay server supports both TCP and UDP
            //if you want to restrict the network protocol, use "turn:turn.icelink.fm:443?transport=udp"
            //or "turn:turn.icelink.fm:443?transport=tcp". For further info, refer to RFC 7065 3.1 URI Scheme Syntax
            new IceServer("turn:turn.frozenmountain.com:80", "test", "pa55w0rd!"),
            new IceServer("turns:turn.frozenmountain.com:443", "test", "pa55w0rd!")
        };

        private Dictionary<View, RemoteMedia> _MediaTable;

        private String _WebsyncServerUrl = "https://v4.websync.fm/websync.ashx"; // WebSync On-Demand

        private LocalMedia _LocalMedia = null;
        private FM.IceLink.Android.LayoutManager _LayoutManager = null;

        private FirebaseTest11.AecContext _AecContext;
        private bool _EnableH264 = false;

        private Context _Context = null;

        private App(Context context)
        {
            this._Context = context.ApplicationContext;

            _MediaTable = new Dictionary<View, RemoteMedia>();

            EnableAudioSend = true;
            EnableAudioReceive = true;
            EnableVideoSend = true;
            EnableVideoReceive = true;

            // Log to the console.
            FM.IceLink.Log.Provider = new FM.IceLink.Android.LogProvider(LogLevel.Debug);
            FM.IceLink.Log.LogLevel = LogLevel.Debug;
        }

        private static App app;

        // Singleton 패턴 사용
        // Singleton 패턴 : 특정 클래스에 대한 인스턴스를 단 한 번만 Static 메모리 영역에 할당하고 해당 클래스에 대한 생성자를 여러 번 호출하더라도 최초에 생성된 객체를 반환함
        public static App GetInstance(Context context)
        {
            if (app == null)
            {
                app = new App(context);
            }
            return app;
        }

        public static void GenerateCertificate()
        {
            Promise<DtlsCertificate> promise = new Promise<DtlsCertificate>();
            _CertificatePromise = promise;

            ManagedThread.Dispatch(() =>
            {
                promise.Resolve(DtlsCertificate.GenerateCertificate());
            });
        }

        public void DownloadH264()
        {
            string downloadPath = _Context.FilesDir.AbsoluteFile.AbsolutePath;
            FM.IceLink.OpenH264.Utility.DownloadOpenH264(downloadPath).WaitForResult();
            Java.Lang.JavaSystem.Load(PathUtility.CombinePaths(downloadPath, FM.IceLink.OpenH264.Utility.GetLibraryName()));
            _EnableH264 = true;
        }

        public Future<FM.IceLink.LocalMedia> StartLocalMedia(VideoChatFragment fragment)
        {
            return _CertificatePromise.Then(new Function1<DtlsCertificate, Future<FM.IceLink.LocalMedia>>((cert) =>
            {
                _Certificate = cert;

                // Set up the local media.
                _AecContext = new AecContext();

                View localView;

                if (EnableScreenShare)
                {
                    _LocalMedia = new ScreenShareLocalMedia(MediaProjection, _Context, _EnableH264, !EnableAudioSend, !EnableVideoSend, _AecContext);
                    localView = (View)(_LocalMedia as ScreenShareLocalMedia).View;
                }
                else
                {
                    _LocalMedia = new CameraLocalMedia(_Context, _EnableH264, !EnableAudioSend, !EnableVideoSend, _AecContext);
                    localView = (View)(_LocalMedia as CameraLocalMedia).View;
                }

                // Set up the layout manager.
                fragment.Activity.RunOnUiThread(() =>
                {
                    _LayoutManager = new FM.IceLink.Android.LayoutManager(VideoChatFragment.Container);
                    _LayoutManager.SetLocalView(localView);
                });

                fragment.RegisterForContextMenu(localView);
                localView.SetOnTouchListener(fragment);

                // Start the local media.
                return _LocalMedia.Start();
            }));
        }

        // 모든 미디어 세션이 끝날 때
        public Future<FM.IceLink.LocalMedia> StopLocalMedia()
        {
            return Promise<FM.IceLink.LocalMedia>.WrapPromise(new Function0<Future<FM.IceLink.LocalMedia>>(() =>
            {
                if (_LocalMedia == null)
                {
                    throw new Exception("Local media has already been stopped.");
                }

                // Stop the local media.
                return _LocalMedia.Stop().Then(new Action1<FM.IceLink.LocalMedia>((m) =>
                 {
                     // Tear down the layout manager.
                     var layoutManager = _LayoutManager;
                     if (layoutManager != null)
                     {
                         layoutManager.RemoveRemoteViews();
                         layoutManager.UnsetLocalView();
                         _LayoutManager = null;
                     }

                     // Tear down the local media.
                     if (_LocalMedia != null)
                     {
                         _LocalMedia.Destroy(); // _LocalMedia.Destroy() will also destroy AecContext.
                         _LocalMedia = null;
                     }
                 }));
            }));
        }

        public Future<object> JoinAsync(VideoChatFragment fragment, TextChatFragment textChat)
        {
            //_TextListener = textChat;
            if (SIGNAL_MANUALLY)
            {
                _Signalling = ManualSignalling(fragment);
            }
            else
            {
                _Signalling = AutoSignalling(fragment);
            }

            return _Signalling.JoinAsync();
        }

        public AutoSignalling AutoSignalling(VideoChatFragment fragment)
        {
            return new AutoSignalling(_WebsyncServerUrl, Name, ChatName, SessionId, new Function1<PeerClient, Connection>((remoteClient) =>
            {
                return Connection(fragment, remoteClient);
            }), new Action2<string, string>((n, m) =>
            {
                //_TextListener.OnReceivedText(n, m);
            }));
        }

        public ManualSignalling ManualSignalling(VideoChatFragment fragment)
        {
            return new ManualSignalling(_WebsyncServerUrl, Name, ChatName, SessionId, new Function1<PeerClient, Connection>((remoteClient) =>
            {
                return Connection(fragment, remoteClient);
            }), new Action2<string, string>((n, m) =>
            {
                //_TextListener.OnReceivedText(n, m);
            }));
        }

        private Connection Connection(VideoChatFragment fragment, PeerClient remoteClient)
        {
            string peerName = "Unknown";
            FM.WebSync.Record r;
            if (remoteClient.BoundRecords != null && remoteClient.BoundRecords.TryGetValue("userName", out r))
            {
                if (!String.IsNullOrEmpty(r.ValueJson))
                {
                    peerName = r.ValueJson.Trim('"');
                }
            }

            // Create connection to remote client.
            RemoteMedia remoteMedia = new RemoteMedia(_Context, _EnableH264, false, false, _AecContext);
            AudioStream audioStream = new AudioStream(_LocalMedia, remoteMedia)
            {
                LocalSend = EnableAudioSend,
                LocalReceive = EnableAudioReceive
            };

            VideoStream videoStream = new VideoStream(_LocalMedia, remoteMedia)
            {
                LocalSend = EnableVideoSend,
                LocalReceive = EnableVideoReceive
            };

            Connection connection;

            if (remoteMedia.View != null)
            {
                // Add the remote view to the layout.
                _LayoutManager.AddRemoteView(remoteMedia.Id, remoteMedia.View);
                _MediaTable.Add(remoteMedia.View, remoteMedia);
                fragment.RegisterForContextMenu(remoteMedia.View);
                remoteMedia.View.SetOnTouchListener(fragment);
            }

            connection = new Connection(new FM.IceLink.Stream[] { audioStream, videoStream });

            connection.IceServers = _IceServers;

            connection.OnStateChange += (c) =>
            {
                if (c.State == ConnectionState.Connected)
                {
                    //_TextListener.onPeerJoined(peerName);
                }
                // Remove the remote view from the layout.
                else if (c.State == ConnectionState.Closing || c.State == ConnectionState.Failing)
                {
                    var layoutManager = _LayoutManager;
                    if (layoutManager != null)
                    {
                        layoutManager.RemoveRemoteView(remoteMedia.Id);
                    }
                    _MediaTable.Remove(remoteMedia.View);
                    remoteMedia.Destroy();
                }
                else if (c.State == ConnectionState.Failed)
                {
                    //_TextListener.onPeerLeft(peerName);
                    if (!SIGNAL_MANUALLY)
                        _Signalling.Reconnect(remoteClient, c);
                }
                else if (c.State == ConnectionState.Closed)
                {
                    //_TextListener.onPeerLeft(peerName);
                }
            };

            connection.LocalDtlsCertificate = _Certificate;

            return connection;
        }

        public Future<object> LeaveAsync()
        {
            return _Signalling.LeaveAsync();
        }

        private bool UsingFrontVideoDevice = true;

        public void UseNextVideoDevice()
        {
            if (_LocalMedia != null && _LocalMedia.VideoSource != null)
            {
                _LocalMedia.ChangeVideoSourceInput(UsingFrontVideoDevice ?
                    ((CameraSource)_LocalMedia.VideoSource).BackInput :
                    ((CameraSource)_LocalMedia.VideoSource).FrontInput);

                UsingFrontVideoDevice = !UsingFrontVideoDevice;
            }
        }

        public Future<object> PauseLocalVideo()
        {
            if (_LocalMedia != null && !EnableScreenShare)
            {
                VideoSource videoSource = _LocalMedia.VideoSource;
                if (videoSource != null)
                {
                    if (videoSource.State == MediaSourceState.Started)
                    {
                        return videoSource.Stop();
                    }
                }
            }
            return Promise<object>.ResolveNow<object>(null);
        }

        public Future<object> ResumeLocalVideo()
        {
            if (_LocalMedia != null)
            {
                VideoSource videoSource = _LocalMedia.VideoSource;
                if (videoSource != null)
                {
                    if (videoSource.State == MediaSourceState.Stopped)
                    {
                        return videoSource.Start();
                    }
                }
            }
            return Promise<object>.ResolveNow<object>(null);
        }

        public void IsRecordingAudio(View v, bool record)
        {
            View view;
            if (EnableScreenShare)
            {
                view = (this._LocalMedia as ScreenShareLocalMedia).View;
            }
            else
            {
                view = (this._LocalMedia as CameraLocalMedia).View;
            }

            if (view == v)
            {
                if (_LocalMedia.IsRecordingAudio != record)
                {
                    if (EnableScreenShare)
                    {
                        (this._LocalMedia as ScreenShareLocalMedia).ToggleAudioRecording();
                    }
                    else
                    {
                        (this._LocalMedia as CameraLocalMedia).ToggleAudioRecording();
                    }
                }
            }
            else
            {
                RemoteMedia remote = _MediaTable[v];
                if (remote.IsRecordingAudio != record)
                {
                    remote.ToggleAudioRecording();
                }
            }
        }

        public bool IsRecordingAudio(View v)
        {
            View view = GetView();

            if (view == v)
            {
                return _LocalMedia.IsRecordingAudio;
            }
            else
            {
                return _MediaTable[v].IsRecordingAudio;
            }
        }

        public void IsRecordingVideo(View v, bool record)
        {
            View view = GetView();

            if (view == v)
            {
                if (_LocalMedia.IsRecordingVideo != record)
                {
                    if (EnableScreenShare)
                    {
                        (this._LocalMedia as ScreenShareLocalMedia).ToggleVideoRecording();
                    }
                    else
                    {
                        (this._LocalMedia as CameraLocalMedia).ToggleVideoRecording();
                    }
                }
            }
            else
            {
                RemoteMedia remote = _MediaTable[v];
                if (remote.IsRecordingVideo != record)
                {
                    remote.ToggleVideoRecording();
                }
            }
        }

        public bool IsRecordingVideo(View v)
        {
            View view = GetView();

            if (view == v)
            {
                return _LocalMedia.IsRecordingVideo;
            }
            else
            {
                return _MediaTable[v].IsRecordingVideo;
            }
        }



        public void AudioMuted(View v, bool mute)
        {
            View view = GetView();

            if (GetView() == v)
            {
                _LocalMedia.AudioMuted = mute;
            }
            else
            {
                _MediaTable[v].AudioMuted = mute;
            }
        }

        public bool AudioMuted(View v)
        {
            if (GetView() == v)
            {
                return _LocalMedia.AudioMuted;
            }
            else
            {
                return _MediaTable[v].AudioMuted;
            }
        }

        public void VideoMuted(View v, bool mute)
        {
            if (GetView() == v)
            {
                _LocalMedia.VideoMuted = mute;
            }
            else
            {
                _MediaTable[v].VideoMuted = mute;
            }
        }

        public bool VideoMuted(View v)
        {
            if (GetView() == v)
            {
                return _LocalMedia.VideoMuted;
            }
            else
            {
                return _MediaTable[v].VideoMuted;
            }
        }

        public void WriteLine(string message)
        {
            _Signalling.WriteLine(message);
        }

        private View GetView()
        {
            if (EnableScreenShare)
            {
                return (this._LocalMedia as ScreenShareLocalMedia).View;
            }
            else
            {
                return (this._LocalMedia as CameraLocalMedia).View;
            }
        }

        public interface IOnReceivedTextListener
        {
            //void OnReceivedText(String name, String message);
            
        }

       /* public Data GetData()
        {
            Data data = new Data()
            {
                chatName = chatname
            };

            return data;
        }

        public class Data
        {
            public string chatName { get; set; }
        }*/
    }
}
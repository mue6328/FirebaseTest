using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Support.V7.App;
using Firebase.Database;
using Firebase;
using Refit;
using System;
using Java.Lang;
using Java.Util;
using Fragment = Android.Support.V4.App.Fragment;
using Java.Net;
using Java.IO;
using Android.Util;
using Java.Interop;
using System.IO;
using Android.Support.V4.View;
using Android.Support.Design.Widget;
using Android;
using Android.Support.V4.Content;
using FM.IceLink;
using Android.Content.PM;

namespace FirebaseTest11
{
    [Activity(Label = "ChatActivity", Theme = "@style/AppTheme")]
    class ChatActivity : AppCompatActivity, VideoChatFragment.IOnVideoReadyListener, TextChatFragment.IOnTextReadyListener, ViewPager.IOnPageChangeListener
    {
        private bool localMediaStarted = false;
        private bool conferenceStarted = false;
        private bool VideoReady = false;
        private bool TextReady = false;
        private App app;

        private ViewPager viewPager;
        private TabLayout tabLayout;


        public void onTextReady()
        {
            TextReady = true;

            if (VideoReady && TextReady)
            {
                Start();
            }
        }

        public void onVideoReady()
        {
            VideoReady = true;

            if (VideoReady && TextReady)
            {
                Start();
            }
        }

        public override void OnBackPressed()
        {
            Stop();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ChatActivity);

            app = App.GetInstance(this);

            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            //저장된 값이 유효하면 저장되어있는 값 key로 가져오기
            if (savedInstanceState != null)
            {
                localMediaStarted = savedInstanceState.GetBoolean("localMediaStarted", false);
                conferenceStarted = savedInstanceState.GetBoolean("conferenceStarted", false);
            }

            //영상화면과 채팅화면 
            viewPager = (ViewPager)FindViewById(Resource.Id.pager);
            PagerAdapter adapter = new PagerAdapter(SupportFragmentManager, this);
            viewPager.Adapter = adapter;

            viewPager.AddOnPageChangeListener(this);

            tabLayout = (TabLayout)FindViewById(Resource.Id.tab_layout);
            tabLayout.SetupWithViewPager(viewPager);

            // Iterate over all tabs and set the custom view
            for (int i = 0; i < tabLayout.TabCount; i++)
            {
                TabLayout.Tab tab = tabLayout.GetTabAt(i);
                tab.SetCustomView(adapter.GetTabView(i));
            }
        }

        private Action0 startFn;

        private void Start()
        {
            if (!localMediaStarted)
            {
                IList<Fragment> fragments = SupportFragmentManager.Fragments;
                VideoChatFragment videoChatFragment = (VideoChatFragment)(fragments[0] is VideoChatFragment ? fragments[0] : fragments[1]);
                TextChatFragment textChatFragment = (TextChatFragment)(fragments[0] is TextChatFragment ? fragments[0] : fragments[1]);

                startFn = () =>
                {
                    app.StartLocalMedia(videoChatFragment).Then(new FM.IceLink.Function1<LocalMedia, Future<object>>((lm) =>
                    {
                        return app.JoinAsync(videoChatFragment, textChatFragment).Then((p) => conferenceStarted = true);
                    }), (ex) =>
                    {
                        FM.IceLink.Log.Error("Could not start local media.", ex);
                        Alert(ex.Message);
                    }).Fail((e) =>
                    {
                        FM.IceLink.Log.Error("Could not join conference.", e);
                        Alert(e.Message);
                    });
                };
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                {
                    List<System.String> requiredPermissions = new List<string>();

                    if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.RecordAudio) != Permission.Granted)
                    {
                        requiredPermissions.Add(Manifest.Permission.RecordAudio);
                    }
                    if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.Camera) != Permission.Granted)
                    {
                        requiredPermissions.Add(Manifest.Permission.Camera);
                    }
                    if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ReadPhoneState) != Permission.Granted)
                    {
                        requiredPermissions.Add(Manifest.Permission.ReadPhoneState);
                    }

                    if (requiredPermissions.Count == 0)
                    {
                        startFn.Invoke();
                    }
                    else
                    {
                        if (ShouldShowRequestPermissionRationale(Manifest.Permission.Camera) || ShouldShowRequestPermissionRationale(Manifest.Permission.RecordAudio))
                        {
                            Toast.MakeText(this, "Access to camera, microphone, and phone call state is required", ToastLength.Short).Show();
                        }
                        RequestPermissions(requiredPermissions.ToArray(), 1);
                    }
                }
                else
                {
                    startFn.Invoke();
                }
                localMediaStarted = true;
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == 1)
            {
                System.Boolean permissionsGranted = true;
                foreach (int grantResult in grantResults)
                {
                    if (grantResult != (int)Permission.Granted)
                    {
                        permissionsGranted = false;
                    }
                }

                if (permissionsGranted)
                {
                    startFn.Invoke();
                }
                else
                {
                    Toast.MakeText(this, "Cannot connect without access to camera, microphone, and storage", ToastLength.Short).Show();
                    for (int i = 0; i < grantResults.Length; i++)
                    {
                        if (grantResults[i] != Permission.Granted)
                        {
                            FM.IceLink.Log.Debug(System.String.Format("Permission to {0} not granted.", permissions[i]));
                        }
                    }
                    Stop();
                }
            }
            else
            {
                Toast.MakeText(this, "Unknown permission requested", ToastLength.Short).Show();
                FM.IceLink.Log.Debug(System.String.Format("Unknown permission requested."));
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
        }

        private void Stop()
        {
            if (localMediaStarted && conferenceStarted)
            {
                app.LeaveAsync().Fail((e) =>
                {
                    FM.IceLink.Log.Error("Could not leave conference.", e);
                    Alert(e.Message);
                });

                app.StopLocalMedia().Then(new Action1<LocalMedia>((lm) =>
                {
                    Finish();
                }))
                .Fail((e) =>
                {
                    FM.IceLink.Log.Error("Could not stop local media.", e);
                    Alert(e.Message);
                });
            }
            else
            {
                Finish();
            }
            localMediaStarted = false;
        }

        // 오류 알림 띄우기
        public void Alert(System.String format, params object[] args)
        {
            System.String text = string.Format(format, args);
            Activity activity = this;
            this.RunOnUiThread(() =>
            {
                Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(activity);
                alert.SetMessage(text);
                alert.SetPositiveButton("OK", new EventHandler<DialogClickEventArgs>((sender, a) => { }));
                alert.Show();
            });
        }
        /*public static ChatDTO Cast(Java.Lang.Object obj) where ChatDTO : class
        {
            var propertyInfo = obj.GetType().GetProperty("Instance");
            return propertyInfo == null ? null : propertyInfo.GetValue(obj, null) as ChatDTO;
        }*/



        ArrayAdapter<string> adapter;

        public void OnCancelled(DatabaseError error)
        {
            
        }

        public void OnChildAdded(DataSnapshot snapshot, string previousChildName)
        {
            addMessage(snapshot, adapter);
        }

        public void OnChildChanged(DataSnapshot snapshot, string previousChildName)
        {
            
        }

        public void OnChildMoved(DataSnapshot snapshot, string previousChildName)
        {
            
        }

        public void OnChildRemoved(DataSnapshot snapshot)
        {
            removeMessage(snapshot, adapter);
        }

        private void addMessage(DataSnapshot dataSnapshot, ArrayAdapter<string> adapter)
        {
            var chatMsg = dataSnapshot.Child("message")?.GetValue(true)?.ToString();
            var chatUser = dataSnapshot.Child("username")?.GetValue(true)?.ToString();


            //Console.WriteLine("user : " + chatUser + " msg " + chatMsg);
            //ChatDTO chatd = new ChatDTO("", "");
            //var chatDTO = dataSnapshot.GetValue(Java.Lang.Class.FromType(typeof(chatd)));
            adapter.Add(chatUser + " : " + chatMsg);
        }

        private void removeMessage(DataSnapshot dataSnapshot, ArrayAdapter<string> adapter)
        {

            var chatMsg = dataSnapshot.Child("message")?.GetValue(true)?.ToString();
            var chatUser = dataSnapshot.Child("username")?.GetValue(true)?.ToString();

            adapter.Remove(chatUser + " : " + chatMsg);
        }

        public void onNewMessage()
        {
            
        }

        public void OnPageScrollStateChanged(int state)
        {
            
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
        {
            
        }

        public void OnPageSelected(int position)
        {
            
        }

        /*private class TCPclient : Java.Lang.Object, IRunnable
        {
            private const string serverIP = "121.190.170.194";
            private const int serverPort = 22;
            private Socket inetSocket = null;
            private string msg;
            private string return_msg;

            
            public TCPclient(string msg)
            {
                this.msg = msg;
            }

            public void Run()
            {
                try
                {
                    inetSocket = new Socket(serverIP, serverPort);
                    try
                    {
                        Log.Debug("TCP", "Sending: " + msg);
                        PrintWriter outputWriter = new PrintWriter(new BufferedWriter(
                                new OutputStreamWriter(inetSocket.OutputStream)), true);

                        BufferedReader inputReader = new BufferedReader(
                                new InputStreamReader(inetSocket.InputStream));
                        return_msg = inputReader.ReadLine();

                        Log.Debug("TCP", "Receive: " + return_msg);
                    }
                    catch (Java.Lang.Exception e)
                    {
                        Log.Error("TCP", "Error1 ", e);
                    }
                    finally
                    {
                        inetSocket.Close();
                    }
                }
                catch (Java.Lang.Exception e)
                {
                    Log.Error("TCP", "Error2 ", e);
                }
            }
        }*/

    }
}
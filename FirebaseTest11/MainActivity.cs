using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Gms.Common;
using Android.Runtime;
using Android.Widget;
using Android.Util;
using Firebase.Iid;
using Firebase.Messaging;
using Java.Util;
using System;
using Android.Content;
using Firebase;
using Firebase.Database;
using static Android.Widget.AdapterView;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using Android.Text;
using Android.Media.Projection;
using Android.Views;

namespace FirebaseTest11
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IChildEventListener
    {
        static readonly string TAG = "MainActivity";

        internal static readonly string CHANNEL_ID = "my_notification_channel";
        internal static readonly int NOTIFICATION_ID = 100;

        private EditText userChat;
        //private EditText userEdit;
        private Button userNext;
        private ListView chatList;
        private EditText sessionText;
        private Switch audioSendCheckBox;
        private Switch audioReceiveCheckBox;
        private Switch videoSendCheckBox;
        private Switch videoReceiveCheckBox;
        private Switch screenShareCheckBox;
        //ArrayAdapter<string> adapter;

        private FirebaseDatabase database;
        private DatabaseReference reference;

        private App app;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            App.GenerateCertificate();

            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.NoTitle);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            Window.SetSoftInputMode(SoftInput.StateAlwaysHidden);

            SetContentView(Resource.Layout.activity_main);

            //FirebaseApp.InitializeApp(this);

            FM.LiveSwitch.Log.LogLevel = FM.LiveSwitch.LogLevel.Debug;

            database = FirebaseDatabase.Instance;
            reference = database.Reference;

            var token = FirebaseInstanceId.Instance.Token;

            Console.WriteLine("토큰 : " + token);

            if (Intent.Extras != null)
            {
                foreach (var key in Intent.Extras.KeySet())
                {
                    var value = Intent.Extras.GetString(key);
                    Log.Debug(TAG, "Key: {0} Value: {1}", key, value);
                }
            }



            IsPlayServicesAvailable();

            CreateNotificationChannel();

            userChat = FindViewById<EditText>(Resource.Id.user_chat);
            //userEdit = FindViewById<EditText>(Resource.Id.user_edit);
            userNext = FindViewById<Button>(Resource.Id.user_next);
            chatList = FindViewById<ListView>(Resource.Id.chat_list);
            sessionText = FindViewById<EditText>(Resource.Id.sessionText);
            audioSendCheckBox = (Switch)FindViewById(Resource.Id.audioSendSwitch);
            audioReceiveCheckBox = (Switch)FindViewById(Resource.Id.audioReceiveSwitch);
            videoSendCheckBox = (Switch)FindViewById(Resource.Id.videoSendSwitch);
            videoReceiveCheckBox = (Switch)FindViewById(Resource.Id.videoReceiveSwitch);
            screenShareCheckBox = (Switch)FindViewById(Resource.Id.screenShareSwitch);

            if (!FM.IceLink.Android.Utility.IsSDKVersionSupported(BuildVersionCodes.Lollipop))
            {
                //screenShareCheckBox.Enabled = false;
            }

            try
            {
                //사용가능한 키인지 확인
                try
                {
                    string key;
                    /*using (StreamReader sr = new StreamReader(Assets.Open("icelink.key")))
                    {*/
                        key = "fmeyJpZCI6IjUzNGZkN2Y0LTQzYWItNDMxZS04N2YxLWFjZGYwMDQ5YjI1ZCIsImFpZCI6IjAxNzg2ZGI3LTQ4ZTItNDk3Ny1iNjhiLWFjZGYwMDQ4YWQ2NSIsInBjIjoiSWNlTGluayIsIml0Ijp0cnVlLCJ2ZiI6NjM3NTAyNTYwOTkyOTcwMDAwLCJ2dCI6NjM3NTI4NDgwOTkyOTcwMDAwfQ==.nrlO8ND7K6PqvyqSaZKLLEcgib1u7isl5Y+Lv0NzNsVSZR2SBmnv226HKCEbBQHDOLXrvVtO8aLQC6PRPT7rONDSv6Q2yDv3wYBsd1A09myKoL0rKLAWPIaTDiE1FPPOCI8hjMGfBdBR2QfoBhpVpJH+oQTFgIHnX2NFe7pesWI=";
                    //}

                    FM.IceLink.License.SetKey(key);
                }
                catch (Exception)
                {
                    Alert("Invalid icelink key.");
                }

                //APP class 인스턴스 받아오기
                app = App.GetInstance(this);

                //접속 id로 사용한 6자리 코드 랜덤으로 생성
                // Create a random 6 digit number for the new session ID.
                sessionText.Text = new FM.IceLink.Randomizer().Next(100000, 999999).ToString();
                sessionText.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(6) });

                //이름 랜덤으로 생성해서 텍스트뷰에 추가
                /*nameText.Text = Names[new FM.IceLink.Randomizer().Next(Names.Length)];
                nameText.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(20) });*/

                //접속 버튼 클릭 시
                userNext.Click += (sender, e) =>
                {
                    SwitchToVideoChat(sessionText.Text, userChat.Text);
                };

                //audioSend checkbox
                audioSendCheckBox.CheckedChange += (compoundButton, b) =>
                {
                    app.EnableAudioSend = b.IsChecked;
                };

                //audioReceive checkbox
                audioReceiveCheckBox.CheckedChange += (compoundButton, b) =>
                {
                    app.EnableAudioReceive = b.IsChecked;
                };

                //videoReceive checkbox
                videoReceiveCheckBox.CheckedChange += (compoundButton, b) =>
                {
                    app.EnableVideoReceive = b.IsChecked;
                };

                //videoSend checkbox
                videoSendCheckBox.CheckedChange += (compoundButton, b) =>
                {
                    app.EnableVideoSend = b.IsChecked;
                };

                //screenShare checkbox
                screenShareCheckBox.CheckedChange += (compoundButton, b) =>
                {
                    app.EnableScreenShare = b.IsChecked;
                };



                if (FM.IceLink.OpenH264.Utility.IsSupported())
                {
                    // Don't allow join until H.264 is downloaded (in background)
                    Toast.MakeText(this, "Downloading OpenH264 Library...", ToastLength.Short).Show();
                    new Thread(new ThreadStart(() =>
                    {
                        app.DownloadH264();
                        RunOnUiThread(() =>
                        {
                            userNext.Enabled = true;
                            Toast.MakeText(this, "Download Complete", ToastLength.Short).Show();
                        });
                    })).Start();
                }
                else
                {
                    userNext.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                FM.IceLink.Log.Error("SessionSelector", ex);
            }

            Toast.MakeText(this, token, ToastLength.Long).Show();

            //TcpClient tcpClient

            /*userNext.Click += (sender, args) =>
            {
                if (userChat.ToString().Equals(""))
                    return;

                var intent = new Intent(this, typeof(ChatActivity));

                app.ChatName = userChat.Text;
                *//*intent.PutExtra("chatName", userChat.Text);
                //intent.PutExtra("userName", userEdit.Text);
                intent.PutExtra("token", token);*//*
                StartActivity(intent);
            };*/

            showChatList();

           /* chatList.ItemClick += delegate (object sender, ItemClickEventArgs e)
            {
                if (userChat.ToString().Equals(""))
                    return;

                var intent = new Intent(this, typeof(ChatActivity));
                intent.PutExtra("chatName", chatList.GetItemAtPosition(e.Position).ToString());
                intent.PutExtra("token", token);
                StartActivity(intent);
                Console.WriteLine("chatname" + chatList.GetItemAtPosition(e.Position));
            };*/
            /*FirebaseInstanceId.Instance.GetInstanceId().AddOnSuccessListener(this, IInstanceIdResult) => {
                string token = IInstanceIdResult.getToken();
                
            }*/
            /*var logTokenButton = FindViewById<Button>(Resource.Id.logTokenButton);
            logTokenButton.Click += delegate
            {
                Log.Debug(TAG, "InstanceID token: " + FirebaseInstanceId.Instance.Token);
            };*/

            /*var subscribeButton = FindViewById<Button>(Resource.Id.subscribeButton);
            subscribeButton.Click += delegate {
                FirebaseMessaging.Instance.SubscribeToTopic("news");
                Log.Debug(TAG, "Subscribed to remote notifications");
            };*/
        }

        

            ArrayAdapter<String> adapter;

        private void showChatList()
        {
            reference.Child("chat").AddChildEventListener(this);

            adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, Android.Resource.Id.Text1);

            chatList.Adapter = adapter;
        }

        


        public bool IsPlayServicesAvailable() // Google 서비스 APK 설치 여부 확인
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (resultCode != ConnectionResult.Success)
            {
                if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
                    Toast.MakeText(this, GoogleApiAvailability.Instance.GetErrorString(resultCode), ToastLength.Long).Show();
                else
                {
                    Toast.MakeText(this, "This device is not supported", ToastLength.Long).Show();
                    Finish();
                }
                return false;
            }
            else
            {
                Toast.MakeText(this, "Google Play Services is available.", ToastLength.Long).Show();
                return true;
            }
        }

        void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            var channel = new NotificationChannel(CHANNEL_ID,
                                                  "FCM Notifications",
                                                  NotificationImportance.Default)
            {

                Description = "Firebase Cloud Messages appear in this channel"
            };

            var notificationManager = (NotificationManager)GetSystemService(Android.Content.Context.NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        public void OnCancelled(DatabaseError error)
        {
            
        }

        public void OnChildAdded(DataSnapshot snapshot, string previousChildName)
        {
            Console.WriteLine("snapshot" + snapshot.Key);
            adapter.Add(snapshot.Key);
        }

        public void OnChildChanged(DataSnapshot snapshot, string previousChildName)
        {
            
        }

        public void OnChildMoved(DataSnapshot snapshot, string previousChildName)
        {
            
        }

        public void OnChildRemoved(DataSnapshot snapshot)
        {
            
        }

        //뒤로가기로 액티비티로 돌아왔을 때
        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == 42 && FM.IceLink.Android.Utility.IsSDKVersionSupported(BuildVersionCodes.Lollipop))
            {
                if (data == null)
                {
                    Alert("Must allow screen sharing before the chat can start.");
                }
                else
                {
                    MediaProjectionManager manager = GetSystemService(MediaProjectionService).JavaCast<MediaProjectionManager>();
                    app.MediaProjection = manager.GetMediaProjection((int)resultCode, data);
                    StartActivity(new Intent(ApplicationContext, typeof(ChatActivity)));
                }
            }
        }

        private void SwitchToVideoChat(String sessionId, String name)
        {
            //세션 id와 이름을 입력해야만 넘어가짐
            if (sessionId.Length == 6)
            {
                if (name.Length > 0)
                {
                    app.SessionId = sessionId;
                    app.Name = name;
                    app.EnableScreenShare = screenShareCheckBox.Checked;

                    if (FM.IceLink.Android.Utility.IsSDKVersionSupported(BuildVersionCodes.Lollipop))
                    {
                        MediaProjectionManager manager = GetSystemService(MediaProjectionService).JavaCast<MediaProjectionManager>();
                        Intent screenCaptureIntent = manager.CreateScreenCaptureIntent();

                        this.StartActivityForResult(screenCaptureIntent, 42);
                    }
                    else
                    {
                        // Show the video chat.var intent = new Intent(this, typeof(ChatActivity));

                        app.ChatName = userChat.Text;
                        /*intent.PutExtra("chatName", userChat.Text);
                        //intent.PutExtra("userName", userEdit.Text);
                        intent.PutExtra("token", token);*/
                        //StartActivity(intent);
                        StartActivity(new Intent(ApplicationContext, typeof(ChatActivity)));
                    }
                }
                else
                {
                    Alert("Must have a name.");
                }
            }
            else
            {
                Alert("Session ID must be 6 digits long.");
            }
        }

        public void Alert(String format, params object[] args)
        {
            string text = string.Format(format, args);
            Activity self = this;
            self.RunOnUiThread(() =>
            {
                if (!IsFinishing)
                {
                    Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(self);
                    alert.SetMessage(text);
                    alert.SetPositiveButton("OK", (sender, arg) => { alert.Show(); });
                }
            });
        }

        /* private void sendNotificationUser(string token)
         {
             Model model = new Model(token, new NotificationModel(add, content));
             notificationApi api = ApiClient.get

         }

         private void sendPushTokenToDB()
         {
             FirebaseInstanceId.Instance.GetInstanceId().AddOnSuccessListener(this, new AddOnSuccessListener) => {
                 string token;

             }
         }*/
    }
}
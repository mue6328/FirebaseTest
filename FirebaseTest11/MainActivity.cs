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
        //ArrayAdapter<string> adapter;

        private FirebaseDatabase database;
        private DatabaseReference reference;

        private App app;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            
            base.OnCreate(savedInstanceState);
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

            app = App.GetInstance(this);

            userChat = FindViewById<EditText>(Resource.Id.user_chat);
            //userEdit = FindViewById<EditText>(Resource.Id.user_edit);
            userNext = FindViewById<Button>(Resource.Id.user_next);
            chatList = FindViewById<ListView>(Resource.Id.chat_list);


            var applicationId = "my-app";
            var userId = "my-name";
            var deviceId = "00000000-0000-0000-0000-000000000000";
            var channelId = "11111111-1111-1111-1111-111111111111";

            var client = new FM.LiveSwitch.Client("http://localhost:8080/sync", applicationId, userId, deviceId, null, new[] { "role1", "role2" });

            string ltoken = FM.LiveSwitch.Token.GenerateClientRegisterToken(
                applicationId,
                client.UserId,
                client.DeviceId,
                client.Id,
                client.Roles,
                new[] { new FM.LiveSwitch.ChannelClaim(channelId) },
                "--replaceThisWithYourOwnSharedSecret--"
            );


            client.Register(token).Then((FM.LiveSwitch.Channel[] channels) =>
            {
                Console.WriteLine("connected to channel: " + channels[0].Id);
            }).Fail((Exception ex) =>
            {
                Console.WriteLine("registration failed");
            });

            Toast.MakeText(this, token, ToastLength.Long).Show();

            //TcpClient tcpClient

            userNext.Click += (sender, args) =>
            {
                if (userChat.ToString().Equals(""))
                    return;

                var intent = new Intent(this, typeof(ChatActivity));

                app.ChatName = userChat.Text;
                intent.PutExtra("chatName", userChat.Text);
                //intent.PutExtra("userName", userEdit.Text);
                intent.PutExtra("token", token);
                StartActivity(intent);
            };

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
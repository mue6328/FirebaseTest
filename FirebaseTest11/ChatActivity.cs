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
using Java.Net;
using Java.IO;
using Android.Util;
using Java.Interop;
using System.IO;

namespace FirebaseTest11
{
    [Activity(Label = "ChatActivity", Theme = "@style/AppTheme")]
    class ChatActivity : AppCompatActivity, IChildEventListener
    {
        private string chatName;
        //private string userName;
        private string mobile_token = "cLOOaZYGTBWSm5RG-zXRDe:APA91bGVMSzARoOnMKir6LTSz26XHTw9g4DwI81vhacpwuN4eq-_NENGXGw1TEGdEwv0TV2H3NSfI8J93otT-VlWkzLIPf1kFJzO4Gv8L7aERyryyCvO-nqyESde0rA_ztA1mRQFOx2-";
        private string pc_token = "cp1FK9KkQhWa6UtnSf9p9l:APA91bGJZMJgg8b9DImjnAIurY_x6eCNucvs3EwkyWge9npB1Ry9N02h9uIa-WsudSwhOajaW9IzqxFjT8Kc8yt-zYICGmE_ofZYfXErOB4nwBmdtaUtEkxwI9ELZ-d9H2f6pt882eug";

        private ListView chatView;
        private EditText chatEdit;
        private Button chatSend;

        private int randomUserName;
        

        private FirebaseDatabase database;
        private DatabaseReference reference;

        private notificationApi notiApi;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_chat);

            notiApi = RestService.For<notificationApi>(Utils.FCM_URL);

            FirebaseApp.InitializeApp(this);

            database = FirebaseDatabase.Instance;
            reference = database.Reference;

            chatView = FindViewById<ListView>(Resource.Id.chat_view);
            chatEdit = FindViewById<EditText>(Resource.Id.chat_edit);
            chatSend = FindViewById<Button>(Resource.Id.chat_send);

            System.Random random = new System.Random();
            randomUserName = random.Next(1, 10000);

            var intent = Intent;
            if (intent == null)
                Toast.MakeText(this, "null", ToastLength.Long).Show();
            chatName = intent.GetStringExtra("chatName");
            //userName = intent.GetStringExtra("userName");

           

            Toast.MakeText(this, chatName + " " +  " ", ToastLength.Long).Show();
            openChat(chatName);

            chatSend.Click += (sender, args) =>
            {
                if (chatEdit.Text.Equals(""))
                    return;

                var chat = new ChatDTO(chatEdit.Text, "user" + randomUserName);

                /*TCPclient tcpThread = new TCPclient(chatEdit.Text);
                Thread thread = new Thread(tcpThread);
                thread.Start();*/

                reference.Child("chat").Child(chatName).Push().SetValue(ChatDTO.MsgModelToMap(chat));
                sendNotification();
                chatEdit.Text = "";
            };
            
        }

        /*public static ChatDTO Cast(Java.Lang.Object obj) where ChatDTO : class
        {
            var propertyInfo = obj.GetType().GetProperty("Instance");
            return propertyInfo == null ? null : propertyInfo.GetValue(obj, null) as ChatDTO;
        }*/

        private void sendNotification()
        {
            var intent = Intent;
            //token = intent.GetStringExtra("token");
            
            if (intent.GetStringExtra("token").Equals(pc_token))
                notiApi.sendNotification(new Model(mobile_token, new NotificationModel("ddddd", "ddd"), new ChatDTO(chatEdit.Text, "user" + randomUserName)));
            else
                notiApi.sendNotification(new Model(pc_token, new NotificationModel("ddddd", "ddd"), new ChatDTO(chatEdit.Text, "user" + randomUserName)));
            //Toast.MakeText(this, notiApi.sendNotification(new Model(token, new ChatDTO(chatEdit.Text, userName))).Result.ToString(), ToastLength.Long).Show();
        }

        ArrayAdapter<string> adapter;
        private void openChat(string chatName)
        {
            reference.Child("chat").Child(chatName).AddChildEventListener(this);

            adapter = new ArrayAdapter<string>(Application.Context,
                Android.Resource.Layout.SimpleListItem1,
                Android.Resource.Id.Text1);

            chatView.Adapter = adapter;
            chatView.TranscriptMode = TranscriptMode.AlwaysScroll;

            //Toast.MakeText(this, chatName, ToastLength.Long).Show();

            
        }

        public void OnCancelled(DatabaseError error)
        {
            throw new System.NotImplementedException();
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
            throw new System.NotImplementedException();
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
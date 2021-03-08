using Android.App;
using Android.Content;
using Firebase.Messaging;
using System.Collections.Generic;
using Android.Util;

namespace FirebaseTest11
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    class MyFirebaseMessagingService : FirebaseMessagingService
    {
        const string TAG = "MyFirebaseMsgService";
        public override void OnMessageReceived(RemoteMessage message) // Foreground에서 Push 알림을 받았을 때 실행
        {
            Log.Debug(TAG, "From: " + message.From);

            var body = message.GetNotification().Body;
            var title = message.GetNotification().Title;
            SendNotification(message.Data, body, title);
        }

        void SendNotification(IDictionary<string, string> data, string body, string title)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            foreach (var key in data.Keys)
            {
                intent.PutExtra(key, data[key]);
            }

            var pendingIntent = PendingIntent.GetActivity(this,
                                                          MainActivity.NOTIFICATION_ID,
                                                          intent,
                                                          PendingIntentFlags.OneShot);

            var notificationBuilder = new Notification.Builder(this, MainActivity.CHANNEL_ID) 
                                      .SetSmallIcon(Resource.Mipmap.ic_launcher_foreground) 
                                      .SetContentTitle(title)
                                      .SetContentText(body)
                                      .SetAutoCancel(true)
                                      .SetContentIntent(pendingIntent);

            NotificationManager notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;



            notificationManager.Notify(MainActivity.NOTIFICATION_ID, notificationBuilder.Build());
        }

        /*public override void OnMessageReceived(RemoteMessage remoteMessage)
        {
            if (remoteMessage.GetNotification() != null)
            {
                Console.WriteLine("알림 메시지 : " + remoteMessage.GetNotification().Body);
                string messageBody = remoteMessage.GetNotification().Body;
                string messageTitle = remoteMessage.GetNotification().Title;
                Intent intent = new Intent(this, Class);
                intent.AddFlags(ActivityFlags.ClearTop);
                PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);
                string channelId = "Channel ID";
                // Uri defaultSoundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);
                Notification.Builder builder =
                    new Notification.Builder(this, channelId)
                        .SetSmallIcon(Resource.Mipmap.ic_launcher)
                        .SetContentTitle(messageTitle)
                        .SetContentText(messageBody)
                        .SetAutoCancel(true)
                        ///   .SetSound()
                        .SetContentIntent(pendingIntent);
                NotificationManager notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;

                //       if(Build.VERSION.SdkInt >= Build.VERSION_CODES.O) 
                notificationManager.Notify(0, builder.Build());
            }
        }*/

    }
}
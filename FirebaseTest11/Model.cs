using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FirebaseTest11
{
    class Model
    {
        [JsonProperty("to")]
        private string to;
        [JsonProperty("notification")]
        private NotificationModel notification;
        [JsonProperty("data")]
        private ChatDTO data;

        public Model(string to, NotificationModel notification, ChatDTO data)
        {
            this.to = to;
            this.notification = notification;
            this.data = data;
        }

        public string getTo()
        {
            return to;
        }

        public void setTo(string to)
        {
            this.to = to;
        }

        public NotificationModel getNotification()
        {
            return notification;
        }

        public void setNotification(NotificationModel notification)
        {
            this.notification = notification;
        }

        public ChatDTO getData()
        {
            return data;
        }

        public void setData(ChatDTO data)
        {
            this.data = data;
        }
    }
}
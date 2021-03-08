using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FirebaseTest11
{
    class NotificationModel
    {
        private string title;
        private string body;

        public NotificationModel(string title, string body)
        {
            this.title = title;
            this.body = body;
        }

        public string getTitle()
        {
            return title;
        }

        public void setTitle(string title)
        {
            this.title = title;
        }
        public string getbody()
        {
            return body;
        }

        public void setbody(string body)
        {
            this.body = body;
        }
    }
}
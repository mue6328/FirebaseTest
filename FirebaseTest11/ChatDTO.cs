using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FirebaseTest11
{
    class ChatDTO  {
        private string message;
        private string userName;

        public ChatDTO(string message, string userName)
        {
            this.message = message;
            this.userName = userName;
        }

        public static HashMap MsgModelToMap(ChatDTO chat)
        {
            HashMap map = new HashMap();

            map.Put("username", chat.userName);
            map.Put("message", chat.message);

            return map;
        }

        public string getMessage()
        {
            return message;
        }

        public void setMessage(string message)
        {
            this.message = message;
        }

        public string getUserName()
        {
            return userName;
        }

        public void setUserName(string userName)
        {
            this.userName = userName;
        }
    }
}
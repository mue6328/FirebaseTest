using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseTest11
{
    
    interface notificationApi
    {
        [Headers("Authorization: key=" + Utils.token, "Content-Type : application/json")]
        [Post("/fcm/send")]
        Task<IApiResponse> sendNotification(
            [Body] Model notification);
    }
}
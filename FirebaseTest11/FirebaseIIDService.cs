using Android.App;
using Android.Content;
using Android.Util;
using Firebase.Iid;

namespace FirebaseTest11
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    class FirebaseIIDService : FirebaseInstanceIdService
    {
        // Registration ID가 갱신될 때만 호출됨 (한번 호출되면 갱신되기 전까지 호출되지 않음)
        public override void OnTokenRefresh()
        {
            string refreshedToken = FirebaseInstanceId.Instance.Token; // 토큰 발급
            Log.Debug("FirebaseIIDService ", "Refreshed token: " + refreshedToken);

            SendRegistrationIdToServer(refreshedToken);
        }

        private void SendRegistrationIdToServer(string refreshedToken)
        {
            // 토큰을 앱 서버로 전송
        }
    }
}
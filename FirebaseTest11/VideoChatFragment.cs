using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;
using Fragment = Android.Support.V4.App.Fragment;

namespace FirebaseTest11
{
    public class VideoChatFragment : Fragment, View.IOnTouchListener
    {
        private App app;
        public static RelativeLayout Container;
        private FrameLayout layout;

        private GestureDetector gestureDetector;

        private View openContextMenuView;

        private IOnVideoReadyListener listener;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // RetainInstance = true : 프래그먼트가 재생성되도 Destroy(파괴)되지 않도록 설정함
            // 이 값을 설정하지 않으면 Default값으로 false가 들어감
            RetainInstance = true;

            app = App.GetInstance(null);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            return inflater.Inflate(Resource.Layout.VideoChatFragment, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            try
            {

                // For demonstration purposes, use the double-tap gesture
                // to switch between the front and rear camera.
                gestureDetector = new GestureDetector(this.Activity, new GestureDetector.SimpleOnGestureListener());
                gestureDetector.DoubleTap += GestureDetector_DoubleTap;

                layout = (FrameLayout)view.FindViewById(Resource.Id.layout);
                View.SetOnTouchListener(this);

                // Preserve a static container across
                // activity destruction/recreation.
                RelativeLayout c = (RelativeLayout)view.FindViewById(Resource.Id.container);
                if (Container == null)
                {
                    Container = c;

                    Toast.MakeText(Activity, "Double-tap to switch camera.", ToastLength.Short).Show();
                }
                layout.RemoveView(c);

                listener.onVideoReady();
            }
            catch (Exception ex)
            {
                FM.IceLink.Log.Error("VideoChatFragment", ex);
            }
        }

        // 화면을 더블클릭 할시 셀카 / 정면 카메라 변경
        private void GestureDetector_DoubleTap(object sender, GestureDetector.DoubleTapEventArgs e)
        {
            if (app.EnableScreenShare == false)
            {
                app.UseNextVideoDevice();
            }
        }

        // 캠 화면을 눌렀을 때 아이템 선택
        public override bool OnContextItemSelected(IMenuItem item)
        {
            int id = item.ItemId;

            if (id == Resource.Id.mute_audio)
            {
                app.AudioMuted(openContextMenuView, !item.IsChecked);
            }
            else if (id == Resource.Id.mute_video)
            {
                app.VideoMuted(openContextMenuView, !item.IsChecked);
            }
            else if (id == Resource.Id.record_video)
            {
                app.IsRecordingVideo(openContextMenuView, !item.IsChecked);
            }
            else if (id == Resource.Id.record_audio)
            {
                app.IsRecordingAudio(openContextMenuView, !item.IsChecked);
            }

            return true;
        }

        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            base.OnCreateContextMenu(menu, v, menuInfo);
            MenuInflater inflater = new MenuInflater(this.Context);
            inflater.Inflate(Resource.Menu.menu, menu);
            openContextMenuView = v;

            // 
            menu.GetItem(0).SetChecked(app.VideoMuted(v));
            menu.GetItem(1).SetChecked(app.AudioMuted(v));
            menu.GetItem(2).SetChecked(app.IsRecordingVideo(v));
            menu.GetItem(3).SetChecked(app.IsRecordingAudio(v));
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            // Handle the double-tap event.
            if (gestureDetector == null || !gestureDetector.OnTouchEvent(e))
            {
                return View.OnTouchEvent(e);
            }
            return false;
        }

        // Pause 상태(액티비티가 보이지 않게 되었을 때) 캠 화면을 멈춤
        public override void OnPause()
        {
            // Android requires us to pause the local
            // video feed when pausing the activity.
            // Not doing this can cause unexpected side
            // effects and crashes.
            app.PauseLocalVideo().WaitForResult();

            // Remove the static container from the current layout.
            if (Container != null)
            {
                layout.RemoveView(Container);
            }

            base.OnPause();
        }

        // Resume 상태(액티비티가 보일 때) 캠 화면을 실행
        public override void OnResume()
        {
            base.OnResume();

            // Add the static container to the current layout.
            if (Container != null)
            {
                layout.AddView(Container);
            }

            // Resume the local video feed.
            app.ResumeLocalVideo().WaitForResult();
        }

        // OnAttach (Fragment가 생성되며 첫 번째로 호출됨)
        public override void OnAttach(Activity activity)
        {
            base.OnAttach(activity);
            if (activity is IOnVideoReadyListener)
            {
                listener = (IOnVideoReadyListener)activity;
            }
            else
            {
                throw new Exception(activity.ToString() + " must implement VideoChatFragment.OnVideoReadyListener");
            }
        }

        public interface IOnVideoReadyListener
        {
            void onVideoReady();
        }
    }
}
using Android.Content;
using Android.Views;
using FM.IceLink;
using FM.IceLink.Android;

namespace FirebaseTest11
{
    public class CameraLocalMedia : LocalMedia<View>
    {
        private CameraPreview viewSink;
        private VideoConfig videoConfig = new VideoConfig(640, 480, 30);

        protected override ViewSink<View> CreateViewSink()
        {
            return null;
        }

        protected override VideoSource CreateVideoSource()
        {
            return new CameraSource(viewSink, videoConfig);
        }

        public CameraLocalMedia(Context context, bool enableSoftwareH264, bool disableAudio, bool disableVideo, AecContext aecContext)
            : base(context, enableSoftwareH264, disableAudio, disableVideo, aecContext)
        {
            this.context = context;

            viewSink = new CameraPreview(context, LayoutScale.Contain);

            base.Initialize();
        }

        public new View View
        {
            get { return viewSink.View; }
        }
    }
}
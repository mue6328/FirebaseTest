using Android.Content;
using Android.Media.Projection;
using Android.Widget;
using FM.IceLink;
using FM.IceLink.Android;

namespace FirebaseTest11
{
    public class ScreenShareLocalMedia : LocalMedia<FrameLayout>
    {
        private MediaProjectionSource projectionSource;

        protected override ViewSink<FrameLayout> CreateViewSink()
        {
            return new OpenGLSink(context);
        }

        protected override VideoSource CreateVideoSource()
        {
            return projectionSource;
        }

        public ScreenShareLocalMedia(MediaProjection projection, Context context, bool enableSoftwareH264, bool disableAudio, bool disableVideo, AecContext aecContext)
                : base(context, enableSoftwareH264, disableAudio, disableVideo, aecContext)
        {
            this.context = context;
            projectionSource = new MediaProjectionSource(projection, context, 3);

            base.Initialize();
        }
    }
}
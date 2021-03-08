using Android.Content;
using Android.Widget;
using FM.IceLink;
using FM.IceLink.Android;
using System;
using System.IO;

namespace FirebaseTest11
{
    public class RemoteMedia : RtcRemoteMedia<FrameLayout>
    {
        private bool enableSoftwareH264;
        private Context context;

        protected override ViewSink<FrameLayout> CreateViewSink()
        {
            return new OpenGLSink(context);
        }

        protected override AudioSink CreateAudioRecorder(AudioFormat audioFormat)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string filename = Path.Combine(path, Id + "-remote-audio-" + audioFormat.Name.ToLower() + ".mkv");
            return new FM.IceLink.Matroska.AudioSink(filename);
        }

        protected override VideoSink CreateVideoRecorder(VideoFormat videoFormat)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string filename = Path.Combine(path, Id + "-remote-video-" + videoFormat.Name.ToLower() + ".mkv");
            return new FM.IceLink.Matroska.VideoSink(filename);
        }

        protected override VideoPipe CreateImageConverter(VideoFormat videoFormat)
        {
            return new FM.IceLink.Yuv.ImageConverter(videoFormat);
        }

        protected override AudioDecoder CreateOpusDecoder(AudioConfig audioConfig)
        {
            return new FM.IceLink.Opus.Decoder(audioConfig);
        }

        protected override AudioSink CreateAudioSink(AudioConfig audioConfig)
        {
            return new FM.IceLink.Android.AudioTrackSink(audioConfig);
        }

        protected override VideoDecoder CreateH264Decoder()
        {
            if (enableSoftwareH264)
            {
                return new FM.IceLink.OpenH264.Decoder();
            }
            else
            {
                return null;
            }
        }

        protected override VideoDecoder CreateVp8Decoder()
        {
            return new FM.IceLink.Vp8.Decoder();
        }

        protected override VideoDecoder CreateVp9Decoder()
        {
            return new FM.IceLink.Vp9.Decoder();
        }

        public RemoteMedia(Context context, bool enableSoftwareH264, bool disableAudio, bool disableVideo, AecContext aecContext)
                : base(disableAudio, disableVideo, aecContext)
        {
            this.context = context;
            this.enableSoftwareH264 = enableSoftwareH264;

            base.Initialize();
        }
    }
}
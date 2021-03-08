using Android.Content;
using FM.IceLink;
using FM.IceLink.Android;
using System;
using System.IO;

namespace FirebaseTest11
{
    public abstract class LocalMedia<TView> : RtcLocalMedia<TView>
    {
        private bool enableSoftwareH264;
        protected Context context;

        protected override AudioSink CreateAudioRecorder(AudioFormat audioFormat)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string filename = Path.Combine(path, Id + "-local-audio-" + audioFormat.Name.ToLower() + ".mkv");
            return new FM.IceLink.Matroska.AudioSink(filename);
        }

        protected override VideoSink CreateVideoRecorder(VideoFormat videoFormat)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string filename = Path.Combine(path, Id + "-local-video-" + videoFormat.Name.ToLower() + ".mkv");
            return new FM.IceLink.Matroska.VideoSink(filename);
        }

        protected override VideoPipe CreateImageConverter(VideoFormat videoFormat)
        {
            return new FM.IceLink.Yuv.ImageConverter(videoFormat);
        }

        protected override AudioSource CreateAudioSource(AudioConfig audioConfig)
        {
            return new AudioRecordSource(context, audioConfig);
        }

        protected override AudioEncoder CreateOpusEncoder(AudioConfig audioConfig)
        {
            return new FM.IceLink.Opus.Encoder(audioConfig);
        }

        protected override VideoEncoder CreateH264Encoder()
        {
            if (enableSoftwareH264)
            {
                return new FM.IceLink.OpenH264.Encoder();
            }
            else
            {
                return null;
            }
        }

        protected override VideoEncoder CreateVp8Encoder()
        {
            return new FM.IceLink.Vp8.Encoder();
        }

        protected override VideoEncoder CreateVp9Encoder()
        {
            return new FM.IceLink.Vp9.Encoder();
        }

        public LocalMedia(Context context, bool enableSoftwareH264, bool disableAudio, bool disableVideo, AecContext aecContext)
                : base(disableAudio, disableVideo, aecContext)
        {
            this.enableSoftwareH264 = enableSoftwareH264;
            this.context = context;
        }
    }
}
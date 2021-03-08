using Android.Content;
using Android.Media.Projection;
using Android.Widget;
using FM.IceLink;
using FM.IceLink.Android;

namespace FirebaseTest11
{
    public class AecContext : FM.IceLink.AecContext
    {
        protected override AecPipe CreateProcessor()
        {
            AudioConfig config = new AudioConfig(48000, 2);
            return new FM.IceLink.AudioProcessing.AecProcessor(config, AudioTrackSink.getBufferDelay(config) + AudioRecordSource.getBufferDelay(config));
        }

        protected override AudioSink CreateOutputMixerSink(AudioConfig config)
        {
            return new AudioTrackSink(config);
        }
    }
}

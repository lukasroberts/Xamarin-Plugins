using AVFoundation;
using Foundation;
using System;
using System.IO;

namespace Plugin.SimpleAudioPlayer
{
    /// <summary>
    /// Implementation for SimpleAudioPlayer
    /// </summary>
    public class AVAudioEngineSimplePlayerImplementation : ISimpleAudioPlayer
    {
        ///<Summary>
        /// Raised when playback completes or loops
        ///</Summary>
        public event EventHandler PlaybackEnded;

        AVAudioEngine engine;
        AVAudioUnitTimePitch pitch;
        AVAudioPlayerNode player;
        AVAudioFile audioFile;
        private float _bpm;
        private float _adjustedBpm;
        private bool _hasPlayedFirst = false;

        ///<Summary>
        /// Length of audio in seconds
        ///</Summary>
        public double Duration
        { get { return 0; } }

        ///<Summary>
        /// Current position of audio in seconds
        ///</Summary>
        public double CurrentPosition
        { get { return 0; } }

        ///<Summary>
        /// Playback volume (0 to 1)
        ///</Summary>
        public double Volume
        {
            get { return player == null ? 0 : player.Volume; }
            set { SetVolume(value, Balance); }
        }

        ///<Summary>
        /// Balance left/right: -1 is 100% left : 0% right, 1 is 100% right : 0% left, 0 is equal volume left/right
        ///</Summary>
        public double Balance
        {
            get { return _balance; }
            set { SetVolume(Volume, _balance = value); }
        }
        double _balance = 0;

        ///<Summary>
        /// Indicates if the currently loaded audio file is playing
        ///</Summary>
        public bool IsPlaying
        { get { return player == null ? false : player.Playing; } }

        ///<Summary>
        /// Continously repeats the currently playing sound
        ///</Summary>
        public bool Loop
        {
            get { return true; }
            set
            {
                _loop = value;
                if (player != null)
                {
                }
            }
        }
        bool _loop;

        ///<Summary>
        /// Indicates if the position of the loaded audio file can be updated - always returns true on iOS
        ///</Summary>
        public bool CanSeek
        { get { return player == null ? false : true; } }

        ///<Summary>
        /// Load wave or mp3 audio file as a stream
        ///</Summary>
        public bool Load(Stream audioStream)
        {
            DeletePlayer();

            var data = NSData.FromStream(audioStream);

            return true;
        }

        ///<Summary>
        /// Load wave or mp3 audio file from the Android assets folder
        ///</Summary>
        public bool Load(string fileName, float bpm)
        {
            _bpm = bpm;
            DeletePlayer();

            NSError error = new NSError();

            try
            {
                if (!String.IsNullOrWhiteSpace(fileName))
                {
                    string directory = Path.GetDirectoryName(fileName);
                    string filename = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName).Substring(1);
                    NSUrl url = NSBundle.MainBundle.GetUrlForResource(filename, extension, directory);
                    audioFile = new AVAudioFile(url, out error);
                }

            }
            catch (Exception e)
            {
                return false;
            }

            if (audioFile != null)
            {
                engine = new AVAudioEngine();
                player = new AVAudioPlayerNode();
                pitch = new AVAudioUnitTimePitch();

                engine.AttachNode(player);
                engine.AttachNode(pitch);

                engine.Connect(player, pitch, audioFile.ProcessingFormat);
                engine.Connect(pitch, engine.MainMixerNode, audioFile.ProcessingFormat);

                engine.Prepare();
                NSError startError = new NSError();
                engine.StartAndReturnError(out startError);
            }

            return true;
        }

        void DeletePlayer()
        {
            Stop();

            if (player != null && player.Playing)
            {
                player.Stop();
            }

            if (engine != null && engine.Running)
            {
                engine.Stop();
            }

            if (player != null && engine != null && pitch != null)
            {
                engine.Dispose();
                player.Dispose();
                pitch.Dispose();
                engine = null;
                player = null;
                pitch = null;
            }
        }

        private void OnPlaybackEnded(object sender, AVStatusEventArgs e)
        {
            PlaybackEnded?.Invoke(sender, e);
        }

        ///<Summary>
        /// Begin playback or resume if paused
        ///</Summary>
        public void Play()
        {
            if (player == null)
                return;

            if (!_hasPlayedFirst)
            {
                NSError er;
                var audioFileBuffer = new AVAudioPcmBuffer(audioFile.ProcessingFormat, (uint)audioFile.Length);
                audioFile.ReadIntoBuffer(audioFileBuffer, out er);
                player.ScheduleBuffer(audioFileBuffer, null, AVAudioPlayerNodeBufferOptions.Loops, null);
                _hasPlayedFirst = true;
            }

            player.PlayAtTime(new AVAudioTime(0));
        }

        ///<Summary>
        /// Pause playback if playing (does not resume)
        ///</Summary>
        public void Pause()
        {
            player?.Pause();
        }

        ///<Summary>
        /// Stop playack and set the current position to the beginning
        ///</Summary>
        public void Stop()
        {
            player?.Pause();
            Seek(0);
        }

        ///<Summary>
        /// Seek a position in seconds in the currently loaded sound file 
        ///</Summary>
        public void Seek(double position)
        {
            if (player == null)
                return;
            //player.CurrentTime = position;
        }

        void SetVolume(double volume, double balance)
        {
            if (player == null)
                return;

            volume = Math.Max(0, volume);
            volume = Math.Min(1, volume);

            balance = Math.Max(-1, balance);
            balance = Math.Min(1, balance);

            player.Volume = (float)volume;
            player.Pan = (float)balance;
        }

        void OnPlaybackEnded()
        {
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        }

        bool isDisposed = false;
        ///<Summary>
        /// Dispose SimpleAudioPlayer and release resources
        ///</Summary>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            if (disposing)
                DeletePlayer();

            isDisposed = true;
        }

        ~AVAudioEngineSimplePlayerImplementation()
        {
            Dispose(false);
        }

        ///<Summary>
        /// Dispose SimpleAudioPlayer and release resources
        ///</Summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public void ChangePitch(float amountToChange)
        {
            if (player != null)
            {
                _adjustedBpm = amountToChange;
                pitch.Rate = _adjustedBpm / _bpm;
                //pitch.Pitch = Remap(amountToChange, 55, 220, -2400, 2400);
            }
        }

        public float Remap(float from, float fromMin, float fromMax, float toMin, float toMax)
        {
            var fromAbs = from - fromMin;
            var fromMaxAbs = fromMax - fromMin;

            var normal = fromAbs / fromMaxAbs;

            var toMaxAbs = toMax - toMin;
            var toAbs = toMaxAbs * normal;

            var to = toAbs + toMin;

            return to;
        }

        bool ISimpleAudioPlayer.IsPlaying()
        {
            return player?.Playing ?? false;
        }
    }
}
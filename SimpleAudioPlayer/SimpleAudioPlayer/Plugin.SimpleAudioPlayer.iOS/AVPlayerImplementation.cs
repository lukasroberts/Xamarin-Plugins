using AVFoundation;
using Foundation;
using System;
using System.IO;

namespace Plugin.SimpleAudioPlayer
{
    /// <summary>
    /// Implementation for SimpleAudioPlayer
    /// </summary>
    public class AVPlayerImplementation : ISimpleAudioPlayer
    {
        ///<Summary>
        /// Raised when playback completes or loops
        ///</Summary>
        public event EventHandler PlaybackEnded;

        AVPlayer avplayer;
        AVAsset avasset;
        AVPlayerItem avplayerItem;
        AVPlayerLooper looper;
       
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
            get { return avplayer == null ? 0 : avplayer.Volume; }
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
        { get { return avplayerItem == null ? false : true; } }

        ///<Summary>
        /// Continously repeats the currently playing sound
        ///</Summary>
        public bool Loop
        {
            get { return true; }
            set
            {
                _loop = value;
                if (looper != null)
                {
                    
                }
            }
        }
        bool _loop;

        ///<Summary>
        /// Indicates if the position of the loaded audio file can be updated - always returns true on iOS
        ///</Summary>
        public bool CanSeek
        { get { return avplayer == null ? false : true; } }

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
        public bool Load(string fileName)
        {
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
                    avasset = AVAsset.FromUrl(url);
                    avplayerItem = new AVPlayerItem(avasset);
                    avplayerItem.AudioTimePitchAlgorithm = AVAudioTimePitchAlgorithm.Varispeed;

                    avplayer = new AVPlayer(avplayerItem);
                    NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, (obj) =>
                    { avplayer.Seek(CoreMedia.CMTime.Zero); avplayer.PlayImmediatelyAtRate(_rate); obj.Dispose(); }, avplayer.CurrentItem);
                }
                    
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        void DeletePlayer()
        {
            Stop();

            if (avplayer != null && avplayer.TimeControlStatus == AVPlayerTimeControlStatus.Playing)
            {
                avplayer.Pause();
            }

            if (avplayer != null)
            {
                avplayer.Dispose();
                avplayer = null;
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
            if (avplayer == null)
                return;

            avplayer.Play();
        }

        ///<Summary>
        /// Pause playback if playing (does not resume)
        ///</Summary>
        public void Pause()
        {
            avplayer?.Pause();
        }

        ///<Summary>
        /// Stop playack and set the current position to the beginning
        ///</Summary>
        public void Stop()
        {
            avplayer?.Pause();
            Seek(0);
        }

        ///<Summary>
        /// Seek a position in seconds in the currently loaded sound file 
        ///</Summary>
        public void Seek(double position)
        {
            if (avplayer == null)
                return;
            //player.CurrentTime = position;
        }

        void SetVolume(double volume, double balance)
        {
            if (avplayer == null)
                return;

            volume = Math.Max(0, volume);
            volume = Math.Min(1, volume);

            balance = Math.Max(-1, balance);
            balance = Math.Min(1, balance);

            avplayer.Volume = (float)volume;
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

        ~AVPlayerImplementation()
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
            _rate = amountToChange;
            avplayer.PlayImmediatelyAtRate(amountToChange);
            //avplayer.Rate = amountToChange;
        }
        float _rate = 1.0f;

        bool ISimpleAudioPlayer.IsPlaying()
        {
            return avplayer.TimeControlStatus == AVPlayerTimeControlStatus.Playing;
        }
    }
}
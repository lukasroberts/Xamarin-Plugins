using Android.Content.Res;
using System;
using System.Collections.Generic;
using System.IO;
using Uri = Android.Net.Uri;

namespace Plugin.SimpleAudioPlayer
{
    /// <summary>
    /// Implementation for Feature
    /// </summary>
    [Android.Runtime.Preserve(AllMembers = true)]
    public class SimpleAudioPlayerImplementation : ISimpleAudioPlayer
    {
        ///<Summary>
        /// Raised when audio playback completes successfully 
        ///</Summary>
        public event EventHandler PlaybackEnded;

        Android.Media.SoundPool pool;

        static int index = 0;

        ///<Summary>
        /// Playback volume (0 to 1)
        ///</Summary>
        public double Volume
        {
            get { return _volume; }
            set { SetVolume(_volume = value, Balance); }
        }
        double _volume = 0.5;

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
        /// Sets the maximum of number of simultaneous streams that can be played simultaneously.
        ///</Summary>
        public int MaxStreams
        {
            get { return _maxStreams; }
            set { _maxStreams = value; }
        }
        int _maxStreams;

        float _pitchSpeed;

        string path;

        private List<int> soundIds;
        private List<int> sampleIds;
        private int currentlyPlayingSampleId;

        /// <summary>
        /// Instantiates a new SimpleAudioPlayer
        /// </summary>
        public SimpleAudioPlayerImplementation()
        {
            soundIds = new List<int>();
            sampleIds = new List<int>();
            _pitchSpeed = 1;

            var audioAttributes = new Android.Media.AudioAttributes.Builder()
                .SetContentType(Android.Media.AudioContentType.Sonification)
                .SetUsage(Android.Media.AudioUsageKind.AssistanceSonification)
                .SetLegacyStreamType(Android.Media.Stream.Music)
                .Build();
            pool = new Android.Media.SoundPool.Builder()
                .SetMaxStreams(MaxStreams)
                .SetAudioAttributes(audioAttributes)
                .Build();

            pool.LoadComplete += Pool_LoadComplete;

            SetVolume(_volume, _balance);
        }

        private void Pool_LoadComplete(object sender, Android.Media.SoundPool.LoadCompleteEventArgs e)
        {
            if(e.Status == 0)
            {
                sampleIds.Add(e.SampleId);
            }
        }

        ///<Summary>
        /// Load wav or mp3 audio file as a stream
        ///</Summary>
        public bool Load(Stream audioStream)
        {
            DeleteFile(path);

            //cache to the file system
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), $"cache{index++}.wav");
            var fileStream = File.Create(path);
            audioStream.CopyTo(fileStream);
            fileStream.Close();

            try
            {
                soundIds.Add(pool.Load(path, 1));
            }
            catch
            {
                try
                {
                    var context = Android.App.Application.Context;
                    //streamIds.Add(pool.Load(context, Uri.Parse(Uri.Encode(path))));
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        ///<Summary>
        /// Load wav or mp3 audio file from the Android Resources folder
        ///</Summary>
        public bool Load(string fileName)
        {
            AssetFileDescriptor afd = Android.App.Application.Context.Assets.OpenFd(fileName);

            soundIds.Add(pool.Load(afd, 1));

            return true;
        }

        void DeletePlayer()
        {
            Stop();

            if (pool != null)
            {
                pool.LoadComplete -= Pool_LoadComplete;
                pool.Release();
                pool.Dispose();
                pool = null;                
            }

            DeleteFile(path);
            path = string.Empty;
        }

        void DeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) == false)
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                }
            }
        }

        ///<Summary>
        /// Begin playback or resume if paused
        ///</Summary>
        public void Play()
        {
        }

        ///<Summary>
        /// Begin playback or resume if paused
        ///</Summary>
        public void Play(int soundId)
        {
            if (pool == null)
                return;

            currentlyPlayingSampleId = soundId;

            var volume = GetVolume();
            pool.Play(soundId, volume.Item1, volume.Item2, 1, -1, _pitchSpeed);
        }

        ///<Summary>
        /// Stop playack and set the current position to the beginning
        ///</Summary>
        public void Stop()
        {
            pool?.Stop(currentlyPlayingSampleId);
            PlaybackEnded(this, null);
        }

        ///<Summary>
        /// Pause playback if playing (does not resume)
        ///</Summary>
        public void Pause()
        {
            pool?.Pause(currentlyPlayingSampleId);
        }

        (float, float) GetVolume()
        {
            Volume = Math.Max(0, Volume);
            Volume = Math.Min(1, Volume);

            Balance = Math.Max(-1, Balance);
            Balance = Math.Min(1, Balance);

            // Using the "constant power pan rule." See: http://www.rs-met.com/documents/tutorials/PanRules.pdf
            var left = Math.Cos((Math.PI * (Balance + 1)) / 4) * Volume;
            var right = Math.Sin((Math.PI * (Balance + 1)) / 4) * Volume;

            return ((float)left, (float)right);
        }

        ///<Summary>
        /// Sets the playback volume as a double between 0 and 1
        /// Sets both left and right channels
        ///</Summary>
        void SetVolume(double volume, double balance)
        {
            volume = Math.Max(0, volume);
            volume = Math.Min(1, volume);

            balance = Math.Max(-1, balance);
            balance = Math.Min(1, balance);

            // Using the "constant power pan rule." See: http://www.rs-met.com/documents/tutorials/PanRules.pdf
            var left = Math.Cos((Math.PI * (balance + 1)) / 4) * volume;
            var right = Math.Sin((Math.PI * (balance + 1)) / 4) * volume;

            pool?.SetVolume(0, (float)left, (float)right);
        }

        bool isDisposed = false;

        ///<Summary>
		/// Dispose SimpleAudioPlayer and release resources
		///</Summary>
       	protected virtual void Dispose(bool disposing)
        {
            if (isDisposed || pool == null)
                return;

            if (disposing)
                DeletePlayer();

            isDisposed = true;
        }

        ~SimpleAudioPlayerImplementation()
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
            _pitchSpeed = amountToChange;

            amountToChange = Math.Max(amountToChange, 0.5f);
            amountToChange = Math.Min(amountToChange, 2.0f);

            pool?.SetRate(currentlyPlayingSampleId, amountToChange);
        }
    }
}

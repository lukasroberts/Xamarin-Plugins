using System;
using System.IO;

namespace Plugin.SimpleAudioPlayer
{
  /// <summary>
  /// Interface for SimpleAudioPlayer
  /// </summary>
  public interface ISimpleAudioPlayer : IDisposable
  {
        ///<Summary>
        /// Raised when audio playback completes successfully 
        ///</Summary>
        event EventHandler PlaybackEnded;

        ///<Summary>
        /// Playback volume 0 to 1 where 0 is no-sound and 1 is full volume
        ///</Summary>
        double Volume { get; set; }

        ///<Summary>
        /// Balance left/right: -1 is 100% left : 0% right, 1 is 100% right : 0% left, 0 is equal volume left/right
        ///</Summary>
        double Balance { get; set; }

        ///<Summary>
        /// Load wav or mp3 audio file as a stream
        ///</Summary>
        bool Load(Stream audioStream);

        ///<Summary>
        /// Load wav or mp3 audio file from local path
        ///</Summary>
        bool Load(string fileName);

        ///<Summary>
        /// Begin playback or resume if paused
        ///</Summary>
        void Play();

        ///<Summary>
        /// Pause playback if playing (does not resume)
        ///</Summary>
        void Pause();

        ///<Summary>
        /// Stop playack and set the current position to the beginning
        ///</Summary>
        void Stop();

        /// <summary>
        /// Increases of decreases the pitch by the specified amount in real time
        /// </summary>
        /// <param name="amountToChange">The amount to change the pitch rate, 0.5 to 2.0 on Android</param>
        void ChangePitch(float amountToChange);

        /// <summary>
        /// Determines whether the audio player is currently streaming sound
        /// </summary>
        /// <returns></returns>
        bool IsPlaying();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    // Try this instead:
    //http://msdn.microsoft.com/en-us/magazine/ee309883.aspx
    //http://naudio.codeplex.com/


    /// <summary>
    /// This is a singleton that will play multiple sounds simultaneously
    /// </summary>
    /// <remarks>
    /// You could do that just by having multiple instances of MediaPlayer yourself, but if 10 different consumers want
    /// to play the same sound (and it's a monotenous sound, like a thruster or buzz), then you'd only need one instance
    /// playing that sound, until all 10 have said to stop
	/// 
	/// NOTE: This class is very much a work in progress.
	/// 
	/// I tried to use this to make a thruster sound, and used a program to combine/distort various generated noises (white,
	/// pink, brown, etc).  Then I made the final sound file roughly quarter second.  When a continuous thruster sound was
	/// wanted, the sound clip was repeated over and over, but there was a slight delay between playing.
	/// 
	/// Instead, use a longer sound file (20 seconds or so), then keep pausing/resuming when they want short bursts, instead
	/// of stopping, and repositioning to 0.
    /// </remarks>
    public class SoundPool
    {
        // Types of scenarios:
        //     - multiple sources playing the same repeatable sound (at the same volume)
        //     - take in a 3D vector, and calculate balance
        //     - take in volume

        #region Declaration Section
        #endregion

        #region Public Properties

        private double _distanceFalloffRatio = 1d;
        /// <summary>
        /// This is used when calculating how quickly sound volume should drop relative to distance from the listener
        /// (default is 1)
        /// </summary>
        /// <remarks>
        /// negative is illegal
        /// 0 means it never drops off
        /// .5 means it takes 2 units to drop off 1 (half as slow to drop off)
        /// 1 is default
        /// 2 means it drops off twice as quickly (half a unit is 1)
        /// </remarks>
        public double DistanceFalloffRatio
        {
            get
            {
                return _distanceFalloffRatio;
            }
            set
            {
                if (value < 0d)
                {
                    throw new ArgumentOutOfRangeException("DistanceFalloffRatio", "Negative values aren't allowed (" + value.ToString() + ")");
                }

                _distanceFalloffRatio = value;
            }
        }

        #endregion

        public void Test(string waveFileName)
        {
            MediaPlayer player = new MediaPlayer();
            player.Open(new Uri(waveFileName));
            player.Play();

            //player.Balance
            //player.Volume

            //player.SpeedRatio            // wish it had pitch instead




        }

        #region Test Player

        private MediaPlayer _testPlayer = new MediaPlayer();

        private bool _shouldRepeat = false;

        public SoundPool(string waveFileName)
        {
            _testPlayer.Open(new Uri(waveFileName));

            _testPlayer.MediaEnded += new EventHandler(testPlayer_MediaEnded);
        }

        public void Test1()
        {
            if (_testPlayer.Position.TotalSeconds > 0d && _testPlayer.Position < _testPlayer.NaturalDuration.TimeSpan)
            {
                // It's in the middle of playing
                return;
            }

            _testPlayer.Stop();
            _testPlayer.Position = new TimeSpan(0);

            _testPlayer.Play();
        }

        public void TestPlayRepeat()
        {
            _shouldRepeat = true;

            _testPlayer.Play();
        }
        public void TestPlayPause()
        {
            _testPlayer.Pause();
        }


        private void testPlayer_MediaEnded(object sender, EventArgs e)
        {
            if (!_shouldRepeat)
            {
                return;
            }

            // There is a tiny gap before it plays again.  Very annoying (the MediaElement has the same gap when a repeating animation is controlling it)
            _testPlayer.Stop();
            _testPlayer.Position = new TimeSpan(0);

            _testPlayer.Play();
        }

        #endregion

        #region Private Methods

        private void GetPositionalSoundSettings(out double volume, out double balance, Vector offset, double sourceVolume)
        {
            // The media player only supports stereo playback, but I need the 3D vector to calculate distance
            GetPositionalSoundSettings(out volume, out balance, new Vector3D(offset.X, offset.Y, 0), sourceVolume);
        }
        /// <summary>
        /// This will emulate sounds coming from locations relative to the listener
        /// </summary>
        /// <remarks>
        /// Note that sound intensity doesn't diminish linearly from by distance, but by:
        ///    I = P/(4*pi*r^2)
        /// 
        /// I is final intensity
        /// P is intensity at the source
        /// r is distance from the listener
        /// </remarks>
        /// <param name="volume">0 (none) to 1 (full)</param>
        /// <param name="balance">-1 (all left) to 1 (all right)</param>
        /// <param name="offset">The location of the sound relative to the listener</param>
        /// <param name="sourceVolume">0 (none) to 1 (full) - this is how loud the source would be at the listener</param>
        private void GetPositionalSoundSettings(out double volume, out double balance, Vector3D offset, double sourceVolume)
        {
            #region Calculate Volume

            // I won't range check the input volume - I'll leave it up to the media player to either bomb or cap it

            double intensityRatio = 1d;
            double offsetLength = offset.Length;
            if (_distanceFalloffRatio == 0d || Math3D.IsNearZero(offsetLength))
            {
                volume = sourceVolume;
            }
            else
            {
                double distance = offsetLength * _distanceFalloffRatio;

                intensityRatio = 1d / (4d * Math.PI * distance * distance);    // I need this intensity when calculating balance
                volume = sourceVolume * intensityRatio;
            }

            #endregion

            #region Calculate Balance

            // This means that if a sound is a distance of very near zero, and all the way to the left or right, then instead of +-1, it is +-.5
            const double MAXOPPOSITE_EAR = .5d;
            // When the intensity would be 75% of max, then I won't add anything to the opposite ear
            const double MAXOPPOSITE_DISTANCEINTENSITY = .75d;

            // Getting the angle (I don't want Z)
            double angle = Vector3D.AngleBetween(new Vector3D(1, 0, 0), new Vector3D(offset.X, offset.Y, 0));

            // cos(0) is 1, cos(90) is 0, cos(180) is -1.  Exactly what I need
            balance = Math.Cos(Math3D.DegreesToRadians(angle));

            //NOTE:  The problem with a pure cosine is that if a loud sound is sitting very near the person, but on the left, then in reality, the right
            // ear would hear something, but a simple cosine would be all the way -1
            //
            // So, I'm going to approach .5 for objects that are near (still on the left, but can be heard on the right)
            if (intensityRatio > MAXOPPOSITE_DISTANCEINTENSITY)
            {
                // So when the intensity is 1 (right next to the head), then I will give back .5
                // When the intensity is at the limit, then balance will be the pure cosine value
                // Anywhere in between is a linear interpelation
                balance = UtilityCore.GetScaledValue_Capped(balance, MAXOPPOSITE_EAR * balance, MAXOPPOSITE_DISTANCEINTENSITY, 1d, intensityRatio);
            }

            #endregion

            //TODO:  Take in relative velocity, then manipulate playback speed to emulate doppler (unless there is a way to change pitch directly)
        }

        #endregion
    }
}

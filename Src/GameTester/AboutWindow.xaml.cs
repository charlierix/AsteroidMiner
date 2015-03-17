using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace Game.GameTester
{
    public partial class AboutWindow : Window
    {
        #region Constructor

        public AboutWindow()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            lblGeneral.Text =
@"This app is a collection of testers with the end goal of making an artificial life simulation.  When that alife simulator is more finished, I'll create a dedicated app for that.

I'm sharing this with the public because even if you don't care about the alife simulator, you may find some of the code useful in your own projects.

This is written in C#, and uses WPF.  The physics engine is an open source dll called Newton Dynamics.

The physics engine has an open source zlib licence, so I'm doing the same for this app.  But personally, I don't care if people copy bits of my code for their own use, that's why I'm sharing this publicly.";

            lblALifeGoal.Text =
@"The goal for this app is to be an artificial life simulation.  At first, it will be space with asteroids and minerals, but eventually there will be fluid and planets.  So you could have flyers, swimmers, crawlers all in the same map.

The bots will start off as randomly generated with random neural net brains, or they could be created with the editor.  The designs that are able to survive their environment will be able to breed or reproduce asexually.

I also want the human player to have the option to play along side the bots, harvesting minerals or other bots.  They should be able to upgrade their ship, or create a new one, whatever.  The goal isn't to be an in depth video game, just a fun way to interact with an artificial life simulation.

The user could also just choose to run the simulation without having their own ship.  They should be able to manipulate the environment: import/clone/delete/modify/move bots or other game objects, tweek map settings, etc.

Most of the settings for the workings of a bot are defined by the map.  The bot's shape is defined for that bot, but the density of all the parts, energy draw, etc are defined by the map.  This way, you could make thrusters expensive, and tractor beams cheap, then sit back to see what emerges.

The defintion of a map will also hold size, vector fields, radiation, gravity, asteroids, planets, stars, etc.

The idea is that the bots will evolve to be optimal for the map they are in.  A robust bot could be cloned across many maps and still thrive.";

            lblPointsOfInterest.Text =
@"Everything in the Orig folder is old stuff, pre wpf.  Once wpf came out, I had to rename a lot of the math classes with My in front of them.  There may still be some useful stuff in there though.

The Game.HelperClassesWPF project has quite a bit in there.  It is a combination of other people's code and mine:
	- Extenders has some handy extension methods (Vector.ToPoint() and Point.ToVector() should ship native, they make life a lot easier)
	- Math3D has a lot in it
	- UtilityWPF has a bunch of color stuff, as well as a bunch of 3D geometry builders (cube, sphere, etc)
	- StaticRandom is a singleton that creates one random class per request thread
	- Triangle is a good triangle primitive (ITriangle, and two implementations of that)
	- Trackball is reworked from the codeplex 3D tools Trackball, it has an easy way to set up mappings
	- ScreenSpaceLines3D is also from codeplex 3D tools, it is very useful to draw lines in 3D

Game.Newt.v2.NewtonDynamics is a C# wrapper to the newton dll.  Newton is a great engine, I have a feeling a lot of the downloads will just be for this.";

            txtLicense.Text =
@"This software is provided 'as-is', without any express or implied warranty. In no event will the authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this software for any purpose, including commercial applications, and to alter it and redistribute it freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.

3. This notice may not be removed or altered from any source distribution.";
        }

        #endregion

        #region Event Listeners

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Pull the url text out of the sender
                string link = GetLinkText(sender);
                if (link == null)
                {
                    MessageBox.Show("Couldn't figure what the url", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show this on the default browser
                System.Diagnostics.Process.Start(link);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private static string GetLinkText(object sender)
        {
            Hyperlink senderCast = sender as Hyperlink;
            if (senderCast == null)
            {
                return null;
            }

            if(senderCast.NavigateUri != null)
            {
                // The link was explicitely set
                return senderCast.NavigateUri.AbsoluteUri;
            }

            // The link is the text in the inline
            Run inline = senderCast.Inlines.FirstOrDefault() as Run;
            if (inline == null)
            {
                return null;
            }

            string retVal = inline.Text;
            if (string.IsNullOrEmpty(retVal))
            {
                return null;
            }

            // I was going to write some regex to validate it, but somebody mentioned this method
            Uri test;
            if (!Uri.TryCreate(retVal, UriKind.RelativeOrAbsolute, out test))
            {
                return null;
            }

            // Exit Function
            return retVal;
        }

        #endregion
    }
}

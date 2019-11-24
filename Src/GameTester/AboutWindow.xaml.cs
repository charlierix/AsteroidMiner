﻿using System;
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

            #region lblGeneral

            lblGeneral.Text =
@"This app is a collection of testers with the end goal of making an artificial life simulation.  When that alife simulator is more finished, I'll create a dedicated app for that.

I'm sharing this with the public because even if you don't care about the alife simulator, you may find some of the code useful in your own projects.

This is written in C#, and uses WPF.  The physics engine is an open source dll called Newton Dynamics.

The physics engine has an open source zlib licence, so I'm doing the same for this app.  But personally, I don't care if people copy bits of my code for their own use, that's why I'm sharing this publicly.";

            #endregion
            #region lblALifeGoal

            lblALifeGoal.Text =
@"The goal for this app is to be an artificial life simulation.  At first, it will be space with asteroids and minerals, but eventually there will be fluid and planets.  So you could have flyers, swimmers, crawlers all in the same map.

The bots will start off as randomly generated with random neural net brains, or they could be created with the editor.  The designs that are able to survive their environment will be able to breed or reproduce asexually.

I also want the human player to have the option to play along side the bots, harvesting minerals or other bots.  They should be able to upgrade their ship, or create a new one, whatever.  The goal isn't to be an in depth video game, just a fun way to interact with an artificial life simulation.

The user could also just choose to run the simulation without having their own ship.  They should be able to manipulate the environment: import/clone/delete/modify/move bots or other game objects, tweek map settings, etc.

Most of the settings for the workings of a bot are defined by the map.  The bot's shape is defined for that bot, but the density of all the parts, energy draw, etc are defined by the map.  This way, you could make thrusters expensive, and tractor beams cheap, then sit back to see what emerges.

The defintion of a map will also hold size, vector fields, radiation, gravity, asteroids, planets, stars, etc.

The idea is that the bots will evolve to be optimal for the map they are in.  A robust bot could be cloned across many maps and still thrive.";

            #endregion
            #region lblPointsOfInterest

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

            #endregion
            #region txtLicense_zLib

            txtLicense_zLib.Text =
@"This software is provided 'as-is', without any express or implied warranty. In no event will the authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this software for any purpose, including commercial applications, and to alter it and redistribute it freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.

3. This notice may not be removed or altered from any source distribution.";

            #endregion
            #region txtLicense_Apache

            txtLicense_Apache.Text =
@"Encog(tm) Core - C#/.Net Version
http://www.heatonresearch.com/encog/
https://github.com/encog/encog-dotnet-core

For more information on Heaton Research copyrights, licenses and trademarks visit:
http://www.heatonresearch.com/copyright

-------------------------------------------------------------------------------
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      ""License"" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      ""Licensor"" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      ""Legal Entity"" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      ""control"" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      ""You"" (or ""Your"") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      ""Source"" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      ""Object"" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      ""Work"" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      ""Derivative Works"" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      ""Contribution"" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, ""submitted""
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as ""Not a Contribution.""

      ""Contributor"" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a ""NOTICE"" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an ""AS IS"" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS


-----------------------------------------------------------------------
- Copyright notice from Libsvm                                        - 
- (http://www.csie.ntu.edu.tw/~cjlin/libsvm/)                         -
-----------------------------------------------------------------------
Libsvm
Copyright (c) 2000-2010 Chih-Chung Chang and Chih-Jen Lin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:

1. Redistributions of source code must retain the above copyright
notice, this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright
notice, this list of conditions and the following disclaimer in the
documentation and/or other materials provided with the distribution.

3. Neither name of copyright holders nor the names of its contributors
may be used to endorse or promote products derived from this software
without specific prior written permission.


THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
";

            #endregion
            #region txtLicense_MIT

            txtLicense_MIT.Text = @"/******************************************************************************
 *
 * The MIT License (MIT)
 *
 * MIConvexHull, Copyright (c) 2015 David Sehnal, Matthew Campbell
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the ""Software""), to deal
 * in the Software without restriction, including without limitation the rights
 *to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
  * copies of the Software, and to permit persons to whom the Software is
  *furnished to do so, subject to the following conditions:
            *
            *The above copyright notice and this permission notice shall be included in
 *all copies or substantial portions of the Software.

*
*THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *THE SOFTWARE.

*
*****************************************************************************/


/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2016 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software; you can redistribute it and/or modify
 * it under the terms of The MIT License (MIT).
 *
 * You should have received a copy of the MIT License
 * along with SharpNEAT; if not, see https://opensource.org/licenses/MIT.
 */
";

            #endregion
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

            if (senderCast.NavigateUri != null)
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

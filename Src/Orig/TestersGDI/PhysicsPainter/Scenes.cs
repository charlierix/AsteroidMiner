using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Game.Orig.HelperClassesOrig;
using Game.Orig.HelperClassesGDI.Controls;
using Game.Orig.Map;
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
	public partial class Scenes : PiePanel
	{
		#region Declaration Section

		private SimpleMap _map = null;
		private ObjectRenderer _personalRenderer = null;

		private List<RadarBlip[]> _sceneBlips = new List<RadarBlip[]>();

		//	These tokens will be ignored (not saved to a scene)
        private List<long> _ignoreTokens = null;

		#endregion

		#region Constructor

		public Scenes()
		{
			InitializeComponent();

			toolTip1.SetToolTip(btnLoad, "F5");
			toolTip1.SetToolTip(btnSave, "F6");
		}

		#endregion

		#region Public Methods

		public void SetPointers(SimpleMap map, ObjectRenderer renderer, List<long> ignoreTokens)
		{
			//	Store Stuff
			_map = map;
            _ignoreTokens = ignoreTokens;

            // Make my picturebox look like what the renderer is painting on
			pictureBox1.BackColor = renderer.Viewer.BackColor;
            pictureBox1.SetBorder(renderer.Viewer.BoundryLower, renderer.Viewer.BoundryUpper);
			if (renderer.Viewer.ShouldDrawCheckerBackground)
			{
				pictureBox1.ShowCheckerBackground(renderer.Viewer.CheckerOtherColor, renderer.Viewer.NumCheckersPerSide);
			}
			else
			{
                pictureBox1.HideBackground();
			}

			if (renderer.Viewer.ShouldDrawBorder)
			{
                pictureBox1.ShowBorder(renderer.Viewer.BorderColor, renderer.Viewer.BorderWidth);
			}
			else
			{
                pictureBox1.HideBorder();
			}

			//	I want my viewer to show the whole scene
            pictureBox1.ZoomFit();

			//	Now clone the renderer
            _personalRenderer = new ObjectRenderer(pictureBox1);
		}

		/// <summary>
		/// This is meant to be called when they push a hotkey
		/// </summary>
		public void SaveCurrentScene()
		{
			btnSave_Click(this, new EventArgs());
		}
		/// <summary>
		/// This is meant to be called when they push a hotkey
		/// </summary>
		public void LoadCurrentScene()
		{
			btnLoad_Click(this, new EventArgs());
		}

		#endregion

		#region Misc Control Events

		private void btnSave_Click(object sender, EventArgs e)
		{
			if (_map == null)
			{
				MessageBox.Show("Control hasn't been set up yet", "Save Scene", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			List<RadarBlip> clonedBlips = new List<RadarBlip>();

			//	Clone them as fast as I can
			foreach (RadarBlip blip in _map.GetAllBlips())
			{
				if (_ignoreTokens.Contains(blip.Token))
				{
					continue;
				}

				//TODO:  Clone this right (instead of hardcoding ballblip)
				clonedBlips.Add(CloneBlip(blip, _map));
			}

			//	Store it
			_sceneBlips.Add(clonedBlips.ToArray());

			listView1.Items.Add("Scene " + ((int)(listView1.Items.Count + 1)).ToString());
			listView1.Items[listView1.Items.Count - 1].Selected = true;

            // Draw the scene
            DrawScene(clonedBlips.ToArray());
		}
		private void btnLoad_Click(object sender, EventArgs e)
		{
			if (_map == null)
			{
				MessageBox.Show("Control hasn't been set up yet", "Load Scene", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			ListViewItem selectedItem = GetSelectedItem();

			if (selectedItem == null)
			{
				MessageBox.Show("Nothing Selected", "Load Scene", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			if (selectedItem.Index < 0 || selectedItem.Index >= _sceneBlips.Count)
			{
				MessageBox.Show("Lists out of sync", "Load Scene", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			_map.Clear(_ignoreTokens);

			foreach (RadarBlip blip in _sceneBlips[selectedItem.Index])
			{
				//	If I don't clone them, then when the balls move, those new positions will be stored in my list
				_map.Add(CloneBlip(blip, _map));
			}
		}

		private void btnRemove_Click(object sender, EventArgs e)
		{
			if (_map == null)
			{
				MessageBox.Show("Control hasn't been set up yet", "Remove Scene", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			ListViewItem selectedItem = GetSelectedItem();

			if (selectedItem == null)
			{
				MessageBox.Show("Nothing Selected", "Remove Scene", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			if (selectedItem.Index < 0 || selectedItem.Index >= _sceneBlips.Count)
			{
				MessageBox.Show("Lists out of sync", "Load Scene", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			int selectedIndex = selectedItem.Index;		//	cache this for later (it goes to -1 after being removed from the listview)

			//	Remove scene
			_sceneBlips.RemoveAt(selectedIndex);
			listView1.Items.RemoveAt(selectedIndex);

			//	Don't show the old scene anymore (when the next item is selected, that scene will display instead)
			pictureBox1.BackgroundImage = null;

			#region Rename Scenes

			foreach (ListViewItem item in listView1.Items)
			{
				if (Regex.Match(item.Text, @"Scene \d+").Success)		//	if it doesn't match this pattern, then they named it, and I don't want to change that
				{
					item.Text = "Scene " + ((int)(item.Index + 1)).ToString();
				}
			}

			#endregion

			//	Select a different scene
			if (listView1.Items.Count - 1 >= selectedIndex)
			{
				//	Select the item that is sitting in the same location as the item that was just removed
				listView1.Items[selectedIndex].Selected = true;
			}
			else if (listView1.Items.Count > 0)
			{
				//	Select the last item
				listView1.Items[listView1.Items.Count - 1].Selected = true;
			}

		}

		private void listView1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_map == null)
			{
				MessageBox.Show("Control hasn't been set up yet", "Index Change", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (listView1.SelectedIndices.Count == 0)		//	I don't call GetSelectedItem here, because it doesn't work right in this event.  Also, this event only seems to be called when the listview is visible, so SelectedIndices can be trusted
			{
				btnRemove.Enabled = false;
				btnLoad.Enabled = false;
				return;
			}
			if (listView1.SelectedIndices[0] < 0 || listView1.SelectedIndices[0] >= _sceneBlips.Count)
			{
				MessageBox.Show("Lists out of sync", "Index Change", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			btnRemove.Enabled = true;
			btnLoad.Enabled = true;

            DrawScene(_sceneBlips[listView1.SelectedIndices[0]]);
		}

		#endregion

		#region Private Methods

		public static RadarBlip CloneBlip(RadarBlip blip, SimpleMap map)
		{
			BallBlip retVal = new BallBlip((Ball)blip.Sphere.Clone(), blip.CollisionStyle, blip.Qual, map.GetNextToken());
			retVal.Ball.Velocity.StoreNewValues(((Ball)blip.Sphere).Velocity);

			if (blip.Sphere is TorqueBall)
			{
				((TorqueBall)retVal.Sphere).AngularMomentum.StoreNewValues(((TorqueBall)blip.Sphere).AngularMomentum);
			}

			return retVal;
		}

		private ListViewItem GetSelectedItem()
		{
			foreach (ListViewItem item in listView1.Items)
			{
				if (item.Selected)
				{
					return item;
				}
			}

			return null;
		}

        private void DrawScene(RadarBlip[] blips)
        {
            pictureBox1.ZoomFit();
            pictureBox1.PrepareForNewDraw();       // _personalRenderer.Viewer is picturebox1

            //	Draw all the blips
            foreach (RadarBlip blip in blips)
            {
                //	Draw the blip
                if (blip.Sphere is RigidBody)
                {
                    _personalRenderer.DrawRigidBody((RigidBody)blip.Sphere, ObjectRenderer.DrawMode.Standard, blip.CollisionStyle, false);
                }
                else if (blip.Sphere is SolidBall)
                {
                    _personalRenderer.DrawSolidBall((SolidBall)blip.Sphere, ObjectRenderer.DrawMode.Standard, blip.CollisionStyle, false);
                }
                else if (blip.Sphere is Ball)
                {
                    _personalRenderer.DrawBall((Ball)blip.Sphere, ObjectRenderer.DrawMode.Standard, blip.CollisionStyle, false);
                }
                else
                {
                    throw new ApplicationException("Unknown Blip");
                }
            }

            pictureBox1.FinishedDrawing();
        }

		#endregion
	}
}

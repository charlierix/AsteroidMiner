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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

using Game.Newt.HelperClasses;
using Game.Newt.HelperClasses.Primitives3D;

namespace Game.Newt.Testers
{
	/// <summary>
	/// This form was written to test DoubleVector.GetAngleAroundAxis
	/// </summary>
	public partial class DoubleVectorWindow : Window
	{
		#region Class: ItemColors

		private class ItemColors
		{
			private Random _rand = new Random();

			public Color Highlight = UtilityWPF.ColorFromHex("F2BC57");
			public Color LightLight = UtilityWPF.ColorFromHex("FBFCF2");
			public Color Light = UtilityWPF.ColorFromHex("F1F1E4");
			public Color Gray = UtilityWPF.ColorFromHex("8B8C84");
			public Color DarkGray = UtilityWPF.ColorFromHex("3C4140");

			//public Color TrackballAxisX = UtilityWPF.ColorFromHex("8B8C84");
			//public Color TrackballAxisY = UtilityWPF.ColorFromHex("72726C");
			//public Color TrackballAxisZ = UtilityWPF.ColorFromHex("989990");

			public Color TrackballAxisX_From = UtilityWPF.ColorFromHex("4C638C");
			public Color TrackballAxisY_From = UtilityWPF.ColorFromHex("4D5B73");
			public Color TrackballAxisZ_From = UtilityWPF.ColorFromHex("4C638C");

			public Color TrackballAxisX_To = UtilityWPF.ColorFromHex("694C8C");
			public Color TrackballAxisY_To = UtilityWPF.ColorFromHex("5E4D73");
			public Color TrackballAxisZ_To = UtilityWPF.ColorFromHex("694C8C");
			public SpecularMaterial TrackballAxisSpecular = new SpecularMaterial(Brushes.White, 100d);

			public Color TrackballGrabberHoverLight = UtilityWPF.ColorFromHex("3F382A");

			public Color FromLine(int index)
			{
				//return this.Gray;
				return UtilityWPF.ColorFromHex("446EB8");
			}
			public Color ToLine(int index)
			{
				//return this.Gray;
				return UtilityWPF.ColorFromHex("6B4896");
			}
			public Color RotatedLine(int index)
			{
				return this.Highlight;
			}
		}

		#endregion

		#region Declaration Section

		private const double LINELENGTH_X = 2d;
		private const double LINELENGTH_Y = .5d;
		private const double LINELENGTH_Z = 1.5d;

		private Random _rand = new Random();
		private ItemColors _colors = new ItemColors();
		private bool _isInitialized = false;

		/// <summary>
		/// This listens to the mouse/keyboard and controls the camera
		/// </summary>
		private TrackBallRoam _trackball = null;

		//	0=X, 1=Y, 2=Z
		ScreenSpaceLines3D[] _lineFrom = null;
		ScreenSpaceLines3D[] _lineTo = null;
		ScreenSpaceLines3D[] _lineTransformed = null;

		private List<ModelVisual3D> _orientationVisualsFrom = new List<ModelVisual3D>();
		private TrackballGrabber _orientationTrackballFrom = null;

		private List<ModelVisual3D> _orientationVisualsTo = new List<ModelVisual3D>();
		private TrackballGrabber _orientationTrackballTo = null;

		#endregion

		#region Constructor

		public DoubleVectorWindow()
		{
			InitializeComponent();
		}

		#endregion

		#region Event Listeners

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Camera Trackball
				_trackball = new TrackBallRoam(_camera);
				_trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
				_trackball.AllowZoomOnMouseWheel = true;
				_trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
				//_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

				//	Trackball Controls
				SetupFromTrackball();
				SetupToTrackball();

				//	Add the 3 axiis to the main viewport
				_lineFrom = BuildLines(_orientationTrackballFrom.Transform, true);
				_lineTo = BuildLines(_orientationTrackballTo.Transform, false);

				_isInitialized = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void Window_Closed(object sender, EventArgs e)
		{

		}

		private void OrientationTrackballFrom_RotationChanged(object sender, EventArgs e)
		{
			//TODO:  The TrackballGrabber class is rotating the camera, because I can't get accurate results without doing that.
			Matrix3D transformMatrix = _orientationTrackballFrom.Transform.Value;
			transformMatrix.Invert();
			_cameraFromRotateLight.Transform = new MatrixTransform3D(transformMatrix);


			//	Wipe the transformed lines until they click the button
			ClearLines(_lineTransformed);
			_lineTransformed = null;

			//	Rebuild the from lines
			ClearLines(_lineFrom);
			_lineFrom = BuildLines(_orientationTrackballFrom.Transform, true);



			//	TODO:  When the TrackballGrabber no longer rotates the camera, the visuals will need to be rotated instead
			//foreach (ModelVisual3D model in _modelOrientationVisuals)
			//{
			//    model.Transform = _modelOrientationTrackball.Transform;
			//}
		}
		private void OrientationTrackballTo_RotationChanged(object sender, EventArgs e)
		{
			//TODO:  The TrackballGrabber class is rotating the camera, because I can't get accurate results without doing that.
			Matrix3D transformMatrix = _orientationTrackballTo.Transform.Value;
			transformMatrix.Invert();
			_cameraToLight.Transform = new MatrixTransform3D(transformMatrix);

			//	Wipe the transformed lines until they click the button
			ClearLines(_lineTransformed);
			_lineTransformed = null;

			//	Rebuild the to lines
			ClearLines(_lineTo);
			_lineTo = BuildLines(_orientationTrackballTo.Transform, false);
		}

		private void grdFrom_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (chkSnap45Degrees.IsChecked.Value)
			{
				EnsureSnapped45(_orientationTrackballFrom);
			}
		}
		private void grdTo_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (chkSnap45Degrees.IsChecked.Value)
			{
				EnsureSnapped45(_orientationTrackballTo);
			}
		}
		private void chkSnap45Degrees_Checked(object sender, RoutedEventArgs e)
		{
			if (chkSnap45Degrees.IsChecked.Value)
			{
				EnsureSnapped45(_orientationTrackballFrom);
				EnsureSnapped45(_orientationTrackballTo);
			}
		}

		private void btnTestIt_Click(object sender, RoutedEventArgs e)
		{
			Vector3D defaultStand = new Vector3D(LINELENGTH_X * .75d, 0, 0);
			Vector3D defaultOrth = new Vector3D(0, 0, LINELENGTH_Z * .75d);

			DoubleVector from = new DoubleVector(_orientationTrackballFrom.Transform.Transform(defaultStand), _orientationTrackballFrom.Transform.Transform(defaultOrth));
			DoubleVector to = new DoubleVector(_orientationTrackballTo.Transform.Transform(defaultStand), _orientationTrackballTo.Transform.Transform(defaultOrth));

			//	This is the method that this form is testing
			Quaternion quat = from.GetAngleAroundAxis(to);

			//DoubleVector rotated = new DoubleVector(quat.GetRotatedVector(defaultStand), quat.GetRotatedVector(defaultOrth));
			DoubleVector rotated = new DoubleVector(quat.GetRotatedVector(from.Standard), quat.GetRotatedVector(from.Orth));		//	quat describes how to rotate from From to To (not from Default to To)

			//Vector3D axis = quat.Axis.ToUnit() * (LINELENGTH_X * 2d);
			Vector3D axis = _orientationTrackballFrom.Transform.Transform(quat.Axis.ToUnit() * (LINELENGTH_X * 1.5d));		//	I rotate axis by from, because I want it relative to from (axis isn't in world coords)

			ClearLines(_lineTransformed);
			_lineTransformed = BuildLines(rotated, axis);
		}

		#endregion

		#region Private Methods

		private static void EnsureSnapped45(TrackballGrabber trackball)
		{
			//TODO:  I think this is invalid thinking.  Instead?:
			//		Use the quaternion to project a vector
			//		Snap that projected vector to the nearest aligned vector
			//		Get a new quaternion that describes how to rotate to that snapped vector

			//TODO:  When the trackball's transform is set, the lights get screwed up

			Quaternion currentOrientation = trackball.Transform.ToQuaternion();

			//	Snap the axis to nearest 45
			Vector3D snappedAxis = currentOrientation.Axis;

			//	Snap the angle to nearest 45
			double snappedAngle = GetSnappedAngle(currentOrientation.Angle, 45d);

			trackball.Transform = new RotateTransform3D(new AxisAngleRotation3D(snappedAxis, snappedAngle));
		}

		private static double GetSnappedAngle(double angle, double snapValue)
		{
			double retVal = angle;

			//	Make sure angle is between -360 to 360
			while (retVal < -360d)
			{
				retVal += 360d;
			}

			while (retVal > 360d)
			{
				retVal -= 360d;
			}

			double absSnapValue = Math.Abs(snapValue);

			//	Check for positive angles
			for (double fromAngle = 0d; fromAngle <= 360d; fromAngle += absSnapValue)
			{
				if (retVal > fromAngle && retVal < fromAngle + absSnapValue)
				{
					if (retVal - fromAngle < absSnapValue * .5d)
					{
						retVal = fromAngle;
					}
					else
					{
						retVal = fromAngle + absSnapValue;
					}

					return retVal;
				}
			}

			//	Check for negative angles
			for (double fromAngle = 0d; fromAngle >= -360d; fromAngle -= absSnapValue)
			{
				if (retVal > fromAngle - absSnapValue && retVal < fromAngle)
				{
					if (fromAngle - retVal < absSnapValue * .5d)
					{
						retVal = fromAngle;
					}
					else
					{
						retVal = fromAngle - absSnapValue;
					}

					return retVal;
				}
			}

			//	It's already snapped to that value
			return retVal;
		}

		private void SetupFromTrackball()
		{
			RotateTransform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0d));

			SetupTrackballSprtAddIt(ref _orientationTrackballFrom, _orientationVisualsFrom, _viewportFrom, grdFrom, _colors, transform, true);
			_orientationTrackballFrom.RotationChanged += new EventHandler(OrientationTrackballFrom_RotationChanged);
		}
		private void SetupToTrackball()
		{
			//NOTE:  When they start with different transforms, the display vectors are screwed up
			RotateTransform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0d));

			SetupTrackballSprtAddIt(ref _orientationTrackballTo, _orientationVisualsTo, _viewportTo, grdTo, _colors, transform, false);
			_orientationTrackballTo.RotationChanged += new EventHandler(OrientationTrackballTo_RotationChanged);
		}
		private void SetupTrackballSprtAddIt(ref TrackballGrabber trackball, List<ModelVisual3D> visuals, Viewport3D viewport, FrameworkElement eventSource, ItemColors colors, RotateTransform3D modelTransform, bool isFrom)
		{
			#region major arrow along x

			//	Material
			MaterialGroup materials = new MaterialGroup();
			materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(isFrom ? colors.TrackballAxisX_From : colors.TrackballAxisX_To)));
			materials.Children.Add(colors.TrackballAxisSpecular);

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;
			geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, .075, 1d);

			Transform3DGroup transform = new Transform3DGroup();		//	rotate needs to be added before translate
			transform.Children.Add(new TranslateTransform3D(new Vector3D(.5d, 0, 0)));

			geometry.Transform = transform;

			//	Model Visual
			ModelVisual3D model = new ModelVisual3D();
			model.Content = geometry;

			//	Add it
			viewport.Children.Add(model);
			visuals.Add(model);

			#endregion
			#region x line cone

			//	Material
			materials = new MaterialGroup();
			materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(isFrom ? colors.TrackballAxisX_From : colors.TrackballAxisX_To)));
			materials.Children.Add(colors.TrackballAxisSpecular);

			//	Geometry Model
			geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;
			geometry.Geometry = UtilityWPF.GetCone_AlongX(10, .15d, .3d);

			transform = new Transform3DGroup();		//	rotate needs to be added before translate
			transform.Children.Add(new TranslateTransform3D(new Vector3D(1d + .1d, 0, 0)));

			geometry.Transform = transform;

			//	Model Visual
			model = new ModelVisual3D();
			model.Content = geometry;

			//	Add it
			viewport.Children.Add(model);
			visuals.Add(model);

			#endregion
			#region x line cap

			//	Material
			materials = new MaterialGroup();
			materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(isFrom ? colors.TrackballAxisX_From : colors.TrackballAxisX_To)));
			materials.Children.Add(colors.TrackballAxisSpecular);

			//	Geometry Model
			geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;
			geometry.Geometry = UtilityWPF.GetSphere(20, .075d);

			//	Model Visual
			model = new ModelVisual3D();
			model.Content = geometry;

			//	Add it
			viewport.Children.Add(model);
			visuals.Add(model);

			#endregion

			#region minor arrow along z

			//	Material
			materials = new MaterialGroup();
			materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(isFrom ? colors.TrackballAxisZ_From : colors.TrackballAxisZ_To)));
			materials.Children.Add(colors.TrackballAxisSpecular);

			//	Geometry Model
			geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;
			geometry.Geometry = UtilityWPF.GetCylinder_AlongX(10, .05, .5d);

			transform = new Transform3DGroup();		//	rotate needs to be added before translate
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, -1, 0), 90d)));
			transform.Children.Add(new TranslateTransform3D(new Vector3D(0, 0, .25d)));

			geometry.Transform = transform;

			//	Model Visual
			model = new ModelVisual3D();
			model.Content = geometry;

			//	Add it
			viewport.Children.Add(model);
			visuals.Add(model);

			#endregion
			#region z line cone

			//	Material
			materials = new MaterialGroup();
			materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(isFrom ? colors.TrackballAxisZ_From : colors.TrackballAxisZ_To)));
			materials.Children.Add(colors.TrackballAxisSpecular);

			//	Geometry Model
			geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;
			geometry.Geometry = UtilityWPF.GetCone_AlongX(10, .075d, .2d);

			transform = new Transform3DGroup();		//	rotate needs to be added before translate
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, -1, 0), 90d)));
			transform.Children.Add(new TranslateTransform3D(new Vector3D(0, 0, .5d + .1d)));

			geometry.Transform = transform;

			//	Model Visual
			model = new ModelVisual3D();
			model.Content = geometry;

			//	Add it
			viewport.Children.Add(model);
			visuals.Add(model);

			#endregion

			#region faint line along y

			ScreenSpaceLines3D line = new ScreenSpaceLines3D(true);
			line.Thickness = 1d;
			line.Color = isFrom ? colors.TrackballAxisY_From : colors.TrackballAxisY_To;
			line.AddLine(new Point3D(0, -.75d, 0), new Point3D(0, .75d, 0));

			//	Add it
			viewport.Children.Add(line);
			visuals.Add(line);

			#endregion

			trackball = new TrackballGrabber(eventSource, viewport, 1d, colors.TrackballGrabberHoverLight);
			trackball.Transform = modelTransform;
		}

		private ScreenSpaceLines3D[] BuildLines(RotateTransform3D transform, bool isFromLine)
		{
			ScreenSpaceLines3D[] retVal = new ScreenSpaceLines3D[3];

			for (int cntr = 0; cntr < retVal.Length; cntr++)
			{
				retVal[cntr] = new ScreenSpaceLines3D(true);
				retVal[cntr].Color = isFromLine ? _colors.FromLine(cntr) : _colors.ToLine(cntr);
				retVal[cntr].Thickness = cntr == 1 ? 1d : 2d;
			}

			retVal[0].AddLine(transform.Transform(new Point3D(0, 0, 0)), transform.Transform(new Point3D(LINELENGTH_X, 0, 0)));
			retVal[1].AddLine(transform.Transform(new Point3D(0, -LINELENGTH_Y, 0)), transform.Transform(new Point3D(0, LINELENGTH_Y, 0)));
			retVal[2].AddLine(transform.Transform(new Point3D(0, 0, 0)), transform.Transform(new Point3D(0, 0, LINELENGTH_Z)));

			for (int cntr = 0; cntr < retVal.Length; cntr++)
			{
				_viewport.Children.Add(retVal[cntr]);
			}

			return retVal;
		}
		private ScreenSpaceLines3D[] BuildLines(DoubleVector transformedLine, Vector3D rotationAxis)
		{
			ScreenSpaceLines3D[] retVal = new ScreenSpaceLines3D[4];

			for (int cntr = 0; cntr < retVal.Length; cntr++)
			{
				retVal[cntr] = new ScreenSpaceLines3D(true);
				retVal[cntr].Color = _colors.RotatedLine(cntr);

				if (cntr == 1)
				{
					retVal[cntr].Thickness = 3d;		//	this one isn't really used
				}
				else if (cntr == 3)
				{
					retVal[cntr].Thickness = 1d;
				}
				else
				{
					retVal[cntr].Thickness = 6d;
				}
			}

			retVal[0].AddLine(new Point3D(0, 0, 0), transformedLine.Standard.ToPoint());
			retVal[1].AddLine(new Point3D(0, 0, 0), new Point3D(0, 0, 0));		//	just adding this so everything stays lined up
			retVal[2].AddLine(new Point3D(0, 0, 0), transformedLine.Orth.ToPoint());
			retVal[3].AddLine(new Point3D(0, 0, 0), rotationAxis.ToPoint());

			for (int cntr = 0; cntr < retVal.Length; cntr++)
			{
				_viewport.Children.Add(retVal[cntr]);
			}

			return retVal;
		}
		private void ClearLines(ScreenSpaceLines3D[] lines)
		{
			if (lines != null)
			{
				for (int cntr = 0; cntr < lines.Length; cntr++)
				{
					if (_viewport.Children.Contains(lines[cntr]))
					{
						_viewport.Children.Remove(lines[cntr]);
					}
				}
			}
		}

		#endregion
	}
}

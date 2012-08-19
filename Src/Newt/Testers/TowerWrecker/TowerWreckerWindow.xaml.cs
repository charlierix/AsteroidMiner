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
using Game.Newt.NewtonDynamics;
using Game.HelperClasses;

namespace Game.Newt.Testers.TowerWrecker
{
	//TODO:  Make a utility method similar to AddCannonBall/AddBrick/GetWPFModel that builds a collision hull, wpf visual, body.  It should take in an args class, and return
	//	a class that holds the physics body, and all the pieces of the visual.  The args tell it which pieces to build (only wpf, only physics, everything, etc).  This method should cover
	//	about 80% of the basic bodies that get built

	//TODO:  The explosion class is only pushing on the center of mass.  It needs to account for the geometry of what it's hitting - make this a generic helper, I also want to the
	//	helper to calculate fluid friction along arbitrary directions (to model fish and airplanes) - the explosion is like a point light source, and fluid is more of a directional light

	public partial class TowerWreckerWindow : Window
	{
		#region Enum: LeftClickAction

		private enum LeftClickAction
		{
			ShootBall,
			PanCamera,
			Remove,
			RemoveLine,
			Explode,
			ExplodeLine,
			Implode,
			ImplodeLine,
			ForceBeam
		}

		#endregion

		#region Class: ExplosionWithVisual

		private class ExplosionWithVisual : Explosion, IDisposable
		{
			#region Declaration Section

			private Viewport3D _viewport = null;

			//	This is the expanding sphere
			private ModelVisual3D _visual = null;
			private DiffuseMaterial _material = null;

			//	Every explosion needs a light
			private PointLight _pointLight = null;

			private bool _isExplode = false;
			private double _visualStartRadius = 0d;
			private Color _baseColor = Colors.Transparent;

			#endregion

			#region Constructor

			/// <summary>
			/// This overload will keep the explosion relative to the body.
			/// NOTE:  This class won't touch body.Visuals
			/// </summary>
			public ExplosionWithVisual(Body body, double waveSpeed, double forceAtCenter, double maxRadius, Viewport3D viewport, double visualStartRadius)
				: base(body, waveSpeed, forceAtCenter, maxRadius)
			{
				_viewport = viewport;
				_visualStartRadius = visualStartRadius;
				CreateVisual();
			}
			public ExplosionWithVisual(Point3D centerPoint, double waveSpeed, double forceAtCenter, double maxRadius, Viewport3D viewport, double visualStartRadius)
				: base(centerPoint, waveSpeed, forceAtCenter, maxRadius)
			{
				_viewport = viewport;
				_visualStartRadius = visualStartRadius;
				CreateVisual();
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				if (disposing)
				{
					RemoveVisual();
				}
			}

			#endregion

			#region Overrides

			public override bool Update(double elapsedTime)
			{
				bool retVal = base.Update(elapsedTime);

				if (retVal)
				{
					RemoveVisual();
				}
				else
				{
					#region Update Visual

					//	Transparency
					double transparency = UtilityHelper.GetScaledValue_Capped(0d, 1d, _visualStartRadius, this.MaxRadius * .75d, this.Radius);		//	I want it to become invisible sooner
					_material.Brush = new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.Transparent, _baseColor, transparency));

					//	Scale (this is what Body.OnBodyMoved does, plus scale)
					//NOTE:  The radius gets huge, but the force drops off quickly, so it doesn't look right if I scale it to full size
					double scale = UtilityHelper.GetScaledValue_Capped(1d, (this.MaxRadius * .05d) / _visualStartRadius, _visualStartRadius, this.MaxRadius, this.Radius);

					Transform3DGroup transform = new Transform3DGroup();
					transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

					if (this.Body == null)
					{
						transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));
					}
					else
					{
						transform.Children.Add(new MatrixTransform3D(this.Body.OffsetMatrix));
					}

					_visual.Transform = transform;

					//NOTE:  The shell is smaller than this.Radius (once it overtakes _visualStartRadius)
					if (this.Radius > _visualStartRadius * 2)
					{
						_pointLight.Range = this.Radius;
					}

					#endregion
				}

				//	Exit Function
				return retVal;
			}

			#endregion

			#region Private Methods

			private void CreateVisual()
			{
				//NOTE:  I am not adding this to this.Body.Visuals, because I need to scale the visual as well (also, this class removes the visual when update reaches zero)

				_isExplode = _forceAtCenter > 0d;

				//	Figure out colors
				Color reflectColor, lightColor;
				if (_isExplode)
				{
					//	Explode
					_baseColor = Colors.Coral;
					reflectColor = Colors.Gold;
					lightColor = Color.FromArgb(128, 252, 255, 136);
				}
				else
				{
					//	Implode
					_baseColor = Colors.CornflowerBlue;
					reflectColor = Colors.DarkOrchid;
					lightColor = Color.FromArgb(128, 183, 190, 255);
				}

				//	Shell
				_visual = GetWPFModel(out _material, _baseColor, reflectColor, 30d, _visualStartRadius, this.Position);		//	I want to keep this pretty close to the original method, so I'll let it build a full ModelVisual, and I'll pull the model out of that

				Model3DGroup models = new Model3DGroup();
				models.Children.Add(_visual.Content);

				//	Light
				_pointLight = new PointLight();
				_pointLight.Color = lightColor;
				_pointLight.Range = _visualStartRadius * 2;		//	radius will eventually overtake this, then it will be set to radius
				_pointLight.QuadraticAttenuation = .33;
				models.Children.Add(_pointLight);

				//	Now store the group instead
				_visual.Content = models;

				_viewport.Children.Add(_visual);
			}
			private void RemoveVisual()
			{
				if (_visual != null)
				{
					_viewport.Children.Remove(_visual);
				}
			}

			private static ModelVisual3D GetWPFModel(out DiffuseMaterial bodyMaterial, Color color, Color reflectionColor, double reflectionIntensity, double radius, Point3D position)
			{
				//	Material
				MaterialGroup materials = new MaterialGroup();
				bodyMaterial = new DiffuseMaterial(new SolidColorBrush(color));
				materials.Children.Add(bodyMaterial);
				materials.Children.Add(new SpecularMaterial(new SolidColorBrush(reflectionColor), reflectionIntensity));

				//	Geometry Model
				GeometryModel3D geometry = new GeometryModel3D();
				geometry.Material = materials;
				geometry.BackMaterial = materials;
				geometry.Geometry = UtilityWPF.GetSphere(5, radius, radius, radius);

				//	Transform
				TranslateTransform3D transform = new TranslateTransform3D(position.ToVector());

				//	Model Visual
				ModelVisual3D retVal = new ModelVisual3D();
				retVal.Content = geometry;
				retVal.Transform = transform;

				//	Exit Function
				return retVal;
			}

			#endregion
		}

		#endregion
		#region Class: Explosion

		/// <summary>
		/// This models an explosion over time
		/// </summary>
		/// <remarks>
		/// The radius of the shockwave is a constant velocity (very fast, but constant)
		/// 
		/// I assume the force of the wave is k/dist^2
		/// 
		/// Not sure, but I assume the forces behind the shockwave are much smaller, so it's a moving wall of force
		/// 
		/// This would make a good base class.  This is a spherical explosion (not a shaped charge) that doesn't get deformed by the bodies that
		/// it hits.
		/// 
		/// It looks like if you are going to have a shaped charge, forces increase along the wrinkle lines?
		/// 
		/// TODO:  Put this class in a helper dll
		/// </remarks>
		private class Explosion
		{
			#region Declaration Section

			protected double _waveSpeed = 0d;
			protected double _forceAtCenter = 0d;

			#endregion

			#region Constructor

			/// <summary>
			/// NOTE:  You will probably want to make the projectile non collidable when it becomes the source of a collision (swap its material with one
			/// that doesn't collide)
			/// </summary>
			public Explosion(Body body, double waveSpeed, double forceAtCenter, double maxRadius)
			{
				this.Body = body;

				_waveSpeed = waveSpeed;
				_forceAtCenter = forceAtCenter;
				_maxRadius = maxRadius;
			}
			public Explosion(Point3D centerPoint, double waveSpeed, double forceAtCenter, double maxRadius)
			{
				_position = centerPoint;

				_waveSpeed = waveSpeed;
				_forceAtCenter = forceAtCenter;
				_maxRadius = maxRadius;
			}

			#endregion

			#region Public Properties

			/// <summary>
			/// This is the body that exploded (could be null if this is an arbitrarily generated explosion)
			/// </summary>
			public Body Body
			{
				get;
				private set;
			}

			private Point3D? _position = null;
			public Point3D Position
			{
				get
				{
					if (_position != null)
					{
						//	They used the constructor that doesn't take a body, and instead takes a position directly
						return _position.Value;
					}
					else
					{
						//	They used the constructor that takes a body.  Use its position (it could still be moving)
						return this.Body.Position;
					}
				}
				set
				{
					if (_position == null)
					{
						throw new InvalidOperationException("Position can only be set when this class used the constructor that takes an explicit position");
					}

					_position = value;
				}
			}

			private double _radius = 0d;
			/// <summary>
			/// This is the maximum radius that this explosion currently reaches (changes every time update is called)
			/// </summary>
			public double Radius
			{
				get
				{
					return _radius;
				}
			}

			private double _maxRadius = 0d;
			public double MaxRadius
			{
				get
				{
					return _maxRadius;
				}
			}

			#endregion

			#region Public Methods

			/// <summary>
			/// This will whack the body by the explosion.
			/// NOTE:  Only call from within the Body.ApplyForceAndTorque callback
			/// </summary>
			public void ApplyForceToBody(Body body)
			{
				//TODO:  This needs to add some rotation.  Otherwise it just looks odd (everything is too even)

				Vector3D explosiveForce = GetForceAtPoint(body.Position);
				if (explosiveForce.X != 0d || explosiveForce.Y != 0d || explosiveForce.Z != 0d)
				{
					body.AddForce(explosiveForce);
				}
			}
			/// <summary>
			/// This returns the force that this explosion generates on the point (point is in world coords)
			/// </summary>
			/// <remarks>
			/// This is over simplified.  The force should really depend on the shape of the object, and the object should also interfere with the
			/// explosion's shock wave (reflect it back like a water ripple bouncing off a wall)
			/// </remarks>
			public Vector3D GetForceAtPoint(Point3D point)
			{
				Vector3D lineFromCenter = point - this.Position;

				double distanceSquared = lineFromCenter.LengthSquared;

				if (distanceSquared > _radius * _radius || distanceSquared == 0d)
				{
					return new Vector3D(0, 0, 0);
				}
				//else if (distance < _radius * .666667)		//	commenting this, because fast moving waves will completly miss bodies which looks really bad
				//{
				//    //	The force is only felt in the shockwave.  Once the wave has passed there is nearly no force
				//    //TODO:  Figure out what this force is.  Every site I visit talks about the force of an explosion, but not the force within this bubble behind
				//    //	the shockwave.  Looking at slow motion video of explosions, it seems pretty still, so I'm just going to return zero
				//    //
				//    //	It's a complete guess, but I'm going to set the depth of this wave at 33% of the current radius
				//    return new Vector3D(0, 0, 0);
				//}

				double force = _forceAtCenter / (4d * Math.PI * distanceSquared);
				if (double.IsInfinity(force) || double.IsNaN(force) || force > _forceAtCenter)
				{
					force = _forceAtCenter;
				}

				lineFromCenter.Normalize();
				lineFromCenter = lineFromCenter * force;

				return lineFromCenter;
			}

			/// <summary>
			/// This updates the explosion (which changes the current force and radius).  Returns true when the explosion is finished
			/// </summary>
			public virtual bool Update(double elapsedTime)
			{
				_radius += elapsedTime * _waveSpeed;

				if (_radius > _maxRadius)
				{
					//	This explosion has expired.  I'm not going to bother cleaning up member variables.  The caller should remove this class anyway
					return true;
				}

				return false;
			}

			#endregion
		}

		#endregion
		#region Class: BodyMaterial

		private class BodyMaterial
		{
			public BodyMaterial(DiffuseMaterial material, Color origColor, double origSize)
			{
				this.Material = material;
				this.OrigColor = origColor;
				this.OrigSize = origSize;
			}

			public DiffuseMaterial Material = null;
			public Color OrigColor = Colors.Black;
			public double OrigSize = 0d;
			public double MaxImpulse = 0d;
		}

		#endregion

		#region Declaration Section

		private const double TERRAINRADIUS = 100d;
		private const double GRAVITY = -9.8d;

		private World _world = null;

		private MaterialManager _materialManager = null;
		private int _material_Terrain = -1;
		private int _material_Brick = -1;
		private int _material_Projectile = -1;
		private int _material_ExplodingProjectile = -1;		//	there is no property on body to turn off collision detection, that's done by its current material

		private Body _terrain = null;

		private List<Body> _bricks = new List<Body>();
		private List<Body> _projectiles = new List<Body>();
		private SortedList<Body, double> _projectilesLitFuse = new SortedList<Body, double>();		//	when the double counts down to zero, the projectile needs to move from _projectiles to _explosions
		private List<ExplosionWithVisual> _explosions = new List<ExplosionWithVisual>();

		private SortedList<Body, BodyMaterial> _bodyMaterials = new SortedList<Body, BodyMaterial>();
		//private SortedList<Body, ModelVisual3D> _explosionVisuals = new SortedList<Body, ModelVisual3D>();

		/// <summary>
		/// This listens to the mouse/keyboard and controls the camera
		/// </summary>
		private TrackBallRoam _trackball = null;

		private bool _hasGravity = true;

		/// <summary>
		/// When a model is created, this is the direction it starts out as
		/// </summary>
		private DoubleVector _defaultDirectionFacing = new DoubleVector(1, 0, 0, 0, 0, 1);

		private double _lastTowerHeight = 25d;

		private LeftClickAction _leftClickAction = LeftClickAction.PanCamera;
		private ForceBeamSettingsArgs _forceBeamSettings = null;
		private bool _isRayFiring = false;		//	goes true when _leftClickAction is one of the continuous ray actions and they are actively pushing down on the left mouse button
		private Point3D _rayPoint;
		private Vector3D _rayDirection;

		private bool _isInitialized = false;

		#endregion

		#region Constructor

		public TowerWreckerWindow()
		{
			InitializeComponent();

			_isInitialized = true;
		}

		#endregion

		#region Event Listeners

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Init World
				_world = new World();
				//_world.SetSolverModel(SolverModel.ExactMode);		//	all of them are slower than adaptive
				_world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);
				_world.SetWorldSize(new Point3D(-1000, -1000, -1000), new Point3D(1000, 1000, 1000));
				_world.UnPause();

				#region Materials

				_materialManager = new MaterialManager(_world);

				//	Terrain
				Game.Newt.NewtonDynamics.Material material = new Game.Newt.NewtonDynamics.Material();
				material.Elasticity = .2d;
				_material_Terrain = _materialManager.AddMaterial(material);

				//	Brick
				material = new Game.Newt.NewtonDynamics.Material();
				_material_Brick = _materialManager.AddMaterial(material);

				//	Projectile
				material = new Game.Newt.NewtonDynamics.Material();
				material.Elasticity = .6d;
				material.IsContinuousCollision = true;
				_material_Projectile = _materialManager.AddMaterial(material);

				//	Exploding Projectile
				material = new Game.Newt.NewtonDynamics.Material();
				material.IsCollidable = false;
				_material_ExplodingProjectile = _materialManager.AddMaterial(material);

				_materialManager.RegisterCollisionEvent(_material_Projectile, _material_Brick, Collision_ProjectileBrick);
				_materialManager.RegisterCollisionEvent(_material_Brick, _material_Brick, Collision_BrickBrick);

				#endregion

				#region Terrain

				#region WPF Model (plus collision hull)

				//	Material
				MaterialGroup materials = new MaterialGroup();
				materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 110, 96, 72))));
				materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.Ivory, Colors.Transparent, .15d)), 25d));

				//	Geometry Model
				GeometryModel3D geometry = new GeometryModel3D();
				geometry.Material = materials;
				geometry.BackMaterial = materials;

				double terrainHeight = TERRAINRADIUS / 10d;

				geometry.Geometry = UtilityWPF.GetCylinder_AlongX(100, TERRAINRADIUS, terrainHeight);
				CollisionHull hull = CollisionHull.CreateCylinder(_world, 0, TERRAINRADIUS, terrainHeight, null);

				//	Transform
				Transform3DGroup transform = new Transform3DGroup();		//	rotate needs to be added before translate
				transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));
				transform.Children.Add(new TranslateTransform3D(new Vector3D(0, 0, terrainHeight / -2d)));		//	I want the objects to be able to add to z=0

				//	Model Visual
				ModelVisual3D model = new ModelVisual3D();
				model.Content = geometry;
				model.Transform = transform;

				//	Add to the viewport
				_viewport.Children.Add(model);

				#endregion

				//	Make a physics body that represents this shape
				_terrain = new Body(hull, transform.Value, 0, new Visual3D[] { model });		//	using zero mass tells newton it's static scenery (stuff bounces off of it, but it will never move)
				_terrain.MaterialGroupID = _material_Terrain;



				//AddDebugDot(new Point3D(0, 0, 0), 5, Colors.Yellow);
				//AddDebugDot(new Point3D(0, 0, 8), 3, Colors.Yellow);
				//AddDebugDot(new Point3D(0, 0, 12), 1, Colors.Yellow);


				#endregion

				//	Trackball
				_trackball = new TrackBallRoam(_camera);
				_trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
				_trackball.AllowZoomOnMouseWheel = true;
				_trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
				//_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

				RadioLeftClick_Checked(this, new RoutedEventArgs());
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void Window_Closed(object sender, EventArgs e)
		{
			try
			{
				ClearProjectilesAndExplosions();
				ClearBricks();

				_world.Pause();
				_world.Dispose();
				_world = null;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void World_Updating(object sender, WorldUpdatingArgs e)
		{
			#region Explosions

			int index = 0;
			while (index < _explosions.Count)
			{
				ExplosionWithVisual explosion = _explosions[index];

				//	Tell this explosion to advance its shockwave
				if (explosion.Update(e.ElapsedTime))
				{
					#region Remove Explosion

					//	It has expired, remove it
					explosion.Dispose();
					explosion.Body.Dispose();
					_explosions.RemoveAt(index);

					#endregion
				}
				else
				{
					index++;
				}
			}

			#endregion
			#region Lit Fuses

			index = 0;
			while (index < _projectilesLitFuse.Keys.Count)
			{
				bool isExplode = !radProjectilesImplode.IsChecked.Value;		//	not checking if explode is selected, because they may have switched to standard while the fuse was lit, just defaulting to explode

				Body projectile = _projectilesLitFuse.Keys[index];

				_projectilesLitFuse[projectile] -= e.ElapsedTime;
				if (_projectilesLitFuse[projectile] <= 0)
				{
					#region Convert to explosion

					ExplodeBody(projectile, isExplode, 1d);

					//	Remove from projectile lists
					_projectiles.Remove(projectile);
					_projectilesLitFuse.Remove(projectile);

					#endregion
				}
				else
				{
					index++;
				}
			}

			#endregion
		}
		private void Body_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
		{
			try
			{
				#region Explosions

				if (e.Body.MaterialGroupID == _material_Brick || e.Body.MaterialGroupID == _material_Projectile)
				{
					foreach (Explosion explosion in _explosions)
					{
						explosion.ApplyForceToBody(e.Body);
					}
				}

				#endregion

				#region Gravity

				if (_hasGravity && !e.Body.IsAsleep)
				{
					e.Body.AddForce(new Vector3D(0, 0, GRAVITY * e.Body.Mass));
				}

				#endregion

				#region Force Beam

				if (_isRayFiring && _leftClickAction == LeftClickAction.ForceBeam)		//	_forceBeamSettings will never be null by this point
				{
					Point3D bodyPosition = e.Body.Position;

					Point3D nearestPointOnLine = Math3D.GetNearestPointAlongLine(_rayPoint, _rayDirection, bodyPosition);
					Vector3D directionToLine = nearestPointOnLine - bodyPosition;

					if (directionToLine.LengthSquared < _forceBeamSettings.Radius * _forceBeamSettings.Radius)
					{
						#region Calculate Force Multiplier

						//	Get a ratio from 0 to 1
						double distanceRatio = directionToLine.Length / _forceBeamSettings.Radius;

						double forceMultiplier;
						if (_forceBeamSettings.IsLinearDropoff)
						{
							forceMultiplier = 1d - distanceRatio;
						}
						else
						{
							forceMultiplier = .01d / (distanceRatio * distanceRatio);
							if (forceMultiplier > 1d || double.IsInfinity(forceMultiplier) || double.IsNaN(forceMultiplier))
							{
								forceMultiplier = 1d;
							}
						}

						#endregion

						Vector3D addForce = new Vector3D(0, 0, 0);

						if (distanceRatio > 0d)
						{
							#region Away/Toward

							Vector3D forceToward = directionToLine.ToUnit() * _forceBeamSettings.TowardAwayForce;
							forceToward = forceToward.GetRotatedVector(_rayDirection, _forceBeamSettings.Angle);

							addForce += forceToward;

							#endregion
						}

						//	Push/Pull
						addForce += _rayDirection.ToUnit() * _forceBeamSettings.PushPullForce;

						//	Add force to the body
						e.Body.AddForce(addForce);
					}
				}

				#endregion
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Collision_ProjectileBrick(object sender, MaterialCollisionArgs e)
		{
			const double MAXIMPULSE = 150;

			if (e.Body0.MaterialGroupID == _material_ExplodingProjectile || e.Body1.MaterialGroupID == _material_ExplodingProjectile)
			{
				//	Since I'm changing the material to an explosion, this event still gets called several times in a single frame, but the body has changed
				//	material type and GetBody throws an exception
				return;
			}

			Body projectile = e.GetBody(_material_Projectile);

			#region Light Fuse

			if (radProjectilesExplode.IsChecked.Value && !_projectilesLitFuse.ContainsKey(projectile))
			{
				_projectilesLitFuse.Add(projectile, .01);		//	seconds
			}
			else if (radProjectilesImplode.IsChecked.Value && !_projectilesLitFuse.ContainsKey(projectile))
			{
				_projectilesLitFuse.Add(projectile, .01);		//	seconds
			}

			#endregion

			#region Paint Red

			if (chkColoredImpacts.IsChecked.Value)
			{
				BodyMaterial material = _bodyMaterials[e.GetBody(_material_Brick)];
				foreach (MaterialCollision collision in e.Collisions)
				{
					if (collision.ContactNormalSpeed > material.MaxImpulse)
					{
						//	This is the biggest force this brick has felt, paint it a shade of red
						material.MaxImpulse = collision.ContactNormalSpeed;

						byte red = Convert.ToByte(UtilityHelper.GetScaledValue_Capped(material.OrigColor.R, 255, 0, MAXIMPULSE, material.MaxImpulse));		//	increase red
						byte green = Convert.ToByte(UtilityHelper.GetScaledValue_Capped(0, material.OrigColor.G, MAXIMPULSE, 0, material.MaxImpulse));		//	decrease green and blue
						byte blue = Convert.ToByte(UtilityHelper.GetScaledValue_Capped(0, material.OrigColor.B, MAXIMPULSE, 0, material.MaxImpulse));

						material.Material.Brush = new SolidColorBrush(Color.FromArgb(255, red, green, blue));
					}
				}
			}

			#endregion
		}
		private void Collision_BrickBrick(object sender, MaterialCollisionArgs e)
		{
			const double MAXIMPULSE = 150;

			//TODO:  There are 3 copies of the same code, consolidate a bit

			#region Paint Blue

			if (chkColoredImpacts.IsChecked.Value)
			{
				BodyMaterial material0 = _bodyMaterials[e.Body0];
				BodyMaterial material1 = _bodyMaterials[e.Body1];

				foreach (MaterialCollision collision in e.Collisions)
				{
					if (collision.ContactNormalSpeed > material0.MaxImpulse)
					{
						double speed = collision.ContactNormalSpeed;

						//	This is the biggest force this brick has felt, paint it a shade of red
						material0.MaxImpulse = speed;

						byte red = Convert.ToByte(UtilityHelper.GetScaledValue_Capped(0, material0.OrigColor.R, MAXIMPULSE, 0, speed));		//	decrease red and green
						byte green = Convert.ToByte(UtilityHelper.GetScaledValue_Capped(0, material0.OrigColor.G, MAXIMPULSE, 0, speed));
						byte blue = Convert.ToByte(UtilityHelper.GetScaledValue_Capped(material0.OrigColor.B, 255, 0, MAXIMPULSE, speed));		//	increase blue

						material0.Material.Brush = new SolidColorBrush(Color.FromArgb(255, red, green, blue));
					}

					if (collision.ContactNormalSpeed > material1.MaxImpulse)
					{
						double speed = collision.ContactNormalSpeed;

						//	This is the biggest force this brick has felt, paint it a shade of red
						material1.MaxImpulse = speed;

						byte red = Convert.ToByte(UtilityHelper.GetScaledValue_Capped(0, material1.OrigColor.R, MAXIMPULSE, 0, speed));		//	decrease red and green
						byte green = Convert.ToByte(UtilityHelper.GetScaledValue_Capped(0, material1.OrigColor.G, MAXIMPULSE, 0, speed));
						byte blue = Convert.ToByte(UtilityHelper.GetScaledValue_Capped(material1.OrigColor.B, 255, 0, MAXIMPULSE, speed));		//	increase blue

						material1.Material.Brush = new SolidColorBrush(Color.FromArgb(255, red, green, blue));
					}
				}
			}

			#endregion
		}

		private void trkSimulationSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			try
			{
				if (_world == null)
				{
					return;
				}

				//	Get linear from 0 to 1
				double speed = UtilityHelper.GetScaledValue_Capped(0d, 1d, trkSimulationSpeed.Minimum, trkSimulationSpeed.Maximum, trkSimulationSpeed.Value);

				//	The interesting stuff happens near zero, so keep the numbers small longer
				speed = Math.Pow(speed, 2d);

				//	Now scale this
				speed = UtilityHelper.GetScaledValue_Capped(.001d, 1d, 0d, 1d, speed);

				_world.SimulationSpeed = speed;
				lblSimSpeed.Content = "sim speed: " + Math.Round(speed, 3).ToString();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void chkGravity_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				_hasGravity = chkGravity.IsChecked.Value;

				if (_hasGravity)
				{
					foreach (Body body in _bricks)
					{
						if (body.IsAsleep)
						{
							//	I had a case where I turned on long time after a collision, and bricks stayed suspended in midair (this isn't guaranteed to get them all, but
							//	should almost every time)
							body.AutoSleep = false;
							body.AutoSleep = true;
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		//TODO:  Handle MosueLeave (or capture the mouse if firing a ray)
		private void grdViewPort_MouseDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				if (_leftClickAction == LeftClickAction.PanCamera || e.LeftButton != MouseButtonState.Pressed)
				{
					return;
				}

				//	All the other actions need a ray fired
				Point point = e.GetPosition(grdViewPort);
				FireRay(point);

				//	Some of the actions need the ray to be continous
				switch (_leftClickAction)
				{
					case LeftClickAction.ExplodeLine:
					case LeftClickAction.ImplodeLine:
					case LeftClickAction.RemoveLine:
					case LeftClickAction.ForceBeam:
						_isRayFiring = true;
						break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void grdViewPort_MouseUp(object sender, MouseButtonEventArgs e)
		{
			try
			{
				if (e.LeftButton == MouseButtonState.Released)
				{
					_isRayFiring = false;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void grdViewPort_MouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				if (_isRayFiring)
				{
					Point point = e.GetPosition(grdViewPort);
					FireRay(point);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void RadioLeftClick_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!_isInitialized)
				{
					return;
				}

				_leftClickAction = GetLeftClickAction();

				bool showCombo = false;
				bool isPanCamera = false;
				bool showTrackbar = false;
				bool showLineCheckbox = false;
				bool showBeamSettings = false;

				switch (_leftClickAction)
				{
					case LeftClickAction.ShootBall:
						showCombo = true;
						//showTrackbar = true;		//	just use the trkBulletSpeed slider
						break;

					case LeftClickAction.PanCamera:
						isPanCamera = true;
						break;

					case LeftClickAction.Remove:
					case LeftClickAction.RemoveLine:
						showLineCheckbox = true;
						break;

					case LeftClickAction.Explode:
					case LeftClickAction.ExplodeLine:
					case LeftClickAction.Implode:
					case LeftClickAction.ImplodeLine:
						showTrackbar = true;
						showLineCheckbox = true;
						break;

					case LeftClickAction.ForceBeam:
						showBeamSettings = true;
						_forceBeamSettings = forceBeamSettings1.BeamSettings;		//	just making sure it's current
						break;

					default:
						throw new ApplicationException("Unknown LeftClickAction: " + _leftClickAction.ToString());
				}

				//	Combo
				if (showCombo)
				{
					cboLeftBallType.Visibility = Visibility.Visible;
				}
				else
				{
					cboLeftBallType.Visibility = Visibility.Collapsed;
				}

				//	Trackball
				_trackball.Mappings.Clear();
				if (isPanCamera)
				{
					_trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
				}
				else
				{
					_trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
				}

				//	Trackbar
				if (showTrackbar)
				{
					trkLeftExplodePower.Visibility = Visibility.Visible;
				}
				else
				{
					trkLeftExplodePower.Visibility = Visibility.Collapsed;
				}

				//	Checkbox
				if (showLineCheckbox)
				{
					chkLeftLine.Visibility = Visibility.Visible;
				}
				else
				{
					chkLeftLine.Visibility = Visibility.Collapsed;
				}

				//	Force Beam Settings
				if (showBeamSettings)
				{
					forceBeamSettings1.Visibility = Visibility.Visible;
				}
				else
				{
					forceBeamSettings1.Visibility = Visibility.Collapsed;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void BeamSettings_Changed(object sender, ForceBeamSettingsArgs e)
		{
			_forceBeamSettings = e;
		}

		private void btnPellet_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Point3D position = GetCannonPosition();
				Vector3D velocity = GetCannonVelocity();
				Vector3D angularVelocity = new Vector3D();
				DoubleVector directionFacing = new DoubleVector(velocity, Math3D.GetArbitraryOrhonganal(velocity));
				Vector3D size = new Vector3D(.5d, .5d, .5d);

				AddCannonBall(CollisionShapeType.Sphere, Colors.SteelBlue, size, 2d, position, velocity, angularVelocity, directionFacing);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnSlug_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Point3D position = GetCannonPosition();
				Vector3D velocity = GetCannonVelocity();
				Vector3D angularVelocity = new Vector3D();
				DoubleVector directionFacing = new DoubleVector(velocity, Math3D.GetArbitraryOrhonganal(velocity));
				Vector3D size = new Vector3D(.5d, .8d, 0d);		//	x is radius, y is height

				AddCannonBall(CollisionShapeType.Cylinder, Colors.DimGray, size, 6d, position, velocity, angularVelocity, directionFacing);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnBaseball_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Point3D position = GetCannonPosition();
				Vector3D velocity = GetCannonVelocity();
				Vector3D angularVelocity = new Vector3D();
				DoubleVector directionFacing = new DoubleVector(velocity, Math3D.GetArbitraryOrhonganal(velocity));
				Vector3D size = new Vector3D(2d, 2d, 2d);

				AddCannonBall(CollisionShapeType.Sphere, Colors.GhostWhite, size, 5d, position, velocity, angularVelocity, directionFacing);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnCannon_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Color color = Color.FromArgb(255, 45, 45, 32);
				Point3D position = GetCannonPosition();
				Vector3D velocity = GetCannonVelocity();
				Vector3D angularVelocity = new Vector3D();
				DoubleVector directionFacing = new DoubleVector(velocity, Math3D.GetArbitraryOrhonganal(velocity));
				Vector3D size = new Vector3D(2d, 2d, 2d);

				AddCannonBall(CollisionShapeType.Sphere, color, size, 15d, position, velocity, angularVelocity, directionFacing);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnWrecker_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Color color = Color.FromArgb(255, 50, 15, 15);
				Point3D position = GetCannonPosition();
				Vector3D velocity = GetCannonVelocity();
				Vector3D angularVelocity = new Vector3D();
				DoubleVector directionFacing = new DoubleVector(velocity, Math3D.GetArbitraryOrhonganal(velocity));
				Vector3D size = new Vector3D(4d, 4d, 4d);

				AddCannonBall(CollisionShapeType.Sphere, color, size, 60d, position, velocity, angularVelocity, directionFacing);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnDrillHead_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Point3D position = Math3D.GetRandomVectorSpherical2D(3).ToPoint();
				position.Z = _lastTowerHeight + 5;
				Vector3D velocity = new Vector3D(0, 0, -3);
				Vector3D angularVelocity = new Vector3D(UtilityHelper.GetScaledValue_Capped(0d, 400d, trkBulletSpeed.Minimum, trkBulletSpeed.Maximum, trkBulletSpeed.Value), 0, 0);		//	AddCannonBall rotates this so it will be about the z axis
				DoubleVector directionFacing = new DoubleVector(0, 0, -1, 1, 0, 0);
				Vector3D size = new Vector3D(3d, 5d, 0d);		//	x is radius, y is height

				AddCannonBall(CollisionShapeType.Cone, Colors.Indigo, size, 200d, position, velocity, angularVelocity, directionFacing);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnSpinningBar_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Point3D position = Math3D.GetRandomVectorSpherical2D(5).ToPoint();
				position.Z = _lastTowerHeight + 1d;
				Vector3D velocity = new Vector3D(0, 0, -3);
				Vector3D angularVelocity = new Vector3D(0, 0, UtilityHelper.GetScaledValue_Capped(5d, 200d, trkBulletSpeed.Minimum, trkBulletSpeed.Maximum, trkBulletSpeed.Value));
				//DoubleVector directionFacing = _defaultDirectionFacing;		//	just leaving it the same as default
				Vector3D size = new Vector3D(50d, 2d, .25d);

				AddCannonBall(CollisionShapeType.Box, Colors.Silver, size, 350d, position, velocity, angularVelocity, _defaultDirectionFacing);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnBigSpinner_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//Point3D position = Math3D.GetRandomVectorSpherical2D(_rand, 240).ToPoint();

				//	I want this to act like a sword, so I don't want it starting in the center
				Vector3D position = new Vector3D(150, 0, 0);
				position = Math3D.RotateAroundAxis(position, new Vector3D(0, 0, 1), Math3D.GetNearZeroValue(Math.PI * 2d));
				position.Z = Math3D.GetNearZeroValue(5d) + 10d;
				Vector3D velocity = new Vector3D(0, 0, 0);
				Vector3D angularVelocity = new Vector3D(0, 0, UtilityHelper.GetScaledValue_Capped(.1d, 5d, trkBulletSpeed.Minimum, trkBulletSpeed.Maximum, trkBulletSpeed.Value));

				//Vector3D dirFacingStand = new Vector3D(1, 0, Math3D.GetNearZeroValue(_rand, .01d));
				//Vector3D dirFacingOrth = new Vector3D(0, 0, 1);
				//dirFacingOrth = dirFacingOrth.GetRotatedVector(new Vector3D(0, -1, 0), Vector3D.AngleBetween(new Vector3D(1, 0, 0), dirFacingStand));
				//DoubleVector directionFacing = new DoubleVector(dirFacingStand, dirFacingOrth);

				DoubleVector directionFacing = _defaultDirectionFacing.GetRotatedVector(new Vector3D(0, 1, 0), Math3D.GetNearZeroValue(1d));

				Vector3D size = new Vector3D(350d, 7d, .25d);

				AddCannonBall(CollisionShapeType.Box, Colors.Silver, size, 3000d, position.ToPoint(), velocity, angularVelocity, directionFacing);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnClearProjectiles_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearProjectilesAndExplosions();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnTowerMarbles_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearProjectilesAndExplosions();
				ClearBricks();

				#region Figure out how many marbles

				int xLimit = 12;
				int yLimit, zLimit;

				if (radFew.IsChecked.Value)
				{
					xLimit = 3;
					yLimit = 3;
					zLimit = 6;
				}
				else if (radNormal.IsChecked.Value)
				{
					xLimit = 5;
					yLimit = 5;
					zLimit = 10;
				}
				else if (radMany.IsChecked.Value)
				{
					xLimit = 8;
					yLimit = 8;
					zLimit = 16;
				}
				else if (radExtreme.IsChecked.Value)
				{
					xLimit = 12;
					yLimit = 12;
					zLimit = 24;
				}
				else
				{
					MessageBox.Show("Unknow number of bricks", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				#endregion

				for (int x = -xLimit; x <= xLimit; x++)
				{
					for (int y = -yLimit; y <= yLimit; y++)
					{
						for (int z = 0; z <= zLimit; z++)		//	start on the floor
						{
							Color color = UtilityWPF.GetRandomColor(255, 110, 160);
							Vector3D size = new Vector3D(.5d, .5d, .5d);
							Point3D position = new Point3D(x, y, z + .5d);

							AddBrick(CollisionShapeType.Sphere, color, size, 1d, position, _defaultDirectionFacing);
						}
					}
				}

				_lastTowerHeight = zLimit + .5d;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnTowerSpikes_Click(object sender, RoutedEventArgs e)
		{
			const double BASERADIUS = 2d;

			try
			{
				ClearProjectilesAndExplosions();
				ClearBricks();

				#region Figure out how many spikes

				int spikeLimit;

				if (radFew.IsChecked.Value)
				{
					spikeLimit = 15;
				}
				else if (radNormal.IsChecked.Value)
				{
					spikeLimit = 20;
				}
				else if (radMany.IsChecked.Value)
				{
					spikeLimit = 25;
				}
				else if (radExtreme.IsChecked.Value)
				{
					spikeLimit = 30;
				}
				else
				{
					MessageBox.Show("Unknow number of bricks", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				#endregion

				List<Vector3D> prevPositions = new List<Vector3D>();

				for (int cntr = 0; cntr < spikeLimit; cntr++)
				{
					#region Find non colliding position

					Vector3D pos2D = new Vector3D();
					bool havePosition = false;
					for (int infiniteLoopCntr = 0; infiniteLoopCntr < 100; infiniteLoopCntr++)
					{
						//	Try this one
						pos2D = Math3D.GetRandomVectorSpherical2D(20d);

						havePosition = true;
						foreach (Vector3D prevPos in prevPositions)
						{
							if ((pos2D - prevPos).Length < BASERADIUS * 2)
							{
								//	Colliding with a previous tower
								havePosition = false;
								break;
							}
						}

						if (havePosition)
						{
							//	This is a unique position, quit trying
							break;
						}
					}

					if (!havePosition)
					{
						//	Couldn't find a unique position, quit trying to make towers
						break;
					}

					//	Remember the location of this tower
					prevPositions.Add(pos2D);

					#endregion

					BuildSpike(pos2D.ToPoint(), true);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}

		}
		private void btnTowerSpike_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearProjectilesAndExplosions();
				ClearBricks();

				BuildSpike(new Point3D(0, 0, 0), false);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnTowerDense2_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearProjectilesAndExplosions();
				ClearBricks();

				//	Spike
				AddBrick(CollisionShapeType.Cone, Colors.Brown, new Vector3D(10d, 20d, 0d), 200d, new Point3D(0, 0, 10), new DoubleVector(0, 0, 1, -1, 0, 0));

				BuildDenseTower(25d);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnTowerDense_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearProjectilesAndExplosions();
				ClearBricks();

				BuildDenseTower(0d);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnThickWall_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearProjectilesAndExplosions();
				ClearBricks();

				#region Figure out how many bricks

				int xLimit, yLimit, zLimit;

				if (radFew.IsChecked.Value)
				{
					xLimit = 1;
					yLimit = 5;
					zLimit = 10;
				}
				else if (radNormal.IsChecked.Value)
				{
					xLimit = 2;
					yLimit = 10;
					zLimit = 15;
				}
				else if (radMany.IsChecked.Value)
				{
					xLimit = 4;
					yLimit = 20;
					zLimit = 30;
				}
				else if (radExtreme.IsChecked.Value)
				{
					xLimit = 10;
					yLimit = 30;
					zLimit = 35;
				}
				else
				{
					MessageBox.Show("Unknow number of bricks", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				#endregion

				bool isAlt = false;

				for (int x = -xLimit; x <= xLimit + 1; x++)
				{
					isAlt = !isAlt;

					for (int y = -yLimit; y <= yLimit; y += 3)
					{
						for (int z = 0; z <= zLimit; z++)		//	start on the floor
						{
							#region Main Brick

							Color color = ThickWallSprtColor(x, xLimit);
							Vector3D size = new Vector3D(1, 2, 1);

							double offset;
							if (z % 2 == 0)
							{
								offset = isAlt ? -.75d : .75d;
							}
							else
							{
								offset = isAlt ? .75d : -.75d;
							}

							Point3D position = new Point3D(x, y + offset, z + .5d);

							AddBrick(CollisionShapeType.Box, color, size, 1d, position, _defaultDirectionFacing);

							#endregion

							#region End Brick

							if ((y == -yLimit && offset > 0) || (y >= yLimit - 2 && offset < 0))
							{
								color = ThickWallSprtColor(x, xLimit);
								size = new Vector3D(1, .5, 1);

								if (offset < 0)
								{
									offset -= .75d;
								}
								else
								{
									offset += .75d;
								}

								position = new Point3D(x, y - offset, z + .5d);
								AddBrick(CollisionShapeType.Box, color, size, 1d, position, _defaultDirectionFacing);
							}

							#endregion
						}
					}
				}

				_lastTowerHeight = zLimit + .5d;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btn5Walls_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearProjectilesAndExplosions();
				ClearBricks();

				#region Figure out how many bricks

				int yLimit, zLimit;

				if (radFew.IsChecked.Value)
				{
					yLimit = 5;
					zLimit = 10;
				}
				else if (radNormal.IsChecked.Value)
				{
					yLimit = 10;
					zLimit = 15;
				}
				else if (radMany.IsChecked.Value)
				{
					yLimit = 20;
					zLimit = 30;
				}
				else if (radExtreme.IsChecked.Value)
				{
					yLimit = 40;
					zLimit = 60;
				}
				else
				{
					MessageBox.Show("Unknow number of bricks", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				#endregion

				bool isAlt = false;

				for (int x = -18; x <= 18; x += 9)
				{
					isAlt = !isAlt;

					for (int y = -yLimit; y <= yLimit; y += 3)
					{
						for (int z = 0; z <= zLimit; z++)		//	start on the floor
						{
							#region Main Brick

							byte colorIntensity = Convert.ToByte(StaticRandom.Next(114, 143));
							Color color = Color.FromArgb(255, colorIntensity, colorIntensity, colorIntensity);
							Vector3D size = new Vector3D(1, 2, 1);

							double offset;
							if (z % 2 == 0)		//	every other layer is shifted (and the alt walls are shifted opposite)
							{
								offset = isAlt ? -.75d : .75d;
							}
							else
							{
								offset = isAlt ? .75d : -.75d;
							}

							Point3D position = new Point3D(x, y + offset, z + .5d);

							AddBrick(CollisionShapeType.Box, color, size, 1d, position, _defaultDirectionFacing);

							#endregion

							#region End Brick

							if ((y == -yLimit && offset > 0) || (y >= yLimit - 2 && offset < 0))
							{
								colorIntensity = Convert.ToByte(StaticRandom.Next(114, 143));
								color = Color.FromArgb(255, colorIntensity, colorIntensity, colorIntensity);
								size = new Vector3D(1, .5, 1);

								if (offset < 0)
								{
									offset -= .75d;
								}
								else
								{
									offset += .75d;
								}

								position = new Point3D(x, y - offset, z + .5d);
								AddBrick(CollisionShapeType.Box, color, size, 1d, position, _defaultDirectionFacing);
							}

							#endregion
						}
					}
				}

				_lastTowerHeight = zLimit + .5d;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btn3Walls_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ClearProjectilesAndExplosions();
				ClearBricks();

				#region Figure out how many bricks

				int xLimit = 12;
				int yLimit, zLimit;

				if (radFew.IsChecked.Value)
				{
					yLimit = 5;
					zLimit = 10;
				}
				else if (radNormal.IsChecked.Value)
				{
					yLimit = 10;
					zLimit = 15;
				}
				else if (radMany.IsChecked.Value)
				{
					yLimit = 20;
					zLimit = 30;
				}
				else if (radExtreme.IsChecked.Value)
				{
					yLimit = 40;
					zLimit = 60;
				}
				else
				{
					MessageBox.Show("Unknow number of bricks", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				#endregion

				byte colorMinGray = 64;
				byte colorMaxGray = 96;
				byte colorMinHue = 128;
				byte colorMaxHue = 160;

				for (int x = -xLimit; x <= xLimit; x += xLimit)		//	leave gaps between walls
				{
					for (int y = -yLimit; y <= yLimit; y++)
					{
						for (int z = 0; z <= zLimit; z++)		//	start on the floor
						{
							#region Color

							Color color;
							if (x < 0)
							{
								//	Red
								color = UtilityWPF.GetRandomColor(255, colorMinHue, colorMaxHue, colorMinGray, colorMaxGray, colorMinGray, colorMaxGray);
							}
							else if (x == 0)
							{
								//	Green
								color = UtilityWPF.GetRandomColor(255, colorMinGray, colorMaxGray, colorMinHue, colorMaxHue, colorMinGray, colorMaxGray);
							}
							else
							{
								//	Blue
								color = UtilityWPF.GetRandomColor(255, colorMinGray, colorMaxGray, colorMinGray, colorMaxGray, colorMinHue, colorMaxHue);
							}

							#endregion

							Vector3D size = new Vector3D(1, 1, 1);
							Point3D position = new Point3D(x, y, z + .5d);

							AddBrick(CollisionShapeType.Box, color, size, 1d, position, _defaultDirectionFacing);
						}
					}
				}

				_lastTowerHeight = zLimit + .5d;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#endregion

		#region Private Methods

		private Point3D GetCannonPosition()
		{
			return new Point3D(-35, 0, 4.1);
		}
		private Vector3D GetCannonVelocity()
		{
			return new Vector3D(trkBulletSpeed.Value,
												Math3D.GetNearZeroValue(8),
												2 + (StaticRandom.NextDouble() * 5d));

		}
		private void AddCannonBall(CollisionShapeType shape, Color color, Vector3D size, double mass, Point3D position, Vector3D velocity, Vector3D angularVelocity, DoubleVector directionFacing)
		{
			//	Get the wpf model
			CollisionHull hull;
			Transform3DGroup transform;
			Quaternion rotation;
			DiffuseMaterial bodyMaterial;
			ModelVisual3D model = GetWPFModel(out hull, out transform, out rotation, out bodyMaterial, shape, color, Colors.Gray, 50d, size, position, directionFacing, true);

			//	Add to the viewport
			_viewport.Children.Add(model);

			//	Make a physics body that represents this shape
			Body body = new Body(hull, transform.Value, mass, new Visual3D[] { model });
			body.IsContinuousCollision = true;
			body.MaterialGroupID = _material_Projectile;
			body.Velocity = velocity;
			//body.AngularVelocity = Math3D.RotateAroundAxis(angularVelocity, axis, radians);
			body.AngularVelocity = rotation.GetRotatedVector(angularVelocity);
			body.AngularDamping = new Vector3D(0, 0, 0);		//	this one becomes very noticable with the big spinner

			body.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);

			_projectiles.Add(body);
			_bodyMaterials.Add(body, new BodyMaterial(bodyMaterial, color, size.Length));
		}

		private void AddBrick(CollisionShapeType shape, Color color, Vector3D size, double mass, Point3D position, DoubleVector directionFacing)
		{
			//	Get the wpf model
			CollisionHull hull;
			Transform3DGroup transform;
			Quaternion rotation;
			DiffuseMaterial bodyMaterial;
			ModelVisual3D model = GetWPFModel(out hull, out transform, out rotation, out bodyMaterial, shape, color, Colors.White, 100d, size, position, directionFacing, true);

			//	Add to the viewport
			_viewport.Children.Add(model);

			//	Make a physics body that represents this shape
			Body body = new Body(hull, transform.Value, 1d, new Visual3D[] { model });
			body.MaterialGroupID = _material_Brick;

			body.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);

			_bricks.Add(body);
			_bodyMaterials.Add(body, new BodyMaterial(bodyMaterial, color, size.Length));
		}

		private ModelVisual3D GetWPFModel(out CollisionHull hull, out Transform3DGroup transform, out Quaternion rotation, out DiffuseMaterial bodyMaterial, CollisionShapeType shape, Color color, Color reflectionColor, double reflectionIntensity, Vector3D size, Point3D position, DoubleVector directionFacing, bool createHull)
		{
			//	Material
			MaterialGroup materials = new MaterialGroup();
			bodyMaterial = new DiffuseMaterial(new SolidColorBrush(color));
			materials.Children.Add(bodyMaterial);
			materials.Children.Add(new SpecularMaterial(new SolidColorBrush(reflectionColor), reflectionIntensity));

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;

			hull = null;

			switch (shape)
			{
				case CollisionShapeType.Box:
					Vector3D halfSize = size / 2d;
					geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfSize.X, -halfSize.Y, -halfSize.Z), new Point3D(halfSize.X, halfSize.Y, halfSize.Z));
					if (createHull)
					{
						hull = CollisionHull.CreateBox(_world, 0, size, null);
					}
					break;

				case CollisionShapeType.Sphere:
					geometry.Geometry = UtilityWPF.GetSphere(5, size.X, size.Y, size.Z);
					if (createHull)
					{
						hull = CollisionHull.CreateSphere(_world, 0, size, null);
					}
					break;

				case CollisionShapeType.Cylinder:
					geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, size.X, size.Y);
					if (createHull)
					{
						hull = CollisionHull.CreateCylinder(_world, 0, size.X, size.Y, null);
					}
					break;

				case CollisionShapeType.Cone:
					geometry.Geometry = UtilityWPF.GetCone_AlongX(20, size.X, size.Y);
					if (createHull)
					{
						hull = CollisionHull.CreateCone(_world, 0, size.X, size.Y, null);
					}
					break;

				default:
					throw new ApplicationException("Unexpected CollisionShapeType: " + shape.ToString());
			}

			//	Transform
			transform = new Transform3DGroup();		//	rotate needs to be added before translate



			//rotation = _defaultDirectionFacing.GetAngleAroundAxis(directionFacing);		//	can't use double vector, it over rotates (not anymore, but this is still isn't rotating correctly)

			rotation = Math3D.GetRotation(_defaultDirectionFacing.Standard, directionFacing.Standard);



			transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
			transform.Children.Add(new TranslateTransform3D(position.ToVector()));

			//	Model Visual
			ModelVisual3D retVal = new ModelVisual3D();
			retVal.Content = geometry;
			retVal.Transform = transform;

			//	Exit Function
			return retVal;
		}

		private void ClearProjectilesAndExplosions()
		{
			_projectilesLitFuse.Clear();

			#region Explosions

			foreach (ExplosionWithVisual explosion in _explosions)
			{
				explosion.Dispose();

				explosion.Body.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
				explosion.Body.Dispose();
			}

			_explosions.Clear();

			#endregion

			#region Projectiles

			foreach (Body body in _projectiles)
			{
				_bodyMaterials.Remove(body);
				foreach (Visual3D visual in body.Visuals)
				{
					_viewport.Children.Remove(visual);
				}
				body.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
				body.Dispose();
			}

			_projectiles.Clear();

			#endregion
		}
		private void ClearBricks()
		{
			List<Body> bricks = new List<Body>(_bricks);
			foreach (Body brick in bricks)
			{
				RemoveBrick(brick);
			}

			//_bricks.Clear();		
		}
		private bool RemoveBrick(Body brick)
		{
			if (!_bricks.Contains(brick))
			{
				return false;
			}

			_bodyMaterials.Remove(brick);

			foreach (Visual3D visual in brick.Visuals)
			{
				_viewport.Children.Remove(visual);
			}

			brick.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);

			brick.Dispose();

			_bricks.Remove(brick);

			return true;
		}

		private void BuildSpike(Point3D startPoint, bool isOneOfMany)
		{
			#region Figure out how many bricks

			double zLimit, zStep;

			if (radFew.IsChecked.Value)
			{
				zLimit = 30d;
				zStep = isOneOfMany ? 3d : 1d;
			}
			else if (radNormal.IsChecked.Value)
			{
				zLimit = 50d;
				zStep = isOneOfMany ? 2.5d : .5d;
			}
			else if (radMany.IsChecked.Value)
			{
				zLimit = 60d;
				zStep = isOneOfMany ? 1.5d : .33d;
			}
			else if (radExtreme.IsChecked.Value)
			{
				zLimit = 100d;
				zStep = isOneOfMany ? 1d : .25d;
			}
			else
			{
				MessageBox.Show("Unknow number of bricks", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			#endregion

			DoubleVector dirFacingColumn = new DoubleVector(0, 0, 1, -1, 0, 0);		//	x becomes z

			Color baseColor = UtilityWPF.GetRandomColor(255, 64, 192);

			for (double z = 0d; z <= zLimit; z += zStep)		//	start on the floor
			{
				Color color = UtilityWPF.AlphaBlend(Colors.Black, baseColor, UtilityHelper.GetScaledValue_Capped(0d, .5d, 0, zLimit, zLimit - z));

				double radius = UtilityHelper.GetScaledValue_Capped(.25d, 2d, 0d, zLimit, zLimit - z);
				Vector3D size = new Vector3D(radius, zStep, 0d);		//	x is radius, y is height

				double mass = radius * 2d;

				Point3D position = new Point3D(startPoint.X, startPoint.Y, startPoint.Z + z + (zStep / 2d));

				AddBrick(CollisionShapeType.Cylinder, color, size, mass, position, dirFacingColumn);
			}

			_lastTowerHeight = zLimit + zStep;
		}
		private void BuildDenseTower(double heightOffset)
		{
			#region Figure out how many bricks

			int xLimit, yLimit, zLimit;

			if (radFew.IsChecked.Value)
			{
				xLimit = 1;
				yLimit = 1;
				zLimit = 30;
			}
			else if (radNormal.IsChecked.Value)
			{
				xLimit = 4;
				yLimit = 4;
				zLimit = 50;
			}
			else if (radMany.IsChecked.Value)
			{
				xLimit = 7;
				yLimit = 7;
				zLimit = 70;
			}
			else if (radExtreme.IsChecked.Value)
			{
				xLimit = 10;
				yLimit = 10;
				zLimit = 80;
			}
			else
			{
				MessageBox.Show("Unknow number of bricks", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			#endregion

			for (int x = -xLimit; x <= xLimit; x += 2)		//	leave gaps between walls
			{
				for (int y = -yLimit; y <= yLimit; y += 2)
				{
					for (int z = 0; z <= zLimit; z++)		//	start on the floor
					{
						double brightness = UtilityHelper.GetScaledValue_Capped(30, 185, 0, zLimit, z);
						Color color = UtilityWPF.GetRandomColor(255, Convert.ToByte(brightness - 15), Convert.ToByte(brightness + 15));

						Vector3D size = new Vector3D(1.5d, 1.5d, 1d);
						Point3D position = new Point3D(x, y, z + .5d + heightOffset);

						AddBrick(CollisionShapeType.Box, color, size, 1d, position, _defaultDirectionFacing);
					}
				}
			}

			_lastTowerHeight = zLimit + .5d;
		}

		//NOTE:  This doesn't remove the body from anything, but does add the explosion to _explosions
		private ExplosionWithVisual ExplodeBody(Body body, bool isExplode, double multiplier)
		{
			double explodeStartRadius = 1d;
			if (_bodyMaterials.ContainsKey(body))
			{
				explodeStartRadius = _bodyMaterials[body].OrigSize * .5d;
			}

			//	Remove old visuals
			if (body.Visuals != null)
			{
				_bodyMaterials.Remove(body);

				foreach (Visual3D visual in body.Visuals)
				{
					_viewport.Children.Remove(visual);
				}
				body.Visuals = null;
			}

			//	Turn it into a ghost
			body.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
			body.MaterialGroupID = _material_ExplodingProjectile;		//	the body now won't collide with anything

			//	Create the explosion
			ExplosionWithVisual retVal = null;
			if (isExplode)
			{
				//	Explode
				retVal = new ExplosionWithVisual(body, body.Mass * multiplier * 60d, body.Mass * multiplier * 4000d, body.Mass * multiplier * 16d, _viewport, explodeStartRadius);		//	crude, but I thought I heard something about mass and energy being the same thing  :)
			}
			else
			{
				//	Implode
				retVal = new ExplosionWithVisual(body, body.Mass * multiplier * 60d, body.Mass * multiplier * -3d, body.Mass * multiplier * 16d, _viewport, explodeStartRadius);
			}

			_explosions.Add(retVal);

			//	Exit Function
			return retVal;
		}

		private Color ThickWallSprtColor(int x, int xLimit)
		{
			//	Gray scale
			double brightness = UtilityHelper.GetScaledValue_Capped(30, 185, 2 * xLimit + 2, 0, xLimit + x);
			return UtilityWPF.GetRandomColor(255, Convert.ToByte(brightness - 15), Convert.ToByte(brightness + 15));
		}

		/// <summary>
		/// This isn't meant to run in production code, it's just a way for me to place test dots while writing this
		/// </summary>
		private void AddDebugDot(Point3D position, double radius, Color color)
		{
			//	Material
			MaterialGroup materials = new MaterialGroup();
			materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
			materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;
			geometry.Geometry = UtilityWPF.GetSphere(3, radius, radius, radius);

			//	Model Visual
			ModelVisual3D model = new ModelVisual3D();
			model.Content = geometry;
			model.Transform = new TranslateTransform3D(position.ToVector());

			//	Add to the viewport
			_viewport.Children.Add(model);
		}

		private LeftClickAction GetLeftClickAction()
		{
			if (radLeftShootBall.IsChecked.Value)
			{
				return LeftClickAction.ShootBall;
			}
			else if (radLeftPanCamera.IsChecked.Value)
			{
				return LeftClickAction.PanCamera;
			}
			else if (radLeftRemove.IsChecked.Value)
			{
				if (chkLeftLine.IsChecked.Value)
				{
					return LeftClickAction.RemoveLine;
				}
				else
				{
					return LeftClickAction.Remove;
				}
			}
			else if (radLeftExplode.IsChecked.Value)
			{
				if (chkLeftLine.IsChecked.Value)
				{
					return LeftClickAction.ExplodeLine;
				}
				else
				{
					return LeftClickAction.Explode;
				}
			}
			else if (radLeftImplode.IsChecked.Value)
			{
				if (chkLeftLine.IsChecked.Value)
				{
					return LeftClickAction.ImplodeLine;
				}
				else
				{
					return LeftClickAction.Implode;
				}
			}
			else if (radLeftForceBeam.IsChecked.Value)
			{
				return LeftClickAction.ForceBeam;
			}
			else
			{
				throw new ApplicationException("Unknown left click mode");
			}
		}

		private void FireRay(Point clickPoint)
		{
			//	This is how microsoft recomends doing hit tests, but I don't care about wpf models.  I just want the world coords of the mouse
			//HitTestResult result = VisualTreeHelper.HitTest(grdViewPort, clickPoint, );

			//	Project the mouse click into world coords
			RayHitTestParameters hitParam = UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint);
			_rayPoint = hitParam.Origin;
			_rayDirection = hitParam.Direction;

			if (_leftClickAction == LeftClickAction.ShootBall)
			{
				FireRaySprtShootBall();
				return;
			}

			List<WorldBase.IntersectionPoint> hits = _world.RayCast(_rayPoint, _rayPoint + (_rayDirection.ToUnit() * 1000d));

			switch (_leftClickAction)
			{
				case LeftClickAction.Remove:
					#region Remove

					if (hits.Count > 0)
					{
						RemoveBrick(hits[0].Body);
					}

					#endregion
					break;

				case LeftClickAction.RemoveLine:
					#region RemoveLine

					foreach (WorldBase.IntersectionPoint hit in hits)
					{
						RemoveBrick(hit.Body);
					}

					#endregion
					break;

				case LeftClickAction.Explode:
					#region Explode

					if (hits.Count > 0 && hits[0].Body.MaterialGroupID == _material_Brick)
					{
						ExplodeBody(hits[0].Body, true, trkLeftExplodePower.Value);

						//	Remove from brick list
						_bricks.Remove(hits[0].Body);
					}

					#endregion
					break;

				case LeftClickAction.ExplodeLine:
					#region ExplodeLine

					foreach (WorldBase.IntersectionPoint hit in hits)
					{
						if (hit.Body.MaterialGroupID == _material_Brick)
						{
							ExplodeBody(hit.Body, true, trkLeftExplodePower.Value);

							//	Remove from brick list
							_bricks.Remove(hit.Body);
						}
					}

					#endregion
					break;

				case LeftClickAction.Implode:
					#region Implode

					if (hits.Count > 0 && hits[0].Body.MaterialGroupID == _material_Brick)
					{
						ExplodeBody(hits[0].Body, false, trkLeftExplodePower.Value);

						//	Remove from brick list
						_bricks.Remove(hits[0].Body);
					}

					#endregion
					break;

				case LeftClickAction.ImplodeLine:
					#region ImplodeLine

					foreach (WorldBase.IntersectionPoint hit in hits)
					{
						if (hit.Body.MaterialGroupID == _material_Brick)
						{
							ExplodeBody(hit.Body, false, trkLeftExplodePower.Value);

							//	Remove from brick list
							_bricks.Remove(hit.Body);
						}
					}

					#endregion
					break;

				case LeftClickAction.ForceBeam:
					//	Nothing to do here, it's all in the apply force/torque callback
					break;

				default:
					//	Paint them green for now
					foreach (WorldBase.IntersectionPoint hit in hits)
					{
						if (_bodyMaterials.ContainsKey(hit.Body))		//	the terrain isn't in the list
						{
							_bodyMaterials[hit.Body].Material.Brush = new SolidColorBrush(Colors.Chartreuse);
						}
					}
					break;
			}
		}
		private void FireRaySprtShootBall()
		{
			Vector3D velocity = _rayDirection.ToUnit() * trkBulletSpeed.Value;
			DoubleVector directionFacing = new DoubleVector(velocity, Math3D.GetArbitraryOrhonganal(velocity));

			switch (cboLeftBallType.Text)
			{
				case "Nerf":
					#region Nerf

					AddCannonBall(
						CollisionShapeType.Cylinder,
						Colors.Coral,
						new Vector3D(.75d, 5d, 0d),		//	x is radius, y is height
						.1d,
						_rayPoint,
						velocity,
						new Vector3D(),
						directionFacing);

					#endregion
					break;

				case "Pellet":
					#region Pellet

					AddCannonBall(
						CollisionShapeType.Sphere,
						Colors.SteelBlue,
						new Vector3D(.5d, .5d, .5d),
						2d,
						_rayPoint,
						velocity,
						new Vector3D(),
						directionFacing);

					#endregion
					break;

				case "Slug":
					#region Slug

					AddCannonBall(
						CollisionShapeType.Cylinder,
						Colors.DimGray,
						new Vector3D(.5d, .8d, 0d),		//	x is radius, y is height
						6d,
						_rayPoint,
						velocity,
						new Vector3D(),
						directionFacing);

					#endregion
					break;

				case "Baseball":
					#region Baseball

					AddCannonBall(
						CollisionShapeType.Sphere,
						Colors.GhostWhite,
						new Vector3D(2d, 2d, 2d),
						5d,
						_rayPoint,
						velocity,
						new Vector3D(),
						directionFacing);

					#endregion
					break;

				case "Cannon":
					#region Cannon

					AddCannonBall(
						CollisionShapeType.Sphere,
						Color.FromArgb(255, 45, 45, 32),
						new Vector3D(2d, 2d, 2d),
						15d,
						_rayPoint,
						velocity,
						new Vector3D(),
						directionFacing);

					#endregion
					break;

				case "Wrecker":
					#region Wrecker

					AddCannonBall(
						CollisionShapeType.Sphere,
						Color.FromArgb(255, 50, 15, 15),
						new Vector3D(4d, 4d, 4d),
						60d,
						_rayPoint,
						velocity,
						new Vector3D(),
						directionFacing);

					#endregion
					break;

				case "Shotgun":
					#region Shotgun

					//	Build a transform to go from origin to camera
					DoubleVector originDirFacing = new DoubleVector(0, 0, 1, 0, 1, 0);
					Quaternion rotationToCameraRay = originDirFacing.GetAngleAroundAxis(new DoubleVector( _rayDirection, Math3D.GetArbitraryOrhonganal(_rayDirection)));
					Transform3DGroup transformToCamera = new Transform3DGroup();
					transformToCamera.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotationToCameraRay)));
					transformToCamera.Children.Add(new TranslateTransform3D(_rayPoint.ToVector()));

					for (int cntr = 0; cntr < 10; cntr++)
					{
						Vector3D pos2D = Math3D.GetRandomVectorSpherical2D(1d);
						//Vector3D vel2D = new Vector3D(pos2D.X, pos2D.Y, trkBulletSpeed.Value * 2d);


						//Point3D position = _rayPoint;
						velocity = _rayDirection.ToUnit() * trkBulletSpeed.Value * 2d;		//TODO:  Diverge the shot

						Point3D position = transformToCamera.Transform(pos2D.ToPoint());
						//velocity = transformToCamera.Transform(vel2D);


						directionFacing = new DoubleVector(velocity, Math3D.GetArbitraryOrhonganal(velocity));

						AddCannonBall(
							CollisionShapeType.Sphere,
							Color.FromRgb(25, 25, 25),
							new Vector3D(.1d, .1d, .1d),
							.2d,
							position,
							velocity,
							new Vector3D(),
							directionFacing);

					}

					#endregion
					break;

				default:
					MessageBox.Show("Unknown ball type: " + cboLeftBallType.Text, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
			}


		}

		#endregion
	}
}

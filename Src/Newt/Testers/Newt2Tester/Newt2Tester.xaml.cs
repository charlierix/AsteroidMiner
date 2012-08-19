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
using System.Windows.Threading;

using Game.Newt.HelperClasses;
using Game.Newt.HelperClasses.Primitives3D;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.Testers.Newt2Tester
{
	public partial class Newt2Tester : Window
	{
		#region Declaration Section

		private const double CREATEOBJECTBOUNDRY = 17;

		private World _world = null;

		private List<Body[]> _bodySets = new List<Body[]>();
		private SortedList<Body, List<FluidHull>> _fluidHulls = new SortedList<Body, List<FluidHull>>();

		/// <summary>
		/// This listens to the mouse/keyboard and controls the camera
		/// </summary>
		private TrackBallRoam _trackball = null;

		/// <summary>
		/// These are the lines that show the boundries.  Whenever the camera moves, they need to recalculate
		/// so they appear as a 2D line on the screen (they are actually 3D meshes)
		/// </summary>
		private List<ScreenSpaceLines3D> _lines = new List<ScreenSpaceLines3D>();

		//	I add temp visuals to this list, and when the timer fires, they clear up
		DispatcherTimer _debugVisualTimer = null;
		private List<Visual3D> _debugVisuals = new List<Visual3D>();

		private FluidEmulationArgs _fluidEmulation = null;

		private ApplyVectorFieldArgs _vectorField = null;

		private BodyAttractionArgs _bodyAttraction = null;
		/// <summary>
		/// These are the forces that get applied to the bodies once per frame (due to gravity)
		/// </summary>
		private SortedList<Body, Vector3D> _bodyAttractionForces = new SortedList<Body, Vector3D>();

		#endregion

		#region Constructor

		public Newt2Tester()
		{
			InitializeComponent();

			_debugVisualTimer = new DispatcherTimer();
			_debugVisualTimer.Interval = new TimeSpan(0, 0, 10);
			_debugVisualTimer.IsEnabled = false;
			_debugVisualTimer.Tick += new EventHandler(debugVisualTimer_Tick);
		}

		#endregion

		#region Event Listeners

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				//	Init World
				_world = new World();
				_world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);
				_world.UnPause();

				//	Trackball
				_trackball = new TrackBallRoam(_camera);
				_trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
				_trackball.AllowZoomOnMouseWheel = true;
				_trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
				//_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
				_trackball.ShouldHitTestOnOrbit = true;

				SetCollisionBoundry();
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
				//NOTE:  If there are two windows running, this will kill the other
				_world.Pause();
				_fluidHulls.Clear();
				ObjectStorage.Instance.ClearAll();
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
			try
			{
				if (_bodyAttraction != null)
				{
					#region Body/Body Attraction

					_bodyAttractionForces.Clear();

					//	Init the forces list
					for (int cntr = 0; cntr < _bodySets.Count; cntr++)
					{
						foreach (Body body in _bodySets[cntr])
						{
							_bodyAttractionForces.Add(body, new Vector3D());
						}
					}

					//	Apply Gravity (bodies within the same set aren't compared to each other)
					for (int outerSetCntr = 0; outerSetCntr < _bodySets.Count - 1; outerSetCntr++)
					{
						for (int outerCntr = 0; outerCntr < _bodySets[outerSetCntr].Length; outerCntr++)
						{
							Body bodyOuter = _bodySets[outerSetCntr][outerCntr];
							Point3D position1 = bodyOuter.Position;

							for (int innerSetCntr = outerSetCntr + 1; innerSetCntr < _bodySets.Count; innerSetCntr++)
							{
								for (int innerCntr = 0; innerCntr < _bodySets[innerSetCntr].Length; innerCntr++)
								{
									Body bodyInner = _bodySets[innerSetCntr][innerCntr];
									Point3D position2 = bodyInner.Position;

									#region Apply Gravity

									Vector3D gravityLink = position1 - position2;

									//	Calculate Force
									double force;
									switch (_bodyAttraction.AttractionType)
									{
										case BodyAttractionType.Constant:
											#region Constant

											force = _bodyAttraction.Strength;

											#endregion
											break;

										case BodyAttractionType.Spring:
											#region Spring

											force = _bodyAttraction.Strength * gravityLink.Length;

											#endregion
											break;

										case BodyAttractionType.SpringInverseDist:
											#region SpringInverseDist

											//	Force should be max when distance is zero, and linearly drop off to nothing
											double inverseDistance = _bodyAttraction.Distance - gravityLink.Length;
											if (inverseDistance > 0d)
											{
												force = _bodyAttraction.Strength * inverseDistance;
											}
											else
											{
												force = 0d;
											}

											#endregion
											break;

										case BodyAttractionType.SpringDesiredDistance:
											#region SpringDesiredDistance

											//TODO:  Come up with a better name.  This is attracted to a point along a ring around the body

											//	If inside the ring, it is attracted to the closer part of the ring (directly behind it, actually away from the other body)
											force = _bodyAttraction.Strength * (gravityLink.Length - _bodyAttraction.Distance);

											#endregion
											break;

										case BodyAttractionType.Gravity:
											#region Gravity

											force = _bodyAttraction.Strength * (bodyOuter.Mass * bodyInner.Mass) / gravityLink.LengthSquared;

											#endregion
											break;

										case BodyAttractionType.Tangent:
											#region Tangent

											//	At a distance of _bodyAttraction.Distance, the tangent should have gone through one cycle (pi)
											//force = Math.Tan((gravityLink.Length / _bodyAttraction.Distance) * Math.PI);
											force = Math.Tan(gravityLink.Length * Math.PI / _bodyAttraction.Distance);
											//force = Math.Sin(gravityLink.Length * Math.PI / _bodyAttraction.Distance);

											#endregion
											break;

										default:
											throw new ApplicationException("Unknown BodyAttractionType: " + _bodyAttraction.AttractionType.ToString());
									}

									if (!_bodyAttraction.IsToward)
									{
										force *= -1;
									}

									if (!double.IsNaN(force) && !double.IsInfinity(force))
									{
										gravityLink.Normalize();
										gravityLink *= force;

										_bodyAttractionForces[bodyInner] += gravityLink;
										_bodyAttractionForces[bodyOuter] -= gravityLink;
									}

									#endregion
								}
							}

						}
					}

					#endregion
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void Body_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
		{
			try
			{
				if (_vectorField != null)
				{
					#region Vector Field

					//	Field Type
					Vector3D direction;
					switch (_vectorField.FieldType)
					{
						case VectorFieldType.Inward:
							direction = e.Body.Position.ToVector().ToUnit() * _vectorField.Strength * -1d;
							break;

						case VectorFieldType.Outward:
							direction = e.Body.Position.ToVector().ToUnit() * _vectorField.Strength;
							break;

						case VectorFieldType.SwirlInward:
							direction = e.Body.Position.ToVector() * -1;
							direction.Normalize();
							direction = direction.GetRotatedVector(new Vector3D(0, 0, 1), 10d);
							direction *= _vectorField.Strength;
							break;

						case VectorFieldType.Swirl:
							direction = e.Body.Position.ToVector() * -1;
							direction.Z = 0;
							direction.Normalize();
							direction = direction.GetRotatedVector(new Vector3D(0, 0, 1), 90d);
							direction *= _vectorField.Strength;
							break;

						case VectorFieldType.Z0Plane:
							direction = e.Body.Position.ToVector();
							direction.X = 0;
							direction.Y = 0;
							if (direction.Z > 0)
							{
								direction.Z = -_vectorField.Strength;
							}
							else
							{
								direction.Z = _vectorField.Strength;
							}
							break;

						default:		//	not looking for none, the class should be null when none
							throw new ApplicationException("Unexpected VectorFieldType: " + _vectorField.FieldType.ToString());
					}

					//	Apply Force/Acceleration
					if (_vectorField.UseForce)
					{
						e.Body.AddForce(direction);
					}
					else
					{
						e.Body.AddForce(direction * e.Body.Mass);
					}

					#endregion
				}

				//	Apply Gravity
				if (_bodyAttractionForces.ContainsKey(e.Body))
				{
					e.Body.AddForce(_bodyAttractionForces[e.Body]);
				}

				if (_fluidHulls.ContainsKey(e.Body))
				{
					foreach (FluidHull hull in _fluidHulls[e.Body])
					{
						hull.Transform = new MatrixTransform3D(e.Body.OffsetMatrix);
						hull.Update();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void grdViewPort_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			try
			{
				RecalculateLineGeometries();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void Camera_Changed(object sender, EventArgs e)
		{
			try
			{
				RecalculateLineGeometries();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void addSimple1_AddBody(object sender, AddBodyArgs e)
		{
			try
			{
				#region WPF Model (plus collision hull)

				//	Material
				MaterialGroup materials = new MaterialGroup();
				materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.GetRandomColor(255, 64, 192))));
				materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));

				//	Geometry Model
				GeometryModel3D geometry = new GeometryModel3D();
				geometry.Material = materials;
				geometry.BackMaterial = materials;

				CollisionHull hull = null;
				switch (e.CollisionShape)
				{
					case CollisionShapeType.Box:
						Vector3D halfSize = e.Size / 2d;
						geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfSize.X, -halfSize.Y, -halfSize.Z), new Point3D(halfSize.X, halfSize.Y, halfSize.Z));
						hull = CollisionHull.CreateBox(_world, 0, e.Size, null);
						break;

					case CollisionShapeType.Sphere:
						geometry.Geometry = UtilityWPF.GetSphere(5, e.Size.X, e.Size.Y, e.Size.Z);
						hull = CollisionHull.CreateSphere(_world, 0, e.Size, null);
						break;

					case CollisionShapeType.Cylinder:
						geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, e.Radius, e.Height);
						hull = CollisionHull.CreateCylinder(_world, 0, e.Radius, e.Height, null);
						break;

					case CollisionShapeType.Cone:
						geometry.Geometry = UtilityWPF.GetCone_AlongX(20, e.Radius, e.Height);
						hull = CollisionHull.CreateCone(_world, 0, e.Radius, e.Height, null);
						break;

					case CollisionShapeType.Capsule:
					case CollisionShapeType.ChamferCylinder:
						MessageBox.Show("finish this");
						return;

					default:
						throw new ApplicationException("Unknown ConvexBody3D.CollisionShape: " + e.CollisionShape.ToString());
				}

				//	Transform
				Transform3DGroup transform = new Transform3DGroup();		//	rotate needs to be added before translate
				transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(Math3D.GetRandomVectorSpherical(10), Math3D.GetNearZeroValue(360d))));
				transform.Children.Add(new TranslateTransform3D(Math3D.GetRandomVectorSpherical(CREATEOBJECTBOUNDRY)));

				//	Model Visual
				ModelVisual3D model = new ModelVisual3D();
				model.Content = geometry;
				model.Transform = transform;

				//	Add to the viewport
				_viewport.Children.Add(model);

				#endregion

				#region Physics Body

				//	Make a physics body that represents this shape
				Body body = new Body(hull, transform.Value, e.Mass, new Visual3D[] { model });
				body.Velocity = Math3D.GetRandomVectorSpherical2D(1d);

				//body.LinearDamping = .01f;
				//body.AngularDamping = new Vector3D(.01f, .01f, .01f);

				body.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);

				Body[] bodySet = new Body[] { body };
				_bodySets.Add(bodySet);

				#endregion

				BodiesAdded(bodySet);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void addJoined1_AddJoint(object sender, AddJoinedBodiesArgs e)
		{
			try
			{
				//	Figure out the centerpoint
				Point3D centerPoint = Math3D.GetRandomVectorSpherical(CREATEOBJECTBOUNDRY).ToPoint();

				//	Get a random rotation
				//TODO:  Figure out why it fails when I give it a rotation
				//Quaternion finalRotation = new Quaternion(Math3D.GetRandomVectorSpherical(_rand, 10), Math3D.GetNearZeroValue(_rand, 360d));
				Quaternion finalRotation = new Quaternion(new Vector3D(0, 0, 1), 0);

				//	All bodies will be the same color
				Color color = UtilityWPF.GetRandomColor(255, 64, 192);

				Body[] bodies = null;

				#region Get Bodies

				switch (e.JointType)
				{
					case AddJointType.BallAndSocket:
					case AddJointType.Hinge:
					case AddJointType.Slider:
					case AddJointType.Corkscrew:
					case AddJointType.UniversalJoint:
						bodies = new Body[2];
						GetJointBodyPair(out bodies[0], out bodies[1], e.Body1Type, e.Body2Type, centerPoint, finalRotation, e.SeparationDistance, color);
						break;

					case AddJointType.UpVector:
					case AddJointType.Multi_BallAndChain:
					case AddJointType.Multi_Tetrahedron:
						MessageBox.Show("finish this");
						return;

					default:
						throw new ApplicationException("Unknown AddJointType: " + e.JointType.ToString());
				}

				#endregion

				_bodySets.Add(bodies);

				#region Setup Joint

				Vector3D directionAlong = finalRotation.GetRotatedVector(new Vector3D(1, 0, 0));
				Vector3D directionOrth1 = finalRotation.GetRotatedVector(new Vector3D(0, 1, 0));
				Vector3D directionOrth2 = finalRotation.GetRotatedVector(new Vector3D(0, 0, 1));

				switch (e.JointType)
				{
					case AddJointType.BallAndSocket:
						#region BallAndSocket

						JointBallAndSocket ballAndSocket = JointBallAndSocket.CreateBallAndSocket(_world, centerPoint, bodies[0], bodies[1]);
						ballAndSocket.ShouldLinkedBodiesCollideEachOther = true;

						//TODO:  Let the user define these limits
						//ballAndSocket.SetConeLimits();

						#endregion
						break;

					case AddJointType.Hinge:
						#region Hinge

						JointHinge hinge = JointHinge.CreateHinge(_world, centerPoint, directionOrth1, bodies[0], bodies[1]);
						hinge.ShouldLinkedBodiesCollideEachOther = true;

						#endregion
						break;

					case AddJointType.Slider:
						#region Slider

						JointSlider slider = JointSlider.CreateSlider(_world, centerPoint, directionAlong, bodies[0], bodies[1]);
						slider.ShouldLinkedBodiesCollideEachOther = true;

						#endregion
						break;

					case AddJointType.Corkscrew:
						#region Corkscrew

						JointCorkscrew corkscrew = JointCorkscrew.CreateCorkscrew(_world, centerPoint, directionAlong, bodies[0], bodies[1]);
						corkscrew.ShouldLinkedBodiesCollideEachOther = true;

						#endregion
						break;

					case AddJointType.UniversalJoint:
						#region UniversalJoint

						JointUniversal uJoint = JointUniversal.CreateUniversal(_world, centerPoint, directionOrth1, directionOrth2, bodies[0], bodies[1]);
						uJoint.ShouldLinkedBodiesCollideEachOther = true;

						#endregion
						break;

					default:
						throw new ApplicationException("Unexpected AddJointType: " + e.JointType.ToString());
				}

				#endregion

				BodiesAdded(bodies);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void fluidEmulation1_ValueChanged(object sender, FluidEmulationArgs e)
		{
			try
			{
				_fluidEmulation = e;

				//	Add/Remove fluid
				if (_fluidEmulation.EmulateFluid)
				{
					if (_fluidHulls.Count == 0)		//	if it's nonzero, then they already have a fluid hull assigned to them
					{
						foreach (Body[] bodySet in _bodySets)
						{
							foreach (Body body in bodySet)
							{
								ApplyFluidHull(body);
							}
						}
					}
				}
				else
				{
					_fluidHulls.Clear();
				}

				//	Set viscosity
				foreach (List<FluidHull> hulls in _fluidHulls.Values)
				{
					foreach (FluidHull hull in hulls)
					{
						hull.FluidViscosity = _fluidEmulation.Viscosity;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void setVelocities1_Stop(object sender, EventArgs e)
		{
			try
			{
				foreach (Body[] set in _bodySets)
				{
					foreach (Body body in set)
					{
						body.Velocity = new Vector3D();
						body.AngularVelocity = new Vector3D();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void setVelocities1_WhackEm(object sender, SetVelocityArgs e)
		{
			try
			{
				foreach (Body[] set in _bodySets)
				{
					foreach (Body body in set)
					{
						if (e.TranslationSpeed > 0d)
						{
							#region Translate

							//	Figure out direction (not worried about length)
							Vector3D direction;
							switch (e.Direction)
							{
								case SetVelocityDirection.Random:
									direction = Math3D.GetRandomVectorSpherical(1d);
									break;

								case SetVelocityDirection.FromCenter:
									direction = body.Position.ToVector();
									break;

								case SetVelocityDirection.TowardCenter:
									direction = -body.Position.ToVector();
									break;

								default:
									throw new ApplicationException("Unknown SetVelocityDirection: " + e.Direction.ToString());
							}

							//	Set length to 1
							direction.Normalize();
							direction *= e.TranslationSpeed;

							//	Apply velocity
							if (e.OverwriteCurrentVelocity)
							{
								body.Velocity = direction;
							}
							else
							{
								body.Velocity += direction;
							}

							#endregion
						}

						if (e.RotationSpeed > 0d)
						{
							#region Rotate

							//	Figure out axis (not worried about length)
							Vector3D axis;
							switch (e.Direction)
							{
								case SetVelocityDirection.Random:
								case SetVelocityDirection.FromCenter:		//	just letting all rotations be random
								case SetVelocityDirection.TowardCenter:
									axis = Math3D.GetRandomVectorSpherical(1d);
									break;

								//case SetVelocityDirection.FromCenter:
								//TODO:  Make the axis at a right angle from the this direction (but with a consistent "up")
								//axis = body.Position.ToVector();
								//break;

								//case SetVelocityDirection.TowardCenter:
								//axis = -body.Position.ToVector();
								//break;

								default:
									throw new ApplicationException("Unknown SetVelocityDirection: " + e.Direction.ToString());
							}

							axis.Normalize();
							axis *= e.RotationSpeed;

							if (e.OverwriteCurrentVelocity)
							{
								body.AngularVelocity = axis;
							}
							else
							{
								body.AngularVelocity += axis;
							}

							#endregion
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void vectorField1_ApplyVectorField(object sender, ApplyVectorFieldArgs e)
		{
			try
			{
				if (e.FieldType == VectorFieldType.None)
				{
					_vectorField = null;
				}
				else
				{
					_vectorField = e;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void bodyAttraction1_AttractionChanged(object sender, BodyAttractionArgs e)
		{
			try
			{
				if (e.AttractionType == BodyAttractionType.None)
				{
					_bodyAttraction = null;
				}
				else
				{
					_bodyAttraction = e;
				}

				_bodyAttractionForces.Clear();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void debugVisualTimer_Tick(object sender, EventArgs e)
		{
			_debugVisualTimer.IsEnabled = false;

			foreach (Visual3D visual in _debugVisuals)
			{
				_viewport.Children.Remove(visual);
			}

			_debugVisuals.Clear();
		}

		private void btnRestart_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("finish this");
		}
		private void btnClear_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_world.Pause();

				//	Remove the visuals (this tester is pretty simple, the only visuals are the ones tied to the bodies)
				foreach (Body[] bodySet in _bodySets)
				{
					foreach (Body body in bodySet)
					{
						if (body.Visuals != null)
						{
							foreach (Visual3D visual in body.Visuals)
							{
								_viewport.Children.Remove(visual);
							}
						}
					}
				}

				_fluidHulls.Clear();

				_bodySets.Clear();

				//	If I don't call this, the bodies will be disposed out from under the world
				_world.ClearCollisionBoundry();

				//	It is dangerous to dispose collision hulls before all bodies are disposed.  Body.GetJoints won't return anything, so I made a method off of
				//	object storage to make it easy to do a wipe
				//TODO:  This is still too heavy handed.  If there are two tester windows open, this will wipe the other window's items too
				ObjectStorage.Instance.ClearAll();

				//	Put the world's collision boundry back
				SetCollisionBoundry();

				_world.UnPause();

				#region Flawed

				//#region Joints

				//List<JointBase> joints = new List<JointBase>();

				//foreach (Body[] bodySet in _bodySets)
				//{
				//    foreach (Body body in bodySet)
				//    {
				//NOTE:  body.GetJoints wasn't working when I tested it
				//        foreach (JointBase joint in body.GetJoints())
				//        {
				//            if (!joints.Contains(joint))
				//            {
				//                joints.Add(joint);
				//            }
				//        }
				//    }
				//}

				//foreach (JointBase joint in joints)
				//{
				//    joint.Dispose();
				//}
				//joints.Clear();

				//#endregion
				//List<CollisionHull> collisionHulls = new List<CollisionHull>();
				//#region Bodies

				//foreach (Body[] bodySet in _bodySets)
				//{
				//    foreach (Body body in bodySet)
				//    {
				//        collisionHulls.Add(body.CollisionHull);

				//        if (body.Visual != null)
				//        {
				//            _viewport.Children.Remove(body.Visual);
				//        }

				//        body.Dispose();
				//    }
				//}

				//_bodySets.Clear();

				//#endregion
				//#region Collision Hulls

				//foreach(CollisionHull hull in collisionHulls)
				//{
				//    hull.Dispose();
				//}

				//collisionHulls.Clear();

				//#endregion

				#endregion
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#endregion

		#region Private Methods

		private void BodiesAdded(Body[] bodySet)
		{
			if (_fluidEmulation != null && _fluidEmulation.EmulateFluid)
			{
				foreach(Body body in bodySet)
				{
					ApplyFluidHull(body);
				}
			}
		}

		private void ApplyFluidHull(Body body)
		{
			if (body.Visuals != null)
			{
				foreach (Visual3D visual in body.Visuals)
				{
					if (visual is ModelVisual3D)
					{
						ModelVisual3D visualCast = (ModelVisual3D)visual;
						if (visualCast.Content is GeometryModel3D)
						{
							GeometryModel3D contentCast = (GeometryModel3D)visualCast.Content;

							if (contentCast.Geometry is MeshGeometry3D)
							{
								FluidHull hull = FluidHull.FromGeometry((MeshGeometry3D)contentCast.Geometry, contentCast.Transform, true);
								hull.FluidViscosity = _fluidEmulation.Viscosity;
								hull.Body = body;

								if (!_fluidHulls.ContainsKey(body))
								{
									_fluidHulls.Add(body, new List<FluidHull>());
								}

								_fluidHulls[body].Add(hull);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// This places a dot in the viewport that will dissapear in a couble seconds
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

			//	Temporarily add to the viewport
			_debugVisuals.Add(model);
			_viewport.Children.Add(model);
			_debugVisualTimer.IsEnabled = true;
		}

		private void RecalculateLineGeometries()
		{
			foreach (ScreenSpaceLines3D line in _lines)
			{
				line.CalculateGeometry();
			}
		}

		private void GetJointBodyPair(out Body body1, out Body body2, AddJointBodyType bodyType1, AddJointBodyType bodyType2, Point3D centerPoint, Quaternion rotation, double separationDistance, Color color)
		{
			#region Body 1

			double distanceToPermiter;
			Quaternion localRotation;
			GetJointBodyPairSprtOffset(out distanceToPermiter, out localRotation, bodyType1);

			Vector3D offset = new Vector3D(distanceToPermiter + (separationDistance / 2d), 0, 0);		//	adding to the separation distance because I don't want centerpoint to centerpoint, I want edge to edge
			offset = rotation.GetRotatedVector(offset);

			Point3D shiftedCenter = centerPoint + offset;


			localRotation = rotation.ToUnit() * localRotation.ToUnit();
			RotateTransform3D finalRotation = new RotateTransform3D(new QuaternionRotation3D(localRotation));


			body1 = GetJointBodyPairSprtBody(GetJointBodyPairSprtHullType(bodyType1), shiftedCenter, finalRotation, color);

			#endregion


			#region Body 2

			GetJointBodyPairSprtOffset(out distanceToPermiter, out localRotation, bodyType2);
			offset = new Vector3D(distanceToPermiter + (separationDistance / 2d), 0, 0);
			offset = rotation.GetRotatedVector(offset);
			shiftedCenter = centerPoint - offset;		//	subtracting instead of adding


			localRotation = new Quaternion(new Vector3D(0, 0, 1), 180d).ToUnit() * rotation.ToUnit() * localRotation.ToUnit();		//	throwing in an extra 180 degrees of spin
			finalRotation = new RotateTransform3D(new QuaternionRotation3D(localRotation));


			body2 = GetJointBodyPairSprtBody(GetJointBodyPairSprtHullType(bodyType2), shiftedCenter, finalRotation, color);

			#endregion
		}
		private static void GetJointBodyPairSprtOffset(out double distance, out Quaternion rotation, AddJointBodyType bodyType)
		{
			switch (bodyType)
			{
				case AddJointBodyType.Box_Corner:
					distance = Math.Sqrt(3d);

					//TODO:  Figure out how to rotate a cube onto its corner
					rotation = new Quaternion(new Vector3D(0, 0, 1), 45d).ToUnit() * new Quaternion(new Vector3D(0, 1, 0), 45d).ToUnit();
					//rotation = new Quaternion(new Vector3D(0, 0, 1), 45d).ToUnit() + new Quaternion(new Vector3D(0, 1, 0), 45d).ToUnit() + new Quaternion(new Vector3D(1, 0, 0), 45d).ToUnit();
					break;

				case AddJointBodyType.Box_Edge:
					distance = Math.Sqrt(2d);
					rotation = new Quaternion(new Vector3D(0, 0, 1), 45d);
					break;

				case AddJointBodyType.Cone_Tip:		//TODO:  Finish these
					distance = 1d;
					rotation = new Quaternion(new Vector3D(0, 0, 1), 180d);
					break;

				case AddJointBodyType.Cylinder_Edge:
					distance = 1d;
					rotation = new Quaternion(new Vector3D(0, 0, 1), 90d);
					break;

				case AddJointBodyType.Box_Face:
				case AddJointBodyType.Cone_Base:
				case AddJointBodyType.Cylinder_Cap:
				case AddJointBodyType.Sphere:
					distance = 1d;
					rotation = new Quaternion(new Vector3D(0, 0, 1), 0d);
					break;

				default:
					throw new ApplicationException("Unknown AddJointBodyType: " + bodyType.ToString());
			}
		}
		private static CollisionShapeType GetJointBodyPairSprtHullType(AddJointBodyType bodyType)
		{
			switch (bodyType)
			{
				case AddJointBodyType.Box_Corner:
				case AddJointBodyType.Box_Edge:
				case AddJointBodyType.Box_Face:
					return CollisionShapeType.Box;

				case AddJointBodyType.Cone_Base:
				case AddJointBodyType.Cone_Tip:
					return CollisionShapeType.Cone;

				case AddJointBodyType.Cylinder_Cap:
				case AddJointBodyType.Cylinder_Edge:
					return CollisionShapeType.Cylinder;

				case AddJointBodyType.Sphere:
					return CollisionShapeType.Sphere;

				default:
					throw new ApplicationException("Unknown AddJointBodyType: " + bodyType.ToString());
			}
		}
		private Body GetJointBodyPairSprtBody(CollisionShapeType bodyType, Point3D centerPoint, RotateTransform3D rotation, Color color)
		{
			#region WPF Model (plus collision hull)

			//	Material
			MaterialGroup materials = new MaterialGroup();
			materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
			materials.Children.Add(new SpecularMaterial(Brushes.White, 100d));

			//	Geometry Model
			GeometryModel3D geometry = new GeometryModel3D();
			geometry.Material = materials;
			geometry.BackMaterial = materials;

			CollisionHull hull = null;
			switch (bodyType)
			{
				case CollisionShapeType.Box:
					geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-1, -1, -1d), new Point3D(1d, 1d, 1d));
					hull = CollisionHull.CreateBox(_world, 0, new Vector3D(2d, 2d, 2d), null);
					break;

				case CollisionShapeType.Sphere:
					geometry.Geometry = UtilityWPF.GetSphere(5, 1d, 1d, 1d);
					hull = CollisionHull.CreateSphere(_world, 0, new Vector3D(1d, 1d, 1d), null);
					break;

				case CollisionShapeType.Cylinder:
					geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, 1d, 2d);
					hull = CollisionHull.CreateCylinder(_world, 0, 1d, 2d, null);
					break;

				case CollisionShapeType.Cone:
					geometry.Geometry = UtilityWPF.GetCone_AlongX(20, 1d, 2d);
					hull = CollisionHull.CreateCone(_world, 0, 1d, 2d, null);
					break;

				case CollisionShapeType.Capsule:
				case CollisionShapeType.ChamferCylinder:
					throw new ApplicationException("finish this");

				default:
					throw new ApplicationException("Unknown ConvexBody3D.CollisionShape: " + bodyType.ToString());
			}

			//	Transform
			Transform3DGroup transform = new Transform3DGroup();		//	rotate needs to be added before translate
			transform.Children.Add(rotation);
			transform.Children.Add(new TranslateTransform3D(centerPoint.ToVector()));

			//	Model Visual
			ModelVisual3D model = new ModelVisual3D();
			model.Content = geometry;
			model.Transform = transform;

			//	Add to the viewport
			_viewport.Children.Add(model);

			#endregion

			#region Physics Body

			//	Make a physics body that represents this shape
			Body body = new Body(hull, transform.Value, 1d, new Visual3D[] { model });		//	being lazy with mass, but since size is fixed, it won't be too noticable
			body.Velocity = Math3D.GetRandomVectorSpherical2D(1d);

			//body.LinearDamping = .01f;
			//body.AngularDamping = new Vector3D(.01f, .01f, .01f);

			body.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);

			//	This will be done later
			//_bodySets.Add(body);

			#endregion

			//	Exit Function
			return body;
		}

		private void SetCollisionBoundry()
		{
			//	Remove existing lines from the viewport
			if (_lines.Count > 0)
			{
				foreach (ScreenSpaceLines3D line in _lines)
				{
					_viewport.Children.Remove(line);
				}
			}

			//	Tell the world about the collision boundry (it will clean itself up if this is a second call)
			List<Point3D[]> innerLines, outerLines;
			_world.SetCollisionBoundry(out innerLines, out outerLines, new Point3D(-100, -100, -100), new Point3D(100, 100, 100));

			//	Draw the lines
			Color lineColor = Color.FromArgb(32, 0, 0, 0);
			foreach (Point3D[] line in innerLines)
			{
				ScreenSpaceLines3D lineModel = new ScreenSpaceLines3D(false);
				lineModel.Thickness = 1d;
				lineModel.Color = lineColor;
				lineModel.AddLine(line[0], line[1]);

				_viewport.Children.Add(lineModel);
				_lines.Add(lineModel);
			}

			RecalculateLineGeometries();
		}

		#endregion
	}
}

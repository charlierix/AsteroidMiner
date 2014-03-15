using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.Testers.Arcanorum
{
    public class BotNPC : Bot
    {
        #region Declaration Section

        private readonly DragHitShape _dragPlane;

        private AIMousePlate _mousePlate = null;

        #endregion

        #region Constructor

        public BotNPC(BotDNA dna, Point3D position, World world, Map map, KeepItems2D keepItems2D, MaterialIDs materialIDs, Viewport3D viewport, EditorOptions editorOptions, ItemOptionsArco itemOptions, IGravityField gravity, DragHitShape dragPlane, bool runNeural, bool repairPartPositions)
            : base(dna, position, world, map, keepItems2D, materialIDs, viewport, editorOptions, itemOptions, gravity, runNeural, repairPartPositions)
        {
            _dragPlane = dragPlane;

            _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);
            this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);

            _mousePlate = new AIMousePlate(_dragPlane);
            _mousePlate.MaxXY = this.Radius * 20;
            _mousePlate.Scale = 1;
            this.Ram.SetAIStuff(_mousePlate);
        }

        #endregion

        #region Overrides

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _world.Updating -= new EventHandler<WorldUpdatingArgs>(World_Updating);
                this.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(Body_ApplyForceAndTorque);
            }

            base.Dispose(disposing);
        }

        #endregion
        #region Event Listeners

        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            this.Update(e.ElapsedTime);

            Point3D position = this.PositionWorld;

            // Make sure the plate is not sitting on the drag plane (this is assuming that the bot IS on the drag plane)
            Vector3D? normal = _dragPlane.GetNormal(position);
            if (normal != null)
            {
                normal = normal.Value.ToUnit();

                _mousePlate.Position = this.PositionWorld + (normal.Value * 10);
                //NOTE: The drag plane will either be a plane or cylinder, so an up vector of 0,1,0 should always work (normal should always be in the xz plane)
                _mousePlate.Look = normal.Value * -1;
            }

            //TODO: Do real AI
            if (StaticRandom.Next(50) == 0)
            {
                _mousePlate.CurrentPoint2D = Math3D.GetRandomVector(_mousePlate.MaxXY).ToPoint2D();
            }

            // Project the 2D point into a real 3D point, and chase after that
            Point3D? chasePoint = _mousePlate.ProjectTo3D();

            chasePoint = AdjustIfOffPlane(position, this.Radius, _dragPlane, chasePoint);

            if (chasePoint != null)
            {
                this.DraggingBot.SetPosition(chasePoint.Value);
            }
        }
        private void Body_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            //NOTE: this.DraggingPlayer is handling the force toward the mouse


            //TODO: Make a class that does this
            Vector3D angularVelocity = this.PhysicsBody.AngularVelocity;

            if (angularVelocity.LengthSquared > 1)
            {
                this.PhysicsBody.AngularVelocity = angularVelocity * .9d;
            }
        }

        #endregion

        #region Private Methods

        //TODO: This functionality should be part of MapObjectChaseVelocity
        private static Point3D? AdjustIfOffPlane(Point3D itemPos, double itemRadius, DragHitShape dragPlane, Point3D? chasePoint)
        {
            const double MAXOFFSET = 1.5;
            const double PERCENTATMAX = .01;        // even if they are really far off the plane, don't completely chase the plane, some percent needs to go toward the chase point passed in

            Point3D? pointOnPlane = dragPlane.CastRay(itemPos);
            if (pointOnPlane == null)
            {
                // Don't know where they are relative to the plane (this should never happen)
                return chasePoint;
            }
            else if (Math3D.IsNearValue(pointOnPlane.Value, itemPos))
            {
                // They're position is the same as the point on the plane (they are already on the plane)
                return chasePoint;
            }

            Vector3D vectToPlane = (itemPos - pointOnPlane.Value);
            double distToPlane = vectToPlane.Length;
            if (distToPlane < itemRadius * .01)
            {
                // They are less than a percent of their body size off the plane, just return the point passed in
                return chasePoint;
            }

            if (chasePoint == null)
            {
                // Null was passed in, just go straight for the plane (this should never happen)
                return pointOnPlane;
            }

            // Figure out how much to dive for the plane vs go for the chase point
            double offset = distToPlane / itemRadius;
            double percent = PERCENTATMAX;
            if (offset < MAXOFFSET)
            {
                // They are less than the max allowable distance from the plane
                percent = UtilityHelper.GetScaledValue_Capped(PERCENTATMAX, 1d, 0, MAXOFFSET, MAXOFFSET - offset);
            }

            Vector3D direction = chasePoint.Value - pointOnPlane.Value;

            return pointOnPlane.Value + (direction * percent);
        }

        #endregion
    }


    // Inputs:
    //  Current Velocity
    //  Weapon Tip Locations
    //  Weapon Angular Velocity
    //
    //  Vision
    //      - have either one or multiple rings
    //      - have it light up for any item, or tuned to certain types
    //
    //  Need a way to see other bot weapon stats (type, position, ang vel)



    // Outputs:
    //  Ring
    //      - simple version will just be a single ring
    //      - more complex could be a ring with a distance modifier (but that would give a lot of power to that modifier's neuron single)
    //      - more complex could be a few rings


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public class CameraHelper
    {
        #region Declaration Section

        private readonly Player _player;
        private readonly PerspectiveCamera _camera;
        private readonly PerspectiveCamera _cameraMap;
        private readonly MinimapHelper _blips;

        //private readonly double _angle = 90d;     // I don't want to allow tilting the camera

        private readonly Vector3D _up = new Vector3D(0, 1, 0);
        private readonly Vector3D _right = new Vector3D(1, 0, 0);		// used to do rotations (orthogonal to look dir and up dir)

        private readonly Vector3D _cameraPosition = new Vector3D(0, 0, 30);     //NOTE: This is multiplied by radius (and inverse of zoom)
        private readonly Vector3D _cameraLookDirection = new Vector3D(0, 0, -10);

        //TODO: Expose these two as public properties
        private double _zoom = 1d;
        private bool _cameraAlwaysLooksUp = false;

        #endregion

        #region Constructor

        public CameraHelper(Player player, PerspectiveCamera camera, PerspectiveCamera cameraMap, MinimapHelper blips)
        {
            _player = player;
            _camera = camera;
            _cameraMap = cameraMap;
            _blips = blips;
        }

        #endregion

        #region Public Methods

        public void Update()
        {
            Body body = _player.Ship.PhysicsBody;

            //Point3D shipPosWorld = body.PositionToWorld(body.CenterOfMass);
            Point3D shipPosWorld = body.Position;

            //TODO:  Eliminate the wobble - may need to have desired properties for the camera, but diffuse that a bit over time (sounds good when I write it, but how)
            // Maybe give the camera mass, and let the change in position be some kind of spring force - however, it is spring forces that clamp the ship into 2D

            Vector3D cameraUp = body.DirectionToWorld(_up);
            Vector3D cameraRight = body.DirectionToWorld(_right);
            Vector3D cameraPos = _cameraPosition * (_player.Ship.Radius * (1d / _zoom));        // multiply by radius so that bigger ships see more
            Vector3D cameraDirFacing = _cameraLookDirection;

            //double actualRotateDegrees = 90d - _angle;		// the camera angle is stored so 90 is looking straight down and 0 is looking straight out, but cameraPos starts looking straight down
            //cameraPos = cameraPos.GetRotatedVector(cameraRight, actualRotateDegrees);
            //cameraUp = cameraUp.GetRotatedVector(cameraRight, actualRotateDegrees);
            //cameraDirFacing = cameraDirFacing.GetRotatedVector(cameraRight, actualRotateDegrees);

            // Camera
            _camera.Position = shipPosWorld + cameraPos;

            _camera.UpDirection = cameraUp;
            //_camera.LookDirection = cameraDirFacing;


            // Camera (minimap)
            _cameraMap.Position = shipPosWorld + new Vector3D(0, 0, 500);

            if (!_cameraAlwaysLooksUp)
            {
                //_cameraMap.UpDirection = body.DirectionToWorld(_upTransformed);
                _cameraMap.UpDirection = cameraUp;
            }

            _blips.OrientUprightBlips(_up, _cameraMap.UpDirection);
        }

        #endregion
    }
}

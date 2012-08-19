using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153
{
    public struct BoundingBox
    {
        private static readonly Vector3D _centerOffset = new Vector3D(0.5, 0.5, 0.5);
        public Point3D Min;
        public Point3D Max;

        public BoundingBox(Point3D min, Point3D max)
        {
            this.Min = min;
            this.Max = max;
        }

        public Point3D CenterPos
        {
            get
            {
                return CenterPosFromFactor(_centerOffset);
            }
        }

        public Vector3D CenterOffset
        {
            get
            {
                return CenterOffsetFromFactor(_centerOffset);
            }
        }

        public Size3D Size
        {
            get
            {
                return new Size3D(
                    Max.X - Min.X,
                    Max.Y - Min.Y,
                    Max.Z - Min.Z);
            }
        }

        public Point3D CenterPosFromFactor(Vector3D offsetFactor)
        {
            return Min + CenterOffsetFromFactor(offsetFactor);
        }

        public Vector3D CenterOffsetFromFactor(Vector3D offsetFactor)
        {
            Size3D size = this.Size;
            return new Vector3D(
                size.X * offsetFactor.X,
                size.Y * offsetFactor.Y,
                size.Z * offsetFactor.Z);
        }

        public void Union(BoundingBox box)
        {
            if (box.Min.X < this.Min.X)
                this.Min.X = box.Min.X;
            if (box.Min.Y < this.Min.Y)
                this.Min.Y = box.Min.Y;
            if (box.Min.Z < this.Min.Z)
                this.Min.Z = box.Min.Z;

            if (box.Max.X > this.Max.X)
                this.Max.X = box.Max.X;
            if (box.Max.Y > this.Max.Y)
                this.Max.Y = box.Max.Y;
            if (box.Max.Z > this.Max.Z)
                this.Max.Z = box.Max.Z;
        }
    }
}

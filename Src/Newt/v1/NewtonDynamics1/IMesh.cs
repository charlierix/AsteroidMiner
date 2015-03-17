using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1
{
    public interface IMesh
    {
        IList<Point3D> GetPoints();
    }
}

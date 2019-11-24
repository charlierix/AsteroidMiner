using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Newt.v2.Arcanorum
{
    public interface IGivesDamage
    {
        DamageProps CalculateDamage(MaterialCollision[] collisions);
    }
}

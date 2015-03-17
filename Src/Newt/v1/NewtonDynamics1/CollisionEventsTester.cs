using System;
using System.Collections.Generic;
using System.Text;

using Game.Newt.v1.NewtonDynamics1.Api;

namespace Game.Newt.v1.NewtonDynamics1
{
    /// <summary>
    /// This is an attempt to get events for when a collision occurs
    /// </summary>
    public class CollisionEventsTester
    {

        private EventHandler<CContactBeginEventArgs> m_ContactBegin;
        private Newton.NewtonContactBegin m_NewtonContactBegin;

        private EventHandler<CContactProcessEventArgs> m_ContactProcess;
        private Newton.NewtonContactProcess m_NewtonContactProcess;

        private EventHandler<CContactEndEventArgs> m_ContactEnd;
        private Newton.NewtonContactEnd m_NewtonContactEnd;


        public void Test1()
        {
            m_NewtonContactBegin = new Newton.NewtonContactBegin(InvokeContactBegin);



        }


        // These get raised when the newton engine raises them.  They then call the virtual methods, which then raise public .net events
        private int InvokeContactBegin(IntPtr pMaterial, IntPtr pNewtonBody0, IntPtr pNewtonBody1)
        {
            //OnContactBegin(new CContactBeginEventArgs(pMaterial, (CBody)CHashTables.Body[pNewtonBody0], (CBody)CHashTables.Body[pNewtonBody1]));

            return 1;
        }

        private int InvokeContactProcess(IntPtr pMaterial, IntPtr pContact)
        {
            //OnContactProcess(new CContactProcessEventArgs(pMaterial, pContact));

            return 1;
        }

        private void InvokeContactEnd(IntPtr pMaterial)
        {
            //OnContactEnd(new CContactEndEventArgs(pMaterial));
        }

    }
}

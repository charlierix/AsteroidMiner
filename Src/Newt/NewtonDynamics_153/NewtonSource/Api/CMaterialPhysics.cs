using System;

namespace Game.Newt.NewtonDynamics_153.Api
{
	public class CMaterialPhysics
	{
		#region Members

		protected CWorld m_World;
		public CWorld World { get { return m_World; } }

		private EventHandler<CContactBeginEventArgs> m_ContactBegin;
		private Newton.NewtonContactBegin m_NewtonContactBegin;

		private EventHandler<CContactProcessEventArgs> m_ContactProcess;
		private Newton.NewtonContactProcess m_NewtonContactProcess;

		private EventHandler<CContactEndEventArgs> m_ContactEnd;
		private Newton.NewtonContactEnd m_NewtonContactEnd;

		private CMaterialPhysicsSpecialEffect m_Effect = new CMaterialPhysicsSpecialEffect();

		#endregion

		#region Constructor

		public CMaterialPhysics(CWorld pWorld)
		{
			m_World = pWorld;
		}

		#endregion

		#region Methods

		public void SetDefaultSoftness(CMaterial pId0, CMaterial pId1, float pSoftness)
		{
			Newton.NewtonMaterialSetDefaultSoftness(m_World.Handle, pId0.ID, pId1.ID, pSoftness);
		}

		public void SetDefaultElasticity(CMaterial pId0, CMaterial pId1, float pElasticCoef)
		{
			Newton.NewtonMaterialSetDefaultElasticity(m_World.Handle, pId0.ID, pId1.ID, pElasticCoef);
		}

		public void SetDefaultCollidable(CMaterial pId0, CMaterial pId1, int pState)
		{
			Newton.NewtonMaterialSetDefaultCollidable(m_World.Handle, pId0.ID, pId1.ID, pState);
		}

		public void SetContinuousCollisionMode(CMaterial pId0, CMaterial pId1, int pState)
		{
			Newton.NewtonMaterialSetContinuousCollisionMode(m_World.Handle, pId0.ID, pId1.ID, pState);
		}

		public void SetDefaultFriction(CMaterial pId0, CMaterial pId1, float pStaticFriction, float pKineticFriction)
		{
			Newton.NewtonMaterialSetDefaultFriction(m_World.Handle, pId0.ID, pId1.ID, pStaticFriction, pKineticFriction);
		}

		public CMaterialPhysicsSpecialEffect GetSpecialEffect(CMaterial pId0, CMaterial pId1)
		{
			IntPtr aKey = Newton.NewtonMaterialGetUserData(m_World.Handle, pId0.ID, pId1.ID);

			return (CMaterialPhysicsSpecialEffect)CHashTables.MaterialsUserData[aKey];
		}

		public void SetCollisionCallback(CMaterial pId0, CMaterial pId1, CMaterialPhysicsSpecialEffect pSpecialEffect, EventHandler<CContactBeginEventArgs> pContactBegin, EventHandler<CContactProcessEventArgs> pContactProcess, EventHandler<CContactEndEventArgs> pContactEnd)
		{
            IntPtr aKey = IntPtr.Zero;
            if (pSpecialEffect != null)
            {
                aKey = (IntPtr)pSpecialEffect.GetHashCode();
                CHashTables.MaterialsUserData.Add(aKey, pSpecialEffect);
            }

            // Takes in a delegate, stores it, but then hooks 
			m_ContactBegin = pContactBegin;
			m_NewtonContactBegin = new Newton.NewtonContactBegin(InvokeContactBegin);

			m_ContactProcess = pContactProcess;
			m_NewtonContactProcess = new Newton.NewtonContactProcess(InvokeContactProcess);

			m_ContactEnd = pContactEnd;
			m_NewtonContactEnd = new Newton.NewtonContactEnd(InvokeContactEnd);

			Newton.NewtonMaterialSetCollisionCallback(m_World.Handle, pId0.ID, pId1.ID, aKey, m_NewtonContactBegin, m_NewtonContactProcess, m_NewtonContactEnd);
		}

		#endregion

		#region Invokes

		private int InvokeContactBegin(IntPtr pMaterial, IntPtr pNewtonBody0, IntPtr pNewtonBody1)
		{
            if (OnContactBegin(new CContactBeginEventArgs(pMaterial, (CBody)CHashTables.Body[pNewtonBody0], (CBody)CHashTables.Body[pNewtonBody1])))
            {
                return 1;
            }
            else
            {
                return 0;
            }
		}

		private int InvokeContactProcess(IntPtr pMaterial, IntPtr pContact)
		{
            if (OnContactProcess(new CContactProcessEventArgs(pMaterial, pContact)))
            {
                return 1;
            }
            else
            {
                return 0;
            }
		}

		private void InvokeContactEnd(IntPtr pMaterial)
		{
			OnContactEnd(new CContactEndEventArgs(pMaterial));
		}

		#endregion

		#region Virtuals

		protected virtual bool OnContactBegin(CContactBeginEventArgs pEventArgs)
		{
			if (m_ContactBegin != null)
			{
				m_ContactBegin(this, pEventArgs);
			}

            return pEventArgs.AllowCollision;
		}

		protected virtual bool OnContactProcess(CContactProcessEventArgs pEventArgs)
		{
			if (m_ContactProcess != null)
			{
				m_ContactProcess(this, pEventArgs);
			}

            return pEventArgs.AllowCollision;
		}

		protected virtual void OnContactEnd(CContactEndEventArgs pEventArgs)
		{
			if (m_ContactEnd != null)
			{
				m_ContactEnd(this, pEventArgs);
			}
		}

		#endregion
	}
}












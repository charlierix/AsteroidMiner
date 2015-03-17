using System;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
	public class CContact
	{
		#region Members

		protected IntPtr m_Handle;
		internal IntPtr Handle { get { return m_Handle; } }

		#endregion


		#region Constructor

		public CContact(IntPtr pHandle)
		{
			m_Handle = pHandle;
		}

		#endregion
	}
}

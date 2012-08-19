using System;

namespace Game.Newt.NewtonDynamics_153.Api
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

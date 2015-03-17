using System;
using System.Windows.Media.Media3D;
using System.Collections.Generic;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
	public class CCollisionTree : CCollision
	{
		private int _faceCount;

		#region Constructor

		public CCollisionTree(CWorld pWorld)
			: base(pWorld)
		{
		}

		#endregion

		#region Events

		private EventHandler<CSerializeEventArgs> m_Serialize;
		private Newton.NewtonSerialize m_NewtonSerialize;

		private EventHandler<CTreeCollisionEventArgs> m_TreeCollision;
		private Newton.NewtonTreeCollision m_NewtonTreeCollision;

		private EventHandler<CDeserializeEventArgs> m_Deserialize;
		private Newton.NewtonDeserialize m_NewtonDeserialize;

		#endregion

		#region Methods

		public void CreateCollisionTree(EventHandler<CTreeCollisionEventArgs> pTreeCollision)
		{
			m_TreeCollision = pTreeCollision;
			m_NewtonTreeCollision = new Newton.NewtonTreeCollision(InvokeTreeCollision);
			m_Handle = Newton.NewtonCreateTreeCollision(m_World.Handle, m_NewtonTreeCollision);
		}
		public void CreateCollisionTree(IList<Point3D> points, int pointsPerFace, bool optimise)
		{
			m_Handle = Newton.NewtonCreateTreeCollision(m_World.Handle, null);
			TreeBeginBuild();
			TreeAddFaces(points, pointsPerFace);
			TreeEndBuild(optimise);
		}

		public void CreateCollisionTree()
		{
			m_Handle = Newton.NewtonCreateTreeCollision(m_World.Handle, null);
		}
		public void TreeBeginBuild()
		{
			_faceCount = 0;
			Newton.NewtonTreeCollisionBeginBuild(m_Handle);
		}
		public void TreeAddFaces(IList<Point3D> points, int pointsPerFace)
		{
			if (pointsPerFace < 3)
				throw new ArgumentException("A face has to have at least 3 points.", "pointsPerFace");

			Point3D[] facePoints = new Point3D[pointsPerFace];
			for (int i = 0, count = points.Count - (pointsPerFace - 1); i < count; )
			{
				for (int f = 0; f < pointsPerFace; i++, f++)
					facePoints[f] = points[i];
				TreeAddFace(facePoints);
			}
		}
		public void TreeAddFace(Point3D[] facePoints)
		{
			float[] aVertices = new float[facePoints.Length * 3];

			for (int i = 0, count = facePoints.Length, p = 0; p < count; p++)
			{
				aVertices[i++] = (float)facePoints[p].X;
				aVertices[i++] = (float)facePoints[p].Y;
				aVertices[i++] = (float)facePoints[p].Z;
			}

			Newton.NewtonTreeCollisionAddFace(m_Handle,
				facePoints.Length,
				aVertices,
				sizeof(float) * 3,
				0);

			_faceCount++;
		}
		public void TreeEndBuild(bool pOptimize)
		{
			Newton.NewtonTreeCollisionEndBuild(m_Handle, Convert.ToInt32(pOptimize));
		}

		public void TreeSerialize(EventHandler<CSerializeEventArgs> pSerializeFunction, int pSerializeHandle)
		{
			m_Serialize = pSerializeFunction;
			m_NewtonSerialize = new Newton.NewtonSerialize(InvokeTreeCollisionSerialize);

			Newton.NewtonTreeCollisionSerialize(m_Handle,
					m_NewtonSerialize,
					pSerializeHandle);
		}

		public int CreateTreeFromSerialization(EventHandler<CTreeCollisionEventArgs> pTreeCollision, EventHandler<CDeserializeEventArgs> pDeserializeFunction, int pSerializeHandle)
		{
			m_TreeCollision = pTreeCollision;
			m_NewtonTreeCollision = new Newton.NewtonTreeCollision(InvokeTreeCollision);

			m_Deserialize = pDeserializeFunction;
			m_NewtonDeserialize = new Newton.NewtonDeserialize(InvokeTreeCollisionDeserialize);

			return Newton.NewtonCreateTreeCollisionFromSerialization(m_World.Handle,
					m_NewtonTreeCollision,
					m_NewtonDeserialize,
					pSerializeHandle);
		}

		#endregion

		#region Invokes

		private void InvokeTreeCollision(IntPtr pNewtonBodyWithTreeCollision, IntPtr pNewtonBody, float[] pVertex, int pVertexstrideInBytes, int IndexCount, int[] pIndexArray)
		{
			OnTreeCollision(new CTreeCollisionEventArgs(
				(CBody)CHashTables.BodyUserData[pNewtonBody],
				new NewtonVector3(pVertex).ToDirectX(),
				pVertexstrideInBytes,
				IndexCount,
				pIndexArray));
		}

		private void InvokeTreeCollisionDeserialize(IntPtr pSerializeHandle, IntPtr pBuffer, uint pSize)
		{
			OnTreeCollisionDeserialize(new CDeserializeEventArgs((IntPtr)pSerializeHandle,
				pBuffer,
				pSize));
		}

		private void InvokeTreeCollisionSerialize(IntPtr pSerializeHandle, IntPtr pBuffer, uint pSize)
		{
			OnTreeCollisionSerialize(new CSerializeEventArgs((IntPtr)pSerializeHandle,
				pBuffer,
				pSize));
		}

		#endregion

		#region Virtuals

		protected virtual void OnTreeCollision(CTreeCollisionEventArgs pEventArgs)
		{
			if (m_TreeCollision != null)
			{
				m_TreeCollision(this, pEventArgs);
			}
		}

		protected virtual void OnTreeCollisionSerialize(CSerializeEventArgs pEventArgs)
		{
			if (m_Serialize != null)
			{
				m_Serialize(this, pEventArgs);
			}
		}

		protected virtual void OnTreeCollisionDeserialize(CDeserializeEventArgs pEventArgs)
		{
			if (m_Deserialize != null)
			{
				m_Deserialize(this, pEventArgs);
			}
		}

		#endregion

		/*
				internal static extern int NewtonTreeCollisionGetFaceAtribute(IntPtr pNewtonTreeCollision,
					[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]int[] pFaceIndexArray);
				internal static extern int NewtonTreeCollisionGetFaceAtribute(IntPtr pNewtonTreeCollision,
					[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]int[] pFaceIndexArray);

				internal static extern void NewtonTreeCollisionSetFaceAtribute(IntPtr pNewtonTreeCollision,
					[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]int[] pFaceIndexArray,
					int pAttribute);
				internal static extern void NewtonTreeCollisionSetFaceAtribute(IntPtr pNewtonTreeCollision,
					[MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]int[] pFaceIndexArray,
					int pAttribute);
		*/
	}
}

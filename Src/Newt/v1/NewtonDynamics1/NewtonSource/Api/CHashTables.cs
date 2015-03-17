using System.Collections;

namespace Game.Newt.v1.NewtonDynamics1.Api
{
    public class CHashTables
    {
        #region Members

        private static Hashtable s_Body = new Hashtable();
        private static Hashtable s_Collision = new Hashtable();
        private static Hashtable s_Material = new Hashtable();
        private static Hashtable s_Contact = new Hashtable();
        private static Hashtable s_Joint = new Hashtable();
        private static Hashtable s_Tire = new Hashtable();

        private static Hashtable s_BodyUserData = new Hashtable();
        private static Hashtable s_MaterialUserData = new Hashtable();
        private static Hashtable s_MaterialsUserData = new Hashtable();
        private static Hashtable s_JointUserData = new Hashtable();
        private static Hashtable s_TireUserData = new Hashtable();
        private static Hashtable s_BoneUserData = new Hashtable();

        #endregion

        #region Properties

        public static Hashtable Body
        {
            get
            {
                return s_Body;
            }
        }

        public static Hashtable Collision
        {
            get
            {
                return s_Collision;
            }
        }

        public static Hashtable Material
        {
            get
            {
                return s_Material;
            }
        }

        public static Hashtable Contact
        {
            get
            {
                return s_Contact;
            }
        }

        public static Hashtable Joint
        {
            get
            {
                return s_Joint;
            }
        }

        public static Hashtable Tire
        {
            get
            {
                return s_Tire;
            }
        }

        public static Hashtable BodyUserData
        {
            get
            {
                return s_BodyUserData;
            }
        }

        public static Hashtable MaterialUserData
        {
            get
            {
                return s_MaterialUserData;
            }
        }

        public static Hashtable MaterialsUserData
        {
            get
            {
                return s_MaterialsUserData;
            }
        }

        public static Hashtable JointUserData
        {
            get
            {
                return s_JointUserData;
            }
        }

        public static Hashtable TireUserData
        {
            get
            {
                return s_TireUserData;
            }
        }

        public static Hashtable BoneUserData
        {
            get
            {
                return s_BoneUserData;
            }
        }

        #endregion

        #region Methods

        // 1.53 isn't thread safe anyway, so I'll just follow suit  :)
        public static void Clear()
        {
            s_Body.Clear();
            s_Collision.Clear();
            s_Material.Clear();
            s_Contact.Clear();
            s_Joint.Clear();
            s_Tire.Clear();

            s_BodyUserData.Clear();
            s_MaterialUserData.Clear();
            s_MaterialsUserData.Clear();
            s_JointUserData.Clear();
            s_TireUserData.Clear();
            s_BoneUserData.Clear();
        }

        #endregion
    }
}

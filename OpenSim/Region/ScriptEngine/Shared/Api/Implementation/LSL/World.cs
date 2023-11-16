using System;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api: MarshalByRefObject, ILSL_Api, IScriptApi
    {
        
        public void llRezAtRoot(string inventory, LSL_Vector pos, LSL_Vector vel, LSL_Rotation rot, int param)
        {
            doObjectRez(inventory, pos, vel, rot, param, true);
        }


        public void llRezObject(string inventory, LSL_Vector pos, LSL_Vector vel, LSL_Rotation rot, int param)
        {
            doObjectRez(inventory, pos, vel, rot, param, false);
        }
        
        
        public void llLookAt(LSL_Vector target, double strength, double damping)
        {

            // Get the normalized vector to the target
            LSL_Vector from = llGetPos();

            // normalized direction to target
            LSL_Vector dir = llVecNorm(target - from);

            // use vertical to help compute left axis
//            LSL_Vector up = new LSL_Vector(0.0, 0.0, 1.0);
            // find normalized left axis parallel to horizon
//            LSL_Vector left = llVecNorm(LSL_Vector.Cross(up, dir));

            LSL_Vector left = new LSL_Vector(-dir.y, dir.x, 0.0f);
            left = llVecNorm(left);
            // make up orthogonal to left and dir
            LSL_Vector up  = LSL_Vector.Cross(dir, left);

            // compute rotation based on orthogonal axes
            // and rotate so Z points to target with X below horizont
            LSL_Rotation rot = new LSL_Rotation(0.0, 0.707107, 0.0, 0.707107) * llAxes2Rot(dir, left, up);

            SceneObjectGroup sog = m_host.ParentGroup;
            if(sog == null || sog.IsDeleted)
                return;

            if (!sog.UsesPhysics || sog.IsAttachment)
            {
                // Do nothing if either value is 0 (this has been checked in SL)
                if (strength <= 0.0 || damping <= 0.0)
                    return;

                llSetLocalRot(rot);
            }
            else
            {
                if (strength == 0)
                {
                    llSetLocalRot(rot);
                    return;
                }

                sog.StartLookAt(rot, (float)strength, (float)damping);
            }
        }

        public void llStopLookAt()
        {
            m_host.StopLookAt();
        }
        
        
        public void llCollisionFilter(LSL_String name, LSL_Key id, LSL_Integer accept)
        {
            UUID.TryParse(id, out UUID objectID);
            if(objectID.IsZero())
                m_host.SetCollisionFilter(accept != 0, name.m_string.ToLower(System.Globalization.CultureInfo.InvariantCulture), string.Empty);
            else
                m_host.SetCollisionFilter(accept != 0, name.m_string.ToLower(System.Globalization.CultureInfo.InvariantCulture), objectID.ToString());
        }

    }
}
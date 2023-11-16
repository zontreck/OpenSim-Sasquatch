using System;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    /**
     * These functions remain purely because it would break things if they were removed. In-world deprecation notices are instead provided. all functionality is removed
     */
    public partial class LSL_Api: MarshalByRefObject, ILSL_Api, IScriptApi
    {
        

        public void llMakeExplosion(int particles, double scale, double vel, double lifetime, double arc, string texture, LSL_Vector offset)
        {
            Deprecated("llMakeExplosion", "Use llParticleSystem instead");
            ScriptSleep(m_sleepMsOnMakeExplosion);
        }

        public void llMakeFountain(int particles, double scale, double vel, double lifetime, double arc, int bounce, string texture, LSL_Vector offset, double bounce_offset)
        {
            Deprecated("llMakeFountain", "Use llParticleSystem instead");
            ScriptSleep(m_sleepMsOnMakeFountain);
        }

        public void llMakeSmoke(int particles, double scale, double vel, double lifetime, double arc, string texture, LSL_Vector offset)
        {
            Deprecated("llMakeSmoke", "Use llParticleSystem instead");
            ScriptSleep(m_sleepMsOnMakeSmoke);
        }

        public void llMakeFire(int particles, double scale, double vel, double lifetime, double arc, string texture, LSL_Vector offset)
        {
            Deprecated("llMakeFire", "Use llParticleSystem instead");
            ScriptSleep(m_sleepMsOnMakeFire);
        }

        public void llTakeCamera(string avatar)
        {
            Deprecated("llTakeCamera", "Use llSetCameraParams instead");
        }

        public void llReleaseCamera(string avatar)
        {
            Deprecated("llReleaseCamera", "Use llClearCameraParams instead");
        }
    }
}
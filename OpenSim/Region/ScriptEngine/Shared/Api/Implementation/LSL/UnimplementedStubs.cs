using System;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api : MarshalByRefObject, ILSL_Api, IScriptApi
    {
        public void llStopPointAt()
        {
        }

        public void llPointAt(LSL_Vector pos)
        {
        }


        public LSL_Float llGetEnergy()
        {
            // TODO: figure out real energy value
            return 1.0f;
        }
    }
}
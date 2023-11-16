using System;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api : MarshalByRefObject, ILSL_Api, IScriptApi
    {
        public void llLinkParticleSystem(int linknumber, LSL_Types.list rules)
        {
            var parts = GetLinkParts(linknumber);

            foreach (var part in parts) SetParticleSystem(part, rules, "llLinkParticleSystem");
        }

        public void llParticleSystem(LSL_Types.list rules)
        {
            SetParticleSystem(m_host, rules, "llParticleSystem");
        }
    }
}
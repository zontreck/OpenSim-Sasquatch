using System;
using System.Diagnostics;
using OpenMetaverse;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api : MarshalByRefObject, ILSL_Api, IScriptApi
    {
        /// <summary>
        ///     Reset the named script. The script must be present
        ///     in the same prim.
        /// </summary>
        [DebuggerNonUserCode]
        public void llResetScript()
        {
            // We need to tell the URL module, if we hav one, to release
            // the allocated URLs
            if (m_UrlModule != null)
                m_UrlModule.ScriptRemoved(m_item.ItemID);

            m_ScriptEngine.ApiResetScript(m_item.ItemID);
        }

        public void llResetOtherScript(string name)
        {
            UUID item = GetScriptByName(name);

            if (item.IsZero())
            {
                Error("llResetOtherScript", "Can't find script '" + name + "'");
                return;
            }

            if (item.Equals(m_item.ItemID))
                llResetScript();
            else
                m_ScriptEngine.ResetScript(item);
        }

        public void llSetScriptState(string name, int run)
        {
            UUID item = GetScriptByName(name);


            // These functions are supposed to be robust,
            // so get the state one step at a time.

            if (!item.IsZero())
                m_ScriptEngine.SetScriptState(item, run == 0 ? false : true, item.Equals(m_item.ItemID));
            else
                Error("llSetScriptState", "Can't find script '" + name + "'");
        }


        public void llSetTimerEvent(double sec)
        {
            if (sec != 0.0 && sec < m_MinTimerInterval)
                sec = m_MinTimerInterval;
            // Setting timer repeat
            m_AsyncCommands.TimerPlugin.SetTimerEvent(m_host.LocalId, m_item.ItemID, sec);
        }

        public virtual void llSleep(double sec)
        {
//            m_log.Info("llSleep snoozing " + sec + "s.");

            Sleep((int)(sec * 1000));
        }


        public void llMessageLinked(int linknumber, int num, string msg, string id)
        {
            var parts = GetLinkParts(linknumber);

            UUID partItemID;
            foreach (var part in parts)
            foreach (var item in part.Inventory.GetInventoryItems())
                if (item.Type == ScriptBaseClass.INVENTORY_SCRIPT)
                {
                    partItemID = item.ItemID;
                    var linkNumber = m_host.LinkNum;
                    if (m_host.ParentGroup.PrimCount == 1)
                        linkNumber = 0;

                    object[] resobj =
                    {
                        new LSL_Types.LSLInteger(linkNumber), new LSL_Types.LSLInteger(num),
                        new LSL_Types.LSLString(msg), new LSL_Types.LSLString(id)
                    };

                    m_ScriptEngine.PostScriptEvent(partItemID,
                        new EventParams("link_message",
                            resobj, new DetectParams[0]));
                }
        }


        public LSL_Integer llGetScriptState(string name)
        {
            UUID item = GetScriptByName(name);

            if (!item.IsZero()) return m_ScriptEngine.GetScriptState(item) ? 1 : 0;

            Error("llGetScriptState", "Can't find script '" + name + "'");

            // If we didn't find it, then it's safe to
            // assume it is not running.

            return 0;
        }
    }
}
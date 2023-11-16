using System;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api: MarshalByRefObject, ILSL_Api, IScriptApi
    {
        
        public void llWhisper(int channelID, string text)
        {
            byte[] binText = Utils.StringToBytesNoTerm(text, 1023);
            World.SimChat(binText,
                          ChatTypeEnum.Whisper, channelID, m_host.AbsolutePosition, m_host.Name, m_host.UUID, false);

            IWorldComm wComm = m_ScriptEngine.World.RequestModuleInterface<IWorldComm>();
            if (wComm != null)
                wComm.DeliverMessage(ChatTypeEnum.Whisper, channelID, m_host.Name, m_host.UUID, Util.UTF8.GetString(binText), m_host.AbsolutePosition);
        }


        public void llSay(int channelID, string text)
        {

            if (channelID == 0)
//                m_SayShoutCount++;
                CheckSayShoutTime();

            if (m_SayShoutCount >= 11)
                ScriptSleep(2000);

            if (m_scriptConsoleChannelEnabled && (channelID == m_scriptConsoleChannel))
            {
                Console.WriteLine(text);
            }
            else
            {
                byte[] binText = Utils.StringToBytesNoTerm(text, 1023);
                World.SimChat(binText,
                              ChatTypeEnum.Say, channelID, m_host.AbsolutePosition, m_host.Name, m_host.UUID, false);

                IWorldComm wComm = m_ScriptEngine.World.RequestModuleInterface<IWorldComm>();
                if (wComm != null)
                    wComm.DeliverMessage(ChatTypeEnum.Say, channelID, m_host.Name, m_host.UUID, Util.UTF8.GetString(binText), m_host.AbsolutePosition);
            }
        }

        public void llShout(int channelID, string text)
        {

            if (channelID == 0)
//                m_SayShoutCount++;
                CheckSayShoutTime();

            if (m_SayShoutCount >= 11)
                ScriptSleep(2000);

            byte[] binText = Utils.StringToBytesNoTerm(text, 1023);

            World.SimChat(binText,
                          ChatTypeEnum.Shout, channelID, m_host.AbsolutePosition, m_host.Name, m_host.UUID, true);

            IWorldComm wComm = m_ScriptEngine.World.RequestModuleInterface<IWorldComm>();
            if (wComm != null)
                wComm.DeliverMessage(ChatTypeEnum.Shout, channelID, m_host.Name, m_host.UUID, Util.UTF8.GetString(binText), m_host.AbsolutePosition);
        }

        public void llRegionSay(int channelID, string text)
        {
            if (channelID == 0)
            {
                Error("llRegionSay", "Cannot use on channel 0");
                return;
            }

            byte[] binText = Utils.StringToBytesNoTerm(text, 1023);

            // debug channel is also sent to avatars
            if (channelID == ScriptBaseClass.DEBUG_CHANNEL)
            {
                World.SimChat(binText,
                    ChatTypeEnum.Shout, channelID, m_host.ParentGroup.RootPart.AbsolutePosition, m_host.Name, m_host.UUID, true);
            }

            IWorldComm wComm = m_ScriptEngine.World.RequestModuleInterface<IWorldComm>();
            if (wComm != null)
                wComm.DeliverMessage(ChatTypeEnum.Region, channelID, m_host.Name, m_host.UUID, Util.UTF8.GetString(binText));
        }

        public void  llRegionSayTo(string target, int channel, string msg)
        {
            if (channel == ScriptBaseClass.DEBUG_CHANNEL)
                return;

            if(UUID.TryParse(target, out UUID TargetID) && TargetID.IsNotZero())
            {
                IWorldComm wComm = m_ScriptEngine.World.RequestModuleInterface<IWorldComm>();
                if (wComm != null)
                {
                    if (msg.Length > 1023)
                        msg = msg.Substring(0, 1023);

                    wComm.DeliverMessageTo(TargetID, channel, m_host.AbsolutePosition, m_host.Name, m_host.UUID, msg);
                }
            }
        }
    }
}
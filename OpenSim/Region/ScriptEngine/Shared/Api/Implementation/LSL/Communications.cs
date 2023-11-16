using System;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;
using OpenSim.Services.Interfaces;

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
        
        public void llInstantMessage(string userKey, string message)
        {
            if (m_TransferModule == null || String.IsNullOrEmpty(message))
                return;

           if (!UUID.TryParse(userKey, out UUID userID) || userID.IsZero())
            {
                Error("llInstantMessage","An invalid key  was passed to llInstantMessage");
                ScriptSleep(2000);
                return;
            }

            Vector3 pos = m_host.AbsolutePosition;
            GridInstantMessage msg = new GridInstantMessage
            {
                fromAgentID = m_host.OwnerID.Guid,
                toAgentID = userID.Guid,
                imSessionID = m_host.UUID.Guid, // This is the item we're mucking with here
                timestamp = (uint)Util.UnixTimeSinceEpoch(),
                fromAgentName = m_host.Name, //client.FirstName + " " + client.LastName;// fromAgentName;
                dialog = 19, // MessageFromObject
                fromGroup = false,
                offline = 0,
                ParentEstateID = World.RegionInfo.EstateSettings.EstateID,
                Position = pos,
                RegionID = World.RegionInfo.RegionID.Guid,
                message = (message.Length > 1024) ? message.Substring(0, 1024) : message,
                binaryBucket = Util.StringToBytes256("{0}/{1}/{2}/{3}", m_regionName, (int)pos.X, (int)pos.Y, (int)pos.Z)
            };

            m_TransferModule?.SendInstantMessage(msg, delegate(bool success) {});
            ScriptSleep(m_sleepMsOnInstantMessage);
      }

        public void llEmail(string address, string subject, string message)
        {
            if (m_emailModule == null)
            {
                Error("llEmail", "Email module not configured");
                return;
            }

            // this is a fire and forget no event is sent to script
            Action<string> act = eventID =>
            {
                //Restrict email destination to the avatars registered email address?
                //The restriction only applies if the destination address is not local.
                if (m_restrictEmail == true && address.Contains(m_internalObjectHost) == false)
                {
                    UserAccount account = m_userAccountService.GetUserAccount(RegionScopeID, m_host.OwnerID);
                    if (account == null)
                        return;

                    if (String.IsNullOrEmpty(account.Email))
                        return;
                    address = account.Email;
                }

                m_emailModule.SendEmail(m_host.UUID, m_host.ParentGroup.OwnerID, address, subject, message);
                // no dataserver event
            };

            m_AsyncCommands.DataserverPlugin.RegisterRequest(m_host.LocalId,
                                                     m_item.ItemID, act);
            ScriptSleep(m_sleepMsOnEmail);
        }

        public void llGetNextEmail(string address, string subject)
        {
            if (m_emailModule == null)
            {
                Error("llGetNextEmail", "Email module not configured");
                return;
            }
            Email email;

            email = m_emailModule.GetNextEmail(m_host.UUID, address, subject);

            if (email == null)
                return;

            m_ScriptEngine.PostObjectEvent(m_host.LocalId,
                    new EventParams("email",
                    new Object[] {
                        new LSL_String(email.time),
                        new LSL_String(email.sender),
                        new LSL_String(email.subject),
                        new LSL_String(email.message),
                        new LSL_Integer(email.numLeft)},
                    new DetectParams[0]));

        }

        public void llTargetedEmail(LSL_Integer target, LSL_String subject, LSL_String message)
        {

            SceneObjectGroup parent = m_host.ParentGroup;
            if (parent == null || parent.IsDeleted)
                return;

            if (m_emailModule == null)
            {
                Error("llTargetedEmail", "Email module not configured");
                return;
            }

            if (subject.Length + message.Length > 4096)
            {
                Error("llTargetedEmail", "Message is too large");
                return;
            }

            // this is a fire and forget no event is sent to script
            Action<string> act = eventID =>
            {
                UserAccount account = null;
                if (target == ScriptBaseClass.TARGETED_EMAIL_OBJECT_OWNER)
                {
                    if(parent.OwnerID.Equals(parent.GroupID))
                        return;
                    account = m_userAccountService.GetUserAccount(RegionScopeID, parent.OwnerID);
                }
                else if (target == ScriptBaseClass.TARGETED_EMAIL_ROOT_CREATOR)
                {
                    // non standard avoid creator spam
                    if(m_item.CreatorID.Equals(parent.RootPart.CreatorID))
                    {
                        account = m_userAccountService.GetUserAccount(RegionScopeID, parent.RootPart.CreatorID);
                    }
                    else
                        return;
                }
                else
                    return;

                if (account == null)
                {
                    return;
                }

                string address = account.Email;
                if (String.IsNullOrEmpty(address))
                {
                    return;
                }

                m_emailModule.SendEmail(m_host.UUID, m_host.ParentGroup.OwnerID, address, subject, message);
            };

            m_AsyncCommands.DataserverPlugin.RegisterRequest(m_host.LocalId,
                                                     m_item.ItemID, act);
            ScriptSleep(m_sleepMsOnEmail);
        }

        
    }
}
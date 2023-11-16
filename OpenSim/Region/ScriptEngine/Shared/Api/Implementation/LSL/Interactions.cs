using System;
using System.Diagnostics;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api: MarshalByRefObject, ILSL_Api, IScriptApi
    {
        

        public LSL_Integer llListen(int channelID, string name, string ID, string msg)
        {
            IWorldComm wComm = m_ScriptEngine.World.RequestModuleInterface<IWorldComm>();
            if (wComm == null)
                return -1;

            UUID.TryParse(ID, out UUID keyID);
            return wComm.Listen(m_item.ItemID, m_host.UUID, channelID, name, keyID, msg);
        }

        public void llListenControl(int number, int active)
        {
            IWorldComm wComm = m_ScriptEngine.World.RequestModuleInterface<IWorldComm>();
            if (wComm != null)
                wComm.ListenControl(m_item.ItemID, number, active);
        }

        public void llListenRemove(int number)
        {
            IWorldComm wComm = m_ScriptEngine.World.RequestModuleInterface<IWorldComm>();
            if (wComm != null)
                wComm.ListenRemove(m_item.ItemID, number);
        }
        
        

        public void llSensor(string name, string id, int type, double range, double arc)
        {
            UUID.TryParse(id, out UUID keyID);
            m_AsyncCommands.SensorRepeatPlugin.SenseOnce(m_host.LocalId, m_item.ItemID, name, keyID, type, range, arc, m_host);
       }

        public void llSensorRepeat(string name, string id, int type, double range, double arc, double rate)
        {
            UUID.TryParse(id, out UUID keyID);
            m_AsyncCommands.SensorRepeatPlugin.SetSenseRepeatEvent(m_host.LocalId, m_item.ItemID, name, keyID, type, range, arc, rate, m_host);
        }

        public void llSensorRemove()
        {
            m_AsyncCommands.SensorRepeatPlugin.UnSetSenseRepeaterEvents(m_host.LocalId, m_item.ItemID);
        }

        public LSL_String llDetectedName(int number)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, number);
            if (detectedParams == null)
                return String.Empty;
            return detectedParams.Name;
        }

        public LSL_Key llDetectedKey(int number)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, number);
            if (detectedParams == null)
                return String.Empty;
            return detectedParams.Key.ToString();
        }

        public LSL_Key llDetectedOwner(int number)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, number);
            if (detectedParams == null)
                return String.Empty;
            return detectedParams.Owner.ToString();
        }

        public LSL_Integer llDetectedType(int number)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, number);
            if (detectedParams == null)
                return 0;
            return new LSL_Integer(detectedParams.Type);
        }

        public LSL_Vector llDetectedPos(int number)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, number);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.Position;
        }

        public LSL_Vector llDetectedVel(int number)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, number);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.Velocity;
        }

        public LSL_Vector llDetectedGrab(int number)
        {
            DetectParams parms = m_ScriptEngine.GetDetectParams(m_item.ItemID, number);
            if (parms == null)
                return new LSL_Vector(0, 0, 0);

            return parms.OffsetPos;
        }

        public LSL_Rotation llDetectedRot(int number)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, number);
            if (detectedParams == null)
                return new LSL_Rotation();
            return detectedParams.Rotation;
        }

        public LSL_Integer llDetectedGroup(int number)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, number);
            if (detectedParams == null)
                return new LSL_Integer(0);
            if (m_host.GroupID.Equals(detectedParams.Group))
                return new LSL_Integer(1);
            return new LSL_Integer(0);
        }

        public LSL_Integer llDetectedLinkNumber(int number)
        {
            DetectParams parms = m_ScriptEngine.GetDetectParams(m_item.ItemID, number);
            if (parms == null)
                return new LSL_Integer(0);

            return new LSL_Integer(parms.LinkNum);
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchBinormal for details
        /// </summary>
        public LSL_Vector llDetectedTouchBinormal(int index)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, index);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.TouchBinormal;
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchFace for details
        /// </summary>
        public LSL_Integer llDetectedTouchFace(int index)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, index);
            if (detectedParams == null)
                return new LSL_Integer(-1);
            return new LSL_Integer(detectedParams.TouchFace);
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchNormal for details
        /// </summary>
        public LSL_Vector llDetectedTouchNormal(int index)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, index);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.TouchNormal;
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchPos for details
        /// </summary>
        public LSL_Vector llDetectedTouchPos(int index)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, index);
            if (detectedParams == null)
                return new LSL_Vector();
            return detectedParams.TouchPos;
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchST for details
        /// </summary>
        public LSL_Vector llDetectedTouchST(int index)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, index);
            if (detectedParams == null)
                return new LSL_Vector(-1.0, -1.0, 0.0);
            return detectedParams.TouchST;
        }

        /// <summary>
        /// See http://wiki.secondlife.com/wiki/LlDetectedTouchUV for details
        /// </summary>
        public LSL_Vector llDetectedTouchUV(int index)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, index);
            if (detectedParams == null)
                return new LSL_Vector(-1.0, -1.0, 0.0);
            return detectedParams.TouchUV;
        }
        
        
        public void llTakeControls(int controls, int accept, int pass_on)
        {
            if (!m_item.PermsGranter.IsZero())
            {
                ScenePresence presence = World.GetScenePresence(m_item.PermsGranter);

                if (presence != null)
                {
                    if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        presence.RegisterControlEventsToScript(controls, accept, pass_on, m_host.LocalId, m_item.ItemID);
                    }
                }
            }

        }

        public void llReleaseControls()
        {

            if (!m_item.PermsGranter.IsZero())
            {
                ScenePresence presence = World.GetScenePresence(m_item.PermsGranter);

                if (presence != null)
                {
                    if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        // Unregister controls from Presence
                        presence.UnRegisterControlEventsToScript(m_host.LocalId, m_item.ItemID);
                        // Remove Take Control permission.
                        m_item.PermsMask &= ~ScriptBaseClass.PERMISSION_TAKE_CONTROLS;
                    }
                }
            }
        }

        public void llAttachToAvatar(LSL_Integer attachmentPoint)
        {

            if (m_item.PermsGranter != m_host.OwnerID)
                return;

            SceneObjectGroup grp = m_host.ParentGroup;
            if (grp == null || grp.IsDeleted || grp.IsAttachment)
                return;

            if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_ATTACH) != 0)
                AttachToAvatar(attachmentPoint);
        }

        public void llAttachToAvatarTemp(LSL_Integer attachmentPoint)
        {
            IAttachmentsModule attachmentsModule = World.RequestModuleInterface<IAttachmentsModule>();
            if (attachmentsModule == null)
                return;

            if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_ATTACH) == 0)
                return;

            SceneObjectGroup grp = m_host.ParentGroup;
            if (grp == null || grp.IsDeleted || grp.IsAttachment)
                return;

            if (!World.TryGetScenePresence(m_item.PermsGranter, out ScenePresence target))
                return;

            if (target.UUID != grp.OwnerID)
            {
                uint effectivePerms = grp.EffectiveOwnerPerms;

                if ((effectivePerms & (uint)PermissionMask.Transfer) == 0)
                    return;

                UUID permsgranter = m_item.PermsGranter;
                int permsmask = m_item.PermsMask;

                grp.SetOwner(target.UUID, target.ControllingClient.ActiveGroupId);

                if (World.Permissions.PropagatePermissions())
                {
                    foreach (SceneObjectPart child in grp.Parts)
                    {
                        child.Inventory.ChangeInventoryOwner(target.UUID);
                        child.TriggerScriptChangedEvent(Changed.OWNER);
                        child.ApplyNextOwnerPermissions();
                    }
                    grp.InvalidateEffectivePerms();
                }

                m_item.PermsMask = permsmask;
                m_item.PermsGranter = permsgranter;

                grp.RootPart.ObjectSaleType = 0;
                grp.RootPart.SalePrice = 10;

                grp.HasGroupChanged = true;
                grp.RootPart.SendPropertiesToClient(target.ControllingClient);
                grp.RootPart.ScheduleFullUpdate();
            }

            attachmentsModule.AttachObject(target, grp, (uint)attachmentPoint, false, false, true);
        }

    public void llDetachFromAvatar()
        {

            if (m_host.ParentGroup.AttachmentPoint == 0)
                return;

            if (m_item.PermsGranter != m_host.OwnerID)
                return;

            if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_ATTACH) != 0)
                DetachFromAvatar();
        }


    }
}
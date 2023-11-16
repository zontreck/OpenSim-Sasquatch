/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LitJson;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Packets;
using OpenMetaverse.Rendering;
using OpenSim.Framework;
using OpenSim.Region.CoreModules.World.Land;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Animation;
using OpenSim.Region.Framework.Scenes.Scripting;
using OpenSim.Region.Framework.Scenes.Serialization;
using OpenSim.Region.PhysicsModules.SharedBase;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;
using OpenSim.Services.Interfaces;
using AssetLandmark = OpenSim.Framework.AssetLandmark;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using LSL_Float = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLFloat;
using LSL_Integer = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLInteger;
using LSL_Key = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLString;
using LSL_List = OpenSim.Region.ScriptEngine.Shared.LSL_Types.list;
using LSL_Rotation = OpenSim.Region.ScriptEngine.Shared.LSL_Types.Quaternion;
using LSL_String = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLString;
using LSL_Vector = OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3;
using PermissionMask = OpenSim.Framework.PermissionMask;
using PrimType = OpenSim.Region.Framework.Scenes.PrimType;
using RegionFlags = OpenSim.Framework.RegionFlags;
using RegionInfo = OpenSim.Framework.RegionInfo;

#pragma warning disable IDE1006

namespace OpenSim.Region.ScriptEngine.Shared.Api
{
    /// <summary>
    ///     Contains all LSL ll-functions. This class will be in Default AppDomain.
    /// </summary>
    public class LSL_Api : MarshalByRefObject, ILSL_Api, IScriptApi
    {
        /* The new / changed functions were tested with the following LSL script:

        default
        {
            state_entry()
            {
                rotation rot = llEuler2Rot(<0,70,0> * DEG_TO_RAD);

                llOwnerSay("to get here, we rotate over: "+ (string) llRot2Axis(rot));
                llOwnerSay("and we rotate for: "+ (llRot2Angle(rot) * RAD_TO_DEG));

                // convert back and forth between quaternion <-> vector and angle

                rotation newrot = llAxisAngle2Rot(llRot2Axis(rot),llRot2Angle(rot));

                llOwnerSay("Old rotation was: "+(string) rot);
                llOwnerSay("re-converted rotation is: "+(string) newrot);

                llSetRot(rot);  // to check the parameters in the prim
            }
        }
        */


        public void llSetVehicleType(int type)
        {
            if (!m_host.ParentGroup.IsDeleted) m_host.ParentGroup.RootPart.SetVehicleType(type);
        }

        //CFK 9/28: Most, but not all of the underlying plumbing between here and the physics modules is in
        //CFK 9/28: so these are not complete yet.
        public void llSetVehicleFloatParam(int param, LSL_Float value)
        {
            if (!m_host.ParentGroup.IsDeleted) m_host.ParentGroup.RootPart.SetVehicleFloatParam(param, (float)value);
        }

        //CFK 9/28: Most, but not all of the underlying plumbing between here and the physics modules is in
        //CFK 9/28: so these are not complete yet.
        public void llSetVehicleVectorParam(int param, LSL_Vector vec)
        {
            if (!m_host.ParentGroup.IsDeleted) m_host.ParentGroup.RootPart.SetVehicleVectorParam(param, vec);
        }

        //CFK 9/28: Most, but not all of the underlying plumbing between here and the physics modules is in
        //CFK 9/28: so these are not complete yet.
        public void llSetVehicleRotationParam(int param, LSL_Rotation rot)
        {
            if (!m_host.ParentGroup.IsDeleted) m_host.ParentGroup.RootPart.SetVehicleRotationParam(param, rot);
        }

        public void llSetVehicleFlags(int flags)
        {
            if (!m_host.ParentGroup.IsDeleted) m_host.ParentGroup.RootPart.SetVehicleFlags(flags, false);
        }

        public void llRemoveVehicleFlags(int flags)
        {
            if (!m_host.ParentGroup.IsDeleted) m_host.ParentGroup.RootPart.SetVehicleFlags(flags, true);
        }

        public void llSitTarget(LSL_Vector offset, LSL_Rotation rot)
        {
            SitTarget(m_host, offset, rot);
        }

        public void llLinkSitTarget(LSL_Integer link, LSL_Vector offset, LSL_Rotation rot)
        {
            if (link == ScriptBaseClass.LINK_ROOT)
            {
                SitTarget(m_host.ParentGroup.RootPart, offset, rot);
            }
            else if (link == ScriptBaseClass.LINK_THIS)
            {
                SitTarget(m_host, offset, rot);
            }
            else
            {
                SceneObjectPart part = m_host.ParentGroup.GetLinkNumPart(link);
                if (null != part) SitTarget(part, offset, rot);
            }
        }

        public LSL_Key llAvatarOnSitTarget()
        {
            return m_host.SitTargetAvatar.ToString();
        }

        // http://wiki.secondlife.com/wiki/LlAvatarOnLinkSitTarget
        public LSL_Key llAvatarOnLinkSitTarget(LSL_Integer linknum)
        {
            if (linknum == ScriptBaseClass.LINK_SET ||
                linknum == ScriptBaseClass.LINK_ALL_CHILDREN ||
                linknum == ScriptBaseClass.LINK_ALL_OTHERS ||
                linknum == 0)
                return ScriptBaseClass.NULL_KEY;

            List<SceneObjectPart> parts = GetLinkParts(linknum);
            if (parts.Count == 0)
                return ScriptBaseClass.NULL_KEY;
            return parts[0].SitTargetAvatar.ToString();
        }


        public void llAddToLandPassList(LSL_Key avatar, LSL_Float hours)
        {
            if (!UUID.TryParse(avatar, out var key) || key.IsZero())
                return;

            ILandObject land = World.LandChannel.GetLandObject(m_host.AbsolutePosition);
            if (World.Permissions.CanEditParcelProperties(m_host.OwnerID, land, GroupPowers.LandManagePasses, false))
            {
                LandAccessEntry entry;

                var expires = hours != 0 ? Util.UnixTimeSinceEpoch() + (int)(3600.0 * hours) : 0;
                var idx = land.LandData.ParcelAccessList.FindIndex(
                    delegate(LandAccessEntry e)
                    {
                        if (e.Flags == AccessList.Access && e.AgentID.Equals(key))
                            return true;
                        return false;
                    });

                if (idx != -1)
                {
                    entry = land.LandData.ParcelAccessList[idx];
                    if (entry.Expires == 0)
                        return;
                    if (expires != 0 && expires < entry.Expires)
                        return;

                    entry.Expires = expires;
                    World.EventManager.TriggerLandObjectUpdated((uint)land.LandData.LocalID, land);
                    return;
                }

                entry = new LandAccessEntry
                {
                    AgentID = key,
                    Flags = AccessList.Access,
                    Expires = expires
                };

                land.LandData.ParcelAccessList.Add(entry);
                World.EventManager.TriggerLandObjectUpdated((uint)land.LandData.LocalID, land);
            }

            ScriptSleep(m_sleepMsOnAddToLandPassList);
        }

        public void llSetTouchText(string text)
        {
            if (text.Length <= 9)
                m_host.TouchName = text;
            else
                m_host.TouchName = text.Substring(0, 9);
        }

        public void llSetSitText(string text)
        {
            if (text.Length <= 9)
                m_host.SitName = text;
            else
                m_host.SitName = text.Substring(0, 9);
        }

        public void llSetCameraEyeOffset(LSL_Vector offset)
        {
            m_host.SetCameraEyeOffset(offset);

            if (m_host.ParentGroup.RootPart.GetCameraEyeOffset().IsZero())
                m_host.ParentGroup.RootPart.SetCameraEyeOffset(offset);
        }

        public void llSetCameraAtOffset(LSL_Vector offset)
        {
            m_host.SetCameraAtOffset(offset);

            if (m_host.ParentGroup.RootPart.GetCameraAtOffset().IsZero())
                m_host.ParentGroup.RootPart.SetCameraAtOffset(offset);
        }

        public void llSetLinkCamera(LSL_Integer link, LSL_Vector eye, LSL_Vector at)
        {
            if (link == ScriptBaseClass.LINK_SET ||
                link == ScriptBaseClass.LINK_ALL_CHILDREN ||
                link == ScriptBaseClass.LINK_ALL_OTHERS) return;

            SceneObjectPart part = null;

            switch (link)
            {
                case ScriptBaseClass.LINK_ROOT:
                    part = m_host.ParentGroup.RootPart;
                    break;
                case ScriptBaseClass.LINK_THIS:
                    part = m_host;
                    break;
                default:
                    part = m_host.ParentGroup.GetLinkNumPart(link);
                    break;
            }

            if (null != part)
            {
                part.SetCameraEyeOffset(eye);
                part.SetCameraAtOffset(at);
            }
        }

        public LSL_String llDumpList2String(LSL_List src, string seperator)
        {
            if (src.Length == 0) return string.Empty;
            var ret = string.Empty;
            foreach (var o in src.Data) ret = ret + o + seperator;
            ret = ret.Substring(0, ret.Length - seperator.Length);
            return ret;
        }

        public LSL_Integer llScriptDanger(LSL_Vector pos)
        {
            return World.LSLScriptDanger(m_host, pos) ? 1 : 0;
        }

        public void llDialog(LSL_Key avatar, LSL_String message, LSL_List buttons, int chat_channel)
        {
            IDialogModule dm = World.RequestModuleInterface<IDialogModule>();

            if (dm == null)
                return;

            if (!UUID.TryParse(avatar, out var av) || av.IsZero())
            {
                Error("llDialog", "First parameter must be a valid key");
                return;
            }

            if (!m_host.GetOwnerName(out string fname, out string lname))
                return;

            var length = buttons.Length;
            if (length < 1)
            {
                buttons.Add(new LSL_String("Ok"));
                length = 1;
            }
            else if (length > 12)
            {
                Error("llDialog", "No more than 12 buttons can be shown");
                return;
            }

            if (message.Length == 0)
                Error("llDialog", "Empty message");
            else if (Encoding.UTF8.GetByteCount(message) > 512) Error("llDialog", "Message longer than 512 bytes");

            var buts = new string[length];
            for (var i = 0; i < length; i++)
            {
                buts[i] = buttons.Data[i].ToString();
                if (buts[i].Length == 0)
                {
                    Error("llDialog", "Button label cannot be blank");
                    return;
                }

/*
                if (buttons.Data[i].ToString().Length > 24)
                {
                    Error("llDialog", "Button label cannot be longer than 24 characters");
                    return;
                }
*/
                buts[i] = buttons.Data[i].ToString();
            }

            dm.SendDialogToUser(
                av, m_host.Name, m_host.UUID, m_host.OwnerID, fname, lname,
                message, new UUID("00000000-0000-2222-3333-100000001000"), chat_channel, buts);

            ScriptSleep(m_sleepMsOnDialog);
        }

        public void llVolumeDetect(int detect)
        {
            if (!m_host.ParentGroup.IsDeleted)
                m_host.ParentGroup.ScriptSetVolumeDetect(detect != 0);
        }

        public void llRemoteLoadScript(string target, string name, int running, int start_param)
        {
            Deprecated("llRemoteLoadScript", "Use llRemoteLoadScriptPin instead");
            ScriptSleep(m_sleepMsOnRemoteLoadScript);
        }

        public void llSetRemoteScriptAccessPin(int pin)
        {
            m_host.ScriptAccessPin = pin;
        }

        public void llRemoteLoadScriptPin(string target, string name, int pin, int running, int start_param)
        {
            if (!UUID.TryParse(target, out var destId) || destId.IsZero())
            {
                Error("llRemoteLoadScriptPin", "invalid key '" + target + "'");
                return;
            }

            // target must be a different prim than the one containing the script
            if (m_host.UUID.Equals(destId)) return;

            // copy the first script found with this inventory name
            TaskInventoryItem item = m_host.Inventory.GetInventoryItem(name);

            // make sure the object is a script
            if (item == null || item.Type != 10)
            {
                Error("llRemoteLoadScriptPin", "Can't find script '" + name + "'");
                return;
            }

            if ((item.BasePermissions & (uint)PermissionMask.Copy) == 0)
            {
                Error("llRemoteLoadScriptPin", "No copy rights");
                return;
            }

            // the rest of the permission checks are done in RezScript, so check the pin there as well
            World.RezScriptFromPrim(item.ItemID, m_host, destId, pin, running, start_param);

            // this will cause the delay even if the script pin or permissions were wrong - seems ok
            ScriptSleep(m_sleepMsOnRemoteLoadScriptPin);
        }

        public void llOpenRemoteDataChannel()
        {
            IXMLRPC xmlrpcMod = m_ScriptEngine.World.RequestModuleInterface<IXMLRPC>();
            if (xmlrpcMod != null && xmlrpcMod.IsEnabled())
            {
                var channelID = xmlrpcMod.OpenXMLRPCChannel(m_host.LocalId, m_item.ItemID, UUID.Zero);
                IXmlRpcRouter xmlRpcRouter = m_ScriptEngine.World.RequestModuleInterface<IXmlRpcRouter>();
                if (xmlRpcRouter != null)
                {
                    string ExternalHostName = m_ScriptEngine.World.RegionInfo.ExternalHostName;

                    xmlRpcRouter.RegisterNewReceiver(m_ScriptEngine.ScriptModule, channelID, m_host.UUID,
                        m_item.ItemID, string.Format("http://{0}:{1}/", ExternalHostName,
                            xmlrpcMod.Port.ToString()));
                }

                object[] resobj =
                {
                    new LSL_Integer(1),
                    new LSL_String(channelID.ToString()),
                    new LSL_String(ScriptBaseClass.NULL_KEY),
                    new LSL_String(string.Empty),
                    new LSL_Integer(0),
                    new LSL_String(string.Empty)
                };
                m_ScriptEngine.PostScriptEvent(m_item.ItemID, new EventParams("remote_data", resobj,
                    new DetectParams[0]));
            }

            ScriptSleep(m_sleepMsOnOpenRemoteDataChannel);
        }

        public LSL_Key llSendRemoteData(string channel, string dest, int idata, string sdata)
        {
            IXMLRPC xmlrpcMod = m_ScriptEngine.World.RequestModuleInterface<IXMLRPC>();
            ScriptSleep(m_sleepMsOnSendRemoteData);
            if (xmlrpcMod == null)
                return "";
            return xmlrpcMod.SendRemoteData(m_host.LocalId, m_item.ItemID, channel, dest, idata, sdata).ToString();
        }

        public void llRemoteDataReply(string channel, string message_id, string sdata, int idata)
        {
            IXMLRPC xmlrpcMod = m_ScriptEngine.World.RequestModuleInterface<IXMLRPC>();
            if (xmlrpcMod != null)
                xmlrpcMod.RemoteDataReply(channel, message_id, sdata, idata);
            ScriptSleep(m_sleepMsOnRemoteDataReply);
        }

        public void llCloseRemoteDataChannel(string channel)
        {
            IXmlRpcRouter xmlRpcRouter = m_ScriptEngine.World.RequestModuleInterface<IXmlRpcRouter>();
            if (xmlRpcRouter != null) xmlRpcRouter.UnRegisterReceiver(channel, m_item.ItemID);

            IXMLRPC xmlrpcMod = m_ScriptEngine.World.RequestModuleInterface<IXMLRPC>();
            if (xmlrpcMod != null)
                xmlrpcMod.CloseXMLRPCChannel((UUID)channel);
            ScriptSleep(m_sleepMsOnCloseRemoteDataChannel);
        }

        public LSL_String llMD5String(string src, int nonce)
        {
            return Util.Md5Hash(string.Format("{0}:{1}", src, nonce.ToString()), Encoding.UTF8);
        }

        public LSL_String llSHA1String(string src)
        {
            return Util.SHA1Hash(src, Encoding.UTF8).ToLower();
        }

        public LSL_String llSHA256String(LSL_String input)
        {
            // Create a SHA256
            using (var sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Util.bytesToHexString(bytes, true);
            }
        }

        public void llSetPrimitiveParams(LSL_List rules)
        {
            SetLinkPrimParams(ScriptBaseClass.LINK_THIS, rules, "llSetPrimitiveParams");

            ScriptSleep(m_sleepMsOnSetPrimitiveParams);
        }

        public void llSetLinkPrimitiveParams(int linknumber, LSL_List rules)
        {
            SetLinkPrimParams(linknumber, rules, "llSetLinkPrimitiveParams");

            ScriptSleep(m_sleepMsOnSetLinkPrimitiveParams);
        }

        public void llSetLinkPrimitiveParamsFast(int linknumber, LSL_List rules)
        {
            SetLinkPrimParams(linknumber, rules, "llSetLinkPrimitiveParamsFast");
        }

        public void llSetKeyframedMotion(LSL_List frames, LSL_List options)
        {
            SceneObjectGroup group = m_host.ParentGroup;

            if (group.RootPart.PhysActor != null && group.RootPart.PhysActor.IsPhysical)
                return;
            if (group.IsAttachment)
                return;

            if (frames.Data.Length > 0) // We are getting a new motion
            {
                if (group.RootPart.KeyframeMotion != null)
                    group.RootPart.KeyframeMotion.Delete();
                group.RootPart.KeyframeMotion = null;

                var idx = 0;

                var mode = KeyframeMotion.PlayMode.Forward;
                var data = KeyframeMotion.DataFormat.Translation | KeyframeMotion.DataFormat.Rotation;

                while (idx < options.Data.Length)
                {
                    int option = options.GetLSLIntegerItem(idx++);
                    var remain = options.Data.Length - idx;

                    switch (option)
                    {
                        case ScriptBaseClass.KFM_MODE:
                            if (remain < 1)
                                break;
                            int modeval = options.GetLSLIntegerItem(idx++);
                            switch (modeval)
                            {
                                case ScriptBaseClass.KFM_FORWARD:
                                    mode = KeyframeMotion.PlayMode.Forward;
                                    break;
                                case ScriptBaseClass.KFM_REVERSE:
                                    mode = KeyframeMotion.PlayMode.Reverse;
                                    break;
                                case ScriptBaseClass.KFM_LOOP:
                                    mode = KeyframeMotion.PlayMode.Loop;
                                    break;
                                case ScriptBaseClass.KFM_PING_PONG:
                                    mode = KeyframeMotion.PlayMode.PingPong;
                                    break;
                            }

                            break;
                        case ScriptBaseClass.KFM_DATA:
                            if (remain < 1)
                                break;
                            int dataval = options.GetLSLIntegerItem(idx++);
                            data = (KeyframeMotion.DataFormat)dataval;
                            break;
                    }
                }

                group.RootPart.KeyframeMotion = new KeyframeMotion(group, mode, data);

                idx = 0;

                var elemLength = 2;
                if (data == (KeyframeMotion.DataFormat.Translation | KeyframeMotion.DataFormat.Rotation))
                    elemLength = 3;

                var keyframes = new List<KeyframeMotion.Keyframe>();
                var hasTranslation = (data & KeyframeMotion.DataFormat.Translation) != 0;
                var hasRotation = (data & KeyframeMotion.DataFormat.Rotation) != 0;
                while (idx < frames.Data.Length)
                {
                    var remain = frames.Data.Length - idx;

                    if (remain < elemLength)
                        break;

                    var frame = new KeyframeMotion.Keyframe
                    {
                        Position = null,
                        Rotation = null
                    };

                    if (hasTranslation)
                    {
                        var tempv = frames.GetVector3Item(idx++);
                        frame.Position = new Vector3((float)tempv.x, (float)tempv.y, (float)tempv.z);
                    }

                    if (hasRotation)
                    {
                        var tempq = frames.GetQuaternionItem(idx++);
                        var q = new Quaternion((float)tempq.x, (float)tempq.y, (float)tempq.z, (float)tempq.s);
                        frame.Rotation = q;
                    }

                    var tempf = (float)frames.GetLSLFloatItem(idx++);
                    frame.TimeMS = (int)(tempf * 1000.0f);

                    keyframes.Add(frame);
                }

                group.RootPart.KeyframeMotion.SetKeyframes(keyframes.ToArray());
                group.RootPart.KeyframeMotion.Start();
            }
            else
            {
                if (group.RootPart.KeyframeMotion == null)
                    return;

                if (options.Data.Length == 0)
                {
                    group.RootPart.KeyframeMotion.Stop();
                    return;
                }

                var idx = 0;

                while (idx < options.Data.Length)
                {
                    int option = options.GetLSLIntegerItem(idx++);

                    switch (option)
                    {
                        case ScriptBaseClass.KFM_COMMAND:
                            int cmd = options.GetLSLIntegerItem(idx++);
                            switch (cmd)
                            {
                                case ScriptBaseClass.KFM_CMD_PLAY:
                                    group.RootPart.KeyframeMotion.Start();
                                    break;
                                case ScriptBaseClass.KFM_CMD_STOP:
                                    group.RootPart.KeyframeMotion.Stop();
                                    break;
                                case ScriptBaseClass.KFM_CMD_PAUSE:
                                    group.RootPart.KeyframeMotion.Pause();
                                    break;
                            }

                            break;
                    }
                }
            }
        }

        public LSL_List llGetPhysicsMaterial()
        {
            var result = new LSL_List();

            result.Add(new LSL_Float(m_host.GravityModifier));
            result.Add(new LSL_Float(m_host.Restitution));
            result.Add(new LSL_Float(m_host.Friction));
            result.Add(new LSL_Float(m_host.Density));

            return result;
        }

        public void llSetPhysicsMaterial(int material_bits,
            LSL_Float material_gravity_modifier, LSL_Float material_restitution,
            LSL_Float material_friction, LSL_Float material_density)
        {
            SetPhysicsMaterial(m_host, material_bits, (float)material_density, (float)material_friction,
                (float)material_restitution, (float)material_gravity_modifier);
        }

        public LSL_String llStringToBase64(string str)
        {
            try
            {
                byte[] encData_byte;
                encData_byte = Util.UTF8.GetBytes(str);
                var encodedData = Convert.ToBase64String(encData_byte);
                return encodedData;
            }
            catch
            {
                Error("llBase64ToString", "Error encoding string");
                return string.Empty;
            }
        }

        public LSL_String llBase64ToString(string str)
        {
            try
            {
                var b = Convert.FromBase64String(str);
                return Encoding.UTF8.GetString(b);
            }
            catch
            {
                Error("llBase64ToString", "Error decoding string");
                return string.Empty;
            }
        }


        public void llRemoteDataSetRegion()
        {
            Deprecated("llRemoteDataSetRegion", "Use llOpenRemoteDataChannel instead");
        }

        public LSL_Float llLog10(double val)
        {
            return Math.Log10(val);
        }

        public LSL_Float llLog(double val)
        {
            return Math.Log(val);
        }

        public LSL_List llGetAnimationList(LSL_Key id)
        {
            if (!UUID.TryParse(id, out var avID) || avID.IsZero())
                return new LSL_List();

            ScenePresence av = World.GetScenePresence(avID);
            if (av == null || av.IsChildAgent) // only if in the region
                return new LSL_List();

            var anims = av.Animator.GetAnimationArray();
            var l = new LSL_List();
            foreach (var foo in anims)
                l.Add(new LSL_Key(foo.ToString()));
            return l;
        }

        public void llSetParcelMusicURL(string url)
        {
            ILandObject land = World.LandChannel.GetLandObject(m_host.AbsolutePosition);

            if (land.LandData.OwnerID != m_host.OwnerID)
                return;

            land.SetMusicUrl(url);

            ScriptSleep(m_sleepMsOnSetParcelMusicURL);
        }

        public LSL_String llGetParcelMusicURL()
        {
            ILandObject land = World.LandChannel.GetLandObject(m_host.AbsolutePosition);

            if (land.LandData.OwnerID != m_host.OwnerID)
                return string.Empty;

            return land.GetMusicUrl();
        }

        public LSL_Vector llGetRootPosition()
        {
            return new LSL_Vector(m_host.ParentGroup.AbsolutePosition);
        }

        /// <summary>
        ///     http://lslwiki.net/lslwiki/wakka.php?wakka=llGetRot
        ///     http://lslwiki.net/lslwiki/wakka.php?wakka=ChildRotation
        ///     Also tested in sl in regards to the behaviour in attachments/mouselook
        ///     In the root prim:-
        ///     Returns the object rotation if not attached
        ///     Returns the avatars rotation if attached
        ///     Returns the camera rotation if attached and the avatar is in mouselook
        /// </summary>
        public LSL_Rotation llGetRootRotation()
        {
            Quaternion q;
            if (m_host.ParentGroup.AttachmentPoint != 0)
            {
                ScenePresence avatar = World.GetScenePresence(m_host.ParentGroup.AttachedAvatar);
                if (avatar != null)
                    if ((avatar.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) != 0)
                        q = avatar.CameraRotation; // Mouselook
                    else
                        q = avatar.GetWorldRotation(); // Currently infrequently updated so may be inaccurate
                else
                    q = m_host.ParentGroup.GroupRotation; // Likely never get here but just in case
            }
            else
            {
                q = m_host.ParentGroup.GroupRotation; // just the group rotation
            }

            return new LSL_Rotation(q);
        }

        public LSL_String llGetObjectDesc()
        {
            return m_host.Description ?? string.Empty;
        }

        public void llSetObjectDesc(string desc)
        {
            m_host.Description = desc ?? string.Empty;
        }

        public LSL_Key llGetCreator()
        {
            return m_host.CreatorID.ToString();
        }

        public LSL_String llGetTimestamp()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }

        public LSL_Integer llGetNumberOfPrims()
        {
            return m_host.ParentGroup.PrimCount + m_host.ParentGroup.GetSittingAvatarsCount();
        }

        /// <summary>
        ///     Full implementation of llGetBoundingBox according to SL 2015-04-15.
        ///     http://wiki.secondlife.com/wiki/LlGetBoundingBox
        ///     http://lslwiki.net/lslwiki/wakka.php?wakka=llGetBoundingBox
        ///     Returns local bounding box of avatar without attachments
        ///     if target is non-seated avatar or prim/mesh in avatar attachment.
        ///     Returns local bounding box of object
        ///     if target is seated avatar or prim/mesh in object.
        ///     Uses less accurate box models for speed.
        /// </summary>
        public LSL_List llGetBoundingBox(string obj)
        {
            var result = new LSL_List();

            // If the ID is not valid, return null result
            if (!UUID.TryParse(obj, out var objID) || objID.IsZero())
            {
                result.Add(new LSL_Vector());
                result.Add(new LSL_Vector());
                return result;
            }

            // Check if this is an attached prim. If so, replace
            // the UUID with the avatar UUID and report it's bounding box
            SceneObjectPart part = World.GetSceneObjectPart(objID);
            if (part != null && part.ParentGroup.IsAttachment)
                objID = part.ParentGroup.AttachedAvatar;

            // Find out if this is an avatar ID. If so, return it's box
            ScenePresence presence = World.GetScenePresence(objID);
            if (presence != null)
            {
                LSL_Vector lower;
                LSL_Vector upper;

                var box = presence.Appearance.AvatarBoxSize * 0.5f;

                if (presence.Animator.Animations.ImplicitDefaultAnimation.AnimID.Equals(
                        DefaultAvatarAnimations.AnimsUUIDbyName["SIT_GROUND_CONSTRAINED"]))
                {
                    // This is for ground sitting avatars TODO!
                    lower = new LSL_Vector(-box.X - 0.1125, -box.Y, box.Z * -1.0f);
                    upper = new LSL_Vector(box.X + 0.1125, box.Y, box.Z * -1.0f);
                }
                else
                {
                    // This is for standing/flying avatars
                    lower = new LSL_Vector(-box.X, -box.Y, -box.Z);
                    upper = new LSL_Vector(box.X, box.Y, box.Z);
                }

                if (lower.x > upper.x)
                    lower.x = upper.x;
                if (lower.y > upper.y)
                    lower.y = upper.y;
                if (lower.z > upper.z)
                    lower.z = upper.z;

                result.Add(lower);
                result.Add(upper);
                return result;
            }

            part = World.GetSceneObjectPart(objID);

            // Currently only works for single prims without a sitting avatar
            if (part == null)
            {
                result.Add(new LSL_Vector());
                result.Add(new LSL_Vector());
                return result;
            }

            var sog = part.ParentGroup;
            if (sog.IsDeleted)
            {
                result.Add(new LSL_Vector());
                result.Add(new LSL_Vector());
                return result;
            }

            sog.GetBoundingBox(out var minX, out var maxX, out var minY, out var maxY, out var minZ, out var maxZ);

            result.Add(new LSL_Vector(minX, minY, minZ));
            result.Add(new LSL_Vector(maxX, maxY, maxZ));
            return result;
        }


        public LSL_Vector llGetGeometricCenter()
        {
            return new LSL_Vector(m_host.GetGeometricCenter());
        }

        public LSL_List llGetPrimitiveParams(LSL_List rules)
        {
            var result = new LSL_List();

            var remaining = GetPrimParams(m_host, rules, ref result);

            while (!(remaining is null) && remaining.Length > 1)
            {
                int linknumber = remaining.GetLSLIntegerItem(0);
                rules = remaining.GetSublist(1, -1);
                List<SceneObjectPart> parts = GetLinkParts(linknumber);
                if (parts.Count == 0)
                    break;
                foreach (var part in parts)
                    remaining = GetPrimParams(part, rules, ref result);
            }

            return result;
        }

        public LSL_List llGetLinkPrimitiveParams(int linknumber, LSL_List rules)
        {
            // according to SL wiki this must indicate a single link number or link_root or link_this.
            // keep other options as before

            List<SceneObjectPart> parts;
            List<ScenePresence> avatars;

            var res = new LSL_List();
            var remaining = new LSL_List();

            while (rules.Length > 0)
            {
                parts = GetLinkParts(linknumber);
                avatars = GetLinkAvatars(linknumber);

                remaining = new LSL_List();
                foreach (var part in parts) remaining = GetPrimParams(part, rules, ref res);
                foreach (var avatar in avatars) remaining = GetPrimParams(avatar, rules, ref res);

                if (remaining.Length > 0)
                {
                    linknumber = remaining.GetLSLIntegerItem(0);
                    rules = remaining.GetSublist(1, -1);
                }
                else
                {
                    break;
                }
            }

            return res;
        }

        public LSL_List llGetPrimMediaParams(int face, LSL_List rules)
        {
            ScriptSleep(m_sleepMsOnGetPrimMediaParams);
            return GetPrimMediaParams(m_host, face, rules);
        }

        public LSL_List llGetLinkMedia(LSL_Integer link, LSL_Integer face, LSL_List rules)
        {
            ScriptSleep(m_sleepMsOnGetLinkMedia);
            if (link == ScriptBaseClass.LINK_ROOT) return GetPrimMediaParams(m_host.ParentGroup.RootPart, face, rules);

            if (link == ScriptBaseClass.LINK_THIS) return GetPrimMediaParams(m_host, face, rules);

            SceneObjectPart part = m_host.ParentGroup.GetLinkNumPart(link);
            if (null != part)
                return GetPrimMediaParams(part, face, rules);

            return new LSL_List();
        }

        public LSL_Integer llSetPrimMediaParams(LSL_Integer face, LSL_List rules)
        {
            ScriptSleep(m_sleepMsOnSetPrimMediaParams);
            return SetPrimMediaParams(m_host, face, rules);
        }

        public LSL_Integer llSetLinkMedia(LSL_Integer link, LSL_Integer face, LSL_List rules)
        {
            ScriptSleep(m_sleepMsOnSetLinkMedia);
            if (link == ScriptBaseClass.LINK_ROOT) return SetPrimMediaParams(m_host.ParentGroup.RootPart, face, rules);

            if (link == ScriptBaseClass.LINK_THIS) return SetPrimMediaParams(m_host, face, rules);

            SceneObjectPart part = m_host.ParentGroup.GetLinkNumPart(link);
            if (null != part)
                return SetPrimMediaParams(part, face, rules);

            return ScriptBaseClass.LSL_STATUS_NOT_FOUND;
        }

        public LSL_Integer llClearPrimMedia(LSL_Integer face)
        {
            ScriptSleep(m_sleepMsOnClearPrimMedia);
            return ClearPrimMedia(m_host, face);
        }

        public LSL_Integer llClearLinkMedia(LSL_Integer link, LSL_Integer face)
        {
            ScriptSleep(m_sleepMsOnClearLinkMedia);
            if (link == ScriptBaseClass.LINK_ROOT) return ClearPrimMedia(m_host.ParentGroup.RootPart, face);

            if (link == ScriptBaseClass.LINK_THIS) return ClearPrimMedia(m_host, face);

            SceneObjectPart part = m_host.ParentGroup.GetLinkNumPart(link);
            if (null != part)
                return ClearPrimMedia(part, face);

            return ScriptBaseClass.LSL_STATUS_NOT_FOUND;
        }

        //  <summary>
        //  Converts a 32-bit integer into a Base64
        //  character string. Base64 character strings
        //  are always 8 characters long. All iinteger
        //  values are acceptable.
        //  </summary>
        //  <param name="number">
        //  32-bit integer to be converted.
        //  </param>
        //  <returns>
        //  8 character string. The 1st six characters
        //  contain the encoded number, the last two
        //  characters are padded with "=".
        //  </returns>

        public LSL_String llIntegerToBase64(int number)
        {
            // uninitialized string

            var imdt = new char[8];


            // Manually unroll the loop

            imdt[7] = '=';
            imdt[6] = '=';
            imdt[5] = i2ctable[(number << 4) & 0x3F];
            imdt[4] = i2ctable[(number >> 2) & 0x3F];
            imdt[3] = i2ctable[(number >> 8) & 0x3F];
            imdt[2] = i2ctable[(number >> 14) & 0x3F];
            imdt[1] = i2ctable[(number >> 20) & 0x3F];
            imdt[0] = i2ctable[(number >> 26) & 0x3F];

            return new string(imdt);
        }

        //  <summary>
        //  Converts an eight character base-64 string
        //  into a 32-bit integer.
        //  </summary>
        //  <param name="str">
        //  8 characters string to be converted. Other
        //  length strings return zero.
        //  </param>
        //  <returns>
        //  Returns an integer representing the
        //  encoded value providedint he 1st 6
        //  characters of the string.
        //  </returns>
        //  <remarks>
        //  This is coded to behave like LSL's
        //  implementation (I think), based upon the
        //  information available at the Wiki.
        //  If more than 8 characters are supplied,
        //  zero is returned.
        //  If a NULL string is supplied, zero will
        //  be returned.
        //  If fewer than 6 characters are supplied, then
        //  the answer will reflect a partial
        //  accumulation.
        //  <para>
        //  The 6-bit segments are
        //  extracted left-to-right in big-endian mode,
        //  which means that segment 6 only contains the
        //  two low-order bits of the 32 bit integer as
        //  its high order 2 bits. A short string therefore
        //  means loss of low-order information. E.g.
        //
        //  |<---------------------- 32-bit integer ----------------------->|<-Pad->|
        //  |<--Byte 0----->|<--Byte 1----->|<--Byte 2----->|<--Byte 3----->|<-Pad->|
        //  |3|3|2|2|2|2|2|2|2|2|2|2|1|1|1|1|1|1|1|1|1|1| | | | | | | | | | |P|P|P|P|
        //  |1|0|9|8|7|6|5|4|3|2|1|0|9|8|7|6|5|4|3|2|1|0|9|8|7|6|5|4|3|2|1|0|P|P|P|P|
        //  |  str[0]   |  str[1]   |  str[2]   |  str[3]   |  str[4]   |  str[6]   |
        //
        //  </para>
        //  </remarks>

        public LSL_Integer llBase64ToInteger(string str)
        {
            var number = 0;
            int digit;


            //    Require a well-fromed base64 string

            if (str.Length > 8)
                return 0;

            //    The loop is unrolled in the interests
            //    of performance and simple necessity.
            //
            //    MUST find 6 digits to be well formed
            //      -1 == invalid
            //       0 == padding

            if ((digit = c2itable[str[0]]) <= 0) return digit < 0 ? 0 : number;
            number += --digit << 26;

            if ((digit = c2itable[str[1]]) <= 0) return digit < 0 ? 0 : number;
            number += --digit << 20;

            if ((digit = c2itable[str[2]]) <= 0) return digit < 0 ? 0 : number;
            number += --digit << 14;

            if ((digit = c2itable[str[3]]) <= 0) return digit < 0 ? 0 : number;
            number += --digit << 8;

            if ((digit = c2itable[str[4]]) <= 0) return digit < 0 ? 0 : number;
            number += --digit << 2;

            if ((digit = c2itable[str[5]]) <= 0) return digit < 0 ? 0 : number;
            number += --digit >> 4;

            // ignore trailing padding

            return number;
        }

        public LSL_Float llGetGMTclock()
        {
            return DateTime.UtcNow.TimeOfDay.TotalSeconds;
        }

        public LSL_String llGetHTTPHeader(LSL_Key request_id, string header)
        {
            if (m_UrlModule != null)
                return m_UrlModule.GetHttpHeader(new UUID(request_id), header);
            return string.Empty;
        }


        public LSL_String llGetSimulatorHostname()
        {
            IUrlModule UrlModule = World.RequestModuleInterface<IUrlModule>();
            return UrlModule.ExternalHostNameForLSL;
        }

        //  <summary>
        //  Scan the string supplied in 'src' and
        //  tokenize it based upon two sets of
        //  tokenizers provided in two lists,
        //  separators and spacers.
        //  </summary>
        //
        //  <remarks>
        //  Separators demarcate tokens and are
        //  elided as they are encountered. Spacers
        //  also demarcate tokens, but are themselves
        //  retained as tokens.
        //
        //  Both separators and spacers may be arbitrarily
        //  long strings. i.e. ":::".
        //
        //  The function returns an ordered list
        //  representing the tokens found in the supplied
        //  sources string. If two successive tokenizers
        //  are encountered, then a null-string entry is
        //  added to the list.
        //
        //  It is a precondition that the source and
        //  toekizer lisst are non-null. If they are null,
        //  then a null pointer exception will be thrown
        //  while their lengths are being determined.
        //
        //  A small amount of working memoryis required
        //  of approximately 8*#tokenizers + 8*srcstrlen.
        //
        //  There are many ways in which this function
        //  can be implemented, this implementation is
        //  fairly naive and assumes that when the
        //  function is invooked with a short source
        //  string and/or short lists of tokenizers, then
        //  performance will not be an issue.
        //
        //  In order to minimize the perofrmance
        //  effects of long strings, or large numbers
        //  of tokeizers, the function skips as far as
        //  possible whenever a toekenizer is found,
        //  and eliminates redundant tokenizers as soon
        //  as is possible.
        //
        //  The implementation tries to minimize temporary
        //  garbage generation.
        //  </remarks>

        public LSL_List llParseStringKeepNulls(string src, LSL_List separators, LSL_List spacers)
        {
            return ParseString2List(src, separators, spacers, true);
        }

        public LSL_Integer llGetObjectPermMask(int mask)
        {
            switch (mask)
            {
                case ScriptBaseClass.MASK_BASE:
                    return PermissionMaskToLSLPerm(m_host.BaseMask);

                case ScriptBaseClass.MASK_OWNER:
                    return PermissionMaskToLSLPerm(m_host.OwnerMask);

                case ScriptBaseClass.MASK_GROUP:
                    return PermissionMaskToLSLPerm(m_host.GroupMask);

                case ScriptBaseClass.MASK_EVERYONE:
                    return PermissionMaskToLSLPerm(m_host.EveryoneMask);

                case ScriptBaseClass.MASK_NEXT:
                    return PermissionMaskToLSLPerm(m_host.NextOwnerMask);
            }

            return -1;
        }

        public void llSetObjectPermMask(int mask, int value)
        {
            if (!m_AllowGodFunctions || !World.Permissions.IsAdministrator(m_host.OwnerID))
                return;

            // not even admins have right to violate basic rules
            if (mask != ScriptBaseClass.MASK_BASE)
            {
                mask &= PermissionMaskToLSLPerm(m_host.BaseMask);
                if (mask != ScriptBaseClass.MASK_OWNER)
                    mask &= PermissionMaskToLSLPerm(m_host.OwnerMask);
            }

            switch (mask)
            {
                case ScriptBaseClass.MASK_BASE:
                    value = fixedCopyTransfer(value);
                    m_host.BaseMask = LSLPermToPermissionMask(value, m_host.BaseMask);
                    break;

                case ScriptBaseClass.MASK_OWNER:
                    value = fixedCopyTransfer(value);
                    m_host.OwnerMask = LSLPermToPermissionMask(value, m_host.OwnerMask);
                    break;

                case ScriptBaseClass.MASK_GROUP:
                    m_host.GroupMask = LSLPermToPermissionMask(value, m_host.GroupMask);
                    break;

                case ScriptBaseClass.MASK_EVERYONE:
                    m_host.EveryoneMask = LSLPermToPermissionMask(value, m_host.EveryoneMask);
                    break;

                case ScriptBaseClass.MASK_NEXT:
                    value = fixedCopyTransfer(value);
                    m_host.NextOwnerMask = LSLPermToPermissionMask(value, m_host.NextOwnerMask);
                    break;
                default:
                    return;
            }

            m_host.ParentGroup.AggregatePerms();
        }

        public LSL_Integer llGetInventoryPermMask(string itemName, int mask)
        {
            TaskInventoryItem item = m_host.Inventory.GetInventoryItem(itemName);

            if (item == null)
                return -1;

            switch (mask)
            {
                case ScriptBaseClass.MASK_BASE:
                    return PermissionMaskToLSLPerm(item.BasePermissions);
                case ScriptBaseClass.MASK_OWNER:
                    return PermissionMaskToLSLPerm(item.CurrentPermissions);
                case ScriptBaseClass.MASK_GROUP:
                    return PermissionMaskToLSLPerm(item.GroupPermissions);
                case ScriptBaseClass.MASK_EVERYONE:
                    return PermissionMaskToLSLPerm(item.EveryonePermissions);
                case ScriptBaseClass.MASK_NEXT:
                    return PermissionMaskToLSLPerm(item.NextPermissions);
            }

            return -1;
        }

        public void llSetInventoryPermMask(string itemName, int mask, int value)
        {
            if (!m_AllowGodFunctions || !World.Permissions.IsAdministrator(m_host.OwnerID))
                return;

            TaskInventoryItem item = m_host.Inventory.GetInventoryItem(itemName);

            if (item != null)
            {
                if (mask != ScriptBaseClass.MASK_BASE)
                {
                    mask &= PermissionMaskToLSLPerm(item.BasePermissions);
                    if (mask != ScriptBaseClass.MASK_OWNER)
                        mask &= PermissionMaskToLSLPerm(item.CurrentPermissions);
                }

                /*
                if(item.Type == (int)(AssetType.Settings))
                    value |= ScriptBaseClass.PERM_COPY;
                */

                switch (mask)
                {
                    case ScriptBaseClass.MASK_BASE:
                        value = fixedCopyTransfer(value);
                        item.BasePermissions = LSLPermToPermissionMask(value, item.BasePermissions);
                        break;
                    case ScriptBaseClass.MASK_OWNER:
                        value = fixedCopyTransfer(value);
                        item.CurrentPermissions = LSLPermToPermissionMask(value, item.CurrentPermissions);
                        break;
                    case ScriptBaseClass.MASK_GROUP:
                        item.GroupPermissions = LSLPermToPermissionMask(value, item.GroupPermissions);
                        break;
                    case ScriptBaseClass.MASK_EVERYONE:
                        item.EveryonePermissions = LSLPermToPermissionMask(value, item.EveryonePermissions);
                        break;
                    case ScriptBaseClass.MASK_NEXT:
                        value = fixedCopyTransfer(value);
                        item.NextPermissions = LSLPermToPermissionMask(value, item.NextPermissions);
                        break;
                    default:
                        return;
                }

                m_host.ParentGroup.InvalidateDeepEffectivePerms();
                m_host.ParentGroup.AggregatePerms();
            }
        }

        public LSL_Key llGetInventoryCreator(string itemName)
        {
            TaskInventoryItem item = m_host.Inventory.GetInventoryItem(itemName);

            if (item == null)
            {
                Error("llGetInventoryCreator", "Can't find item '" + itemName + "'");
                return string.Empty;
            }

            return item.CreatorID.ToString();
        }

        public LSL_String llGetInventoryAcquireTime(string itemName)
        {
            TaskInventoryItem item = m_host.Inventory.GetInventoryItem(itemName);

            if (item == null)
            {
                Error("llGetInventoryAcquireTime", "Can't find item '" + itemName + "'");
                return string.Empty;
            }

            var date = Util.ToDateTime(item.CreationDate);
            return date.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        public void llOwnerSay(string msg)
        {
            if (m_host.OwnerID.Equals(m_host.GroupID))
                return;
            World.SimChatBroadcast(msg, ChatTypeEnum.Owner, 0,
                m_host.AbsolutePosition, m_host.Name, m_host.UUID, false);
        }

        public LSL_Key llRequestSecureURL()
        {
            if (m_UrlModule != null)
                return m_UrlModule.RequestSecureURL(m_ScriptEngine.ScriptModule, m_host, m_item.ItemID, null)
                    .ToString();
            return ScriptBaseClass.NULL_KEY;
        }

        public LSL_Key llRequestSimulatorData(string simulator, int data)
        {
            try
            {
                if (m_regionName.Equals(simulator))
                {
                    var lreply = string.Empty;
                    RegionInfo rinfo = World.RegionInfo;
                    switch (data)
                    {
                        case ScriptBaseClass.DATA_SIM_POS:
                            lreply = new LSL_Vector(
                                rinfo.RegionLocX,
                                rinfo.RegionLocY,
                                0).ToString();
                            break;
                        case ScriptBaseClass.DATA_SIM_STATUS:
                            lreply = "up"; // Duh!
                            break;
                        case ScriptBaseClass.DATA_SIM_RATING:
                            switch (rinfo.RegionSettings.Maturity)
                            {
                                case 0:
                                    lreply = "PG";
                                    break;
                                case 1:
                                    lreply = "MATURE";
                                    break;
                                case 2:
                                    lreply = "ADULT";
                                    break;
                                default:
                                    lreply = "UNKNOWN";
                                    break;
                            }

                            break;
                        case ScriptBaseClass.DATA_SIM_RELEASE:
                            lreply = "OpenSim";
                            break;
                        default:
                            ScriptSleep(m_sleepMsOnRequestSimulatorData);
                            return ScriptBaseClass.NULL_KEY; // Raise no event
                    }

                    string ltid = m_AsyncCommands.DataserverPlugin.RequestWithImediatePost(m_host.LocalId,
                        m_item.ItemID, lreply);
                    ScriptSleep(m_sleepMsOnRequestSimulatorData);
                    return ltid;
                }

                Action<string> act = eventID =>
                {
                    GridRegion info = World.GridService.GetRegionByName(RegionScopeID, simulator);
                    var reply = "unknown";
                    if (info != null)
                        switch (data)
                        {
                            case ScriptBaseClass.DATA_SIM_POS:
                                // Hypergrid is currently placing real destination region co-ords into RegionSecret.
                                // But other code can also use this field for a genuine RegionSecret!  Therefore, if
                                // anything is present we need to disambiguate.
                                //
                                // FIXME: Hypergrid should be storing this data in a different field.
                                var regionFlags = (RegionFlags)m_ScriptEngine.World.GridService.GetRegionFlags(
                                    info.ScopeID, info.RegionID);

                                if ((regionFlags & RegionFlags.Hyperlink) != 0)
                                {
                                    Utils.LongToUInts(Convert.ToUInt64(info.RegionSecret), out var rx, out var ry);
                                    reply = new LSL_Vector(rx, ry, 0).ToString();
                                }
                                else
                                {
                                    // Local grid co-oridnates
                                    reply = new LSL_Vector(info.RegionLocX, info.RegionLocY, 0).ToString();
                                }

                                break;
                            case ScriptBaseClass.DATA_SIM_STATUS:
                                reply = "up"; // Duh!
                                break;
                            case ScriptBaseClass.DATA_SIM_RATING:
                                switch (info.Maturity)
                                {
                                    case 0:
                                        reply = "PG";
                                        break;
                                    case 1:
                                        reply = "MATURE";
                                        break;
                                    case 2:
                                        reply = "ADULT";
                                        break;
                                    default:
                                        reply = "UNKNOWN";
                                        break;
                                }

                                break;
                            case ScriptBaseClass.DATA_SIM_RELEASE:
                                reply = "OpenSim";
                                break;
                        }

                    m_AsyncCommands.DataserverPlugin.DataserverReply(eventID, reply);
                };

                UUID tid = m_AsyncCommands.DataserverPlugin.RegisterRequest(
                    m_host.LocalId, m_item.ItemID, act);

                ScriptSleep(m_sleepMsOnRequestSimulatorData);
                return tid.ToString();
            }
            catch (Exception)
            {
                //m_log.Error("[LSL_API]: llRequestSimulatorData" + e.ToString());
                return ScriptBaseClass.NULL_KEY;
            }
        }

        public LSL_Key llRequestURL()
        {
            if (m_UrlModule != null)
                return m_UrlModule.RequestURL(m_ScriptEngine.ScriptModule, m_host, m_item.ItemID, null).ToString();
            return ScriptBaseClass.NULL_KEY;
        }

        public void llForceMouselook(int mouselook)
        {
            m_host.SetForceMouselook(mouselook != 0);
        }

        public LSL_Float llGetObjectMass(LSL_Key id)
        {
            if (!UUID.TryParse(id, out var key) || key.IsZero())
                return 0;

            // return total object mass
            SceneObjectPart part = World.GetSceneObjectPart(key);
            if (part != null)
                return part.ParentGroup.GetMass();

            // the object is null so the key is for an avatar
            ScenePresence avatar = World.GetScenePresence(key);
            if (avatar != null)
            {
                if (avatar.IsChildAgent)
                    // reference http://www.lslwiki.net/lslwiki/wakka.php?wakka=llGetObjectMass
                    // child agents have a mass of 1.0
                    return 1;
                return avatar.GetMass();
            }

            return 0;
        }

        /// <summary>
        ///     llListReplaceList removes the sub-list defined by the inclusive indices
        ///     start and end and inserts the src list in its place. The inclusive
        ///     nature of the indices means that at least one element must be deleted
        ///     if the indices are within the bounds of the existing list. I.e. 2,2
        ///     will remove the element at index 2 and replace it with the source
        ///     list. Both indices may be negative, with the usual interpretation. An
        ///     interesting case is where end is lower than start. As these indices
        ///     bound the list to be removed, then 0->end, and start->lim are removed
        ///     and the source list is added as a suffix.
        /// </summary>
        public LSL_List llListReplaceList(LSL_List dest, LSL_List src, int start, int end)
        {
            LSL_List pref;


            // Note that although we have normalized, both
            // indices could still be negative.
            if (start < 0) start = start + dest.Length;

            if (end < 0) end = end + dest.Length;
            // The comventional case, remove a sequence starting with
            // start and ending with end. And then insert the source
            // list.
            if (start <= end)
            {
                // If greater than zero, then there is going to be a
                // surviving prefix. Otherwise the inclusive nature
                // of the indices mean that we're going to add the
                // source list as a prefix.
                if (start > 0)
                {
                    pref = dest.GetSublist(0, start - 1);
                    // Only add a suffix if there is something
                    // beyond the end index (it's inclusive too).
                    if (end + 1 < dest.Length)
                        return pref + src + dest.GetSublist(end + 1, -1);
                    return pref + src;
                }
                // If start is less than or equal to zero, then
                // the new list is simply a prefix. We still need to
                // figure out any necessary surgery to the destination
                // based upon end. Note that if end exceeds the upper
                // bound in this case, the entire destination list
                // is removed.

                if (start == 0)
                {
                    if (end + 1 < dest.Length)
                        return src + dest.GetSublist(end + 1, -1);
                    return src;
                }

                // Start < 0
                if (end + 1 < dest.Length)
                    return dest.GetSublist(end + 1, -1);
                return new LSL_List();
            }
            // Finally, if start > end, we strip away a prefix and
            // a suffix, to leave the list that sits <between> ens
            // and start, and then tag on the src list. AT least
            // that's my interpretation. We can get sublist to do
            // this for us. Note that one, or both of the indices
            // might have been negative.

            return dest.GetSublist(end + 1, start - 1) + src;
        }

        public void llLoadURL(string avatar_id, string message, string url)
        {
            if (m_host.OwnerID.Equals(m_host.GroupID))
                return;
            try
            {
                var m_checkuri = new Uri(url);
                if (m_checkuri.Scheme != Uri.UriSchemeHttp && m_checkuri.Scheme != Uri.UriSchemeHttps)
                {
                    Error("llLoadURL", "Invalid url schema");
                    ScriptSleep(200);
                    return;
                }
            }
            catch
            {
                Error("llLoadURL", "Invalid url");
                ScriptSleep(200);
                return;
            }

            IDialogModule dm = World.RequestModuleInterface<IDialogModule>();
            if (null != dm)
                dm.SendUrlToUser(
                    new UUID(avatar_id), m_host.Name, m_host.UUID, m_host.OwnerID, false, message, url);

            ScriptSleep(m_sleepMsOnLoadURL);
        }

        public void llParcelMediaCommandList(LSL_List commandList)
        {
            // TODO: Not implemented yet (missing in libomv?):
            //  PARCEL_MEDIA_COMMAND_LOOP_SET    float loop      Use this to get or set the parcel's media loop duration. (1.19.1 RC0 or later)


            // according to the docs, this command only works if script owner and land owner are the same
            // lets add estate owners and gods, too, and use the generic permission check.
            ILandObject landObject = World.LandChannel.GetLandObject(m_host.AbsolutePosition);
            if (!World.Permissions.CanEditParcelProperties(m_host.OwnerID, landObject, GroupPowers.ChangeMedia, false))
                return;

            var update = false; // send a ParcelMediaUpdate (and possibly change the land's media URL)?
            byte loop = 0;

            var landData = landObject.LandData;
            var url = landData.MediaURL;
            var texture = landData.MediaID.ToString();
            var autoAlign = landData.MediaAutoScale != 0;
            var mediaType = ""; // TODO these have to be added as soon as LandData supports it
            var description = "";
            var width = 0;
            var height = 0;

            ParcelMediaCommandEnum? commandToSend = null;
            var time = 0.0f; // default is from start

            uint cmndFlags = 0;
            ScenePresence presence = null;
            int cmd;
            for (var i = 0; i < commandList.Data.Length; i++)
            {
                if (commandList.Data[i] is LSL_Integer)
                    cmd = (LSL_Integer)commandList.Data[i];
                else
                    cmd = (int)commandList.Data[i];

                var command = (ParcelMediaCommandEnum)cmd;

                switch (command)
                {
                    case ParcelMediaCommandEnum.Agent:
                        // we send only to one agent
                        if (i + 1 < commandList.Length)
                        {
                            if (commandList.Data[i + 1] is LSL_String)
                            {
                                if (UUID.TryParse((LSL_String)commandList.Data[i + 1], out var agentID) &&
                                    agentID.IsNotZero())
                                {
                                    presence = World.GetScenePresence(agentID);
                                    if (presence == null || presence.IsNPC)
                                        return;
                                }
                            }
                            else
                            {
                                Error("llParcelMediaCommandList",
                                    "The argument of PARCEL_MEDIA_COMMAND_AGENT must be a key");
                            }

                            ++i;
                        }

                        break;

                    case ParcelMediaCommandEnum.Loop:
                        loop = 1;
                        cmndFlags |= 1 << ScriptBaseClass.PARCEL_MEDIA_COMMAND_LOOP;
                        commandToSend = command;
                        update = true; //need to send the media update packet to set looping
                        break;

                    case ParcelMediaCommandEnum.Play:
                        loop = 0;
                        cmndFlags |= 1 << ScriptBaseClass.PARCEL_MEDIA_COMMAND_PLAY;
                        commandToSend = command;
                        update = true; //need to send the media update packet to make sure it doesn't loop
                        break;

                    case ParcelMediaCommandEnum.Pause:
                        cmndFlags |= 1 << ScriptBaseClass.PARCEL_MEDIA_COMMAND_PAUSE;
                        commandToSend = command;
                        break;
                    case ParcelMediaCommandEnum.Stop:
                        cmndFlags |= 1 << ScriptBaseClass.PARCEL_MEDIA_COMMAND_STOP;
                        commandToSend = command;
                        break;
                    case ParcelMediaCommandEnum.Unload:
                        cmndFlags |= 1 << ScriptBaseClass.PARCEL_MEDIA_COMMAND_UNLOAD;
                        commandToSend = command;
                        break;

                    case ParcelMediaCommandEnum.Url:
                        if (i + 1 < commandList.Length)
                        {
                            if (commandList.Data[i + 1] is LSL_String)
                            {
                                url = (LSL_String)commandList.Data[i + 1];
                                if (string.IsNullOrWhiteSpace(url))
                                    url = string.Empty;
                                else
                                    try
                                    {
                                        var dummy = new Uri(url, UriKind.Absolute);
                                    }
                                    catch
                                    {
                                        Error("llParcelMediaCommandList", "invalid PARCEL_MEDIA_COMMAND_URL");
                                        return;
                                    }

                                update = true;
                            }
                            else
                            {
                                Error("llParcelMediaCommandList",
                                    "The argument of PARCEL_MEDIA_COMMAND_URL must be a string");
                            }

                            ++i;
                        }

                        break;

                    case ParcelMediaCommandEnum.Texture:
                        if (i + 1 < commandList.Length)
                        {
                            if (commandList.Data[i + 1] is LSL_String)
                            {
                                texture = (LSL_String)commandList.Data[i + 1];
                                update = true;
                            }
                            else
                            {
                                Error("llParcelMediaCommandList",
                                    "The argument of PARCEL_MEDIA_COMMAND_TEXTURE must be a string or a key");
                            }

                            ++i;
                        }

                        break;

                    case ParcelMediaCommandEnum.Time:
                        if (i + 1 < commandList.Length)
                        {
                            if (commandList.Data[i + 1] is LSL_Float)
                            {
                                time = (float)(LSL_Float)commandList.Data[i + 1];
                                cmndFlags |= 1 << ScriptBaseClass.PARCEL_MEDIA_COMMAND_TIME;
                            }
                            else
                            {
                                Error("llParcelMediaCommandList",
                                    "The argument of PARCEL_MEDIA_COMMAND_TIME must be a float");
                            }

                            ++i;
                        }

                        break;

                    case ParcelMediaCommandEnum.AutoAlign:
                        if (i + 1 < commandList.Length)
                        {
                            if (commandList.Data[i + 1] is LSL_Integer)
                            {
                                autoAlign = (LSL_Integer)commandList.Data[i + 1];
                                update = true;
                            }

                            else
                            {
                                Error("llParcelMediaCommandList",
                                    "The argument of PARCEL_MEDIA_COMMAND_AUTO_ALIGN must be an integer");
                            }

                            ++i;
                        }

                        break;

                    case ParcelMediaCommandEnum.Type:
                        if (i + 1 < commandList.Length)
                        {
                            if (commandList.Data[i + 1] is LSL_String)
                            {
                                mediaType = (LSL_String)commandList.Data[i + 1];
                                update = true;
                            }
                            else
                            {
                                Error("llParcelMediaCommandList",
                                    "The argument of PARCEL_MEDIA_COMMAND_TYPE must be a string");
                            }

                            ++i;
                        }

                        break;

                    case ParcelMediaCommandEnum.Desc:
                        if (i + 1 < commandList.Length)
                        {
                            if (commandList.Data[i + 1] is LSL_String)
                            {
                                description = (LSL_String)commandList.Data[i + 1];
                                update = true;
                            }
                            else
                            {
                                Error("llParcelMediaCommandList",
                                    "The argument of PARCEL_MEDIA_COMMAND_DESC must be a string");
                            }

                            ++i;
                        }

                        break;

                    case ParcelMediaCommandEnum.Size:
                        if (i + 2 < commandList.Length)
                        {
                            if (commandList.Data[i + 1] is LSL_Integer)
                            {
                                if (commandList.Data[i + 2] is LSL_Integer)
                                {
                                    width = (LSL_Integer)commandList.Data[i + 1];
                                    height = (LSL_Integer)commandList.Data[i + 2];
                                    update = true;
                                }
                                else
                                {
                                    Error("llParcelMediaCommandList",
                                        "The second argument of PARCEL_MEDIA_COMMAND_SIZE must be an integer");
                                }
                            }
                            else
                            {
                                Error("llParcelMediaCommandList",
                                    "The first argument of PARCEL_MEDIA_COMMAND_SIZE must be an integer");
                            }

                            i += 2;
                        }

                        break;

                    default:
                        NotImplemented("llParcelMediaCommandList",
                            "Parameter not supported yet: " + Enum.Parse(typeof(ParcelMediaCommandEnum),
                                commandList.Data[i].ToString()));
                        break;
                } //end switch
            } //end for

            // if we didn't get a presence, we send to all and change the url
            // if we did get a presence, we only send to the agent specified, and *don't change the land settings*!

            // did something important change or do we only start/stop/pause?
            if (update)
            {
                if (presence == null)
                {
                    // we send to all
                    landData.MediaID = new UUID(texture);
                    landData.MediaAutoScale = autoAlign ? (byte)1 : (byte)0;
                    landData.MediaWidth = width;
                    landData.MediaHeight = height;
                    landData.MediaType = mediaType;

                    // do that one last, it will cause a ParcelPropertiesUpdate
                    landObject.SetMediaUrl(url);

                    // now send to all (non-child) agents in the parcel
                    World.ForEachRootScenePresence(delegate(ScenePresence sp)
                    {
                        if (sp.currentParcelUUID.Equals(landData.GlobalID))
                            sp.ControllingClient.SendParcelMediaUpdate(landData.MediaURL,
                                landData.MediaID,
                                landData.MediaAutoScale,
                                mediaType,
                                description,
                                width, height,
                                loop);
                    });
                }
                else if (!presence.IsChildAgent)
                {
                    // we only send to one (root) agent
                    presence.ControllingClient.SendParcelMediaUpdate(url,
                        new UUID(texture),
                        autoAlign ? (byte)1 : (byte)0,
                        mediaType,
                        description,
                        width, height,
                        loop);
                }
            }

            if (commandToSend != null)
            {
                // the commandList contained a start/stop/... command, too
                if (presence == null)
                    // send to all (non-child) agents in the parcel
                    World.ForEachRootScenePresence(delegate(ScenePresence sp)
                    {
                        if (sp.currentParcelUUID.Equals(landData.GlobalID))
                            sp.ControllingClient.SendParcelMediaCommand(cmndFlags,
                                commandToSend.Value, time);
                    });
                else if (!presence.IsChildAgent)
                    presence.ControllingClient.SendParcelMediaCommand(cmndFlags,
                        commandToSend.Value, time);
            }

            ScriptSleep(m_sleepMsOnParcelMediaCommandList);
        }

        public LSL_List llParcelMediaQuery(LSL_List aList)
        {
            var list = new LSL_List();
            Vector3 pos = m_host.AbsolutePosition;

            ILandObject landObject = World.LandChannel.GetLandObject(pos);
            if (landObject == null)
                return list;

            if (!World.Permissions.CanEditParcelProperties(m_host.OwnerID, landObject, GroupPowers.ChangeMedia, false))
                return list;

            var land = landObject.LandData;
            if (land == null)
                return list;

            //TO DO: make the implementation for the missing commands
            //PARCEL_MEDIA_COMMAND_LOOP_SET    float loop      Use this to get or set the parcel's media loop duration. (1.19.1 RC0 or later)
            for (var i = 0; i < aList.Data.Length; i++)
                if (aList.Data[i] != null)
                    switch ((ParcelMediaCommandEnum)Convert.ToInt32(aList.Data[i].ToString()))
                    {
                        case ParcelMediaCommandEnum.Url:
                            list.Add(new LSL_String(land.MediaURL));
                            break;
                        case ParcelMediaCommandEnum.Desc:
                            list.Add(new LSL_String(land.MediaDescription));
                            break;
                        case ParcelMediaCommandEnum.Texture:
                            list.Add(new LSL_String(land.MediaID.ToString()));
                            break;
                        case ParcelMediaCommandEnum.Type:
                            list.Add(new LSL_String(land.MediaType));
                            break;
                        case ParcelMediaCommandEnum.Size:
                            list.Add(new LSL_String(land.MediaWidth));
                            list.Add(new LSL_String(land.MediaHeight));
                            break;
                        default:
                            var mediaCommandEnum = ParcelMediaCommandEnum.Url;
                            NotImplemented("llParcelMediaQuery",
                                "Parameter not supported yet: " + Enum.Parse(mediaCommandEnum.GetType(),
                                    aList.Data[i].ToString()));
                            break;
                    }

            ScriptSleep(m_sleepMsOnParcelMediaQuery);
            return list;
        }

        public LSL_Integer llModPow(int a, int b, int c)
        {
            Math.DivRem((long)Math.Pow(a, b), c, out var tmp);
            ScriptSleep(m_sleepMsOnModPow);
            return (int)tmp;
        }

        public LSL_Integer llGetInventoryType(string name)
        {
            TaskInventoryItem item = m_host.Inventory.GetInventoryItem(name);

            if (item == null)
                return -1;

            return item.Type;
        }

        public void llSetPayPrice(int price, LSL_List quick_pay_buttons)
        {
            if (m_host.LocalId != m_host.ParentGroup.RootPart.LocalId)
                return;

            if (quick_pay_buttons.Data.Length < 4)
            {
                int x;
                for (x = quick_pay_buttons.Data.Length; x <= 4; x++) quick_pay_buttons.Add(ScriptBaseClass.PAY_HIDE);
            }

            var nPrice = new int[5];
            nPrice[0] = price;
            nPrice[1] = quick_pay_buttons.GetLSLIntegerItem(0);
            nPrice[2] = quick_pay_buttons.GetLSLIntegerItem(1);
            nPrice[3] = quick_pay_buttons.GetLSLIntegerItem(2);
            nPrice[4] = quick_pay_buttons.GetLSLIntegerItem(3);
            m_host.ParentGroup.RootPart.PayPrice = nPrice;
            m_host.ParentGroup.HasGroupChanged = true;
        }

        public LSL_Vector llGetCameraPos()
        {
            if (m_item.PermsGranter.IsZero())
                return Vector3.Zero;

            if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_TRACK_CAMERA) == 0)
            {
                Error("llGetCameraPos", "No permissions to track the camera");
                return Vector3.Zero;
            }

//            ScenePresence presence = World.GetScenePresence(m_host.OwnerID);
            ScenePresence presence = World.GetScenePresence(m_item.PermsGranter);
            if (presence != null)
            {
                var pos = new LSL_Vector(presence.CameraPosition);
                return pos;
            }

            return Vector3.Zero;
        }

        public LSL_Rotation llGetCameraRot()
        {
            if (m_item.PermsGranter.IsZero())
                return Quaternion.Identity;

            if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_TRACK_CAMERA) == 0)
            {
                Error("llGetCameraRot", "No permissions to track the camera");
                return Quaternion.Identity;
            }

//            ScenePresence presence = World.GetScenePresence(m_host.OwnerID);
            ScenePresence presence = World.GetScenePresence(m_item.PermsGranter);
            if (presence != null) return new LSL_Rotation(presence.CameraRotation);

            return Quaternion.Identity;
        }

        public void llSetPrimURL(string url)
        {
            Deprecated("llSetPrimURL", "Use llSetPrimMediaParams instead");
            ScriptSleep(m_sleepMsOnSetPrimURL);
        }

        public void llRefreshPrimURL()
        {
            Deprecated("llRefreshPrimURL");
            ScriptSleep(m_sleepMsOnRefreshPrimURL);
        }

        public LSL_String llEscapeURL(string url)
        {
            try
            {
                return Uri.EscapeDataString(url);
            }
            catch (Exception ex)
            {
                return "llEscapeURL: " + ex;
            }
        }

        public LSL_String llUnescapeURL(string url)
        {
            try
            {
                return Uri.UnescapeDataString(url);
            }
            catch (Exception ex)
            {
                return "llUnescapeURL: " + ex;
            }
        }

        public void llMapDestination(string simname, LSL_Vector pos, LSL_Vector lookAt)
        {
            DetectParams detectedParams = m_ScriptEngine.GetDetectParams(m_item.ItemID, 0);
            if (detectedParams == null)
            {
                if (m_host.ParentGroup.IsAttachment == true)
                    detectedParams = new DetectParams
                    {
                        Key = m_host.OwnerID
                    };
                else
                    return;
            }

            ScenePresence avatar = World.GetScenePresence(detectedParams.Key);
            if (avatar != null)
                avatar.ControllingClient.SendScriptTeleportRequest(m_host.Name,
                    simname, pos, lookAt);
            ScriptSleep(m_sleepMsOnMapDestination);
        }

        public void llAddToLandBanList(LSL_Key avatar, LSL_Float hours)
        {
            if (!UUID.TryParse(avatar, out var key) || key.IsZero())
                return;

            ILandObject land = World.LandChannel.GetLandObject(m_host.AbsolutePosition);
            if (World.Permissions.CanEditParcelProperties(m_host.OwnerID, land, GroupPowers.LandManageBanned, false))
            {
                LandAccessEntry entry;
                var expires = hours != 0 ? Util.UnixTimeSinceEpoch() + (int)(3600.0 * hours) : 0;

                var idx = land.LandData.ParcelAccessList.FindIndex(
                    delegate(LandAccessEntry e)
                    {
                        if (e.Flags == AccessList.Ban && e.AgentID.Equals(key))
                            return true;
                        return false;
                    });

                if (idx != -1)
                {
                    entry = land.LandData.ParcelAccessList[idx];
                    if (entry.Expires == 0)
                        return;
                    if (expires != 0 && expires < entry.Expires)
                        return;

                    entry.Expires = expires;
                    World.EventManager.TriggerLandObjectUpdated((uint)land.LandData.LocalID, land);
                    return;
                }

                entry = new LandAccessEntry
                {
                    AgentID = key,
                    Flags = AccessList.Ban,
                    Expires = expires
                };

                land.LandData.ParcelAccessList.Add(entry);

                World.EventManager.TriggerLandObjectUpdated((uint)land.LandData.LocalID, land);
            }

            ScriptSleep(m_sleepMsOnAddToLandBanList);
        }

        public void llRemoveFromLandPassList(string avatar)
        {
            if (!UUID.TryParse(avatar, out var key) || key.IsZero())
                return;

            ILandObject land = World.LandChannel.GetLandObject(m_host.AbsolutePosition);
            if (World.Permissions.CanEditParcelProperties(m_host.OwnerID, land, GroupPowers.LandManagePasses, false))
            {
                var idx = land.LandData.ParcelAccessList.FindIndex(
                    delegate(LandAccessEntry e)
                    {
                        if (e.Flags == AccessList.Access && e.AgentID.Equals(key))
                            return true;
                        return false;
                    });

                if (idx != -1)
                {
                    land.LandData.ParcelAccessList.RemoveAt(idx);
                    World.EventManager.TriggerLandObjectUpdated((uint)land.LandData.LocalID, land);
                }
            }

            ScriptSleep(m_sleepMsOnRemoveFromLandPassList);
        }

        public void llRemoveFromLandBanList(string avatar)
        {
            if (!UUID.TryParse(avatar, out var key) || key.IsZero())
                return;

            ILandObject land = World.LandChannel.GetLandObject(m_host.AbsolutePosition);
            if (World.Permissions.CanEditParcelProperties(m_host.OwnerID, land, GroupPowers.LandManageBanned, false))
            {
                var idx = land.LandData.ParcelAccessList.FindIndex(
                    delegate(LandAccessEntry e)
                    {
                        if (e.Flags == AccessList.Ban && e.AgentID.Equals(key))
                            return true;
                        return false;
                    });

                if (idx != -1)
                {
                    land.LandData.ParcelAccessList.RemoveAt(idx);
                    World.EventManager.TriggerLandObjectUpdated((uint)land.LandData.LocalID, land);
                }
            }

            ScriptSleep(m_sleepMsOnRemoveFromLandBanList);
        }

        public void llSetCameraParams(LSL_List rules)
        {
            // the object we are in
            UUID objectID = m_host.ParentUUID;
            if (objectID.IsZero())
                return;

            // we need the permission first, to know which avatar we want to set the camera for
            UUID agentID = m_item.PermsGranter;

            if (agentID.IsZero())
                return;

            if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_CONTROL_CAMERA) == 0)
                return;

            ScenePresence presence = World.GetScenePresence(agentID);

            // we are not interested in child-agents
            if (presence.IsChildAgent) return;

            var parameters = new SortedDictionary<int, float>();
            var data = rules.Data;
            for (var i = 0; i < data.Length; ++i)
            {
                int type;
                try
                {
                    type = Convert.ToInt32(data[i++].ToString());
                }
                catch
                {
                    Error("llSetCameraParams", string.Format("Invalid camera param type {0}", data[i - 1]));
                    return;
                }

                if (i >= data.Length) break; // odd number of entries => ignore the last

                // some special cases: Vector parameters are split into 3 float parameters (with type+1, type+2, type+3)
                switch (type)
                {
                    case ScriptBaseClass.CAMERA_FOCUS:
                    case ScriptBaseClass.CAMERA_FOCUS_OFFSET:
                    case ScriptBaseClass.CAMERA_POSITION:
                        var v = (LSL_Vector)data[i];
                        try
                        {
                            parameters.Add(type + 1, (float)v.x);
                        }
                        catch
                        {
                            switch (type)
                            {
                                case ScriptBaseClass.CAMERA_FOCUS:
                                    Error("llSetCameraParams", "CAMERA_FOCUS: Parameter x is invalid");
                                    return;
                                case ScriptBaseClass.CAMERA_FOCUS_OFFSET:
                                    Error("llSetCameraParams", "CAMERA_FOCUS_OFFSET: Parameter x is invalid");
                                    return;
                                case ScriptBaseClass.CAMERA_POSITION:
                                    Error("llSetCameraParams", "CAMERA_POSITION: Parameter x is invalid");
                                    return;
                            }
                        }

                        try
                        {
                            parameters.Add(type + 2, (float)v.y);
                        }
                        catch
                        {
                            switch (type)
                            {
                                case ScriptBaseClass.CAMERA_FOCUS:
                                    Error("llSetCameraParams", "CAMERA_FOCUS: Parameter y is invalid");
                                    return;
                                case ScriptBaseClass.CAMERA_FOCUS_OFFSET:
                                    Error("llSetCameraParams", "CAMERA_FOCUS_OFFSET: Parameter y is invalid");
                                    return;
                                case ScriptBaseClass.CAMERA_POSITION:
                                    Error("llSetCameraParams", "CAMERA_POSITION: Parameter y is invalid");
                                    return;
                            }
                        }

                        try
                        {
                            parameters.Add(type + 3, (float)v.z);
                        }
                        catch
                        {
                            switch (type)
                            {
                                case ScriptBaseClass.CAMERA_FOCUS:
                                    Error("llSetCameraParams", "CAMERA_FOCUS: Parameter z is invalid");
                                    return;
                                case ScriptBaseClass.CAMERA_FOCUS_OFFSET:
                                    Error("llSetCameraParams", "CAMERA_FOCUS_OFFSET: Parameter z is invalid");
                                    return;
                                case ScriptBaseClass.CAMERA_POSITION:
                                    Error("llSetCameraParams", "CAMERA_POSITION: Parameter z is invalid");
                                    return;
                            }
                        }

                        break;
                    default:
                        // TODO: clean that up as soon as the implicit casts are in
                        if (data[i] is LSL_Float)
                            parameters.Add(type, (float)((LSL_Float)data[i]).value);
                        else if (data[i] is LSL_Integer)
                            parameters.Add(type, ((LSL_Integer)data[i]).value);
                        else
                            try
                            {
                                parameters.Add(type, Convert.ToSingle(data[i]));
                            }
                            catch
                            {
                                Error("llSetCameraParams", string.Format("{0}: Parameter is invalid", type));
                            }

                        break;
                }
            }

            if (parameters.Count > 0) presence.ControllingClient.SendSetFollowCamProperties(objectID, parameters);
        }

        public void llClearCameraParams()
        {
            // the object we are in
            UUID objectID = m_host.ParentUUID;
            if (objectID.IsZero())
                return;

            // we need the permission first, to know which avatar we want to clear the camera for
            UUID agentID = m_item.PermsGranter;

            if (agentID.IsZero())
                return;

            if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_CONTROL_CAMERA) == 0)
                return;

            ScenePresence presence = World.GetScenePresence(agentID);

            // we are not interested in child-agents
            if (presence.IsChildAgent)
                return;

            presence.ControllingClient.SendClearFollowCamProperties(objectID);
        }

        public LSL_Float llListStatistics(int operation, LSL_List src)
        {
            switch (operation)
            {
                case ScriptBaseClass.LIST_STAT_RANGE:
                    return src.Range();
                case ScriptBaseClass.LIST_STAT_MIN:
                    return src.Min();
                case ScriptBaseClass.LIST_STAT_MAX:
                    return src.Max();
                case ScriptBaseClass.LIST_STAT_MEAN:
                    return src.Mean();
                case ScriptBaseClass.LIST_STAT_MEDIAN:
                    return LSL_List.ToDoubleList(src).Median();
                case ScriptBaseClass.LIST_STAT_NUM_COUNT:
                    return src.NumericLength();
                case ScriptBaseClass.LIST_STAT_STD_DEV:
                    return src.StdDev();
                case ScriptBaseClass.LIST_STAT_SUM:
                    return src.Sum();
                case ScriptBaseClass.LIST_STAT_SUM_SQUARES:
                    return src.SumSqrs();
                case ScriptBaseClass.LIST_STAT_GEOMETRIC_MEAN:
                    return src.GeometricMean();
                case ScriptBaseClass.LIST_STAT_HARMONIC_MEAN:
                    return src.HarmonicMean();
                default:
                    return 0.0;
            }
        }

        public LSL_Integer llGetUnixTime()
        {
            return Util.UnixTimeSinceEpoch();
        }

        public LSL_Integer llGetParcelFlags(LSL_Vector pos)
        {
            return (int)World.LandChannel.GetLandObject((float)pos.x, (float)pos.y).LandData.Flags;
        }

        public LSL_Integer llGetRegionFlags()
        {
            IEstateModule estate = World.RequestModuleInterface<IEstateModule>();
            if (estate == null)
                return 67108864;
            return (int)estate.GetRegionFlags();
        }

        public LSL_String llXorBase64Strings(string str1, string str2)
        {
            var padding = 0;

            ScriptSleep(300);

            if (str1.Length == 0)
                return string.Empty;
            if (str2.Length == 0)
                return str1;

            var len = str2.Length;
            if (len % 4 != 0) // LL is EVIL!!!!
            {
                while (str2.EndsWith("="))
                    str2 = str2.Substring(0, str2.Length - 1);

                len = str2.Length;
                var mod = len % 4;

                if (mod == 1)
                    str2 = str2.Substring(0, str2.Length - 1);
                else if (mod == 2)
                    str2 += "==";
                else if (mod == 3)
                    str2 += "=";
            }

            byte[] data1;
            byte[] data2;
            try
            {
                data1 = Convert.FromBase64String(str1);
                data2 = Convert.FromBase64String(str2);
            }
            catch
            {
                return string.Empty;
            }

            // Remove padding
            while (str1.EndsWith("="))
            {
                str1 = str1.Substring(0, str1.Length - 1);
                padding++;
            }

            while (str2.EndsWith("="))
                str2 = str2.Substring(0, str2.Length - 1);

            var d1 = new byte[str1.Length];
            var d2 = new byte[str2.Length];

            for (var i = 0; i < str1.Length; i++)
            {
                var idx = b64.IndexOf(str1.Substring(i, 1));
                if (idx == -1)
                    idx = 0;
                d1[i] = (byte)idx;
            }

            for (var i = 0; i < str2.Length; i++)
            {
                var idx = b64.IndexOf(str2.Substring(i, 1));
                if (idx == -1)
                    idx = 0;
                d2[i] = (byte)idx;
            }

            var output = string.Empty;

            for (var pos = 0; pos < d1.Length; pos++)
                output += b64[d1[pos] ^ d2[pos % d2.Length]];

            // Here's a funny thing: LL blithely violate the base64
            // standard pretty much everywhere. Here, padding is
            // added only if the first input string had it, rather
            // than when the data actually needs it. This can result
            // in invalid base64 being returned. Go figure.

            while (padding-- > 0)
                output += "=";

            return output;
        }

        public LSL_String llXorBase64StringsCorrect(string str1, string str2)
        {
            if (str1.Length == 0)
                return string.Empty;
            if (str2.Length == 0)
                return str1;

            var len = str2.Length;
            if (len % 4 != 0) // LL is EVIL!!!!
            {
                str2.TrimEnd('=');

                len = str2.Length;
                if (len == 0)
                    return str1;

                var mod = len % 4;

                if (mod == 1)
                    str2 = str2.Substring(0, len - 1);
                else if (mod == 2)
                    str2 += "==";
                else if (mod == 3)
                    str2 += "=";
            }

            byte[] data1;
            byte[] data2;
            try
            {
                data1 = Convert.FromBase64String(str1);
                data2 = Convert.FromBase64String(str2);
            }
            catch (Exception)
            {
                return string.Empty;
            }

            var len2 = data2.Length;
            if (len2 == 0)
                return str1;

            for (int pos = 0, pos2 = 0; pos < data1.Length; pos++)
            {
                data1[pos] ^= data2[pos2];
                if (++pos2 >= len2)
                    pos2 = 0;
            }

            return Convert.ToBase64String(data1);
        }

        public LSL_String llXorBase64(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str2))
                return str1;

            str1 = truncateBase64(str1);
            if (string.IsNullOrEmpty(str1))
                return string.Empty;

            str2 = truncateBase64(str2);
            if (string.IsNullOrEmpty(str2))
                return str1;

            byte[] data1;
            byte[] data2;
            try
            {
                data1 = Convert.FromBase64String(str1);
                data2 = Convert.FromBase64String(str2);
            }
            catch (Exception)
            {
                return string.Empty;
            }

            var len2 = data2.Length;
            if (len2 == 0)
                return str1;

            for (int pos = 0, pos2 = 0; pos < data1.Length; pos++)
            {
                data1[pos] ^= data2[pos2];
                if (++pos2 >= len2)
                    pos2 = 0;
            }

            return Convert.ToBase64String(data1);
        }

        public LSL_Key llHTTPRequest(string url, LSL_List parameters, string body)
        {
            IHttpRequestModule httpScriptMod = m_ScriptEngine.World.RequestModuleInterface<IHttpRequestModule>();
            if (httpScriptMod == null)
                return string.Empty;

            if (!httpScriptMod.CheckThrottle(m_host.LocalId, m_host.OwnerID))
                return ScriptBaseClass.NULL_KEY;

            try
            {
                var m_checkuri = new Uri(url);
                if (m_checkuri.Scheme != Uri.UriSchemeHttp && m_checkuri.Scheme != Uri.UriSchemeHttps)
                {
                    Error("llHTTPRequest", "Invalid url schema");
                    return string.Empty;
                }
            }
            catch
            {
                Error("llHTTPRequest", "Invalid url");
                return string.Empty;
            }

            var param = new List<string>();
            bool ok;
            var nCustomHeaders = 0;

            for (var i = 0; i < parameters.Data.Length; i += 2)
            {
                ok = int.TryParse(parameters.Data[i].ToString(), out var flag);
                if (!ok || flag < 0 ||
                    flag > (int)HttpRequestConstants.HTTP_PRAGMA_NO_CACHE)
                {
                    Error("llHTTPRequest", "Parameter " + i + " is an invalid flag");
                    ScriptSleep(200);
                    return string.Empty;
                }

                param.Add(parameters.Data[i].ToString()); //Add parameter flag

                if (flag != (int)HttpRequestConstants.HTTP_CUSTOM_HEADER)
                {
                    param.Add(parameters.Data[i + 1].ToString()); //Add parameter value
                }
                else
                {
                    //Parameters are in pairs and custom header takes
                    //arguments in pairs so adjust for header marker.
                    ++i;

                    //Maximum of 8 headers are allowed based on the
                    //Second Life documentation for llHTTPRequest.
                    for (var count = 1; count <= 8; ++count)
                    {
                        if (nCustomHeaders >= 8)
                        {
                            Error("llHTTPRequest", "Max number of custom headers is 8, excess ignored");
                            break;
                        }

                        //Enough parameters remaining for (another) header?
                        if (parameters.Data.Length - i < 2)
                        {
                            //There must be at least one name/value pair for custom header
                            if (count == 1)
                                Error("llHTTPRequest", "Missing name/value for custom header at parameter " + i);
                            return string.Empty;
                        }

                        var paramName = parameters.Data[i].ToString();

                        var paramNamelwr = paramName.ToLower();
                        if (paramNamelwr.StartsWith("proxy-"))
                        {
                            Error("llHTTPRequest", "Name is invalid as a custom header at parameter " + i);
                            return string.Empty;
                        }

                        if (paramNamelwr.StartsWith("sec-"))
                        {
                            Error("llHTTPRequest", "Name is invalid as a custom header at parameter " + i);
                            return string.Empty;
                        }

                        var noskip = true;
                        if (HttpForbiddenHeaders.TryGetValue(paramName, out bool fatal))
                        {
                            if (fatal)
                            {
                                Error("llHTTPRequest", "Name is invalid as a custom header at parameter " + i);
                                return string.Empty;
                            }

                            noskip = false;
                        }

                        var paramValue = parameters.Data[i + 1].ToString();
                        if (paramName.Length + paramValue.Length > 253)
                        {
                            Error("llHTTPRequest",
                                "name and value length exceds 253 characters for custom header at parameter " + i);
                            return string.Empty;
                        }

                        if (noskip)
                        {
                            param.Add(paramName);
                            param.Add(paramValue);
                            nCustomHeaders++;
                        }

                        //Have we reached the end of the list of headers?
                        //End is marked by a string with a single digit.
                        if (i + 2 >= parameters.Data.Length ||
                            char.IsDigit(parameters.Data[i + 2].ToString()[0]))
                            break;

                        i += 2;
                    }
                }
            }

            Vector3 position = m_host.AbsolutePosition;
            Vector3 velocity = m_host.Velocity;
            Quaternion rotation = m_host.GetWorldRotation();

            var ownerName = string.Empty;
            ScenePresence scenePresence = World.GetScenePresence(m_host.OwnerID);
            if (scenePresence == null)
                ownerName = resolveName(m_host.OwnerID);
            else
                ownerName = scenePresence.Name;

            RegionInfo regionInfo = World.RegionInfo;

            var httpHeaders = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(m_lsl_shard))
                httpHeaders["X-SecondLife-Shard"] = m_lsl_shard;
            httpHeaders["X-SecondLife-Object-Name"] = m_host.Name;
            httpHeaders["X-SecondLife-Object-Key"] = m_host.UUID.ToString();
            httpHeaders["X-SecondLife-Region"] = string.Format("{0} ({1}, {2})", regionInfo.RegionName,
                regionInfo.WorldLocX, regionInfo.WorldLocY);
            httpHeaders["X-SecondLife-Local-Position"] = string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000})",
                position.X, position.Y, position.Z);
            httpHeaders["X-SecondLife-Local-Velocity"] = string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000})",
                velocity.X, velocity.Y, velocity.Z);
            httpHeaders["X-SecondLife-Local-Rotation"] = string.Format(
                "({0:0.000000}, {1:0.000000}, {2:0.000000}, {3:0.000000})", rotation.X, rotation.Y, rotation.Z,
                rotation.W);
            httpHeaders["X-SecondLife-Owner-Name"] = ownerName;
            httpHeaders["X-SecondLife-Owner-Key"] = m_host.OwnerID.ToString();
            if (!string.IsNullOrWhiteSpace(m_lsl_user_agent))
                httpHeaders["User-Agent"] = m_lsl_user_agent;

            // See if the URL contains any header hacks
            var urlParts = url.Split('\n');
            if (urlParts.Length > 1)
            {
                // Iterate the passed headers and parse them
                for (var i = 1; i < urlParts.Length; i++)
                {
                    // The rest of those would be added to the body in SL.
                    // Let's not do that.
                    if (urlParts[i].Length == 0)
                        break;

                    // See if this could be a valid header
                    var headerParts = urlParts[i].Split(new[] { ':' }, 2);
                    if (headerParts.Length != 2)
                        continue;

                    var headerName = headerParts[0].Trim();
                    if (!HttpForbiddenInHeaders.Contains(headerName))
                    {
                        var headerValue = headerParts[1].Trim();
                        httpHeaders[headerName] = headerValue;
                    }
                }

                // Finally, strip any protocol specifier from the URL
                url = urlParts[0].Trim();
                var idx = url.IndexOf(" HTTP/");
                if (idx != -1)
                    url = url.Substring(0, idx);
            }

            var authregex = @"^(https?:\/\/)(\w+):(\w+)@(.*)$";
            var r = new Regex(authregex);
            var gnums = r.GetGroupNumbers();
            var m = r.Match(url);
            if (m.Success)
                //for (int i = 1; i < gnums.Length; i++)
                //{
                //System.Text.RegularExpressions.Group g = m.Groups[gnums[i]];
                //CaptureCollection cc = g.Captures;
                //}
                if (m.Groups.Count == 5)
                {
                    httpHeaders["Authorization"] = string.Format("Basic {0}",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(m.Groups[2] + ":" + m.Groups[3])));
                    url = m.Groups[1] + m.Groups[4].ToString();
                }

            var reqID = httpScriptMod.StartHttpRequest(m_host.LocalId, m_item.ItemID, url, param, httpHeaders, body,
                out var status);

            if (status == HttpInitialRequestStatus.DISALLOWED_BY_FILTER)
                Error("llHttpRequest", string.Format("Request to {0} disallowed by filter", url));

            return reqID.IsZero() ? "" : reqID.ToString();
        }


        public void llHTTPResponse(LSL_Key id, int status, string body)
        {
            // Partial implementation: support for parameter flags needed
            //   see http://wiki.secondlife.com/wiki/llHTTPResponse


            if (m_UrlModule != null)
                m_UrlModule.HttpResponse(new UUID(id), status, body);
        }

        public void llResetLandBanList()
        {
            LandData land = World.LandChannel.GetLandObject(m_host.AbsolutePosition).LandData;
            if (land.ParcelAccessList.Count > 0 && land.OwnerID.Equals(m_host.OwnerID))
            {
                var todelete = new List<LandAccessEntry>();
                foreach (var entry in land.ParcelAccessList)
                    if (entry.Flags == AccessList.Ban)
                        todelete.Add(entry);
                foreach (var entry in todelete)
                    land.ParcelAccessList.Remove(entry);
            }

            ScriptSleep(m_sleepMsOnResetLandBanList);
        }

        public void llResetLandPassList()
        {
            LandData land = World.LandChannel.GetLandObject(m_host.AbsolutePosition).LandData;
            if (land.ParcelAccessList.Count > 0 && land.OwnerID.Equals(m_host.OwnerID))
            {
                var todelete = new List<LandAccessEntry>();
                foreach (var entry in land.ParcelAccessList)
                    if (entry.Flags == AccessList.Access)
                        todelete.Add(entry);
                foreach (var entry in todelete)
                    land.ParcelAccessList.Remove(entry);
            }

            ScriptSleep(m_sleepMsOnResetLandPassList);
        }

        public LSL_Integer llGetParcelPrimCount(LSL_Vector pos, int category, int sim_wide)
        {
            ILandObject lo = World.LandChannel.GetLandObject((float)pos.x, (float)pos.y);

            if (lo == null)
                return 0;

            var pc = lo.PrimCounts;

            if (sim_wide != ScriptBaseClass.FALSE)
            {
                if (category == ScriptBaseClass.PARCEL_COUNT_TOTAL)
                    return pc.Simulator;
                // counts not implemented yet
                return 0;
            }

            if (category == ScriptBaseClass.PARCEL_COUNT_TOTAL)
                return pc.Total;
            if (category == ScriptBaseClass.PARCEL_COUNT_OWNER)
                return pc.Owner;
            if (category == ScriptBaseClass.PARCEL_COUNT_GROUP)
                return pc.Group;
            if (category == ScriptBaseClass.PARCEL_COUNT_OTHER)
                return pc.Others;
            if (category == ScriptBaseClass.PARCEL_COUNT_SELECTED)
                return pc.Selected;
            if (category == ScriptBaseClass.PARCEL_COUNT_TEMP)
                return 0; // counts not implemented yet

            return 0;
        }

        public LSL_List llGetParcelPrimOwners(LSL_Vector pos)
        {
            var land = (LandObject)World.LandChannel.GetLandObject((float)pos.x, (float)pos.y);
            var ret = new LSL_List();
            if (land != null)
                foreach (var detectedParams in land.GetLandObjectOwners())
                {
                    ret.Add(new LSL_String(detectedParams.Key.ToString()));
                    ret.Add(new LSL_Integer(detectedParams.Value));
                }

            ScriptSleep(m_sleepMsOnGetParcelPrimOwners);
            return ret;
        }

        public LSL_Integer llGetObjectPrimCount(LSL_Key object_id)
        {
            if (!UUID.TryParse(object_id, out var id) || id.IsZero())
                return 0;

            SceneObjectPart part = World.GetSceneObjectPart(id);
            if (part == null)
                return 0;

            return part.ParentGroup.PrimCount;
        }

        public LSL_Integer llGetParcelMaxPrims(LSL_Vector pos, int sim_wide)
        {
            ILandObject lo = World.LandChannel.GetLandObject((float)pos.x, (float)pos.y);

            if (lo == null)
                return 0;

            if (sim_wide != 0)
                return lo.GetSimulatorMaxPrimCount();
            return lo.GetParcelMaxPrimCount();
        }

        public LSL_List llGetParcelDetails(LSL_Vector pos, LSL_List param)
        {
            ILandObject parcel = World.LandChannel.GetLandObject(pos);
            if (parcel == null) return new LSL_List(0);

            var land = parcel.LandData;
            if (land == null) return new LSL_List(0);

            var ret = new LSL_List();
            foreach (var o in param.Data)
                switch (o.ToString())
                {
                    case "0":
                        ret.Add(new LSL_String(land.Name));
                        break;
                    case "1":
                        ret.Add(new LSL_String(land.Description));
                        break;
                    case "2":
                        ret.Add(new LSL_Key(land.OwnerID.ToString()));
                        break;
                    case "3":
                        ret.Add(new LSL_Key(land.GroupID.ToString()));
                        break;
                    case "4":
                        ret.Add(new LSL_Integer(land.Area));
                        break;
                    case "5":
                        ret.Add(new LSL_Key(land.GlobalID.ToString()));
                        break;
                    case "6":
                        ret.Add(new LSL_Integer(land.SeeAVs ? 1 : 0));
                        break;
                    case "7":
                        ret.Add(new LSL_Integer(parcel.GetParcelMaxPrimCount()));
                        break;
                    case "8":
                        ret.Add(new LSL_Integer(parcel.PrimCounts.Total));
                        break;
                    case "9":
                        ret.Add(new LSL_Vector(land.UserLocation));
                        break;
                    case "10":
                        ret.Add(new LSL_Vector(land.UserLookAt));
                        break;
                    case "11":
                        ret.Add(new LSL_Integer(land.LandingType));
                        break;
                    case "12":
                        ret.Add(new LSL_Integer(land.Flags));
                        break;
                    case "13":
                        ret.Add(new LSL_Integer(World.LSLScriptDanger(m_host, pos) ? 1 : 0));
                        break;
                    case "64":
                        ret.Add(new LSL_Integer(land.Dwell));
                        break;
                    case "65":
                        ret.Add(new LSL_Integer(land.ClaimDate));
                        break;
                    default:
                        ret.Add(new LSL_Integer(0));
                        break;
                }

            return ret;
        }

        public LSL_String llStringTrim(LSL_String src, LSL_Integer type)
        {
            if (type == ScriptBaseClass.STRING_TRIM_HEAD) return ((string)src).TrimStart();
            if (type == ScriptBaseClass.STRING_TRIM_TAIL) return ((string)src).TrimEnd();
            if (type == ScriptBaseClass.STRING_TRIM) return ((string)src).Trim();
            return src;
        }

        public LSL_List llGetObjectDetails(LSL_Key id, LSL_List args)
        {
            var ret = new LSL_List();
            if (!UUID.TryParse(id, out var key) || key.IsZero())
                return ret;

            var count = 0;
            ScenePresence av = World.GetScenePresence(key);
            if (av != null)
            {
                List<SceneObjectGroup> Attachments = null;
                int? nAnimated = null;
                foreach (var o in args.Data)
                    switch (int.Parse(o.ToString()))
                    {
                        case ScriptBaseClass.OBJECT_NAME:
                            ret.Add(new LSL_String(av.Firstname + " " + av.Lastname));
                            break;
                        case ScriptBaseClass.OBJECT_DESC:
                            ret.Add(new LSL_String(""));
                            break;
                        case ScriptBaseClass.OBJECT_POS:
                            Vector3 avpos;

                            if (av.ParentID != 0 && av.ParentPart != null &&
                                av.ParentPart.ParentGroup != null && av.ParentPart.ParentGroup.RootPart != null)
                            {
                                avpos = av.OffsetPosition;

                                if (!av.LegacySitOffsets)
                                {
                                    var sitOffset = Zrot(av.Rotation) * (av.Appearance.AvatarHeight * 0.02638f * 2.0f);
                                    avpos -= sitOffset;
                                }

                                var sitRoot = av.ParentPart.ParentGroup.RootPart;
                                avpos = sitRoot.GetWorldPosition() + avpos * sitRoot.GetWorldRotation();
                            }
                            else
                            {
                                avpos = av.AbsolutePosition;
                            }

                            ret.Add(new LSL_Vector(avpos.X, avpos.Y, avpos.Z));
                            break;
                        case ScriptBaseClass.OBJECT_ROT:
                            var avrot = av.GetWorldRotation();
                            ret.Add(new LSL_Rotation(avrot));
                            break;
                        case ScriptBaseClass.OBJECT_VELOCITY:
                            var avvel = av.GetWorldVelocity();
                            ret.Add(new LSL_Vector(avvel.X, avvel.Y, avvel.Z));
                            break;
                        case ScriptBaseClass.OBJECT_OWNER:
                            ret.Add(new LSL_Key((string)id));
                            break;
                        case ScriptBaseClass.OBJECT_GROUP:
                            ret.Add(new LSL_String(ScriptBaseClass.NULL_KEY));
                            break;
                        case ScriptBaseClass.OBJECT_CREATOR:
                            ret.Add(new LSL_Key(ScriptBaseClass.NULL_KEY));
                            break;
                        // For the following 8 see the Object version below
                        case ScriptBaseClass.OBJECT_RUNNING_SCRIPT_COUNT:
                            ret.Add(new LSL_Integer(av.RunningScriptCount()));
                            break;
                        case ScriptBaseClass.OBJECT_TOTAL_SCRIPT_COUNT:
                            ret.Add(new LSL_Integer(av.ScriptCount()));
                            break;
                        case ScriptBaseClass.OBJECT_SCRIPT_MEMORY:
                            ret.Add(new LSL_Integer(av.RunningScriptCount() * 16384));
                            break;
                        case ScriptBaseClass.OBJECT_SCRIPT_TIME:
                            ret.Add(new LSL_Float(av.ScriptExecutionTime() / 1000.0f));
                            break;
                        case ScriptBaseClass.OBJECT_PRIM_EQUIVALENCE:
                            ret.Add(new LSL_Integer(1));
                            break;
                        case ScriptBaseClass.OBJECT_SERVER_COST:
                            ret.Add(new LSL_Float(0));
                            break;
                        case ScriptBaseClass.OBJECT_STREAMING_COST:
                            ret.Add(new LSL_Float(0));
                            break;
                        case ScriptBaseClass.OBJECT_PHYSICS_COST:
                            ret.Add(new LSL_Float(0));
                            break;
                        case ScriptBaseClass.OBJECT_CHARACTER_TIME: // Pathfinding
                            ret.Add(new LSL_Float(0));
                            break;
                        case ScriptBaseClass.OBJECT_ROOT:
                            var p = av.ParentPart;
                            if (p != null)
                                ret.Add(new LSL_String(p.ParentGroup.RootPart.UUID.ToString()));
                            else
                                ret.Add(new LSL_Key((string)id));
                            break;
                        case ScriptBaseClass.OBJECT_ATTACHED_POINT:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_PATHFINDING_TYPE: // Pathfinding
                            ret.Add(new LSL_Integer(ScriptBaseClass.OPT_AVATAR));
                            break;
                        case ScriptBaseClass.OBJECT_PHYSICS:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_PHANTOM:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_TEMP_ON_REZ:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_RENDER_WEIGHT:
                            ret.Add(new LSL_Integer(-1));
                            break;
                        case ScriptBaseClass.OBJECT_HOVER_HEIGHT:
                            ret.Add(new LSL_Float(0));
                            break;
                        case ScriptBaseClass.OBJECT_BODY_SHAPE_TYPE:
                            LSL_Float shapeType;
                            if (av.Appearance.VisualParams[(int)AvatarAppearance.VPElement.SHAPE_MALE] != 0)
                                shapeType = new LSL_Float(1);
                            else
                                shapeType = new LSL_Float(0);
                            ret.Add(shapeType);
                            break;
                        case ScriptBaseClass.OBJECT_LAST_OWNER_ID:
                            ret.Add(new LSL_Key(ScriptBaseClass.NULL_KEY));
                            break;
                        case ScriptBaseClass.OBJECT_CLICK_ACTION:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_OMEGA:
                            ret.Add(new LSL_Vector(Vector3.Zero));
                            break;
                        case ScriptBaseClass.OBJECT_PRIM_COUNT:
                            if (Attachments == null)
                                Attachments = av.GetAttachments();
                            count = 0;
                            try
                            {
                                foreach (var Attachment in Attachments)
                                    count += Attachment.PrimCount;
                            }
                            catch
                            {
                            }

                            ;
                            ret.Add(new LSL_Integer(count));
                            break;
                        case ScriptBaseClass.OBJECT_TOTAL_INVENTORY_COUNT:
                            if (Attachments == null)
                                Attachments = av.GetAttachments();
                            count = 0;
                            try
                            {
                                foreach (var Attachment in Attachments)
                                {
                                    var parts = Attachment.Parts;
                                    for (var i = 0; i < parts.Length; i++)
                                        count += parts[i].Inventory.Count;
                                }
                            }
                            catch
                            {
                            }

                            ;
                            ret.Add(new LSL_Integer(count));
                            break;
                        case ScriptBaseClass.OBJECT_REZZER_KEY:
                            ret.Add(new LSL_Key((string)id));
                            break;
                        case ScriptBaseClass.OBJECT_GROUP_TAG:
                            ret.Add(new LSL_String(av.Grouptitle));
                            break;
                        case ScriptBaseClass.OBJECT_TEMP_ATTACHED:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_ATTACHED_SLOTS_AVAILABLE:
                            ret.Add(new LSL_Integer(Constants.MaxAgentAttachments - av.GetAttachmentsCount()));
                            break;
                        case ScriptBaseClass.OBJECT_CREATION_TIME:
                            ret.Add(new LSL_String(""));
                            break;
                        case ScriptBaseClass.OBJECT_SELECT_COUNT:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_SIT_COUNT:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_ANIMATED_COUNT:
                            count = 0;
                            if (nAnimated.HasValue)
                            {
                                count = nAnimated.Value;
                            }
                            else
                            {
                                if (Attachments == null)
                                    Attachments = av.GetAttachments();
                                try
                                {
                                    for (var i = 0; i < Attachments.Count; ++i)
                                        if (Attachments[i].RootPart.Shape.MeshFlagEntry)
                                            ++count;
                                }
                                catch
                                {
                                }

                                ;
                                nAnimated = count;
                            }

                            ret.Add(new LSL_Integer(count));
                            break;

                        case ScriptBaseClass.OBJECT_ANIMATED_SLOTS_AVAILABLE:
                            count = 0;
                            if (nAnimated.HasValue)
                            {
                                count = nAnimated.Value;
                            }
                            else
                            {
                                if (Attachments == null)
                                    Attachments = av.GetAttachments();
                                count = 0;
                                try
                                {
                                    for (var i = 0; i < Attachments.Count; ++i)
                                        if (Attachments[i].RootPart.Shape.MeshFlagEntry)
                                            ++count;
                                }
                                catch
                                {
                                }

                                ;
                                nAnimated = count;
                            }

                            count = 2 - count; // for now hardcoded max (simulator features, viewers settings, etc)
                            if (count < 0)
                                count = 0;
                            ret.Add(new LSL_Integer(count));
                            break;

                        case ScriptBaseClass.OBJECT_ACCOUNT_LEVEL:
                            ret.Add(new LSL_Integer(1));
                            break;
                        case ScriptBaseClass.OBJECT_MATERIAL:
                            ret.Add(new LSL_Integer((int)Material.Flesh));
                            break;
                        case ScriptBaseClass.OBJECT_MASS:
                            ret.Add(new LSL_Float(av.GetMass()));
                            break;
                        case ScriptBaseClass.OBJECT_TEXT:
                            ret.Add(new LSL_String(""));
                            break;
                        case ScriptBaseClass.OBJECT_REZ_TIME:
                            ret.Add(new LSL_String(""));
                            break;
                        case ScriptBaseClass.OBJECT_LINK_NUMBER:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_SCALE:
                            ret.Add(new LSL_Vector(av.Appearance.AvatarBoxSize));
                            break;
                        case ScriptBaseClass.OBJECT_TEXT_COLOR:
                            ret.Add(new LSL_Vector(0f, 0f, 0f));
                            break;
                        case ScriptBaseClass.OBJECT_TEXT_ALPHA:
                            ret.Add(new LSL_Float(1.0f));
                            break;
                        default:
                            // Invalid or unhandled constant.
                            ret.Add(new LSL_Integer(ScriptBaseClass.OBJECT_UNKNOWN_DETAIL));
                            break;
                    }

                return ret;
            }

            SceneObjectPart obj = World.GetSceneObjectPart(key);
            if (obj != null)
                foreach (var o in args.Data)
                    switch (int.Parse(o.ToString()))
                    {
                        case ScriptBaseClass.OBJECT_NAME:
                            ret.Add(new LSL_String(obj.Name));
                            break;
                        case ScriptBaseClass.OBJECT_DESC:
                            ret.Add(new LSL_String(obj.Description));
                            break;
                        case ScriptBaseClass.OBJECT_POS:
                            ret.Add(new LSL_Vector(obj.AbsolutePosition));
                            break;
                        case ScriptBaseClass.OBJECT_ROT:
                            Quaternion rot;

                            if (obj.ParentGroup.IsAttachment)
                            {
                                ScenePresence sp = World.GetScenePresence(obj.ParentGroup.AttachedAvatar);
                                rot = sp != null ? sp.GetWorldRotation() : Quaternion.Identity;
                            }
                            else
                            {
                                if (obj.ParentGroup.RootPart.LocalId == obj.LocalId)
                                    rot = obj.ParentGroup.GroupRotation;
                                else
                                    rot = obj.GetWorldRotation();
                            }

                            var objrot = new LSL_Rotation(rot);
                            ret.Add(objrot);

                            break;
                        case ScriptBaseClass.OBJECT_VELOCITY:
                            Vector3 vel;

                            if (obj.ParentGroup.IsAttachment)
                            {
                                ScenePresence sp = World.GetScenePresence(obj.ParentGroup.AttachedAvatar);
                                vel = sp != null ? sp.GetWorldVelocity() : Vector3.Zero;
                            }
                            else
                            {
                                vel = obj.Velocity;
                            }

                            ret.Add(new LSL_Vector(vel));
                            break;
                        case ScriptBaseClass.OBJECT_OWNER:
                            ret.Add(new LSL_String(obj.OwnerID.ToString()));
                            break;
                        case ScriptBaseClass.OBJECT_GROUP:
                            ret.Add(new LSL_String(obj.GroupID.ToString()));
                            break;
                        case ScriptBaseClass.OBJECT_CREATOR:
                            ret.Add(new LSL_String(obj.CreatorID.ToString()));
                            break;
                        case ScriptBaseClass.OBJECT_RUNNING_SCRIPT_COUNT:
                            ret.Add(new LSL_Integer(obj.ParentGroup.RunningScriptCount()));
                            break;
                        case ScriptBaseClass.OBJECT_TOTAL_SCRIPT_COUNT:
                            ret.Add(new LSL_Integer(obj.ParentGroup.ScriptCount()));
                            break;
                        case ScriptBaseClass.OBJECT_SCRIPT_MEMORY:
                            // The value returned in SL for mono scripts is 65536 * number of active scripts
                            // and 16384 * number of active scripts for LSO. since llGetFreememory
                            // is coded to give the LSO value use it here
                            ret.Add(new LSL_Integer(obj.ParentGroup.RunningScriptCount() * 16384));
                            break;
                        case ScriptBaseClass.OBJECT_SCRIPT_TIME:
                            // Average cpu time in seconds per simulator frame expended on all scripts in the object
                            ret.Add(new LSL_Float(obj.ParentGroup.ScriptExecutionTime() / 1000.0f));
                            break;
                        case ScriptBaseClass.OBJECT_PRIM_EQUIVALENCE:
                            // according to the SL wiki A prim or linkset will have prim
                            // equivalent of the number of prims in a linkset if it does not
                            // contain a mesh anywhere in the link set or is not a normal prim
                            // The value returned in SL for normal prims is prim count
                            ret.Add(new LSL_Integer(obj.ParentGroup.PrimCount));
                            break;

                        // costs below may need to be diferent for root parts, need to check
                        case ScriptBaseClass.OBJECT_SERVER_COST:
                            // The linden calculation is here
                            // http://wiki.secondlife.com/wiki/Mesh/Mesh_Server_Weight
                            // The value returned in SL for normal prims looks like the prim count
                            ret.Add(new LSL_Float(0));
                            break;
                        case ScriptBaseClass.OBJECT_STREAMING_COST:
                            // The value returned in SL for normal prims is prim count * 0.06
                            ret.Add(new LSL_Float(obj.StreamingCost));
                            break;
                        case ScriptBaseClass.OBJECT_PHYSICS_COST:
                            // The value returned in SL for normal prims is prim count
                            ret.Add(new LSL_Float(obj.PhysicsCost));
                            break;
                        case ScriptBaseClass.OBJECT_CHARACTER_TIME: // Pathfinding
                            ret.Add(new LSL_Float(0));
                            break;
                        case ScriptBaseClass.OBJECT_ROOT:
                            ret.Add(new LSL_String(obj.ParentGroup.RootPart.UUID.ToString()));
                            break;
                        case ScriptBaseClass.OBJECT_ATTACHED_POINT:
                            ret.Add(new LSL_Integer(obj.ParentGroup.AttachmentPoint));
                            break;
                        case ScriptBaseClass.OBJECT_PATHFINDING_TYPE:
                            var pcode = obj.Shape.PCode;
                            if (obj.ParentGroup.AttachmentPoint != 0
                                || pcode == (byte)PCode.Grass
                                || pcode == (byte)PCode.Tree
                                || pcode == (byte)PCode.NewTree)
                                ret.Add(new LSL_Integer(ScriptBaseClass.OPT_OTHER));
                            else
                                ret.Add(new LSL_Integer(ScriptBaseClass.OPT_LEGACY_LINKSET));
                            break;
                        case ScriptBaseClass.OBJECT_PHYSICS:
                            if (obj.ParentGroup.AttachmentPoint != 0)
                                ret.Add(new LSL_Integer(0)); // Always false if attached
                            else
                                ret.Add(new LSL_Integer(obj.ParentGroup.UsesPhysics ? 1 : 0));
                            break;
                        case ScriptBaseClass.OBJECT_PHANTOM:
                            if (obj.ParentGroup.AttachmentPoint != 0)
                                ret.Add(new LSL_Integer(0)); // Always false if attached
                            else
                                ret.Add(new LSL_Integer(obj.ParentGroup.IsPhantom ? 1 : 0));
                            break;
                        case ScriptBaseClass.OBJECT_TEMP_ON_REZ:
                            ret.Add(new LSL_Integer(obj.ParentGroup.IsTemporary ? 1 : 0));
                            break;
                        case ScriptBaseClass.OBJECT_RENDER_WEIGHT:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_HOVER_HEIGHT:
                            ret.Add(new LSL_Float(0));
                            break;
                        case ScriptBaseClass.OBJECT_BODY_SHAPE_TYPE:
                            ret.Add(new LSL_Float(-1));
                            break;
                        case ScriptBaseClass.OBJECT_LAST_OWNER_ID:
                            ret.Add(new LSL_Key(obj.ParentGroup.LastOwnerID.ToString()));
                            break;
                        case ScriptBaseClass.OBJECT_CLICK_ACTION:
                            ret.Add(new LSL_Integer(obj.ClickAction));
                            break;
                        case ScriptBaseClass.OBJECT_OMEGA:
                            ret.Add(new LSL_Vector(obj.AngularVelocity));
                            break;
                        case ScriptBaseClass.OBJECT_PRIM_COUNT:
                            ret.Add(new LSL_Integer(obj.ParentGroup.PrimCount));
                            break;
                        case ScriptBaseClass.OBJECT_TOTAL_INVENTORY_COUNT:
                            var parts = obj.ParentGroup.Parts;
                            count = 0;
                            for (var i = 0; i < parts.Length; i++)
                                count += parts[i].Inventory.Count;
                            ret.Add(new LSL_Integer(count));
                            break;
                        case ScriptBaseClass.OBJECT_REZZER_KEY:
                            ret.Add(new LSL_Key(obj.ParentGroup.RezzerID.ToString()));
                            break;
                        case ScriptBaseClass.OBJECT_GROUP_TAG:
                            ret.Add(new LSL_String(string.Empty));
                            break;
                        case ScriptBaseClass.OBJECT_TEMP_ATTACHED:
                            if (obj.ParentGroup.AttachmentPoint != 0 && obj.ParentGroup.FromItemID.IsZero())
                                ret.Add(new LSL_Integer(1));
                            else
                                ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_ATTACHED_SLOTS_AVAILABLE:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_CREATION_TIME:
                            var date = Util.ToDateTime(obj.ParentGroup.RootPart.CreationDate);
                            ret.Add(new LSL_String(date.ToString("yyyy-MM-ddTHH:mm:ssZ",
                                CultureInfo.InvariantCulture)));
                            break;
                        case ScriptBaseClass.OBJECT_SELECT_COUNT:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_SIT_COUNT:
                            ret.Add(new LSL_Integer(obj.ParentGroup.GetSittingAvatarsCount()));
                            break;
                        case ScriptBaseClass.OBJECT_ANIMATED_COUNT:
                            if (obj.ParentGroup.RootPart.Shape.MeshFlagEntry)
                                ret.Add(new LSL_Integer(1));
                            else
                                ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_ANIMATED_SLOTS_AVAILABLE:
                            ret.Add(new LSL_Integer(0));
                            break;
                        case ScriptBaseClass.OBJECT_ACCOUNT_LEVEL:
                            ret.Add(new LSL_Integer(1));
                            break;
                        case ScriptBaseClass.OBJECT_MATERIAL:
                            ret.Add(new LSL_Integer(obj.Material));
                            break;
                        case ScriptBaseClass.OBJECT_MASS:
                            float mass;
                            if (obj.ParentGroup.IsAttachment)
                            {
                                ScenePresence attachedAvatar = World.GetScenePresence(obj.ParentGroup.AttachedAvatar);
                                mass = attachedAvatar is null ? 0 : attachedAvatar.GetMass();
                            }
                            else
                            {
                                mass = obj.ParentGroup.GetMass();
                            }

                            mass *= 100f;
                            ret.Add(new LSL_Float(mass));
                            break;
                        case ScriptBaseClass.OBJECT_TEXT:
                            ret.Add(new LSL_String(obj.Text));
                            break;
                        case ScriptBaseClass.OBJECT_REZ_TIME:
                            ret.Add(new LSL_String(obj.Rezzed.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ",
                                CultureInfo.InvariantCulture)));
                            break;
                        case ScriptBaseClass.OBJECT_LINK_NUMBER:
                            ret.Add(new LSL_Integer(obj.LinkNum));
                            break;
                        case ScriptBaseClass.OBJECT_SCALE:
                            ret.Add(new LSL_Vector(obj.Scale));
                            break;
                        case ScriptBaseClass.OBJECT_TEXT_COLOR:
                            var textColor = obj.GetTextColor();
                            ret.Add(new LSL_Vector(textColor.R, textColor.G, textColor.B));
                            break;
                        case ScriptBaseClass.OBJECT_TEXT_ALPHA:
                            ret.Add(new LSL_Float(obj.GetTextColor().A));
                            break;
                        default:
                            // Invalid or unhandled constant.
                            ret.Add(new LSL_Integer(ScriptBaseClass.OBJECT_UNKNOWN_DETAIL));
                            break;
                    }

            return ret;
        }

        public LSL_Key llGetNumberOfNotecardLines(string name)
        {
            if (!UUID.TryParse(name, out var assetID))
            {
                TaskInventoryItem item = m_host.Inventory.GetInventoryItem(name);

                if (item != null && item.Type == 7)
                    assetID = item.AssetID;
            }

            if (assetID.IsZero())
            {
                // => complain loudly, as specified by the LSL docs
                Error("llGetNumberOfNotecardLines", "Can't find notecard '" + name + "'");
                return ScriptBaseClass.NULL_KEY;
            }

            if (NotecardCache.IsCached(assetID))
            {
                string ftid = m_AsyncCommands.DataserverPlugin.RequestWithImediatePost(m_host.LocalId, m_item.ItemID,
                    NotecardCache.GetLines(assetID).ToString());
                ScriptSleep(m_sleepMsOnGetNumberOfNotecardLines);
                return ftid;
            }

            Action<string> act = eventID =>
            {
                if (NotecardCache.IsCached(assetID))
                {
                    m_AsyncCommands.DataserverPlugin.DataserverReply(eventID,
                        NotecardCache.GetLines(assetID).ToString());
                    return;
                }

                AssetBase a = World.AssetService.Get(assetID.ToString());
                if (a == null || a.Type != 7)
                {
                    m_AsyncCommands.DataserverPlugin.DataserverReply(eventID, string.Empty);
                    return;
                }

                NotecardCache.Cache(assetID, a.Data);
                m_AsyncCommands.DataserverPlugin.DataserverReply(eventID, NotecardCache.GetLines(assetID).ToString());
            };

            UUID tid = m_AsyncCommands.DataserverPlugin.RegisterRequest(m_host.LocalId, m_item.ItemID, act);
            ScriptSleep(m_sleepMsOnGetNumberOfNotecardLines);
            return tid.ToString();
        }

        public LSL_Key llGetNotecardLine(string name, int line)
        {
            if (!UUID.TryParse(name, out var assetID))
            {
                TaskInventoryItem item = m_host.Inventory.GetInventoryItem(name);

                if (item != null && item.Type == 7)
                    assetID = item.AssetID;
            }

            if (assetID.IsZero())
            {
                // => complain loudly, as specified by the LSL docs
                Error("llGetNotecardLine", "Can't find notecard '" + name + "'");
                return ScriptBaseClass.NULL_KEY;
            }

            if (NotecardCache.IsCached(assetID))
            {
                string eid = m_AsyncCommands.DataserverPlugin.RequestWithImediatePost(m_host.LocalId, m_item.ItemID,
                    NotecardCache.GetLine(assetID, line, m_notecardLineReadCharsMax));

                ScriptSleep(m_sleepMsOnGetNotecardLine);
                return eid;
            }

            Action<string> act = eventID =>
            {
                if (NotecardCache.IsCached(assetID))
                {
                    m_AsyncCommands.DataserverPlugin.DataserverReply(eventID,
                        NotecardCache.GetLine(assetID, line, m_notecardLineReadCharsMax));
                    return;
                }

                AssetBase a = World.AssetService.Get(assetID.ToString());
                if (a == null || a.Type != 7)
                {
                    m_AsyncCommands.DataserverPlugin.DataserverReply(eventID, string.Empty);
                    return;
                }

                NotecardCache.Cache(assetID, a.Data);
                m_AsyncCommands.DataserverPlugin.DataserverReply(
                    eventID, NotecardCache.GetLine(assetID, line, m_notecardLineReadCharsMax));
            };

            UUID tid = m_AsyncCommands.DataserverPlugin.RegisterRequest(m_host.LocalId, m_item.ItemID, act);
            ScriptSleep(m_sleepMsOnGetNotecardLine);
            return tid.ToString();
        }

        public void SetPrimitiveParamsEx(LSL_Key prim, LSL_List rules, string originFunc)
        {
            if (!UUID.TryParse(prim, out var id) || id.IsZero())
                return;

            SceneObjectPart obj = World.GetSceneObjectPart(id);
            if (obj == null)
                return;

            var sog = obj.ParentGroup;
            if (sog == null || sog.IsDeleted)
                return;

            var objRoot = sog.RootPart;
            if (objRoot == null || objRoot.OwnerID.NotEqual(m_host.OwnerID) ||
                (objRoot.OwnerMask & (uint)PermissionMask.Modify) == 0)
                return;

            uint rulesParsed = 0;
            var remaining = SetPrimParams(obj, rules, originFunc, ref rulesParsed);

            while (remaining.Length > 2)
            {
                int linknumber;
                try
                {
                    linknumber = remaining.GetLSLIntegerItem(0);
                }
                catch (InvalidCastException)
                {
                    Error(originFunc,
                        string.Format("Error running rule #{0} -> PRIM_LINK_TARGET parameter must be integer",
                            rulesParsed));
                    return;
                }

                List<ISceneEntity> entities = GetLinkEntities(obj, linknumber);
                if (entities.Count == 0)
                    break;

                rules = remaining.GetSublist(1, -1);
                foreach (var entity in entities)
                    if (entity is SceneObjectPart)
                        remaining = SetPrimParams((SceneObjectPart)entity, rules, originFunc, ref rulesParsed);
                    else
                        remaining = SetAgentParams((ScenePresence)entity, rules, originFunc, ref rulesParsed);
            }
        }

        public LSL_List GetPrimitiveParamsEx(LSL_Key prim, LSL_List rules)
        {
            var result = new LSL_List();

            if (!UUID.TryParse(prim, out var id))
                return result;

            SceneObjectPart obj = World.GetSceneObjectPart(id);
            if (obj == null)
                return result;

            var sog = obj.ParentGroup;
            if (sog == null || sog.IsDeleted)
                return result;

            var objRoot = sog.RootPart;
            if (objRoot == null || objRoot.OwnerID.NotEqual(m_host.OwnerID) ||
                (objRoot.OwnerMask & (uint)PermissionMask.Modify) == 0)
                return result;

            var remaining = GetPrimParams(obj, rules, ref result);

            while (remaining.Length > 1)
            {
                int linknumber;
                try
                {
                    linknumber = remaining.GetLSLIntegerItem(0);
                }
                catch (InvalidCastException)
                {
                    Error("", "Error PRIM_LINK_TARGET: parameter must be integer");
                    return result;
                }

                List<ISceneEntity> entities = GetLinkEntities(obj, linknumber);
                if (entities.Count == 0)
                    break;

                rules = remaining.GetSublist(1, -1);
                foreach (var entity in entities)
                    if (entity is SceneObjectPart)
                        remaining = GetPrimParams((SceneObjectPart)entity, rules, ref result);
                    else
                        remaining = GetPrimParams((ScenePresence)entity, rules, ref result);
            }

            return result;
        }

        public LSL_Integer llGetLinkNumberOfSides(LSL_Integer link)
        {
            List<SceneObjectPart> parts = GetLinkParts(link);
            if (parts.Count < 1)
                return 0;

            return GetNumberOfSides(parts[0]);
        }

        public LSL_String llGetUsername(LSL_Key id)
        {
            return Name2Username(llKey2Name(id));
        }

        public LSL_Key llRequestUsername(LSL_Key id)
        {
            if (!UUID.TryParse(id, out var key) || key.IsZero())
                return string.Empty;

            ScenePresence lpresence = World.GetScenePresence(key);
            if (lpresence != null)
            {
                var lname = lpresence.Name;
                string ftid = m_AsyncCommands.DataserverPlugin.RequestWithImediatePost(m_host.LocalId,
                    m_item.ItemID, Name2Username(lname));
                return ftid;
            }

            Action<string> act = eventID =>
            {
                var name = string.Empty;
                ScenePresence presence = World.GetScenePresence(key);
                if (presence != null)
                {
                    name = presence.Name;
                }
                else if (World.TryGetSceneObjectPart(key, out SceneObjectPart sop) && sop != null)
                {
                    name = sop.Name;
                }
                else
                {
                    UserAccount account = m_userAccountService.GetUserAccount(RegionScopeID, key);
                    if (account != null) name = account.FirstName + " " + account.LastName;
                }

                m_AsyncCommands.DataserverPlugin.DataserverReply(eventID, Name2Username(name));
            };

            UUID rq = m_AsyncCommands.DataserverPlugin.RegisterRequest(m_host.LocalId, m_item.ItemID, act);
            ScriptSleep(m_sleepMsOnRequestAgentData);
            return rq.ToString();
        }

        public LSL_String llGetDisplayName(LSL_Key id)
        {
            if (UUID.TryParse(id, out var key) && key.IsNotZero())
            {
                ScenePresence presence = World.GetScenePresence(key);
                if (presence != null) return presence.Name;
            }

            return string.Empty;
        }

        public LSL_Key llRequestDisplayName(LSL_Key id)
        {
            if (!UUID.TryParse(id, out var key) || key.IsZero())
                return string.Empty;

            ScenePresence lpresence = World.GetScenePresence(key);
            if (lpresence != null)
            {
                var lname = lpresence.Name;
                string ftid = m_AsyncCommands.DataserverPlugin.RequestWithImediatePost(m_host.LocalId,
                    m_item.ItemID, lname);
                return ftid;
            }

            Action<string> act = eventID =>
            {
                var name = string.Empty;
                ScenePresence presence = World.GetScenePresence(key);
                if (presence != null)
                {
                    name = presence.Name;
                }
                else if (World.TryGetSceneObjectPart(key, out SceneObjectPart sop) && sop != null)
                {
                    name = sop.Name;
                }
                else
                {
                    UserAccount account = m_userAccountService.GetUserAccount(RegionScopeID, key);
                    if (account != null) name = account.FirstName + " " + account.LastName;
                }

                m_AsyncCommands.DataserverPlugin.DataserverReply(eventID, name);
            };

            UUID rq = m_AsyncCommands.DataserverPlugin.RegisterRequest(m_host.LocalId, m_item.ItemID, act);
            return rq.ToString();
        }
/*
        // not done:
        private ContactResult[] testRay2NonPhysicalPhantom(Vector3 rayStart, Vector3 raydir, float raylenght)
        {
            ContactResult[] contacts = null;
            World.ForEachSOG(delegate(SceneObjectGroup group)
            {
                if (m_host.ParentGroup == group)
                    return;

                if (group.IsAttachment)
                    return;

                if(group.RootPart.PhysActor != null)
                    return;

                contacts = group.RayCastGroupPartsOBBNonPhysicalPhantom(rayStart, raydir, raylenght);
            });
            return contacts;
        }
*/

        public LSL_List llCastRay(LSL_Vector start, LSL_Vector end, LSL_List options)
        {
            var list = new LSL_List();

            Vector3 rayStart = start;
            Vector3 rayEnd = end;
            var dir = rayEnd - rayStart;

            var dist = dir.LengthSquared();
            if (dist < 1e-6)
            {
                list.Add(new LSL_Integer(0));
                return list;
            }

            var count = 1;
            var detectPhantom = false;
            var dataFlags = 0;
            var rejectTypes = 0;

            for (var i = 0; i < options.Length; i += 2)
                if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_MAX_HITS)
                    count = options.GetLSLIntegerItem(i + 1);
                else if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_DETECT_PHANTOM)
                    detectPhantom = options.GetLSLIntegerItem(i + 1) > 0;
                else if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_DATA_FLAGS)
                    dataFlags = options.GetLSLIntegerItem(i + 1);
                else if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_REJECT_TYPES)
                    rejectTypes = options.GetLSLIntegerItem(i + 1);

            if (count > 16)
                count = 16;

            var results = new List<ContactResult>();

            bool checkTerrain = (rejectTypes & ScriptBaseClass.RC_REJECT_LAND) == 0;
            bool checkAgents = (rejectTypes & ScriptBaseClass.RC_REJECT_AGENTS) == 0;
            bool checkNonPhysical = (rejectTypes & ScriptBaseClass.RC_REJECT_NONPHYSICAL) == 0;
            bool checkPhysical = (rejectTypes & ScriptBaseClass.RC_REJECT_PHYSICAL) == 0;
            bool rejectHost = (rejectTypes & ScriptBaseClass.RC_REJECT_HOST) != 0;
            bool rejectHostGroup = (rejectTypes & ScriptBaseClass.RC_REJECT_HOSTGROUP) != 0;

            if (World.SupportsRayCastFiltered())
            {
                RayFilterFlags rayfilter = 0;
                if (checkTerrain)
                    rayfilter = RayFilterFlags.land;
                if (checkAgents)
                    rayfilter |= RayFilterFlags.agent;
                if (checkPhysical)
                    rayfilter |= RayFilterFlags.physical;
                if (checkNonPhysical)
                    rayfilter |= RayFilterFlags.nonphysical;
                if (detectPhantom)
                    rayfilter |= RayFilterFlags.LSLPhantom;

                if (rayfilter == 0)
                {
                    list.Add(new LSL_Integer(0));
                    return list;
                }

                rayfilter |= RayFilterFlags.BackFaceCull;

                dist = (float)Math.Sqrt(dist);
                var direction = dir * (1.0f / dist);

                // get some more contacts to sort ???
                object physresults = World.RayCastFiltered(rayStart, direction, dist, 2 * count, rayfilter);

                if (physresults != null) results = (List<ContactResult>)physresults;

                // for now physics doesn't detect sitted avatars so do it outside physics
                if (checkAgents)
                {
                    var agentHits = AvatarIntersection(rayStart, rayEnd, true);
                    foreach (var r in agentHits)
                        results.Add(r);
                }

                // TODO: Replace this with a better solution. ObjectIntersection can only
                // detect nonphysical phantoms. They are detected by virtue of being
                // nonphysical (e.g. no PhysActor) so will not conflict with detecting
                // physicsl phantoms as done by the physics scene
                // We don't want anything else but phantoms here.
                if (detectPhantom)
                {
                    var objectHits = ObjectIntersection(rayStart, rayEnd, false, false, true);
                    foreach (var r in objectHits)
                        results.Add(r);
                }

                // Double check this because of current ODE distance problems
                if (checkTerrain && dist > 60)
                {
                    var skipGroundCheck = false;

                    foreach (var c in results)
                        if (c.ConsumerID == 0) // Physics gave us a ground collision
                            skipGroundCheck = true;

                    if (!skipGroundCheck)
                    {
                        var tmp = dir.X * dir.X + dir.Y * dir.Y;
                        if (tmp > 2500)
                        {
                            var groundContact = GroundIntersection(rayStart, rayEnd);
                            if (groundContact != null)
                                results.Add((ContactResult)groundContact);
                        }
                    }
                }
            }
            else
            {
                if (checkAgents)
                {
                    var agentHits = AvatarIntersection(rayStart, rayEnd, false);
                    foreach (var r in agentHits)
                        results.Add(r);
                }

                if (checkPhysical || checkNonPhysical || detectPhantom)
                {
                    var objectHits =
                        ObjectIntersection(rayStart, rayEnd, checkPhysical, checkNonPhysical, detectPhantom);
                    for (var iter = 0; iter < objectHits.Length; iter++)
                    {
                        // Redistance the Depth because the Scene RayCaster returns distance from center to make the rezzing code simpler.
                        objectHits[iter].Depth = Vector3.Distance(objectHits[iter].Pos, rayStart);
                        results.Add(objectHits[iter]);
                    }
                }

                if (checkTerrain)
                {
                    var groundContact = GroundIntersection(rayStart, rayEnd);
                    if (groundContact != null)
                        results.Add((ContactResult)groundContact);
                }
            }

            results.Sort(delegate(ContactResult a, ContactResult b) { return a.Depth.CompareTo(b.Depth); });

            var values = 0;
            SceneObjectGroup thisgrp = m_host.ParentGroup;

            foreach (var result in results)
            {
                if (result.Depth > dist)
                    continue;

                // physics ray can return colisions with host prim
                if (rejectHost && m_host.LocalId == result.ConsumerID)
                    continue;

                var itemID = UUID.Zero;
                var linkNum = 0;

                SceneObjectPart part = World.GetSceneObjectPart(result.ConsumerID);
                // It's a prim!
                if (part != null)
                {
                    if (rejectHostGroup && part.ParentGroup == thisgrp)
                        continue;

                    if ((dataFlags & ScriptBaseClass.RC_GET_ROOT_KEY) == ScriptBaseClass.RC_GET_ROOT_KEY)
                        itemID = part.ParentGroup.UUID;
                    else
                        itemID = part.UUID;

                    linkNum = part.LinkNum;
                }
                else
                {
                    ScenePresence sp = World.GetScenePresence(result.ConsumerID);
                    /// It it a boy? a girl?
                    if (sp != null)
                        itemID = sp.UUID;
                }

                list.Add(new LSL_String(itemID.ToString()));
                list.Add(new LSL_String(result.Pos.ToString()));

                if ((dataFlags & ScriptBaseClass.RC_GET_LINK_NUM) == ScriptBaseClass.RC_GET_LINK_NUM)
                    list.Add(new LSL_Integer(linkNum));

                if ((dataFlags & ScriptBaseClass.RC_GET_NORMAL) == ScriptBaseClass.RC_GET_NORMAL)
                    list.Add(new LSL_Vector(result.Normal));

                values++;
                if (values >= count)
                    break;
            }

            list.Add(new LSL_Integer(values));
            return list;
        }

        public LSL_Integer llManageEstateAccess(int action, string avatar)
        {
            if (!UUID.TryParse(avatar, out var id) || id.IsZero())
                return 0;

            EstateSettings estate = World.RegionInfo.EstateSettings;
            if (!estate.IsEstateOwner(m_host.OwnerID) || !estate.IsEstateManagerOrOwner(m_host.OwnerID))
                return 0;

            UserAccount account = m_userAccountService.GetUserAccount(RegionScopeID, id);
            var isAccount = account != null ? true : false;
            var isGroup = false;
            if (!isAccount)
            {
                IGroupsModule groups = World.RequestModuleInterface<IGroupsModule>();
                if (groups != null)
                {
                    var group = groups.GetGroupRecord(id);
                    isGroup = group != null ? true : false;
                    if (!isGroup)
                        return 0;
                }
                else
                {
                    return 0;
                }
            }

            switch (action)
            {
                case ScriptBaseClass.ESTATE_ACCESS_ALLOWED_AGENT_ADD:
                    if (!isAccount) return 0;
                    if (estate.HasAccess(id)) return 1;
                    if (estate.IsBanned(id, World.GetUserFlags(id)))
                        estate.RemoveBan(id);
                    estate.AddEstateUser(id);
                    break;
                case ScriptBaseClass.ESTATE_ACCESS_ALLOWED_AGENT_REMOVE:
                    if (!isAccount || !estate.HasAccess(id)) return 0;
                    estate.RemoveEstateUser(id);
                    break;
                case ScriptBaseClass.ESTATE_ACCESS_ALLOWED_GROUP_ADD:
                    if (!isGroup) return 0;
                    if (estate.GroupAccess(id)) return 1;
                    estate.AddEstateGroup(id);
                    break;
                case ScriptBaseClass.ESTATE_ACCESS_ALLOWED_GROUP_REMOVE:
                    if (!isGroup || !estate.GroupAccess(id)) return 0;
                    estate.RemoveEstateGroup(id);
                    break;
                case ScriptBaseClass.ESTATE_ACCESS_BANNED_AGENT_ADD:
                    if (!isAccount) return 0;
                    if (estate.IsBanned(id, World.GetUserFlags(id))) return 1;
                    var ban = new EstateBan
                    {
                        EstateID = estate.EstateID,
                        BannedUserID = id
                    };
                    estate.AddBan(ban);
                    break;
                case ScriptBaseClass.ESTATE_ACCESS_BANNED_AGENT_REMOVE:
                    if (!isAccount || !estate.IsBanned(id, World.GetUserFlags(id))) return 0;
                    estate.RemoveBan(id);
                    break;
                default: return 0;
            }

            return 1;
        }

        public LSL_Integer llGetMemoryLimit()
        {
            // The value returned for Mono scripts in SL
            return 65536;
        }

        public LSL_Integer llSetMemoryLimit(LSL_Integer limit)
        {
            // Treat as an LSO script
            return ScriptBaseClass.FALSE;
        }

        public LSL_Integer llGetSPMaxMemory()
        {
            // The value returned for Mono scripts in SL
            return 65536;
        }

        public virtual LSL_Integer llGetUsedMemory()
        {
            // The value returned for Mono scripts in SL
            return 65536;
        }

        public void llScriptProfiler(LSL_Integer flags)
        {
            // This does nothing for LSO scripts in SL
        }

        public void llSetSoundQueueing(int queue)
        {
            if (m_SoundModule != null)
                m_SoundModule.SetSoundQueueing(m_host.UUID, queue == ScriptBaseClass.TRUE.value);
        }

        public void llLinkSetSoundQueueing(int linknumber, int queue)
        {
            if (m_SoundModule != null)
                foreach (SceneObjectPart sop in GetLinkParts(linknumber))
                    m_SoundModule.SetSoundQueueing(sop.UUID, queue == ScriptBaseClass.TRUE.value);
        }

        public void llSetAnimationOverride(LSL_String animState, LSL_String anim)
        {
            if (m_item.PermsGranter.IsZero())
            {
                llShout(ScriptBaseClass.DEBUG_CHANNEL, "No permission to override animations");
                return;
            }

            if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_OVERRIDE_ANIMATIONS) == 0)
            {
                llShout(ScriptBaseClass.DEBUG_CHANNEL, "No permission to override animations");
                return;
            }

            ScenePresence presence = World.GetScenePresence(m_item.PermsGranter);
            if (presence == null)
                return;

            var state = string.Empty;

            foreach (KeyValuePair<string, string> kvp in MovementAnimationsForLSL)
                if (kvp.Value.ToLower() == ((string)animState).ToLower())
                {
                    state = kvp.Key;
                    break;
                }

            if (state.Length == 0)
            {
                llShout(ScriptBaseClass.DEBUG_CHANNEL, "Invalid animation state " + animState);
                return;
            }


            UUID animID;

            animID = ScriptUtils.GetAssetIdFromItemName(m_host, anim, (int)AssetType.Animation);

            if (animID.IsZero())
            {
                var animupper = ((string)anim).ToUpperInvariant();
                DefaultAvatarAnimations.AnimsUUIDbyName.TryGetValue(animupper, out animID);
            }

            if (animID.IsZero())
            {
                llShout(ScriptBaseClass.DEBUG_CHANNEL, "Animation not found");
                return;
            }

            presence.SetAnimationOverride(state, animID);
        }

        public void llResetAnimationOverride(LSL_String animState)
        {
            ScenePresence presence = World.GetScenePresence(m_item.PermsGranter);
            if (presence == null)
                return;

            if (m_item.PermsGranter.IsZero())
            {
                llShout(ScriptBaseClass.DEBUG_CHANNEL, "No permission to override animations");
                return;
            }

            if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_OVERRIDE_ANIMATIONS) == 0)
            {
                llShout(ScriptBaseClass.DEBUG_CHANNEL, "No permission to override animations");
                return;
            }

            if (animState == "ALL")
            {
                presence.SetAnimationOverride("ALL", UUID.Zero);
                return;
            }

            var state = string.Empty;

            foreach (KeyValuePair<string, string> kvp in MovementAnimationsForLSL)
                if (kvp.Value.ToLower() == ((string)animState).ToLower())
                {
                    state = kvp.Key;
                    break;
                }

            if (state.Length == 0) return;

            presence.SetAnimationOverride(state, UUID.Zero);
        }

        public LSL_String llGetAnimationOverride(LSL_String animState)
        {
            ScenePresence presence = World.GetScenePresence(m_item.PermsGranter);
            if (presence == null)
                return string.Empty;

            if (m_item.PermsGranter.IsZero())
            {
                llShout(ScriptBaseClass.DEBUG_CHANNEL, "No permission to override animations");
                return string.Empty;
            }

            if ((m_item.PermsMask & (ScriptBaseClass.PERMISSION_OVERRIDE_ANIMATIONS |
                                     ScriptBaseClass.PERMISSION_TRIGGER_ANIMATION)) == 0)
            {
                llShout(ScriptBaseClass.DEBUG_CHANNEL, "No permission to override animations");
                return string.Empty;
            }

            var state = string.Empty;

            foreach (KeyValuePair<string, string> kvp in MovementAnimationsForLSL)
                if (kvp.Value.ToLower() == ((string)animState).ToLower())
                {
                    state = kvp.Key;
                    break;
                }

            if (state.Length == 0) return string.Empty;

            if (!presence.TryGetAnimationOverride(state, out var animID) || animID.IsZero())
                return animState;

            foreach (var kvp in DefaultAvatarAnimations.AnimsUUIDbyName)
                if (kvp.Value.Equals(animID))
                    return kvp.Key.ToLower();

            foreach (TaskInventoryItem item in m_host.Inventory.GetInventoryItems())
                if (item.AssetID.Equals(animID))
                    return item.Name;

            return string.Empty;
        }

        public LSL_Integer llGetDayLength()
        {
            if (m_envModule == null)
                return 14400;

            return m_envModule.GetDayLength(m_host.GetWorldPosition());
        }

        public LSL_Integer llGetRegionDayLength()
        {
            if (m_envModule == null)
                return 14400;

            return m_envModule.GetRegionDayLength();
        }

        public LSL_Integer llGetDayOffset()
        {
            if (m_envModule == null)
                return 57600;

            return m_envModule.GetDayOffset(m_host.GetWorldPosition());
        }

        public LSL_Integer llGetRegionDayOffset()
        {
            if (m_envModule == null)
                return 57600;

            return m_envModule.GetRegionDayOffset();
        }

        public LSL_Vector llGetSunDirection()
        {
            if (m_envModule == null)
                return Vector3.Zero;

            return m_envModule.GetSunDir(m_host.GetWorldPosition());
        }

        public LSL_Vector llGetRegionSunDirection()
        {
            if (m_envModule == null)
                return Vector3.Zero;

            float z = m_host.GetWorldPosition().Z;
            return m_envModule.GetRegionSunDir(z);
        }

        public LSL_Vector llGetMoonDirection()
        {
            if (m_envModule == null)
                return Vector3.Zero;

            return m_envModule.GetMoonDir(m_host.GetWorldPosition());
        }

        public LSL_Vector llGetRegionMoonDirection()
        {
            if (m_envModule == null)
                return Vector3.Zero;

            float z = m_host.GetWorldPosition().Z;
            return m_envModule.GetRegionMoonDir(z);
        }

        public LSL_Rotation llGetSunRotation()
        {
            if (m_envModule == null)
                return Quaternion.Identity;

            return m_envModule.GetSunRot(m_host.GetWorldPosition());
        }

        public LSL_Rotation llGetRegionSunRotation()
        {
            if (m_envModule == null)
                return Quaternion.Identity;

            float z = m_host.GetWorldPosition().Z;
            return m_envModule.GetRegionSunRot(z);
        }

        public LSL_Rotation llGetMoonRotation()
        {
            if (m_envModule == null)
                return Quaternion.Identity;

            return m_envModule.GetMoonRot(m_host.GetWorldPosition());
        }

        public LSL_Rotation llGetRegionMoonRotation()
        {
            if (m_envModule == null)
                return Quaternion.Identity;

            float z = m_host.GetWorldPosition().Z;
            return m_envModule.GetRegionMoonRot(z);
        }

        public LSL_List llJson2List(LSL_String json)
        {
            if (string.IsNullOrEmpty(json))
                return new LSL_List();
            if (json == "[]")
                return new LSL_List();
            if (json == "{}")
                return new LSL_List();
            var first = ((string)json)[0];

            if (first != '[' && first != '{')
            {
                // we already have a single element
                var l = new LSL_List();
                l.Add(json);
                return l;
            }

            JsonData jsdata;
            try
            {
                jsdata = JsonMapper.ToObject(json);
            }
            catch (Exception e)
            {
                var m = e.Message; // debug point
                return json;
            }

            try
            {
                return JsonParseTop(jsdata);
            }
            catch (Exception e)
            {
                var m = e.Message; // debug point
                return (LSL_String)ScriptBaseClass.JSON_INVALID;
            }
        }

        public LSL_String llList2Json(LSL_String type, LSL_List values)
        {
            try
            {
                var sb = new StringBuilder();
                if (type == ScriptBaseClass.JSON_ARRAY)
                {
                    sb.Append("[");
                    var i = 0;
                    foreach (var o in values.Data)
                    {
                        sb.Append(ListToJson(o));
                        if (i++ < values.Data.Length - 1)
                            sb.Append(",");
                    }

                    sb.Append("]");
                    return (LSL_String)sb.ToString();
                    ;
                }

                if (type == ScriptBaseClass.JSON_OBJECT)
                {
                    sb.Append("{");
                    for (var i = 0; i < values.Data.Length; i += 2)
                    {
                        if (!(values.Data[i] is LSL_String))
                            return ScriptBaseClass.JSON_INVALID;
                        var key = ((LSL_String)values.Data[i]).m_string;
                        key = EscapeForJSON(key, true);
                        sb.Append(key);
                        sb.Append(":");
                        sb.Append(ListToJson(values.Data[i + 1]));
                        if (i < values.Data.Length - 2)
                            sb.Append(",");
                    }

                    sb.Append("}");
                    return (LSL_String)sb.ToString();
                }

                return ScriptBaseClass.JSON_INVALID;
            }
            catch
            {
                return ScriptBaseClass.JSON_INVALID;
            }
        }

        public LSL_String llJsonSetValue(LSL_String json, LSL_List specifiers, LSL_String value)
        {
            var noSpecifiers = specifiers.Length == 0;
            JsonData workData;
            try
            {
                if (noSpecifiers)
                    specifiers.Add(new LSL_Integer(0));

                if (!string.IsNullOrEmpty(json))
                {
                    workData = JsonMapper.ToObject(json);
                }
                else
                {
                    workData = new JsonData();
                    workData.SetJsonType(JsonType.Array);
                }
            }
            catch (Exception e)
            {
                var m = e.Message; // debug point
                return ScriptBaseClass.JSON_INVALID;
            }

            try
            {
                var replace = JsonSetSpecific(workData, specifiers, 0, value);
                if (replace != null)
                    workData = replace;
            }
            catch (Exception e)
            {
                var m = e.Message; // debug point
                return ScriptBaseClass.JSON_INVALID;
            }

            try
            {
                var r = JsonMapper.ToJson(workData);
                if (noSpecifiers)
                    r = r.Substring(1, r.Length - 2); // strip leading and trailing brakets
                return r;
            }
            catch (Exception e)
            {
                var m = e.Message; // debug point
            }

            return ScriptBaseClass.JSON_INVALID;
        }

        public LSL_String llJsonGetValue(LSL_String json, LSL_List specifiers)
        {
            if (string.IsNullOrWhiteSpace(json))
                return ScriptBaseClass.JSON_INVALID;

            if (specifiers.Length > 0 && (json == "{}" || json == "[]"))
                return ScriptBaseClass.JSON_INVALID;

            var first = ((string)json)[0];
            if (first != '[' && first != '{')
            {
                if (specifiers.Length > 0)
                    return ScriptBaseClass.JSON_INVALID;
                json = "[" + json + "]"; // could handle single element case.. but easier like this
                specifiers.Add((LSL_Integer)0);
            }

            JsonData jsonData;
            try
            {
                jsonData = JsonMapper.ToObject(json);
            }
            catch (Exception e)
            {
                var m = e.Message; // debug point
                return ScriptBaseClass.JSON_INVALID;
            }

            JsonData elem = null;
            if (specifiers.Length == 0)
            {
                elem = jsonData;
            }
            else
            {
                if (!JsonFind(jsonData, specifiers, 0, out elem))
                    return ScriptBaseClass.JSON_INVALID;
            }

            return JsonElementToString(elem);
        }

        public LSL_String llJsonValueType(LSL_String json, LSL_List specifiers)
        {
            if (string.IsNullOrWhiteSpace(json))
                return ScriptBaseClass.JSON_INVALID;

            if (specifiers.Length > 0 && (json == "{}" || json == "[]"))
                return ScriptBaseClass.JSON_INVALID;

            var first = ((string)json)[0];
            if (first != '[' && first != '{')
            {
                if (specifiers.Length > 0)
                    return ScriptBaseClass.JSON_INVALID;
                json = "[" + json + "]"; // could handle single element case.. but easier like this
                specifiers.Add((LSL_Integer)0);
            }

            JsonData jsonData;
            try
            {
                jsonData = JsonMapper.ToObject(json);
            }
            catch (Exception e)
            {
                var m = e.Message; // debug point
                return ScriptBaseClass.JSON_INVALID;
            }

            JsonData elem = null;
            if (specifiers.Length == 0)
            {
                elem = jsonData;
            }
            else
            {
                if (!JsonFind(jsonData, specifiers, 0, out elem))
                    return ScriptBaseClass.JSON_INVALID;
            }

            if (elem == null)
                return ScriptBaseClass.JSON_NULL;

            var elemType = elem.GetJsonType();
            switch (elemType)
            {
                case JsonType.Array:
                    return ScriptBaseClass.JSON_ARRAY;
                case JsonType.Boolean:
                    return (bool)elem ? ScriptBaseClass.JSON_TRUE : ScriptBaseClass.JSON_FALSE;
                case JsonType.Double:
                case JsonType.Int:
                case JsonType.Long:
                    return ScriptBaseClass.JSON_NUMBER;
                case JsonType.Object:
                    return ScriptBaseClass.JSON_OBJECT;
                case JsonType.String:
                    var s = (string)elem;
                    if (s == ScriptBaseClass.JSON_NULL)
                        return ScriptBaseClass.JSON_NULL;
                    if (s == ScriptBaseClass.JSON_TRUE)
                        return ScriptBaseClass.JSON_TRUE;
                    if (s == ScriptBaseClass.JSON_FALSE)
                        return ScriptBaseClass.JSON_FALSE;
                    return ScriptBaseClass.JSON_STRING;
                case JsonType.None:
                    return ScriptBaseClass.JSON_NULL;
                default:
                    return ScriptBaseClass.JSON_INVALID;
            }
        }

        public LSL_String llChar(LSL_Integer unicode)
        {
            if (unicode == 0)
                return string.Empty;
            try
            {
                return char.ConvertFromUtf32(unicode);
            }
            catch
            {
            }

            return "\ufffd";
        }

        public LSL_Integer llOrd(LSL_String s, LSL_Integer index)
        {
            if (string.IsNullOrEmpty(s))
                return 0;

            if (index < 0)
                index += s.Length;

            if (index < 0 || index >= s.Length)
                return 0;

            var c = s.m_string[index];
            if (c >= 0xdc00 && c <= 0xdfff)
            {
                --index;
                if (index < 0)
                    return 0;

                var a = c - 0xdc00;
                c = s.m_string[index];
                if (c < 0xd800 || c > 0xdbff)
                    return 0;
                c -= (char)(0xd800 - 0x40);
                return a + (c << 10);
            }

            if (c >= 0xd800)
            {
                if (c < 0xdc00)
                {
                    ++index;
                    if (index >= s.Length)
                        return 0;

                    c -= (char)(0xd800 - 0x40);
                    var a = c << 10;

                    c = s.m_string[index];
                    if (c < 0xdc00 || c > 0xdfff)
                        return 0;
                    c -= (char)0xdc00;
                    return a + c;
                }

                if (c < 0xe000) return 0;
            }

            return (int)c;
        }

        public LSL_Integer llHash(LSL_String s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;
            var hash = 0;
            char c;
            for (var i = 0; i < s.Length; ++i)
            {
                hash *= 65599;
                // on modern intel/amd this is faster than the tradicional optimization:
                // hash = (hash << 6) + (hash << 16) - hash;
                c = s.m_string[i];
                if (c >= 0xd800)
                {
                    if (c < 0xdc00)
                    {
                        ++i;
                        if (i >= s.Length)
                            return 0;

                        c -= (char)(0xd800 - 0x40);
                        hash += c << 10;

                        c = s.m_string[i];
                        if (c < 0xdc00 || c > 0xdfff)
                            return 0;
                        c -= (char)0xdc00;
                    }
                    else if (c < 0xe000)
                    {
                        return 0;
                    }
                }

                hash += c;
            }

            return hash;
        }

        public LSL_String llReplaceSubString(LSL_String src, LSL_String pattern, LSL_String replacement, int count)
        {
            RegexOptions RegexOptions;
            if (count < 0)
            {
                RegexOptions = RegexOptions.CultureInvariant | RegexOptions.RightToLeft;
                count = -count;
            }
            else
            {
                RegexOptions = RegexOptions.CultureInvariant;
                if (count == 0)
                    count = -1;
            }


            try
            {
                if (string.IsNullOrEmpty(src.m_string))
                    return src;

                if (string.IsNullOrEmpty(pattern.m_string))
                    return src;

                var rx = new Regex(pattern, RegexOptions, new TimeSpan(500000)); // 50ms)
                if (replacement == null)
                    return rx.Replace(src.m_string, string.Empty, count);

                return rx.Replace(src.m_string, replacement.m_string, count);
            }
            catch
            {
                return src;
            }
        }

        public void CreateLink(string target, int parent)
        {
            if (!UUID.TryParse(target, out var targetID) || targetID.IsZero())
                return;

            SceneObjectGroup hostgroup = m_host.ParentGroup;
            if (hostgroup.AttachmentPoint != 0)
                return; // Fail silently if attached
            if ((hostgroup.RootPart.OwnerMask & (uint)PermissionMask.Modify) == 0)
                return;

            SceneObjectPart targetPart = World.GetSceneObjectPart(targetID);
            if (targetPart == null)
                return;

            var targetgrp = targetPart.ParentGroup;

            if (targetgrp == null || targetgrp.OwnerID.NotEqual(hostgroup.OwnerID))
                return;

            if (targetgrp.AttachmentPoint != 0)
                return; // Fail silently if attached
            if ((targetgrp.RootPart.OwnerMask & (uint)PermissionMask.Modify) == 0)
                return;

            SceneObjectGroup parentPrim = null, childPrim = null;

            if (parent != 0)
            {
                parentPrim = hostgroup;
                childPrim = targetgrp;
            }
            else
            {
                parentPrim = targetgrp;
                childPrim = hostgroup;
            }

            // Required for linking
            childPrim.RootPart.ClearUpdateSchedule();
            parentPrim.LinkToGroup(childPrim, true);

            parentPrim.TriggerScriptChangedEvent(Changed.LINK);
            parentPrim.RootPart.CreateSelected = false;
            parentPrim.HasGroupChanged = true;
            parentPrim.ScheduleGroupForFullUpdate();

            IClientAPI client = null;
            ScenePresence sp = World.GetScenePresence(m_host.OwnerID);
            if (sp != null)
                client = sp.ControllingClient;

            if (client != null)
                parentPrim.SendPropertiesToClient(client);

            ScriptSleep(m_sleepMsOnCreateLink);
        }

        public void BreakLink(int linknum)
        {
            if (linknum < ScriptBaseClass.LINK_THIS)
                return;

            SceneObjectGroup parentSOG = m_host.ParentGroup;

            if (parentSOG.AttachmentPoint != 0)
                return; // Fail silently if attached

            if ((parentSOG.RootPart.OwnerMask & (uint)PermissionMask.Modify) == 0)
                return;

            SceneObjectPart childPrim = null;

            switch (linknum)
            {
                case ScriptBaseClass.LINK_ROOT:
                case ScriptBaseClass.LINK_SET:
                case ScriptBaseClass.LINK_ALL_OTHERS:
                case ScriptBaseClass.LINK_ALL_CHILDREN:
                    break;
                case ScriptBaseClass.LINK_THIS: // not as spec
                    childPrim = m_host;
                    break;
                default:
                    childPrim = parentSOG.GetLinkNumPart(linknum);
                    break;
            }

            if (linknum == ScriptBaseClass.LINK_ROOT)
            {
                var avs = parentSOG.GetSittingAvatars();
                foreach (var av in avs)
                    av.StandUp();

                var parts = new List<SceneObjectPart>(parentSOG.Parts);
                parts.Remove(parentSOG.RootPart);
                if (parts.Count > 0)
                    foreach (var part in parts)
                        parentSOG.DelinkFromGroup(part.LocalId, true);

                parentSOG.HasGroupChanged = true;
                parentSOG.ScheduleGroupForFullUpdate();
                parentSOG.TriggerScriptChangedEvent(Changed.LINK);

                if (parts.Count > 0)
                {
                    var newRoot = parts[0];
                    parts.Remove(newRoot);

                    foreach (var part in parts)
                    {
                        part.ClearUpdateSchedule();
                        newRoot.ParentGroup.LinkToGroup(part.ParentGroup);
                    }

                    newRoot.ParentGroup.HasGroupChanged = true;
                    newRoot.ParentGroup.ScheduleGroupForFullUpdate();
                }
            }
            else
            {
                if (childPrim == null)
                    return;

                var avs = parentSOG.GetSittingAvatars();
                foreach (var av in avs)
                    av.StandUp();

                parentSOG.DelinkFromGroup(childPrim.LocalId, true);
            }
        }

        public void BreakAllLinks()
        {
            SceneObjectGroup parentPrim = m_host.ParentGroup;
            if (parentPrim.AttachmentPoint != 0)
                return; // Fail silently if attached

            var parts = new List<SceneObjectPart>(parentPrim.Parts);
            parts.Remove(parentPrim.RootPart);

            foreach (var part in parts)
            {
                parentPrim.DelinkFromGroup(part.LocalId, true);
                parentPrim.TriggerScriptChangedEvent(Changed.LINK);
            }

            parentPrim.HasGroupChanged = true;
            parentPrim.ScheduleGroupForFullUpdate();
        }

        private void DoLLTeleport(ScenePresence sp, string destination, Vector3 targetPos, Vector3 targetLookAt)
        {
            var assetID = ScriptUtils.GetAssetIdFromKeyOrItemName(m_host, destination);

            // The destination is not an asset ID and also doesn't name a landmark.
            // Use it as a sim name
            if (assetID.IsZero())
            {
                if (string.IsNullOrEmpty(destination))
                    World.RequestTeleportLocation(sp.ControllingClient, m_regionName, targetPos, targetLookAt,
                        (uint)TeleportFlags.ViaLocation);
                else
                    World.RequestTeleportLocation(sp.ControllingClient, destination, targetPos, targetLookAt,
                        (uint)TeleportFlags.ViaLocation);
                return;
            }

            AssetBase lma = World.AssetService.Get(assetID.ToString());
            if (lma == null || lma.Data == null || lma.Data.Length == 0)
                return;

            if (lma.Type != (sbyte)AssetType.Landmark)
                return;

            var lm = new AssetLandmark(lma);

            World.RequestTeleportLandmark(sp.ControllingClient, lm, targetLookAt);
        }

        protected int GetNumberOfSides(SceneObjectPart part)
        {
            return part.GetNumberOfSides();
        }

        protected LSL_Vector GetTextureOffset(SceneObjectPart part, int face)
        {
            var tex = part.Shape.Textures;
            var offset = new LSL_Vector();
            if (face == ScriptBaseClass.ALL_SIDES) face = 0;
            if (face >= 0 && face < GetNumberOfSides(part))
            {
                offset.x = tex.GetFace((uint)face).OffsetU;
                offset.y = tex.GetFace((uint)face).OffsetV;
                offset.z = 0.0;
                return offset;
            }

            return offset;
        }

        protected LSL_Float GetTextureRot(SceneObjectPart part, int face)
        {
            var tex = part.Shape.Textures;
            if (face == -1) face = 0;
            if (face >= 0 && face < GetNumberOfSides(part))
                return tex.GetFace((uint)face).Rotation;
            return 0.0;
        }

        private void SetTextureAnim(SceneObjectPart part, int mode, int face, int sizex, int sizey, double start,
            double length, double rate)
        {
            //ALL_SIDES
            if (face == ScriptBaseClass.ALL_SIDES)
                face = 255;

            var pTexAnim = new Primitive.TextureAnimation
            {
                Flags = (Primitive.TextureAnimMode)mode,
                Face = (uint)face,
                Length = (float)length,
                Rate = (float)rate,
                SizeX = (uint)sizex,
                SizeY = (uint)sizey,
                Start = (float)start
            };

            part.AddTextureAnimation(pTexAnim);
            part.SendFullUpdateToAllClients();
            part.ParentGroup.HasGroupChanged = true;
        }

        internal Primitive.ParticleSystem.ParticleDataFlags ConvertUINTtoFlags(uint flags)
        {
            var returnval = Primitive.ParticleSystem.ParticleDataFlags.None;

            return returnval;
        }

        protected Primitive.ParticleSystem getNewParticleSystemWithSLDefaultValues()
        {
            var ps = new Primitive.ParticleSystem
            {
                PartStartColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f),
                PartEndColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f),
                PartStartScaleX = 1.0f,
                PartStartScaleY = 1.0f,
                PartEndScaleX = 1.0f,
                PartEndScaleY = 1.0f,
                BurstSpeedMin = 1.0f,
                BurstSpeedMax = 1.0f,
                BurstRate = 0.1f,
                PartMaxAge = 10.0f,
                BurstPartCount = 1,
                BlendFuncSource = ScriptBaseClass.PSYS_PART_BF_SOURCE_ALPHA,
                BlendFuncDest = ScriptBaseClass.PSYS_PART_BF_ONE_MINUS_SOURCE_ALPHA,
                PartStartGlow = 0.0f,
                PartEndGlow = 0.0f
            };

            return ps;
        }

        public void SetParticleSystem(SceneObjectPart part, LSL_List rules, string originFunc, bool expire = false)
        {
            if (rules.Length == 0)
            {
                part.RemoveParticleSystem();
                part.ParentGroup.HasGroupChanged = true;
            }
            else
            {
                var prules = getNewParticleSystemWithSLDefaultValues();
                var tempv = new LSL_Vector();

                float tempf = 0;
                var tmpi = 0;

                for (var i = 0; i < rules.Length; i += 2)
                {
                    int psystype;
                    try
                    {
                        psystype = rules.GetLSLIntegerItem(i);
                    }
                    catch (InvalidCastException)
                    {
                        Error(originFunc,
                            string.Format(
                                "Error running particle system params index #{0}: particle system parameter type must be integer",
                                i));
                        return;
                    }

                    switch (psystype)
                    {
                        case ScriptBaseClass.PSYS_PART_FLAGS:
                            try
                            {
                                prules.PartDataFlags =
                                    (Primitive.ParticleSystem.ParticleDataFlags)(uint)rules.GetLSLIntegerItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_FLAGS: arg #{0} - parameter 1 must be integer",
                                        i + 1));
                                return;
                            }

                            break;

                        case ScriptBaseClass.PSYS_PART_START_COLOR:
                            try
                            {
                                tempv = rules.GetVector3Item(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_START_COLOR: arg #{0} - parameter 1 must be vector",
                                        i + 1));
                                return;
                            }

                            prules.PartStartColor.R = (float)tempv.x;
                            prules.PartStartColor.G = (float)tempv.y;
                            prules.PartStartColor.B = (float)tempv.z;
                            break;

                        case ScriptBaseClass.PSYS_PART_START_ALPHA:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_START_ALPHA: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.PartStartColor.A = tempf;
                            break;

                        case ScriptBaseClass.PSYS_PART_END_COLOR:
                            try
                            {
                                tempv = rules.GetVector3Item(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_END_COLOR: arg #{0} - parameter 1 must be vector",
                                        i + 1));
                                return;
                            }

                            prules.PartEndColor.R = (float)tempv.x;
                            prules.PartEndColor.G = (float)tempv.y;
                            prules.PartEndColor.B = (float)tempv.z;
                            break;

                        case ScriptBaseClass.PSYS_PART_END_ALPHA:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_END_ALPHA: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.PartEndColor.A = tempf;
                            break;

                        case ScriptBaseClass.PSYS_PART_START_SCALE:
                            try
                            {
                                tempv = rules.GetVector3Item(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_START_SCALE: arg #{0} - parameter 1 must be vector",
                                        i + 1));
                                return;
                            }

                            prules.PartStartScaleX = validParticleScale((float)tempv.x);
                            prules.PartStartScaleY = validParticleScale((float)tempv.y);
                            break;

                        case ScriptBaseClass.PSYS_PART_END_SCALE:
                            try
                            {
                                tempv = rules.GetVector3Item(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_END_SCALE: arg #{0} - parameter 1 must be vector",
                                        i + 1));
                                return;
                            }

                            prules.PartEndScaleX = validParticleScale((float)tempv.x);
                            prules.PartEndScaleY = validParticleScale((float)tempv.y);
                            break;

                        case ScriptBaseClass.PSYS_PART_MAX_AGE:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_MAX_AGE: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.PartMaxAge = tempf;
                            break;

                        case ScriptBaseClass.PSYS_SRC_ACCEL:
                            try
                            {
                                tempv = rules.GetVector3Item(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_ACCEL: arg #{0} - parameter 1 must be vector",
                                        i + 1));
                                return;
                            }

                            prules.PartAcceleration.X = (float)tempv.x;
                            prules.PartAcceleration.Y = (float)tempv.y;
                            prules.PartAcceleration.Z = (float)tempv.z;
                            break;

                        case ScriptBaseClass.PSYS_SRC_PATTERN:
                            try
                            {
                                tmpi = rules.GetLSLIntegerItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_PATTERN: arg #{0} - parameter 1 must be integer",
                                        i + 1));
                                return;
                            }

                            prules.Pattern = (Primitive.ParticleSystem.SourcePattern)tmpi;
                            break;

                        // PSYS_SRC_INNERANGLE and PSYS_SRC_ANGLE_BEGIN use the same variables. The
                        // PSYS_SRC_OUTERANGLE and PSYS_SRC_ANGLE_END also use the same variable. The
                        // client tells the difference between the two by looking at the 0x02 bit in
                        // the PartFlags variable.
                        case ScriptBaseClass.PSYS_SRC_INNERANGLE:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_INNERANGLE: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.InnerAngle = tempf;
                            prules.PartFlags &= 0xFFFFFFFD; // Make sure new angle format is off.
                            break;

                        case ScriptBaseClass.PSYS_SRC_OUTERANGLE:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_OUTERANGLE: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.OuterAngle = tempf;
                            prules.PartFlags &= 0xFFFFFFFD; // Make sure new angle format is off.
                            break;

                        case ScriptBaseClass.PSYS_PART_BLEND_FUNC_SOURCE:
                            try
                            {
                                tmpi = rules.GetLSLIntegerItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_BLEND_FUNC_SOURCE: arg #{0} - parameter 1 must be integer",
                                        i + 1));
                                return;
                            }

                            prules.BlendFuncSource = (byte)tmpi;
                            break;

                        case ScriptBaseClass.PSYS_PART_BLEND_FUNC_DEST:
                            try
                            {
                                tmpi = rules.GetLSLIntegerItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_BLEND_FUNC_DEST: arg #{0} - parameter 1 must be integer",
                                        i + 1));
                                return;
                            }

                            prules.BlendFuncDest = (byte)tmpi;
                            break;

                        case ScriptBaseClass.PSYS_PART_START_GLOW:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_START_GLOW: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.PartStartGlow = tempf;
                            break;

                        case ScriptBaseClass.PSYS_PART_END_GLOW:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_PART_END_GLOW: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.PartEndGlow = tempf;
                            break;

                        case ScriptBaseClass.PSYS_SRC_TEXTURE:
                            try
                            {
                                prules.Texture =
                                    ScriptUtils.GetAssetIdFromKeyOrItemName(m_host, rules.GetStrictStringItem(i + 1));
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_TEXTURE: arg #{0} - parameter 1 must be string or key",
                                        i + 1));
                                return;
                            }

                            break;

                        case ScriptBaseClass.PSYS_SRC_BURST_RATE:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_BURST_RATE: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.BurstRate = tempf;
                            break;

                        case ScriptBaseClass.PSYS_SRC_BURST_PART_COUNT:
                            try
                            {
                                prules.BurstPartCount = (byte)(int)rules.GetLSLIntegerItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_BURST_PART_COUNT: arg #{0} - parameter 1 must be integer",
                                        i + 1));
                                return;
                            }

                            break;

                        case ScriptBaseClass.PSYS_SRC_BURST_RADIUS:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_BURST_RADIUS: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.BurstRadius = tempf;
                            break;

                        case ScriptBaseClass.PSYS_SRC_BURST_SPEED_MIN:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_BURST_SPEED_MIN: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.BurstSpeedMin = tempf;
                            break;

                        case ScriptBaseClass.PSYS_SRC_BURST_SPEED_MAX:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_BURST_SPEED_MAX: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.BurstSpeedMax = tempf;
                            break;

                        case ScriptBaseClass.PSYS_SRC_MAX_AGE:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_MAX_AGE: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.MaxAge = tempf;
                            break;

                        case ScriptBaseClass.PSYS_SRC_TARGET_KEY:
                            if (UUID.TryParse(rules.Data[i + 1].ToString(), out var key))
                                prules.Target = key;
                            else
                                prules.Target = part.UUID;
                            break;

                        case ScriptBaseClass.PSYS_SRC_OMEGA:
                            // AL: This is an assumption, since it is the only thing that would match.
                            try
                            {
                                tempv = rules.GetVector3Item(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_OMEGA: arg #{0} - parameter 1 must be vector",
                                        i + 1));
                                return;
                            }

                            prules.AngularVelocity.X = (float)tempv.x;
                            prules.AngularVelocity.Y = (float)tempv.y;
                            prules.AngularVelocity.Z = (float)tempv.z;
                            break;

                        case ScriptBaseClass.PSYS_SRC_ANGLE_BEGIN:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_ANGLE_BEGIN: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.InnerAngle = tempf;
                            prules.PartFlags |= 0x02; // Set new angle format.
                            break;

                        case ScriptBaseClass.PSYS_SRC_ANGLE_END:
                            try
                            {
                                tempf = (float)rules.GetLSLFloatItem(i + 1);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule PSYS_SRC_ANGLE_END: arg #{0} - parameter 1 must be float",
                                        i + 1));
                                return;
                            }

                            prules.OuterAngle = tempf;
                            prules.PartFlags |= 0x02; // Set new angle format.
                            break;
                    }
                }

                prules.CRC = 1;

                part.AddNewParticleSystem(prules, expire);
                if (!expire || prules.MaxAge != 0 || prules.MaxAge > 300)
                    part.ParentGroup.HasGroupChanged = true;
            }

            part.SendFullUpdateToAllClients();
        }

        private float validParticleScale(float value)
        {
            if (value > 7.96f) return 7.96f;
            return value;
        }

        protected void SitTarget(SceneObjectPart part, LSL_Vector offset, LSL_Rotation rot)
        {
            // LSL quaternions can normalize to 0, normal Quaternions can't.
            if (rot.s == 0 && rot.x == 0 && rot.y == 0 && rot.z == 0)
                rot.s = 1; // ZERO_ROTATION = 0,0,0,1

            part.SitTargetPosition = offset;
            part.SitTargetOrientation = rot;
            part.ParentGroup.HasGroupChanged = true;
        }

        protected ObjectShapePacket.ObjectDataBlock SetPrimitiveBlockShapeParams(SceneObjectPart part, int holeshape,
            LSL_Vector cut, float hollow, LSL_Vector twist, byte profileshape, byte pathcurve)
        {
            float tempFloat; // Use in float expressions below to avoid byte cast precision issues.
            var shapeBlock = new ObjectShapePacket.ObjectDataBlock();
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return shapeBlock;

            if (holeshape != ScriptBaseClass.PRIM_HOLE_DEFAULT &&
                holeshape != ScriptBaseClass.PRIM_HOLE_CIRCLE &&
                holeshape != ScriptBaseClass.PRIM_HOLE_SQUARE &&
                holeshape != ScriptBaseClass.PRIM_HOLE_TRIANGLE)
                holeshape = ScriptBaseClass.PRIM_HOLE_DEFAULT;
            shapeBlock.PathCurve = pathcurve;
            shapeBlock.ProfileCurve = (byte)holeshape; // Set the hole shape.
            shapeBlock.ProfileCurve += profileshape; // Add in the profile shape.
            if (cut.x < 0f)
                cut.x = 0f;
            else if (cut.x > 1f) cut.x = 1f;
            if (cut.y < 0f)
                cut.y = 0f;
            else if (cut.y > 1f) cut.y = 1f;
            if (cut.y - cut.x < 0.02f)
            {
                cut.x = cut.y - 0.02f;
                if (cut.x < 0.0f)
                {
                    cut.x = 0.0f;
                    cut.y = 0.02f;
                }
            }

            shapeBlock.ProfileBegin = (ushort)(50000 * cut.x);
            shapeBlock.ProfileEnd = (ushort)(50000 * (1 - cut.y));
            if (hollow < 0f) hollow = 0f;
            // If the prim is a Cylinder, Prism, Sphere, Torus or Ring (or not a
            // Box or Tube) and the hole shape is a square, hollow is limited to
            // a max of 70%. The viewer performs its own check on this value but
            // we need to do it here also so llGetPrimitiveParams can have access
            // to the correct value.
            if (profileshape != (byte)ProfileCurve.Square &&
                holeshape == ScriptBaseClass.PRIM_HOLE_SQUARE)
            {
                if (hollow > 0.70f) hollow = 0.70f;
            }
            // Otherwise, hollow is limited to 99%.
            else
            {
                if (hollow > 0.99f) hollow = 0.99f;
            }

            shapeBlock.ProfileHollow = (ushort)(50000 * hollow);
            if (twist.x < -1.0f)
                twist.x = -1.0f;
            else if (twist.x > 1.0f) twist.x = 1.0f;
            if (twist.y < -1.0f)
                twist.y = -1.0f;
            else if (twist.y > 1.0f) twist.y = 1.0f;
            tempFloat = 100.0f * (float)twist.x;
            if (tempFloat >= 0)
                tempFloat += 0.5f;
            else
                tempFloat -= 0.5f;
            shapeBlock.PathTwistBegin = (sbyte)tempFloat;

            tempFloat = 100.0f * (float)twist.y;
            if (tempFloat >= 0)
                tempFloat += 0.5f;
            else
                tempFloat -= 0.5f;
            shapeBlock.PathTwist = (sbyte)tempFloat;

            shapeBlock.ObjectLocalID = part.LocalId;

            part.Shape.SculptEntry = false;
            return shapeBlock;
        }

        // Prim type box, cylinder and prism.
        protected void SetPrimitiveShapeParams(SceneObjectPart part, int holeshape, LSL_Vector cut, float hollow,
            LSL_Vector twist, LSL_Vector taper_b, LSL_Vector topshear, byte profileshape, byte pathcurve)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            float tempFloat; // Use in float expressions below to avoid byte cast precision issues.
            ObjectShapePacket.ObjectDataBlock shapeBlock;

            shapeBlock = SetPrimitiveBlockShapeParams(part, holeshape, cut, hollow, twist, profileshape, pathcurve);

            if (taper_b.x < 0f)
                taper_b.x = 0f;
            else if (taper_b.x > 2f) taper_b.x = 2f;
            if (taper_b.y < 0f)
                taper_b.y = 0f;
            else if (taper_b.y > 2f) taper_b.y = 2f;
            tempFloat = 100.0f * (2.0f - (float)taper_b.x);
            if (tempFloat >= 0)
                tempFloat += 0.5f;
            else
                tempFloat -= 0.5f;
            shapeBlock.PathScaleX = (byte)tempFloat;

            tempFloat = 100.0f * (2.0f - (float)taper_b.y);
            if (tempFloat >= 0)
                tempFloat += 0.5f;
            else
                tempFloat -= 0.5f;
            shapeBlock.PathScaleY = (byte)tempFloat;

            if (topshear.x < -0.5f)
                topshear.x = -0.5f;
            else if (topshear.x > 0.5f) topshear.x = 0.5f;
            if (topshear.y < -0.5f)
                topshear.y = -0.5f;
            else if (topshear.y > 0.5f) topshear.y = 0.5f;
            tempFloat = 100.0f * (float)topshear.x;
            if (tempFloat >= 0)
                tempFloat += 0.5f;
            else
                tempFloat -= 0.5f;
            shapeBlock.PathShearX = (byte)tempFloat;

            tempFloat = 100.0f * (float)topshear.y;
            if (tempFloat >= 0)
                tempFloat += 0.5f;
            else
                tempFloat -= 0.5f;
            shapeBlock.PathShearY = (byte)tempFloat;

            part.Shape.SculptEntry = false;
            part.UpdateShape(shapeBlock);
        }

        // Prim type sphere.
        protected void SetPrimitiveShapeParams(SceneObjectPart part, int holeshape, LSL_Vector cut, float hollow,
            LSL_Vector twist, LSL_Vector dimple, byte profileshape, byte pathcurve)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            ObjectShapePacket.ObjectDataBlock shapeBlock;

            shapeBlock = SetPrimitiveBlockShapeParams(part, holeshape, cut, hollow, twist, profileshape, pathcurve);

            // profile/path swapped for a sphere
            shapeBlock.PathBegin = shapeBlock.ProfileBegin;
            shapeBlock.PathEnd = shapeBlock.ProfileEnd;

            shapeBlock.PathScaleX = 100;
            shapeBlock.PathScaleY = 100;

            if (dimple.x < 0f)
                dimple.x = 0f;
            else if (dimple.x > 1f) dimple.x = 1f;
            if (dimple.y < 0f)
                dimple.y = 0f;
            else if (dimple.y > 1f) dimple.y = 1f;
            if (dimple.y - dimple.x < 0.02f)
            {
                dimple.x = dimple.y - 0.02f;
                if (dimple.x < 0.0f)
                {
                    dimple.x = 0.0f;
                    dimple.y = 0.02f;
                }
            }

            shapeBlock.ProfileBegin = (ushort)(50000 * dimple.x);
            shapeBlock.ProfileEnd = (ushort)(50000 * (1 - dimple.y));

            part.Shape.SculptEntry = false;
            part.UpdateShape(shapeBlock);
        }

        // Prim type torus, tube and ring.
        protected void SetPrimitiveShapeParams(SceneObjectPart part, int holeshape, LSL_Vector cut, float hollow,
            LSL_Vector twist, LSL_Vector holesize, LSL_Vector topshear, LSL_Vector profilecut, LSL_Vector taper_a,
            float revolutions, float radiusoffset, float skew, byte profileshape, byte pathcurve)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            float tempFloat; // Use in float expressions below to avoid byte cast precision issues.
            ObjectShapePacket.ObjectDataBlock shapeBlock;

            shapeBlock = SetPrimitiveBlockShapeParams(part, holeshape, cut, hollow, twist, profileshape, pathcurve);

            // profile/path swapped for a torrus, tube, ring
            shapeBlock.PathBegin = shapeBlock.ProfileBegin;
            shapeBlock.PathEnd = shapeBlock.ProfileEnd;

            if (holesize.x < 0.01f)
                holesize.x = 0.01f;
            else if (holesize.x > 1f) holesize.x = 1f;
            tempFloat = 100.0f * (2.0f - (float)holesize.x) + 0.5f;
            shapeBlock.PathScaleX = (byte)tempFloat;

            if (holesize.y < 0.01f)
                holesize.y = 0.01f;
            else if (holesize.y > 0.5f) holesize.y = 0.5f;
            tempFloat = 100.0f * (2.0f - (float)holesize.y) + 0.5f;
            shapeBlock.PathScaleY = (byte)tempFloat;

            if (topshear.x < -0.5f)
                topshear.x = -0.5f;
            else if (topshear.x > 0.5f) topshear.x = 0.5f;
            tempFloat = (float)(100.0d * topshear.x);
            if (tempFloat >= 0)
                tempFloat += 0.5f;
            else
                tempFloat -= 0.5f;
            shapeBlock.PathShearX = (byte)tempFloat;

            if (topshear.y < -0.5f)
                topshear.y = -0.5f;
            else if (topshear.y > 0.5f) topshear.y = 0.5f;
            tempFloat = (float)(100.0d * topshear.y);
            if (tempFloat >= 0)
                tempFloat += 0.5f;
            else
                tempFloat -= 0.5f;
            shapeBlock.PathShearY = (byte)tempFloat;

            if (profilecut.x < 0f)
                profilecut.x = 0f;
            else if (profilecut.x > 1f) profilecut.x = 1f;
            if (profilecut.y < 0f)
                profilecut.y = 0f;
            else if (profilecut.y > 1f) profilecut.y = 1f;
            if (profilecut.y - profilecut.x < 0.02f)
            {
                profilecut.x = profilecut.y - 0.02f;
                if (profilecut.x < 0.0f)
                {
                    profilecut.x = 0.0f;
                    profilecut.y = 0.02f;
                }
            }

            shapeBlock.ProfileBegin = (ushort)(50000 * profilecut.x);
            shapeBlock.ProfileEnd = (ushort)(50000 * (1 - profilecut.y));
            if (taper_a.x < -1f) taper_a.x = -1f;
            if (taper_a.x > 1f) taper_a.x = 1f;
            tempFloat = 100.0f * (float)taper_a.x;
            if (tempFloat >= 0)
                tempFloat += 0.5f;
            else
                tempFloat -= 0.5f;
            shapeBlock.PathTaperX = (sbyte)tempFloat;

            if (taper_a.y < -1f)
                taper_a.y = -1f;
            else if (taper_a.y > 1f) taper_a.y = 1f;
            tempFloat = 100.0f * (float)taper_a.y;
            if (tempFloat >= 0)
                tempFloat += 0.5f;
            else
                tempFloat -= 0.5f;
            shapeBlock.PathTaperY = (sbyte)tempFloat;

            if (revolutions < 1f) revolutions = 1f;
            if (revolutions > 4f) revolutions = 4f;
            tempFloat = 66.66667f * (revolutions - 1.0f) + 0.5f;
            shapeBlock.PathRevolutions = (byte)tempFloat;
            // limits on radiusoffset depend on revolutions and hole size (how?) seems like the maximum range is 0 to 1
            if (radiusoffset < 0f) radiusoffset = 0f;
            if (radiusoffset > 1f) radiusoffset = 1f;
            tempFloat = 100.0f * radiusoffset + 0.5f;
            shapeBlock.PathRadiusOffset = (sbyte)tempFloat;
            if (skew < -0.95f) skew = -0.95f;
            if (skew > 0.95f) skew = 0.95f;
            tempFloat = 100.0f * skew;
            if (tempFloat >= 0)
                tempFloat += 0.5f;
            else
                tempFloat -= 0.5f;
            shapeBlock.PathSkew = (sbyte)tempFloat;

            part.Shape.SculptEntry = false;
            part.UpdateShape(shapeBlock);
        }

        // Prim type sculpt.
        protected void SetPrimitiveShapeParams(SceneObjectPart part, string map, int type, byte pathcurve)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            var shapeBlock = new ObjectShapePacket.ObjectDataBlock();

            if (!UUID.TryParse(map, out var sculptId))
                sculptId = ScriptUtils.GetAssetIdFromItemName(m_host, map, (int)AssetType.Texture);

            if (sculptId.IsZero())
                return;

            shapeBlock.PathCurve = pathcurve;
            shapeBlock.ObjectLocalID = part.LocalId;
            shapeBlock.PathScaleX = 100;
            shapeBlock.PathScaleY = 150;

            var flag = type & (ScriptBaseClass.PRIM_SCULPT_FLAG_INVERT | ScriptBaseClass.PRIM_SCULPT_FLAG_MIRROR);

            if (type != (ScriptBaseClass.PRIM_SCULPT_TYPE_CYLINDER | flag) &&
                type != (ScriptBaseClass.PRIM_SCULPT_TYPE_PLANE | flag) &&
                type != (ScriptBaseClass.PRIM_SCULPT_TYPE_SPHERE | flag) &&
                type != (ScriptBaseClass.PRIM_SCULPT_TYPE_TORUS | flag))
                // default
                type = type | ScriptBaseClass.PRIM_SCULPT_TYPE_SPHERE;

            part.Shape.SetSculptProperties((byte)type, sculptId);
            part.Shape.SculptEntry = true;
            part.UpdateShape(shapeBlock);
        }

        private void SetLinkPrimParams(int linknumber, LSL_List rules, string originFunc)
        {
            var parts = new List<object>();
            List<SceneObjectPart> prims = GetLinkParts(linknumber);
            List<ScenePresence> avatars = GetLinkAvatars(linknumber);
            foreach (var p in prims)
                parts.Add(p);
            foreach (var p in avatars)
                parts.Add(p);

            var remaining = new LSL_List();
            uint rulesParsed = 0;

            if (parts.Count > 0)
            {
                foreach (var part in parts)
                    if (part is SceneObjectPart)
                        remaining = SetPrimParams((SceneObjectPart)part, rules, originFunc, ref rulesParsed);
                    else
                        remaining = SetPrimParams((ScenePresence)part, rules, originFunc, ref rulesParsed);

                while (remaining.Length > 2)
                {
                    linknumber = remaining.GetLSLIntegerItem(0);
                    rules = remaining.GetSublist(1, -1);
                    parts.Clear();
                    prims = GetLinkParts(linknumber);
                    avatars = GetLinkAvatars(linknumber);
                    foreach (var p in prims)
                        parts.Add(p);
                    foreach (var p in avatars)
                        parts.Add(p);

                    remaining = new LSL_List();
                    foreach (var part in parts)
                        if (part is SceneObjectPart)
                            remaining = SetPrimParams((SceneObjectPart)part, rules, originFunc, ref rulesParsed);
                        else
                            remaining = SetPrimParams((ScenePresence)part, rules, originFunc, ref rulesParsed);
                }
            }
        }

        private void SetPhysicsMaterial(SceneObjectPart part, int material_bits,
            float material_density, float material_friction,
            float material_restitution, float material_gravity_modifier)
        {
            var physdata = new ExtraPhysicsData
            {
                PhysShapeType = (PhysShapeType)part.PhysicsShapeType,
                Density = part.Density,
                Friction = part.Friction,
                Bounce = part.Restitution,
                GravitationModifier = part.GravityModifier
            };

            if ((material_bits & ScriptBaseClass.DENSITY) != 0)
                physdata.Density = material_density;
            if ((material_bits & ScriptBaseClass.FRICTION) != 0)
                physdata.Friction = material_friction;
            if ((material_bits & ScriptBaseClass.RESTITUTION) != 0)
                physdata.Bounce = material_restitution;
            if ((material_bits & ScriptBaseClass.GRAVITY_MULTIPLIER) != 0)
                physdata.GravitationModifier = material_gravity_modifier;

            part.UpdateExtraPhysics(physdata);
        }

        // vector up using libomv (c&p from sop )
        // vector up rotated by r
        private Vector3 Zrot(Quaternion r)
        {
            double x, y, z, m;

            m = r.X * r.X + r.Y * r.Y + r.Z * r.Z + r.W * r.W;
            if (Math.Abs(1.0 - m) > 0.000001)
            {
                m = 1.0 / Math.Sqrt(m);
                r.X *= (float)m;
                r.Y *= (float)m;
                r.Z *= (float)m;
                r.W *= (float)m;
            }

            x = 2 * (r.X * r.Z + r.Y * r.W);
            y = 2 * (-r.X * r.W + r.Y * r.Z);
            z = -r.X * r.X - r.Y * r.Y + r.Z * r.Z + r.W * r.W;

            return new Vector3((float)x, (float)y, (float)z);
        }

        protected LSL_List SetPrimParams(SceneObjectPart part, LSL_List rules, string originFunc, ref uint rulesParsed)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return new LSL_List();

            var idx = 0;
            var idxStart = 0;

            var parentgrp = part.ParentGroup;

            var positionChanged = false;
            var materialChanged = false;
            LSL_Vector currentPosition = GetPartLocalPos(part);

            try
            {
                while (idx < rules.Length)
                {
                    ++rulesParsed;
                    int code = rules.GetLSLIntegerItem(idx++);

                    var remain = rules.Length - idx;
                    idxStart = idx;

                    int face;
                    LSL_Vector v;

                    switch (code)
                    {
                        case ScriptBaseClass.PRIM_POSITION:
                        case ScriptBaseClass.PRIM_POS_LOCAL:
                            if (remain < 1)
                                return new LSL_List();

                            try
                            {
                                v = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                if (code == ScriptBaseClass.PRIM_POSITION)
                                    Error(originFunc,
                                        string.Format(
                                            "Error running rule #{0} -> PRIM_POSITION: arg #{1} - parameter 1 must be vector",
                                            rulesParsed, idx - idxStart - 1));
                                else
                                    Error(originFunc,
                                        string.Format(
                                            "Error running rule #{0} -> PRIM_POS_LOCAL: arg #{1} - parameter 1 must be vector",
                                            rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            if (part.IsRoot && !part.ParentGroup.IsAttachment)
                                currentPosition = GetSetPosTarget(part, v, currentPosition, true);
                            else
                                currentPosition = GetSetPosTarget(part, v, currentPosition, false);
                            positionChanged = true;

                            break;
                        case ScriptBaseClass.PRIM_SIZE:
                            if (remain < 1)
                                return new LSL_List();

                            v = rules.GetVector3Item(idx++);
                            SetScale(part, v);

                            break;
                        case ScriptBaseClass.PRIM_ROTATION:
                            if (remain < 1)
                                return new LSL_List();
                            LSL_Rotation q;
                            try
                            {
                                q = rules.GetQuaternionItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_ROTATION: arg #{1} - parameter 1 must be rotation",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            // try to let this work as in SL...
                            if (part.ParentID == 0 || (part.ParentGroup != null && part == part.ParentGroup.RootPart))
                            {
                                // special case: If we are root, rotate complete SOG to new rotation
                                SetRot(part, q);
                            }
                            else
                            {
                                // we are a child. The rotation values will be set to the one of root modified by rot, as in SL. Don't ask.
                                var rootPart = part.ParentGroup.RootPart;
                                SetRot(part, rootPart.RotationOffset * (Quaternion)q);
                            }

                            break;

                        case ScriptBaseClass.PRIM_TYPE:
                            if (remain < 3)
                                return new LSL_List();

                            try
                            {
                                code = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_TYPE: arg #{1} - parameter 1 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            remain = rules.Length - idx;
                            float hollow;
                            LSL_Vector twist;
                            LSL_Vector taper_b;
                            LSL_Vector topshear;
                            float revolutions;
                            float radiusoffset;
                            float skew;
                            LSL_Vector holesize;
                            LSL_Vector profilecut;

                            switch (code)
                            {
                                case ScriptBaseClass.PRIM_TYPE_BOX:
                                    if (remain < 6)
                                        return new LSL_List();

                                    try
                                    {
                                        face = rules.GetLSLIntegerItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_BOX: arg #{1} - parameter 2 must be integer",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        v = rules.GetVector3Item(idx++); // cut
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_BOX: arg #{1} - parameter 3 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        hollow = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_BOX: arg #{1} - parameter 4 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        twist = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_BOX: arg #{1} - parameter 5 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        taper_b = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_BOX: arg #{1} - parameter 6 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        topshear = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_BOX: arg #{1} - parameter 7 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    SetPrimitiveShapeParams(part, face, v, hollow, twist, taper_b, topshear,
                                        (byte)ProfileShape.Square, (byte)Extrusion.Straight);
                                    break;

                                case ScriptBaseClass.PRIM_TYPE_CYLINDER:
                                    if (remain < 6)
                                        return new LSL_List();

                                    try
                                    {
                                        face = rules.GetLSLIntegerItem(idx++); // holeshape
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_CYLINDER: arg #{1} - parameter 3 must be integer",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        v = rules.GetVector3Item(idx++); // cut
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_CYLINDER: arg #{1} - parameter 4 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        hollow = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_CYLINDER: arg #{1} - parameter 5 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        twist = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_CYLINDER: arg #{1} - parameter 6 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        taper_b = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_CYLINDER: arg #{1} - parameter 7 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        topshear = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_CYLINDER: arg #{1} - parameter 8 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    SetPrimitiveShapeParams(part, face, v, hollow, twist, taper_b, topshear,
                                        (byte)ProfileShape.Circle, (byte)Extrusion.Straight);
                                    break;

                                case ScriptBaseClass.PRIM_TYPE_PRISM:
                                    if (remain < 6)
                                        return new LSL_List();

                                    try
                                    {
                                        face = rules.GetLSLIntegerItem(idx++); // holeshape
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_PRISM: arg #{1} - parameter 3 must be integer",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        v = rules.GetVector3Item(idx++); //cut
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_PRISM: arg #{1} - parameter 4 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        hollow = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_PRISM: arg #{1} - parameter 5 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        twist = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_PRISM: arg #{1} - parameter 6 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        taper_b = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_PRISM: arg #{1} - parameter 7 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        topshear = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_PRISM: arg #{1} - parameter 8 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    SetPrimitiveShapeParams(part, face, v, hollow, twist, taper_b, topshear,
                                        (byte)ProfileShape.EquilateralTriangle, (byte)Extrusion.Straight);
                                    break;

                                case ScriptBaseClass.PRIM_TYPE_SPHERE:
                                    if (remain < 5)
                                        return new LSL_List();

                                    try
                                    {
                                        face = rules.GetLSLIntegerItem(idx++); // holeshape
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_SPHERE: arg #{1} - parameter 3 must be integer",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        v = rules.GetVector3Item(idx++); // cut
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_SPHERE: arg #{1} - parameter 4 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        hollow = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_SPHERE: arg #{1} - parameter 5 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        twist = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_SPHERE: arg #{1} - parameter 6 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        taper_b = rules.GetVector3Item(idx++); // dimple
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_SPHERE: arg #{1} - parameter 7 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    SetPrimitiveShapeParams(part, face, v, hollow, twist, taper_b,
                                        (byte)ProfileShape.HalfCircle, (byte)Extrusion.Curve1);
                                    break;

                                case ScriptBaseClass.PRIM_TYPE_TORUS:
                                    if (remain < 11)
                                        return new LSL_List();

                                    try
                                    {
                                        face = rules.GetLSLIntegerItem(idx++); // holeshape
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TORUS: arg #{1} - parameter 3 must be integer",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        v = rules.GetVector3Item(idx++); //cut
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TORUS: arg #{1} - parameter 4 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        hollow = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TORUS: arg #{1} - parameter 5 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        twist = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TORUS: arg #{1} - parameter 6 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        holesize = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TORUS: arg #{1} - parameter 7 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        topshear = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TORUS: arg #{1} - parameter 8 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        profilecut = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TORUS: arg #{1} - parameter 9 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        taper_b = rules.GetVector3Item(idx++); // taper_a
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TORUS: arg #{1} - parameter 10 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        revolutions = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TORUS: arg #{1} - parameter 11 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        radiusoffset = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TORUS: arg #{1} - parameter 12 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        skew = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TORUS: arg #{1} - parameter 13 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    SetPrimitiveShapeParams(part, face, v, hollow, twist, holesize, topshear,
                                        profilecut, taper_b,
                                        revolutions, radiusoffset, skew, (byte)ProfileShape.Circle,
                                        (byte)Extrusion.Curve1);
                                    break;

                                case ScriptBaseClass.PRIM_TYPE_TUBE:
                                    if (remain < 11)
                                        return new LSL_List();

                                    try
                                    {
                                        face = rules.GetLSLIntegerItem(idx++); // holeshape
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TUBE: arg #{1} - parameter 3 must be integer",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        v = rules.GetVector3Item(idx++); //cut
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TUBE: arg #{1} - parameter 4 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        hollow = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TUBE: arg #{1} - parameter 5 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        twist = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TUBE: arg #{1} - parameter 6 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        holesize = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TUBE: arg #{1} - parameter 7 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        topshear = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TUBE: arg #{1} - parameter 8 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        profilecut = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TUBE: arg #{1} - parameter 9 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        taper_b = rules.GetVector3Item(idx++); // taper_a
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TUBE: arg #{1} - parameter 10 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        revolutions = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TUBE: arg #{1} - parameter 11 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        radiusoffset = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TUBE: arg #{1} - parameter 12 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        skew = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_TUBE: arg #{1} - parameter 13 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    SetPrimitiveShapeParams(part, face, v, hollow, twist, holesize, topshear,
                                        profilecut, taper_b,
                                        revolutions, radiusoffset, skew, (byte)ProfileShape.Square,
                                        (byte)Extrusion.Curve1);
                                    break;

                                case ScriptBaseClass.PRIM_TYPE_RING:
                                    if (remain < 11)
                                        return new LSL_List();

                                    try
                                    {
                                        face = rules.GetLSLIntegerItem(idx++); // holeshape
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_RING: arg #{1} - parameter 3 must be integer",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        v = rules.GetVector3Item(idx++); //cut
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_RING: arg #{1} - parameter 4 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        hollow = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_RING: arg #{1} - parameter 5 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        twist = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_RING: arg #{1} - parameter 6 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        holesize = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_RING: arg #{1} - parameter 7 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        topshear = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_RING: arg #{1} - parameter 8 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        profilecut = rules.GetVector3Item(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_RING: arg #{1} - parameter 9 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        taper_b = rules.GetVector3Item(idx++); // taper_a
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_RING: arg #{1} - parameter 10 must be vector",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        revolutions = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_RING: arg #{1} - parameter 11 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        radiusoffset = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_RING: arg #{1} - parameter 12 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    try
                                    {
                                        skew = (float)rules.GetLSLFloatItem(idx++);
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_RING: arg #{1} - parameter 13 must be float",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    SetPrimitiveShapeParams(part, face, v, hollow, twist, holesize, topshear,
                                        profilecut, taper_b,
                                        revolutions, radiusoffset, skew, (byte)ProfileShape.EquilateralTriangle,
                                        (byte)Extrusion.Curve1);
                                    break;

                                case ScriptBaseClass.PRIM_TYPE_SCULPT:
                                    if (remain < 2)
                                        return new LSL_List();

                                    var map = rules.Data[idx++].ToString();
                                    try
                                    {
                                        face = rules.GetLSLIntegerItem(idx++); // type
                                    }
                                    catch (InvalidCastException)
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_TYPE, PRIM_TYPE_SCULPT: arg #{1} - parameter 4 must be integer",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }

                                    SetPrimitiveShapeParams(part, map, face, (byte)Extrusion.Curve1);
                                    break;
                            }

                            break;

                        case ScriptBaseClass.PRIM_TEXTURE:
                            if (remain < 5)
                                return new LSL_List();

                            face = rules.GetLSLIntegerItem(idx++);
                            string tex;
                            LSL_Vector repeats;
                            LSL_Vector offsets;
                            double rotation;

                            tex = rules.Data[idx++].ToString();
                            try
                            {
                                repeats = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_TEXTURE: arg #{1} - parameter 3 must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                offsets = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_TEXTURE: arg #{1} - parameter 4 must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                rotation = rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_TEXTURE: arg #{1} - parameter 5 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            SetTextureParams(part, tex, repeats.x, repeats.y, offsets.x, offsets.y, rotation, face);
                            break;

                        case ScriptBaseClass.PRIM_COLOR:
                            if (remain < 3)
                                return new LSL_List();

                            LSL_Vector color;
                            double alpha;

                            try
                            {
                                face = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_COLOR: arg #{1} - parameter 2 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                color = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_COLOR: arg #{1} - parameter 3 must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                alpha = rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_COLOR: arg #{1} - parameter 4 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            part.SetFaceColorAlpha(face, color, alpha);

                            break;

                        case ScriptBaseClass.PRIM_FLEXIBLE:
                            if (remain < 7)
                                return new LSL_List();
                            bool flexi;
                            int softness;
                            float gravity;
                            float friction;
                            float wind;
                            float tension;
                            LSL_Vector force;

                            try
                            {
                                flexi = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_FLEXIBLE: arg #{1} - parameter 2 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                softness = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_FLEXIBLE: arg #{1} - parameter 3 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                gravity = (float)rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_FLEXIBLE: arg #{1} - parameter 4 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                friction = (float)rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_FLEXIBLE: arg #{1} - parameter 5 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                wind = (float)rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_FLEXIBLE: arg #{1} - parameter 6 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                tension = (float)rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_FLEXIBLE: arg #{1} - parameter 7 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                force = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_FLEXIBLE: arg #{1} - parameter 8 must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            SetFlexi(part, flexi, softness, gravity, friction, wind, tension, force);

                            break;

                        case ScriptBaseClass.PRIM_POINT_LIGHT:
                            if (remain < 5)
                                return new LSL_List();
                            bool light;
                            LSL_Vector lightcolor;
                            float intensity;
                            float radius;
                            float falloff;

                            try
                            {
                                light = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_POINT_LIGHT: arg #{1} - parameter 2 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                lightcolor = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_POINT_LIGHT: arg #{1} - parameter 3 must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                intensity = (float)rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_POINT_LIGHT: arg #{1} - parameter 4 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                radius = (float)rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_POINT_LIGHT: arg #{1} - parameter 5 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                falloff = (float)rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_POINT_LIGHT: arg #{1} - parameter 6 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            SetPointLight(part, light, lightcolor, intensity, radius, falloff);

                            break;

                        case ScriptBaseClass.PRIM_GLOW:
                            if (remain < 2)
                                return new LSL_List();

                            float glow;

                            try
                            {
                                face = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_GLOW: arg #{1} - parameter 2 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                glow = (float)rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_GLOW: arg #{1} - parameter 3 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            SetGlow(part, face, glow);

                            break;

                        case ScriptBaseClass.PRIM_BUMP_SHINY:
                            if (remain < 3)
                                return new LSL_List();

                            int shiny;
                            Bumpiness bump;

                            try
                            {
                                face = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_BUMP_SHINY: arg #{1} - parameter 2 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                shiny = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_BUMP_SHINY: arg #{1} - parameter 3 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                bump = (Bumpiness)(int)rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_BUMP_SHINY: arg #{1} - parameter 4 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            SetShiny(part, face, shiny, bump);

                            break;

                        case ScriptBaseClass.PRIM_FULLBRIGHT:
                            if (remain < 2)
                                return new LSL_List();
                            bool st;

                            try
                            {
                                face = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_FULLBRIGHT: arg #{1} - parameter 2 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                st = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_FULLBRIGHT: arg #{1} - parameter 4 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            SetFullBright(part, face, st);
                            break;

                        case ScriptBaseClass.PRIM_MATERIAL:
                            if (remain < 1)
                                return new LSL_List();
                            int mat;

                            try
                            {
                                mat = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_MATERIAL: arg #{1} - parameter 2 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            if (mat < 0 || mat > 7)
                                return new LSL_List();

                            part.Material = Convert.ToByte(mat);
                            break;

                        case ScriptBaseClass.PRIM_PHANTOM:
                            if (remain < 1)
                                return new LSL_List();

                            var ph = rules.Data[idx++].ToString();
                            part.ParentGroup.ScriptSetPhantomStatus(ph.Equals("1"));

                            break;

                        case ScriptBaseClass.PRIM_PHYSICS:
                            if (remain < 1)
                                return new LSL_List();
                            var phy = rules.Data[idx++].ToString();
                            part.ScriptSetPhysicsStatus(phy.Equals("1"));
                            break;

                        case ScriptBaseClass.PRIM_PHYSICS_SHAPE_TYPE:
                            if (remain < 1)
                                return new LSL_List();

                            int shape_type;

                            try
                            {
                                shape_type = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_PHYSICS_SHAPE_TYPE: arg #{1} - parameter 2 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            var physdata = new ExtraPhysicsData
                            {
                                Density = part.Density,
                                Bounce = part.Restitution,
                                GravitationModifier = part.GravityModifier,
                                PhysShapeType = (PhysShapeType)shape_type
                            };

                            part.UpdateExtraPhysics(physdata);

                            break;

                        case ScriptBaseClass.PRIM_PHYSICS_MATERIAL:
                            if (remain < 5)
                                return new LSL_List();

                            int material_bits = rules.GetLSLIntegerItem(idx++);
                            var material_density = (float)rules.GetLSLFloatItem(idx++);
                            var material_friction = (float)rules.GetLSLFloatItem(idx++);
                            var material_restitution = (float)rules.GetLSLFloatItem(idx++);
                            var material_gravity_modifier = (float)rules.GetLSLFloatItem(idx++);

                            SetPhysicsMaterial(part, material_bits, material_density, material_friction,
                                material_restitution, material_gravity_modifier);

                            break;

                        case ScriptBaseClass.PRIM_TEMP_ON_REZ:
                            if (remain < 1)
                                return new LSL_List();
                            var temp = rules.Data[idx++].ToString();

                            part.ParentGroup.ScriptSetTemporaryStatus(temp.Equals("1"));

                            break;

                        case ScriptBaseClass.PRIM_TEXGEN:
                            if (remain < 2)
                                return new LSL_List();
                            //face,type
                            int style;

                            try
                            {
                                face = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_TEXGEN: arg #{1} - parameter 2 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                style = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_TEXGEN: arg #{1} - parameter 3 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            SetTexGen(part, face, style);
                            break;
                        case ScriptBaseClass.PRIM_TEXT:
                            if (remain < 3)
                                return new LSL_List();
                            string primText;
                            LSL_Vector primTextColor;
                            LSL_Float primTextAlpha;

                            try
                            {
                                primText = rules.GetStrictStringItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_TEXT: arg #{1} - parameter 2 must be string",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                primTextColor = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_TEXT: arg #{1} - parameter 3 must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                primTextAlpha = rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_TEXT: arg #{1} - parameter 4 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            var av3 = Vector3.Clamp(primTextColor, 0.0f, 1.0f);
                            part.SetText(primText, av3, Utils.Clamp((float)primTextAlpha, 0.0f, 1.0f));

                            break;

                        case ScriptBaseClass.PRIM_NAME:
                            if (remain < 1)
                                return new LSL_List();
                            try
                            {
                                var primName = rules.GetStrictStringItem(idx++);
                                part.Name = primName;
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_NAME: arg #{1} - parameter 2 must be string",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            break;
                        case ScriptBaseClass.PRIM_DESC:
                            if (remain < 1)
                                return new LSL_List();
                            try
                            {
                                var primDesc = rules.GetStrictStringItem(idx++);
                                part.Description = primDesc;
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_DESC: arg #{1} - parameter 2 must be string",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            break;
                        case ScriptBaseClass.PRIM_ROT_LOCAL:
                            if (remain < 1)
                                return new LSL_List();
                            LSL_Rotation rot;
                            try
                            {
                                rot = rules.GetQuaternionItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_ROT_LOCAL: arg #{1} - parameter 2 must be rotation",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            SetRot(part, rot);
                            break;

                        case ScriptBaseClass.PRIM_OMEGA:
                            if (remain < 3)
                                return new LSL_List();
                            LSL_Vector axis;
                            LSL_Float spinrate;
                            LSL_Float gain;

                            try
                            {
                                axis = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_OMEGA: arg #{1} - parameter 2 must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                spinrate = rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_OMEGA: arg #{1} - parameter 3 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            try
                            {
                                gain = rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_OMEGA: arg #{1} - parameter 4 must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            TargetOmega(part, axis, (double)spinrate, (double)gain);
                            break;

                        case ScriptBaseClass.PRIM_SLICE:
                            if (remain < 1)
                                return new LSL_List();
                            LSL_Vector slice;
                            try
                            {
                                slice = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_SLICE: arg #{1} - parameter 2 must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            part.UpdateSlice((float)slice.x, (float)slice.y);
                            break;

                        case ScriptBaseClass.PRIM_SIT_TARGET:
                            if (remain < 3)
                                return new LSL_List();

                            int active;
                            try
                            {
                                active = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_SIT_TARGET: arg #{1} - parameter 1 must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            LSL_Vector offset;
                            try
                            {
                                offset = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_SIT_TARGET: arg #{1} - parameter 2 must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            LSL_Rotation sitrot;
                            try
                            {
                                sitrot = rules.GetQuaternionItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_SIT_TARGET: arg #{1} - parameter 3 must be rotation",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            // not SL compatible since we don't have a independent flag to control active target but use the values of offset and rotation
                            if (active == 1)
                            {
                                if (offset.x == 0 && offset.y == 0 && offset.z == 0 && sitrot.s == 1.0)
                                    offset.z = 1e-5f; // hack
                                SitTarget(part, offset, sitrot);
                            }
                            else if (active == 0)
                            {
                                SitTarget(part, Vector3.Zero, Quaternion.Identity);
                            }

                            break;

                        case ScriptBaseClass.PRIM_ALPHA_MODE:
                            if (remain < 3)
                                return new LSL_List();

                            try
                            {
                                face = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_ALPHA_MODE: arg #{1} - must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            int materialAlphaMode;
                            try
                            {
                                materialAlphaMode = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_ALPHA_MODE: arg #{1} - must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            if (materialAlphaMode < 0 || materialAlphaMode > 3)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_ALPHA_MODE: arg #{1} - must be 0 to 3",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            int materialMaskCutoff;
                            try
                            {
                                materialMaskCutoff = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_ALPHA_MODE: arg #{1} - must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            if (materialMaskCutoff < 0 || materialMaskCutoff > 255)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_ALPHA_MODE: arg #{1} - must be 0 to 255",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            materialChanged |= SetMaterialAlphaMode(part, face, materialAlphaMode, materialMaskCutoff);
                            break;

                        case ScriptBaseClass.PRIM_NORMAL:
                            if (remain < 5)
                                return new LSL_List();

                            try
                            {
                                face = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format("Error running rule #{0} -> PRIM_NORMAL: arg #{1} - must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            var mapname = rules.Data[idx++].ToString();
                            var mapID = UUID.Zero;
                            if (!string.IsNullOrEmpty(mapname))
                            {
                                mapID = ScriptUtils.GetAssetIdFromItemName(m_host, mapname, (int)AssetType.Texture);
                                if (mapID.IsZero())
                                    if (!UUID.TryParse(mapname, out mapID))
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_NORMAL: arg #{1} - must be a UUID or a texture name on object inventory",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }
                            }

                            LSL_Vector mnrepeat;
                            try
                            {
                                mnrepeat = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format("Error running rule #{0} -> PRIM_NORMAL: arg #{1} - must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            LSL_Vector mnoffset;
                            try
                            {
                                mnoffset = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format("Error running rule #{0} -> PRIM_NORMAL: arg #{1} - must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            LSL_Float mnrot;
                            try
                            {
                                mnrot = rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format("Error running rule #{0} -> PRIM_NORMAL: arg #{1} - must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            var repeatX = (float)Util.Clamp(mnrepeat.x, -100.0, 100.0);
                            var repeatY = (float)Util.Clamp(mnrepeat.y, -100.0, 100.0);
                            var offsetX = (float)Util.Clamp(mnoffset.x, 0, 1.0);
                            var offsetY = (float)Util.Clamp(mnoffset.y, 0, 1.0);

                            materialChanged |= SetMaterialNormalMap(part, face, mapID, repeatX, repeatY, offsetX,
                                offsetY, (float)mnrot);
                            break;

                        case ScriptBaseClass.PRIM_SPECULAR:
                            if (remain < 8)
                                return new LSL_List();

                            try
                            {
                                face = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_SPECULAR: arg #{1} - must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            var smapname = rules.Data[idx++].ToString();
                            var smapID = UUID.Zero;
                            if (!string.IsNullOrEmpty(smapname))
                            {
                                smapID = ScriptUtils.GetAssetIdFromItemName(m_host, smapname, (int)AssetType.Texture);
                                if (smapID.IsZero())
                                    if (!UUID.TryParse(smapname, out smapID))
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_SPECULAR: arg #{1} - must be a UUID or a texture name on object inventory",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }
                            }

                            LSL_Vector msrepeat;
                            try
                            {
                                msrepeat = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format("Error running rule #{0} -> PRIM_SPECULAR: arg #{1} - must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            LSL_Vector msoffset;
                            try
                            {
                                msoffset = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format("Error running rule #{0} -> PRIM_SPECULAR: arg #{1} - must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            LSL_Float msrot;
                            try
                            {
                                msrot = rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format("Error running rule #{0} -> PRIM_SPECULAR: arg #{1} - must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            LSL_Vector mscolor;
                            try
                            {
                                mscolor = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format("Error running rule #{0} -> PRIM_SPECULAR: arg #{1} - must be vector",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            LSL_Integer msgloss;
                            try
                            {
                                msgloss = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_SPECULAR: arg #{1} - must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            LSL_Integer msenv;
                            try
                            {
                                msenv = rules.GetLSLIntegerItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_SPECULAR: arg #{1} - must be integer",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            var srepeatX = (float)Util.Clamp(msrepeat.x, -100.0, 100.0);
                            var srepeatY = (float)Util.Clamp(msrepeat.y, -100.0, 100.0);
                            var soffsetX = (float)Util.Clamp(msoffset.x, -1.0, 1.0);
                            var soffsetY = (float)Util.Clamp(msoffset.y, -1.0, 1.0);
                            var colorR = (byte)(255.0 * Util.Clamp(mscolor.x, 0, 1.0) + 0.5);
                            var colorG = (byte)(255.0 * Util.Clamp(mscolor.y, 0, 1.0) + 0.5);
                            var colorB = (byte)(255.0 * Util.Clamp(mscolor.z, 0, 1.0) + 0.5);
                            var gloss = (byte)Util.Clamp((int)msgloss, 0, 255);
                            var env = (byte)Util.Clamp((int)msenv, 0, 255);

                            materialChanged |= SetMaterialSpecMap(part, face, smapID, srepeatX, srepeatY, soffsetX,
                                soffsetY,
                                (float)msrot, colorR, colorG, colorB, gloss, env);

                            break;

                        case ScriptBaseClass.PRIM_LINK_TARGET:
                            if (remain <
                                3) // setting to 3 on the basis that parsing any usage of PRIM_LINK_TARGET that has nothing following it is pointless.
                                return new LSL_List();

                            return rules.GetSublist(idx, -1);

                        case ScriptBaseClass.PRIM_PROJECTOR:
                            if (remain < 4)
                                return new LSL_List();

                            var stexname = rules.Data[idx++].ToString();
                            var stexID = UUID.Zero;
                            if (!string.IsNullOrEmpty(stexname))
                            {
                                stexID = ScriptUtils.GetAssetIdFromItemName(m_host, stexname, (int)AssetType.Texture);
                                if (stexID.IsZero())
                                    if (!UUID.TryParse(stexname, out stexID))
                                    {
                                        Error(originFunc,
                                            string.Format(
                                                "Error running rule #{0} -> PRIM_PROJECTOR: arg #{1} - must be a UUID or a texture name on object inventory",
                                                rulesParsed, idx - idxStart - 1));
                                        return new LSL_List();
                                    }
                            }

                            LSL_Float fov;
                            try
                            {
                                fov = rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format("Error running rule #{0} -> PRIM_PROJECTOR: arg #{1} - must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            LSL_Float focus;
                            try
                            {
                                focus = rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format("Error running rule #{0} -> PRIM_PROJECTOR: arg #{1} - must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            LSL_Float amb;
                            try
                            {
                                amb = rules.GetLSLFloatItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format("Error running rule #{0} -> PRIM_PROJECTOR: arg #{1} - must be float",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            if (stexID.IsNotZero())
                            {
                                part.Shape.ProjectionEntry = true;
                                part.Shape.ProjectionTextureUUID = stexID;
                                part.Shape.ProjectionFOV = Util.Clamp((float)fov, 0, 3.0f);
                                part.Shape.ProjectionFocus = Util.Clamp((float)focus, 0, 20.0f);
                                part.Shape.ProjectionAmbiance = Util.Clamp((float)amb, 0, 1.0f);

                                part.ParentGroup.HasGroupChanged = true;
                                part.ScheduleFullUpdate();
                            }
                            else if (part.Shape.ProjectionEntry)
                            {
                                part.Shape.ProjectionEntry = false;

                                part.ParentGroup.HasGroupChanged = true;
                                part.ScheduleFullUpdate();
                            }

                            break;

                        default:
                            Error(originFunc,
                                string.Format("Error running rule #{0}: arg #{1} - unsupported parameter", rulesParsed,
                                    idx - idxStart));
                            return new LSL_List();
                    }
                }
            }
            catch (InvalidCastException e)
            {
                Error(originFunc,
                    string.Format("Error running rule #{0}: arg #{1} - ", rulesParsed, idx - idxStart) + e.Message);
            }
            finally
            {
                if (positionChanged)
                {
                    if (part.ParentGroup.RootPart == part)
                    {
                        var parent = part.ParentGroup;
//                        Util.FireAndForget(delegate(object x) {
                        parent.UpdateGroupPosition(currentPosition);
//                        });
                    }
                    else
                    {
                        part.OffsetPosition = currentPosition;
//                        SceneObjectGroup parent = part.ParentGroup;
//                        parent.HasGroupChanged = true;
//                        parent.ScheduleGroupForTerseUpdate();
                        part.ScheduleTerseUpdate();
                    }
                }

                if (materialChanged)
                    if (part.ParentGroup != null && !part.ParentGroup.IsDeleted)
                    {
                        part.TriggerScriptChangedEvent(Changed.TEXTURE);
                        part.ScheduleFullUpdate();
                        part.ParentGroup.HasGroupChanged = true;
                    }
            }

            return new LSL_List();
        }

        protected bool SetMaterialAlphaMode(SceneObjectPart part, int face, int materialAlphaMode,
            int materialMaskCutoff)
        {
            if (m_materialsModule == null)
                return false;

            var nsides = part.GetNumberOfSides();

            if (face == ScriptBaseClass.ALL_SIDES)
            {
                var changed = false;
                for (var i = 0; i < nsides; i++)
                    changed |= SetFaceMaterialAlphaMode(part, i, materialAlphaMode, materialMaskCutoff);
                return changed;
            }

            if (face >= 0 && face < nsides)
                return SetFaceMaterialAlphaMode(part, face, materialAlphaMode, materialMaskCutoff);

            return false;
        }

        protected bool SetFaceMaterialAlphaMode(SceneObjectPart part, int face, int materialAlphaMode,
            int materialMaskCutoff)
        {
            var tex = part.Shape.Textures;
            var texface = tex.CreateFace((uint)face);
            if (texface == null)
                return false;

            FaceMaterial mat = null;
            var oldid = texface.MaterialID;
            if (oldid.IsZero())
            {
                if (materialAlphaMode == 1)
                    return false;
            }
            else
            {
                mat = m_materialsModule.GetMaterialCopy(oldid);
            }

            if (mat == null)
                mat = new FaceMaterial();

            mat.DiffuseAlphaMode = (byte)materialAlphaMode;
            mat.AlphaMaskCutoff = (byte)materialMaskCutoff;

            UUID id = m_materialsModule
                .AddNewMaterial(mat); // id is a hash of entire material hash, so this means no change
            if (oldid.Equals(id))
                return false;

            texface.MaterialID = id;
            part.Shape.TextureEntry = tex.GetBytes(9);
            m_materialsModule.RemoveMaterial(oldid);
            return true;
        }

        protected bool SetMaterialNormalMap(SceneObjectPart part, int face, UUID mapID, float repeatX, float repeatY,
            float offsetX, float offsetY, float rot)
        {
            if (m_materialsModule == null)
                return false;

            var nsides = part.GetNumberOfSides();

            if (face == ScriptBaseClass.ALL_SIDES)
            {
                var changed = false;
                for (var i = 0; i < nsides; i++)
                    changed |= SetFaceMaterialNormalMap(part, i, mapID, repeatX, repeatY, offsetX, offsetY, rot);
                return changed;
            }

            if (face >= 0 && face < nsides)
                return SetFaceMaterialNormalMap(part, face, mapID, repeatX, repeatY, offsetX, offsetY, rot);

            return false;
        }

        protected bool SetFaceMaterialNormalMap(SceneObjectPart part, int face, UUID mapID, float repeatX,
            float repeatY,
            float offsetX, float offsetY, float rot)

        {
            var tex = part.Shape.Textures;
            var texface = tex.CreateFace((uint)face);
            if (texface == null)
                return false;

            FaceMaterial mat = null;
            var oldid = texface.MaterialID;

            if (oldid.IsZero())
            {
                if (mapID.IsZero())
                    return false;
            }
            else
            {
                mat = m_materialsModule.GetMaterialCopy(oldid);
            }

            if (mat == null)
                mat = new FaceMaterial();

            mat.NormalMapID = mapID;
            mat.NormalOffsetX = offsetX;
            mat.NormalOffsetY = offsetY;
            mat.NormalRepeatX = repeatX;
            mat.NormalRepeatY = repeatY;
            mat.NormalRotation = rot;

            mapID = m_materialsModule.AddNewMaterial(mat);

            if (oldid.Equals(mapID))
                return false;

            texface.MaterialID = mapID;
            part.Shape.TextureEntry = tex.GetBytes(9);
            m_materialsModule.RemoveMaterial(oldid);
            return true;
        }

        protected bool SetMaterialSpecMap(SceneObjectPart part, int face, UUID mapID, float repeatX, float repeatY,
            float offsetX, float offsetY, float rot,
            byte colorR, byte colorG, byte colorB,
            byte gloss, byte env)
        {
            if (m_materialsModule == null)
                return false;

            var nsides = part.GetNumberOfSides();

            if (face == ScriptBaseClass.ALL_SIDES)
            {
                var changed = false;
                for (var i = 0; i < nsides; i++)
                    changed |= SetFaceMaterialSpecMap(part, i, mapID, repeatX, repeatY, offsetX, offsetY, rot,
                        colorR, colorG, colorB, gloss, env);
                return changed;
            }

            if (face >= 0 && face < nsides)
                return SetFaceMaterialSpecMap(part, face, mapID, repeatX, repeatY, offsetX, offsetY, rot,
                    colorR, colorG, colorB, gloss, env);

            return false;
        }

        protected bool SetFaceMaterialSpecMap(SceneObjectPart part, int face, UUID mapID, float repeatX, float repeatY,
            float offsetX, float offsetY, float rot,
            byte colorR, byte colorG, byte colorB,
            byte gloss, byte env)
        {
            var tex = part.Shape.Textures;
            var texface = tex.CreateFace((uint)face);
            if (texface == null)
                return false;

            FaceMaterial mat = null;
            var oldid = texface.MaterialID;

            if (oldid.IsZero())
            {
                if (mapID.IsZero())
                    return false;
            }
            else
            {
                mat = m_materialsModule.GetMaterialCopy(oldid);
            }

            if (mat == null)
                mat = new FaceMaterial();

            mat.SpecularMapID = mapID;
            mat.SpecularOffsetX = offsetX;
            mat.SpecularOffsetY = offsetY;
            mat.SpecularRepeatX = repeatX;
            mat.SpecularRepeatY = repeatY;
            mat.SpecularRotation = rot;
            mat.SpecularLightColorR = colorR;
            mat.SpecularLightColorG = colorG;
            mat.SpecularLightColorB = colorB;
            mat.SpecularLightExponent = gloss;
            mat.EnvironmentIntensity = env;

            mapID = m_materialsModule.AddNewMaterial(mat);

            if (oldid.Equals(mapID))
                return false;

            texface.MaterialID = mapID;
            part.Shape.TextureEntry = tex.GetBytes(9);
            m_materialsModule.RemoveMaterial(oldid);
            return true;
        }

        protected LSL_List SetAgentParams(ScenePresence sp, LSL_List rules, string originFunc, ref uint rulesParsed)
        {
            var idx = 0;
            var idxStart = 0;

            try
            {
                while (idx < rules.Length)
                {
                    ++rulesParsed;
                    int code = rules.GetLSLIntegerItem(idx++);

                    var remain = rules.Length - idx;
                    idxStart = idx;

                    switch (code)
                    {
                        case ScriptBaseClass.PRIM_POSITION:
                        case ScriptBaseClass.PRIM_POS_LOCAL:
                            if (remain < 1)
                                return new LSL_List();

                            try
                            {
                                sp.OffsetPosition = rules.GetVector3Item(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                if (code == ScriptBaseClass.PRIM_POSITION)
                                    Error(originFunc,
                                        string.Format(
                                            "Error running rule #{0} -> PRIM_POSITION: arg #{1} - parameter 2 must be vector",
                                            rulesParsed, idx - idxStart - 1));
                                else
                                    Error(originFunc,
                                        string.Format(
                                            "Error running rule #{0} -> PRIM_POS_LOCAL: arg #{1} - parameter 2 must be vector",
                                            rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            break;

                        case ScriptBaseClass.PRIM_ROTATION:
                            if (remain < 1)
                                return new LSL_List();

                            Quaternion inRot;

                            try
                            {
                                inRot = rules.GetQuaternionItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_ROTATION: arg #{1} - parameter 2 must be rotation",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            var parentPart = sp.ParentPart;

                            if (parentPart != null)
                                sp.Rotation = m_host.GetWorldRotation() * inRot;

                            break;

                        case ScriptBaseClass.PRIM_ROT_LOCAL:
                            if (remain < 1)
                                return new LSL_List();

                            try
                            {
                                sp.Rotation = rules.GetQuaternionItem(idx++);
                            }
                            catch (InvalidCastException)
                            {
                                Error(originFunc,
                                    string.Format(
                                        "Error running rule #{0} -> PRIM_ROT_LOCAL: arg #{1} - parameter 2 must be rotation",
                                        rulesParsed, idx - idxStart - 1));
                                return new LSL_List();
                            }

                            break;

                        case ScriptBaseClass.PRIM_TYPE:
                            Error(originFunc, "PRIM_TYPE disallowed on agent");
                            return new LSL_List();

                        case ScriptBaseClass.PRIM_OMEGA:
                            Error(originFunc, "PRIM_OMEGA disallowed on agent");
                            return new LSL_List();

                        case ScriptBaseClass.PRIM_LINK_TARGET:
                            if (remain <
                                3) // setting to 3 on the basis that parsing any usage of PRIM_LINK_TARGET that has nothing following it is pointless.
                                return new LSL_List();

                            return rules.GetSublist(idx, -1);

                        default:
                            Error(originFunc,
                                string.Format("Error running rule #{0} on agent: arg #{1} - disallowed on agent",
                                    rulesParsed, idx - idxStart));
                            return new LSL_List();
                    }
                }
            }
            catch (InvalidCastException e)
            {
                Error(
                    originFunc,
                    string.Format("Error running rule #{0}: arg #{1} - ", rulesParsed, idx - idxStart) + e.Message);
            }

            return new LSL_List();
        }

        /// <summary>
        ///     Helper to calculate bounding box of an avatar.
        /// </summary>
        private void BoundingBoxOfScenePresence(ScenePresence sp, out Vector3 lower, out Vector3 upper)
        {
            // Adjust from OS model
            // avatar height = visual height - 0.2, bounding box height = visual height
            // to SL model
            // avatar height = visual height, bounding box height = visual height + 0.2
            var height = sp.Appearance.AvatarHeight + m_avatarHeightCorrection;

            // According to avatar bounding box in SL 2015-04-18:
            // standing = <-0.275,-0.35,-0.1-0.5*h> : <0.275,0.35,0.1+0.5*h>
            // groundsitting = <-0.3875,-0.5,-0.05-0.375*h> : <0.3875,0.5,0.5>
            // sitting = <-0.5875,-0.35,-0.35-0.375*h> : <0.1875,0.35,-0.25+0.25*h>

            // When avatar is sitting
            if (sp.ParentPart != null)
            {
                lower = new Vector3(m_lABB1SitX0, m_lABB1SitY0, m_lABB1SitZ0 + m_lABB1SitZ1 * height);
                upper = new Vector3(m_lABB2SitX0, m_lABB2SitY0, m_lABB2SitZ0 + m_lABB2SitZ1 * height);
            }
            // When avatar is groundsitting
            else if (sp.Animator.Animations.ImplicitDefaultAnimation.AnimID.Equals(
                         DefaultAvatarAnimations.AnimsUUIDbyName["SIT_GROUND_CONSTRAINED"]))
            {
                lower = new Vector3(m_lABB1GrsX0, m_lABB1GrsY0, m_lABB1GrsZ0 + m_lABB1GrsZ1 * height);
                upper = new Vector3(m_lABB2GrsX0, m_lABB2GrsY0, m_lABB2GrsZ0 + m_lABB2GrsZ1 * height);
            }
            // When avatar is standing or flying
            else
            {
                lower = new Vector3(m_lABB1StdX0, m_lABB1StdY0, m_lABB1StdZ0 + m_lABB1StdZ1 * height);
                upper = new Vector3(m_lABB2StdX0, m_lABB2StdY0, m_lABB2StdZ0 + m_lABB2StdZ1 * height);
            }
        }

        public LSL_List GetPrimParams(SceneObjectPart part, LSL_List rules, ref LSL_List res)
        {
            var idx = 0;
            int face;
            Primitive.TextureEntry tex;
            var nsides = GetNumberOfSides(part);

            while (idx < rules.Length)
            {
                int code = rules.GetLSLIntegerItem(idx++);
                var remain = rules.Length - idx;

                switch (code)
                {
                    case ScriptBaseClass.PRIM_MATERIAL:
                        res.Add(new LSL_Integer(part.Material));
                        break;

                    case ScriptBaseClass.PRIM_PHYSICS:
                        if ((part.GetEffectiveObjectFlags() & (uint)PrimFlags.Physics) != 0)
                            res.Add(new LSL_Integer(1));
                        else
                            res.Add(new LSL_Integer(0));
                        break;

                    case ScriptBaseClass.PRIM_TEMP_ON_REZ:
                        if ((part.GetEffectiveObjectFlags() & (uint)PrimFlags.TemporaryOnRez) != 0)
                            res.Add(new LSL_Integer(1));
                        else
                            res.Add(new LSL_Integer(0));
                        break;

                    case ScriptBaseClass.PRIM_PHANTOM:
                        if ((part.GetEffectiveObjectFlags() & (uint)PrimFlags.Phantom) != 0)
                            res.Add(new LSL_Integer(1));
                        else
                            res.Add(new LSL_Integer(0));
                        break;

                    case ScriptBaseClass.PRIM_POSITION:
                        var v = new LSL_Vector(part.AbsolutePosition.X,
                            part.AbsolutePosition.Y,
                            part.AbsolutePosition.Z);
                        res.Add(v);
                        break;

                    case ScriptBaseClass.PRIM_SIZE:
                        res.Add(new LSL_Vector(part.Scale));
                        break;

                    case ScriptBaseClass.PRIM_ROTATION:
                        res.Add(GetPartRot(part));
                        break;

                    case ScriptBaseClass.PRIM_PHYSICS_SHAPE_TYPE:
                        res.Add(new LSL_Integer(part.PhysicsShapeType));
                        break;

                    case ScriptBaseClass.PRIM_TYPE:
                        // implementing box
                        var Shape = part.Shape;
                        var primType = (int)part.GetPrimType();
                        res.Add(new LSL_Integer(primType));
                        var topshearx = (sbyte)Shape.PathShearX / 100.0; // Fix negative values for PathShearX
                        var topsheary = (sbyte)Shape.PathShearY / 100.0; // and PathShearY.
                        switch (primType)
                        {
                            case ScriptBaseClass.PRIM_TYPE_BOX:
                            case ScriptBaseClass.PRIM_TYPE_CYLINDER:
                            case ScriptBaseClass.PRIM_TYPE_PRISM:
                                res.Add(new LSL_Integer(Shape.ProfileCurve) & 0xf0); // Isolate hole shape nibble.
                                res.Add(new LSL_Vector(Shape.ProfileBegin / 50000.0, 1 - Shape.ProfileEnd / 50000.0,
                                    0));
                                res.Add(new LSL_Float(Shape.ProfileHollow / 50000.0));
                                res.Add(new LSL_Vector(Shape.PathTwistBegin / 100.0, Shape.PathTwist / 100.0, 0));
                                res.Add(new LSL_Vector(1 - (Shape.PathScaleX / 100.0 - 1),
                                    1 - (Shape.PathScaleY / 100.0 - 1), 0));
                                res.Add(new LSL_Vector(topshearx, topsheary, 0));
                                break;

                            case ScriptBaseClass.PRIM_TYPE_SPHERE:
                                res.Add(new LSL_Integer(Shape.ProfileCurve) & 0xf0); // Isolate hole shape nibble.
                                res.Add(new LSL_Vector(Shape.PathBegin / 50000.0, 1 - Shape.PathEnd / 50000.0, 0));
                                res.Add(new LSL_Float(Shape.ProfileHollow / 50000.0));
                                res.Add(new LSL_Vector(Shape.PathTwistBegin / 100.0, Shape.PathTwist / 100.0, 0));
                                res.Add(new LSL_Vector(Shape.ProfileBegin / 50000.0, 1 - Shape.ProfileEnd / 50000.0,
                                    0));
                                break;

                            case ScriptBaseClass.PRIM_TYPE_SCULPT:
                                res.Add(new LSL_String(Shape.SculptTexture.ToString()));
                                res.Add(new LSL_Integer(Shape.SculptType));
                                break;

                            case ScriptBaseClass.PRIM_TYPE_RING:
                            case ScriptBaseClass.PRIM_TYPE_TUBE:
                            case ScriptBaseClass.PRIM_TYPE_TORUS:
                                // holeshape
                                res.Add(new LSL_Integer(Shape.ProfileCurve) & 0xf0); // Isolate hole shape nibble.

                                // cut
                                res.Add(new LSL_Vector(Shape.PathBegin / 50000.0, 1 - Shape.PathEnd / 50000.0, 0));

                                // hollow
                                res.Add(new LSL_Float(Shape.ProfileHollow / 50000.0));

                                // twist
                                res.Add(new LSL_Vector(Shape.PathTwistBegin / 100.0, Shape.PathTwist / 100.0, 0));

                                // vector holesize
                                res.Add(new LSL_Vector(1 - (Shape.PathScaleX / 100.0 - 1),
                                    1 - (Shape.PathScaleY / 100.0 - 1), 0));

                                // vector topshear
                                res.Add(new LSL_Vector(topshearx, topsheary, 0));

                                // vector profilecut
                                res.Add(new LSL_Vector(Shape.ProfileBegin / 50000.0, 1 - Shape.ProfileEnd / 50000.0,
                                    0));

                                // vector tapera
                                res.Add(new LSL_Vector(Shape.PathTaperX / 100.0, Shape.PathTaperY / 100.0, 0));

                                // float revolutions
                                res.Add(new LSL_Float(Math.Round(Shape.PathRevolutions * 0.015d, 2,
                                    MidpointRounding.AwayFromZero)) + 1.0d);
                                // Slightly inaccurate, because an unsigned byte is being used to represent
                                // the entire range of floating-point values from 1.0 through 4.0 (which is how
                                // SL does it).
                                //
                                // Using these formulas to store and retrieve PathRevolutions, it is not
                                // possible to use all values between 1.00 and 4.00. For instance, you can't
                                // represent 1.10. You can represent 1.09 and 1.11, but not 1.10. So, if you
                                // use llSetPrimitiveParams to set revolutions to 1.10 and then retreive them
                                // with llGetPrimitiveParams, you'll retrieve 1.09. You can also see a similar
                                // behavior in the viewer as you cannot set 1.10. The viewer jumps to 1.11.
                                // In SL, llSetPrimitveParams and llGetPrimitiveParams can set and get a value
                                // such as 1.10. So, SL must store and retreive the actual user input rather
                                // than only storing the encoded value.

                                // float radiusoffset
                                res.Add(new LSL_Float(Shape.PathRadiusOffset / 100.0));

                                // float skew
                                res.Add(new LSL_Float(Shape.PathSkew / 100.0));
                                break;
                        }

                        break;

                    case ScriptBaseClass.PRIM_TEXTURE:
                        if (remain < 1)
                            return new LSL_List();

                        face = rules.GetLSLIntegerItem(idx++);
                        tex = part.Shape.Textures;

                        if (face == ScriptBaseClass.ALL_SIDES)
                        {
                            for (face = 0; face < nsides; face++)
                            {
                                var texface = tex.GetFace((uint)face);

                                res.Add(new LSL_String(texface.TextureID.ToString()));
                                res.Add(new LSL_Vector(texface.RepeatU,
                                    texface.RepeatV,
                                    0));
                                res.Add(new LSL_Vector(texface.OffsetU,
                                    texface.OffsetV,
                                    0));
                                res.Add(new LSL_Float(texface.Rotation));
                            }
                        }
                        else
                        {
                            if (face >= 0 && face < nsides)
                            {
                                var texface = tex.GetFace((uint)face);

                                res.Add(new LSL_String(texface.TextureID.ToString()));
                                res.Add(new LSL_Vector(texface.RepeatU,
                                    texface.RepeatV,
                                    0));
                                res.Add(new LSL_Vector(texface.OffsetU,
                                    texface.OffsetV,
                                    0));
                                res.Add(new LSL_Float(texface.Rotation));
                            }
                        }

                        break;

                    case ScriptBaseClass.PRIM_COLOR:
                        if (remain < 1)
                            return new LSL_List();

                        face = rules.GetLSLIntegerItem(idx++);
                        tex = part.Shape.Textures;
                        Color4 texcolor;

                        if (face == ScriptBaseClass.ALL_SIDES)
                        {
                            for (face = 0; face < nsides; face++)
                            {
                                texcolor = tex.GetFace((uint)face).RGBA;
                                res.Add(new LSL_Vector(texcolor.R,
                                    texcolor.G,
                                    texcolor.B));
                                res.Add(new LSL_Float(texcolor.A));
                            }
                        }
                        else
                        {
                            texcolor = tex.GetFace((uint)face).RGBA;
                            res.Add(new LSL_Vector(texcolor.R,
                                texcolor.G,
                                texcolor.B));
                            res.Add(new LSL_Float(texcolor.A));
                        }

                        break;

                    case ScriptBaseClass.PRIM_BUMP_SHINY:
                    {
                        if (remain < 1)
                            return new LSL_List();

                        face = rules.GetLSLIntegerItem(idx++);
                        tex = part.Shape.Textures;
                        int shiny;
                        if (face == ScriptBaseClass.ALL_SIDES)
                        {
                            for (face = 0; face < nsides; face++)
                            {
                                var shinyness = tex.GetFace((uint)face).Shiny;
                                if (shinyness == Shininess.High)
                                    shiny = ScriptBaseClass.PRIM_SHINY_HIGH;
                                else if (shinyness == Shininess.Medium)
                                    shiny = ScriptBaseClass.PRIM_SHINY_MEDIUM;
                                else if (shinyness == Shininess.Low)
                                    shiny = ScriptBaseClass.PRIM_SHINY_LOW;
                                else
                                    shiny = ScriptBaseClass.PRIM_SHINY_NONE;
                                res.Add(new LSL_Integer(shiny));
                                res.Add(new LSL_Integer((int)tex.GetFace((uint)face).Bump));
                            }
                        }
                        else
                        {
                            var shinyness = tex.GetFace((uint)face).Shiny;
                            if (shinyness == Shininess.High)
                                shiny = ScriptBaseClass.PRIM_SHINY_HIGH;
                            else if (shinyness == Shininess.Medium)
                                shiny = ScriptBaseClass.PRIM_SHINY_MEDIUM;
                            else if (shinyness == Shininess.Low)
                                shiny = ScriptBaseClass.PRIM_SHINY_LOW;
                            else
                                shiny = ScriptBaseClass.PRIM_SHINY_NONE;
                            res.Add(new LSL_Integer(shiny));
                            res.Add(new LSL_Integer((int)tex.GetFace((uint)face).Bump));
                        }

                        break;
                    }
                    case ScriptBaseClass.PRIM_FULLBRIGHT:
                    {
                        if (remain < 1)
                            return new LSL_List();

                        face = rules.GetLSLIntegerItem(idx++);

                        tex = part.Shape.Textures;
                        int fullbright;
                        if (face == ScriptBaseClass.ALL_SIDES)
                        {
                            for (face = 0; face < nsides; face++)
                            {
                                if (tex.GetFace((uint)face).Fullbright)
                                    fullbright = ScriptBaseClass.TRUE;
                                else
                                    fullbright = ScriptBaseClass.FALSE;
                                res.Add(new LSL_Integer(fullbright));
                            }
                        }
                        else
                        {
                            if (tex.GetFace((uint)face).Fullbright)
                                fullbright = ScriptBaseClass.TRUE;
                            else
                                fullbright = ScriptBaseClass.FALSE;
                            res.Add(new LSL_Integer(fullbright));
                        }

                        break;
                    }
                    case ScriptBaseClass.PRIM_FLEXIBLE:
                        var shape = part.Shape;

                        // at sl this does not return true state, but if data was set
                        if (shape.FlexiEntry)
                            // correct check should had been:
                            //if (shape.PathCurve == (byte)Extrusion.Flexible)
                            res.Add(new LSL_Integer(1)); // active
                        else
                            res.Add(new LSL_Integer(0));
                        res.Add(new LSL_Integer(shape.FlexiSoftness)); // softness
                        res.Add(new LSL_Float(shape.FlexiGravity)); // gravity
                        res.Add(new LSL_Float(shape.FlexiDrag)); // friction
                        res.Add(new LSL_Float(shape.FlexiWind)); // wind
                        res.Add(new LSL_Float(shape.FlexiTension)); // tension
                        res.Add(new LSL_Vector(shape.FlexiForceX, // force
                            shape.FlexiForceY,
                            shape.FlexiForceZ));
                        break;

                    case ScriptBaseClass.PRIM_TEXGEN:
                        // (PRIM_TEXGEN_DEFAULT, PRIM_TEXGEN_PLANAR)
                        if (remain < 1)
                            return new LSL_List();

                        face = rules.GetLSLIntegerItem(idx++);

                        tex = part.Shape.Textures;
                        if (face == ScriptBaseClass.ALL_SIDES)
                        {
                            for (face = 0; face < nsides; face++)
                                if (tex.GetFace((uint)face).TexMapType == MappingType.Planar)
                                    res.Add(new LSL_Integer(ScriptBaseClass.PRIM_TEXGEN_PLANAR));
                                else
                                    res.Add(new LSL_Integer(ScriptBaseClass.PRIM_TEXGEN_DEFAULT));
                        }
                        else
                        {
                            if (tex.GetFace((uint)face).TexMapType == MappingType.Planar)
                                res.Add(new LSL_Integer(ScriptBaseClass.PRIM_TEXGEN_PLANAR));
                            else
                                res.Add(new LSL_Integer(ScriptBaseClass.PRIM_TEXGEN_DEFAULT));
                        }

                        break;

                    case ScriptBaseClass.PRIM_POINT_LIGHT:
                        shape = part.Shape;

                        if (shape.LightEntry)
                            res.Add(new LSL_Integer(1)); // active
                        else
                            res.Add(new LSL_Integer(0));
                        res.Add(new LSL_Vector(shape.LightColorR, // color
                            shape.LightColorG,
                            shape.LightColorB));
                        res.Add(new LSL_Float(shape.LightIntensity)); // intensity
                        res.Add(new LSL_Float(shape.LightRadius)); // radius
                        res.Add(new LSL_Float(shape.LightFalloff)); // falloff
                        break;

                    case ScriptBaseClass.PRIM_GLOW:
                        if (remain < 1)
                            return new LSL_List();

                        face = rules.GetLSLIntegerItem(idx++);

                        tex = part.Shape.Textures;
                        float primglow;
                        if (face == ScriptBaseClass.ALL_SIDES)
                        {
                            for (face = 0; face < nsides; face++)
                            {
                                primglow = tex.GetFace((uint)face).Glow;
                                res.Add(new LSL_Float(primglow));
                            }
                        }
                        else
                        {
                            primglow = tex.GetFace((uint)face).Glow;
                            res.Add(new LSL_Float(primglow));
                        }

                        break;

                    case ScriptBaseClass.PRIM_TEXT:
                        var textColor = part.GetTextColor();
                        res.Add(new LSL_String(part.Text));
                        res.Add(new LSL_Vector(textColor.R,
                            textColor.G,
                            textColor.B));
                        res.Add(new LSL_Float(textColor.A));
                        break;

                    case ScriptBaseClass.PRIM_NAME:
                        res.Add(new LSL_String(part.Name));
                        break;

                    case ScriptBaseClass.PRIM_DESC:
                        res.Add(new LSL_String(part.Description));
                        break;
                    case ScriptBaseClass.PRIM_ROT_LOCAL:
                        res.Add(new LSL_Rotation(part.RotationOffset));
                        break;

                    case ScriptBaseClass.PRIM_POS_LOCAL:
                        res.Add(new LSL_Vector(GetPartLocalPos(part)));
                        break;
                    case ScriptBaseClass.PRIM_SLICE:
                        var prim_type = part.GetPrimType();
                        var useProfileBeginEnd = prim_type == PrimType.SPHERE || prim_type == PrimType.TORUS ||
                                                 prim_type == PrimType.TUBE || prim_type == PrimType.RING;
                        res.Add(new LSL_Vector(
                            (useProfileBeginEnd ? part.Shape.ProfileBegin : part.Shape.PathBegin) / 50000.0,
                            1 - (useProfileBeginEnd ? part.Shape.ProfileEnd : part.Shape.PathEnd) / 50000.0,
                            0
                        ));
                        break;

                    case ScriptBaseClass.PRIM_OMEGA:
                        // this may return values diferent from SL since we don't handle set the same way
                        var gain = 1.0f; // we don't use gain and don't store it
                        var axis = part.AngularVelocity;
                        var spin = axis.Length();
                        if (spin < 1.0e-6)
                        {
                            axis = Vector3.Zero;
                            gain = 0.0f;
                            spin = 0.0f;
                        }
                        else
                        {
                            axis = axis * (1.0f / spin);
                        }

                        res.Add(new LSL_Vector(axis.X,
                            axis.Y,
                            axis.Z));
                        res.Add(new LSL_Float(spin));
                        res.Add(new LSL_Float(gain));
                        break;

                    case ScriptBaseClass.PRIM_SIT_TARGET:
                        if (part.IsSitTargetSet)
                        {
                            res.Add(new LSL_Integer(1));
                            res.Add(new LSL_Vector(part.SitTargetPosition));
                            res.Add(new LSL_Rotation(part.SitTargetOrientation));
                        }
                        else
                        {
                            res.Add(new LSL_Integer(0));
                            res.Add(new LSL_Vector(Vector3.Zero));
                            res.Add(new LSL_Rotation(Quaternion.Identity));
                        }

                        break;

                    case ScriptBaseClass.PRIM_NORMAL:
                    case ScriptBaseClass.PRIM_SPECULAR:
                    case ScriptBaseClass.PRIM_ALPHA_MODE:
                        if (remain < 1)
                            return new LSL_List();

                        face = rules.GetLSLIntegerItem(idx++);
                        tex = part.Shape.Textures;
                        if (face == ScriptBaseClass.ALL_SIDES)
                        {
                            for (face = 0; face < nsides; face++)
                            {
                                var texface = tex.GetFace((uint)face);
                                getLSLFaceMaterial(ref res, code, part, texface);
                            }
                        }
                        else
                        {
                            if (face >= 0 && face < nsides)
                            {
                                var texface = tex.GetFace((uint)face);
                                getLSLFaceMaterial(ref res, code, part, texface);
                            }
                        }

                        break;

                    case ScriptBaseClass.PRIM_LINK_TARGET:

                        // TODO: Should be issuing a runtime script warning in this case.
                        if (remain < 2)
                            return new LSL_List();

                        return rules.GetSublist(idx, -1);

                    case ScriptBaseClass.PRIM_PROJECTOR:
                        if (part.Shape.ProjectionEntry)
                        {
                            res.Add(new LSL_String(part.Shape.ProjectionTextureUUID.ToString()));
                            res.Add(new LSL_Float(part.Shape.ProjectionFOV));
                            res.Add(new LSL_Float(part.Shape.ProjectionFocus));
                            res.Add(new LSL_Float(part.Shape.ProjectionAmbiance));
                        }
                        else
                        {
                            res.Add(new LSL_String(ScriptBaseClass.NULL_KEY));
                            res.Add(new LSL_Float(0));
                            res.Add(new LSL_Float(0));
                            res.Add(new LSL_Float(0));
                        }

                        break;
                }
            }

            return new LSL_List();
        }

        private string GetMaterialTextureUUIDbyRights(UUID origID, SceneObjectPart part)
        {
            if (World.Permissions.CanEditObject(m_host.ParentGroup.UUID, m_host.ParentGroup.RootPart.OwnerID))
                return origID.ToString();

            lock (part.TaskInventory)
            {
                foreach (var inv in part.TaskInventory)
                    if (inv.Value.InvType == (int)InventoryType.Texture && inv.Value.AssetID.Equals(origID))
                        return origID.ToString();
            }

            return ScriptBaseClass.NULL_KEY;
        }

        private void getLSLFaceMaterial(ref LSL_List res, int code, SceneObjectPart part,
            Primitive.TextureEntryFace texface)
        {
            var matID = UUID.Zero;
            if (m_materialsModule != null)
                matID = texface.MaterialID;

            if (!matID.IsZero())
            {
                FaceMaterial mat = m_materialsModule.GetMaterial(matID);
                if (mat != null)
                {
                    if (code == ScriptBaseClass.PRIM_NORMAL)
                    {
                        res.Add(new LSL_String(GetMaterialTextureUUIDbyRights(mat.NormalMapID, part)));
                        res.Add(new LSL_Vector(mat.NormalRepeatX, mat.NormalRepeatY, 0));
                        res.Add(new LSL_Vector(mat.NormalOffsetX, mat.NormalOffsetY, 0));
                        res.Add(new LSL_Float(mat.NormalRotation));
                    }
                    else if (code == ScriptBaseClass.PRIM_SPECULAR)
                    {
                        const float colorScale = 1.0f / 255f;
                        res.Add(new LSL_String(GetMaterialTextureUUIDbyRights(mat.SpecularMapID, part)));
                        res.Add(new LSL_Vector(mat.SpecularRepeatX, mat.SpecularRepeatY, 0));
                        res.Add(new LSL_Vector(mat.SpecularOffsetX, mat.SpecularOffsetY, 0));
                        res.Add(new LSL_Float(mat.SpecularRotation));
                        res.Add(new LSL_Vector(mat.SpecularLightColorR * colorScale,
                            mat.SpecularLightColorG * colorScale,
                            mat.SpecularLightColorB * colorScale));
                        res.Add(new LSL_Integer(mat.SpecularLightExponent));
                        res.Add(new LSL_Integer(mat.EnvironmentIntensity));
                    }
                    else if (code == ScriptBaseClass.PRIM_ALPHA_MODE)
                    {
                        res.Add(new LSL_Integer(mat.DiffuseAlphaMode));
                        res.Add(new LSL_Integer(mat.AlphaMaskCutoff));
                    }

                    return;
                }
            }

            // material not found
            if (code == ScriptBaseClass.PRIM_NORMAL || code == ScriptBaseClass.PRIM_SPECULAR)
            {
                res.Add(new LSL_String(ScriptBaseClass.NULL_KEY));
                res.Add(new LSL_Vector(1.0, 1.0, 0));
                res.Add(new LSL_Vector(0, 0, 0));
                res.Add(new LSL_Float(0));

                if (code == ScriptBaseClass.PRIM_SPECULAR)
                {
                    res.Add(new LSL_Vector(1.0, 1.0, 1.0));
                    res.Add(new LSL_Integer(51));
                    res.Add(new LSL_Integer(0));
                }
            }
            else if (code == ScriptBaseClass.PRIM_ALPHA_MODE)
            {
                res.Add(new LSL_Integer(1));
                res.Add(new LSL_Integer(0));
            }
        }

        private LSL_List GetPrimMediaParams(SceneObjectPart part, int face, LSL_List rules)
        {
            // LSL Spec http://wiki.secondlife.com/wiki/LlGetPrimMediaParams says to fail silently if face is invalid
            // TODO: Need to correctly handle case where a face has no media (which gives back an empty list).
            // Assuming silently fail means give back an empty list.  Ideally, need to check this.
            if (face < 0 || face > part.GetNumberOfSides() - 1)
                return new LSL_List();

            IMoapModule module = m_ScriptEngine.World.RequestModuleInterface<IMoapModule>();
            if (null == module)
                return new LSL_List();

            var me = module.GetMediaEntry(part, face);

            // As per http://wiki.secondlife.com/wiki/LlGetPrimMediaParams
            if (null == me)
                return new LSL_List();

            var res = new LSL_List();

            for (var i = 0; i < rules.Length; i++)
            {
                int code = rules.GetLSLIntegerItem(i);

                switch (code)
                {
                    case ScriptBaseClass.PRIM_MEDIA_ALT_IMAGE_ENABLE:
                        // Not implemented
                        res.Add(new LSL_Integer(0));
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_CONTROLS:
                        if (me.Controls == MediaControls.Standard)
                            res.Add(new LSL_Integer(ScriptBaseClass.PRIM_MEDIA_CONTROLS_STANDARD));
                        else
                            res.Add(new LSL_Integer(ScriptBaseClass.PRIM_MEDIA_CONTROLS_MINI));
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_CURRENT_URL:
                        res.Add(new LSL_String(me.CurrentURL));
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_HOME_URL:
                        res.Add(new LSL_String(me.HomeURL));
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_AUTO_LOOP:
                        res.Add(me.AutoLoop ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_AUTO_PLAY:
                        res.Add(me.AutoPlay ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_AUTO_SCALE:
                        res.Add(me.AutoScale ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_AUTO_ZOOM:
                        res.Add(me.AutoZoom ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_FIRST_CLICK_INTERACT:
                        res.Add(me.InteractOnFirstClick ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_WIDTH_PIXELS:
                        res.Add(new LSL_Integer(me.Width));
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_HEIGHT_PIXELS:
                        res.Add(new LSL_Integer(me.Height));
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_WHITELIST_ENABLE:
                        res.Add(me.EnableWhiteList ? ScriptBaseClass.TRUE : ScriptBaseClass.FALSE);
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_WHITELIST:
                        var urls = (string[])me.WhiteList.Clone();

                        for (var j = 0; j < urls.Length; j++)
                            urls[j] = Uri.EscapeDataString(urls[j]);

                        res.Add(new LSL_String(string.Join(", ", urls)));
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_PERMS_INTERACT:
                        res.Add(new LSL_Integer((int)me.InteractPermissions));
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_PERMS_CONTROL:
                        res.Add(new LSL_Integer((int)me.ControlPermissions));
                        break;

                    default: return ScriptBaseClass.LSL_STATUS_MALFORMED_PARAMS;
                }
            }

            return res;
        }

        private LSL_Integer SetPrimMediaParams(SceneObjectPart part, LSL_Integer face, LSL_List rules)
        {
            // LSL Spec http://wiki.secondlife.com/wiki/LlSetPrimMediaParams says to fail silently if face is invalid
            // Assuming silently fail means sending back LSL_STATUS_OK.  Ideally, need to check this.
            // Don't perform the media check directly
            if (face < 0 || face > part.GetNumberOfSides() - 1)
                return ScriptBaseClass.LSL_STATUS_NOT_FOUND;

            IMoapModule module = m_ScriptEngine.World.RequestModuleInterface<IMoapModule>();
            if (null == module)
                return ScriptBaseClass.LSL_STATUS_NOT_SUPPORTED;

            var me = module.GetMediaEntry(part, face);
            if (null == me)
                me = new MediaEntry();

            var i = 0;

            while (i < rules.Length - 1)
            {
                int code = rules.GetLSLIntegerItem(i++);

                switch (code)
                {
                    case ScriptBaseClass.PRIM_MEDIA_ALT_IMAGE_ENABLE:
                        me.EnableAlterntiveImage = rules.GetLSLIntegerItem(i++) != 0 ? true : false;
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_CONTROLS:
                        int v = rules.GetLSLIntegerItem(i++);
                        if (ScriptBaseClass.PRIM_MEDIA_CONTROLS_STANDARD == v)
                            me.Controls = MediaControls.Standard;
                        else
                            me.Controls = MediaControls.Mini;
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_CURRENT_URL:
                        me.CurrentURL = rules.GetStringItem(i++);
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_HOME_URL:
                        me.HomeURL = rules.GetStringItem(i++);
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_AUTO_LOOP:
                        me.AutoLoop = ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false;
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_AUTO_PLAY:
                        me.AutoPlay = ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false;
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_AUTO_SCALE:
                        me.AutoScale = ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false;
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_AUTO_ZOOM:
                        me.AutoZoom = ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false;
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_FIRST_CLICK_INTERACT:
                        me.InteractOnFirstClick = ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false;
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_WIDTH_PIXELS:
                        me.Width = rules.GetLSLIntegerItem(i++);
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_HEIGHT_PIXELS:
                        me.Height = rules.GetLSLIntegerItem(i++);
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_WHITELIST_ENABLE:
                        me.EnableWhiteList = ScriptBaseClass.TRUE == rules.GetLSLIntegerItem(i++) ? true : false;
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_WHITELIST:
                        var rawWhiteListUrls = rules.GetStringItem(i++).Split(',');
                        var whiteListUrls = new List<string>();
                        Array.ForEach(
                            rawWhiteListUrls, delegate(string rawUrl) { whiteListUrls.Add(rawUrl.Trim()); });
                        me.WhiteList = whiteListUrls.ToArray();
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_PERMS_INTERACT:
                        me.InteractPermissions = (MediaPermission)(byte)(int)rules.GetLSLIntegerItem(i++);
                        break;

                    case ScriptBaseClass.PRIM_MEDIA_PERMS_CONTROL:
                        me.ControlPermissions = (MediaPermission)(byte)(int)rules.GetLSLIntegerItem(i++);
                        break;

                    default: return ScriptBaseClass.LSL_STATUS_MALFORMED_PARAMS;
                }
            }

            module.SetMediaEntry(part, face, me);

            return ScriptBaseClass.LSL_STATUS_OK;
        }

        private LSL_Integer ClearPrimMedia(SceneObjectPart part, LSL_Integer face)
        {
            // LSL Spec http://wiki.secondlife.com/wiki/LlClearPrimMedia says to fail silently if face is invalid
            // Assuming silently fail means sending back LSL_STATUS_OK.  Ideally, need to check this.
            // FIXME: Don't perform the media check directly
            if (face < 0 || face > part.GetNumberOfSides() - 1)
                return ScriptBaseClass.LSL_STATUS_NOT_FOUND;

            IMoapModule module = m_ScriptEngine.World.RequestModuleInterface<IMoapModule>();
            if (null == module)
                return ScriptBaseClass.LSL_STATUS_NOT_SUPPORTED;

            module.ClearMediaEntry(part, face);

            return ScriptBaseClass.LSL_STATUS_OK;
        }

        private LSL_List ParseString2List(string src, LSL_List separators, LSL_List spacers, bool keepNulls)
        {
            var srclen = src.Length;
            var seplen = separators.Length;
            var separray = separators.Data;
            var spclen = spacers.Length;
            var spcarray = spacers.Data;
            var dellen = 0;
            var delarray = new string[seplen + spclen];

            var outlen = 0;
            var outarray = new string[srclen * 2 + 1];

            int i, j;
            string d;


            /*
             * Convert separator and spacer lists to C# strings.
             * Also filter out null strings so we don't hang.
             */
            for (i = 0; i < seplen; i++)
            {
                d = separray[i].ToString();
                if (d.Length > 0) delarray[dellen++] = d;
            }

            seplen = dellen;

            for (i = 0; i < spclen; i++)
            {
                d = spcarray[i].ToString();
                if (d.Length > 0) delarray[dellen++] = d;
            }

            /*
             * Scan through source string from beginning to end.
             */
            for (i = 0;;)
            {
                /*
                 * Find earliest delimeter in src starting at i (if any).
                 */
                var earliestDel = -1;
                var earliestSrc = srclen;
                string earliestStr = null;
                for (j = 0; j < dellen; j++)
                {
                    d = delarray[j];
                    if (d != null)
                    {
                        var index = src.IndexOf(d, i);
                        if (index < 0)
                        {
                            delarray[j] = null; // delim nowhere in src, don't check it anymore
                        }
                        else if (index < earliestSrc)
                        {
                            earliestSrc = index; // where delimeter starts in source string
                            earliestDel = j; // where delimeter is in delarray[]
                            earliestStr = d; // the delimeter string from delarray[]
                            if (index == i) break; // can't do any better than found at beg of string
                        }
                    }
                }

                /*
                 * Output source string starting at i through start of earliest delimeter.
                 */
                if (keepNulls || earliestSrc > i) outarray[outlen++] = src.Substring(i, earliestSrc - i);

                /*
                 * If no delimeter found at or after i, we're done scanning.
                 */
                if (earliestDel < 0) break;

                /*
                 * If delimeter was a spacer, output the spacer.
                 */
                if (earliestDel >= seplen) outarray[outlen++] = earliestStr;

                /*
                 * Look at rest of src string following delimeter.
                 */
                i = earliestSrc + earliestStr.Length;
            }

            /*
             * Make up an exact-sized output array suitable for an LSL_List object.
             */
            var outlist = new object[outlen];
            for (i = 0; i < outlen; i++) outlist[i] = new LSL_String(outarray[i]);
            return new LSL_List(outlist);
        }

        private int PermissionMaskToLSLPerm(uint value)
        {
            value &= fullperms;
            if (value == fullperms)
                return ScriptBaseClass.PERM_ALL;
            if (value == 0)
                return 0;

            var ret = 0;

            if ((value & (uint)PermissionMask.Copy) != 0)
                ret |= ScriptBaseClass.PERM_COPY;

            if ((value & (uint)PermissionMask.Modify) != 0)
                ret |= ScriptBaseClass.PERM_MODIFY;

            if ((value & (uint)PermissionMask.Move) != 0)
                ret |= ScriptBaseClass.PERM_MOVE;

            if ((value & (uint)PermissionMask.Transfer) != 0)
                ret |= ScriptBaseClass.PERM_TRANSFER;

            return ret;
        }

        private uint LSLPermToPermissionMask(int lslperm, uint oldvalue)
        {
            lslperm &= ScriptBaseClass.PERM_ALL;
            if (lslperm == ScriptBaseClass.PERM_ALL)
                return oldvalue |= fullperms;

            oldvalue &= ~fullperms;
            if (lslperm != 0)
            {
                if ((lslperm & ScriptBaseClass.PERM_COPY) != 0)
                    oldvalue |= (uint)PermissionMask.Copy;

                if ((lslperm & ScriptBaseClass.PERM_MODIFY) != 0)
                    oldvalue |= (uint)PermissionMask.Modify;

                if ((lslperm & ScriptBaseClass.PERM_MOVE) != 0)
                    oldvalue |= (uint)PermissionMask.Move;

                if ((lslperm & ScriptBaseClass.PERM_TRANSFER) != 0)
                    oldvalue |= (uint)PermissionMask.Transfer;
            }

            return oldvalue;
        }

        private int fixedCopyTransfer(int value)
        {
            if ((value & (ScriptBaseClass.PERM_COPY | ScriptBaseClass.PERM_TRANSFER)) == 0)
                value |= ScriptBaseClass.PERM_TRANSFER;
            return value;
        }

        private string truncateBase64(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var paddingPos = -1;
            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (c >= 'A' && c <= 'Z')
                    continue;
                if (c >= 'a' && c <= 'z')
                    continue;
                if (c >= '0' && c <= '9')
                    continue;
                if (c == '+' || c == '/')
                    continue;
                paddingPos = i;
                break;
            }

            if (paddingPos == 0)
                return string.Empty;

            if (paddingPos > 0)
                input = input.Substring(0, paddingPos);

            var remainder = input.Length % 4;
            switch (remainder)
            {
                case 0:
                    return input;
                case 1:
                    return input.Substring(0, input.Length - 1);
                case 2:
                    return input + "==";
            }

            return input + "=";
        }

        internal UUID GetScriptByName(string name)
        {
            TaskInventoryItem item = m_host.Inventory.GetInventoryItem(name);

            if (item == null || item.Type != 10)
                return UUID.Zero;

            return item.ItemID;
        }

        /// <summary>
        ///     Reports the script error in the viewer's Script Warning/Error dialog and shouts it on the debug channel.
        /// </summary>
        /// <param name="command">The name of the command that generated the error.</param>
        /// <param name="message">The error message to report to the user.</param>
        internal void Error(string command, string message)
        {
            var text = command + ": " + message;
            if (text.Length > 1023) text = text.Substring(0, 1023);

            World.SimChat(Utils.StringToBytes(text), ChatTypeEnum.DebugChannel, ScriptBaseClass.DEBUG_CHANNEL,
                m_host.ParentGroup.RootPart.AbsolutePosition, m_host.Name, m_host.UUID, false);

            IWorldComm wComm = m_ScriptEngine.World.RequestModuleInterface<IWorldComm>();
            if (wComm != null)
                wComm.DeliverMessage(ChatTypeEnum.Shout, ScriptBaseClass.DEBUG_CHANNEL, m_host.Name, m_host.UUID, text);
            Sleep(1000);
        }

        /// <summary>
        ///     Reports that the command is not implemented as a script error.
        /// </summary>
        /// <param name="command">The name of the command that is not implemented.</param>
        /// <param name="message">Additional information to report to the user. (Optional)</param>
        internal void NotImplemented(string command, string message = "")
        {
            if (throwErrorOnNotImplemented)
            {
                if (message != "") message = " - " + message;

                throw new NotImplementedException("Command not implemented: " + command + message);
            }

            var text = "Command not implemented";
            if (message != "") text = text + " - " + message;

            Error(command, text);
        }

        /// <summary>
        ///     Reports that the command is deprecated as a script error.
        /// </summary>
        /// <param name="command">The name of the command that is deprecated.</param>
        /// <param name="message">Additional information to report to the user. (Optional)</param>
        internal void Deprecated(string command, string message = "")
        {
            var text = "Command deprecated";
            if (message != "") text = text + " - " + message;

            Error(command, text);
        }

        protected void WithNotecard(UUID assetID, AssetRequestCallback cb)
        {
            World.AssetService.Get(assetID.ToString(), this,
                delegate(string i, object sender, AssetBase a)
                {
                    UUID.TryParse(i, out var uuid);
                    cb(uuid, a);
                });
        }

        public void print(string str)
        {
            // yes, this is a real LSL function. See: http://wiki.secondlife.com/wiki/Print
            var ossl = (IOSSL_Api)m_ScriptEngine.GetApi(m_item.ItemID, "OSSL");
            if (ossl != null)
            {
                ossl.CheckThreatLevel(ThreatLevel.High, "print");
                m_log.Info("LSL print():" + str);
            }
        }

        private string Name2Username(string name)
        {
            var parts = name.Split(' ');
            if (parts.Length < 2)
                return name.ToLower();
            if (parts[1].Equals("Resident"))
                return parts[0].ToLower();

            return name.Replace(" ", ".").ToLower();
        }

        private bool InBoundingBox(ScenePresence avatar, Vector3 point)
        {
            var height = avatar.Appearance.AvatarHeight;
            var b1 = avatar.AbsolutePosition + new Vector3(-0.22f, -0.22f, -height / 2);
            var b2 = avatar.AbsolutePosition + new Vector3(0.22f, 0.22f, height / 2);

            if (point.X > b1.X && point.X < b2.X &&
                point.Y > b1.Y && point.Y < b2.Y &&
                point.Z > b1.Z && point.Z < b2.Z)
                return true;
            return false;
        }

        private ContactResult[] AvatarIntersection(Vector3 rayStart, Vector3 rayEnd, bool skipPhys)
        {
            var contacts = new List<ContactResult>();

            var ab = rayEnd - rayStart;
            var ablen = ab.Length();

            World.ForEachScenePresence(delegate(ScenePresence sp)
            {
                if (skipPhys && sp.PhysicsActor != null)
                    return;

                var ac = sp.AbsolutePosition - rayStart;

                double d = Math.Abs(Vector3.Mag(Vector3.Cross(ab, ac)) / ablen);

                if (d > 1.5)
                    return;

                double d2 = Vector3.Dot(Vector3.Negate(ab), ac);

                if (d2 > 0)
                    return;

                var dp = Math.Sqrt(Vector3.Mag(ac) * Vector3.Mag(ac) - d * d);
                var p = rayStart + Vector3.Divide(Vector3.Multiply(ab, (float)dp), Vector3.Mag(ab));

                if (!InBoundingBox(sp, p))
                    return;

                var result = new ContactResult
                {
                    ConsumerID = sp.LocalId,
                    Depth = Vector3.Distance(rayStart, p),
                    Normal = Vector3.Zero,
                    Pos = p
                };

                contacts.Add(result);
            });

            return contacts.ToArray();
        }

        private ContactResult[] ObjectIntersection(Vector3 rayStart, Vector3 rayEnd, bool includePhysical,
            bool includeNonPhysical, bool includePhantom)
        {
            var ray = new Ray(rayStart, Vector3.Normalize(rayEnd - rayStart));
            var contacts = new List<ContactResult>();

            var ab = rayEnd - rayStart;

            World.ForEachSOG(delegate(SceneObjectGroup group)
            {
                if (m_host.ParentGroup == group)
                    return;

                if (group.IsAttachment)
                    return;

                if (group.RootPart.PhysActor == null)
                {
                    if (!includePhantom)
                        return;
                }
                else
                {
                    if (group.RootPart.PhysActor.IsPhysical)
                    {
                        if (!includePhysical)
                            return;
                    }
                    else
                    {
                        if (!includeNonPhysical)
                            return;
                    }
                }

                // Find the radius ouside of which we don't even need to hit test
                var radius = 0.0f;
                group.GetAxisAlignedBoundingBoxRaw(out var minX, out var maxX, out var minY, out var maxY, out var minZ,
                    out var maxZ);

                if (Math.Abs(minX) > radius)
                    radius = Math.Abs(minX);
                if (Math.Abs(minY) > radius)
                    radius = Math.Abs(minY);
                if (Math.Abs(minZ) > radius)
                    radius = Math.Abs(minZ);
                if (Math.Abs(maxX) > radius)
                    radius = Math.Abs(maxX);
                if (Math.Abs(maxY) > radius)
                    radius = Math.Abs(maxY);
                if (Math.Abs(maxZ) > radius)
                    radius = Math.Abs(maxZ);
                radius = radius * 1.413f;
                var ac = group.AbsolutePosition - rayStart;
//                Vector3 bc = group.AbsolutePosition - rayEnd;

                double d = Math.Abs(Vector3.Mag(Vector3.Cross(ab, ac)) / Vector3.Distance(rayStart, rayEnd));

                // Too far off ray, don't bother
                if (d > radius)
                    return;

                // Behind ray, drop
                double d2 = Vector3.Dot(Vector3.Negate(ab), ac);
                if (d2 > 0)
                    return;

                ray = new Ray(rayStart, Vector3.Normalize(rayEnd - rayStart));
                var intersection = group.TestIntersection(ray, true, false);
                // Miss.
                if (!intersection.HitTF)
                    return;

                var b1 = group.AbsolutePosition + new Vector3(minX, minY, minZ);
                var b2 = group.AbsolutePosition + new Vector3(maxX, maxY, maxZ);
                //m_log.DebugFormat("[LLCASTRAY]: min<{0},{1},{2}>, max<{3},{4},{5}> = hitp<{6},{7},{8}>", b1.X,b1.Y,b1.Z,b2.X,b2.Y,b2.Z,intersection.ipoint.X,intersection.ipoint.Y,intersection.ipoint.Z);
                if (!(intersection.ipoint.X >= b1.X && intersection.ipoint.X <= b2.X &&
                      intersection.ipoint.Y >= b1.Y && intersection.ipoint.Y <= b2.Y &&
                      intersection.ipoint.Z >= b1.Z && intersection.ipoint.Z <= b2.Z))
                    return;

                var result = new ContactResult
                {
                    ConsumerID = group.LocalId,
                    //Depth = intersection.distance;
                    Normal = intersection.normal,
                    Pos = intersection.ipoint
                };
                result.Depth = Vector3.Mag(rayStart - result.Pos);

                contacts.Add(result);
            });

            return contacts.ToArray();
        }

        private ContactResult? GroundIntersection(Vector3 rayStart, Vector3 rayEnd)
        {
            double[,] heightfield = World.Heightmap.GetDoubles();
            var contacts = new List<ContactResult>();

            var min = 2048.0;
            var max = 0.0;

            // Find the min and max of the heightfield
            for (var x = 0; x < World.Heightmap.Width; x++)
            for (var y = 0; y < World.Heightmap.Height; y++)
            {
                if (heightfield[x, y] > max)
                    max = heightfield[x, y];
                if (heightfield[x, y] < min)
                    min = heightfield[x, y];
            }


            // A ray extends past rayEnd, but doesn't go back before
            // rayStart. If the start is above the highest point of the ground
            // and the ray goes up, we can't hit the ground. Ever.
            if (rayStart.Z > max && rayEnd.Z >= rayStart.Z)
                return null;

            // Same for going down
            if (rayStart.Z < min && rayEnd.Z <= rayStart.Z)
                return null;

            var trilist = new List<Tri>();

            // Create our triangle list
            for (var x = 1; x < World.Heightmap.Width; x++)
            for (var y = 1; y < World.Heightmap.Height; y++)
            {
                var t1 = new Tri();
                var t2 = new Tri();

                var p1 = new Vector3(x - 1, y - 1, (float)heightfield[x - 1, y - 1]);
                var p2 = new Vector3(x, y - 1, (float)heightfield[x, y - 1]);
                var p3 = new Vector3(x, y, (float)heightfield[x, y]);
                var p4 = new Vector3(x - 1, y, (float)heightfield[x - 1, y]);

                t1.p1 = p1;
                t1.p2 = p2;
                t1.p3 = p3;

                t2.p1 = p3;
                t2.p2 = p4;
                t2.p3 = p1;

                trilist.Add(t1);
                trilist.Add(t2);
            }

            // Ray direction
            var rayDirection = rayEnd - rayStart;

            foreach (var t in trilist)
            {
                // Compute triangle plane normal and edges
                var u = t.p2 - t.p1;
                var v = t.p3 - t.p1;
                var n = Vector3.Cross(u, v);

                if (n.IsZero())
                    continue;

                var w0 = rayStart - t.p1;
                double a = -Vector3.Dot(n, w0);
                double b = Vector3.Dot(n, rayDirection);

                // Not intersecting the plane, or in plane (same thing)
                // Ignoring this MAY cause the ground to not be detected
                // sometimes
                if (Math.Abs(b) < 0.000001)
                    continue;

                var r = a / b;

                // ray points away from plane
                if (r < 0.0)
                    continue;

                var ip = rayStart + Vector3.Multiply(rayDirection, (float)r);

                var uu = Vector3.Dot(u, u);
                var uv = Vector3.Dot(u, v);
                var vv = Vector3.Dot(v, v);
                var w = ip - t.p1;
                var wu = Vector3.Dot(w, u);
                var wv = Vector3.Dot(w, v);
                var d = uv * uv - uu * vv;

                var cs = (uv * wv - vv * wu) / d;
                if (cs < 0 || cs > 1.0)
                    continue;
                var ct = (uv * wu - uu * wv) / d;
                if (ct < 0 || cs + ct > 1.0)
                    continue;

                // Add contact point
                var result = new ContactResult
                {
                    ConsumerID = 0,
                    Depth = Vector3.Distance(rayStart, ip),
                    Normal = n,
                    Pos = ip
                };

                contacts.Add(result);
            }

            if (contacts.Count == 0)
                return null;

            contacts.Sort(delegate(ContactResult a, ContactResult b) { return (int)(a.Depth - b.Depth); });

            return contacts[0];
        }


        /// <summary>
        ///     Implementation of llCastRay similar to SL 2015-04-21.
        ///     http://wiki.secondlife.com/wiki/LlCastRay
        ///     Uses pure geometry, bounding shapes, meshing and no physics
        ///     for prims, sculpts, meshes, avatars and terrain.
        ///     Implements all flags, reject types and data flags.
        ///     Can handle both objects/groups and prims/parts, by config.
        ///     May sometimes be inaccurate owing to calculation precision,
        ///     meshing detail level and a bug in libopenmetaverse PrimMesher.
        /// </summary>
        public LSL_List llCastRayV3(LSL_Vector start, LSL_Vector end, LSL_List options)
        {
            var result = new LSL_List();

            // Prepare throttle data
            var calledMs = Environment.TickCount;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            UUID regionId = World.RegionInfo.RegionID;
            var userId = UUID.Zero;
            var msAvailable = 0;
            // Throttle per owner when attachment or "vehicle" (sat upon)
            if (m_host.ParentGroup.IsAttachment || m_host.ParentGroup.GetSittingAvatarsCount() > 0)
            {
                userId = m_host.OwnerID;
                msAvailable = m_msPerAvatarInCastRay;
            }
            // Throttle per parcel when not attachment or vehicle
            else
            {
                LandData land = World.GetLandData(m_host.GetWorldPosition());
                if (land != null)
                    msAvailable = m_msPerRegionInCastRay * land.Area / 65536;
            }

            // Clamp for "oversized" parcels on varregions
            if (msAvailable > m_msMaxInCastRay)
                msAvailable = m_msMaxInCastRay;

            // Check throttle data
            var fromCalledMs = calledMs - m_msThrottleInCastRay;
            lock (m_castRayCalls)
            {
                for (var i = m_castRayCalls.Count - 1; i >= 0; i--)
                    // Delete old calls from throttle data
                    if (m_castRayCalls[i].CalledMs < fromCalledMs)
                        m_castRayCalls.RemoveAt(i);
                    // Use current region (in multi-region sims)
                    else if (m_castRayCalls[i].RegionId.Equals(regionId))
                        // Reduce available time with recent calls
                        if (m_castRayCalls[i].UserId.Equals(userId))
                            msAvailable -= m_castRayCalls[i].UsedMs;

                // Return failure if not enough available time
                if (msAvailable < m_msMinInCastRay)
                {
                    result.Add(new LSL_Integer(ScriptBaseClass.RCERR_CAST_TIME_EXCEEDED));
                    return result;
                }
            }

            // Initialize
            var rayHits = new List<RayHit>();
            float tol = m_floatToleranceInCastRay;
            Vector3 pos1Ray = start;
            Vector3 pos2Ray = end;

            // Get input options
            var rejectTypes = 0;
            var dataFlags = 0;
            var maxHits = 1;
            var notdetectPhantom = true;
            for (var i = 0; i < options.Length; i += 2)
                if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_REJECT_TYPES)
                    rejectTypes = options.GetLSLIntegerItem(i + 1);
                else if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_DATA_FLAGS)
                    dataFlags = options.GetLSLIntegerItem(i + 1);
                else if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_MAX_HITS)
                    maxHits = options.GetLSLIntegerItem(i + 1);
                else if (options.GetLSLIntegerItem(i) == ScriptBaseClass.RC_DETECT_PHANTOM)
                    notdetectPhantom = options.GetLSLIntegerItem(i + 1) == 0;
            if (maxHits > m_maxHitsInCastRay)
                maxHits = m_maxHitsInCastRay;
            bool rejectAgents = (rejectTypes & ScriptBaseClass.RC_REJECT_AGENTS) != 0;
            bool rejectPhysical = (rejectTypes & ScriptBaseClass.RC_REJECT_PHYSICAL) != 0;
            bool rejectNonphysical = (rejectTypes & ScriptBaseClass.RC_REJECT_NONPHYSICAL) != 0;
            bool rejectLand = (rejectTypes & ScriptBaseClass.RC_REJECT_LAND) != 0;
            bool getNormal = (dataFlags & ScriptBaseClass.RC_GET_NORMAL) != 0;
            bool getRootKey = (dataFlags & ScriptBaseClass.RC_GET_ROOT_KEY) != 0;
            bool getLinkNum = (dataFlags & ScriptBaseClass.RC_GET_LINK_NUM) != 0;

            // Calculate some basic parameters
            var vecRay = pos2Ray - pos1Ray;
            var rayLength = vecRay.Length();

            // Try to get a mesher and return failure if none, degenerate ray, or max 0 hits
            IRendering primMesher = null;
            var renderers = RenderingLoader.ListRenderers(Util.ExecutingDirectory());
            if (renderers.Count < 1 || rayLength < tol || m_maxHitsInCastRay < 1)
            {
                result.Add(new LSL_Integer(ScriptBaseClass.RCERR_UNKNOWN));
                return result;
            }

            primMesher = RenderingLoader.LoadRenderer(renderers[0]);

            // Iterate over all objects/groups and prims/parts in region
            World.ForEachSOG(
                delegate(SceneObjectGroup group)
                {
                    if (group.IsDeleted || group.RootPart == null)
                        return;
                    // Check group filters unless part filters are configured
                    var isPhysical = group.RootPart.PhysActor != null && group.RootPart.PhysActor.IsPhysical;
                    var isNonphysical = !isPhysical;
                    var isPhantom = group.IsPhantom || group.IsVolumeDetect;
                    var isAttachment = group.IsAttachment;
                    if (isPhysical && rejectPhysical)
                        return;
                    if (isNonphysical && rejectNonphysical)
                        return;
                    if (isPhantom && notdetectPhantom)
                        return;
                    if (isAttachment && !m_doAttachmentsInCastRay)
                        return;

                    // Parse object/group if passed filters
                    // Iterate over all prims/parts in object/group
                    foreach (var part in group.Parts)
                    {
                        // ignore PhysicsShapeType.None as physics engines do
                        // or we will get into trouble in future
                        if (part.PhysicsShapeType == (byte)PhysicsShapeType.None)
                            continue;
                        isPhysical = part.PhysActor != null && part.PhysActor.IsPhysical;
                        isNonphysical = !isPhysical;
                        isPhantom = (part.Flags & PrimFlags.Phantom) != 0 ||
                                    part.VolumeDetectActive;

                        if (isPhysical && rejectPhysical)
                            continue;
                        if (isNonphysical && rejectNonphysical)
                            continue;
                        if (isPhantom && notdetectPhantom)
                            continue;

                        // Parse prim/part and project ray if passed filters
                        var scalePart = part.Scale;
                        var posPart = part.GetWorldPosition();
                        var rotPart = part.GetWorldRotation();
                        var rotPartInv = Quaternion.Inverse(rotPart);
                        var pos1RayProj = (pos1Ray - posPart) * rotPartInv / scalePart;
                        var pos2RayProj = (pos2Ray - posPart) * rotPartInv / scalePart;

                        // Filter parts by shape bounding boxes
                        var shapeBoxMax = new Vector3(0.5f, 0.5f, 0.5f);
                        if (!part.Shape.SculptEntry)
                            shapeBoxMax = shapeBoxMax * new Vector3(m_primSafetyCoeffX, m_primSafetyCoeffY,
                                m_primSafetyCoeffZ);
                        shapeBoxMax = shapeBoxMax + new Vector3(tol, tol, tol);
                        if (RayIntersectsShapeBox(pos1RayProj, pos2RayProj, shapeBoxMax))
                        {
                            // Prepare data needed to check for ray hits
                            var rayTrans = new RayTrans
                            {
                                PartId = part.UUID,
                                GroupId = part.ParentGroup.UUID,
                                Link = group.PrimCount > 1 ? part.LinkNum : 0,
                                ScalePart = scalePart,
                                PositionPart = posPart,
                                RotationPart = rotPart,
                                ShapeNeedsEnds = true,
                                Position1Ray = pos1Ray,
                                Position1RayProj = pos1RayProj,
                                VectorRayProj = pos2RayProj - pos1RayProj
                            };

                            // Get detail level depending on type
                            var lod = 0;
                            // Mesh detail level
                            if (part.Shape.SculptEntry && part.Shape.SculptType == (byte)SculptType.Mesh)
                                lod = (int)m_meshLodInCastRay;
                            // Sculpt detail level
                            else if (part.Shape.SculptEntry && part.Shape.SculptType == (byte)SculptType.Mesh)
                                lod = (int)m_sculptLodInCastRay;
                            // Shape detail level
                            else if (!part.Shape.SculptEntry)
                                lod = (int)m_primLodInCastRay;

                            // Try to get cached mesh if configured
                            ulong meshKey = 0;
                            FacetedMesh mesh = null;
                            if (m_useMeshCacheInCastRay)
                            {
                                meshKey = part.Shape.GetMeshKey(Vector3.One, 4 << lod);
                                lock (m_cachedMeshes)
                                {
                                    m_cachedMeshes.TryGetValue(meshKey, out mesh);
                                }
                            }

                            // Create mesh if no cached mesh
                            if (mesh == null)
                            {
                                // Make an OMV prim to be able to mesh part
                                var omvPrim = part.Shape.ToOmvPrimitive(posPart, rotPart);
                                byte[] sculptAsset = null;
                                if (omvPrim.Sculpt != null)
                                    sculptAsset = World.AssetService.GetData(omvPrim.Sculpt.SculptTexture.ToString());

                                // When part is mesh, get mesh
                                if (omvPrim.Sculpt != null && omvPrim.Sculpt.Type == SculptType.Mesh &&
                                    sculptAsset != null)
                                {
                                    var meshAsset = new AssetMesh(omvPrim.Sculpt.SculptTexture, sculptAsset);
                                    FacetedMesh.TryDecodeFromAsset(omvPrim, meshAsset, m_meshLodInCastRay, out mesh);
                                    meshAsset = null;
                                }

                                // When part is sculpt, create mesh
                                // Quirk: Generated sculpt mesh is about 2.8% smaller in X and Y than visual sculpt.
                                else if (omvPrim.Sculpt != null && omvPrim.Sculpt.Type != SculptType.Mesh &&
                                         sculptAsset != null)
                                {
                                    IJ2KDecoder imgDecoder = World.RequestModuleInterface<IJ2KDecoder>();
                                    if (imgDecoder != null)
                                    {
                                        var sculpt = imgDecoder.DecodeToImage(sculptAsset);
                                        if (sculpt != null)
                                        {
                                            mesh = primMesher.GenerateFacetedSculptMesh(omvPrim, (Bitmap)sculpt,
                                                m_sculptLodInCastRay);
                                            sculpt.Dispose();
                                        }
                                    }
                                }

                                // When part is shape, create mesh
                                else if (omvPrim.Sculpt == null)
                                {
                                    if (
                                        omvPrim.PrimData.PathBegin == 0.0 && omvPrim.PrimData.PathEnd == 1.0 &&
                                        omvPrim.PrimData.PathTaperX == 0.0 && omvPrim.PrimData.PathTaperY == 0.0 &&
                                        omvPrim.PrimData.PathSkew == 0.0 &&
                                        omvPrim.PrimData.PathTwist - omvPrim.PrimData.PathTwistBegin == 0.0
                                    )
                                        rayTrans.ShapeNeedsEnds = false;
                                    mesh = primMesher.GenerateFacetedMesh(omvPrim, m_primLodInCastRay);
                                }

                                // Cache mesh if configured
                                if (m_useMeshCacheInCastRay && mesh != null)
                                    lock (m_cachedMeshes)
                                    {
                                        if (!m_cachedMeshes.ContainsKey(meshKey))
                                            m_cachedMeshes.Add(meshKey, mesh);
                                    }
                            }

                            // Check mesh for ray hits
                            AddRayInFacetedMesh(mesh, rayTrans, ref rayHits);
                            mesh = null;
                        }
                    }
                }
            );

            // Check avatar filter
            if (!rejectAgents)
                // Iterate over all avatars in region
                World.ForEachRootScenePresence(
                    delegate(ScenePresence sp)
                    {
                        // Get bounding box
                        BoundingBoxOfScenePresence(sp, out var lower, out var upper);
                        // Parse avatar
                        var scalePart = upper - lower;
                        var posPart = sp.AbsolutePosition;
                        var rotPart = sp.GetWorldRotation();
                        var rotPartInv = Quaternion.Inverse(rotPart);
                        posPart = posPart + (lower + upper) * 0.5f * rotPart;
                        // Project ray
                        var pos1RayProj = (pos1Ray - posPart) * rotPartInv / scalePart;
                        var pos2RayProj = (pos2Ray - posPart) * rotPartInv / scalePart;

                        // Filter avatars by shape bounding boxes
                        var shapeBoxMax = new Vector3(0.5f + tol, 0.5f + tol, 0.5f + tol);
                        if (RayIntersectsShapeBox(pos1RayProj, pos2RayProj, shapeBoxMax))
                        {
                            // Prepare data needed to check for ray hits
                            var rayTrans = new RayTrans
                            {
                                PartId = sp.UUID,
                                GroupId = sp.ParentPart != null ? sp.ParentPart.ParentGroup.UUID : sp.UUID,
                                Link = sp.ParentPart != null ? UUID2LinkNumber(sp.ParentPart, sp.UUID) : 0,
                                ScalePart = scalePart,
                                PositionPart = posPart,
                                RotationPart = rotPart,
                                ShapeNeedsEnds = false,
                                Position1Ray = pos1Ray,
                                Position1RayProj = pos1RayProj,
                                VectorRayProj = pos2RayProj - pos1RayProj
                            };

                            // Try to get cached mesh if configured
                            var prim = PrimitiveBaseShape.CreateSphere();
                            var lod = (int)m_avatarLodInCastRay;
                            var meshKey = prim.GetMeshKey(Vector3.One, 4 << lod);
                            FacetedMesh mesh = null;
                            if (m_useMeshCacheInCastRay)
                                lock (m_cachedMeshes)
                                {
                                    m_cachedMeshes.TryGetValue(meshKey, out mesh);
                                }

                            // Create mesh if no cached mesh
                            if (mesh == null)
                            {
                                // Make OMV prim and create mesh
                                prim.Scale = scalePart;
                                var omvPrim = prim.ToOmvPrimitive(posPart, rotPart);
                                mesh = primMesher.GenerateFacetedMesh(omvPrim, m_avatarLodInCastRay);

                                // Cache mesh if configured
                                if (m_useMeshCacheInCastRay && mesh != null)
                                    lock (m_cachedMeshes)
                                    {
                                        if (!m_cachedMeshes.ContainsKey(meshKey))
                                            m_cachedMeshes.Add(meshKey, mesh);
                                    }
                            }

                            // Check mesh for ray hits
                            AddRayInFacetedMesh(mesh, rayTrans, ref rayHits);
                            mesh = null;
                        }
                    }
                );

            // Check terrain filter
            if (!rejectLand)
            {
                // Parse terrain

                // Mesh terrain and check bounding box
                var triangles = TrisFromHeightmapUnderRay(pos1Ray, pos2Ray, out var lower, out var upper);
                lower.Z -= tol;
                upper.Z += tol;
                if ((pos1Ray.Z >= lower.Z || pos2Ray.Z >= lower.Z) && (pos1Ray.Z <= upper.Z || pos2Ray.Z <= upper.Z))
                {
                    // Prepare data needed to check for ray hits
                    var rayTrans = new RayTrans
                    {
                        PartId = UUID.Zero,
                        GroupId = UUID.Zero,
                        Link = 0,
                        ScalePart = new Vector3(1.0f, 1.0f, 1.0f),
                        PositionPart = Vector3.Zero,
                        RotationPart = Quaternion.Identity,
                        ShapeNeedsEnds = true,
                        Position1Ray = pos1Ray,
                        Position1RayProj = pos1Ray,
                        VectorRayProj = vecRay
                    };

                    // Check mesh
                    AddRayInTris(triangles, rayTrans, ref rayHits);
                    triangles = null;
                }
            }

            // Sort hits by ascending distance
            rayHits.Sort((s1, s2) => s1.Distance.CompareTo(s2.Distance));

            // Check excess hits per part and group
            for (var t = 0; t < 2; t++)
            {
                var maxHitsPerType = 0;
                var id = UUID.Zero;
                if (t == 0)
                    maxHitsPerType = m_maxHitsPerPrimInCastRay;
                else
                    maxHitsPerType = m_maxHitsPerObjectInCastRay;

                // Handle excess hits only when needed
                if (maxHitsPerType < m_maxHitsInCastRay)
                {
                    // Find excess hits
                    var hits = new Hashtable();
                    for (var i = rayHits.Count - 1; i >= 0; i--)
                    {
                        if (t == 0)
                            id = rayHits[i].PartId;
                        else
                            id = rayHits[i].GroupId;
                        if (hits.ContainsKey(id))
                            hits[id] = (int)hits[id] + 1;
                        else
                            hits[id] = 1;
                    }

                    // Remove excess hits
                    for (var i = rayHits.Count - 1; i >= 0; i--)
                    {
                        if (t == 0)
                            id = rayHits[i].PartId;
                        else
                            id = rayHits[i].GroupId;
                        var hit = (int)hits[id];
                        if (hit > m_maxHitsPerPrimInCastRay)
                        {
                            rayHits.RemoveAt(i);
                            hit--;
                            hits[id] = hit;
                        }
                    }
                }
            }

            // Parse hits into result list according to data flags
            var hitCount = rayHits.Count;
            if (hitCount > maxHits)
                hitCount = maxHits;
            for (var i = 0; i < hitCount; i++)
            {
                var rayHit = rayHits[i];
                if (getRootKey)
                    result.Add(new LSL_Key(rayHit.GroupId.ToString()));
                else
                    result.Add(new LSL_Key(rayHit.PartId.ToString()));
                result.Add(new LSL_Vector(rayHit.Position));
                if (getLinkNum)
                    result.Add(new LSL_Integer(rayHit.Link));
                if (getNormal)
                    result.Add(new LSL_Vector(rayHit.Normal));
            }

            result.Add(new LSL_Integer(hitCount));

            // Add to throttle data
            stopWatch.Stop();
            lock (m_castRayCalls)
            {
                var castRayCall = new CastRayCall
                {
                    RegionId = regionId,
                    UserId = userId,
                    CalledMs = calledMs,
                    UsedMs = (int)stopWatch.ElapsedMilliseconds
                };
                m_castRayCalls.Add(castRayCall);
            }

            // Return hits
            return result;
        }

        /// <summary>
        ///     Helper to check if a ray intersects a shape bounding box.
        /// </summary>
        private bool RayIntersectsShapeBox(Vector3 pos1RayProj, Vector3 pos2RayProj, Vector3 shapeBoxMax)
        {
            // Skip if ray can't intersect bounding box;
            var rayBoxProjMin = Vector3.Min(pos1RayProj, pos2RayProj);
            var rayBoxProjMax = Vector3.Max(pos1RayProj, pos2RayProj);
            if (
                rayBoxProjMin.X > shapeBoxMax.X || rayBoxProjMin.Y > shapeBoxMax.Y || rayBoxProjMin.Z > shapeBoxMax.Z ||
                rayBoxProjMax.X < -shapeBoxMax.X || rayBoxProjMax.Y < -shapeBoxMax.Y || rayBoxProjMax.Z < -shapeBoxMax.Z
            )
                return false;

            // Check if ray intersect any bounding box side
            var sign = 0;
            var dist = 0.0f;
            var posProj = Vector3.Zero;
            var vecRayProj = pos2RayProj - pos1RayProj;

            // Check both X sides unless ray is parallell to them
            if (Math.Abs(vecRayProj.X) > m_floatToleranceInCastRay)
                for (sign = -1; sign <= 1; sign += 2)
                {
                    dist = (sign * shapeBoxMax.X - pos1RayProj.X) / vecRayProj.X;
                    posProj = pos1RayProj + vecRayProj * dist;
                    if (Math.Abs(posProj.Y) <= shapeBoxMax.Y && Math.Abs(posProj.Z) <= shapeBoxMax.Z)
                        return true;
                }

            // Check both Y sides unless ray is parallell to them
            if (Math.Abs(vecRayProj.Y) > m_floatToleranceInCastRay)
                for (sign = -1; sign <= 1; sign += 2)
                {
                    dist = (sign * shapeBoxMax.Y - pos1RayProj.Y) / vecRayProj.Y;
                    posProj = pos1RayProj + vecRayProj * dist;
                    if (Math.Abs(posProj.X) <= shapeBoxMax.X && Math.Abs(posProj.Z) <= shapeBoxMax.Z)
                        return true;
                }

            // Check both Z sides unless ray is parallell to them
            if (Math.Abs(vecRayProj.Z) > m_floatToleranceInCastRay)
                for (sign = -1; sign <= 1; sign += 2)
                {
                    dist = (sign * shapeBoxMax.Z - pos1RayProj.Z) / vecRayProj.Z;
                    posProj = pos1RayProj + vecRayProj * dist;
                    if (Math.Abs(posProj.X) <= shapeBoxMax.X && Math.Abs(posProj.Y) <= shapeBoxMax.Y)
                        return true;
                }

            // No hits on bounding box so return false
            return false;
        }

        /// <summary>
        ///     Helper to parse FacetedMesh for ray hits.
        /// </summary>
        private void AddRayInFacetedMesh(FacetedMesh mesh, RayTrans rayTrans, ref List<RayHit> rayHits)
        {
            if (mesh != null)
                foreach (var face in mesh.Faces)
                    for (var i = 0; i < face.Indices.Count; i += 3)
                    {
                        var triangle = new Tri
                        {
                            p1 = face.Vertices[face.Indices[i]].Position,
                            p2 = face.Vertices[face.Indices[i + 1]].Position,
                            p3 = face.Vertices[face.Indices[i + 2]].Position
                        };
                        AddRayInTri(triangle, rayTrans, ref rayHits);
                    }
        }

        /// <summary>
        ///     Helper to parse Tri (triangle) List for ray hits.
        /// </summary>
        private void AddRayInTris(List<Tri> triangles, RayTrans rayTrans, ref List<RayHit> rayHits)
        {
            foreach (var triangle in triangles) AddRayInTri(triangle, rayTrans, ref rayHits);
        }

        /// <summary>
        ///     Helper to add ray hit in a Tri (triangle).
        /// </summary>
        private void AddRayInTri(Tri triProj, RayTrans rayTrans, ref List<RayHit> rayHits)
        {
            // Check for hit in triangle
            if (HitRayInTri(triProj, rayTrans.Position1RayProj, rayTrans.VectorRayProj, out var posHitProj,
                    out var normalProj))
            {
                // Hack to circumvent ghost face bug in PrimMesher by removing hits in (ghost) face plane through shape center
                if (Math.Abs(Vector3.Dot(posHitProj, normalProj)) < m_floatToleranceInCastRay &&
                    !rayTrans.ShapeNeedsEnds)
                    return;

                // Transform hit and normal to region coordinate system
                var posHit = rayTrans.PositionPart + posHitProj * rayTrans.ScalePart * rayTrans.RotationPart;
                var normal = Vector3.Normalize(normalProj * rayTrans.ScalePart * rayTrans.RotationPart);

                // Remove duplicate hits at triangle intersections
                var distance = Vector3.Distance(rayTrans.Position1Ray, posHit);
                for (var i = rayHits.Count - 1; i >= 0; i--)
                {
                    if (rayHits[i].PartId != rayTrans.PartId)
                        break;
                    if (Math.Abs(rayHits[i].Distance - distance) < m_floatTolerance2InCastRay)
                        return;
                }

                // Build result data set
                var rayHit = new RayHit
                {
                    PartId = rayTrans.PartId,
                    GroupId = rayTrans.GroupId,
                    Link = rayTrans.Link,
                    Position = posHit,
                    Normal = normal,
                    Distance = distance
                };
                rayHits.Add(rayHit);
            }
        }

        /// <summary>
        ///     Helper to find ray hit in triangle
        /// </summary>
        private bool HitRayInTri(Tri triProj, Vector3 pos1RayProj, Vector3 vecRayProj, out Vector3 posHitProj,
            out Vector3 normalProj)
        {
            float tol = m_floatToleranceInCastRay;
            posHitProj = Vector3.Zero;

            // Calculate triangle edge vectors
            var vec1Proj = triProj.p2 - triProj.p1;
            var vec2Proj = triProj.p3 - triProj.p2;
            var vec3Proj = triProj.p1 - triProj.p3;

            // Calculate triangle normal
            normalProj = Vector3.Cross(vec1Proj, vec2Proj);

            // Skip if degenerate triangle or ray parallell with triangle plane
            var divisor = Vector3.Dot(vecRayProj, normalProj);
            if (Math.Abs(divisor) < tol)
                return false;

            // Skip if exit and not configured to detect
            if (divisor > tol && !m_detectExitsInCastRay)
                return false;

            // Skip if outside ray ends
            var distanceProj = Vector3.Dot(triProj.p1 - pos1RayProj, normalProj) / divisor;
            if (distanceProj < -tol || distanceProj > 1 + tol)
                return false;

            // Calculate hit position in triangle
            posHitProj = pos1RayProj + vecRayProj * distanceProj;

            // Skip if outside triangle bounding box
            var triProjMin = Vector3.Min(Vector3.Min(triProj.p1, triProj.p2), triProj.p3);
            var triProjMax = Vector3.Max(Vector3.Max(triProj.p1, triProj.p2), triProj.p3);
            if (
                posHitProj.X < triProjMin.X - tol || posHitProj.Y < triProjMin.Y - tol ||
                posHitProj.Z < triProjMin.Z - tol ||
                posHitProj.X > triProjMax.X + tol || posHitProj.Y > triProjMax.Y + tol ||
                posHitProj.Z > triProjMax.Z + tol
            )
                return false;

            // Skip if outside triangle
            if (
                Vector3.Dot(Vector3.Cross(vec1Proj, normalProj), posHitProj - triProj.p1) > tol ||
                Vector3.Dot(Vector3.Cross(vec2Proj, normalProj), posHitProj - triProj.p2) > tol ||
                Vector3.Dot(Vector3.Cross(vec3Proj, normalProj), posHitProj - triProj.p3) > tol
            )
                return false;

            // Return hit
            return true;
        }

        /// <summary>
        ///     Helper to parse selected parts of HeightMap into a Tri (triangle) List and calculate bounding box.
        /// </summary>
        private List<Tri> TrisFromHeightmapUnderRay(Vector3 posStart, Vector3 posEnd, out Vector3 lower,
            out Vector3 upper)
        {
            // Get bounding X-Y rectangle of terrain under ray
            lower = Vector3.Min(posStart, posEnd);
            upper = Vector3.Max(posStart, posEnd);
            lower.X = (float)Math.Floor(lower.X);
            lower.Y = (float)Math.Floor(lower.Y);
            var zLower = float.MaxValue;
            upper.X = (float)Math.Ceiling(upper.X);
            upper.Y = (float)Math.Ceiling(upper.Y);
            var zUpper = float.MinValue;

            // Initialize Tri (triangle) List
            var triangles = new List<Tri>();

            // Set parsing lane direction to major ray X-Y axis
            var vec = posEnd - posStart;
            var xAbs = Math.Abs(vec.X);
            var yAbs = Math.Abs(vec.Y);
            var bigX = true;
            if (yAbs > xAbs)
            {
                bigX = false;
                vec = vec / yAbs;
            }
            else if (xAbs > yAbs || xAbs > 0.0f)
            {
                vec = vec / xAbs;
            }
            else
            {
                vec = new Vector3(1.0f, 1.0f, 0.0f);
            }

            // Simplify by start parsing in lower end of lane
            if ((bigX && vec.X < 0.0f) || (!bigX && vec.Y < 0.0f))
            {
                var posTemp = posStart;
                posStart = posEnd;
                posEnd = posTemp;
                vec = vec * -1.0f;
            }

            // First 1x1 rectangle under ray
            var xFloorOld = 0.0f;
            var yFloorOld = 0.0f;
            var pos = posStart;
            var xFloor = (float)Math.Floor(pos.X);
            var yFloor = (float)Math.Floor(pos.Y);
            AddTrisFromHeightmap(xFloor, yFloor, ref triangles, ref zLower, ref zUpper);

            // Parse every remaining 1x1 rectangle under ray
            while (pos != posEnd)
            {
                // Next 1x1 rectangle under ray
                xFloorOld = xFloor;
                yFloorOld = yFloor;
                pos = pos + vec;

                // Clip position to 1x1 rectangle border
                xFloor = (float)Math.Floor(pos.X);
                yFloor = (float)Math.Floor(pos.Y);
                if (bigX && pos.X > xFloor)
                {
                    pos.Y -= vec.Y * (pos.X - xFloor);
                    pos.X = xFloor;
                }
                else if (!bigX && pos.Y > yFloor)
                {
                    pos.X -= vec.X * (pos.Y - yFloor);
                    pos.Y = yFloor;
                }

                // Last 1x1 rectangle under ray
                if ((bigX && pos.X >= posEnd.X) || (!bigX && pos.Y >= posEnd.Y))
                {
                    pos = posEnd;
                    xFloor = (float)Math.Floor(pos.X);
                    yFloor = (float)Math.Floor(pos.Y);
                }

                // Add new 1x1 rectangle in lane
                if ((bigX && xFloor != xFloorOld) || (!bigX && yFloor != yFloorOld))
                    AddTrisFromHeightmap(xFloor, yFloor, ref triangles, ref zLower, ref zUpper);
                // Add last 1x1 rectangle in old lane at lane shift
                if (bigX && yFloor != yFloorOld)
                    AddTrisFromHeightmap(xFloor, yFloorOld, ref triangles, ref zLower, ref zUpper);
                if (!bigX && xFloor != xFloorOld)
                    AddTrisFromHeightmap(xFloorOld, yFloor, ref triangles, ref zLower, ref zUpper);
            }

            // Finalize bounding box Z
            lower.Z = zLower;
            upper.Z = zUpper;

            // Done and returning Tri (triangle)List
            return triangles;
        }

        /// <summary>
        ///     Helper to add HeightMap squares into Tri (triangle) List and adjust bounding box.
        /// </summary>
        private void AddTrisFromHeightmap(float xPos, float yPos, ref List<Tri> triangles, ref float zLower,
            ref float zUpper)
        {
            var xInt = (int)xPos;
            var yInt = (int)yPos;

            // Corner 1 of 1x1 rectangle
            var x = Util.Clamp(xInt + 1, 0, World.Heightmap.Width - 1);
            var y = Util.Clamp(yInt + 1, 0, World.Heightmap.Height - 1);
            var pos1 = new Vector3(x, y, (float)World.Heightmap[x, y]);
            // Adjust bounding box
            zLower = Math.Min(zLower, pos1.Z);
            zUpper = Math.Max(zUpper, pos1.Z);

            // Corner 2 of 1x1 rectangle
            x = Util.Clamp(xInt, 0, World.Heightmap.Width - 1);
            y = Util.Clamp(yInt + 1, 0, World.Heightmap.Height - 1);
            var pos2 = new Vector3(x, y, (float)World.Heightmap[x, y]);
            // Adjust bounding box
            zLower = Math.Min(zLower, pos2.Z);
            zUpper = Math.Max(zUpper, pos2.Z);

            // Corner 3 of 1x1 rectangle
            x = Util.Clamp(xInt, 0, World.Heightmap.Width - 1);
            y = Util.Clamp(yInt, 0, World.Heightmap.Height - 1);
            var pos3 = new Vector3(x, y, (float)World.Heightmap[x, y]);
            // Adjust bounding box
            zLower = Math.Min(zLower, pos3.Z);
            zUpper = Math.Max(zUpper, pos3.Z);

            // Corner 4 of 1x1 rectangle
            x = Util.Clamp(xInt + 1, 0, World.Heightmap.Width - 1);
            y = Util.Clamp(yInt, 0, World.Heightmap.Height - 1);
            var pos4 = new Vector3(x, y, (float)World.Heightmap[x, y]);
            // Adjust bounding box
            zLower = Math.Min(zLower, pos4.Z);
            zUpper = Math.Max(zUpper, pos4.Z);

            // Add triangle 1
            var triangle1 = new Tri
            {
                p1 = pos1,
                p2 = pos2,
                p3 = pos3
            };
            triangles.Add(triangle1);

            // Add triangle 2
            var triangle2 = new Tri
            {
                p1 = pos3,
                p2 = pos4,
                p3 = pos1
            };
            triangles.Add(triangle2);
        }

        /// <summary>
        ///     Helper to get link number for a UUID.
        /// </summary>
        private int UUID2LinkNumber(SceneObjectPart part, UUID id)
        {
            var group = part.ParentGroup;
            if (group != null)
            {
                // Parse every link for UUID
                var linkCount = group.PrimCount + group.GetSittingAvatarsCount();
                for (var link = linkCount; link > 0; link--)
                {
                    ISceneEntity entity = GetLinkEntity(part, link);
                    // Return link number if UUID match
                    if (entity != null && entity.UUID == id)
                        return link;
                }
            }

            // Return link number 0 if no links or UUID matches
            return 0;
        }


        protected LSL_List SetPrimParams(ScenePresence av, LSL_List rules, string originFunc, ref uint rulesParsed)
        {
            //This is a special version of SetPrimParams to deal with avatars which are sitting on the linkset.

            var idx = 0;
            var idxStart = 0;

            var positionChanged = false;
            try
            {
                while (idx < rules.Length)
                {
                    ++rulesParsed;
                    int code = rules.GetLSLIntegerItem(idx++);

                    var remain = rules.Length - idx;
                    idxStart = idx;

                    switch (code)
                    {
                        case ScriptBaseClass.PRIM_POSITION:
                        case ScriptBaseClass.PRIM_POS_LOCAL:
                        {
                            if (remain < 1)
                                return new LSL_List();

                            LSL_Vector v;
                            v = rules.GetVector3Item(idx++);

                            if (!av.LegacySitOffsets)
                            {
                                LSL_Vector sitOffset =
                                    llRot2Up(new LSL_Rotation(av.Rotation.X, av.Rotation.Y, av.Rotation.Z,
                                        av.Rotation.W)) * av.Appearance.AvatarHeight * 0.02638f;

                                v = v + 2 * sitOffset;
                            }

                            av.OffsetPosition = new Vector3((float)v.x, (float)v.y, (float)v.z);
                            positionChanged = true;
                        }
                            break;

                        case ScriptBaseClass.PRIM_ROTATION:
                        {
                            if (remain < 1)
                                return new LSL_List();

                            Quaternion r;
                            r = rules.GetQuaternionItem(idx++);

                            av.Rotation = m_host.GetWorldRotation() * r;
                            positionChanged = true;
                        }
                            break;

                        case ScriptBaseClass.PRIM_ROT_LOCAL:
                        {
                            if (remain < 1)
                                return new LSL_List();

                            LSL_Rotation r;
                            r = rules.GetQuaternionItem(idx++);

                            av.Rotation = r;
                            positionChanged = true;
                        }
                            break;

                        // parse rest doing nothing but number of parameters error check
                        case ScriptBaseClass.PRIM_SIZE:
                        case ScriptBaseClass.PRIM_MATERIAL:
                        case ScriptBaseClass.PRIM_PHANTOM:
                        case ScriptBaseClass.PRIM_PHYSICS:
                        case ScriptBaseClass.PRIM_PHYSICS_SHAPE_TYPE:
                        case ScriptBaseClass.PRIM_TEMP_ON_REZ:
                        case ScriptBaseClass.PRIM_NAME:
                        case ScriptBaseClass.PRIM_DESC:
                            if (remain < 1)
                                return new LSL_List();
                            idx++;
                            break;

                        case ScriptBaseClass.PRIM_GLOW:
                        case ScriptBaseClass.PRIM_FULLBRIGHT:
                        case ScriptBaseClass.PRIM_TEXGEN:
                            if (remain < 2)
                                return new LSL_List();
                            idx += 2;
                            break;

                        case ScriptBaseClass.PRIM_TYPE:
                            if (remain < 3)
                                return new LSL_List();
                            code = rules.GetLSLIntegerItem(idx++);
                            remain = rules.Length - idx;
                            switch (code)
                            {
                                case ScriptBaseClass.PRIM_TYPE_BOX:
                                case ScriptBaseClass.PRIM_TYPE_CYLINDER:
                                case ScriptBaseClass.PRIM_TYPE_PRISM:
                                    if (remain < 6)
                                        return new LSL_List();
                                    idx += 6;
                                    break;

                                case ScriptBaseClass.PRIM_TYPE_SPHERE:
                                    if (remain < 5)
                                        return new LSL_List();
                                    idx += 5;
                                    break;

                                case ScriptBaseClass.PRIM_TYPE_TORUS:
                                case ScriptBaseClass.PRIM_TYPE_TUBE:
                                case ScriptBaseClass.PRIM_TYPE_RING:
                                    if (remain < 11)
                                        return new LSL_List();
                                    idx += 11;
                                    break;

                                case ScriptBaseClass.PRIM_TYPE_SCULPT:
                                    if (remain < 2)
                                        return new LSL_List();
                                    idx += 2;
                                    break;
                            }

                            break;

                        case ScriptBaseClass.PRIM_COLOR:
                        case ScriptBaseClass.PRIM_TEXT:
                        case ScriptBaseClass.PRIM_BUMP_SHINY:
                        case ScriptBaseClass.PRIM_OMEGA:
                        case ScriptBaseClass.PRIM_SIT_TARGET:
                            if (remain < 3)
                                return new LSL_List();
                            idx += 3;
                            break;

                        case ScriptBaseClass.PRIM_TEXTURE:
                        case ScriptBaseClass.PRIM_POINT_LIGHT:
                        case ScriptBaseClass.PRIM_PHYSICS_MATERIAL:
                            if (remain < 5)
                                return new LSL_List();
                            idx += 5;
                            break;

                        case ScriptBaseClass.PRIM_FLEXIBLE:
                            if (remain < 7)
                                return new LSL_List();

                            idx += 7;
                            break;

                        case ScriptBaseClass.PRIM_LINK_TARGET:
                            if (remain <
                                3) // setting to 3 on the basis that parsing any usage of PRIM_LINK_TARGET that has nothing following it is pointless.
                                return new LSL_List();

                            return rules.GetSublist(idx, -1);
                    }
                }
            }
            catch (InvalidCastException e)
            {
                Error(originFunc, string.Format(
                    " error running rule #{0}: arg #{1} {2}",
                    rulesParsed, idx - idxStart, e.Message));
            }
            finally
            {
                if (positionChanged)
                    av.SendTerseUpdateToAllClients();
            }

            return new LSL_List();
        }

        public LSL_List GetPrimParams(ScenePresence avatar, LSL_List rules, ref LSL_List res)
        {
            // avatars case
            // replies as SL wiki

//            SceneObjectPart sitPart = avatar.ParentPart; // most likelly it will be needed
            SceneObjectPart
                sitPart = World.GetSceneObjectPart(avatar
                    .ParentID); // maybe better do this expensive search for it in case it's gone??

            var idx = 0;
            while (idx < rules.Length)
            {
                int code = rules.GetLSLIntegerItem(idx++);
                var remain = rules.Length - idx;

                switch (code)
                {
                    case ScriptBaseClass.PRIM_MATERIAL:
                        res.Add(new LSL_Integer((int)SOPMaterialData.SopMaterial.Flesh));
                        break;

                    case ScriptBaseClass.PRIM_PHYSICS:
                        res.Add(new LSL_Integer(0));
                        break;

                    case ScriptBaseClass.PRIM_TEMP_ON_REZ:
                        res.Add(new LSL_Integer(0));
                        break;

                    case ScriptBaseClass.PRIM_PHANTOM:
                        res.Add(new LSL_Integer(0));
                        break;

                    case ScriptBaseClass.PRIM_POSITION:
                        Vector3 pos;

                        if (sitPart.ParentGroup.RootPart != null)
                        {
                            pos = avatar.OffsetPosition;

                            if (!avatar.LegacySitOffsets)
                            {
                                var sitOffset = Zrot(avatar.Rotation) *
                                                (avatar.Appearance.AvatarHeight * 0.02638f * 2.0f);
                                pos -= sitOffset;
                            }

                            var sitroot = sitPart.ParentGroup.RootPart;
                            pos = sitroot.AbsolutePosition + pos * sitroot.GetWorldRotation();
                        }
                        else
                        {
                            pos = avatar.AbsolutePosition;
                        }

                        res.Add(new LSL_Vector(pos.X, pos.Y, pos.Z));
                        break;

                    case ScriptBaseClass.PRIM_SIZE:
                        var s = avatar.Appearance.AvatarSize;
                        res.Add(new LSL_Vector(s.X, s.Y, s.Z));

                        break;

                    case ScriptBaseClass.PRIM_ROTATION:
                        res.Add(new LSL_Rotation(avatar.GetWorldRotation()));
                        break;

                    case ScriptBaseClass.PRIM_TYPE:
                        res.Add(new LSL_Integer(ScriptBaseClass.PRIM_TYPE_BOX));
                        res.Add(new LSL_Integer(ScriptBaseClass.PRIM_HOLE_DEFAULT));
                        res.Add(new LSL_Vector(0f, 1.0f, 0f));
                        res.Add(new LSL_Float(0.0f));
                        res.Add(new LSL_Vector(0, 0, 0));
                        res.Add(new LSL_Vector(1.0f, 1.0f, 0f));
                        res.Add(new LSL_Vector(0, 0, 0));
                        break;

                    case ScriptBaseClass.PRIM_TEXTURE:
                        if (remain < 1)
                            return new LSL_List();

                        int face = rules.GetLSLIntegerItem(idx++);
                        if (face == ScriptBaseClass.ALL_SIDES)
                        {
                            for (face = 0; face < 21; face++)
                            {
                                res.Add(new LSL_String(""));
                                res.Add(new LSL_Vector(0, 0, 0));
                                res.Add(new LSL_Vector(0, 0, 0));
                                res.Add(new LSL_Float(0.0));
                            }
                        }
                        else
                        {
                            if (face >= 0 && face < 21)
                            {
                                res.Add(new LSL_String(""));
                                res.Add(new LSL_Vector(0, 0, 0));
                                res.Add(new LSL_Vector(0, 0, 0));
                                res.Add(new LSL_Float(0.0));
                            }
                        }

                        break;

                    case ScriptBaseClass.PRIM_COLOR:
                        if (remain < 1)
                            return new LSL_List();

                        face = rules.GetLSLIntegerItem(idx++);

                        if (face == ScriptBaseClass.ALL_SIDES)
                        {
                            for (face = 0; face < 21; face++)
                            {
                                res.Add(new LSL_Vector(0, 0, 0));
                                res.Add(new LSL_Float(0));
                            }
                        }
                        else
                        {
                            res.Add(new LSL_Vector(0, 0, 0));
                            res.Add(new LSL_Float(0));
                        }

                        break;

                    case ScriptBaseClass.PRIM_BUMP_SHINY:
                        if (remain < 1)
                            return new LSL_List();
                        face = rules.GetLSLIntegerItem(idx++);

                        if (face == ScriptBaseClass.ALL_SIDES)
                        {
                            for (face = 0; face < 21; face++)
                            {
                                res.Add(new LSL_Integer(ScriptBaseClass.PRIM_SHINY_NONE));
                                res.Add(new LSL_Integer(ScriptBaseClass.PRIM_BUMP_NONE));
                            }
                        }
                        else
                        {
                            res.Add(new LSL_Integer(ScriptBaseClass.PRIM_SHINY_NONE));
                            res.Add(new LSL_Integer(ScriptBaseClass.PRIM_BUMP_NONE));
                        }

                        break;

                    case ScriptBaseClass.PRIM_FULLBRIGHT:
                        if (remain < 1)
                            return new LSL_List();
                        face = rules.GetLSLIntegerItem(idx++);

                        if (face == ScriptBaseClass.ALL_SIDES)
                            for (face = 0; face < 21; face++)
                                res.Add(new LSL_Integer(ScriptBaseClass.FALSE));
                        else
                            res.Add(new LSL_Integer(ScriptBaseClass.FALSE));
                        break;

                    case ScriptBaseClass.PRIM_FLEXIBLE:
                        res.Add(new LSL_Integer(0));
                        res.Add(new LSL_Integer(0)); // softness
                        res.Add(new LSL_Float(0.0f)); // gravity
                        res.Add(new LSL_Float(0.0f)); // friction
                        res.Add(new LSL_Float(0.0f)); // wind
                        res.Add(new LSL_Float(0.0f)); // tension
                        res.Add(new LSL_Vector(0f, 0f, 0f));
                        break;

                    case ScriptBaseClass.PRIM_TEXGEN:
                        // (PRIM_TEXGEN_DEFAULT, PRIM_TEXGEN_PLANAR)
                        if (remain < 1)
                            return new LSL_List();
                        face = rules.GetLSLIntegerItem(idx++);

                        if (face == ScriptBaseClass.ALL_SIDES)
                            for (face = 0; face < 21; face++)
                                res.Add(new LSL_Integer(ScriptBaseClass.PRIM_TEXGEN_DEFAULT));
                        else
                            res.Add(new LSL_Integer(ScriptBaseClass.PRIM_TEXGEN_DEFAULT));
                        break;

                    case ScriptBaseClass.PRIM_POINT_LIGHT:
                        res.Add(new LSL_Integer(0));
                        res.Add(new LSL_Vector(0f, 0f, 0f));
                        res.Add(new LSL_Float(0f)); // intensity
                        res.Add(new LSL_Float(0f)); // radius
                        res.Add(new LSL_Float(0f)); // falloff
                        break;

                    case ScriptBaseClass.PRIM_GLOW:
                        if (remain < 1)
                            return new LSL_List();
                        face = rules.GetLSLIntegerItem(idx++);

                        if (face == ScriptBaseClass.ALL_SIDES)
                            for (face = 0; face < 21; face++)
                                res.Add(new LSL_Float(0f));
                        else
                            res.Add(new LSL_Float(0f));
                        break;

                    case ScriptBaseClass.PRIM_TEXT:
                        res.Add(new LSL_String(""));
                        res.Add(new LSL_Vector(0f, 0f, 0f));
                        res.Add(new LSL_Float(1.0f));
                        break;

                    case ScriptBaseClass.PRIM_NAME:
                        res.Add(new LSL_String(avatar.Name));
                        break;

                    case ScriptBaseClass.PRIM_DESC:
                        res.Add(new LSL_String(""));
                        break;

                    case ScriptBaseClass.PRIM_ROT_LOCAL:
                        var lrot = avatar.Rotation;
                        res.Add(new LSL_Rotation(lrot.X, lrot.Y, lrot.Z, lrot.W));
                        break;

                    case ScriptBaseClass.PRIM_POS_LOCAL:
                        var lpos = avatar.OffsetPosition;

                        if (!avatar.LegacySitOffsets)
                        {
                            var lsitOffset = Zrot(avatar.Rotation) * (avatar.Appearance.AvatarHeight * 0.02638f * 2.0f);
                            lpos -= lsitOffset;
                        }

                        res.Add(new LSL_Vector(lpos.X, lpos.Y, lpos.Z));
                        break;

                    case ScriptBaseClass.PRIM_LINK_TARGET:
                        if (remain < 3) // setting to 3 on the basis that parsing any usage of PRIM_LINK_TARGET that has nothing following it is pointless.
                            return new LSL_List();

                        return rules.GetSublist(idx, -1);
                }
            }

            return new LSL_List();
        }

        private LSL_List JsonParseTop(JsonData elem)
        {
            var retl = new LSL_List();
            if (elem == null)
                retl.Add((LSL_String)ScriptBaseClass.JSON_NULL);

            var elemType = elem.GetJsonType();
            switch (elemType)
            {
                case JsonType.Int:
                    retl.Add(new LSL_Integer((int)elem));
                    return retl;
                case JsonType.Boolean:
                    retl.Add((LSL_String)((bool)elem ? ScriptBaseClass.JSON_TRUE : ScriptBaseClass.JSON_FALSE));
                    return retl;
                case JsonType.Double:
                    retl.Add(new LSL_Float((double)elem));
                    return retl;
                case JsonType.None:
                    retl.Add((LSL_String)ScriptBaseClass.JSON_NULL);
                    return retl;
                case JsonType.String:
                    retl.Add(new LSL_String((string)elem));
                    return retl;
                case JsonType.Array:
                    foreach (JsonData subelem in elem)
                        retl.Add(JsonParseTopNodes(subelem));
                    return retl;
                case JsonType.Object:
                    var e = ((IOrderedDictionary)elem).GetEnumerator();
                    while (e.MoveNext())
                    {
                        retl.Add(new LSL_String((string)e.Key));
                        retl.Add(JsonParseTopNodes((JsonData)e.Value));
                    }

                    return retl;
                default:
                    throw new Exception(ScriptBaseClass.JSON_INVALID);
            }
        }

        private object JsonParseTopNodes(JsonData elem)
        {
            if (elem == null)
                return (LSL_String)ScriptBaseClass.JSON_NULL;

            var elemType = elem.GetJsonType();
            switch (elemType)
            {
                case JsonType.Int:
                    return new LSL_Integer((int)elem);
                case JsonType.Boolean:
                    return (bool)elem ? (LSL_String)ScriptBaseClass.JSON_TRUE : (LSL_String)ScriptBaseClass.JSON_FALSE;
                case JsonType.Double:
                    return new LSL_Float((double)elem);
                case JsonType.None:
                    return (LSL_String)ScriptBaseClass.JSON_NULL;
                case JsonType.String:
                    return new LSL_String((string)elem);
                case JsonType.Array:
                case JsonType.Object:
                    var s = JsonMapper.ToJson(elem);
                    return (LSL_String)s;
                default:
                    throw new Exception(ScriptBaseClass.JSON_INVALID);
            }
        }

        private string ListToJson(object o)
        {
            if (o is LSL_Float || o is double)
            {
                double float_val;
                if (o is double)
                    float_val = (double)o;
                else
                    float_val = ((LSL_Float)o).value;

                if (double.IsInfinity(float_val))
                    return "\"Inf\"";
                if (double.IsNaN(float_val))
                    return "\"NaN\"";

                return ((LSL_Float)float_val).ToString();
            }

            if (o is LSL_Integer || o is int)
            {
                int i;
                if (o is int)
                    i = (int)o;
                else
                    i = ((LSL_Integer)o).value;
                return i.ToString();
            }

            if (o is LSL_Rotation)
            {
                var sb = new StringBuilder(128);
                sb.Append("\"");
                var r = (LSL_Rotation)o;
                sb.Append(r.ToString());
                sb.Append("\"");
                return sb.ToString();
            }

            if (o is LSL_Vector)
            {
                var sb = new StringBuilder(128);
                sb.Append("\"");
                var v = (LSL_Vector)o;
                sb.Append(v.ToString());
                sb.Append("\"");
                return sb.ToString();
            }

            if (o is LSL_String || o is string)
            {
                string str;
                if (o is string)
                    str = (string)o;
                else
                    str = ((LSL_String)o).m_string;

                if (str == ScriptBaseClass.JSON_TRUE || str == "true")
                    return "true";
                if (str == ScriptBaseClass.JSON_FALSE || str == "false")
                    return "false";
                if (str == ScriptBaseClass.JSON_NULL || str == "null")
                    return "null";
                str.Trim();
                if (str.Length == 0)
                    return "\"\"";
                if (str[0] == '{')
                    return str;
                if (str[0] == '[')
                    return str;
                return EscapeForJSON(str, true);
            }

            throw new IndexOutOfRangeException();
        }

        private string EscapeForJSON(string s, bool AddOuter)
        {
            int i;
            char c;
            string t;
            var len = s.Length;

            var sb = new StringBuilder(len + 64);
            if (AddOuter)
                sb.Append("\"");

            for (i = 0; i < len; i++)
            {
                c = s[i];
                switch (c)
                {
                    case '\\':
                    case '"':
                    case '/':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    default:
                        if (c < ' ')
                        {
                            t = "000" + string.Format("{0:X}", c);
                            sb.Append("\\u" + t.Substring(t.Length - 4));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            if (AddOuter)
                sb.Append("\"");
            return sb.ToString();
        }

        private JsonData JsonSetSpecific(JsonData elem, LSL_List specifiers, int level, LSL_String val)
        {
            var spec = specifiers.Data[level];
            if (spec is LSL_String)
                spec = ((LSL_String)spec).m_string;
            else if (spec is LSL_Integer)
                spec = ((LSL_Integer)spec).value;

            if (!(spec is string || spec is int))
                throw new IndexOutOfRangeException();

            var speclen = specifiers.Data.Length - 1;

            var hasvalue = false;
            JsonData value = null;

            var elemType = elem.GetJsonType();
            if (elemType == JsonType.Array)
            {
                if (spec is int)
                {
                    var v = (int)spec;
                    var c = elem.Count;
                    if (v < 0 || (v != 0 && v > c))
                        throw new IndexOutOfRangeException();
                    if (v == c)
                    {
                        elem.Add(JsonBuildRestOfSpec(specifiers, level + 1, val));
                    }
                    else
                    {
                        hasvalue = true;
                        value = elem[v];
                    }
                }
                else if (spec is string)
                {
                    if ((string)spec == ScriptBaseClass.JSON_APPEND)
                    {
                        elem.Add(JsonBuildRestOfSpec(specifiers, level + 1, val));
                    }
                    else if (elem.Count < 2)
                    {
                        // our initial guess of array was wrong
                        var newdata = new JsonData();
                        newdata.SetJsonType(JsonType.Object);
                        IOrderedDictionary no = newdata;
                        no.Add((string)spec, JsonBuildRestOfSpec(specifiers, level + 1, val));
                        return newdata;
                    }
                }
            }
            else if (elemType == JsonType.Object)
            {
                if (spec is string)
                {
                    IOrderedDictionary e = elem;
                    var key = (string)spec;
                    if (e.Contains(key))
                    {
                        hasvalue = true;
                        value = (JsonData)e[key];
                    }
                    else
                    {
                        e.Add(key, JsonBuildRestOfSpec(specifiers, level + 1, val));
                    }
                }
                else if (spec is int && (int)spec == 0)
                {
                    //we are replacing a object by a array
                    var newData = new JsonData();
                    newData.SetJsonType(JsonType.Array);
                    newData.Add(JsonBuildRestOfSpec(specifiers, level + 1, val));
                    return newData;
                }
            }
            else
            {
                var newData = JsonBuildRestOfSpec(specifiers, level, val);
                return newData;
            }

            if (hasvalue)
            {
                if (level < speclen)
                {
                    var replace = JsonSetSpecific(value, specifiers, level + 1, val);
                    if (replace != null)
                    {
                        if (elemType == JsonType.Array)
                        {
                            if (spec is int)
                            {
                                elem[(int)spec] = replace;
                            }
                            else if (spec is string)
                            {
                                var newdata = new JsonData();
                                newdata.SetJsonType(JsonType.Object);
                                IOrderedDictionary no = newdata;
                                no.Add((string)spec, replace);
                                return newdata;
                            }
                        }
                        else if (elemType == JsonType.Object)
                        {
                            if (spec is string)
                            {
                                elem[(string)spec] = replace;
                            }
                            else if (spec is int && (int)spec == 0)
                            {
                                var newdata = new JsonData();
                                newdata.SetJsonType(JsonType.Array);
                                newdata.Add(replace);
                                return newdata;
                            }
                        }
                    }

                    return null;
                }

                if (speclen == level)
                {
                    if (val == ScriptBaseClass.JSON_DELETE)
                    {
                        if (elemType == JsonType.Array)
                        {
                            if (spec is int)
                            {
                                IList el = elem;
                                el.RemoveAt((int)spec);
                            }
                        }
                        else if (elemType == JsonType.Object)
                        {
                            if (spec is string)
                            {
                                IOrderedDictionary eo = elem;
                                eo.Remove((string)spec);
                            }
                        }

                        return null;
                    }

                    JsonData newval = null;
                    if (val == null || val == ScriptBaseClass.JSON_NULL || val == "null")
                    {
                        newval = null;
                    }
                    else if (val == ScriptBaseClass.JSON_TRUE || val == "true")
                    {
                        newval = new JsonData(true);
                    }
                    else if (val == ScriptBaseClass.JSON_FALSE || val == "false")
                    {
                        newval = new JsonData(false);
                    }
                    else if (float.TryParse(val, out var num))
                    {
                        // assuming we are at en.us already
                        if (num - (int)num == 0.0f && !val.Contains("."))
                        {
                            newval = new JsonData((int)num);
                        }
                        else
                        {
                            num = (float)Math.Round(num, 6);
                            newval = new JsonData(num);
                        }
                    }
                    else
                    {
                        var str = val.m_string;
                        newval = new JsonData(str);
                    }

                    if (elemType == JsonType.Array)
                    {
                        if (spec is int)
                        {
                            elem[(int)spec] = newval;
                        }
                        else if (spec is string)
                        {
                            var newdata = new JsonData();
                            newdata.SetJsonType(JsonType.Object);
                            IOrderedDictionary no = newdata;
                            no.Add((string)spec, newval);
                            return newdata;
                        }
                    }
                    else if (elemType == JsonType.Object)
                    {
                        if (spec is string)
                        {
                            elem[(string)spec] = newval;
                        }
                        else if (spec is int && (int)spec == 0)
                        {
                            var newdata = new JsonData();
                            newdata.SetJsonType(JsonType.Array);
                            newdata.Add(newval);
                            return newdata;
                        }
                    }
                }
            }

            if (val == ScriptBaseClass.JSON_DELETE)
                throw new IndexOutOfRangeException();
            return null;
        }

        private JsonData JsonBuildRestOfSpec(LSL_List specifiers, int level, LSL_String val)
        {
            var spec = level >= specifiers.Data.Length ? null : specifiers.Data[level];
            // 20131224 not used            object specNext = i+1 >= specifiers.Data.Length ? null : specifiers.Data[i+1];

            if (spec == null)
            {
                if (val == null || val == ScriptBaseClass.JSON_NULL || val == "null")
                    return null;
                if (val == ScriptBaseClass.JSON_DELETE)
                    throw new IndexOutOfRangeException();
                if (val == ScriptBaseClass.JSON_TRUE || val == "true")
                    return new JsonData(true);
                if (val == ScriptBaseClass.JSON_FALSE || val == "false")
                    return new JsonData(false);
                if (val == null || val == ScriptBaseClass.JSON_NULL || val == "null")
                    return null;
                if (float.TryParse(val, out var num))
                {
                    // assuming we are at en.us already
                    if (num - (int)num == 0.0f && !val.Contains(".")) return new JsonData((int)num);

                    num = (float)Math.Round(num, 6);
                    return new JsonData(num);
                }

                var str = val.m_string;
                return new JsonData(str);
                throw new IndexOutOfRangeException();
            }

            if (spec is LSL_String)
                spec = ((LSL_String)spec).m_string;
            else if (spec is LSL_Integer)
                spec = ((LSL_Integer)spec).value;

            if (spec is int ||
                (spec is string && (string)spec == ScriptBaseClass.JSON_APPEND))
            {
                if (spec is int && (int)spec != 0)
                    throw new IndexOutOfRangeException();
                var newdata = new JsonData();
                newdata.SetJsonType(JsonType.Array);
                newdata.Add(JsonBuildRestOfSpec(specifiers, level + 1, val));
                return newdata;
            }

            if (spec is string)
            {
                var newdata = new JsonData();
                newdata.SetJsonType(JsonType.Object);
                IOrderedDictionary no = newdata;
                no.Add((string)spec, JsonBuildRestOfSpec(specifiers, level + 1, val));
                return newdata;
            }

            throw new IndexOutOfRangeException();
        }

        private bool JsonFind(JsonData elem, LSL_List specifiers, int level, out JsonData value)
        {
            value = null;
            if (elem == null)
                return false;

            object spec;
            spec = specifiers.Data[level];

            var haveVal = false;
            JsonData next = null;

            if (elem.GetJsonType() == JsonType.Array)
            {
                if (spec is LSL_Integer)
                {
                    int indx = (LSL_Integer)spec;
                    if (indx >= 0 && indx < elem.Count)
                    {
                        haveVal = true;
                        next = elem[indx];
                    }
                }
            }
            else if (elem.GetJsonType() == JsonType.Object)
            {
                if (spec is LSL_String)
                {
                    IOrderedDictionary e = elem;
                    string key = (LSL_String)spec;
                    if (e.Contains(key))
                    {
                        haveVal = true;
                        next = (JsonData)e[key];
                    }
                }
            }

            if (haveVal)
            {
                if (level == specifiers.Data.Length - 1)
                {
                    value = next;
                    return true;
                }

                level++;
                if (next == null)
                    return false;

                var nextType = next.GetJsonType();
                if (nextType != JsonType.Object && nextType != JsonType.Array)
                    return false;

                return JsonFind(next, specifiers, level, out value);
            }

            return false;
        }

        private LSL_String JsonElementToString(JsonData elem)
        {
            if (elem == null)
                return ScriptBaseClass.JSON_NULL;

            var elemType = elem.GetJsonType();
            switch (elemType)
            {
                case JsonType.Array:
                    return new LSL_String(JsonMapper.ToJson(elem));
                case JsonType.Boolean:
                    return new LSL_String((bool)elem ? ScriptBaseClass.JSON_TRUE : ScriptBaseClass.JSON_FALSE);
                case JsonType.Double:
                    var d = (double)elem;
                    var sd = string.Format(Culture.FormatProvider, "{0:0.0#####}", d);
                    return new LSL_String(sd);
                case JsonType.Int:
                    var i = (int)elem;
                    return new LSL_String(i.ToString());
                case JsonType.Long:
                    var l = (long)elem;
                    return new LSL_String(l.ToString());
                case JsonType.Object:
                    return new LSL_String(JsonMapper.ToJson(elem));
                case JsonType.String:
                    var s = (string)elem;
                    return new LSL_String(s);
                case JsonType.None:
                    return ScriptBaseClass.JSON_NULL;
                default:
                    return ScriptBaseClass.JSON_INVALID;
            }
        }

        private struct Tri
        {
            public Vector3 p1;
            public Vector3 p2;
            public Vector3 p3;
        }

        /// <summary>
        ///     Struct for transmitting parameters required for finding llCastRay ray hits.
        /// </summary>
        public struct RayTrans
        {
            public UUID PartId;
            public UUID GroupId;
            public int Link;
            public Vector3 ScalePart;
            public Vector3 PositionPart;
            public Quaternion RotationPart;
            public bool ShapeNeedsEnds;
            public Vector3 Position1Ray;
            public Vector3 Position1RayProj;
            public Vector3 VectorRayProj;
        }

        /// <summary>
        ///     Struct for llCastRay ray hits.
        /// </summary>
        public struct RayHit
        {
            public UUID PartId;
            public UUID GroupId;
            public int Link;
            public Vector3 Position;
            public Vector3 Normal;
            public float Distance;
        }

        /// <summary>
        ///     Struct for llCastRay throttle data.
        /// </summary>
        public struct CastRayCall
        {
            public UUID RegionId;
            public UUID UserId;
            public int CalledMs;
            public int UsedMs;
        }

        #region Not Implemented

        //
        // Listing the unimplemented lsl functions here, please move
        // them from this region as they are completed
        //
        public void llCollisionSprite(LSL_String impact_sprite)
        {
            // Viewer 2.0 broke this and it's likely LL has no intention
            // of fixing it. Therefore, letting this be a NOP seems appropriate.
        }

        public void llGodLikeRezObject(string inventory, LSL_Vector pos)
        {
            if (!World.Permissions.IsGod(m_host.OwnerID))
                NotImplemented("llGodLikeRezObject");

            AssetBase rezAsset = World.AssetService.Get(inventory);
            if (rezAsset == null)
            {
                llSay(0, "Asset not found");
                return;
            }

            SceneObjectGroup group = null;

            try
            {
                var xmlData = Utils.BytesToString(rezAsset.Data);
                group = SceneObjectSerializer.FromOriginalXmlFormat(xmlData);
            }
            catch
            {
                llSay(0, "Asset not found");
                return;
            }

            if (group == null)
            {
                llSay(0, "Asset not found");
                return;
            }

            group.RootPart.AttachedPos = group.AbsolutePosition;

            group.ResetIDs();

            var llpos = new Vector3((float)pos.x, (float)pos.y, (float)pos.z);
            World.AddNewSceneObject(group, true, llpos, Quaternion.Identity, Vector3.Zero);
            group.CreateScriptInstances(0, true, World.DefaultScriptEngine, 3);
            group.ScheduleGroupForFullUpdate();

            // objects rezzed with this method are die_at_edge by default.
            group.RootPart.SetDieAtEdge(true);

            group.ResumeScripts();

            m_ScriptEngine.PostObjectEvent(m_host.LocalId, new EventParams(
                "object_rez", new object[]
                {
                    new LSL_String(
                        group.RootPart.UUID.ToString())
                },
                new DetectParams[0]));
        }

        public LSL_Key llTransferLindenDollars(LSL_Key destination, LSL_Integer amount)
        {
            IMoneyModule money = World.RequestModuleInterface<IMoneyModule>();
            var txn = UUID.Random();
            var toID = UUID.Zero;

            var replydata = "UnKnownError";
            var bad = true;
            while (true)
            {
                if (amount <= 0)
                {
                    replydata = "INVALID_AMOUNT";
                    break;
                }

                if (money == null)
                {
                    replydata = "TRANSFERS_DISABLED";
                    break;
                }

                if (m_host.OwnerID.Equals(m_host.GroupID))
                {
                    replydata = "GROUP_OWNED";
                    break;
                }

                if (m_item == null)
                {
                    replydata = "SERVICE_ERROR";
                    break;
                }

                if (m_item.PermsGranter.IsZero())
                {
                    replydata = "MISSING_PERMISSION_DEBIT";
                    break;
                }

                if ((m_item.PermsMask & ScriptBaseClass.PERMISSION_DEBIT) == 0)
                {
                    replydata = "MISSING_PERMISSION_DEBIT";
                    break;
                }

                if (!UUID.TryParse(destination, out toID))
                {
                    replydata = "INVALID_AGENT";
                    break;
                }

                bad = false;
                break;
            }

            if (bad)
            {
                m_ScriptEngine.PostScriptEvent(m_item.ItemID, new EventParams(
                    "transaction_result", new object[]
                    {
                        new LSL_String(txn.ToString()),
                        new LSL_Integer(0),
                        new LSL_String(replydata)
                    },
                    new DetectParams[0]));
                return txn.ToString();
            }

            //fire and forget...
            Action<string> act = eventID =>
            {
                var replycode = 0;
                try
                {
                    UserAccount account = m_userAccountService.GetUserAccount(RegionScopeID, toID);
                    if (account == null)
                    {
                        replydata = "LINDENDOLLAR_ENTITYDOESNOTEXIST";
                        return;
                    }

                    var result = money.ObjectGiveMoney(m_host.ParentGroup.RootPart.UUID,
                        m_host.ParentGroup.RootPart.OwnerID,
                        toID, amount, txn, out var reason);
                    if (result)
                    {
                        replycode = 1;
                        replydata = destination + "," + amount.ToString();
                        return;
                    }

                    replydata = reason;
                }
                finally
                {
                    m_ScriptEngine.PostScriptEvent(m_item.ItemID, new EventParams(
                        "transaction_result", new object[]
                        {
                            new LSL_String(txn.ToString()),
                            new LSL_Integer(replycode),
                            new LSL_String(replydata)
                        },
                        new DetectParams[0]));
                }
            };

            m_AsyncCommands.DataserverPlugin.RegisterRequest(m_host.LocalId, m_item.ItemID, act);
            return txn.ToString();
        }

        #endregion
    }

    public class NotecardCache
    {
        private static readonly ExpiringCacheOS<UUID, string[]>
            m_Notecards = new ExpiringCacheOS<UUID, string[]>(30000);

        public static void Cache(UUID assetID, byte[] text)
        {
            if (m_Notecards.ContainsKey(assetID, 30000))
                return;

            m_Notecards.AddOrUpdate(assetID, SLUtil.ParseNotecardToArray(text), 30);
        }

        public static bool IsCached(UUID assetID)
        {
            return m_Notecards.ContainsKey(assetID, 30000);
        }

        public static int GetLines(UUID assetID)
        {
            if (m_Notecards.TryGetValue(assetID, 30000, out var text))
                return text.Length;
            return -1;
        }

        /// <summary>
        ///     Get a notecard line.
        /// </summary>
        /// <param name="assetID"></param>
        /// <param name="lineNumber">Lines start at index 0</param>
        /// <returns></returns>
        public static string GetLine(UUID assetID, int lineNumber)
        {
            if (lineNumber >= 0 && m_Notecards.TryGetValue(assetID, 30000, out var text))
            {
                if (lineNumber >= text.Length)
                    return "\n\n\n";
                return text[lineNumber];
            }

            return "";
        }

        /// <summary>
        ///     Get a notecard line.
        /// </summary>
        /// <param name="assetID"></param>
        /// <param name="lineNumber">Lines start at index 0</param>
        /// <param name="maxLength">
        ///     Maximum length of the returned line.
        /// </param>
        /// <returns>
        ///     If the line length is longer than <paramref name="maxLength" />,
        ///     the return string will be truncated.
        /// </returns>
        public static string GetLine(UUID assetID, int lineNumber, int maxLength)
        {
            var line = GetLine(assetID, lineNumber);

            if (line.Length > maxLength)
                return line.Substring(0, maxLength);

            return line;
        }
    }
}
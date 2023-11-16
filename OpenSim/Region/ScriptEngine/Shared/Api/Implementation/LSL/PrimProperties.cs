using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api: MarshalByRefObject, ILSL_Api, IScriptApi
    {
        
        public LSL_Integer llScaleByFactor(double scaling_factor)
        {
            SceneObjectGroup group = m_host.ParentGroup;

            if(scaling_factor < 1e-6)
                return ScriptBaseClass.FALSE;
            if(scaling_factor > 1e6)
                return ScriptBaseClass.FALSE;

            if (group == null || group.IsDeleted || group.inTransit)
                return ScriptBaseClass.FALSE;

            if (group.RootPart.PhysActor != null && group.RootPart.PhysActor.IsPhysical)
                return ScriptBaseClass.FALSE;

            if (group.RootPart.KeyframeMotion != null)
                return ScriptBaseClass.FALSE;

            if(group.GroupResize(scaling_factor))
                return ScriptBaseClass.TRUE;
            else
                return ScriptBaseClass.FALSE;
        }

        public LSL_Float llGetMaxScaleFactor()
        {
            SceneObjectGroup group = m_host.ParentGroup;

            if (group == null || group.IsDeleted || group.inTransit)
                return 1.0f;

            return (LSL_Float)group.GetMaxGroupResizeScale();
        }

        public LSL_Float llGetMinScaleFactor()
        {
            SceneObjectGroup group = m_host.ParentGroup;

            if (group == null || group.IsDeleted || group.inTransit)
                return 1.0f;

            return (LSL_Float)group.GetMinGroupResizeScale();
        }
        
        

        public LSL_Types.LSLFloat llGetAlpha(int face)
        {

            return GetAlpha(m_host, face);
        }

        public void llSetAlpha(double alpha, int face)
        {

            SetAlpha(m_host, alpha, face);
        }

        public void llSetLinkAlpha(int linknumber, double alpha, int face)
        {

            List<SceneObjectPart> parts = GetLinkParts(linknumber);
            if (parts.Count > 0)
            {
                try
                {
                    foreach (SceneObjectPart part in parts)
                        SetAlpha(part, alpha, face);
                }
                finally { }
            }
        }

        public LSL_Types.Vector3 llGetColor(int face)
        {
            return GetColor(m_host, face);
        }
        
        
        public void llSetStatus(int status, int value)
        {
            if (m_host == null || m_host.ParentGroup == null || m_host.ParentGroup.IsDeleted)
                return;

            int statusrotationaxis = 0;

            if ((status & ScriptBaseClass.STATUS_PHYSICS) == ScriptBaseClass.STATUS_PHYSICS)
            {
                if (value != 0)
                {
                    SceneObjectGroup group = m_host.ParentGroup;
                    bool allow = true;

                    int maxprims = World.m_linksetPhysCapacity;
                    bool checkShape = (maxprims > 0 && group.PrimCount > maxprims);

                    foreach (SceneObjectPart part in group.Parts)
                    {
                        if (part.PhysicsShapeType == (byte)PhysicsShapeType.None)
                            continue;

                        if (part.Scale.X > World.m_maxPhys || part.Scale.Y > World.m_maxPhys || part.Scale.Z > World.m_maxPhys)
                        {
                            allow = false;
                            break;
                        }
                        if (checkShape)
                        {
                            if (--maxprims < 0)
                            {
                                allow = false;
                                break;
                            }
                        }
                    }

                    if (!allow)
                        return;

                    if (m_host.ParentGroup.RootPart.PhysActor != null &&
                        m_host.ParentGroup.RootPart.PhysActor.IsPhysical)
                        return;

                    m_host.ScriptSetPhysicsStatus(true);
                }
                else
                {
                    m_host.ScriptSetPhysicsStatus(false);
                }
            }

            if ((status & ScriptBaseClass.STATUS_PHANTOM) == ScriptBaseClass.STATUS_PHANTOM)
            {
                m_host.ParentGroup.ScriptSetPhantomStatus(value != 0);
            }

            if ((status & ScriptBaseClass.STATUS_CAST_SHADOWS) == ScriptBaseClass.STATUS_CAST_SHADOWS)
            {
                m_host.AddFlag(PrimFlags.CastShadows);
            }

            if ((status & ScriptBaseClass.STATUS_ROTATE_X) == ScriptBaseClass.STATUS_ROTATE_X)
            {
                statusrotationaxis |= ScriptBaseClass.STATUS_ROTATE_X;
            }

            if ((status & ScriptBaseClass.STATUS_ROTATE_Y) == ScriptBaseClass.STATUS_ROTATE_Y)
            {
                statusrotationaxis |= ScriptBaseClass.STATUS_ROTATE_Y;
            }

            if ((status & ScriptBaseClass.STATUS_ROTATE_Z) == ScriptBaseClass.STATUS_ROTATE_Z)
            {
                statusrotationaxis |= ScriptBaseClass.STATUS_ROTATE_Z;
            }

            if ((status & ScriptBaseClass.STATUS_BLOCK_GRAB) == ScriptBaseClass.STATUS_BLOCK_GRAB)
                m_host.BlockGrab = value != 0;

            if ((status & ScriptBaseClass.STATUS_BLOCK_GRAB_OBJECT) == ScriptBaseClass.STATUS_BLOCK_GRAB_OBJECT)
                m_host.ParentGroup.BlockGrabOverride = value != 0;

            if ((status & ScriptBaseClass.STATUS_DIE_AT_EDGE) == ScriptBaseClass.STATUS_DIE_AT_EDGE)
            {
                if (value != 0)
                    m_host.SetDieAtEdge(true);
                else
                    m_host.SetDieAtEdge(false);
            }

            if ((status & ScriptBaseClass.STATUS_RETURN_AT_EDGE) == ScriptBaseClass.STATUS_RETURN_AT_EDGE)
            {
                if (value != 0)
                    m_host.SetReturnAtEdge(true);
                else
                    m_host.SetReturnAtEdge(false);
            }

            if ((status & ScriptBaseClass.STATUS_SANDBOX) == ScriptBaseClass.STATUS_SANDBOX)
            {
                if (value != 0)
                    m_host.SetStatusSandbox(true);
                else
                    m_host.SetStatusSandbox(false);
            }

            if (statusrotationaxis != 0)
            {
                m_host.SetAxisRotation(statusrotationaxis, value);
            }
        }
        
        
        public LSL_Integer llGetStatus(int status)
        {
            // m_log.Debug(m_host.ToString() + " status is " + m_host.GetEffectiveObjectFlags().ToString());
            switch (status)
            {
                case ScriptBaseClass.STATUS_PHYSICS:
                    return IsPhysical() ? 1 : 0;

                case ScriptBaseClass.STATUS_PHANTOM:
                    if ((m_host.GetEffectiveObjectFlags() & (uint)PrimFlags.Phantom) == (uint)PrimFlags.Phantom)
                    {
                        return 1;
                    }
                    return 0;

                case ScriptBaseClass.STATUS_CAST_SHADOWS:
                    if ((m_host.GetEffectiveObjectFlags() & (uint)PrimFlags.CastShadows) == (uint)PrimFlags.CastShadows)
                    {
                        return 1;
                    }
                    return 0;

                case ScriptBaseClass.STATUS_BLOCK_GRAB:
                    return m_host.BlockGrab ? 1 : 0;

                case ScriptBaseClass.STATUS_BLOCK_GRAB_OBJECT:
                    return m_host.ParentGroup.BlockGrabOverride ? 1 : 0;

                case ScriptBaseClass.STATUS_DIE_AT_EDGE:
                    if (m_host.GetDieAtEdge())
                        return 1;
                    else
                        return 0;

                case ScriptBaseClass.STATUS_RETURN_AT_EDGE:
                    if (m_host.GetReturnAtEdge())
                        return 1;
                    else
                        return 0;

                case ScriptBaseClass.STATUS_ROTATE_X:
                    // if (m_host.GetAxisRotation(2) != 0)
                    if (m_host.GetAxisRotation((int)SceneObjectGroup.axisSelect.STATUS_ROTATE_X) != 0)
                        return 1;
                    else
                        return 0;

                case ScriptBaseClass.STATUS_ROTATE_Y:
                    if (m_host.GetAxisRotation((int)SceneObjectGroup.axisSelect.STATUS_ROTATE_Y) != 0)
                        return 1;
                    else
                        return 0;

                case ScriptBaseClass.STATUS_ROTATE_Z:
                    if (m_host.GetAxisRotation((int)SceneObjectGroup.axisSelect.STATUS_ROTATE_Z) != 0)
                        return 1;
                    else
                        return 0;

                case ScriptBaseClass.STATUS_SANDBOX:
                    if (m_host.GetStatusSandbox())
                        return 1;
                    else
                        return 0;
            }
            return 0;
        }

        [DebuggerNonUserCode]
        public virtual void llDie()
        {
            if (!m_host.ParentGroup.IsAttachment) throw new SelfDeleteException();
        }



        public void llSetScale(LSL_Types.Vector3 scale)
        {
            SetScale(m_host, scale);
        }

        public LSL_Types.Vector3 llGetScale()
        {
            return new LSL_Types.Vector3(m_host.Scale.X, m_host.Scale.Y, m_host.Scale.Z);
        }

        public void llSetClickAction(int action)
        {
            m_host.ClickAction = (byte)action;
            m_host.ParentGroup.HasGroupChanged = true;
            m_host.ScheduleFullUpdate();
            return;
        }

        public void llSetColor(LSL_Types.Vector3 color, int face)
        {

            SetColor(m_host, color, face);
        }


        public void llSetTexture(string texture, int face)
        {
            SetTexture(m_host, texture, face);
            ScriptSleep(m_sleepMsOnSetTexture);
        }

        public void llSetLinkTexture(int linknumber, string texture, int face)
        {

            List<SceneObjectPart> parts = GetLinkParts(linknumber);
            if (parts.Count > 0)
            {
                try
                {
                    foreach (SceneObjectPart part in parts)
                        SetTexture(part, texture, face);
                }
                finally { }
            }
            ScriptSleep(m_sleepMsOnSetLinkTexture);
        }


        public void llScaleTexture(double u, double v, int face)
        {

            ScaleTexture(m_host, u, v, face);
            ScriptSleep(m_sleepMsOnScaleTexture);
        }

        public void llOffsetTexture(double u, double v, int face)
        {
            OffsetTexture(m_host, u, v, face);
            ScriptSleep(m_sleepMsOnOffsetTexture);
        }

        public void llRotateTexture(double rotation, int face)
        {
            RotateTexture(m_host, rotation, face);
            ScriptSleep(m_sleepMsOnRotateTexture);
        }

        public LSL_String llGetTexture(int face)
        {
            return GetTexture(m_host, face);
        }

        public void llSetPos(LSL_Vector pos)
        {

            SetPos(m_host, pos, true);

            ScriptSleep(m_sleepMsOnSetPos);
        }

        /// <summary>
        /// Tries to move the entire object so that the root prim is within 0.1m of position. http://wiki.secondlife.com/wiki/LlSetRegionPos
        /// Documentation indicates that the use of x/y coordinates up to 10 meters outside the bounds of a region will work but do not specify what happens if there is no adjacent region for the object to move into.
        /// Uses the RegionSize constant here rather than hard-coding 266.0 to alert any developer modifying OpenSim to support variable-sized regions that this method will need tweaking.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>1 if successful, 0 otherwise.</returns>
        public LSL_Integer llSetRegionPos(LSL_Vector pos)
        {

            // BEGIN WORKAROUND
            // IF YOU GET REGION CROSSINGS WORKING WITH THIS FUNCTION, REPLACE THE WORKAROUND.
            //
            // This workaround is to prevent silent failure of this function.
            // According to the specification on the SL Wiki, providing a position outside of the
            if (pos.x < 0 || pos.x > World.RegionInfo.RegionSizeX || pos.y < 0 || pos.y > World.RegionInfo.RegionSizeY)
            {
                return 0;
            }
            // END WORK AROUND
            else if ( // this is not part of the workaround if-block because it's not related to the workaround.
                IsPhysical() ||
                m_host.ParentGroup.IsAttachment || // return FALSE if attachment
                (
                    pos.x < -10.0 || // return FALSE if more than 10 meters into a west-adjacent region.
                    pos.x > (World.RegionInfo.RegionSizeX + 10) || // return FALSE if more than 10 meters into a east-adjacent region.
                    pos.y < -10.0 || // return FALSE if more than 10 meters into a south-adjacent region.
                    pos.y > (World.RegionInfo.RegionSizeY + 10) || // return FALSE if more than 10 meters into a north-adjacent region.
                    pos.z > Constants.RegionHeight // return FALSE if altitude than 4096m
                )
            )
            {
                return 0;
            }

            // if we reach this point, then the object is not physical, it's not an attachment, and the destination is within the valid range.
            // this could possibly be done in the above else-if block, but we're doing the check here to keep the code easier to read.

            Vector3 objectPos = m_host.ParentGroup.RootPart.AbsolutePosition;
            LandData here = World.GetLandData(objectPos);
            LandData there = World.GetLandData(pos);

            // we're only checking prim limits if it's moving to a different parcel under the assumption that if the object got onto the parcel without exceeding the prim limits.

            bool sameParcel = here.GlobalID.Equals(there.GlobalID);

            if (!sameParcel && !World.Permissions.CanRezObject(
                m_host.ParentGroup.PrimCount, m_host.ParentGroup.OwnerID, pos))
            {
                return 0;
            }

            SetPos(m_host.ParentGroup.RootPart, pos, false);

            return VecDistSquare(pos, llGetRootPosition()) <= 0.01 ? 1 : 0;
        }

        public LSL_Vector llGetPos()
        {
            return m_host.GetWorldPosition();
        }

        public LSL_Vector llGetLocalPos()
        {
            return GetPartLocalPos(m_host);
        }

        public void llSetRot(LSL_Rotation rot)
        {

            // try to let this work as in SL...
            if (m_host.ParentID == 0 || (m_host.ParentGroup != null && m_host == m_host.ParentGroup.RootPart))
            {
                // special case: If we are root, rotate complete SOG to new rotation
                SetRot(m_host, rot);
            }
            else
            {
                // we are a child. The rotation values will be set to the one of root modified by rot, as in SL. Don't ask.
                SceneObjectPart rootPart = m_host.ParentGroup.RootPart;
                if (rootPart != null) // better safe than sorry
                {
                    SetRot(m_host, rootPart.RotationOffset * (Quaternion)rot);
                }
            }

            ScriptSleep(m_sleepMsOnSetRot);
        }

        public void llSetLocalRot(LSL_Rotation rot)
        {
            SetRot(m_host, rot);
            ScriptSleep(m_sleepMsOnSetLocalRot);
        }

        /// <summary>
        /// See http://lslwiki.net/lslwiki/wakka.php?wakka=ChildRotation
        /// </summary>
        public LSL_Rotation llGetRot()
        {
            // unlinked or root prim then use llRootRotation
            // see llRootRotaion for references.
            if (m_host.LinkNum == 0 || m_host.LinkNum == 1)
            {
                return llGetRootRotation();
            }

            Quaternion q = m_host.GetWorldRotation();

            if (m_host.ParentGroup != null && m_host.ParentGroup.AttachmentPoint != 0)
            {
                ScenePresence avatar = World.GetScenePresence(m_host.ParentGroup.AttachedAvatar);
                if (avatar != null)
                {
                    if ((avatar.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) != 0)
                        q = avatar.CameraRotation * q; // Mouselook
                    else
                        q = avatar.Rotation * q; // Currently infrequently updated so may be inaccurate
                }
            }

            return new LSL_Rotation(q.X, q.Y, q.Z, q.W);
        }
        
        public LSL_Rotation llGetLocalRot()
        {
            return GetPartLocalRot(m_host);
        }

        public void llSetForce(LSL_Vector force, int local)
        {

            if (!m_host.ParentGroup.IsDeleted)
            {
                if (local != 0)
                    force *= llGetRot();

                m_host.ParentGroup.RootPart.SetForce(force);
            }
        }

        public LSL_Vector llGetForce()
        {
            LSL_Vector force = new LSL_Vector(0.0, 0.0, 0.0);


            if (!m_host.ParentGroup.IsDeleted)
            {
                force = m_host.ParentGroup.RootPart.GetForce();
            }

            return force;
        }

        public void llSetVelocity(LSL_Vector vel, int local)
        {
            m_host.SetVelocity(new Vector3((float)vel.x, (float)vel.y, (float)vel.z), local != 0);
        }

        public void llSetAngularVelocity(LSL_Vector avel, int local)
        {
            m_host.SetAngularVelocity(new Vector3((float)avel.x, (float)avel.y, (float)avel.z), local != 0);
        }
        public LSL_Integer llTarget(LSL_Vector position, double range)
        {
            return m_host.ParentGroup.RegisterTargetWaypoint(m_item.ItemID, position, (float)range);
        }

        public void llTargetRemove(int number)
        {
            m_host.ParentGroup.UnregisterTargetWaypoint(number);
        }

        public LSL_Integer llRotTarget(LSL_Rotation rot, double error)
        {
            return m_host.ParentGroup.RegisterRotTargetWaypoint(m_item.ItemID, rot, (float)error);
        }

        public void llRotTargetRemove(int number)
        {
            m_host.ParentGroup.UnRegisterRotTargetWaypoint(number);
        }

        public void llMoveToTarget(LSL_Vector target, double tau)
        {
            m_host.ParentGroup.MoveToTarget(target, (float)tau);
        }

        public void llStopMoveToTarget()
        {
            m_host.ParentGroup.StopMoveToTarget();
        }
        
        public LSL_Key llGetOwner()
        {

            return m_host.OwnerID.ToString();
        }

    }
}
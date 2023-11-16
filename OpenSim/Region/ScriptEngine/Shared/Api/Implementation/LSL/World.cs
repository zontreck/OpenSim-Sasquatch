using System;
using System.Globalization;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Animation;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api : MarshalByRefObject, ILSL_Api, IScriptApi
    {
        public void llStopLookAt()
        {
            m_host.StopLookAt();
        }


        public LSL_Types.LSLInteger llGetRegionAgentCount()
        {
            var count = 0;
            World.ForEachRootScenePresence(delegate { count++; });

            return new LSL_Types.LSLInteger(count);
        }

        public LSL_Types.Vector3 llGetRegionCorner()
        {
            return new LSL_Types.Vector3(World.RegionInfo.WorldLocX, World.RegionInfo.WorldLocY, 0);
        }

        public LSL_Types.LSLString llGetEnv(LSL_Types.LSLString name)
        {
            string sname = name;
            sname = sname.ToLower();
            switch (sname)
            {
                case "agent_limit":
                    return World.RegionInfo.RegionSettings.AgentLimit.ToString();

                case "dynamic_pathfinding":
                    return "0";

                case "estate_id":
                    return World.RegionInfo.EstateSettings.EstateID.ToString();

                case "estate_name":
                    return World.RegionInfo.EstateSettings.EstateName;

                case "frame_number":
                    return World.Frame.ToString();

                case "region_cpu_ratio":
                    return "1";

                case "region_idle":
                    return "0";

                case "region_product_name":
                    if (World.RegionInfo.RegionType != string.Empty)
                        return World.RegionInfo.RegionType;
                    return "";

                case "region_product_sku":
                    return "OpenSim";

                case "region_start_time":
                    return World.UnixStartTime.ToString();

                case "region_up_time":
                    var time = Util.UnixTimeSinceEpoch() - World.UnixStartTime;
                    return time.ToString();

                case "sim_channel":
                    return "OpenSim";

                case "sim_version":
                    return World.GetSimulatorVersion();

                case "simulator_hostname":
                    var UrlModule = World.RequestModuleInterface<IUrlModule>();
                    return UrlModule.ExternalHostNameForLSL;

                case "region_max_prims":
                    return World.RegionInfo.ObjectCapacity.ToString();

                case "region_object_bonus":
                    return World.RegionInfo.RegionSettings.ObjectBonus.ToString();

                case "whisper_range":
                    return m_whisperdistance.ToString();

                case "chat_range":
                    return m_saydistance.ToString();

                case "shout_range":
                    return m_shoutdistance.ToString();

                default:
                    return "";
            }
        }

        public LSL_Types.LSLInteger llOverMyLand(string id)
        {
            if (UUID.TryParse(id, out var key) && key.IsNotZero())
                try
                {
                    var presence = World.GetScenePresence(key);
                    if (presence != null) // object is an avatar
                    {
                        if (m_host.OwnerID.Equals(World.LandChannel.GetLandObject(presence.AbsolutePosition).LandData
                                .OwnerID))
                            return 1;
                    }
                    else // object is not an avatar
                    {
                        var obj = World.GetSceneObjectPart(key);

                        if (obj != null &&
                            m_host.OwnerID.Equals(
                                World.LandChannel.GetLandObject(obj.AbsolutePosition).LandData.OwnerID))
                            return 1;
                    }
                }
                catch
                {
                }

            return 0;
        }

        public LSL_Types.LSLString llGetLandOwnerAt(LSL_Types.Vector3 pos)
        {
            var land = World.LandChannel.GetLandObject((float)pos.x, (float)pos.y);
            if (land == null)
                return ScriptBaseClass.NULL_KEY;
            return land.LandData.OwnerID.ToString();
        }

        /// <summary>
        ///     According to http://lslwiki.net/lslwiki/wakka.php?wakka=llGetAgentSize
        ///     only the height of avatars vary and that says:
        ///     Width (x) and depth (y) are constant. (0.45m and 0.6m respectively).
        /// </summary>
        public LSL_Types.Vector3 llGetAgentSize(LSL_Types.LSLString id)
        {
            if (!UUID.TryParse(id, out var avID) || avID.IsZero())
                return ScriptBaseClass.ZERO_VECTOR;

            var avatar = World.GetScenePresence(avID);
            if (avatar == null || avatar.IsChildAgent) // Fail if not in the same region
                return ScriptBaseClass.ZERO_VECTOR;

            return new LSL_Types.Vector3(avatar.Appearance.AvatarSize);
        }

        public LSL_Types.LSLInteger llSameGroup(string id)
        {
            if (!UUID.TryParse(id, out var uuid) || uuid.IsZero())
                return 0;

            // Check if it's a group key
            if (uuid.Equals(m_host.ParentGroup.RootPart.GroupID))
                return 1;

            // Handle object case
            var part = World.GetSceneObjectPart(uuid);
            if (part != null)
            {
                if (part.ParentGroup.IsAttachment)
                {
                    uuid = part.ParentGroup.AttachedAvatar;
                }
                else
                {
                    // This will handle both deed and non-deed and also the no
                    // group case
                    if (part.ParentGroup.RootPart.GroupID.Equals(m_host.ParentGroup.RootPart.GroupID))
                        return 1;
                    return 0;
                }
            }

            // Handle the case where id names an avatar
            var presence = World.GetScenePresence(uuid);
            if (presence != null)
            {
                if (presence.IsChildAgent)
                    return 0;

                var client = presence.ControllingClient;
                if (m_host.ParentGroup.RootPart.GroupID.Equals(client.ActiveGroupId))
                    return 1;

                return 0;
            }

            return 0;
        }

        public void llUnSit(string id)
        {
            if (!UUID.TryParse(id, out var key) || key.IsZero())
                return;

            var av = World.GetScenePresence(key);
            if (av == null)
                return;

            var sittingAvatars = m_host.ParentGroup.GetSittingAvatars();

            if (sittingAvatars.Contains(av))
            {
                av.StandUp();
            }
            else
            {
                // If the object owner also owns the parcel
                // or
                // if the land is group owned and the object is group owned by the same group
                // or
                // if the object is owned by a person with estate access.
                var parcel = World.LandChannel.GetLandObject(av.AbsolutePosition);
                if (parcel != null)
                    if (m_host.OwnerID.Equals(parcel.LandData.OwnerID) ||
                        (m_host.OwnerID.Equals(m_host.GroupID) && m_host.GroupID.Equals(parcel.LandData.GroupID)
                                                               && parcel.LandData.IsGroupOwned) ||
                        World.Permissions.IsGod(m_host.OwnerID))
                        av.StandUp();
            }
        }


        public void llGroundRepel(double height, int water, double tau)
        {
            if (m_host.PhysActor != null)
            {
                var ground = (float)llGround(new LSL_Vector(0, 0, 0));
                var waterLevel = (float)llWater(new LSL_Vector(0, 0, 0));
                var hoverType = PIDHoverType.Ground;
                if (water != 0)
                {
                    hoverType = PIDHoverType.GroundAndWater;
                    if (ground < waterLevel)
                        height += waterLevel;
                    else
                        height += ground;
                }
                else
                {
                    height += ground;
                }

                m_host.SetHoverHeight((float)height, hoverType, (float)tau);
            }
        }

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
            LSL_Vector up = LSL_Vector.Cross(dir, left);

            // compute rotation based on orthogonal axes
            // and rotate so Z points to target with X below horizont
            LSL_Rotation rot = new LSL_Rotation(0.0, 0.707107, 0.0, 0.707107) * llAxes2Rot(dir, left, up);

            var sog = m_host.ParentGroup;
            if (sog == null || sog.IsDeleted)
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


        public void llCollisionFilter(LSL_String name, LSL_Key id, LSL_Integer accept)
        {
            UUID.TryParse(id, out UUID objectID);
            if (objectID.IsZero())
                m_host.SetCollisionFilter(accept != 0, name.m_string.ToLower(CultureInfo.InvariantCulture),
                    string.Empty);
            else
                m_host.SetCollisionFilter(accept != 0, name.m_string.ToLower(CultureInfo.InvariantCulture),
                    objectID.ToString());
        }

        public LSL_String llGetDate()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public LSL_Integer llEdgeOfWorld(LSL_Vector pos, LSL_Vector dir)
        {
            if (dir.x == 0 && dir.y == 0)
                return 1; // SL wiki

            float rsx = World.RegionInfo.RegionSizeX;
            float rsy = World.RegionInfo.RegionSizeY;

            // can understand what sl does if position is not in region, so do something :)
            var px = (float)Util.Clamp(pos.x, 0.5, rsx - 0.5);
            var py = (float)Util.Clamp(pos.y, 0.5, rsy - 0.5);

            float ex, ey;

            if (dir.x == 0)
            {
                ex = px;
                ey = dir.y > 0 ? rsy + 1.0f : -1.0f;
            }
            else if (dir.y == 0)
            {
                ex = dir.x > 0 ? rsx + 1.0f : -1.0f;
                ey = py;
            }
            else
            {
                var dx = (float)dir.x;
                var dy = (float)dir.y;

                var t1 = dx * dx + dy * dy;
                t1 = (float)Math.Sqrt(t1);
                dx /= t1;
                dy /= t1;

                if (dx > 0)
                    t1 = (rsx + 1f - px) / dx;
                else
                    t1 = -(px + 1f) / dx;

                float t2;
                if (dy > 0)
                    t2 = (rsy + 1f - py) / dy;
                else
                    t2 = -(py + 1f) / dy;

                if (t1 > t2)
                    t1 = t2;

                ex = px + t1 * dx;
                ey = py + t1 * dy;
            }

            ex += World.RegionInfo.WorldLocX;
            ey += World.RegionInfo.WorldLocY;

            if (World.GridService.GetRegionByPosition(RegionScopeID, (int)ex, (int)ey) != null)
                return 0;
            return 1;
        }

        public LSL_Integer llGetAgentInfo(LSL_Key id)
        {
            if (!UUID.TryParse(id, out var key) || key.IsZero()) return 0;

            var agent = World.GetScenePresence(key);
            if (agent == null) return 0;

            if (agent.IsChildAgent || agent.IsDeleted)
                return 0; // Fail if they are not in the same region

            var flags = 0;
            try
            {
                // note: in OpenSim, sitting seems to cancel AGENT_ALWAYS_RUN, unlike SL
                if (agent.SetAlwaysRun) flags |= ScriptBaseClass.AGENT_ALWAYS_RUN;

                if (agent.HasAttachments())
                {
                    flags |= ScriptBaseClass.AGENT_ATTACHMENTS;
                    if (agent.HasScriptedAttachments())
                        flags |= ScriptBaseClass.AGENT_SCRIPTED;
                }

                if ((agent.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY) != 0)
                {
                    flags |= ScriptBaseClass.AGENT_FLYING;
                    flags |= ScriptBaseClass
                        .AGENT_IN_AIR; // flying always implies in-air, even if colliding with e.g. a wall
                }

                if ((agent.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_AWAY) != 0)
                    flags |= ScriptBaseClass.AGENT_AWAY;

                if (agent.Animator.HasAnimation(busyAnimation)) flags |= ScriptBaseClass.AGENT_BUSY;

                // seems to get unset, even if in mouselook, when avatar is sitting on a prim???
                if ((agent.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) != 0)
                    flags |= ScriptBaseClass.AGENT_MOUSELOOK;

                if ((agent.State & (byte)AgentState.Typing) != 0) flags |= ScriptBaseClass.AGENT_TYPING;

                var agentMovementAnimation = agent.Animator.CurrentMovementAnimation;

                if (agentMovementAnimation == "CROUCH")
                    flags |= ScriptBaseClass.AGENT_CROUCHING;
                else if (agentMovementAnimation == "WALK" || agentMovementAnimation == "CROUCHWALK")
                    flags |= ScriptBaseClass.AGENT_WALKING;

                // not colliding implies in air. Note: flying also implies in-air, even if colliding (see above)

                // note: AGENT_IN_AIR and AGENT_WALKING seem to be mutually exclusive states in SL.

                // note: this may need some tweaking when walking downhill. you "fall down" for a brief instant
                // and don't collide when walking downhill, which instantly registers as in-air, briefly. should
                // there be some minimum non-collision threshold time before claiming the avatar is in-air?
                if ((flags & ScriptBaseClass.AGENT_WALKING) == 0 && !agent.IsColliding)
                    flags |= ScriptBaseClass.AGENT_IN_AIR;

                if (agent.ParentPart != null)
                {
                    flags |= ScriptBaseClass.AGENT_ON_OBJECT;
                    flags |= ScriptBaseClass.AGENT_SITTING;
                }

                if (agent.Animator.Animations.ImplicitDefaultAnimation.AnimID.Equals(
                        DefaultAvatarAnimations.AnimsUUIDbyName["SIT_GROUND_CONSTRAINED"]))
                    flags |= ScriptBaseClass.AGENT_SITTING;

                if (agent.Appearance.VisualParams[(int)AvatarAppearance.VPElement.SHAPE_MALE] > 0)
                    flags |= ScriptBaseClass.AGENT_MALE;
            }
            catch
            {
                return 0;
            }

            return flags;
        }

        public LSL_String llGetAgentLanguage(LSL_Key id)
        {
            // This should only return a value if the avatar is in the same region, but eh. idc.
            if (World.AgentPreferencesService == null)
            {
                Error("llGetAgentLanguage", "No AgentPreferencesService present");
            }
            else
            {
                if (UUID.TryParse(id, out var key) && key.IsNotZero())
                    return new LSL_String(World.AgentPreferencesService.GetLang(key));
            }

            return new LSL_String("en-us");
        }

        /// <summary>
        ///     http://wiki.secondlife.com/wiki/LlGetAgentList
        ///     The list of options is currently not used in SL
        ///     scope is one of:-
        ///     AGENT_LIST_REGION - all in the region
        ///     AGENT_LIST_PARCEL - all in the same parcel as the scripted object
        ///     AGENT_LIST_PARCEL_OWNER - all in any parcel owned by the owner of the
        ///     current parcel.
        ///     AGENT_LIST_EXCLUDENPC ignore NPCs (bit mask)
        /// </summary>
        public LSL_List llGetAgentList(LSL_Integer scope, LSL_List options)
        {
            // do our bit masks part
            var noNPC = (scope & ScriptBaseClass.AGENT_LIST_EXCLUDENPC) != 0;

            // remove bit masks part
            scope &= ~ ScriptBaseClass.AGENT_LIST_EXCLUDENPC;

            // the constants are 1, 2 and 4 so bits are being set, but you
            // get an error "INVALID_SCOPE" if it is anything but 1, 2 and 4
            bool regionWide = scope == ScriptBaseClass.AGENT_LIST_REGION;
            bool parcelOwned = scope == ScriptBaseClass.AGENT_LIST_PARCEL_OWNER;
            bool parcel = scope == ScriptBaseClass.AGENT_LIST_PARCEL;

            var result = new LSL_List();

            if (!regionWide && !parcelOwned && !parcel)
            {
                result.Add("INVALID_SCOPE");
                return result;
            }

            ILandObject land;
            var id = UUID.Zero;

            if (parcel || parcelOwned)
            {
                land = World.LandChannel.GetLandObject(m_host.ParentGroup.RootPart.GetWorldPosition());
                if (land == null)
                {
                    id = UUID.Zero;
                }
                else
                {
                    if (parcelOwned)
                        id = land.LandData.OwnerID;
                    else
                        id = land.LandData.GlobalID;
                }
            }

            World.ForEachRootScenePresence(
                delegate(ScenePresence ssp)
                {
                    if (noNPC && ssp.IsNPC)
                        return;

                    // Gods are not listed in SL
                    if (!ssp.IsDeleted && !ssp.IsViewerUIGod && !ssp.IsChildAgent)
                    {
                        if (!regionWide)
                        {
                            land = World.LandChannel.GetLandObject(ssp.AbsolutePosition);
                            if (land != null)
                                if ((parcelOwned && land.LandData.OwnerID.Equals(id)) ||
                                    (parcel && land.LandData.GlobalID.Equals(id)))
                                    result.Add(new LSL_Key(ssp.UUID.ToString()));
                        }
                        else
                        {
                            result.Add(new LSL_Key(ssp.UUID.ToString()));
                        }
                    }

                    // Maximum of 100 results
                    if (result.Length > 99) return;
                }
            );
            return result;
        }

        public void llEjectFromLand(LSL_Key pest)
        {
            if (UUID.TryParse(pest, out var agentID) && agentID.IsNotZero())
            {
                var presence = World.GetScenePresence(agentID);
                if (presence != null)
                {
                    // agent must be over the owners land
                    var land = World.LandChannel.GetLandObject(presence.AbsolutePosition);
                    if (land == null)
                        return;

                    if (m_host.OwnerID.Equals(land.LandData.OwnerID))
                    {
                        var p = World.GetNearestAllowedPosition(presence, land);
                        presence.TeleportOnEject(p);
                        presence.ControllingClient.SendAlertMessage("You have been ejected from this land");
                    }
                }
            }

            ScriptSleep(m_sleepMsOnEjectFromLand);
        }

        public LSL_Vector llGroundSlope(LSL_Vector offset)
        {
            //Get the slope normal.  This gives us the equation of the plane tangent to the slope.
            var vsn = llGroundNormal(offset);

            //Plug the x,y coordinates of the slope normal into the equation of the plane to get
            //the height of that point on the plane.  The resulting vector gives the slope.
            Vector3 vsl = vsn;
            vsl.Z = (vsn.x * vsn.x + vsn.y * vsn.y) / (-1 * vsn.z);
            vsl.Normalize();
            //Normalization might be overkill here

            vsn.x = vsl.X;
            vsn.y = vsl.Y;
            vsn.z = vsl.Z;

            return vsn;
        }

        public LSL_Vector llGroundNormal(LSL_Vector offset)
        {
            var pos = m_host.GetWorldPosition() + (Vector3)offset;
            // Clamp to valid position
            if (pos.X < 0)
                pos.X = 0;
            else if (pos.X >= World.Heightmap.Width)
                pos.X = World.Heightmap.Width - 1;
            if (pos.Y < 0)
                pos.Y = 0;
            else if (pos.Y >= World.Heightmap.Height)
                pos.Y = World.Heightmap.Height - 1;

            //Find two points in addition to the position to define a plane
            var p0 = new Vector3(pos.X, pos.Y,
                World.Heightmap[(int)pos.X, (int)pos.Y]);
            var p1 = new Vector3();
            var p2 = new Vector3();
            if (pos.X + 1.0f >= World.Heightmap.Width)
                p1 = new Vector3(pos.X + 1.0f, pos.Y,
                    World.Heightmap[(int)pos.X, (int)pos.Y]);
            else
                p1 = new Vector3(pos.X + 1.0f, pos.Y,
                    World.Heightmap[(int)(pos.X + 1.0f), (int)pos.Y]);
            if (pos.Y + 1.0f >= World.Heightmap.Height)
                p2 = new Vector3(pos.X, pos.Y + 1.0f,
                    World.Heightmap[(int)pos.X, (int)pos.Y]);
            else
                p2 = new Vector3(pos.X, pos.Y + 1.0f,
                    World.Heightmap[(int)pos.X, (int)(pos.Y + 1.0f)]);

            //Find normalized vectors from p0 to p1 and p0 to p2
            var v0 = new Vector3(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            var v1 = new Vector3(p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z);
            v0.Normalize();
            v1.Normalize();

            //Find the cross product of the vectors (the slope normal).
            var vsn = new Vector3
            {
                X = v0.Y * v1.Z - v0.Z * v1.Y,
                Y = v0.Z * v1.X - v0.X * v1.Z,
                Z = v0.X * v1.Y - v0.Y * v1.X
            };
            vsn.Normalize();
            //I believe the crossproduct of two normalized vectors is a normalized vector so
            //this normalization may be overkill

            return new LSL_Vector(vsn);
        }

        public LSL_Vector llGroundContour(LSL_Vector offset)
        {
            var x = llGroundSlope(offset);
            return new LSL_Vector(-x.y, x.x, 0.0);
        }


        public virtual LSL_Integer llGetFreeMemory()
        {
            // Make scripts designed for Mono happy
            return 65536;
        }


        public LSL_String llGetRegionName()
        {
            return m_regionName;
        }

        public LSL_Float llGetRegionTimeDilation()
        {
            return (double)World.TimeDilation;
        }

        /// <summary>
        ///     Returns the value reported in the client Statistics window
        /// </summary>
        public LSL_Float llGetRegionFPS()
        {
            return World.StatsReporter.LastReportedSimFPS;
        }
    }
}
using System;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Scripting;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api: MarshalByRefObject, ILSL_Api, IScriptApi
    {
        
        public void llSound(string sound, double volume, int queue, int loop)
        {
            Deprecated("llSound", "Use llPlaySound instead");
        }

        // Xantor 20080528 PlaySound updated so it accepts an objectinventory name -or- a key to a sound
        // 20080530 Updated to remove code duplication
        public void llPlaySound(string sound, double volume)
        {

            if (m_SoundModule == null)
                return;

            UUID soundID = ScriptUtils.GetAssetIdFromKeyOrItemName(m_host, sound, AssetType.Sound);
            if(soundID.IsZero())
                return;

            // send the sound, once, to all clients in range
            m_SoundModule.SendSound(m_host.UUID, soundID, volume, false, 0, false, false);
        }

        public void llLinkPlaySound(LSL_Integer linknumber, string sound, double volume)
        {
            if (m_SoundModule == null)
                return;
            if (m_host.ParentGroup == null || m_host.ParentGroup.IsDeleted)
                return;

            SceneObjectPart sop;
            if (linknumber == ScriptBaseClass.LINK_THIS)
                sop = m_host;
            else if (linknumber < 0)
                return;
            else if (linknumber < 2)
                sop = m_host.ParentGroup.RootPart;
            else
                sop = m_host.ParentGroup.GetLinkNumPart(linknumber);

            if(sop == null)
                return;

            UUID soundID = ScriptUtils.GetAssetIdFromKeyOrItemName(m_host, sound, AssetType.Sound);
            if (soundID.IsZero())
                return;

            // send the sound, once, to all clients in range
            m_SoundModule.SendSound(sop.UUID, soundID, volume, false, 0, false, false);
        }

        public void llLoopSound(string sound, double volume)
        {

            if (m_SoundModule == null)
                return;

            UUID soundID = ScriptUtils.GetAssetIdFromKeyOrItemName(m_host, sound, AssetType.Sound);
            if(soundID.IsZero())
                return;

            m_SoundModule.LoopSound(m_host.UUID, soundID, volume, false,false);
        }

        public void llLoopSoundMaster(string sound, double volume)
        {

            if (m_SoundModule == null)
                return;

            UUID soundID = ScriptUtils.GetAssetIdFromKeyOrItemName(m_host, sound, AssetType.Sound);
            if(soundID.IsZero())
                return;

            m_SoundModule.LoopSound(m_host.UUID, soundID, volume, true, false);
        }

        public void llLoopSoundSlave(string sound, double volume)
        {

            if (m_SoundModule == null)
                return;

            UUID soundID = ScriptUtils.GetAssetIdFromKeyOrItemName(m_host, sound, AssetType.Sound);
            if(soundID.IsZero())
                return;

            m_SoundModule.LoopSound(m_host.UUID, soundID, volume, false, true);
        }

        public void llPlaySoundSlave(string sound, double volume)
        {

            if (m_SoundModule == null)
                return;

            UUID soundID = ScriptUtils.GetAssetIdFromKeyOrItemName(m_host, sound, AssetType.Sound);
            if(soundID.IsZero())
                return;

            // send the sound, once, to all clients in range
            m_SoundModule.SendSound(m_host.UUID, soundID, volume, false, 0, true, false);
        }

        public void llTriggerSound(string sound, double volume)
        {

            if (m_SoundModule == null)
                return;

            UUID soundID = ScriptUtils.GetAssetIdFromKeyOrItemName(m_host, sound, AssetType.Sound);
            if(soundID.IsZero())
                return;

            // send the sound, once, to all clients in rangeTrigger or play an attached sound in this part's inventory.
            m_SoundModule.SendSound(m_host.UUID, soundID, volume, true, 0, false, false);
        }

        public void llStopSound()
        {

            if (m_SoundModule != null)
                m_SoundModule.StopSound(m_host.UUID);
        }

        public void llLinkStopSound(LSL_Integer linknumber)
        {
            if (m_SoundModule != null)
            {
                foreach(SceneObjectPart sop in GetLinkParts(linknumber))
                    m_SoundModule.StopSound(sop.UUID);
            }
        }

        public void llPreloadSound(string sound)
        {

            if (m_SoundModule == null)
                return;

            UUID soundID = ScriptUtils.GetAssetIdFromKeyOrItemName(m_host, sound, AssetType.Sound);
            if(soundID.IsZero())
                return;

            m_SoundModule.PreloadSound(m_host.UUID, soundID);
            ScriptSleep(m_sleepMsOnPreloadSound);
        }

    }
}
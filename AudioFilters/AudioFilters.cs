using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;
using Menu;
using System.Collections.Generic;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour.HookGen;


[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: System.Runtime.CompilerServices.SuppressIldasmAttribute()]
namespace AudioFilters
{
    [BepInEx.BepInPlugin("henpemaz.audiofilters", "AudioFilters", "1.0")]
    public class AudioFilters : BepInEx.BaseUnityPlugin
    {
        public string author = "Henpemaz";
        public static AudioFilters instance;

        UnityEngine.AudioListener listener = null;

        public static class EnumExt_AudioFilters
        {
#pragma warning disable 0649
            public static RoomSettings.RoomEffect.Type AudioFiltersReverb;
#pragma warning restore 0649
        }


        public void OnEnable()
        {
            instance = this;

            On.RainWorld.Start += RainWorld_Start;

            On.VirtualMicrophone.NewRoom += VirtualMicrophone_NewRoom;
            On.VirtualMicrophone.AllQuiet += VirtualMicrophone_AllQuiet;

            // todo make songs bypass effects

            // make intensity
            // add different presets ?
            // make tunable?
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);

            // find listener
            // could instead store game object
            foreach(UnityEngine.Camera cam in GameObject.FindObjectsOfType<UnityEngine.Camera>())
            {
                if(cam.GetComponent<UnityEngine.AudioListener>() is AudioListener l)
                {
                    listener = l;
                }
            }
        }

        // had trouble trying to tag or name these, keep them in a list instead
        private List<Component> activeFilters = new List<Component>();

        private void VirtualMicrophone_NewRoom(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
        {
            orig(self, room);
            ClearAllFilters();

            if (room.roomSettings.GetEffectAmount(EnumExt_AudioFilters.AudioFiltersReverb) > 0f)
            {
                var reverb = listener.gameObject.AddComponent<AudioReverbFilter>();

                reverb.reverbPreset = AudioReverbPreset.SewerPipe;

                activeFilters.Add(reverb);
            }
        }

        private void ClearAllFilters()
        {
            foreach (var c in activeFilters)
            {
                Destroy(c);
            }
            activeFilters.Clear();
        }

        private void VirtualMicrophone_AllQuiet(On.VirtualMicrophone.orig_AllQuiet orig, VirtualMicrophone self)
        {
            orig(self);

            ClearAllFilters();
        }

    }
}

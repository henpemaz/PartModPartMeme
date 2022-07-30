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
[assembly: System.Runtime.CompilerServices.SuppressIldasm()]
namespace AudioFilters
{
    [BepInEx.BepInPlugin("henpemaz.audiofilters", "AudioFilters", "1.0")]
    public class AudioFilters : BepInEx.BaseUnityPlugin
    {
        public string author = "Henpemaz";
        public static AudioFilters instance;
        private bool isHUDSound;

        public static class EnumExt_AudioFilters
        {
#pragma warning disable 0649
            public static RoomSettings.RoomEffect.Type AudioFiltersReverb;
#pragma warning restore 0649
        }

        public void OnEnable()
        {
            instance = this;

            // add filters
            On.VirtualMicrophone.SoundObject.ctor += SoundObject_ctor;
            On.AmbientSoundPlayer.TryInitiation += AmbientSoundPlayer_TryInitiation;

            // remove filters
            On.VirtualMicrophone.NewRoom += VirtualMicrophone_NewRoom;

            // bypass hud sounds
            On.Player.PlayHUDSound += Player_PlayHUDSound;

            // make intensity
            // add different presets
            // make tunable

            // placed rectangle reverb area
        }

        private void Player_PlayHUDSound(On.Player.orig_PlayHUDSound orig, Player self, SoundID soundID)
        {
            isHUDSound = true;
            orig(self, soundID);
            isHUDSound = false;
        }

        private void AmbientSoundPlayer_TryInitiation(On.AmbientSoundPlayer.orig_TryInitiation orig, AmbientSoundPlayer self)
        {
            orig(self);
            if (self.initiated)
            {
                var room = self.mic.camera.loadingRoom ?? self.mic.camera.room;
                if (room.roomSettings.GetEffectAmount(EnumExt_AudioFilters.AudioFiltersReverb) > 0f)
                {
                    Debug.Log("Added reverb to " + room.abstractRoom.name + ":" + self.aSound.sample);
                    var reverb = self.gameObject.AddComponent<AudioReverbFilter>();
                    reverb.reverbPreset = AudioReverbPreset.SewerPipe;
                }
            }
        }

        private void SoundObject_ctor(On.VirtualMicrophone.SoundObject.orig_ctor orig, VirtualMicrophone.SoundObject self, VirtualMicrophone mic, SoundLoader.SoundData soundData, bool loop, float initPan, float initVol, float initPitch, bool startAtRandomTime)
        {
            orig(self, mic, soundData, loop, initPan, initVol, initPitch, startAtRandomTime);

            if (!isHUDSound && self.mic.room.roomSettings.GetEffectAmount(EnumExt_AudioFilters.AudioFiltersReverb) > 0f)
            {
                var reverb = self.gameObject.AddComponent<AudioReverbFilter>();
                reverb.reverbPreset = AudioReverbPreset.SewerPipe;
            }
        }

        private void VirtualMicrophone_NewRoom(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
        {
            orig(self, room);

            // remove or adjust effect of existing objects
            if (room.roomSettings.GetEffectAmount(EnumExt_AudioFilters.AudioFiltersReverb) > 0f)
            {
                foreach (var a in self.ambientSoundPlayers)
                {
                    if (a.gameObject != null)
                    {
                        Debug.Log("Added reverb to " + self.room.abstractRoom.name + ":" + a.aSound.sample);
                        var reverb = a.gameObject.GetComponent<AudioReverbFilter>() ?? a.gameObject.AddComponent<AudioReverbFilter>();
                        reverb.reverbPreset = AudioReverbPreset.SewerPipe;
                    }
                }

                foreach (var o in self.soundObjects)
                {
                    if (o.gameObject != null)
                    {
                        Debug.Log("Added reverb to " + self.room.abstractRoom.name + ":" + o.soundData.soundID);
                        var reverb = o.gameObject.GetComponent<AudioReverbFilter>() ?? o.gameObject.AddComponent<AudioReverbFilter>();
                        reverb.reverbPreset = AudioReverbPreset.SewerPipe;
                    }
                }
            }
            else
            {
                foreach (var a in self.ambientSoundPlayers)
                {
                    if (a.gameObject != null)
                    {
                        var reverb = a.gameObject.GetComponent<AudioReverbFilter>();
                        if (reverb != null) Destroy(a.gameObject.GetComponent<AudioReverbFilter>());
                    }
                }
            }
        }
    }
}

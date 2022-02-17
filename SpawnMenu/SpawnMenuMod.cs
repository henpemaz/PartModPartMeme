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
using RWCustom;

[assembly: AssemblyTrademark("Henpemaz")]

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SpawnMenu
{
    [BepInEx.BepInPlugin("henpemaz.spawnmenumod", "SpawnMenuMod", "1.0")]
    public class SpawnMenuMod : BepInEx.BaseUnityPlugin
    {
        public string author = "Henpemaz";
        public static SpawnMenuMod instance;

        public void OnEnable()
        {
            instance = this;

            On.Menu.PauseMenu.ctor += PauseMenu_ctor;
            On.Menu.PauseMenu.Update += PauseMenu_Update;
            On.Menu.PauseMenu.GrafUpdate += PauseMenu_GrafUpdate;
            On.Menu.PauseMenu.ShutDownProcess += PauseMenu_ShutDownProcess;

            new Hook(typeof(ArenaBehaviors.ArenaGameBehavior).GetProperty("room").GetGetMethod(), typeof(SpawnMenuMod).GetMethod("get_room"), this);
        }

        private void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
        {
            orig(self, manager, game);

            if (!game.IsStorySession || self.game.cameras[0].room == null) return;

            try
            {
                if (manager.arenaSetup == null) // this is required
                {
                    manager.arenaSetup = new ArenaSetup();
                }

                var room = self.game.cameras[0].room;

                SandboxGameSession sb = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SandboxGameSession)) as SandboxGameSession;
                sb.arenaSitting = new ArenaSitting(manager.arenaSetup.GetOrInitiateGameTypeSetup(ArenaSetup.GameTypeID.Sandbox), new MultiplayerUnlocks(manager.rainWorld.progression, new List<string>()));
                sb.game = game;

                var os = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Overseer), null, game.Players[0].pos, new EntityID());
                os.realizedCreature = new Overseer(os, room.world) { room = room, extended = 0f }; // so cursor doesnt flicker
                if (game.Players[0].realizedCreature != null)
                {
                    os.realizedCreature.mainBodyChunk.pos = game.Players[0].realizedCreature.mainBodyChunk.pos;
                    os.realizedCreature.mainBodyChunk.lastPos = game.Players[0].realizedCreature.mainBodyChunk.lastPos;
                }
                room.AddObject(new SandboxOverlayOwner(room, sb, !sb.PlayMode));

                sb.overlay.Initiate(false);
                for (int l = 0; l < SandboxEditorSelector.Width; l++)
                {
                    for (int m = 0; m < SandboxEditorSelector.Height; m++)
                    {
                        var btn = sb.overlay.sandboxEditorSelector.buttons[l, m];
                        // no slugcat and no actions
                        if ((sb.overlay.sandboxEditorSelector.buttons[l, m] is SandboxEditorSelector.CreatureOrItemButton coib 
                            && coib.data.itemType == AbstractPhysicalObject.AbstractObjectType.Creature 
                            && coib.data.critType == CreatureTemplate.Type.Slugcat)
                            ||
                            (sb.overlay.sandboxEditorSelector.buttons[l, m] is SandboxEditorSelector.ActionButton ab
                            && (
                                ab.action == SandboxEditorSelector.ActionButton.Action.Play ||
                                ab.action == SandboxEditorSelector.ActionButton.Action.Randomize ||
                                ab.action == SandboxEditorSelector.ActionButton.Action.ConfigA ||
                                ab.action == SandboxEditorSelector.ActionButton.Action.ConfigB ||
                                ab.action == SandboxEditorSelector.ActionButton.Action.ConfigC
                            )))
                        {
                            sb.overlay.sandboxEditorSelector.buttons[l, m] = null;
                            sb.overlay.sandboxEditorSelector.RemoveSubObject(btn);
                            btn.RemoveSprites();
                        }
                    }
                }

                sb.editor = new ArenaBehaviors.SandboxEditor(sb);
                sb.editor.currentConfig = -1;


                sb.editor.cursors.Add(new ArenaBehaviors.SandboxEditor.EditCursor(sb.editor, os.abstractAI as OverseerAbstractAI, 0, new Vector2(-1000, -1000)));
                room.AddObject(sb.editor.cursors[0]);
                sb.overlay.sandboxEditorSelector.ConnectToEditor(sb.editor);

                sb.sandboxInitiated = true;
                sb.overlay.fadingOut = true;
            }
            catch (Exception e)
            {
                Debug.LogError("SpawnMenuMod failed to create sandbox menu");
                Debug.LogException(e);
            }
        }

        private void PauseMenu_Update(On.Menu.PauseMenu.orig_Update orig, PauseMenu self)
        {
            if (!self.game.IsStorySession || self.game.pauseMenu == null || self.game.cameras[0].room == null) // none of my bisness
            {
                orig(self);
                return;
            }

            self.game.pauseMenu = null; // several menu thinghies check this and stop working :(
            // some room thinghies need updating
            var room = self.game.cameras[0].room;
            var overlayowner = room.updateList.First(o => o is SandboxOverlayOwner) as SandboxOverlayOwner;
            overlayowner.Update(false);
            room.updateList.First(o => o is ArenaBehaviors.SandboxEditor.EditCursor).Update(false);
            foreach(var uad in room.updateList)
            {
                if(uad is ArenaBehaviors.SandboxEditor.PlacedIcon ico) ico.Update(false);
            }
            self.game.pauseMenu = self; // done here

            // grab processed by our menus not pause menu
            // if doing anything, pause buttons shut down
            if ((overlayowner.overlay.sandboxEditorSelector.currentlyVisible
                    || overlayowner.overlay.sandboxEditorSelector.editor.cursors[0].homeInIcon != null
                    || overlayowner.overlay.sandboxEditorSelector.editor.cursors[0].dragIcon != null)
                && self.manager.upcomingProcess == null) // menus freeze input if there's an upcoming process, we use that here to pause the pause menu
            {
                self.pressButton = false; //  prevent processing the grab input that brought up the menu                
                self.manager.upcomingProcess = self.ID;
                orig(self);
                self.manager.upcomingProcess = null;
            }
            else
            {
                orig(self);
            }

            // fade ctrls on menuing
            if (self.controlMap != null && (overlayowner.selector.visFac > 0f || overlayowner.selector.lastVisFac > 0f))
            {
                // this could be rewritten into a hook for ctrlmap update or something.
                self.controlMap.fade = Mathf.Clamp01(self.controlMap.fade - overlayowner.selector.visFac);
                self.controlMap.lastFade = Mathf.Clamp01(self.controlMap.lastFade - overlayowner.selector.lastVisFac);
                // re calc everything
                self.controlMap.controlsMap.setAlpha = new float?(self.controlMap.fade);
                self.controlMap.controlsMap2.setAlpha = new float?(Mathf.Min(self.controlMap.fade, Custom.SCurve(Mathf.InverseLerp(5f, 80f, (float)self.controlMap.counter), 0.8f)));
                self.controlMap.controlsMap3.setAlpha = new float?(Mathf.Min(self.controlMap.fade, Custom.SCurve(Mathf.InverseLerp(5f, 80f, (float)self.controlMap.counter), 0.8f)) * 0.5f);
                if (self.controlMap.pickupButtonInstructions != null)
                {
                    self.controlMap.pickupFade = Mathf.Clamp01(Custom.SCurve(Mathf.InverseLerp(40f, 120f, (float)self.controlMap.counter), 0.5f) - overlayowner.selector.visFac);
                    self.controlMap.lastPickupFade = Mathf.Clamp01(Custom.SCurve(Mathf.InverseLerp(40f, 120f, (float)self.controlMap.counter - 1f), 0.5f) - overlayowner.selector.lastVisFac);
                }
            }
        }

        private void PauseMenu_GrafUpdate(On.Menu.PauseMenu.orig_GrafUpdate orig, PauseMenu self, float timeStacker)
        {
            orig(self, timeStacker);
            if (!self.game.IsStorySession || self.game.pauseMenu == null) return;
            //var room = self.game.cameras[0].room;
            //var overlayowner = room.updateList.First(o => o is SandboxOverlayOwner) as SandboxOverlayOwner;
            //overlayowner?.overlay.GrafUpdate(timeStacker);
            self.game.cameras[0].DrawUpdate(0f, 1f); // so icons and cursor also update otherwise this would get quite verbose in here
            // timespeed 1 so audio doesnt glitch out
        }

        // close everything and actually spawn stuff
        private void PauseMenu_ShutDownProcess(On.Menu.PauseMenu.orig_ShutDownProcess orig, PauseMenu self)
        {
            try
            {
                if (self.game.IsStorySession)
                {
                    var room = self.game.cameras[0].room;
                    var overlayowner = room.updateList.First(o => o is SandboxOverlayOwner) as SandboxOverlayOwner;
                    var editor = overlayowner.gameSession.editor;
                    overlayowner.gameSession.PlayMode = true;
                    On.World.GetAbstractRoom_int += World_GetAbstractRoom_int;
                    On.AbstractWorldEntity.ctor += AbstractWorldEntity_ctor;
                    room.world.singleRoomWorld = true; // deer ai checks this, miros probs too
                    foreach (var ico in editor.icons)
                    {
                        if(ico is ArenaBehaviors.SandboxEditor.CreatureOrItemIcon coii)
                        {
                            var data = new ArenaBehaviors.SandboxEditor.PlacedIconData(coii.pos, coii.iconData, coii.ID);
                            //Debug.Log("SpawnMenu spawning " + coii.iconData.itemType + " " + coii.iconData.critType);
                            overlayowner.gameSession.SpawnEntity(data);
                            if (room.abstractRoom.entities.Last() is AbstractPhysicalObject apo)
                            {
                                //apo.pos.room = room.abstractRoom.index;
                                //Debug.Log("spawning apo of type " + apo.type);
                                //Debug.Log("apo tostr " + apo);
                                //Debug.Log("apo type tostr " + apo.GetType());
                                //if (apo.type == AbstractPhysicalObject.AbstractObjectType.Creature) Debug.Log("spawning creature type " + (apo as AbstractCreature).creatureTemplate.type);
                                apo.RealizeInRoom();
                                //Debug.Log("realized is null ? " + (apo.realizedObject == null));
                                //if(apo.realizedObject != null) Debug.Log("realized is at " + apo.realizedObject.bodyChunks[0].pos);
                            }
                        }
                        ico.Fade();
                    }
                    room.world.singleRoomWorld = false;
                    On.World.GetAbstractRoom_int -= World_GetAbstractRoom_int;
                    On.AbstractWorldEntity.ctor -= AbstractWorldEntity_ctor;
                    overlayowner.Destroy();
                    //room.RemoveObject(overlayowner);
                    overlayowner.overlay.ShutDownProcess();

                    var editCursor = room.updateList.First(o => o is ArenaBehaviors.SandboxEditor.EditCursor) as ArenaBehaviors.SandboxEditor.EditCursor;
                    editCursor.Destroy();
                }
            }
            finally
            {
                On.World.GetAbstractRoom_int -= World_GetAbstractRoom_int;
                On.AbstractWorldEntity.ctor -= AbstractWorldEntity_ctor;
                orig(self);
            }
        }

        private void AbstractWorldEntity_ctor(On.AbstractWorldEntity.orig_ctor orig, AbstractWorldEntity self, World world, WorldCoordinate pos, EntityID ID)
        {
            pos.room = world.game.cameras[0].room.abstractRoom.index;
            orig(self, world, pos, ID);
        }

        private AbstractRoom World_GetAbstractRoom_int(On.World.orig_GetAbstractRoom_int orig, World self, int room)
        {
            return self.game.cameras[0].room.abstractRoom;
        }

        public delegate Room orig_get_room(ArenaBehaviors.ArenaGameBehavior bhv);
        public Room get_room(orig_get_room orig, ArenaBehaviors.ArenaGameBehavior self)
        {
            if(self.gameSession.game.IsStorySession)
                return self.gameSession.game.cameras[0].room;
            return orig(self);
        }
    }
}

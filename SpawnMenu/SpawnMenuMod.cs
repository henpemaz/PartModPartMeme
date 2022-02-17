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
            IL.SandboxGameSession.SpawnEntity += SandboxGameSession_SpawnEntity;
            new Hook(typeof(ArenaBehaviors.ArenaGameBehavior).GetProperty("room").GetGetMethod(), typeof(SpawnMenuMod).GetMethod("get_room"), this);
        }

        private void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
        {
            orig(self, manager, game);

            if (!game.IsStorySession) return;

            try
            {
                if (manager.arenaSetup == null)
                {
                    manager.arenaSetup = new ArenaSetup();
                }

                var room = self.game.cameras[0].room;

                SandboxGameSession sb = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SandboxGameSession)) as SandboxGameSession;
                sb.arenaSitting = new ArenaSitting(manager.arenaSetup.GetOrInitiateGameTypeSetup(ArenaSetup.GameTypeID.Sandbox), new MultiplayerUnlocks(manager.rainWorld.progression, new List<string>()));
                sb.game = game;

                var os = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Overseer), null, new WorldCoordinate() { room = game.world.firstRoomIndex }, new EntityID());

                room.AddObject(new SandboxOverlayOwner(room, sb, !sb.PlayMode));
                sb.overlay.Initiate(false);

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
                Debug.LogError("Failed to create sandbox menu");
                Debug.LogException(e);
            }
        }

        private void PauseMenu_Update(On.Menu.PauseMenu.orig_Update orig, PauseMenu self)
        {
            orig(self);
            if (!self.game.IsStorySession || self.game.pauseMenu == null) return;
            self.game.pauseMenu = null;
            var room = self.game.cameras[0].room;
            var overlayowner = room?.updateList.First(o => o is SandboxOverlayOwner) as SandboxOverlayOwner;
            overlayowner?.Update(false);

            foreach(var uad in room.updateList)
            {
                if(uad is ArenaBehaviors.SandboxEditor.PlacedIcon ico) ico.Update(false);
            }

            var editCursor = room?.updateList.First(o => o is ArenaBehaviors.SandboxEditor.EditCursor) as ArenaBehaviors.SandboxEditor.EditCursor;
            editCursor?.Update(false);
            self.game.pauseMenu = self;
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
                    room.world.singleRoomWorld = true;
                    foreach (var ico in editor.icons)
                    {
                        if(ico is ArenaBehaviors.SandboxEditor.CreatureOrItemIcon coii)
                        {
                            var data = new ArenaBehaviors.SandboxEditor.PlacedIconData(coii.pos, coii.iconData, coii.ID);
                            overlayowner.gameSession.SpawnEntity(data);
                            if (room.abstractRoom.entities.Last() is AbstractPhysicalObject apo) apo.RealizeInRoom();
                        }
                        ico.Fade();
                    }
                    room.world.singleRoomWorld = false;
                    On.World.GetAbstractRoom_int -= World_GetAbstractRoom_int;
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
                orig(self);
            }
        }

        private AbstractRoom World_GetAbstractRoom_int(On.World.orig_GetAbstractRoom_int orig, World self, int room)
        {
            return self.game.cameras[0].room.abstractRoom;
        }

        private void SandboxGameSession_SpawnEntity(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdloca(out _),
                i => i.MatchLdcI4(0),
                i => i.MatchLdcI4(-1),
                i => i.MatchLdcI4(-1),
                i => i.MatchLdcI4(-1),
                i => i.MatchCall<WorldCoordinate>(".ctor")
                ))
            {
                c.Index++;
                c.Remove();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<SandboxGameSession, int>>((s) =>
                {
                    if (s.game.IsStorySession) return s.game.cameras[0].room.abstractRoom.index;
                    return 0;
                });
            }
            else Debug.LogException(new Exception("Couldn't IL-hook SandboxGameSession_SpawnEntity from spawnmenumod")); // deffendisve progrmanig
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

using Partiality.Modloader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;


[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; }
    }
}



namespace ExtendedGates
{
    public class ExtendedGates : PartialityMod
    {
        public ExtendedGates()
        {
            this.ModID = "ExtendedGates";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static ExtendedGates instance;

        static Type uwu;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            On.RainWorld.Start += RainWorld_Start;

            On.GateKarmaGlyph.DrawSprites += GateKarmaGlyph_DrawSprites;

            On.RegionGate.ctor += RegionGate_ctor;
            On.RegionGate.Update += RegionGate_Update;
            On.RegionGate.KarmaBlinkRed += RegionGate_KarmaBlinkRed;

            On.HUD.Map.GateMarker.ctor += GateMarker_ctor;
            On.HUD.Map.MapData.KarmaOfGate += MapData_KarmaOfGate;


            uwu = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "UwUMod")
                {
                    uwu = asm.GetType("UwUMod.UwUMod");
                }
            }
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);
            LoadAditionalResources();
        }

        internal static string[] specialRequirements = // all lowercase here, compared against tolower
        {
            "open",
            "10reinforced",
            "forbidden",
            "comsmark",
            "uwu"
        };


        private void GateMarker_ctor(On.HUD.Map.GateMarker.orig_ctor orig, HUD.Map.GateMarker self, HUD.Map map, int room, int karma, bool showAsOpen)
        {
            if (karma > 4) // above 5 karma support
            {
                orig(self, map, room, 0, showAsOpen);
                Debug.LogError("GateMarker_ctor got karma" + karma);
                switch (karma)
                {
                    case 100: // open
                        self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarma56"); // Uh oh
                        break;
                    case 101: // 10reinforced
                        self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarma10reinforced");
                        break;
                    case 102: // forbidden
                        self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarmaForbidden");
                        break;
                    case 103: // comsmark
                        self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarmaComsmark");
                        break;
                    case 104: // uwu
                        self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarmaUwu");
                        break;

                    default:
                        int? cap = map.hud.rainWorld.progression?.currentSaveState?.deathPersistentSaveData?.karmaCap;
                        if (!cap.HasValue || cap.Value < 6) cap = karma;
                        self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarma" + karma.ToString() + "-" + cap.Value.ToString());
                        break;
                }
            }
            else
            {
                orig(self, map, room, karma, showAsOpen);
            }
        }



        private int MapData_KarmaOfGate(On.HUD.Map.MapData.orig_KarmaOfGate orig, HUD.Map.MapData self, PlayerProgression progression, World initWorld, string roomName)
        {
            // Gotta scan it all, progression was loaded on game-start and doesnt account for enabled/disabled regions ?
            foreach (KeyValuePair<string, string> keyValues in CustomRegions.Mod.CustomWorldMod.activatedPacks)
            {
                //CustomWorldMod.Log($"Custom Regions: Loading KarmaOfGate for {keyValues.Key}", false, CustomWorldMod.DebugLevel.FULL);
                string path = CustomRegions.Mod.CustomWorldMod.resourcePath + keyValues.Value + Path.DirectorySeparatorChar;
                string path2 = path + "World" + Path.DirectorySeparatorChar + "Gates" + Path.DirectorySeparatorChar + "extendedLocks.txt";
                if (File.Exists(path2))
                {
                    string[] array = File.ReadAllLines(path2);

                    for (int i = 0; i < array.Length; i++)
                    {
                        string[] array2 = Regex.Split(array[i], " : ");
                        if (array2[0] == roomName)
                        {

                            string req1 = array2[1];
                            string req2 = array2[2];
                            int result;
                            int result2;
                            if (specialRequirements.Contains(req1.ToLower()))
                            {
                                result = 100 + System.Array.IndexOf(specialRequirements, req1.ToLower());
                            }
                            else
                            {
                                result = RWCustom.Custom.IntClamp(int.Parse(req1) - 1, 0, 9); // changed from 4 to 9
                            }
                            if (specialRequirements.Contains(req2.ToLower()))
                            {
                                result2 = 100 + System.Array.IndexOf(specialRequirements, req2.ToLower());
                            }
                            else
                            {
                                result2 = RWCustom.Custom.IntClamp(int.Parse(req2) - 1, 0, 9); // changed from 4 to 9
                            }

                            bool flag = false;
                            if (roomName == "GATE_LF_SB" || roomName == "GATE_DS_SB" || roomName == "GATE_HI_CC" || roomName == "GATE_SS_UW")
                            {
                                flag = true;
                            }

                            //CustomWorldMod.Log($"Custom Regions: Found custom KarmaOfGate for {keyValues.Key}. Gate [{result}/{result2}]");

                            string[] namearray = Regex.Split(roomName, "_");
                            if (namearray.Length != 3 || (namearray[1] == namearray[2])) // In-region gate support
                            {
                                return Mathf.Max(result, result2);
                            }

                            if (namearray[1] == initWorld.region.name != flag)
                            {
                                return result;
                            }
                            return result2;
                        }
                    }
                }
            }
            return orig(self, progression, initWorld, roomName);
        }

        /// <summary>
        /// Loads karmaGate requirements
        /// </summary>
        private void RegionGate_ctor(On.RegionGate.orig_ctor orig, RegionGate self, Room room)
        {
            orig(self, room);

            foreach (KeyValuePair<string, string> keyValues in CustomRegions.Mod.CustomWorldMod.activatedPacks)
            {
                // CustomWorldMod.Log($"Custom Regions: Loading karmaGate requirement for {keyValues.Key}", false, CustomWorldMod.DebugLevel.FULL);
                string path = CustomRegions.Mod.CustomWorldMod.resourcePath + keyValues.Value + Path.DirectorySeparatorChar;

                string path2 = path + "World" + Path.DirectorySeparatorChar + "Gates" + Path.DirectorySeparatorChar + "extendedLocks.txt";
                bool foundKarma = false;
                if (File.Exists(path2))
                {
                    string[] array = File.ReadAllLines(path2);

                    for (int i = 0; i < array.Length; i++)
                    {
                        string[] array2 = Regex.Split(array[i], " : ");
                        if (array2[0] == room.abstractRoom.name)
                        {
                            self.karmaGlyphs[0].Destroy();
                            self.karmaGlyphs[1].Destroy();

                            string req1 = array2[1];
                            if (specialRequirements.Contains(req1.ToLower()))
                            {
                                self.karmaRequirements[0] = 100 + Array.IndexOf(specialRequirements, req1.ToLower());
                            }
                            else
                            {
                                self.karmaRequirements[0] = RWCustom.Custom.IntClamp(int.Parse(req1) - 1, 0, 9); // changed from 4 to 9
                            }
                            string req2 = array2[2];
                            if (specialRequirements.Contains(req2.ToLower()))
                            {
                                self.karmaRequirements[1] = 100 + Array.IndexOf(specialRequirements, req2.ToLower());
                            }
                            else
                            {
                                self.karmaRequirements[1] = RWCustom.Custom.IntClamp(int.Parse(req2) - 1, 0, 9); // changed from 4 to 9
                            }

                            self.karmaGlyphs = new GateKarmaGlyph[2];
                            for (int j = 0; j < 2; j++)
                            {
                                self.karmaGlyphs[j] = new GateKarmaGlyph(j == 1, self, self.karmaRequirements[j]);
                                room.AddObject(self.karmaGlyphs[j]);
                            }

                            if (array2.Length > 3 && array2[3].ToLower() == "multi") // "Infinite" uses
                            {
                                if (self is WaterGate) // sets water level, don't want to get into crazy float craze
                                {
                                    (self as WaterGate).waterLeft = 30f; // ((!room.world.regionState.gatesPassedThrough[room.abstractRoom.gateIndex]) ? 2f : 1f);
                                }
                                else if (self is ElectricGate)
                                {
                                    (self as ElectricGate).batteryLeft = 30f; // ((!room.world.regionState.gatesPassedThrough[room.abstractRoom.gateIndex]) ? 2f : 1f);
                                }
                            }

                            // CustomWorldMod.Log($"Custom Regions: Found custom karmaGate requirement for {keyValues.Key}. Gate [{self.karmaRequirements[0]}/{self.karmaRequirements[1]}]");
                            foundKarma = true;
                            break;
                        }
                    }
                    if (foundKarma) { break; }
                }
            }
        }

        /// <summary>
        /// Adds support for special gate types UwU
        /// </summary>
        private void RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
        {
            int preupdateCounter = self.startCounter;
            orig(self, eu);
            if (!self.room.game.IsStorySession) return; // Paranoid, just like in the base game
            switch (self.mode)
            {
                case RegionGate.Mode.MiddleClosed:
                    int num = self.PlayersInZone();
                    if (num > 0 && num < 3 && !self.dontOpen && self.PlayersStandingStill() && self.EnergyEnoughToOpen && PlayersMeetSpecialRequirements(self))
                    {
                        self.startCounter = preupdateCounter + 1;
                    }

                    if (self.startCounter == 69)
                    {
                        // OPEN THE GATES on the next frame
                        if (self.room.game.GetStorySession.saveStateNumber == 1)
                        {
                            self.Unlock(); // sets savestate thing for monk
                        }
                        self.unlocked = true;
                    }
                    break;

                case RegionGate.Mode.ClosingAirLock:
                    if (preupdateCounter == 69) // We did it... last frame
                    {
                        // if it shouldn't be unlocked, lock it back
                        self.unlocked = (self.room.game.GetStorySession.saveStateNumber == 1 && self.room.game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates.Contains(self.room.abstractRoom.name)) || (self.room.game.StoryCharacter != 2 && File.Exists(RWCustom.Custom.RootFolderDirectory() + "nifflasmode.txt"));
                    }

                    if (self.room.game.overWorld.worldLoader == null) // In-region gate support
                    {
                        self.waitingForWorldLoader = false;
                    }
                    break;
                case RegionGate.Mode.Closed: // Support for multi-usage gates
                    if (self.EnergyEnoughToOpen) self.mode = RegionGate.Mode.MiddleClosed;
                    break;
            }
        }

        private bool RegionGate_KarmaBlinkRed(On.RegionGate.orig_KarmaBlinkRed orig, RegionGate self)
        {
            return orig(self) && !PlayersMeetSpecialRequirements(self);
        }

        private bool PlayersMeetSpecialRequirements(RegionGate self)
        {
            switch (self.karmaRequirements[(!self.letThroughDir) ? 1 : 0])
            {
                case 100: // open
                    return true;
                case 101: // 10reinforced
                    if (((self.room.game.Players[0].realizedCreature as Player).Karma == 9 && (self.room.game.Players[0].realizedCreature as Player).KarmaIsReinforced) || self.unlocked)
                        return true;
                    break;
                case 102: // forbidden
                    break;
                case 103: // comsmark
                    if (self.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark || self.unlocked)
                        return true;
                    break;
                case 104: // uwu
                    if (uwu != null || self.unlocked)
                        return true;
                    break;
                default: // default karma gate handled by the game
                    break;
            }

            return false;
        }

        /// <summary>
        /// Adds support to karma 6 thru 10 for gates, also special gates
        /// </summary>
        private void GateKarmaGlyph_DrawSprites(On.GateKarmaGlyph.orig_DrawSprites orig, GateKarmaGlyph self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            if (self.symbolDirty && !self.gate.unlocked && self.requirement > 4)
            {
                switch (self.requirement)
                {
                    case 100: // open
                        sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol0");
                        self.symbolDirty = false;
                        break;
                    case 101: // 10reinforced
                        sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol10reinforced");
                        self.symbolDirty = false;
                        break;
                    case 102: // forbidden
                        sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbolForbidden");
                        self.symbolDirty = false;
                        break;
                    case 103: // comsmark
                        sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbolComsmark");
                        self.symbolDirty = false;
                        break;
                    case 104: // uwu
                        sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbolUwu");
                        self.symbolDirty = false;
                        break;

                    default:
                        int cap = (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap;
                        if (cap <= 5 || cap < self.requirement) cap = self.requirement;
                        sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol" + self.requirement.ToString() + cap.ToString());
                        self.symbolDirty = false;
                        break;
                }
            }
            orig(self, sLeaser, rCam, timeStacker, camPos);
        }







        internal void LoadAditionalResources()
        {
            LoadCustomAtlas("ExtendedGateSymbols", Assembly.GetExecutingAssembly().GetManifestResourceStream("ExtendedGates.Resources.ExtendedGateSymbols.png"), Assembly.GetExecutingAssembly().GetManifestResourceStream("ExtendedGates.Resources.ExtendedGateSymbols.txt"));
        }

        public static KeyValuePair<string, string> MetaEntryToKeyVal(string input)
        {
            if (string.IsNullOrEmpty(input)) return new KeyValuePair<string, string>("", "");
            string[] pieces = input.Split(new char[] { ':' }, 2); // No trim option in framework 3.5
            if (pieces.Length == 0) return new KeyValuePair<string, string>("", "");
            if (pieces.Length == 1) return new KeyValuePair<string, string>(pieces[0].Trim(), "");
            return new KeyValuePair<string, string>(pieces[0].Trim(), pieces[1].Trim());
        }

        public static FAtlas LoadCustomAtlas(string atlasName, System.IO.Stream textureStream, System.IO.Stream slicerStream = null, System.IO.Stream metaStream = null)
        {
            try
            {
                Texture2D imageData = new Texture2D(0, 0, TextureFormat.ARGB32, false);
                byte[] bytes = new byte[textureStream.Length];
                textureStream.Read(bytes, 0, (int)textureStream.Length);
                imageData.LoadImage(bytes);
                Dictionary<string, object> slicerData = null;
                if (slicerStream != null)
                {
                    StreamReader sr = new StreamReader(slicerStream, Encoding.UTF8);
                    slicerData = sr.ReadToEnd().dictionaryFromJson();
                }
                Dictionary<string, string> metaData = null;
                if (metaStream != null)
                {
                    StreamReader sr = new StreamReader(metaStream, Encoding.UTF8);
                    metaData = new Dictionary<string, string>(); // Boooooo no linq and no splitlines, shame on you c#
                    for (string fullLine = sr.ReadLine(); fullLine != null; fullLine = sr.ReadLine())
                    {
                        (metaData as IDictionary<string, string>).Add(MetaEntryToKeyVal(fullLine));
                    }
                }

                return LoadCustomAtlas(atlasName, imageData, slicerData, metaData);
            }
            finally
            {
                textureStream.Close();
                slicerStream?.Close();
                metaStream?.Close();
            }
        }

        public static FAtlas LoadCustomAtlas(string atlasName, Texture2D imageData, Dictionary<string, object> slicerData, Dictionary<string, string> metaData)
        {
            // Some defaults, metadata can overwrite
            // common snense
            if (slicerData != null) // sprite atlases are mostly unaliesed
            {
                imageData.anisoLevel = 1;
                imageData.filterMode = 0;
            }
            else // Single-image should clamp
            {
                imageData.wrapMode = TextureWrapMode.Clamp;
            }

            if (metaData != null)
            {
                metaData.TryGetValue("aniso", out string anisoValue);
                if (!string.IsNullOrEmpty(anisoValue) && int.Parse(anisoValue) > -1) imageData.anisoLevel = int.Parse(anisoValue);
                metaData.TryGetValue("filterMode", out string filterMode);
                if (!string.IsNullOrEmpty(filterMode) && int.Parse(filterMode) > -1) imageData.filterMode = (FilterMode)int.Parse(filterMode);
                metaData.TryGetValue("wrapMode", out string wrapMode);
                if (!string.IsNullOrEmpty(wrapMode) && int.Parse(wrapMode) > -1) imageData.wrapMode = (TextureWrapMode)int.Parse(wrapMode);
                // Todo -  the other 100 useless params
            }

            // make singleimage atlas
            FAtlas fatlas = new FAtlas(atlasName, imageData, FAtlasManager._nextAtlasIndex);

            if (slicerData == null) // was actually singleimage
            {
                // Done
                if (Futile.atlasManager.DoesContainAtlas(atlasName))
                {
                    Debug.Log("Single-image atlas '" + atlasName + "' being replaced.");
                    Futile.atlasManager.ActuallyUnloadAtlasOrImage(atlasName); // Unload previous version if present
                }
                if (Futile.atlasManager._allElementsByName.Remove(atlasName)) Debug.Log("Element '" + atlasName + "' being replaced with new one from atlas " + atlasName);
                FAtlasManager._nextAtlasIndex++; // is this guy even used
                Futile.atlasManager.AddAtlas(fatlas); // Simple
                return fatlas;
            }

            // convert to full atlas
            fatlas._elements.Clear();
            fatlas._elementsByName.Clear();
            fatlas._isSingleImage = false;


            //ctrl c
            //ctrl v

            Dictionary<string, object> dictionary2 = (Dictionary<string, object>)slicerData["frames"];
            float resourceScaleInverse = Futile.resourceScaleInverse;
            int num = 0;
            foreach (KeyValuePair<string, object> keyValuePair in dictionary2)
            {
                FAtlasElement fatlasElement = new FAtlasElement();
                fatlasElement.indexInAtlas = num++;
                string text = keyValuePair.Key;
                if (Futile.shouldRemoveAtlasElementFileExtensions)
                {
                    int num2 = text.LastIndexOf(".");
                    if (num2 >= 0)
                    {
                        text = text.Substring(0, num2);
                    }
                }
                fatlasElement.name = text;
                IDictionary dictionary3 = (IDictionary)keyValuePair.Value;
                fatlasElement.isTrimmed = (bool)dictionary3["trimmed"];
                if ((bool)dictionary3["rotated"])
                {
                    throw new NotSupportedException("Futile no longer supports TexturePacker's \"rotated\" flag. Please disable it when creating the " + fatlas._dataPath + " atlas.");
                }
                IDictionary dictionary4 = (IDictionary)dictionary3["frame"];
                float num3 = float.Parse(dictionary4["x"].ToString());
                float num4 = float.Parse(dictionary4["y"].ToString());
                float num5 = float.Parse(dictionary4["w"].ToString());
                float num6 = float.Parse(dictionary4["h"].ToString());
                Rect uvRect = new Rect(num3 / fatlas._textureSize.x, (fatlas._textureSize.y - num4 - num6) / fatlas._textureSize.y, num5 / fatlas._textureSize.x, num6 / fatlas._textureSize.y);
                fatlasElement.uvRect = uvRect;
                fatlasElement.uvTopLeft.Set(uvRect.xMin, uvRect.yMax);
                fatlasElement.uvTopRight.Set(uvRect.xMax, uvRect.yMax);
                fatlasElement.uvBottomRight.Set(uvRect.xMax, uvRect.yMin);
                fatlasElement.uvBottomLeft.Set(uvRect.xMin, uvRect.yMin);
                IDictionary dictionary5 = (IDictionary)dictionary3["sourceSize"];
                fatlasElement.sourcePixelSize.x = float.Parse(dictionary5["w"].ToString());
                fatlasElement.sourcePixelSize.y = float.Parse(dictionary5["h"].ToString());
                fatlasElement.sourceSize.x = fatlasElement.sourcePixelSize.x * resourceScaleInverse;
                fatlasElement.sourceSize.y = fatlasElement.sourcePixelSize.y * resourceScaleInverse;
                IDictionary dictionary6 = (IDictionary)dictionary3["spriteSourceSize"];
                float left = float.Parse(dictionary6["x"].ToString()) * resourceScaleInverse;
                float top = float.Parse(dictionary6["y"].ToString()) * resourceScaleInverse;
                float width = float.Parse(dictionary6["w"].ToString()) * resourceScaleInverse;
                float height = float.Parse(dictionary6["h"].ToString()) * resourceScaleInverse;
                fatlasElement.sourceRect = new Rect(left, top, width, height);
                fatlas._elements.Add(fatlasElement);
                fatlas._elementsByName.Add(fatlasElement.name, fatlasElement);
            }

            // This currently doesn't remove elements from old atlases, just removes elements from the manager.
            bool nameInUse = Futile.atlasManager.DoesContainAtlas(atlasName);
            if (!nameInUse)
            {
                // remove duplicated elements and add atlas
                foreach (FAtlasElement fae in fatlas._elements)
                {
                    if (Futile.atlasManager._allElementsByName.Remove(fae.name)) Debug.Log("Element '" + fae.name + "' being replaced with new one from atlas " + atlasName);
                }
                FAtlasManager._nextAtlasIndex++;
                Futile.atlasManager.AddAtlas(fatlas);
            }
            else
            {
                FAtlas other = Futile.atlasManager.GetAtlasWithName(atlasName);
                bool isFullReplacement = true;
                foreach (FAtlasElement fae in other.elements)
                {
                    if (!fatlas._elementsByName.ContainsKey(fae.name)) isFullReplacement = false;
                }
                if (isFullReplacement)
                {
                    // Done, we're good, unload the old and load the new
                    Debug.Log("Atlas '" + atlasName + "' being fully replaced with custom one");
                    Futile.atlasManager.ActuallyUnloadAtlasOrImage(atlasName); // Unload previous version if present
                    FAtlasManager._nextAtlasIndex++;
                    Futile.atlasManager.AddAtlas(fatlas); // Simple
                }
                else
                {
                    // uuuugh
                    // partially unload the old
                    foreach (FAtlasElement fae in fatlas._elements)
                    {
                        if (Futile.atlasManager._allElementsByName.Remove(fae.name)) Debug.Log("Element '" + fae.name + "' being replaced with new one from atlas " + atlasName);
                    }
                    // load the new with a salted name
                    do
                    {
                        atlasName += UnityEngine.Random.Range(0, 9);
                    }
                    while (Futile.atlasManager.DoesContainAtlas(atlasName));
                    fatlas._name = atlasName;
                    FAtlasManager._nextAtlasIndex++;
                    Futile.atlasManager.AddAtlas(fatlas); // Finally
                }
            }
            return fatlas;
        }
    }
}

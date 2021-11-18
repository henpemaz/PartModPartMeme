using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.IO;
using RWCustom;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace MapExporter
{
    public class MapContent : LizardSkin.IJsonSerializable
    {
        private readonly Dictionary<string, RoomEntry> rooms;
        private readonly List<ConnectionEntry> connections;
        private readonly string name;
        private readonly string acronym;

        public MapContent(World world)
        {
            acronym = world.name;
            name = NameOfRegion(world);

            rooms = (from r in world.abstractRooms select new RoomEntry(r.name)).ToDictionary(e => e.roomName, e => e);
            connections = new List<ConnectionEntry>();

            fgcolors = new List<Color>();
            bgcolors = new List<Color>();

            DevInterface.DevUI fakeDevUi = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(DevInterface.DevUI)) as DevInterface.DevUI;
            DevInterface.MapPage fakeMapPage = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(DevInterface.MapPage)) as DevInterface.MapPage;
            fakeMapPage.owner = fakeDevUi;
            fakeDevUi.game = world.game;
            fakeMapPage.filePath = string.Concat(new object[]
            {
                    RWCustom.Custom.RootFolderDirectory(),
                    "World",
                    Path.DirectorySeparatorChar,
                    "Regions",
                    Path.DirectorySeparatorChar,
                    world.name,
                    Path.DirectorySeparatorChar,
                    "map_",
                    world.name,
                    ".txt"
            });

            LoadMapConfig(fakeMapPage);

            Type.GetType("CustomRegions.DevInterface.MapPageHook, CustomRegions")
                .GetMethod("MapPage_LoadMapConfig", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] {
                        new On.DevInterface.MapPage.orig_LoadMapConfig(LoadMapConfig),
                        fakeMapPage });

            fakeDevUi.game = null;
            fakeMapPage.owner = null;

            foreach (var room in rooms)
            {
                if(!room.Value.everParsed) { Debug.Log("Room " + room.Key + " doesn't have map data"); Debug.LogError("Room " + room.Key + " doesn't have map data"); }
            }
        }

        static float[] vec2arr(Vector2 vec) => new float[] { vec.x, vec.y };
        static float[] vec2arr(Vector3 vec) => new float[] { vec.x, vec.y, vec.z };

        public void UpdateRoom(Room room)
        {
            rooms[room.abstractRoom.name].UpdateEntry(room);
        }

        class RoomEntry : LizardSkin.IJsonSerializable
        {
            public string roomName;

            public RoomEntry(string roomName)
            {
                this.roomName = roomName;
            }
            // from map txt
            public Vector2 devPos;
            public Vector2 canPos;
            public int canLayer;
            public int subregion;
            public bool everParsed = false;
            public void ParseEntry(string line)
            {
                //Debug.Log(line);
                string[] arr = Regex.Split(line, ": ");
                if (roomName != arr[0]) throw new Exception();
                string[] arr2 = Regex.Split(arr[1], ",");
                canPos.x = float.Parse(arr2[0]);
                canPos.y = float.Parse(arr2[1]);
                devPos.x = float.Parse(arr2[2]);
                devPos.y = float.Parse(arr2[3]);
                canLayer = int.Parse(arr2[4]);
                subregion = int.Parse(arr2[5]);
                everParsed = true;
            }

            // from room
            public Vector2[] cameras;
            private int[] size;

            public void UpdateEntry(Room room)
            {
                cameras = room.cameraPositions;

                this.size = new int[] { room.Width, room.Height };
            }

            // wish there was a better way to do this
            public Dictionary<string, object> ToJson()
            {
                return new Dictionary<string, object>()
                    {
                        { "roomName", roomName },
                        { "canPos", vec2arr(canPos) },
                        { "canLayer", canLayer },
                        { "devPos", vec2arr(devPos) },
                        { "subregion", subregion },
                        { "cameras", cameras != null ? (from c in cameras select vec2arr(c)).ToArray() : null},
                    { "size", size}
                    };
            }
        }


        class ConnectionEntry : LizardSkin.IJsonSerializable
        {
            public string roomA;
            public string roomB;
            public IntVector2 posA;
            public IntVector2 posB;
            public int dirA;
            public int dirB;

            public ConnectionEntry(string line)
            {
                string[] stuff = Regex.Split(Regex.Split(line, ": ")[1], ",");
                this.roomA = stuff[0];
                this.roomB = stuff[1];
                this.posA = new IntVector2(int.Parse(stuff[2]), int.Parse(stuff[3]));
                this.posB = new IntVector2(int.Parse(stuff[4]), int.Parse(stuff[5]));
                this.dirA = int.Parse(stuff[6]);
                this.dirB = int.Parse(stuff[7]);
            }

            public Dictionary<string, object> ToJson()
            {
                return new Dictionary<string, object>()
                {
                    { "roomA", roomA },
                    { "roomB", roomB },
                    { "posA", posA },
                    { "posB", posB },
                    { "dirA", dirA },
                    { "dirB", dirB },
                };
            }
        }

        public void LoadMapConfig(DevInterface.MapPage fakeMapPage)
        {
            if (!File.Exists(fakeMapPage.filePath)) return;
            Debug.Log("reading map file: " + fakeMapPage.filePath);
            string[] contents = File.ReadAllLines(fakeMapPage.filePath);
            foreach (var s in contents)
            {
                string sname = Regex.Split(s, ": ")[0];
                if (rooms.TryGetValue(sname, out RoomEntry room)) room.ParseEntry(s);
                if (sname == "Connection") this.connections.Add(new ConnectionEntry(s));
            }
        }

        private string NameOfRegion(World world)
        {
            string text = null;
            // copypaste from game
            switch (world.name)
            {
                case "CC":
                    text = "Chimney Canopy";
                    break;
                case "DS":
                    text = "Drainage System";
                    break;
                case "HI":
                    text = "Industrial Complex";
                    break;
                case "GW":
                    text = "Garbage Wastes";
                    break;
                case "SI":
                    text = "Sky Islands";
                    break;
                case "SU":
                    text = "Outskirts";
                    break;
                case "SH":
                    text = "Shaded Citadel";
                    break;
                case "SL":
                    text = "Shoreline";
                    break;
                case "LF":
                    text = "Farm Arrays";
                    break;
                case "UW":
                    text = "The Exterior";
                    break;
                case "SB":
                    text = "Subterranean";
                    break;
                case "SS":
                    text = "Five Pebbles";
                    break;
            }

            // copypaste from csr
            string regID = world.name;
            bool customRegion = true;
            var vanillaRegions = CustomRegions.Mod.CustomWorldMod.VanillaRegions();
            for (int i = 0; i < vanillaRegions.Length; i++)
            {
                if (regID == vanillaRegions[i])
                {
                    customRegion = false;
                }
            }
            if (customRegion)
            {
                string fullRegionName = "N / A";
                //CustomWorldMod.activatedPacks.TryGetValue(text2, out fullRegionName);
                if (CustomRegions.Mod.CustomWorldMod.activeModdedRegions.Contains(regID))
                {
                    foreach (KeyValuePair<string, string> entry in CustomRegions.Mod.CustomWorldMod.activatedPacks)
                    {
                        if (CustomRegions.Mod.CustomWorldMod.installedPacks[entry.Key].regions.Contains(regID))
                        {
                            string regionName = (string)Type.GetType("CustomRegions.CWorld.RegionHook, CustomRegions")
                                .GetMethod("GetSubRegionName", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static)
                                .Invoke(null, new object[] {
                                        entry.Value,
                                        regID });
                            //string regionName = CustomRegions.CWorld.RegionHook.GetSubRegionName(entry.Value, regID);
                            if (CustomRegions.Mod.CustomWorldMod.installedPacks[entry.Key].useRegionName && regionName != null)
                            {
                                fullRegionName = regionName;
                            }
                            else
                            {
                                fullRegionName = entry.Key;
                            }
                            break;
                        }
                    }
                }
                text = fullRegionName;
            }
            return text;
        }

        public Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>()
            {
                { "name", name },
                { "acronym", acronym },
                { "rooms", rooms },
                { "connections", connections },
                { "fgcolors" , (from s in fgcolors select  vec2arr((Vector3)(Vector4)s)).ToList()},
                { "bgcolors" , (from s in bgcolors select  vec2arr((Vector3)(Vector4)s)).ToList()},
            };
        }

        internal void LogPalette(RoomPalette currentPalette)
        {
            // get sky color and fg color (px 00 and 07)
            Color fg = currentPalette.texture.GetPixel(0, 0);
            Color bg = currentPalette.texture.GetPixel(0, 7);
            this.fgcolors.Add(fg);
            this.bgcolors.Add(bg);
        }

        List<Color> fgcolors;
        List<Color> bgcolors;
    }
}

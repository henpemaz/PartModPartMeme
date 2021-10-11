using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoMod.RuntimeDetour;
using Partiality.Modloader;
using System.Reflection;
using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;
using System.IO;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace CustomSpritesLoader
{
    public partial class CustomSpritesLoader : PartialityMod
    {
        const string modid = "CustomSpritesLoader";
        public CustomSpritesLoader()
        {
            this.ModID = modid;
            this.Version = "1.2";
            this.author = "Henpemaz";

            instance = this;
        }

        public static string description =
@"CustomSpritesLoader mod by Henpemaz
Allows you to load, replace, or partially overwrite game sprites in a much much much easier way than before.

Sprites or atlases placed inside the Load folder will be automatically loaded when the game starts and will be available for mods to use (ie FancySlugcats) or will be used instead of vanilla assets. Elements with names that match vanilla elements will effectivelly overwrite the old ones. These atlases will not be unloaded by the game and can cause performance issues.

Sprites or atlases placed inside the Replace folder will be loaded instead when the game attempts to load an atlas or image of the same name, it is important that it has the same format (atlas or single image) that the game expects to find and that in the case of an atlas it has all the Elements that the original one had. These atlases can be unloaded by the game if the vanilla ones would be unloaded at any point.

You can organize your sprites in sub-folders inside of Load or Replace. If a folder's name starts with ""_"" (underscore), or if the folder contains a file named ""disabled.txt"", then that folder and sub-folders are ignored.

Note to FancySlugcats users: You can simply put your all your atlases inside `CustomSpritesLoader\Load` instead of adding files to `CustomHeads` and atlases to `Futile\Atlases`, and you'll be able to use them the same way.

Happy modding
";


        public static CustomSpritesLoader instance;

        //private static bool initialLoadLock;
        public override void OnEnable()
        {
            base.OnEnable();

            CheckMyFolders();

            //initialLoadLock = true;
            On.RainWorld.LoadResources += RainWorld_LoadResources_hk;
            //On.FAtlasManager.ActuallyUnloadAtlasOrImage += FAtlasManager_ActuallyUnloadAtlasOrImage;
            On.FAtlasManager.AddAtlas += FAtlasManager_AddAtlas_fix;

            On.FAtlasManager.LoadAtlasFromTexture += FAtlasManager_LoadAtlasFromTexture;
            On.FAtlasManager.LoadAtlasFromTexture_1 += FAtlasManager_LoadAtlasFromTexture_1;
            On.FAtlasManager.LoadAtlas += FAtlasManager_LoadAtlas;
            On.FAtlasManager.LoadImage += FAtlasManager_LoadImage;
            On.FAtlasManager.ActuallyLoadAtlasOrImage += FAtlasManager_ActuallyLoadAtlasOrImage;
        }

        private FAtlas FAtlasManager_ActuallyLoadAtlasOrImage(On.FAtlasManager.orig_ActuallyLoadAtlasOrImage orig, FAtlasManager self, string name, string imagePath, string dataPath)
        {
            FAtlas replacement = TryLoadReplacement(name);
            if (replacement != null) return replacement;
            return orig(self, name, imagePath, dataPath);
        }

        private FAtlas FAtlasManager_LoadImage(On.FAtlasManager.orig_LoadImage orig, FAtlasManager self, string imagePath)
        {
            FAtlas replacement = TryLoadReplacement(imagePath);
            if (replacement != null) return replacement;
            return orig(self, imagePath);
        }

        private FAtlas FAtlasManager_LoadAtlas(On.FAtlasManager.orig_LoadAtlas orig, FAtlasManager self, string atlasPath)
        {
            FAtlas replacement = TryLoadReplacement(atlasPath);
            if (replacement != null) return replacement;
            return orig(self, atlasPath);
        }

        private FAtlas FAtlasManager_LoadAtlasFromTexture_1(On.FAtlasManager.orig_LoadAtlasFromTexture_1 orig, FAtlasManager self, string name, string dataPath, Texture texture)
        {
            FAtlas replacement = TryLoadReplacement(name);
            if (replacement != null)
            {
                CopyTextureSettingsToAtlas(texture, replacement);
                return replacement;
            }
            return orig(self, name, dataPath,texture);
        }

        private FAtlas FAtlasManager_LoadAtlasFromTexture(On.FAtlasManager.orig_LoadAtlasFromTexture orig, FAtlasManager self, string name, Texture texture)
        {
            FAtlas replacement = TryLoadReplacement(name);
            if (replacement != null)
            {
                CopyTextureSettingsToAtlas(texture, replacement);
                return replacement;
            }
            return orig (self, name, texture);
        }

        private void CopyTextureSettingsToAtlas(Texture from, FAtlas to)
        {
            if (from == null || to == null || to.texture == null) return;
            to._texture.wrapMode = from.wrapMode;
            to._texture.anisoLevel = from.anisoLevel;
            to._texture.filterMode = from.filterMode;
            // more ?
        }

        public FAtlas TryLoadReplacement(string atlasname)
        {
            if (atlasname.StartsWith("Atlases/"))
            {
                atlasname = atlasname.Substring(8);
                if (!DoIHaveAReplacementForThis(atlasname)) return null;
                if (!ShouldAtlasBeLoadedWithPrefix(atlasname)) knownPrefixedAtlases.Add(atlasname);
            } else if (!DoIHaveAReplacementForThis(atlasname)) return null;
            try
            {
                Debug.Log("CustomSpritesLoader: Loading replacement for " + atlasname);
                string actualatlasname = ShouldAtlasBeLoadedWithPrefix(atlasname) ? "Atlases/" + atlasname : atlasname;
                return CustomAtlasLoader.ReadAndLoadCustomAtlas(atlasname, new FileInfo(knownAtlasReplacements[atlasname]).DirectoryName, actualatlasname);
            }
            catch (Exception e)
            {
                Debug.LogError("CustomSpritesLoader: Error loading replacement atlas " + atlasname + ", skipping");
                Debug.LogException(e);
                return null;
            }
        }

        public bool DoIHaveAReplacementForThis(string atlasname)
        {
            //if (initialLoadLock) return false;
            if (knownAtlasReplacements.ContainsKey(atlasname)) return true;
            return false;
        }

        //List<string> knownAtlasReplacements = new List<string>();
        Dictionary<string, string> knownAtlasReplacements = new Dictionary<string, string>();

        private void CheckMyFolders()
        {
            Directory.CreateDirectory(CustomSpritesLoaderFolder);
            FileInfo readme = new FileInfo(Path.Combine(CustomSpritesLoaderFolder, "Readme.txt"));
            if(!readme.Exists || readme.Length != description.Length)
            {
                StreamWriter readmeWriter = readme.CreateText();
                readmeWriter.Write(description);
                readmeWriter.Flush();
                readmeWriter.Close();
            }
            Directory.CreateDirectory(LoadAtlasesFolder);
            File.Create(Path.Combine(LoadAtlasesFolder, "Place new atlases to be automatically loaded here"));
            Directory.CreateDirectory(ReplaceAtlasesFolder);
            File.Create(Path.Combine(ReplaceAtlasesFolder, "Place full atlas or image replacements here"));

            Debug.Log("CustomSpritesLoader: Scanning for atlas replacements");
            DirectoryInfo atlasesFolder = new DirectoryInfo(ReplaceAtlasesFolder);
            FileInfo[] atlasFiles = atlasesFolder.GetFiles("*.png", SearchOption.AllDirectories);
            foreach (FileInfo atlasFile in atlasFiles)
            {
                if (IsDirectoryDisabled(atlasFile, atlasesFolder)) continue;
                if (!atlasFile.Name.EndsWith(".png")) continue; // fake results ffs
                string basename = atlasFile.Name.Substring(0, atlasFile.Name.Length - 4); // remove .png
                knownAtlasReplacements.Add(basename, atlasFile.FullName);
                Debug.Log("CustomSpritesLoader: Atlas replacement " + basename + " registered");
            }
            Debug.Log("CustomSpritesLoader: Done scanning");
        }

        private bool IsDirectoryDisabled(FileInfo file, DirectoryInfo root)
        {
            DirectoryInfo parentDir = file.Directory;
            while(String.Compare(parentDir.FullName, root.FullName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                if (parentDir.Name.StartsWith("_") || File.Exists(Path.Combine(parentDir.FullName, "disabled.txt")) || File.Exists(Path.Combine(parentDir.FullName, "disabled.txt.txt")) || File.Exists(Path.Combine(parentDir.FullName, "disabled"))) return true;
                parentDir = parentDir.Parent;
            }
            return false;
        }

        private void FAtlasManager_AddAtlas_fix(On.FAtlasManager.orig_AddAtlas orig, FAtlasManager self, FAtlas atlas)
        {
            // Prevent elements being overwritten.
            List<KeyValuePair<string, FAtlasElement>> duplicates = new List<KeyValuePair<string, FAtlasElement>>();
            foreach (KeyValuePair<string, FAtlasElement> entry in atlas._elementsByName)
            {
                if (self._allElementsByName.ContainsKey(entry.Key)) // clash
                {
                    duplicates.Add(entry);
                }
            }
            foreach (KeyValuePair<string, FAtlasElement> dupe in duplicates)
            {
                Debug.Log("CustomSpritesLoader: Preventing duplicate element '" + dupe.Key + "' from being loaded");
                atlas._elements.Remove(dupe.Value);
                atlas._elementsByName.Remove(dupe.Key);
            }

            orig(self, atlas);
        }

        //private void FAtlasManager_ActuallyUnloadAtlasOrImage(On.FAtlasManager.orig_ActuallyUnloadAtlasOrImage orig, FAtlasManager self, string name)
        //{
        //    if (loadedCustomAtlases.Contains(name)) return; // Prevent unloading of custom loaded assets :/
        //    orig(self, name);
        //}

        private bool ShouldAtlasBeLoadedWithPrefix(string atlasname)
        {
            if (knownPrefixedAtlases.Contains(atlasname)) return true;
            return false;
        }

        /// <summary>
        /// Atlases that get the "Atlases/" prefix
        /// </summary>
        public static List<string> knownPrefixedAtlases = new List<string> {
            "outPostSkulls",
            "rainWorld",
            "fontAtlas",
            "uiSprites",
            "shelterGate",
            "regionGate",
            "waterSprites"
        };

        private static string _customSpritesLoaderFolder;
        public static string CustomSpritesLoaderFolder => _customSpritesLoaderFolder != null ? _customSpritesLoaderFolder : _customSpritesLoaderFolder = string.Concat(new object[] 
        { 
            RWCustom.Custom.RootFolderDirectory(),
            "ModConfigs",
            Path.DirectorySeparatorChar,
            modid
        });

        private static string _loadAtlasesFolder;
        public static string LoadAtlasesFolder => _loadAtlasesFolder != null ? _loadAtlasesFolder : _loadAtlasesFolder = string.Concat(new object[]
        {
            CustomSpritesLoaderFolder,
            Path.DirectorySeparatorChar,
            "Load"
        });
        private static string _replaceAtlasesFolder;
        public static string ReplaceAtlasesFolder => _replaceAtlasesFolder != null ? _replaceAtlasesFolder : _replaceAtlasesFolder = string.Concat(new object[]
        {
            CustomSpritesLoaderFolder,
            Path.DirectorySeparatorChar,
            "Replace"
        });

        private void RainWorld_LoadResources_hk(On.RainWorld.orig_LoadResources orig, RainWorld self)
        {
            orig(self);
            LoadCustomAtlases();
        }

        public static KeyValuePair<string, string> MetaEntryToKeyVal(string input)
        {
            if(string.IsNullOrEmpty(input)) return new KeyValuePair<string, string>("", "");
            string[] pieces = input.Split(new char[] { ':' }, 2); // No trim option in framework 3.5
            if (pieces.Length == 0) return new KeyValuePair<string, string>("", "");
            if (pieces.Length == 1) return new KeyValuePair<string, string>(pieces[0].Trim(), "");
            return new KeyValuePair<string, string>(pieces[0].Trim(), pieces[1].Trim());
        }

        private void LoadCustomAtlases()
        {
            Debug.Log("CustomSpritesLoader: LoadCustomAtlases");
            DirectoryInfo atlasesFolder = new DirectoryInfo(LoadAtlasesFolder);
            FileInfo[] atlasFiles = atlasesFolder.GetFiles("*.png", SearchOption.AllDirectories);
            foreach (FileInfo atlasFile in atlasFiles)
            {
                if (IsDirectoryDisabled(atlasFile, atlasesFolder)) continue;
                if (!atlasFile.Name.EndsWith(".png")) continue; // fake results ffs
                try
                {
                    string basename = atlasFile.Name.Substring(0, atlasFile.Name.Length - 4); // remove .png
                    string atlasname = ShouldAtlasBeLoadedWithPrefix(basename) ? "Atlases/" + basename : basename;
                    CustomAtlasLoader.ReadAndLoadCustomAtlas(basename, atlasFile.Directory.FullName, atlasname);
                }
                catch (Exception e)
                {
                    Debug.LogError("CustomSpritesLoader: Error loading custom atlas data for file " + atlasFile.Name + ", skipping");
                    Debug.LogException(e);
                }
            }
            //initialLoadLock = false;
        }
    }
}

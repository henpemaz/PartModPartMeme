using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using UnityEngine;
namespace ConcealedGarden
{
    public class ConcealedGardenProgression
    {
        private static RainWorld rw;
        private static bool SlugBaseExists;
        internal static void Apply()
        {
            On.RainWorld.Start += RainWorld_Start;
            // General progression
            // load or start fresh
            On.PlayerProgression.LoadProgression += PlayerProgression_LoadProgression;
            On.PlayerProgression.InitiateProgression += PlayerProgression_InitiateProgression;

            // Savestate instantiation/fetch
            //On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitiateSaveState; // Slugbase doesn't call orig on this, moved to start
            On.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGame; // lil bugfix

            // Reverts and clears
            On.PlayerProgression.WipeAll += PlayerProgression_WipeAll;
            On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;
            // On.PlayerProgression.Revert += // reverts temp map data, not sure if relevant since it's not saving it and it'll be loading things from disk again

            // Saving
            //On.PlayerProgression.SaveDeathPersistentDataOfCurrentState += PlayerProgression_SaveDeathPersistentDataOfCurrentState; // Slugbase doesn't call orig on this, moved to start
            //On.PlayerProgression.SaveToDisk += PlayerProgression_SaveToDisk; // Slugbase doesn't call orig on this, moved to start

            // First call to progression ctor happens before we hook our hooks. OI initialization must call LoadOIsMisc();
            progression = new ConcealedGardenProgression();
        }


        #region HOOKS

        // HOOKS
        // General behavior: When saving/loading, save/load game first; when wiping, wipe mod first.
        // this way, the game's current state is always avaiable for mods to read.
        private static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            Debug.Log("CG Progression: Start hook");
            rw = self;
            On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitiateSaveState; // Slugbase doesn't call orig on this, moved to start
            On.PlayerProgression.SaveDeathPersistentDataOfCurrentState += PlayerProgression_SaveDeathPersistentDataOfCurrentState; // Slugbase doesn't call orig on this, moved to start
            On.PlayerProgression.SaveToDisk += PlayerProgression_SaveToDisk; // Slugbase doesn't call orig on this, moved to start

            foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "SlugBase")
                {
                    SlugBaseExists = true;
                    Debug.Log("CG Progression: Found slugbase");
                }
            }

            orig(self);
        }

        // Called trying to load a file
        internal static void PlayerProgression_LoadProgression(On.PlayerProgression.orig_LoadProgression orig, PlayerProgression self)
        {
            Debug.Log("CG Progression: LoadProgression hook");
            orig(self);
            LoadOIsProgression();
        }

        // Called when there's no file to load
        internal static void PlayerProgression_InitiateProgression(On.PlayerProgression.orig_InitiateProgression orig, PlayerProgression self)
        {
            Debug.Log("CG Progression: InitiateProgression hook");
            orig(self);
            InitiateOIsProgression();
        }

        internal static void PlayerProgression_WipeAll(On.PlayerProgression.orig_WipeAll orig, PlayerProgression self)
        {

            Debug.Log("CG Progression: WipeAll hook");
            WipeOIsProgression(-1);
            orig(self);
        }

        private static bool getOrInitSavePersLock = false;
        // Called with saveAsDeathOrQuit=true from StoryGameSession; =false from loading Red's statistics
        internal static SaveState PlayerProgression_GetOrInitiateSaveState(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, int saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
        {
            Debug.Log("CG Progression: GetOrInitiateSaveState hook");
            bool loadedFromStarve = self.currentSaveState == null && self.starvedSaveState != null && self.starvedSaveState.saveStateNumber == saveStateNumber;
            bool loadedFromMemory = loadedFromStarve || (self.currentSaveState != null && self.currentSaveState.saveStateNumber == saveStateNumber);

            getOrInitSavePersLock = true;
            SaveState saveState = orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
            getOrInitSavePersLock = false;
            LoadOIsSave(saveState, loadedFromMemory, loadedFromStarve);
            if (saveAsDeathOrQuit) SaveOIsPers(true, true);

            return saveState;
        }

        internal static void SlugcatSelectMenu_StartGame(On.Menu.SlugcatSelectMenu.orig_StartGame orig, Menu.SlugcatSelectMenu self, int storyGameCharacter)
        {
            Debug.Log("CG Progression: StartGame hook");
            orig(self, storyGameCharacter);
            // Bugfix to prevent crazy inconsistency that could happen if played restarted a save they just starved on
            // (vanilla would call 'Wipe' and still load the starve which is clearly a bug)

            if (self.manager.menuSetup.startGameCondition == ProcessManager.MenuSetup.StoryGameInitCondition.New)
                self.manager.rainWorld.progression.starvedSaveState = null;
        }

        internal static void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, int saveStateNumber)
        {
            Debug.Log("CG Progression: WipeSaveState hook");
            WipeOIsProgression(saveStateNumber);
            orig(self, saveStateNumber);
        }

        internal static void PlayerProgression_SaveToDisk(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self, bool saveCurrentState, bool saveMaps, bool saveMiscProg)
        {
            Debug.Log("CG Progression: SaveToDisk hook");
            orig(self, saveCurrentState, saveMaps, saveMiscProg);
            SaveOIsProgression(saveCurrentState, saveCurrentState, saveMiscProg);
        }

        internal static void PlayerProgression_SaveDeathPersistentDataOfCurrentState(On.PlayerProgression.orig_SaveDeathPersistentDataOfCurrentState orig, PlayerProgression self, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            Debug.Log("CG Progression: SaveDeathPersistentDataOfCurrentState hook");
            orig(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);
            if (getOrInitSavePersLock) { /*Debug.Log("CG Progression: locked from running twice");*/ return; }
            SaveOIsPers(saveAsIfPlayerDied, saveAsIfPlayerQuit);
        }

        #endregion HOOKS

        internal static void RunPreSave()
        {
            Debug.Log("CG Progression: RunPreSave");
            progression.ProgressionPreSave();
        }

        internal static void RunPostLoaded()
        {
            Debug.Log("CG Progression: RunPostLoaded");
            progression.ProgressionLoaded();
        }

        internal static void LoadOIsProgression()
        {
            Debug.Log("CG Progression: LoadOIsProgression");
            progression.LoadProgression();
            RunPostLoaded();
        }

        internal static void InitiateOIsProgression()
        {
            Debug.Log("CG Progression: InitiateOIsProgression");
            RunPreSave();
            progression.InitProgression();
            RunPostLoaded();
        }


        internal static void WipeOIsProgression(int saveStateNumber)
        {
            Debug.Log("CG Progression: WipeOIsProgression");
            RunPreSave();
            progression.WipeProgression(saveStateNumber); // Todo add a chance to clear/keep misc data ?
            RunPostLoaded();
        }

        internal static void LoadOIsSave(SaveState saveState, bool loadedFromMemory, bool loadedFromStarve)
        {
            Debug.Log("CG Progression: LoadOIsSave");
            if (loadedFromMemory) { Debug.Log("CG Progression: LoadOIsSave loadedFromMemory"); return; } // We're good ? Not too sure when this happens
            progression.LoadSaveState();
            RunPostLoaded();
        }

        internal static void SaveOIsProgression(bool saveState, bool savePers, bool saveMisc)
        {
            Debug.Log("CG Progression: SaveOIsProgression");
            RunPreSave();
            progression.SaveProgression(saveState, savePers, saveMisc);
        }

        internal static void SaveOIsPers(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            Debug.Log("CG Progression: SaveOIsPers");
            if (rw.progression.currentSaveState == null) { Debug.Log("CG Progression: No progression to save"); return; }
            if (!(saveAsIfPlayerDied || saveAsIfPlayerQuit))
            {
                RunPreSave();
            }
            if (saveAsIfPlayerDied || saveAsIfPlayerQuit)
                progression.SaveDeath(saveAsIfPlayerDied, saveAsIfPlayerQuit);
            else
                progression.SaveProgression(false, true, false);
        }

        internal static ConcealedGardenProgression progression;
        private Dictionary<string, object> saveDict;
        private Dictionary<string, object> persDict;
        private Dictionary<string, object> miscDict;
        private Dictionary<string, object> globalDict;

        public ConcealedGardenProgression(Dictionary<string, object> saveDict = null, Dictionary<string, object> persDict = null, Dictionary<string, object> miscDict = null, Dictionary<string, object> globalDict = null)
        {
            Debug.Log("CG Progression: ConcealedGardenProgression");
            this.saveDict = saveDict ?? new Dictionary<string, object>();
            this.persDict = persDict ?? new Dictionary<string, object>();
            this.miscDict = miscDict ?? new Dictionary<string, object>();
            this.globalDict = globalDict ?? new Dictionary<string, object>();
        }

        public bool transfurred // transformed
        {
            get { if (persDict.TryGetValue("transfurred", out object obj)) return (bool)obj; return false; }
            internal set { persDict["transfurred"] = value; everBeaten = true; }
        }

        public bool fishDream
        {
            get { if (persDict.TryGetValue("fishDream", out object obj)) return (bool)obj; return false; }
            internal set { persDict["fishDream"] = value; }
        }

        public bool everBeaten
        {
            get { if (globalDict.TryGetValue("everBeaten", out object obj)) return (bool)obj; return false; }
            internal set { globalDict["everBeaten"] = value; SaveGlobalData(); }
        }

        public bool achievementEcho
        {
            get { if (globalDict.TryGetValue("achievementEcho", out object obj)) return (bool)obj; return false; }
            internal set { globalDict["achievementEcho"] = value; SaveGlobalData(); }
        }

        public bool achievementTransfurred
        {
            get { if (globalDict.TryGetValue("achievementTransfurred", out object obj)) return (bool)obj; return false; }
            internal set { globalDict["achievementTransfurred"] = value; SaveGlobalData(); }
        }

        internal void LoadProgressionDicts()
        {
            Debug.Log("CG Progression: LoadProgressionDicts");
            LoadGlobalData();
            Debug.Log("CG Progression loading with:");
            Debug.Log($"saveData :{saveData}");
            Debug.Log($"persData :{persData}");
            Debug.Log($"miscData :{miscData}");
            Debug.Log($"data : {data}");

            //progression = new ConcealedGardenProgression();
            saveDict = ((!string.IsNullOrEmpty(saveData) && Json.Deserialize(saveData) is Dictionary<string, object> storedSd) ? storedSd : new Dictionary<string, object>());
            persDict = ((!string.IsNullOrEmpty(persData) && Json.Deserialize(persData) is Dictionary<string, object> storedPd) ? storedPd : new Dictionary<string, object>());
            miscDict = ((!string.IsNullOrEmpty(miscData) && Json.Deserialize(miscData) is Dictionary<string, object> storedMd) ? storedMd : new Dictionary<string, object>());

        }
        internal void SaveProgressionDicts()
        {
            Debug.Log("CG Progression: SaveProgressionDicts");
            SaveGlobalData();
            saveData = Json.Serialize(progression.saveDict);
            persData = Json.Serialize(progression.persDict);
            miscData = Json.Serialize(progression.miscDict);
            Debug.Log("CG Progression saved with:");
            Debug.Log($"saveData :{saveData}");
            Debug.Log($"persData :{persData}");
            Debug.Log($"miscData :{miscData}");
            Debug.Log($"data : {data}");
        }

        internal void SaveGlobalData()
        {
            Debug.Log("CG Progression: SaveGlobalData");
            data = Json.Serialize(globalDict);
            SaveData();
        }

        internal void LoadGlobalData()
        {
            Debug.Log("CG Progression: LoadGlobalData");
            LoadData();
            globalDict = ((!string.IsNullOrEmpty(data) && Json.Deserialize(data) is Dictionary<string, object> storedData) ? storedData : new Dictionary<string, object>());
        }

        protected void ProgressionLoaded()
        {
            Debug.Log("CG Progression: ProgressionLoaded");
            LoadProgressionDicts();
            LizardSkin.LizardSkin.SetCGEverBeaten(progression.everBeaten);
            LizardSkin.LizardSkin.SetCGStoryProgression(progression.transfurred ? 1 : 0);
        }

        protected void ProgressionPreSave()
        {
            Debug.Log("CG Progression: ProgressionPreSave");
            SaveProgressionDicts();
        }

        // Copied and adapted from configmachine 
        #region customData

        /// <summary>
        /// Default <see cref="data"/> of this mod. If this isn't needed, just leave it be.
        /// </summary>
        public virtual string defaultData
        {
            get { return string.Empty; }
        }

        private string _data;

        /// <summary>
        /// Data tied to nothing. Stays even if the user changed Saveslot. Useful for keeping extra data for mod settings.
        /// This won't get saved or loaded automatically and you have to call it by yourself.
        /// Set this to whatever you want and call <see cref="SaveData"/> and <see cref="LoadData"/> when you need.
        /// Causes a call to <see cref="DataOnChange"/> when its value is changed.
        /// </summary>
        public string data
        {
            get { return _data; }
            set { if (_data != value) { _data = value; DataOnChange(); } }
        }

        /// <summary>
        /// Event when either <see cref="data"/> is changed. Override it to add your own behavior.
        /// This is called when 1. You run <see cref="LoadData"/>, 2. Your mod changes <see cref="data"/>.
        /// </summary>
        public virtual void DataOnChange()
        {
        }

        /// <summary>
        /// Load your raw data from ConfigMachine Mod.
        /// Call this by your own.
        /// Check <see cref="progDataTinkered"/> to see if saved data is tinkered or not.
        /// </summary>
        public virtual void LoadData()
        {
            Debug.Log("CG Progression: LoadData");
            _data = defaultData;
            if (!directory.Exists) { Debug.Log("CG Progression: directory didn't exist, using defaultData"); DataOnChange(); return; }
            try
            {
                //Debug.Log("CG Progression: scanning for data file");
                string data = string.Empty;
                foreach (FileInfo file in directory.GetFiles())
                {
                    if (file.Name != "data.txt") { continue; }
                    //Debug.Log("CG Progression: data found, reading...");
                    //LoadData:
                    data = File.ReadAllText(file.FullName, System.Text.Encoding.UTF8);
                    //Debug.Log("CG Progression: got raw :" + data);
                    string key = data.Substring(0, 32);
                    data = data.Substring(32, data.Length - 32);
                    if (Custom.Md5Sum(data) != key)
                    {
                        Debug.Log("CG Progression: data tinkered");
                        dataTinkered = true;
                    }
                    else { /*Debug.Log("CG Progression: key match");*/ dataTinkered = false; }
                    //Debug.Log("CG Progression: decrypt using :" + CryptoDataKey);
                    _data = CompletelyOptional.Crypto.DecryptString(data, CryptoDataKey);
                    //Debug.Log("CG Progression: got decrypted :"+ _data);
                    DataOnChange();
                    return;
                }
            }
            catch (Exception ex) { Debug.Log("CG Progression: something bad happened :" + ex.ToString()); Debug.LogException(ex); }
            Debug.Log("CG Progression: data not found, using defaultData");
            DataOnChange();
        }

        /// <summary>
        /// If you want to see whether your <see cref="data"/> is tinkered or not.
        /// </summary>
        public bool dataTinkered { get; private set; } = false;

        private string CryptoDataKey => "OptionalData" + ConcealedGarden.instance.ModID;

        /// <summary>
        /// Save your raw <see cref="data"/> in file. Call this by your own.
        /// </summary>
        /// <returns>Whether it succeed or not</returns>
        public virtual bool SaveData()
        {
            Debug.Log("CG Progression: SaveData");
            if (!directory.Exists) { Debug.Log("CG Progression: directory didn't exist, creating"); directory.Create(); directory.Refresh(); }

            try
            {
                //Debug.Log("CG Progression: data is :" + _data);
                string path = string.Concat(new object[] {
                directory.FullName,
                "data.txt"
                });
                //Debug.Log("CG Progression: saving to :" + path);
                //Debug.Log("CG Progression: encrypting with :"+ CryptoDataKey);
                string enc = CompletelyOptional.Crypto.EncryptString(_data ?? "", CryptoDataKey);
                //Debug.Log("CG Progression: encrypted content :" + enc);
                string key = Custom.Md5Sum(enc);
                //Debug.Log("CG Progression: hash key :"+key);

                //Debug.Log("CG Progression: saving :" + (key+enc));
                File.WriteAllText(path, key + enc);

                Debug.Log("CG Progression: saved data");
                return true;
            }
            catch (Exception ex) { Debug.Log("CG Progression: something bad happened :"+ex.ToString()); Debug.LogException(ex); }

            Debug.Log("CG Progression: couldn't save data");
            return false;
        }

        #endregion customData

        #region progData
        // Progression API
        // Data that is stored/retrieved mimicking the game's behavior
        // save -> SaveState
        // pers -> DeathPersistentData
        // misc -> Progression & MiscProgressionData
        // See https://media.discordapp.net/attachments/849525819855863809/877611189444698113/unknown.png


        private class InvalidSlugcatException : ArgumentException
        {
            public InvalidSlugcatException() : base($"ConcealedGarden tried to use an invalid Slugcat number") { }
        }

        /// <summary>
        /// Progression API: Whether the Progression Data subsystem is used by the mod or not.
        /// Set this to true to enable saving and loading <see cref="saveData"/>, <see cref="persData"/> and <see cref="miscData"/>
        /// </summary>
        //internal protected bool hasProgData = false;

        /// <summary>
        /// If you want to see whether your most recently loaded <see cref="saveData"/>, <see cref="persData"/> or <see cref="miscData"/> was tinkered or not.
        /// When this happens defaultData will be used instead of loading from the file.
        /// </summary>
        public bool progDataTinkered { get; private set; } = false;


        private string _saveData;
        private string _persData;
        private string _miscData;

        /// <summary>
        /// Progression API: Savedata tied to a specific slugcat's playthrough. This gets reverted automatically if the Slugcat loses.
        /// Enable <see cref="hasProgData"/> to use this. Set this to whatever you want in game. Config Machine will then manage saving automatically.
        /// Typically saveData is only saved when the slugcat hibernates. Exceptionally, it's saved when the player uses a passage, and on Red's ascension/gameover
        /// </summary>
        public string saveData
        {
            get
            {
                //if (!hasProgData) throw new NoProgDataException(this);
                return _saveData;
            }
            set
            {
                //if (!hasProgData) throw new NoProgDataException(this);
                if (_saveData != value) { _saveData = value; Debug.Log("CG Progression: _saveData set with :" + _saveData); }
            }
        }

        /// <summary>
        /// Progression API: Savedata tied to a specific slugcat's playthrough, death-persistent.
        /// Enable <see cref="hasProgData"/> to use this. Set this to whatever you want in game. Config Machine will then manage saving automatically.
        /// Typically persData is saved when 1. going in-game (calls <see cref="SaveDeath"/>) 2. Surviving/dying/quitting/meeting an echo/ascending etc.
        /// </summary>
        public string persData
        {
            get
            {
                //if (!hasProgData) throw new NoProgDataException(this);
                return _persData;
            }
            set
            {
                //if (!hasProgData) throw new NoProgDataException(this);
                if (_persData != value) { _persData = value; Debug.Log("CG Progression: _persData set with :" + _persData); }
            }
        }

        /// <summary>
        /// Progression API: Savedata shared across all slugcats on the same Saveslot.
        /// Enable <see cref="hasProgData"/> to use this. Set this to whatever you want in game. Config Machine will then manage saving automatically.
        /// Typically miscData is saved when saving/starving/dying/meeting an echo/ascending etc, but not when quitting the game to the menu.
        /// </summary>
        public string miscData
        {
            get
            {
                //if (!hasProgData) throw new NoProgDataException(this);
                return _miscData;
            }
            set
            {
                //if (!hasProgData) throw new NoProgDataException(this);
                if (_miscData != value) { _miscData = value; Debug.Log("CG Progression: _miscData set with :" + _miscData); }
            }
        }

        /// <summary>
        /// Progression API: Default <see cref="saveData"/> of this mod.
        /// </summary>
        public virtual string defaultSaveData
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Progression API: Default <see cref="persData"/> of this mod.
        /// </summary>
        public virtual string defaultPersData
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Progression API: Default <see cref="miscData"/> of this mod.
        /// </summary>
        public virtual string defaultMiscData
        {
            get { return string.Empty; }
        }


        #region ProgCRUD
        // CRUD//ILSW
        internal void InitSave()
        {
            Debug.Log("CG Progression: InitSave");
            saveData = defaultSaveData;
        }

        internal void LoadSave(int slugNumber)
        {
            Debug.Log("CG Progression: LoadSave");
            if (slugNumber < 0) throw new InvalidSlugcatException();
            saveData = ReadProgressionFile("save", slugNumber, seed, defaultSaveData);
        }

        internal void SaveSave(int slugNumber)
        {
            Debug.Log("CG Progression: SaveSave");
            if (slugNumber < 0) throw new InvalidSlugcatException();
            if (saveData != defaultSaveData)
                WriteProgressionFile("save", slugNumber, seed, saveData);

        }

        internal void WipeSave(int slugNumber)
        {
            Debug.Log("CG Progression: WipeSave");
            if (slugNumber == -1) DeleteAllProgressionFiles("save");
            else DeleteProgressionFile("save", slugNumber);
            if (slugNumber == slugcat) InitSave();
        }

        internal void InitPers()
        {
            Debug.Log("CG Progression: InitPers");
            persData = defaultPersData;
        }

        internal void LoadPers(int slugNumber)
        {
            Debug.Log("CG Progression: LoadPers");
            if (slugNumber < 0) throw new InvalidSlugcatException();
            persData = ReadProgressionFile("pers", slugNumber, seed, defaultPersData);
        }

        internal void SavePers(int slugNumber)
        {
            Debug.Log("CG Progression: SavePers");
            if (slugNumber < 0) throw new InvalidSlugcatException();
            if (persData != defaultPersData)
                WriteProgressionFile("pers", slugNumber, seed, persData);
        }

        internal void WipePers(int slugNumber)
        {
            Debug.Log("CG Progression: WipePers");
            if (slugNumber == -1) DeleteAllProgressionFiles("pers");
            else DeleteProgressionFile("pers", slugNumber);
            if (slugNumber == slugcat) InitPers();
        }

        internal void InitMisc()
        {
            Debug.Log("CG Progression: InitMisc");
            miscData = defaultMiscData;
        }

        internal void LoadMisc()
        {
            Debug.Log("CG Progression: LoadMisc");
            miscData = ReadProgressionFile("misc", -1, -1, defaultMiscData);
        }

        internal void SaveMisc()
        {
            Debug.Log("CG Progression: SaveMisc");
            if (miscData != defaultMiscData)
                WriteProgressionFile("misc", -1, -1, miscData);
        }

        internal void WipeMisc()
        {
            Debug.Log("CG Progression: WipeMisc");
            DeleteProgressionFile("misc", -1);
            InitMisc();
        }
        #endregion ProgCRUD

        #region ProgIO
        // progdata1_White.txt
        // progpers1_White.txt
        // progmisc1.txt

        private string GetTargetFilename(string file, string slugName)
        {
            return directory.FullName + $"prog{file}{slot}{(string.IsNullOrEmpty(slugName) ? string.Empty : "_" + slugName)}.txt";
        }

        private string ReadProgressionFile(string file, int slugNumber, int validSeed, string defaultData)
        {
            Debug.Log("CG Progression: ReadProgressionFile");
            // some locals here have the same name as class stuff and caused me a headache at some point
            if (!directory.Exists) { Debug.Log("CG Progression: directory doesnt exist, returning defaultData"); return defaultData; }

            string slugName = GetSlugcatName(slugNumber);
            //Debug.Log("CG Progression: got slugName :" + slugName);
            string targetFile = GetTargetFilename(file, slugName);
            Debug.Log("CG Progression: reading from :" + targetFile);

            if (!File.Exists(targetFile)) { Debug.Log("CG Progression: file didn't exist, returning defaultData"); return defaultData; }

            string data = File.ReadAllText(targetFile, System.Text.Encoding.UTF8);
            //Debug.Log("CG Progression: got raw contents :" + data);
            string key = data.Substring(0, 32);
            //Debug.Log("CG Progression: got key :" + key);
            data = data.Substring(32, data.Length - 32);
            //Debug.Log("CG Progression: got raw data :" + data);
            if (Custom.Md5Sum(data) != key)
            {
                Debug.Log("CG Progression: progression file has been tinkered with");
                progDataTinkered = true;
            }
            else
            {
                //Debug.Log("CG Progression: decrypting with :" + CryptoProgDataKey(slugName));
                data = CompletelyOptional.Crypto.DecryptString(data, CryptoProgDataKey(slugName));
                //Debug.Log("CG Progression: got decrypted :" + data);
                string[] seedsplit = Regex.Split(data, "<Seed>"); // expected: <Seed>####<Seed>data
                if (seedsplit.Length >= 3)
                {
                    //Debug.Log("CG Progression: expected format");
                    if (int.TryParse(seedsplit[1], out int seed) && seed == validSeed)
                    {
                        // Debug.Log("CG Progression: got valid seed, returning data :" + seedsplit[2]); 
                        return seedsplit[2];
                    }
                    else Debug.Log("CG Progression: got invalid seed " + seed + ", expected " + validSeed);
                }
                else Debug.Log("CG Progression: unexpected format when looking for seed");
            }
            Debug.Log("CG Progression: couldn't read data, returning defaultData");
            return defaultData;
        }

        private void WriteProgressionFile(string file, int slugNumber, int validSeed, string data)
        {
            Debug.Log("CG Progression: WriteProgressionFile");
            if (!directory.Exists) { Debug.Log("CG Progression: directory missing, create"); directory.Create(); directory.Refresh(); }
            string slugName = GetSlugcatName(slugNumber);
            //Debug.Log("CG Progression: got slugName :"+slugName);
            string targetFile = GetTargetFilename(file, slugName);
            Debug.Log("CG Progression: writing to :"+targetFile);
            //Debug.Log("CG Progression: seed to write :" + validSeed);
            data = $"<Seed>{validSeed}<Seed>{data}";
            //Debug.Log("CG Progression: resulting data to write :"+data);
            //Debug.Log("CG Progression: using key :" + CryptoProgDataKey(slugName));
            string enc = CompletelyOptional.Crypto.EncryptString(data, CryptoProgDataKey(slugName));
            //Debug.Log("CG Progression: encrypted data :" + enc);
            string key = Custom.Md5Sum(enc);
            //Debug.Log("CG Progression: key :"+key);
            //Debug.Log("CG Progression: writing :"+(key+enc));
            File.WriteAllText(targetFile, key + enc);
            //Debug.Log("CG Progression: wrote");
        }

        private void DeleteProgressionFile(string file, int slugNumber)
        {
            Debug.Log("CG Progression: DeleteProgressionFile");
            if (!directory.Exists) return;
            string slugName = GetSlugcatName(slugNumber);
            string targetFile = GetTargetFilename(file, slugName);
            if (!File.Exists(targetFile)) return;
            // no backups for now I suppose
            File.Delete(targetFile);
        }

        private void DeleteAllProgressionFiles(string file)
        {
            Debug.Log("CG Progression: DeleteAllProgressionFiles");
            if (!directory.Exists) return;
            foreach (var f in directory.GetFiles($"prog{file}{slot}_*.txt"))
            {
                f.Delete();
            }
        }

        private protected readonly DirectoryInfo directory = new DirectoryInfo(string.Concat(
                    Custom.RootFolderDirectory(),
                    "ModConfigs",
                    Path.DirectorySeparatorChar,
                    ConcealedGarden.instance.ModID,
                    Path.DirectorySeparatorChar
                    ));
        #endregion ProgIO

        /// <summary>
        /// Progression API: An event called internally whenever CM has loaded this OIs progression through one of it's hooks.
        /// This happens when loading, initializing, wiping etc. When this event happens, all OIs progrdata has been loaded/initialized.
        /// Called regardless of there being an actual value change in save/pers/misc data.
        /// </summary>
        //internal protected virtual void ProgressionLoaded() { }

        /// <summary>
        /// Progression API: An event called internally imediatelly before CM would save progression through one of it's hooks.
        /// This even exists so that the OI can serialize any objects it holds in memory before saving.
        /// When this is called, other OIs might not have yet serialized theirs however.
        /// </summary>
        //internal protected virtual void ProgressionPreSave() { }

        /// <summary>
        /// Progression API: An event that happens when loading into the game, starving, quitting outside of the grace period, and death. Saves death-persistent data of a simulated or real death/quit.
        /// Rain World by default creates a save "as if the player died" when it loads into the game to counter the app unexpectedly closing, so do 'revert' any modifications you wanted to save after calling base() on this method.
        /// This method defaults to saving <see cref="persData"/>, so do any modifications to that to simulate a death/quit, call base(), and then revert your data.
        /// </summary>
        internal protected virtual void SaveDeath(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            Debug.Log("CG Progression: SaveDeath");
            SavePers(slugcat);
        }


        // HOOKPOINTS
        internal void InitProgression() // Called when the slot file isn't found on the game's side.
        {
            Debug.Log("CG Progression: InitProgression");
            // To match the game having a fresh start, wipe all ?
            WipeSave(-1);
            WipePers(-1);
            WipeMisc();
        }

        internal void LoadProgression() // Called on load, slot-switch or post-wipe
        {
            Debug.Log("CG Progression: LoadProgression");
            Debug.Log("CG Progression: slot is :" + slot);
            Debug.Log("CG Progression: slug is :" + slugcat);
            Debug.Log("CG Progression: seed is :" + seed);
            InitSave();
            InitPers();
            LoadMisc();
        }

        internal void SaveProgression(bool saveState, bool savePers, bool saveMisc)
        {
            Debug.Log("CG Progression: SaveProgression");
            Debug.Log("CG Progression: slot is :" + slot);
            Debug.Log("CG Progression: slug is :" + slugcat);
            Debug.Log("CG Progression: seed is :" + seed);
            if (saveState) SaveSave(slugcat);
            if (savePers) SavePers(slugcat);
            if (saveMisc) SaveMisc();
        }

        internal void WipeProgression(int saveStateNumber)
        {
            Debug.Log("CG Progression: WipeProgression");
            WipeSave(saveStateNumber);
            WipePers(saveStateNumber);
            if (saveStateNumber == -1)
                WipeMisc();
        }

        internal void LoadSaveState()
        {
            Debug.Log("CG Progression: LoadSaveState");
            Debug.Log("CG Progression: slot is :" + slot);
            Debug.Log("CG Progression: slug is :" + slugcat);
            Debug.Log("CG Progression: seed is :" + seed);
            LoadSave(slugcat);
            LoadPers(slugcat);
        }

        /// <summary>
        /// Currently selected saveslot
        /// </summary>
        public int slot => rw?.options?.saveSlot ?? -1;

        /// <summary>
        /// Currently selected slugcat
        /// </summary>
        public int slugcat => rw?.progression?.PlayingAsSlugcat ?? -1;

        /// <summary>
        /// Seed of currently loaded savestate
        /// Used for validating loaded progression
        /// </summary>
        public int seed => rw?.progression?.currentSaveState?.seed ?? -1;


        /// <summary>
        /// Reads the death-persistent data of the specified slugcat directly from its file, without replacing <see cref="persData"/>
        /// </summary>
        public string GetProgDataOfSlugcat(string slugName)
        {
            int slugNumber = GetSlugcatOfName(slugName);
            return ReadProgressionFile("pers", slugNumber, GetSlugcatSeed(slugNumber, slot), defaultPersData);
        }
        /// <summary>
        /// Reads the death-persistent data of the specified slugcat directly from its file, without replacing <see cref="persData"/>
        /// </summary>
        public string GetProgDataOfSlugcat(int slugcatNumber) => GetProgDataOfSlugcat(GetSlugcatName(slugcatNumber));

        /// <summary>
        /// Helper for getting the text name for a slugcat, which is used for filenames for data
        /// </summary>
        public static string GetSlugcatName(int slugcat)
        {
            if (slugcat < 0) return null;
            if (slugcat < 3) { return ((SlugcatStats.Name)slugcat).ToString(); }
            if (SlugBaseExists && IsSlugBaseSlugcat(slugcat)) { return GetSlugBaseSlugcatName(slugcat); }
            else { return ((SlugcatStats.Name)slugcat).ToString(); }
        }

        /// <summary>
        /// Reverse helper for getting the slugcat of a given text name
        /// </summary>
        public static int GetSlugcatOfName(string name)
        {
            // I tried to keep the same order as GetSlugcatName but...
            if (string.IsNullOrEmpty(name)) return -1;
            if (name == "White") return 0;
            if (name == "Yellow") return 1;
            if (name == "Red") return 2;
            if (SlugBaseExists && IsSlugBaseName(name)) { return GetSlugBaseSlugcatOfName(name); }
            return (int)Enum.Parse(typeof(SlugcatStats.Name), name);
        }

        internal static int GetSlugcatSeed(int slugcat, int slot)
        {
            // Load from currently loaded save if available and valid
            SaveState save = rw?.progression?.currentSaveState;
            if (save != null && save.saveStateNumber == slugcat)
            {
                return save.seed;
            }
            // Load from slugbase custom save file
            if (SlugBaseExists && IsSlugBaseSlugcat(slugcat))
            {
                return GetSlugBaseSeed(slugcat, slot);
            }
            // Load from vanilla save file
            if (rw.progression.IsThereASavedGame(slugcat))
            {
                string[] progLines = rw.progression.GetProgLines();
                if (progLines.Length != 0)
                {
                    for (int i = 0; i < progLines.Length; i++)
                    {
                        string[] data = Regex.Split(progLines[i], "<progDivB>");
                        if (data.Length == 2 && data[0] == "SAVE STATE" && int.Parse(data[1][21].ToString()) == slugcat)
                        {
                            List<SaveStateMiner.Target> query = new List<SaveStateMiner.Target>()
                        {
                            new SaveStateMiner.Target(">SEED", "<svB>", "<svA>", 20)
                        };
                            List<SaveStateMiner.Result> result = SaveStateMiner.Mine(rw, data[1], query);
                            if (result.Count == 0) break;
                            try
                            {
                                return int.Parse(result[0].data);
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return -1;
        }

        #region SlugBase

        private static bool IsSlugBaseName(string name) => SlugBase.PlayerManager.GetCustomPlayer(name) != null;
        private static bool IsSlugBaseSlugcat(int slugcat) => SlugBase.PlayerManager.GetCustomPlayer(slugcat) != null;
        private static int GetSlugBaseSlugcatOfName(string name) => SlugBase.PlayerManager.GetCustomPlayer(name)?.SlugcatIndex ?? -1;
        private static string GetSlugBaseSlugcatName(int slugcat) => SlugBase.PlayerManager.GetCustomPlayer(slugcat)?.Name;
        private static int GetSlugBaseSeed(int slugcat, int slot)
        {
            SlugBase.SlugBaseCharacter ply = SlugBase.PlayerManager.GetCustomPlayer(slugcat);
            if (ply == null || !SlugBase.SaveManager.HasCustomSaveData(ply.Name, slot)) return -1;
            string saveData = File.ReadAllText(SlugBase.SaveManager.GetSaveFilePath(ply.Name, slot));
            List<SaveStateMiner.Target> query = new List<SaveStateMiner.Target>()
                {
                    new SaveStateMiner.Target(">SEED", "<svB>", "<svA>", 20)
                };
            List<SaveStateMiner.Result> result = SaveStateMiner.Mine(rw, saveData, query);
            if (result.Count != 0)
            {
                try
                {
                    return int.Parse(result[0].data);
                }
                catch (Exception) { }
            }
            return -1;
        }

        #endregion SlugBase

        private string CryptoProgDataKey(string slugName) => "OptionalProgData" + (string.IsNullOrEmpty(slugName) ? "Misc" : slugName) + ConcealedGarden.instance.ModID;
        #endregion progData
    }
}

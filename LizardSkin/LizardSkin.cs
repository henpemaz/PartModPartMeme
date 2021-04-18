using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;


using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Reflection;

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


namespace LizardSkin
{
    public class LizardSkin : PartialityMod
    {

        public LizardSkin()
        {
            this.ModID = "LizardSkin";
            this.Version = "0.1";
            this.author = "Henpemaz";

            instance = this;
        }

        public static LizardSkin instance;

        public static OptionalUI.OptionInterface LoadOI()
        {
            return new LizardSkinOI();
        }

        internal static Type fpg_ref;
        internal static Type jolly_ref;
        internal static Type custail_ref;
        internal static Type colorfoot_ref;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            PlayerGraphicsCosmeticsAdaptor.ApplyHooksToPlayerGraphics();

            // Old mod detection code
            //// store type to check for instances
            //fpg = Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats");
            //jolly_ref = Type.GetType("JollyCoop.PlayerGraphicsHK, JollyCoop");
            //custail_ref = Type.GetType("CustomTail.CustomTail, CustomTail");
            //colorfoot_ref = Type.GetType("Colorfoot.LegMod, Colorfoot");

            // New mod detection code
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "FancySlugcats")
                {
                    fpg_ref = asm.GetType("FancySlugcats.FancyPlayerGraphics");
                }
                else
                if (asm.GetName().Name == "JollyCoop")
                {
                    jolly_ref = asm.GetType("JollyCoop.PlayerGraphicsHK");
                }
                else
                if (asm.GetName().Name == "CustomTail")
                {
                    custail_ref = asm.GetType("CustomTail.CustomTail");
                }
                else
                if (asm.GetName().Name == "Colorfoot")
                {
                    colorfoot_ref = asm.GetType("Colorfoot.LegMod");
                }
            }

            if (fpg_ref != null)
            {
                Debug.Log("LizardSkin: FOUND FancyPlayerGraphics");
                try
                {
                    FancyPlayerGraphicsCosmeticsAdaptor.ApplyHooksToFancyPlayerGraphics();
                    Debug.Log("LizardSkin: FancyPlayerGraphics hooks applied");
                }
                catch (Exception e)
                {
                    Debug.LogError("LizardSkin: ERROR hooking FancyPlayerGraphics, integration disabled, send logs to henpe");
                    fpg_ref = null;
                    Debug.LogException(e);
                }
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND FancyPlayerGraphics, integration disabled");
            }

            if (jolly_ref != null)
            {
                Debug.Log("LizardSkin: FOUND Jolly");
                try
                {
                    PlayerGraphicsCosmeticsAdaptor.ApplyHooksToJollyPlayerGraphicsHK();
                    Debug.Log("LizardSkin: Jolly hooks applied");
                }
                catch (Exception e)
                {
                    Debug.LogError("LizardSkin: ERROR hooking Jolly, integration disabled, send logs to henpe");
                    jolly_ref = null;
                    Debug.LogException(e);
                }
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND Jolly, integration disabled");
            }

            if (custail_ref != null)
            {
                Debug.Log("LizardSkin: FOUND CustomTail, no hooks to apply");
                // No hookies needed, good mod :)
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND CustomTail, integration disabled");
            }

            if (colorfoot_ref != null)
            {
                Debug.Log("LizardSkin: FOUND Colorfoot");
                try
                {
                    PlayerGraphicsCosmeticsAdaptor.ApplyHooksToColorfootPlayerGraphicsPatch();
                    Debug.Log("LizardSkin: Colorfoot hooks applied");
                }
                catch (Exception e)
                {
                    Debug.LogError("LizardSkin: ERROR hooking Colorfoot, integration disabled, send logs to henpe");
                    colorfoot_ref = null;
                    Debug.LogException(e);
                }
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND Colorfoot, integration disabled");
            }

            On.RainWorld.Start += RainWorld_Start_hk;

            // Json goes brrrr
            On.Json.Serializer.SerializeOther += Serializer_SerializeOther;

            TestSerialization();
        }

        private void RainWorld_Start_hk(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);
            LoadAtlasStreamIntoManager(Futile.atlasManager, "LizKinIcons.png", Assembly.GetExecutingAssembly().GetManifestResourceStream("LizardSkin.Resources.LizKinIcons.png"), Assembly.GetExecutingAssembly().GetManifestResourceStream("LizardSkin.Resources.LizKinIcons.txt"));
        }

        private static void TestSerialization()
        {
            Debug.Log("Serialization tests start");
            LizKinConfiguration myConfig = new LizKinConfiguration();
            myConfig.AddDefaultProfile();

            string serialized = Json.Serialize(myConfig);

            LizKinConfiguration deserialized = LizKinConfiguration.MakeFromJson(Json.Deserialize(serialized) as Dictionary<string, object>);

            string serialized2 = Json.Serialize(deserialized);

            if (serialized != serialized2) throw new System.Runtime.Serialization.SerializationException("Reserialization check failed");

            LizKinConfiguration cloned = LizKinConfiguration.Clone(myConfig);

            string serialized3 = Json.Serialize(cloned);

            if (serialized2 != serialized3) throw new System.Runtime.Serialization.SerializationException("Clone check failed");

            Debug.Log("Serialization tests ok");
        }

        private static void Serializer_SerializeOther(On.Json.Serializer.orig_SerializeOther orig, Json.Serializer self, object value)
        {
            if (value is IJsonSerializable) self.SerializeObject((value as IJsonSerializable).ToJson());
            else orig(self, value);
        }


        static void LoadAtlasStreamIntoManager(FAtlasManager atlasManager, string atlasName, System.IO.Stream textureStream, System.IO.Stream jsonStream)
        {
            try
            {
                // load texture
                Texture2D texture2D = new Texture2D(0, 0, TextureFormat.ARGB32, false);
                byte[] bytes = new byte[textureStream.Length];
                textureStream.Read(bytes, 0, (int)textureStream.Length);
                texture2D.LoadImage(bytes);
                // from rainWorld.png.meta unity magic
                texture2D.anisoLevel = 1;
                texture2D.filterMode = 0;

                // make fake singleimage atlas
                FAtlas fatlas = new FAtlas(atlasName, texture2D, FAtlasManager._nextAtlasIndex++);
                fatlas._elements.Clear();
                fatlas._elementsByName.Clear();
                fatlas._isSingleImage = false;

                // actually load the atlas
                StreamReader sr = new StreamReader(jsonStream, Encoding.UTF8);
                Dictionary<string, object> dictionary = sr.ReadToEnd().dictionaryFromJson();

                //ctrl c
                //ctrl v

                Dictionary<string, object> dictionary2 = (Dictionary<string, object>)dictionary["frames"];
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
                //pray
                atlasManager.AddAtlas(fatlas);

            }
            finally
            {
                textureStream.Close();
                jsonStream.Close();
            }
        }

        // TODO - steal AttachedFields.cs

    }
}

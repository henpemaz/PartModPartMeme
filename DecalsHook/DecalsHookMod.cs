using Partiality.Modloader;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace DecalsHook
{
    public class DecalsHookMod : PartialityMod
    {
        public DecalsHookMod()
        {
            this.ModID = "DecalsHookMod";
            this.Version = "1.0";
            this.author = "Henpemaz";
            instance = this;
        }

        public static DecalsHookMod instance;
        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre
            On.DevInterface.CustomDecalRepresentation.CustomDecalControlPanel.Signal += CustomDecalControlPanel_Signal;
            On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.ctor += SelectDecalPanel_ctor;
        }

        private void SelectDecalPanel_ctor(On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.orig_ctor orig, DevInterface.CustomDecalRepresentation.SelectDecalPanel self, DevInterface.DevUI owner, DevInterface.DevUINode parentNode, Vector2 pos, string[] decalNames)
        {
            orig(self, owner, parentNode, pos, decalNames);
            self.subNodes.Add(new DevInterface.Button(self.owner, "btndecalsprev0", self, new Vector2(5f, self.size.y - 25f - 20f * (32 + 1f)), 145f, "Previous"));
            self.subNodes.Add(new DevInterface.Button(self.owner, "btndecalsnext0", self, new Vector2(155f, self.size.y - 25f - 20f * (32 + 1f)), 145f, "Next"));
            OrganizeDecals(0, self);
        }

        private void OrganizeDecals(int page, DevInterface.CustomDecalRepresentation.SelectDecalPanel self)
        {
            self.size = new Vector2(605f, 620f);
            IntVector2 intVector = new IntVector2(0, 0);
            for (int i = 0; i < self.subNodes.Count; i++)
            {
                if(self.subNodes[i] is DevInterface.Button btn)
                {
                    if (!btn.IDstring.StartsWith("btndecalsnext") && !btn.IDstring.StartsWith("btndecalsprev"))
                    {
                        Vector2 target;
                        if (intVector.x / 4 < page || intVector.x / 4 > page) target = new Vector2(10000, 10000);
                        else target = new Vector2(5f + (float)(intVector.x % 4) * 150f, self.size.y - 25f - 20f * (float)intVector.y);
                        btn.pos = target;
                        intVector.y++;
                        if (intVector.y > 28)
                        {
                            intVector.x++;
                            intVector.y = 0;
                        }
                    }
                    else if (btn.IDstring.StartsWith("btndecalsnext"))
                    {
                        typeof(DevInterface.DevUINode).GetField("IDstring", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(btn, "btndecalsnext" + page);
                    }
                    else if (btn.IDstring.StartsWith("btndecalsprev"))
                    {
                        typeof(DevInterface.DevUINode).GetField("IDstring", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(btn, "btndecalsprev" + page);
                    }
                }
            }
        }

        private void CustomDecalControlPanel_Signal(On.DevInterface.CustomDecalRepresentation.CustomDecalControlPanel.orig_Signal orig, DevInterface.CustomDecalRepresentation.CustomDecalControlPanel self, DevInterface.DevUISignalType type, DevInterface.DevUINode sender, string message)
        {
            int page;
            if (sender.IDstring.StartsWith("btndecalsnext") && self.decalsSelectPanel != null)
            {
                page = int.Parse(sender.IDstring.Substring(13));
                page++;
                if(page * 4 * 28 < (self.parentNode as DevInterface.CustomDecalRepresentation).decalFiles.Length)
                OrganizeDecals(page, self.decalsSelectPanel);
            }
            else if (sender.IDstring.StartsWith("btndecalsprev") && self.decalsSelectPanel != null)
            {
                page = int.Parse(sender.IDstring.Substring(13));
                page--;
                if(page >= 0)
                    OrganizeDecals(page, self.decalsSelectPanel);
            } 
            else
            {
                orig(self, type, sender, message);
            }
        }
    }
}
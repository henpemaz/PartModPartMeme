using System;
using System.IO;
using System.Reflection;

namespace Squiddy
{
    public partial class SquiddyBase
    {
        static char[] embedRepl = new char[] { ' ', '\u00A0', '.', ',', '+', '-', '/', '\\', '[', ']', '(', ')', '"', '\''};
        public override Stream GetResource(params string[] path)
        {
            string[] patchedPath = new string[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                if (i < path.Length - 1)
                {
                    patchedPath[i] = string.Join("_", path[i].Split(embedRepl, StringSplitOptions.None));
                }
                else
                {
                    patchedPath[i] = path[i].Replace(" ", "_");
                }
            }
            string patchedpathstr = String.Join(".", patchedPath);
            Stream tryGet = Assembly.GetExecutingAssembly().GetManifestResourceStream("Squiddy.Resources." + patchedpathstr);
            if (tryGet != null) return tryGet;
            return base.GetResource(path);
        }
    }
}
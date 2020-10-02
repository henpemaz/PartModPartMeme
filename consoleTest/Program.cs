using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace consoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] killScores = new int[Enum.GetNames(typeof(MultiplayerUnlocks.SandboxUnlockID)).Length];
            // from Menu.StoryGameStatisticsScreen:16
            for (int i = 0; i < killScores.Length; i++)
            {
                killScores[i] = -1;
            }
            Menu.SandboxSettingsInterface.DefaultKillScores(ref killScores);
            killScores[0] = 1;

            Console.WriteLine("=== CreatureTemplate stuff ===");
            foreach (CreatureTemplate.Type creaturetype in (CreatureTemplate.Type[])Enum.GetValues(typeof(CreatureTemplate.Type)))
            {
                if (CreatureSymbol.DoesCreatureEarnATrophy(creaturetype))
                {
                    if (creaturetype == CreatureTemplate.Type.Centipede)
                    {
                        Console.WriteLine(Enum.GetName(typeof(MultiplayerUnlocks.SandboxUnlockID), MultiplayerUnlocks.SandboxUnlockID.MediumCentipede) + " - " + killScores[(int)MultiplayerUnlocks.SandboxUnlockID.MediumCentipede]);
                        Console.WriteLine(Enum.GetName(typeof(MultiplayerUnlocks.SandboxUnlockID), MultiplayerUnlocks.SandboxUnlockID.BigCentipede) + " - " + killScores[(int)MultiplayerUnlocks.SandboxUnlockID.BigCentipede]);
                    }
                    else
                    {
                        try
                        {
                            MultiplayerUnlocks.SandboxUnlockID result = Custom.ParseEnum<MultiplayerUnlocks.SandboxUnlockID>(creaturetype.ToString());
                            Console.WriteLine(Enum.GetName(typeof(MultiplayerUnlocks.SandboxUnlockID), result) + " - " + killScores[(int)result]);
                        }
                        catch
                        {
                            Console.WriteLine("Not found: " + creaturetype.ToString());
                        }
                        
                    }
                }
            }
        }
    }
}

using Partiality.Modloader;

namespace PacifismEnforcer
{
    public class PacifismEnforcer : PartialityMod
    {
        public PacifismEnforcer()
        {
            this.ModID = "PacifismEnforcer";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static PacifismEnforcer instance;

        public override void OnEnable()
        {
            base.OnEnable();


            On.PlayerSessionRecord.BreakPeaceful += PlayerSessionRecord_BreakPeaceful;
            UnityEngine.Debug.Log("PacifismEnforcer on");
        }

        private void PlayerSessionRecord_BreakPeaceful(On.PlayerSessionRecord.orig_BreakPeaceful orig, PlayerSessionRecord self, Creature victim)
        {
            UnityEngine.Debug.Log("PacifismEnforcer proc");
            orig(self, victim);

            if (!self.peaceful)
            {
                victim.abstractCreature.world.game.Players[self.playerNumber].realizedCreature.Die();
                UnityEngine.Debug.Log("PacifismEnforcer death");
            }
        }
    }
}

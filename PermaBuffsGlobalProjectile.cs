using Terraria;
using ReLogic;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace PermaBuffs
{
    public class PermaBuffsGlobalProjectile : GlobalProjectile
    {
        /*
        // Might be useful in the case I figure out how to store and respawn summons after death.
        public override bool PreKill(Projectile projectile, int timeLeft)
        {
            if (projectile.owner == Main.myPlayer)
            {
                Player player = Main.LocalPlayer;
                PermaBuffsPlayer modPlayer = player.GetModPlayer<PermaBuffsPlayer>();

                for (int i = 0; i < BuffLoader.BuffCount; i++)
                {
                    if (projectile.type == modPlayer.buffItemIDs[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        */
    }
}

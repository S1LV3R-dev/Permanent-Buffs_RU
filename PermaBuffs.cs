using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace PermaBuffs
{
    public class PermaBuffs : Mod
    {
        public override void Load()
        {
            PermaBuffsPlayer.alwaysPermanentKey = KeybindLoader.RegisterKeybind(this, "Toggle Buff Always Permanent", Microsoft.Xna.Framework.Input.Keys.P);
            PermaBuffsPlayer.neverPermanentKey = KeybindLoader.RegisterKeybind(this, "Toggle Buff Never Permanent", Microsoft.Xna.Framework.Input.Keys.N);
        }
    }
}

using System;
using spaar.ModLoader;
using UnityEngine;

namespace BetterComMod
{

    // If you need documentation about any of these values or the mod loader
    // in general, take a look at https://spaar.github.io/besiege-modloader.

    public class YourMod : Mod
    {
        public override string Name { get; } = "<placeholder>";
        public override string DisplayName { get; } = "<placeholder>";
        public override string Author { get; } = "<placeholder>";
        public override Version Version { get; } = new Version(1, 0, 0);

        // You don't need to override this, if you leavie it out it will default
        // to an empty string.
        public override string VersionExtra { get; } = "";

        // You don't need to override this, if you leave it out it will default
        // to the current version.
        public override string BesiegeVersion { get; } = "v0.25";

        // You don't need to override this, if you leave it out it will default
        // to false.
        public override bool CanBeUnloaded { get; } = false;

        // You don't need to override this, if you leave it out it will default
        // to false.
        public override bool Preload { get; } = false;


        public override void OnLoad()
        {
            // Your initialization code here
        }

        public override void OnUnload()
        {
            // Your code here
            // e.g. save configuration, destroy your objects if CanBeUnloaded is true etc
        }
    }
}

using MSCLoader;
using UnityEngine;

namespace CallThePlumber
{
    public class CallThePlumber : Mod
    {
        public override string ID => "CallThePlumber"; // Your (unique) mod ID 
        public override string Name => "Call The Plumber"; // Your mod name
        public override string Author => "casper-3"; // Name of the Author (your name)
        public override string Version => "1.0"; // Version
        public override string Description => ""; // Short description of your mod 
        public override Game SupportedGames => Game.MyWinterCar;

        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.Update, Mod_Update);
            SetupFunction(Setup.ModSettings, Mod_Settings);
        }

        private void Mod_Settings()
        {
            // All settings should be created here. 
            // DO NOT put anything that isn't settings or keybinds in here!
        }

        private void Mod_OnLoad()
        {
            // Called once, when mod is loading after game is fully loaded
        }
        private void Mod_Update()
        {
            // Update is called once per frame
        }
    }
}

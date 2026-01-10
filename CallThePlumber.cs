using MSCLoader;
using UnityEngine;
using HutongGames.PlayMaker;
using System;

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


        GameObject plumberPhone;
        string plumberPhoneNumber = "08123123";
        System.Random rng = new System.Random();
        SettingsSliderInt minCostSetting, maxCostSetting;

        void initializePlumberPhone()
        {
            plumberPhone = new GameObject(plumberPhoneNumber);
            var phonesGO = GameObject.Find("CARPARTS/PARTSYSTEM/PhoneNumbers");

            plumberPhone.transform.SetParent(phonesGO.transform);

            // FSM needed to interact with MWC phone system
            var phoneDataFSM = plumberPhone.AddComponent<PlayMakerFSM>();
            phoneDataFSM.FsmName = "Data";
            var fsm = phoneDataFSM.Fsm;
            fsm.Variables.FloatVariables = [
                new FsmFloat { Name = "CallerCallLenght", Value = 5f },
                new FsmFloat { Name = "Distance", Value = 1800f },
            ];

            fsm.Variables.StringVariables = [
                new FsmString {Name = "CallerAudioVariation", Value = "taxijob_call1"},
                new FsmString { Name = "CallerSubtitle", Value = "Your pipes burst from the cold? Didn't your parents teach you about that stuff?" },
                new FsmString {Name = "Number", Value = plumberPhoneNumber},
            ];

            fsm.Variables.IntVariables = [
                new FsmInt { Name = "Stage", Value = 1 }, // can be anything but 0
            ];

            var readyState = new FsmState(fsm)
            {
                Name = "Ready"
            };

            var calledState = new FsmState(fsm)
            {
                Name = "Called",
            };

            fsm.States = [
                readyState,
                calledState
            ];

            fsm.AddEvent("CALLED"); // Event fired by vanilla phone system

            fsm.GlobalTransitions = [
                new FsmTransition() {
                    FsmEvent = fsm.GetEvent("CALLED"),
                    ToState = "Called"
                }
            ];

            fsm.StartState = "Ready";

            if (phoneDataFSM.FsmInject(stateName: "Called", hook: () => { ModConsole.Print("Plumber scheduled"); })) 
            {
                ModConsole.Print("Injected OnCalled successfully");
            }
            else
            {
                ModConsole.Error("Failed to inject OnCalled");
            }
        }

        void SpawnPlumblingBill()
        {

        }
        
        void PayPlumblingBill()
        {

        }

        void SpawnPlumberAd()
        {

        }

        void DespawnPlumberAd()
        {

        }

        bool IsParentHousePipesBurst()
        {
            return true; // Placeholder
        }

        void fixParentHousePipes()
        {

        }

        int GetRandomCost(int minCost, int maxCost)
        {
            return rng.Next(minCost, maxCost);
        }

        public void OnPlumberCalled()
        {
            ModConsole.Print("Plumber scheduled");
        }

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
            Settings.AddHeader("Gameplay balance");
            minCostSetting = Settings.AddSlider("minCostIntSlider", "Minimum plumbing service cost", minValue: 1, maxValue: 10, value: 5, onValueChanged: null);
            maxCostSetting = Settings.AddSlider("maxCostIntSlider", "Maximum plumbing service cost", minValue: 10, maxValue: 20, value: 15, onValueChanged: null);
        }

        private void Mod_OnLoad()
        {
            // Called once, when mod is loading after game is fully loaded
            initializePlumberPhone();
        }
        private void Mod_Update()
        {
            // Update is called once per frame
        }
    }
}

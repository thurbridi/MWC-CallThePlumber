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

        SettingsSliderInt minCostSlider, maxCostSlider;

        GameObject plumberPhone, parentsHousePipes, phones;

        PlayMakerFSM pipesLogicFsm;

        // TODO: Organize and group related constants somewhere else'
        readonly string plumberPhoneNumber = "08123123";
        readonly string plumberCallSubtitles = "Your pipes burst from the cold? Didn't your parents teach you about that stuff?";
        readonly string plumberCallAudioTrack = "taxijob_call1";
        readonly float plumberCallLength = 5f;
        readonly float plumberCallDistance = 1800f;

        readonly string repairPlumbingEventName = "REPAIRPLUMBING";
        readonly string burstStateName = "State 2";

        void InitializeMod()
        {
            phones = GameObject.Find("CARPARTS/PARTSYSTEM/PhoneNumbers");
            parentsHousePipes = GameObject.Find("YARD/Building/Dynamics/Pipes");
            pipesLogicFsm = parentsHousePipes.GetPlayMaker("Logic");

            InitializePlumberPhoneNumber();
            PatchParentsHousePipesFSM();
        }

        void PatchParentsHousePipesFSM()
        {
            string workingStateName = "State 1";
            string repairingStateName = "Repairing";

            FsmState repairingState = pipesLogicFsm.AddState(repairingStateName);
            
            pipesLogicFsm.GetState(burstStateName).AddTransition(repairPlumbingEventName, repairingStateName);
            repairingState.AddTransition("FINISHED", workingStateName);

            pipesLogicFsm.FsmInject(repairingStateName, () => {
                FsmFloat iceFsmVar = pipesLogicFsm.GetVariable<FsmFloat>("Ice");
                FsmBool pipesOkFsmVar = pipesLogicFsm.GetVariable<FsmBool>("PipesOK");

                iceFsmVar.Value = 0f;
                pipesOkFsmVar.Value = true;
            });
        }

        void InitializePlumberPhoneNumber()
        {
            plumberPhone = new GameObject(plumberPhoneNumber);

            plumberPhone.transform.SetParent(phones.transform);

            // FSM needed to interact with MWC phone system
            string readyStateName = "Ready";
            string calledStateName = "Called";
            string calledEventName = "CALLED";

            PlayMakerFSM phoneDataFSM = plumberPhone.AddComponent<PlayMakerFSM>();
            phoneDataFSM.FsmName = "Data";
            
            // TODO: Refactor using PlayMakerExtensions
            var fsm = phoneDataFSM.Fsm;

            fsm.Variables.FloatVariables = [
                new FsmFloat { Name = "CallerCallLenght", Value = plumberCallLength }, // "Lenght" in name is intentionally misspelled to match MWC
                new FsmFloat { Name = "Distance", Value = plumberCallDistance },
            ];

            fsm.Variables.StringVariables = [
                new FsmString {Name = "CallerAudioVariation", Value = plumberCallAudioTrack},
                new FsmString { Name = "CallerSubtitle", Value = plumberCallSubtitles },
                new FsmString {Name = "Number", Value = plumberPhoneNumber},
            ];

            fsm.Variables.IntVariables = [
                new FsmInt { Name = "Stage", Value = 1 }, // can be anything but 0, otherwise it handles like a magazine calls
            ];

            var readyState = new FsmState(fsm)
            {
                Name = readyStateName
            };

            var calledState = new FsmState(fsm)
            {
                Name = calledStateName,
            };

            fsm.States = [
                readyState,
                calledState
            ];

            fsm.AddEvent(calledEventName); // Event fired by vanilla phone system

            fsm.GlobalTransitions = [
                new FsmTransition() {
                    FsmEvent = fsm.GetEvent(calledEventName),
                    ToState = calledStateName,
                }
            ];

            fsm.StartState = readyStateName;

            if (!phoneDataFSM.FsmInject(stateName: calledStateName, hook: HandlePlumberCalled))
                ModConsole.Error("Failed to inject OnCalled");
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

        bool IsParentsHousePipesBurst()
        {
            return pipesLogicFsm.ActiveStateName == burstStateName;
        }

        void HandlePlumberCalled()
        {
            ModConsole.Print("Plumber scheduled");
            RepairParentsHousePipes();
        }

        void RepairParentsHousePipes()
        {
            ModConsole.Print("Repairing parents house pipes");
            pipesLogicFsm.SendEvent(repairPlumbingEventName);
        }

        void BurstParentsHousePipes() {
            // For debug purposes
            if (!(pipesLogicFsm.ActiveStateName == "State 1")) {
                return; 
            }
            pipesLogicFsm.GetVariable<FsmFloat>("Ice").Value = 100.5f;
            pipesLogicFsm.SendEvent("BURST");
        }

        int GetRandomCost()
        {
            int minCost, maxCost;
            minCost = minCostSlider.GetValue();
            maxCost = maxCostSlider.GetValue();

            return UnityEngine.Random.Range(minCost, maxCost);
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
            minCostSlider = Settings.AddSlider("minCostIntSlider", "Minimum plumbing service cost", minValue: 1, maxValue: 10, value: 5, onValueChanged: null);
            maxCostSlider = Settings.AddSlider("maxCostIntSlider", "Maximum plumbing service cost", minValue: 10, maxValue: 20, value: 15, onValueChanged: null);

#if DEBUG
            Settings.AddHeader("Debug");
            Settings.AddButton(name: "Repair house pipes", onClick: RepairParentsHousePipes);
            Settings.AddButton(name: "Freeze and burst pipes", onClick: BurstParentsHousePipes);
#endif
        }

        private void Mod_OnLoad()
        {
            // Called once, when mod is loading after game is fully loaded
            InitializeMod();
        }
        private void Mod_Update()
        {
            // Update is called once per frame
        }
    }
}

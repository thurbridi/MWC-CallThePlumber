using System;
using MSCCoreLibrary;
using MSCLoader;
using UnityEngine;

namespace CallThePlumber
{
    public class CallThePlumber : Mod
    {
        public override string ID => "CallThePlumber"; // Your (unique) mod ID 
        public override string Name => "Call The Plumber"; // Your mod name
        public override string Author => "casper-3"; // Name of the Author (your name)
        public override string Version => "0.1.0"; // Version
        public override string Description => "Hire a plumber to fix the burst pipes in your parents house."; // Short description of your mod 
        public override Game SupportedGames => Game.MyWinterCar;

        SettingsSlider minCostSlider, maxCostSlider,
            hoursToInvoiceSlider, hoursToRepairStartSlider, hoursToRepairFinishSlider;
        PlayMakerFSM pipesLogicFsm;

        PlumberService plumberService;

        void InitializeMod()
        {
            ModConsole.Log("[CallThePlumber] Initializing mod...");
            pipesLogicFsm = GameObject.Find("YARD/Building/Dynamics/Pipes").GetPlayMaker("Logic");

            PlumberState currentState = SaveLoad.ValueExists(this, "plumberState") ? SaveLoad.ReadValue<PlumberState>(this, "plumberState") :
                IsParentsHousePipesBurst() ? PlumberState.Available : PlumberState.Finished;
            float invoiceCost = SaveLoad.ValueExists(this, "invoiceCost") && (currentState == PlumberState.WaitingPayment) ?
                SaveLoad.ReadValue<float>(this, "invoiceCost")
                : Average(minCostSlider.GetValue(), maxCostSlider.GetValue());

            string phoneNumber = "08114896";
            PlumberService.Config plumberConfig = new()
            {
                phoneNumber = phoneNumber,
                adSubtitles = $"\"Need plumbing services? Call {PlumberService.FormatPhoneNumber(phoneNumber)}\"",
                callSubtitles = "Pipes burst from the cold? Didn't your parents warn you about it? Anyway, I'll send the invoice to you.",
                // TODO: Replace taxijob_call placeholder audio with custom one
                callAudioVariation = "taxijob_call1", // soundGroupName: Callers
                callLength = 5f,
                callDistance = 1800f,
                minCost = minCostSlider.GetValue(),
                maxCost = maxCostSlider.GetValue(),
                currentState = currentState,
                invoiceCost = invoiceCost,
                hoursToInvoice = hoursToInvoiceSlider.GetValue(),
                hoursToRepairStart = hoursToRepairStartSlider.GetValue(),
                hoursToRepairFinish = hoursToRepairFinishSlider.GetValue(),
            };

            plumberService = PlumberService.Instance;
            plumberService.Initialize(plumberConfig);

            if (SaveLoad.ValueExists(this, "plumberActionMethodName"))
            {
                ModConsole.Log("[CallThePlumber] Save ScheduledAction found. Restoring now.");
                GameTime.Days day = SaveLoad.ReadValue<GameTime.Days>(this, "plumberActionDay");
                int hour = SaveLoad.ReadValue<int>(this, "plumberActionHour");
                int minute = SaveLoad.ReadValue<int>(this, "plumberActionMinute");
                string methodName = SaveLoad.ReadValue<string>(this, "plumberActionMethodName");
                Action act = (Action)Delegate.CreateDelegate(typeof(Action), plumberService, methodName);

                TimeScheduler.ScheduleAction(hour, minute, act, day, oneTimeAction: true);
            }

            ModConsole.Log("[CallThePlumber] Mod initialized.");
        }

        float Average(float a, float b)
        {
            return (a + b) / 2f;
        }

        public bool IsParentsHousePipesBurst()
        {
            return pipesLogicFsm.ActiveStateName == "State 2";
        }

        public override void ModSetup()
        {
            SetupFunction(Setup.OnSave, Mod_OnSave);
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.ModSettings, Mod_Settings);
        }

        private void Mod_Settings()
        {
            // All settings should be created here. 
            // DO NOT put anything that isn't settings or keybinds in here!
            float maxCost = 40000f;
            float minCost = 7400f;

            float defaultMinValue = 12500f;
            float defaultMaxValue = 25000f;

            Settings.AddHeader("Gameplay balance");
            Settings.AddText("<size=24><b>Cost Settings</b></size>");
            Settings.AddText("<size=18>The actual cost on the invoice is uniformly sampled between min and max values. Changes to these values will only apply the next time an invoice is generated.</size>");
            minCostSlider = Settings.AddSlider(
                "minCostSlider", "Minimum plumbing service cost (MK)",
                minValue: minCost, maxValue: maxCost, value: defaultMinValue, decimalPoints: 0,
                onValueChanged: () =>
                {
                    maxCostSlider.SetValue(Math.Max(minCostSlider.GetValue(), maxCostSlider.GetValue()));
                    plumberService.UpdateCostSettings(minCostSlider.GetValue(), maxCostSlider.GetValue());
                });

            maxCostSlider = Settings.AddSlider(
                "maxCostSlider", "Maximum plumbing service cost (MK)",
                minValue: minCost, maxValue: maxCost, value: defaultMaxValue, decimalPoints: 0,
                onValueChanged: () =>
                {
                    minCostSlider.SetValue(Math.Min(minCostSlider.GetValue(), maxCostSlider.GetValue()));
                    plumberService.UpdateCostSettings(minCostSlider.GetValue(), maxCostSlider.GetValue());
                });

            Settings.AddText("<size=24><b>Time Settings</b></size>");
            hoursToInvoiceSlider = Settings.AddSlider(
                "hoursToInvoice", "Hours until invoice is delivered after phone call",
                minValue: 0.25f, maxValue: 144f, value: 48f, decimalPoints: 2, onValueChanged: () =>
                {
                    plumberService.SetHoursToInvoice(hoursToInvoiceSlider.GetValue());
                }
                );
            hoursToRepairStartSlider = Settings.AddSlider(
                "hoursToRepairStart", "Hours until plumber arrives after paying the invoice",
                minValue: 0.25f, maxValue: 144f, value: 24f, decimalPoints: 2, onValueChanged: () =>
                {
                    plumberService.SetHoursToRepairStart(hoursToRepairStartSlider.GetValue());
                }
            );
            hoursToRepairFinishSlider = Settings.AddSlider(
                "hoursToRepairFinish", "Hours until plumber finishes the repairs after arriving",
                minValue: 0.25f, maxValue: 144f, value: 72f, decimalPoints: 2, onValueChanged: () =>
                {
                    plumberService.SetHoursToRepairFinish(hoursToRepairFinishSlider.GetValue());
                }
            );

            // TODO: Hide debug completely at 1.0.0 release
            bool isDebugHeaderCollapsed = true;
#if DEBUG
            isDebugHeaderCollapsed = false;
#endif
            //#if DEBUG
            Settings.AddHeader("Debug", collapsedByDefault: isDebugHeaderCollapsed);
            Settings.AddButton(name: "Repair house pipes", onClick: () => plumberService.RepairParentsHousePipes());
            Settings.AddButton(name: "Freeze and burst pipes", onClick: () => plumberService.BurstParentsHousePipes());
            //#endif
        }

        private void Mod_OnLoad()
        {
            // Called once, when mod is loading after game is fully loaded
            AssetBundle ab = LoadAssets.LoadBundle(this, "calltheplumber.unity3d");
            GameObject plumbingBillPrefab = ab.LoadAsset<GameObject>("PlumbingBill");
            GameObject plumbingBill = GameObject.Instantiate(plumbingBillPrefab);
            plumbingBill.name = "PlumbingBill";
            plumbingBill.transform.SetParent(GameObject.Find("Sheets/").transform);
            plumbingBill.SetActive(false);

            GameObject vanillaTaxiAd = GameObject.Find("PERAPORTTI").transform.Find("Building/LOD/InfoBoard/TaxiJob").gameObject;
            GameObject plumberAdPrefab = ab.LoadAsset<GameObject>("PlumberAd");
            GameObject plumberAd = GameObject.Instantiate(plumberAdPrefab);
            plumberAd.name = "PlumberAd";
            plumberAd.transform.SetParent(vanillaTaxiAd.transform.parent, worldPositionStays: false);
            plumberAd.transform.localPosition = new Vector3(0.3500006f, 0f, -0.06900002f);
            plumberAd.transform.localEulerAngles = new Vector3(270f, 180f, 0f);
            plumberAd.transform.localScale = new Vector3(1f, 1f, 1f);

            ab.Unload(false);

            InitializeMod();
        }

        private void Mod_OnSave()
        {
            SaveLoad.WriteValue(this, "plumberState", plumberService.GetPlumberState());
            SaveLoad.WriteValue(this, "invoiceCost", plumberService.GetInvoiceCost());

            TimeScheduler.ScheduledAction plumberAction = plumberService.PlumberScheduledAction;
            if (TimeScheduler.ScheduledActions.Contains(plumberAction))
            {
                SaveLoad.WriteValue(this, "plumberActionDay", plumberAction.Day);
                SaveLoad.WriteValue(this, "plumberActionHour", plumberAction.Hour);
                SaveLoad.WriteValue(this, "plumberActionMinute", plumberAction.Minute);
                SaveLoad.WriteValue(this, "plumberActionMethodName", plumberAction.Action.Method.Name);
            }
            else
            {
                SaveLoad.DeleteValue(this, "plumberActionDay");
                SaveLoad.DeleteValue(this, "plumberActionHour");
                SaveLoad.DeleteValue(this, "plumberActionMinute");
                SaveLoad.DeleteValue(this, "plumberActionMethodName");
            }
        }
    }
}

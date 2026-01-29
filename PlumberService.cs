using HutongGames.PlayMaker;
using MSCCoreLibrary;
using MSCLoader;
using UnityEngine;

namespace CallThePlumber
{
    enum PlumberState
    {
        Available,
        WaitingPayment,
        EnRoute,
        Working,
        Finished,
    }

    internal class PlumberService
    {
        internal class Config()
        {
            // Settings
            public float minCost, maxCost;
            public float hoursToInvoice, hoursToRepairStart, hoursToRepairFinish;

            // Ad at PSK
            public string adSubtitles;

            // Phone call
            public string phoneNumber, callSubtitles, callAudioVariation;
            public float callLength, callDistance;

            // Runtime state
            public PlumberState currentState;
            public float invoiceCost;
        }

        private const string repairPlumbingEventName = "REPAIRPLUMBING";
        private const string burstStateName = "State 2";

        public TimeScheduler.ScheduledAction PlumberScheduledAction { get; private set; } = null;

        private static PlumberService _instance;

        private PlumberService() { }

        public static PlumberService Instance
        {
            get
            {
                _instance ??= new PlumberService();

                return _instance;

            }
        }

        Config config;

        GameObject plumberPhone, plumberAd, plumbingBillEnvelope;
        PlayMakerFSM pipesLogicFsm;

        internal void Initialize(Config config)
        {
            this.config = config;

            pipesLogicFsm = GameObject.Find("YARD/Building/Dynamics/Pipes").GetPlayMaker("Logic");

            PatchParentsHousePipesFSM();
            InitializePlumberPhone();
            InitializePlumbingBillEnvelope();
            InitializePlumbingAd();

            ModConsole.Log($"[CallThePlumber] PlumberService initialized. currentState = {config.currentState}");
        }

        void InitializePlumberPhone()
        {
            GameObject phones = GameObject.Find("CARPARTS/PARTSYSTEM/PhoneNumbers");

            plumberPhone = new GameObject($"{config.phoneNumber}disabled");
            plumberPhone.transform.SetParent(phones.transform);

            // FSM needed to interact with MWC phone system
            string readyStateName = "Ready";
            string calledStateName = "Called";
            string calledEventName = "CALLED";

            PlayMakerFSM phoneDataFSM = plumberPhone.AddComponent<PlayMakerFSM>();
            phoneDataFSM.FsmName = "Data";

            phoneDataFSM.AddVariable(new FsmFloat { Name = "CallerCallLenght", Value = config.callLength }); // "Lenght" in name is intentionally misspelled to match MWC
            phoneDataFSM.AddVariable(new FsmFloat { Name = "Distance", Value = config.callDistance });

            phoneDataFSM.AddVariable(new FsmString { Name = "CallerAudioVariation", Value = config.callAudioVariation });
            phoneDataFSM.AddVariable(new FsmString { Name = "CallerSubtitle", Value = config.callSubtitles });
            phoneDataFSM.AddVariable(new FsmString { Name = "Number", Value = config.phoneNumber });

            phoneDataFSM.AddVariable(new FsmInt { Name = "Stage", Value = 1 }); // can be anything but 0, otherwise it handles like magazine calls


            // TODO: Refactor using PlayMakerExtensions, but there seems to be a bug with GlobalTransitions via extensions.
            var fsm = phoneDataFSM.Fsm;

            fsm.States = [];
            FsmState readyState = phoneDataFSM.AddState(readyStateName);
            FsmState calledState = phoneDataFSM.AddState(calledStateName);

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

            if (config.currentState == PlumberState.Available)
            {
                plumberPhone.name = config.phoneNumber;
                plumberPhone.SetActive(true);
            }
        }

        void InitializePlumbingBillEnvelope()
        {
            GameObject parentsHouseMailbox = GameObject.Find("YARD/Others/PlayerMailBox1/");

            plumbingBillEnvelope = new("EnvelopePlumbingBill")
            {
                layer = LayerMask.NameToLayer("DontCollide"),
            };
            plumbingBillEnvelope.SetActive(config.currentState == PlumberState.WaitingPayment);
            plumbingBillEnvelope.transform.SetParent(parentsHouseMailbox.transform, worldPositionStays: false);
            plumbingBillEnvelope.transform.localPosition = new Vector3(0.024f, -0.001f, 0.168f);
            plumbingBillEnvelope.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            plumbingBillEnvelope.transform.localScale = new Vector3(1f, 1f, 1f);

            var collider = plumbingBillEnvelope.AddComponent<CapsuleCollider>();
            collider.radius = 0.015f;
            collider.height = 0.3f;
            collider.isTrigger = true;

            MailboxEnvelope plumbingInvoice = plumbingBillEnvelope.AddComponent<MailboxEnvelope>();
            plumbingInvoice.billValue = config.invoiceCost;
            plumbingInvoice.onInvoicePaid = () => HandleInvoicePaid();
        }

        void InitializePlumbingAd()
        {
            plumberAd = GameObject.Find("PERAPORTTI").transform.Find("Building/LOD/InfoBoard/PlumberAd").gameObject;

            plumberAd.SetActive(config.currentState == PlumberState.Available);

            plumberAd.AddComponent<SubtitlesOnLook>();
            SubtitlesOnLook subtitlesComponent = plumberAd.GetComponent<SubtitlesOnLook>();
            subtitlesComponent.subtitleText = config.adSubtitles;
            subtitlesComponent.collider = plumberAd.GetComponent<Collider>();
        }

        void PatchParentsHousePipesFSM()
        {
            string workingStateName = "State 1";
            string repairingStateName = "Repairing";

            FsmState repairingState = pipesLogicFsm.AddState(repairingStateName);

            pipesLogicFsm.GetState(burstStateName).AddTransition(repairPlumbingEventName, repairingStateName);
            repairingState.AddTransition("FINISHED", workingStateName);

            pipesLogicFsm.FsmInject(stateName: repairingStateName, hook: () =>
            {
                config.currentState = PlumberState.Finished;
                ModConsole.Log($"[CallThePlumber] PlumberState -> {GetPlumberState()}");

                FsmFloat iceFsmVar = pipesLogicFsm.GetVariable<FsmFloat>("Ice");
                FsmBool pipesOkFsmVar = pipesLogicFsm.GetVariable<FsmBool>("PipesOK");

                iceFsmVar.Value = 0f;
                pipesOkFsmVar.Value = true;

            });

            pipesLogicFsm.FsmInject(stateName: burstStateName, hook: OnHousePipesBurst);
        }

        public void UpdateCostSettings(float minCost, float maxCost)
        {
            config.minCost = minCost;
            config.maxCost = maxCost;
        }

        public static string FormatPhoneNumber(string phoneNumber)
        {
            int length = phoneNumber.Length;

            if (length < 8) return phoneNumber;

            string areaCode = phoneNumber.Substring(0, length - 6);
            string number = phoneNumber.Substring(length - 6);

            return $"{areaCode}-{number}";
        }

        float GetRandomCost()
        {
            return UnityEngine.Random.Range(config.minCost, config.maxCost);
        }

        public void RepairParentsHousePipes()
        {
            pipesLogicFsm.SendEvent(repairPlumbingEventName);
        }

        public void BurstParentsHousePipes()
        {
            // For debug purposes
            if (!(pipesLogicFsm.ActiveStateName == "State 1")) return;

            pipesLogicFsm.GetVariable<FsmFloat>("Ice").Value = 100.5f;
            pipesLogicFsm.SendEvent("BURST");
        }

        public void OnHousePipesBurst()
        {
            config.currentState = PlumberState.Available;
            ModConsole.Log($"[CallThePlumber] PlumberState -> {GetPlumberState()}");

            plumberPhone.name = config.phoneNumber;
            plumberPhone.SetActive(true);
            plumberAd.SetActive(true);
        }

        void SendPlumbingBillEnvelope()
        {
            config.invoiceCost = GetRandomCost();
            plumbingBillEnvelope.GetComponent<MailboxEnvelope>().billValue = config.invoiceCost;
            plumbingBillEnvelope.SetActive(true);
        }

        void HandlePlumberCalled()
        {
            config.currentState = PlumberState.WaitingPayment;
            ModConsole.Log($"[CallThePlumber] PlumberState -> {GetPlumberState()}");

            plumberPhone.name = $"{config.phoneNumber}disabled";
            plumberAd.SetActive(false);

            var actionTime = GameTimeExtensions.GameDateTime.Now();
            actionTime.AdvanceHours(config.hoursToInvoice);
            PlumberScheduledAction = TimeScheduler.ScheduleAction(actionTime.Hour, actionTime.Minute, SendPlumbingBillEnvelope, day: actionTime.Day, oneTimeAction: true);
        }

        void HandleInvoicePaid()
        {
            config.currentState = PlumberState.EnRoute;
            ModConsole.Log($"[CallThePlumber] PlumberState -> {GetPlumberState()}");

            var actionTime = GameTimeExtensions.GameDateTime.Now();
            actionTime.AdvanceHours(config.hoursToRepairStart);
            PlumberScheduledAction = TimeScheduler.ScheduleAction(actionTime.Hour, actionTime.Minute, StartPlumberWork, day: actionTime.Day, oneTimeAction: true);
        }

        void StartPlumberWork()
        {
            config.currentState = PlumberState.Working;
            ModConsole.Log($"[CallThePlumber] PlumberState -> {GetPlumberState()}");

            var actionTime = GameTimeExtensions.GameDateTime.Now();
            actionTime.AdvanceHours(config.hoursToRepairFinish);
            PlumberScheduledAction = TimeScheduler.ScheduleAction(actionTime.Hour, actionTime.Minute, FinishPlumberWork, day: actionTime.Day, oneTimeAction: true);
        }

        void FinishPlumberWork()
        {
            RepairParentsHousePipes();
        }

        public void SetHoursToInvoice(float value)
        {
            config.hoursToInvoice = value;
        }

        public void SetHoursToRepairStart(float value)
        {
            config.hoursToRepairStart = value;
        }

        public void SetHoursToRepairFinish(float value)
        {
            config.hoursToRepairFinish = value;
        }

        public PlumberState GetPlumberState()
        {
            return config.currentState;
        }

        public float GetInvoiceCost()
        {
            return config.invoiceCost;
        }
    }
}

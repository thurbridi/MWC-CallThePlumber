using HutongGames.PlayMaker;
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
            
            // Ad at PSK
            public string adSubtitles;
            
            // Phone call
            public string phoneNumber, callSubtitles, callAudioVariation;
            public float callLength, callDistance;

            // Runtime state
            public PlumberState currentState;
        }

        private const string repairPlumbingEventName = "REPAIRPLUMBING";
        private const string burstStateName = "State 2";

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

            phoneDataFSM.AddVariable(new FsmString { Name = "CallerAudioVariation", Value = config.callAudioVariation});
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
            plumbingBillEnvelope.SetActive(false);
            plumbingBillEnvelope.transform.SetParent(parentsHouseMailbox.transform, worldPositionStays: false);
            plumbingBillEnvelope.transform.localPosition = new Vector3(0.024f, -0.001f, 0.168f);
            plumbingBillEnvelope.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            plumbingBillEnvelope.transform.localScale = new Vector3(1f, 1f, 1f);

            var collider = plumbingBillEnvelope.AddComponent<CapsuleCollider>();
            collider.radius = 0.015f;
            collider.height = 0.3f;
            collider.isTrigger = true;


            MailboxEnvelope plumbingInvoice = plumbingBillEnvelope.AddComponent<MailboxEnvelope>();
            plumbingInvoice.onInvoicePaid = () => HandleInvoicePaid();
        }

        void InitializePlumbingAd()
        {
            plumberAd = GameObject.Find("PERAPORTTI").transform.Find("Building/LOD/InfoBoard/PlumberAd").gameObject;

            if (config.currentState == PlumberState.Available) plumberAd.SetActive(true);
            else plumberAd.SetActive(false);

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
                plumberPhone.name = $"{config.phoneNumber}disabled";
                plumberPhone.SetActive(false);

                FsmFloat iceFsmVar = pipesLogicFsm.GetVariable<FsmFloat>("Ice");
                FsmBool pipesOkFsmVar = pipesLogicFsm.GetVariable<FsmBool>("PipesOK");

                iceFsmVar.Value = 0f;
                pipesOkFsmVar.Value = true;

                config.currentState = PlumberState.Finished;
            });

            pipesLogicFsm.FsmInject(stateName: burstStateName, hook: () =>
            {
                plumberPhone.name = config.phoneNumber;
                plumberPhone.SetActive(true);
                plumberAd.SetActive(true);

                config.currentState = PlumberState.Available;
            });
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
            config.currentState = PlumberState.Finished;
        }

        public void BurstParentsHousePipes()
        {
            // For debug purposes
            if (!(pipesLogicFsm.ActiveStateName == "State 1")) return;

            pipesLogicFsm.GetVariable<FsmFloat>("Ice").Value = 100.5f;
            pipesLogicFsm.SendEvent("BURST");
            config.currentState = PlumberState.Available;
        }

        void HandlePlumberCalled()
        {
            config.currentState = PlumberState.WaitingPayment;

            plumberPhone.name = $"{config.phoneNumber}disabled";
            plumberAd.SetActive(false);
            
            SendPlumbingBillEnvelope();
        }

        void HandleInvoicePaid()
        {
            config.currentState = PlumberState.EnRoute;
            RepairParentsHousePipes();
        }

        public PlumberState GetPlumberState()
        {
            return config.currentState;
        }

        void SendPlumbingBillEnvelope()
        {
            float cost = GetRandomCost();
            plumbingBillEnvelope.GetComponent<MailboxEnvelope>().billValue = cost;
            plumbingBillEnvelope.SetActive(true);
        }
    }
}

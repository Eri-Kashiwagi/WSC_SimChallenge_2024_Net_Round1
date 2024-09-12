using O2DESNet;
using WSC_SimChallenge_2024_Net.Activity;

namespace WSC_SimChallenge_2024_Net.PortSimulation.Entity
{
    public class Vessel
    {
        public int Week;
        public string Id;
        public int AlreadyAllocatedQcCount = 0;
        public int RequiredQcCount = 3;
        public Berth AllocatedBerth = null;
        public List<QC> AllocatedQCs = new List<QC>();
        public QCLine UsedQCLine = null;
        public Dictionary<string, int> DischargingContainersInformation = new Dictionary<string, int>();//orig,destination, and quantity of containers
        public Dictionary<string, int> LoadingContainersInformation = new Dictionary<string, int>();//orig,destination, and quantity of containers
        public DateTime ArrivalTime;
        public DateTime StartBerthingTime;
        public override string ToString()
        {
            return $"Vessel[{Id}]";
        }

        public class Waiting : BaseActivity<Vessel>
        {
            public Waiting(bool debugMode = false, int seed = 0) : base(nameof(Waiting), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofVessel;
                TimeSpan = TimeSpan.Zero;
            }

            public override void Start(Vessel vessel)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Start({vessel})");
                vessel.ArrivalTime = ClockTime;
                HourCounter.ObserveChange(1);
                ProcessingList.Add(vessel);
                PendingList.Remove(vessel);
                ReadyToStartList.Remove(vessel);
                Schedule(() => Complete(vessel), TimeSpan);
                EmitOnStart(vessel);
            }
        }
        public class Berthing : BaseActivity<Vessel>
        {
            public Berthing(bool debugMode = false, int seed = 0) : base(nameof(Berthing), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofVessel;
                NeedExtTryStart = true; //switch off when tesing vessel generator
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public override void RequestToStart(Vessel vessel)
            {
                PendingList.Add(vessel);
                Schedule(() => AttemptToStart(), TimeSpan.FromMicroseconds(1)); // Caution: New added load and AttemptToStart load may not be the same
                                                                                //AttemptToStart(); // Caution: New added load and AttemptToStart load may not be the same
            }

            public override void TryStart(Object obj)
            {
                if(_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryStart({obj})");

                Berth? berth = (obj is Berth) ? (obj as Berth) : null;
                QC? qc = (obj is QC) ? (obj as QC) : null;
                //Console.WriteLine($"{berth},{qc}");
                if (berth == null & qc == null) { return; }
                if (berth != null)// The coming resource is berth
                {
                    //Console.WriteLine($"{berth},{ClockTime}");
                    Vessel vessel = PendingList.Find(v => v.AllocatedBerth == berth);

                    if (vessel.AllocatedQCs.Count == vessel.RequiredQcCount && vessel.AllocatedBerth != null)
                    {
                        ReadyToStartList.Add(vessel);
                        AttemptToStart();
                    }
                }
                else if (qc != null) // The coming resource is qc
                {
                    //Console.WriteLine($"{qc},{ClockTime}");
                    Vessel vessel = PendingList.Find(v => v == qc.ServedVessel);
                    if (vessel == null) 
                    {
                        foreach (var v in PendingList) Console.WriteLine($"PendingList:{v.Id},{v.ArrivalTime}");
                        foreach (var v in CompletedList) Console.WriteLine($"CompletedList:{v.Id},{v.ArrivalTime},{ClockTime},{v.LoadingContainersInformation.Sum(lo=> lo.Value)},{v.AllocatedBerth}");
                        Console.WriteLine($"{berth},{berth == null},{qc},{qc == null}");
                        Console.WriteLine($"{qc},{qc.ServedVessel},{PendingList.Count},{ClockTime}"); 
                    }
                    vessel.AllocatedQCs.Add(qc);

                    if (vessel.AllocatedQCs.Count == vessel.RequiredQcCount && vessel.AllocatedBerth != null)
                    {
                        ReadyToStartList.Add(vessel);
                        AttemptToStart();
                    }
                }
            }

            public override void Start(Vessel vessel)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Start({vessel})");
                vessel.StartBerthingTime = ClockTime;
                HourCounter.ObserveChange(1);
                ProcessingList.Add(vessel);
                PendingList.Remove(vessel);
                ReadyToStartList.Remove(vessel);
                Schedule(() => Complete(vessel), TimeSpan);
                EmitOnStart(vessel);
            }

            public override void TryFinish(Object obj)
            {
                if(_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj})");

                QCLine? qcLine = (obj is QCLine) ? (obj as QCLine) : null;
                if (qcLine == null) { return; }
                foreach (var vessel in CompletedList) 
                {
                    if (qcLine.ServedVessel == vessel)
                    {
                        ReadyToFinishList.Add(vessel);
                        AttemptToFinish(vessel);
                        //Console.WriteLine($"Finish:{vessel}");
                        break;
                    }
                }
            }
        }
    }
}
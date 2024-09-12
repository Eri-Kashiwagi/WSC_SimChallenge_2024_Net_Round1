using O2DESNet;
using System.Data;
using System.Linq;
using WSC_SimChallenge_2024_Net.Activity;

namespace WSC_SimChallenge_2024_Net.PortSimulation.Entity
{
    public class Container
    {
        public int Week;
        public Vessel DischargingVessel;
        public Vessel LoadingVessel;
        public string DischargingVesselID { get; set; }
        public string LoadingVesselID { get; set; }
        public YardBlock BlockStacked { get; set; }
        public QC TargetQC { get; set; }
        public ControlPoint CurrentLocation { get; set; }
        public DateTime ArrivalTime { get; set; }
        public AGV AGVTaken { get; set; }
        public bool InDischarging;
        public string Id;
        public override string ToString()
        {
            return $"Container[{Id}]";
        }

        public class BeingDischarged : BaseActivity<Container>
        {
            public event Action<Container> OnRequestToStart = container=> { };
            public BeingDischarged(bool debugMode = false, int seed = 0) : base(nameof(BeingDischarged), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofContainer;
                NeedExtTryStart = true;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public void GenerateContainers(QCLine qCLine)//Connect QCLine.Dicharging.OnStart
            {
                //Console.WriteLine($"GenerateContainers{qCLine.ServedVessel}");
                Vessel servedVessel = qCLine.ServedVessel;
                int idCounter = 0;
                for (int dest = 0; dest < servedVessel.DischargingContainersInformation.Count; dest++)
                {
                    int _dest = dest;
                    //Console.WriteLine(servedVessel.DischargingContainersInformation["vessel " + dest.ToString()]);
                    for (int i = 0; i < servedVessel.DischargingContainersInformation["vessel " + dest.ToString()]; i++)
                    {
                        string id = $"{servedVessel.Id}, {servedVessel.ArrivalTime}, {idCounter}";
                        Container container = new Container
                        {
                            Id = id,
                            DischargingVesselID = servedVessel.Id,
                            LoadingVesselID = "vessel " + dest.ToString(),
                            ArrivalTime = servedVessel.ArrivalTime,
                            InDischarging = true,
                            Week = servedVessel.Week
                        };
                        OnRequestToStart.Invoke(container);
                        //Console.WriteLine($"{container},{ClockTime}");
                        RequestToStart(container);                        
                        idCounter++;
                    }
                }
            }

            public override void TryStart(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryStart({obj})");

                QC? qc = (obj is QC) ? (obj as QC) : null;
                if (qc == null) { return; }

                Container chosenContainer = PendingList.Find(con=> con== qc.HeldContainer);
                if (chosenContainer != null)
                {
                    ReadyToStartList.Add(chosenContainer);
                    chosenContainer.CurrentLocation = qc.CP;
                    AttemptToStart();
                }
                else
                {
                    Console.WriteLine($"Error：{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryStart({obj}): we don't find the matched QC");
                    return;
                }
            }

            public override void TryFinish(object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Finish({obj})");

                QC? qc = (obj is QC) ? (obj as QC) : null;
                if (qc == null) { return; }

                Container chosenContainer = CompletedList.Find(con => con == qc.HeldContainer);
                if (chosenContainer != null)
                {
                    ReadyToFinishList.Add(chosenContainer);
                    AttemptToFinish(chosenContainer);
                    PortSimModel.Discharging++;
                }
                else
                {
                    Console.WriteLine($"Error：{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj}): we don't find the matched QC");
                    return;
                }
            }
        }

        public class TransportingToYard : BaseActivity<Container>
        {
            public List<AGV> AGVPendingList = new List<AGV>();
            public TransportingToYard(bool debugMode = false, int seed = 0) : base(nameof(TransportingToYard), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofContainer;
                NeedExtTryStart = true;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public override void TryStart(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryStart({obj})");

                AGV? agv = (obj is AGV) ? (obj as AGV) : null;
                if (agv == null) { return; }

                Container container = PendingList.Find(con => con == agv.LoadedContainer);
                if (container != null)
                {
                    ReadyToStartList.Add(container);
                    AttemptToStart();
                }
                else { Console.WriteLine($"Error1：{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj}): we don't find the matched container"); }

            }


            public override void AttemptToFinish(Container container)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.AttemptToFinish({container})");
                if (CompletedList.Contains(container) && NeedExtTryFinish && ReadyToFinishList.Contains(container))// Message before entity
                {
                    Finish(container);
                }
                else if (CompletedList.Contains(container) && NeedExtTryFinish && !ReadyToFinishList.Contains(container)
                    && AGVPendingList.Any(agv => agv.LoadedContainer == container))
                {// Entity before Message
                    AGV agv = AGVPendingList.Find(agv => agv.LoadedContainer == container);
                    AGVPendingList.Remove(agv);
                    ReadyToFinishList.Add(container);
                    Finish(container);
                }
            }


            public override void TryFinish(object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Finish({obj})");

                AGV? agv = (obj is AGV) ? (obj as AGV) : null;
                if (agv == null) { return; }
                AGVPendingList.Add(agv);

                Container container = CompletedList.Find(con => con == agv.LoadedContainer);
                if (container != null)
                {
                    AGVPendingList.Remove(agv);
                    ReadyToFinishList.Add(container);
                    AttemptToFinish(container);
                }
            }
        }

        public class BeingStacked : BaseActivity<Container>
        {
            public BeingStacked(bool debugMode = false, int seed = 0) : base(nameof(BeingStacked), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofContainer;
                NeedExtTryStart = true;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public override void TryStart(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryStart({obj})");

                YC? yc = (obj is YC) ? (obj as YC) : null;
                if (yc == null) { return; }

                Container container = PendingList.Find(con => con == yc.HeldContainer);
                if (container != null)
                {
                    ReadyToStartList.Add(container);
                    AttemptToStart();
                }
                else { Console.WriteLine($"Error11：{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj}): we don't find the matched container"); }
            }


            public override void TryFinish(object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Finish({obj})");

                YC? yc = (obj is YC) ? (obj as YC) : null;
                if (yc == null) { return; }

                Container container = CompletedList.Find(con => con == yc.HeldContainer);
                if (container != null)
                {
                    ReadyToFinishList.Add(container);
                    AttemptToFinish(container);
                }
            }

            public override void Depart(Container container)
            {
                if (ReadyToDepartList.Contains(container))
                {
                    if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Depart({container})");
                    HourCounter.ObserveChange(-1);
                    container.InDischarging = false;
                    container.BlockStacked.StackingContainer(container);
                    ReadyToDepartList.Remove(container);
                    AttemptToStart();
                }
            }
        }

        public class Dwelling : BaseActivity<Container>
        {
            //public List<Container> Checked = new List<Container>();
            public Dwelling(bool debugMode = false, int seed = 0) : base(nameof(Dwelling), debugMode, seed)
            {
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }
            public override void TryFinish(object obj)
            {
                if(_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Finish({obj})");

                QCLine? qcLine = (obj is QCLine) ? (obj as QCLine) : null;
                if (qcLine == null) { return; }

                if (qcLine.LoadingContainersInformation != null)
                {
                    foreach (var infor in qcLine.LoadingContainersInformation)
                    {
                        for (int i = 0; i < infor.Value; i++)
                        {
                            var stockedContainer = CompletedList
                                    .Where(container => container.LoadingVesselID == qcLine.ServedVessel.Id) // Filter by LoadingVesselID
                                    .OrderBy(container => container.ArrivalTime) // Order by arrivaltime
                                    .FirstOrDefault(); // Take the first one, which will be the one with the earliest arrival time

                            if (stockedContainer != null)
                            {
                                stockedContainer.BlockStacked.UnstackingContainer(stockedContainer);
                                ReadyToFinishList.Add(stockedContainer);
                                AttemptToFinish(stockedContainer);
                            }
                            else { Console.WriteLine($"Error：{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj}): we don't have containers to {infor.Key}"); }
                        }
                    }
                }
                else { }//There are not containers for loading
            }
        }

        public class BeingUnstacked : BaseActivity<Container>
        {
            public BeingUnstacked(bool debugMode = false, int seed = 0) : base(nameof(BeingUnstacked), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofContainer;
                NeedExtTryStart = true;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public override void TryStart(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryStart({obj})");

                YC? yc = (obj is YC) ? (obj as YC) : null;
                if (yc == null) { return; }

                Container container = PendingList.Find(con => con == yc.HeldContainer);
                if (container != null)
                {
                    ReadyToStartList.Add(container);
                    AttemptToStart();
                }
                else { Console.WriteLine($"Error11：{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj}): we don't find the matched container"); }
            }


            public override void TryFinish(object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Finish({obj})");

                YC? yc = (obj is YC) ? (obj as YC) : null;
                if (yc == null) { return; }

                Container container = CompletedList.Find(con => con == yc.HeldContainer);
                if (container != null)
                {
                    ReadyToFinishList.Add(container);
                    AttemptToFinish(container);
                }
            }
        }

        public class TransportingToQuaySide : BaseActivity<Container>
        {
            public List<AGV> AGVPendingList = new List<AGV>();
            public TransportingToQuaySide(bool debugMode = false, int seed = 0) : base(nameof(TransportingToQuaySide), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofContainer;
                NeedExtTryStart = true;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public override void TryStart(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryStart({obj})");

                AGV? agv = (obj is AGV) ? (obj as AGV) : null;
                if (agv == null) { return; }

                Container container = PendingList.Find(con => con == agv.LoadedContainer);
                if (container != null)
                {
                    ReadyToStartList.Add(container);
                    AttemptToStart();
                }
                else { Console.WriteLine($"Error3：{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj}): we don't find the matched container"); }

            }

            public override void AttemptToFinish(Container container)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.AttemptToFinish({container})");
                if (CompletedList.Contains(container) && NeedExtTryFinish && ReadyToFinishList.Contains(container))// Message before entity
                {
                    //Console.WriteLine("Finish1");
                    Finish(container);
                }
                else if (CompletedList.Contains(container) && NeedExtTryFinish && !ReadyToFinishList.Contains(container)
                    && AGVPendingList.Any(agv => agv.LoadedContainer == container))
                {// Entity before Message
                    //Console.WriteLine("Finish2");
                    AGV agv = AGVPendingList.Find(agv => agv.LoadedContainer == container);
                    AGVPendingList.Remove(agv);
                    ReadyToFinishList.Add(container);
                    Finish(container);
                }
            }

            public override void TryFinish(object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj})");
                AGV? agv = (obj is AGV) ? (obj as AGV) : null;
                if (agv == null) { return; }
                AGVPendingList.Add(agv);
                //Console.WriteLine($"TryFinish:{agv},{agv.LoadedContainer}");
                
                Container container = CompletedList.Find(con => con == agv.LoadedContainer);
                if (container != null)
                {
                    AGVPendingList.Remove(agv);
                    ReadyToFinishList.Add(container);
                    AttemptToFinish(container);
                }
            }
        }

        public class BeingLoaded : BaseActivity<Container>
        {
            public BeingLoaded(bool debugMode = false, int seed = 0) : base(nameof(BeingLoaded), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofContainer;
                NeedExtTryStart = true;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public override void TryStart(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryStart({obj})");

                QC? qc = (obj is QC) ? (obj as QC) : null;
                if (qc == null) { return; }

                Container heldContainer = PendingList.Find(container => container == qc.HeldContainer);
                if (heldContainer != null)
                { 
                    ReadyToStartList.Add(heldContainer);                    
                }
                else
                {
                    Console.WriteLine($"There are not containers served for {qc.ServedVessel}");
                }
                AttemptToStart();
            }

            public override void TryFinish(object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Finish({obj})");

                QC? qc = (obj is QC) ? (obj as QC) : null;
                if (qc == null) { return; }

                Container heldContainer = CompletedList.Find(container => container == qc.HeldContainer);
                if (heldContainer != null)
                {
                    ReadyToFinishList.Add(heldContainer);
                    AttemptToFinish(heldContainer);

                    PortSimModel.Loading++;
                }
                else
                {
                    Console.WriteLine($"There are not containers served for {qc.ServedVessel}");
                }
            }
        }

    }
}

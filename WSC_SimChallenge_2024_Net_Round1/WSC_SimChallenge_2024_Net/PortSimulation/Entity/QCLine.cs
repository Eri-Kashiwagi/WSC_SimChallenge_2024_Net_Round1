using O2DESNet;
using System.Linq;
using WSC_SimChallenge_2024_Net.Activity;

namespace WSC_SimChallenge_2024_Net.PortSimulation.Entity
{
    public class QCLine
    {
        public string Id;
        public Vessel ServedVessel;
        public Dictionary<string, int> DischargingContainersInformation = new Dictionary<string, int>();//orig,destination, and quantity of containers
        public Dictionary<string, int> LoadingContainersInformation = new Dictionary<string, int>();//orig,destination, and quantity of containers
        public override string ToString()
        {
            return $"QCLine[{Id}]";
        }

        public class Discharging: BaseActivity<QCLine>
        {
            public Discharging(bool debugMode = false, int seed = 0) : base(nameof(Discharging), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofQCLine;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public void CreateQCLine(Vessel vessel)//connected with OnStart of Vessel.Berthing
            {
                DateTime clockTime = ClockTime;
                QCLine qcLine = new QCLine { Id = $"({clockTime},{vessel.Id})", ServedVessel = vessel };
                qcLine.DischargingContainersInformation = new Dictionary<string, int>(vessel.DischargingContainersInformation);
                qcLine.LoadingContainersInformation = vessel.LoadingContainersInformation == null? null : new Dictionary<string, int>(vessel.LoadingContainersInformation);
                RequestToStart(qcLine);
            }

            public override void TryFinish(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj})");

                Container ? container = (obj is Container) ? (obj as Container) : null;
                if (container == null) { return; };

                QCLine workedQCLine = CompletedList.Find(qcLine => qcLine.ServedVessel.Id == container.DischargingVesselID);//find the QCLine
                if (workedQCLine != null ) 
                {
                    //Console.WriteLine($"{container.LoadingVesselID},{workedQCLine.DischargingContainersInformation[container.LoadingVesselID]}");
                    workedQCLine.DischargingContainersInformation[container.LoadingVesselID] -= 1;
                    bool allConatinersBeingDischaged = true;
                    foreach (var info in workedQCLine.DischargingContainersInformation)
                    {
                        if (info.Value != 0)
                        {
                            allConatinersBeingDischaged = false;
                            break;
                        }
                    }

                    if (allConatinersBeingDischaged)
                    {
                        ReadyToFinishList.Add(workedQCLine);
                        AttemptToFinish(workedQCLine);
                        workedQCLine.DischargingContainersInformation.Clear();
                    }
                }
                else 
                {
                    Console.WriteLine($"Error：{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj}): we don't find the QCLine for container {container.Id}");
                    return;
                }                
            }
        }

        public class Loading : BaseActivity<QCLine>
        {
            public Loading(bool debugMode = false, int seed = 0) : base(nameof(Loading), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofQCLine;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }


            public override void AttemptToFinish(QCLine qCLine)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.AttemptToFinish({qCLine})");
                if (CompletedList.Contains(qCLine) && (!NeedExtTryFinish || ReadyToFinishList.Contains(qCLine) || qCLine.LoadingContainersInformation == null))// check whether need loading
                {
                    Finish(qCLine);
                }
            }

            public override void TryFinish(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj})");

                Container? container = (obj is Container) ? (obj as Container) : null;
                if (container == null) { return; };
                
                QCLine workedQCLine = CompletedList.Find(qcLine => qcLine.ServedVessel.Id == container.LoadingVesselID && qcLine.ServedVessel.Week == container.Week+1);//find the QCLine
                if (workedQCLine != null)
                {
                    workedQCLine.LoadingContainersInformation[container.DischargingVesselID] -= 1;
                    bool allConatinersBeingLoaded = true;
                    foreach (var info in workedQCLine.LoadingContainersInformation)
                    {
                        if (info.Value != 0)
                        {
                            allConatinersBeingLoaded = false;
                            break;
                        }
                    }

                    if (allConatinersBeingLoaded)
                    {
                        ReadyToFinishList.Add(workedQCLine);
                        AttemptToFinish(workedQCLine);
                        workedQCLine.LoadingContainersInformation = null;
                    }
                }
                else
                {
                    Console.WriteLine($"Error：{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj}): we don't find the QCLine for container {container.Id}");
                    return;
                }
            }
        }
    }
}

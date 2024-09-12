using O2DESNet;
using O2DESNet.Standard;
using System;
using WSC_SimChallenge_2024_Net.Activity;
using WSC_SimChallenge_2024_Net.StrategyMaking;

namespace WSC_SimChallenge_2024_Net.PortSimulation.Entity
{
    public class Berth
    {
        public int CurrentWorkQC = 0;
        public string Id;
        public Vessel BerthedVessel = null;
        public List<QC> EquippedQCs = new List<QC>();
        public override string ToString()
        {
            return $"Berth[{Id}]";
        }

        public class BeingIdle : BaseActivity<Berth>
        {
            public List<Vessel> VesselPendingList = new List<Vessel>();

            public BeingIdle(bool debugMode = false, int seed = 0) : base(nameof(BeingIdle), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofBerth;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public override void AttemptToFinish(Berth berth)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  Berth{ActivityName}.AttemptToFinish({berth})");
                if (CompletedList.Contains(berth) && NeedExtTryFinish && ReadyToFinishList.Contains(berth))// Message before entity
                {
                    Finish(berth);
                }
                else if (CompletedList.Contains(berth) && NeedExtTryFinish && !ReadyToFinishList.Contains(berth) &&
                    VesselPendingList.Count > 0)// Entity before messsage, there are vessel waiting
                {
                    Berth allocatedBerth = DecisionMaker.CustomeizedAllocatedBerth(VesselPendingList[0]);//allocate berth to vessel;
                    allocatedBerth = allocatedBerth == null ? Default.CustomeizedAllocatedBerth(VesselPendingList[0]) : allocatedBerth;

                    allocatedBerth.BerthedVessel = VesselPendingList[0];
                    VesselPendingList[0].AllocatedBerth = allocatedBerth;
                    VesselPendingList.RemoveAt(0);
                    Finish(allocatedBerth);
                }
            }

            public override void TryFinish(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj})");

                Vessel? vessel = (obj is Vessel) ? (obj as Vessel) : null;
                if (vessel == null){return;};
                VesselPendingList.Add(vessel);

                int numofTryFinish = Math.Min(CompletedList.Count, VesselPendingList.Count);
                List<Berth> tmpBerths = new List<Berth>();

                for (int i = 0; i < numofTryFinish; i++)
                {
                    Berth allocatedBerth = DecisionMaker.CustomeizedAllocatedBerth(VesselPendingList[0]);//allocate berth to vessel;
                    allocatedBerth = allocatedBerth == null ? Default.CustomeizedAllocatedBerth(VesselPendingList[0]) : allocatedBerth;

                    if (allocatedBerth != null && CompletedList.Contains(allocatedBerth))// we do allocate berth
                    {
                        allocatedBerth.BerthedVessel = VesselPendingList[0];//Allocate berth
                        VesselPendingList[0].AllocatedBerth = allocatedBerth;
                        tmpBerths.Add(allocatedBerth);
                        VesselPendingList.RemoveAt(0);
                        ReadyToFinishList.Add(allocatedBerth);
                    }
                    else { break; }
                }

                foreach (var berth in tmpBerths)
                {
                    AttemptToFinish(berth);
                }          
            }
        }

        public class BeingOccupied: BaseActivity<Berth>
        {
            public BeingOccupied(bool debugMode = false, int seed = 0) : base(nameof(BeingOccupied), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofBerth;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public override void TryFinish(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj})");
                Vessel? vessel = (obj is Vessel) ? (obj as Vessel) : null;
                if (vessel == null){return;}

                Berth allocatedBerth = CompletedList.Find(item => item.BerthedVessel == vessel);
                if (allocatedBerth != null)
                {
                    allocatedBerth.BerthedVessel = null;//release before being idle;
                    ReadyToFinishList.Add(allocatedBerth);
                    AttemptToFinish(allocatedBerth);                   
                }
                else
                {
                    Console.WriteLine($"Error：{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj}): we don't find the determined berth for releasing");
                    return;
                }
            }
        }
    }
}

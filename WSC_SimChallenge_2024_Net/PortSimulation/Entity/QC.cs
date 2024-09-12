using O2DESNet;
using O2DESNet.Standard;
using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using WSC_SimChallenge_2024_Net.Activity;
using static WSC_SimChallenge_2024_Net.PortSimulation.Entity.QC;

namespace WSC_SimChallenge_2024_Net.PortSimulation.Entity
{
    public class QC
    {
        public string Id;
        public Vessel ServedVessel;
        public Berth LocatedBerth;
        public Container HeldContainer;
        public ControlPoint CP;
        //public int ReservedNum = 0;
        public override string ToString()
        {
            return $"QC[{Id}]";
        }

        public class BeingIdle: BaseActivity<QC>
        {
            public void ReBeingIdleQC(QC qc){qc.ServedVessel = null;}
            public List<Berth> BerthPendingList = new List<Berth>();
            public BeingIdle(bool debugMode = false, int seed = 0) : base(nameof(BeingIdle), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofQC;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public override void AttemptToFinish(QC qc)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  QC{ActivityName}.AttemptToFinish({qc})");
                if (CompletedList.Contains(qc) && NeedExtTryFinish && ReadyToFinishList.Contains(qc))// Message before entity
                {
                    Finish(qc);
                    //Console.WriteLine($"1:{qc},{ClockTime}");
                }
                else if (CompletedList.Contains(qc) && NeedExtTryFinish && !ReadyToFinishList.Contains(qc) &&
                    BerthPendingList.Count > 0)// Entity before messsage, there are vessel waiting
                {
                    List<QC> AllocatedQCs = CompletedList.FindAll(qc => qc.LocatedBerth == BerthPendingList[0]);
                    if (AllocatedQCs != null)
                    {
                        foreach (var q in AllocatedQCs)
                        {
                            BerthPendingList[0].BerthedVessel.AlreadyAllocatedQcCount++;
                            q.ServedVessel = BerthPendingList[0].BerthedVessel;

                            ReadyToFinishList.Add(q);
                            Finish(q);
                            //Console.WriteLine($"2:{qc},{ClockTime},{BerthPendingList[0]},{BerthPendingList[0].BerthedVessel},{BerthPendingList[0]},{BerthPendingList[0].BerthedVessel.AllocatedQCs.Count}");
                        }
                        if (BerthPendingList[0].BerthedVessel.AlreadyAllocatedQcCount == BerthPendingList[0].BerthedVessel.RequiredQcCount)
                            BerthPendingList.RemoveAt(0);
                    }
                }
            }

            public override void TryFinish(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  QC{ActivityName}.TryFinish({obj})");

                Berth ? berth = (obj is Berth) ? (obj as Berth) : null;
                if (berth == null){return;}
                BerthPendingList.Add(berth);

                List<QC> AllocatedQCs = CompletedList.FindAll(qc => qc.LocatedBerth == BerthPendingList[0]);
                //Console.WriteLine(AllocatedQCs.Count);
                if (AllocatedQCs != null)
                {
                    foreach (var qc in AllocatedQCs)
                    {
                        BerthPendingList[0].BerthedVessel.AlreadyAllocatedQcCount++;
                        qc.ServedVessel = BerthPendingList[0].BerthedVessel;
                        ReadyToFinishList.Add(qc);
                        AttemptToFinish(qc);
                    }
                    if(BerthPendingList[0].BerthedVessel.AlreadyAllocatedQcCount == BerthPendingList[0].BerthedVessel.RequiredQcCount)
                        BerthPendingList.RemoveAt(0);
                    //Console.WriteLine($"Finish2:{ClockTime}");
                }
            }
        }

        public class SettingUp : BaseActivity<QC>
        {
            public SettingUp(bool debugMode = false, int seed = 0) : base(nameof(SettingUp), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofQC;
                TimeSpan = TimeSpan.FromSeconds(1500);//state-dependent or customized delay time
            }
        }

        public class RestoringtoDischarge : BaseActivity<QC>
        {
            public List<Container> ContainerPendingList = new List<Container>();
            public RestoringtoDischarge(bool debugMode = false, int seed = 0) : base(nameof(RestoringtoDischarge), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofQC;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.FromSeconds(35);//state-dependent or customized delay time
            }

            public override void AttemptToFinish(QC qc)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.AttemptToFinish({qc})");
                if (CompletedList.Contains(qc) && NeedExtTryFinish && ReadyToFinishList.Contains(qc))// Message before entity
                {
                    Finish(qc);
                }
                else if (CompletedList.Contains(qc) && NeedExtTryFinish && !ReadyToFinishList.Contains(qc) 
                    && ContainerPendingList.Any(container => container.DischargingVesselID == qc.ServedVessel.Id && container.Week == qc.ServedVessel.Week))
                {// Entity before Message
                    Container pickedContainer = ContainerPendingList.Find(container => container.DischargingVesselID == qc.ServedVessel.Id
                    && container.Week == qc.ServedVessel.Week);
                    qc.ServedVessel.DischargingContainersInformation[pickedContainer.LoadingVesselID] -= 1;
                    qc.HeldContainer = pickedContainer;
                    ContainerPendingList.Remove(pickedContainer);
                    ReadyToFinishList.Add(qc);
                    Finish(qc);
                }
            }

            public override void TryFinish(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj})");

                Container? container = (obj is Container) ? (obj as Container) : null;
                if (container == null){return;}
                ContainerPendingList.Add(container);

                QC qc = CompletedList.Find(q => q.ServedVessel.Id == container.DischargingVesselID && container.Week == q.ServedVessel.Week);
                if (qc != null)
                {
                    qc.ServedVessel.DischargingContainersInformation[container.LoadingVesselID] -= 1;
                    qc.HeldContainer = container;
                    ContainerPendingList.Remove(container);
                    ReadyToFinishList.Add(qc);
                    AttemptToFinish(qc);
                }
            }
        }

        public class Discharging : BaseActivity<QC>
        {
            public Discharging(bool debugMode = false, int seed = 0) : base(nameof(Discharging), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofQC;
                TimeSpan = TimeSpan.FromSeconds(75);//state-dependent or customized delay time
            }
        }

        public class HoldingonDischarging : BaseActivity<QC>
        {
            public List<Container> ContainerPendingList = new List<Container>();
            public HoldingonDischarging(bool debugMode = false, int seed = 0) : base(nameof(HoldingonDischarging), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofQC;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public override void AttemptToFinish(QC qc)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.AttemptToFinish({qc})");               
                if (CompletedList.Contains(qc) && NeedExtTryFinish && ReadyToFinishList.Contains(qc))// Message before entity
                {
                    Finish(qc);
                }
                else if (CompletedList.Contains(qc) && NeedExtTryFinish && !ReadyToFinishList.Contains(qc) 
                    && ContainerPendingList.Any(container => container == qc.HeldContainer))
                {// Entity before Message
                    Container pickedContainer = ContainerPendingList.Find(container => container == qc.HeldContainer);
                    qc.HeldContainer = null;
                    ContainerPendingList.Remove(pickedContainer);
                    ReadyToFinishList.Add(qc);
                    Finish(qc);
                }
            }

            public override void TryFinish(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj})");
                Container? container = (obj is Container) ? (obj as Container) : null;
                ContainerPendingList.Add(container);

                QC qc = CompletedList.Find(q => container == q.HeldContainer);
                if (qc != null)
                {
                    qc.HeldContainer = null;
                    ContainerPendingList.Remove(container);
                    ReadyToFinishList.Add(qc);
                    AttemptToFinish(qc);
                }
            }
        }

        public class RestoringtoLoad : BaseActivity<QC>
        {
            public List<Container> ContainerPendingList = new List<Container>();
            public RestoringtoLoad(bool debugMode = false, int seed = 0) : base(nameof(RestoringtoLoad), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofQC;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.FromSeconds(75);//state-dependent or customized delay time
            }

            public override void AttemptToFinish(QC qc)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.AttemptToFinish({qc})");
                if (CompletedList.Contains(qc) && NeedExtTryFinish && ReadyToFinishList.Contains(qc))// Message before entity
                {
                    Finish(qc);
                }
                else if (CompletedList.Contains(qc) && NeedExtTryFinish && !ReadyToFinishList.Contains(qc) &&
                    ContainerPendingList.Any(container => container.TargetQC.LocatedBerth == qc.LocatedBerth))
                {// Entity before messsage
                    Container pickedContainer = ContainerPendingList.Find(container => container.TargetQC.LocatedBerth == qc.LocatedBerth);
                    qc.ServedVessel.LoadingContainersInformation[pickedContainer.LoadingVesselID] -= 1;
                    qc.HeldContainer = pickedContainer;
                    //qc.ReservedNum--;
                    ContainerPendingList.Remove(pickedContainer);
                    ReadyToFinishList.Add(qc);

                    Finish(qc);
                }
            }

            public override void TryFinish(Object obj)
            {
                Container? container = (obj is Container) ? (obj as Container) : null;
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({container})");
                ContainerPendingList.Add(container);

                QC qc = CompletedList.Find(q => container.TargetQC.LocatedBerth == q.LocatedBerth);
                if (qc != null)
                {
                    qc.ServedVessel.LoadingContainersInformation[container.LoadingVesselID] -= 1;
                    qc.HeldContainer = container;
                    //qc.ReservedNum--;
                    ContainerPendingList.Remove(container);
                    ReadyToFinishList.Add(qc);
                    AttemptToFinish(qc);
                }
            }
        }

        public class Loading : BaseActivity<QC>
        {

            public Loading(bool debugMode = false, int seed = 0) : base(nameof(Loading), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofQC;
                TimeSpan = TimeSpan.FromSeconds(35);//state-dependent or customized delay time
            }
        }

        public class HoldingonLoading : BaseActivity<QC>
        {
            List<Container> ContainerPendingList = new List<Container>();
            public HoldingonLoading (bool debugMode = false, int seed = 0) : base(nameof(HoldingonLoading), debugMode, seed)
            {
                _debugMode = PortSimModel.DebugofQC;
                NeedExtTryFinish = true;
                TimeSpan = TimeSpan.Zero;
            }

            public override void AttemptToFinish(QC qc)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.AttemptToFinish({qc})");
                if (CompletedList.Contains(qc) && NeedExtTryFinish && ReadyToFinishList.Contains(qc))// Message before entity
                {
                    Finish(qc);
                }
                else if (CompletedList.Contains(qc) && NeedExtTryFinish && !ReadyToFinishList.Contains(qc) 
                    && ContainerPendingList.Any(container => container == qc.HeldContainer))// Entity before messsage
                {
                    Container pickedContainer = ContainerPendingList.Find(container => container == qc.HeldContainer);
                    qc.HeldContainer = null;
                    ContainerPendingList.Remove(pickedContainer);
                    ReadyToFinishList.Add(qc);
                    Finish(qc);
                }
            }

            public override void TryFinish(Object obj)
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj})");
                Container? container = (obj is Container) ? (obj as Container) : null;

                ContainerPendingList.Add(container);
                QC qc = CompletedList.Find(q => q.HeldContainer == container);

                if (qc != null) 
                {
                    qc.HeldContainer = null;
                    ContainerPendingList.Remove(container);
                    ReadyToFinishList.Add(qc);

                    AttemptToFinish(qc);
                }
            }
        }
    }
}

//#define DebugofBerth
//#define DebugofVessel
//#define DebugofQC
//#define DebugofQCLine
//#define DebugofContainer
//#define DebugofAGV
//#define DebugofYC
#define Debugofagvewatingyd
using O2DESNet;
using System.Globalization;
using System.Reflection;
using WSC_SimChallenge_2024_Net.Activity;
using WSC_SimChallenge_2024_Net.PortSimulation.Entity;
using static WSC_SimChallenge_2024_Net.PortSimulation.Entity.QC;
using static WSC_SimChallenge_2024_Net.PortSimulation.Entity.YC;


namespace WSC_SimChallenge_2024_Net.PortSimulation
{
    class PortSimModel : Sandbox
    {
        public static int Discharging, Loading = 0;
        public static bool DebugofBerth, DebugofVessel, DebugofQCLine, DebugofQC, DebugofContainer, DebugofAGV, DebugofYC, Debugofagvewatingyd = false;
        public static bool IfDetermineBerth = false;
        public static bool IfDetermineAGVs = false;
        public static bool IfDetermineYardBlock = false;

        public int NumberofBerths = 4;
        public int NumberofQCsPerBerth = 3;
        public int NumberofAGVs;
        public int NumberofYCs;
        public int NumberofInitialAGVsPerQC { get { return NumberofAGVs/(NumberofBerths* NumberofQCsPerBerth); } }
        public int NumberofQCs { get { return NumberofBerths * NumberofQCsPerBerth; } }
        public List<Vessel> Vessels = new List<Vessel> ();
        public List<Berth> Berths = new List<Berth>();
        public List<QC> QCs = new List<QC>();
        public List<YC> YCs = new List<YC>();
        public List<AGV> AGVs = new List<AGV>();
        public List<YardBlock> YardBlocks = new List<YardBlock>();
        public DateTime StartTime;
        public static int BlockCapacity = 1000;
        public static int RunningWeeks = 10;
        public static int WarmUpweeks = 1;

        #region Activities of Berth
        public Berth.BeingIdle berthBeingIdle;
        public Berth.BeingOccupied berthBeingOccupied;
        #endregion

        #region Activities of Vessel
        public Vessel.Waiting vesselWaiting;
        public Vessel.Berthing vesselBerthing;
        #endregion

        #region Activities of QCLine
        public QCLine.Discharging qcLineDischarging;
        public QCLine.Loading qcLineLoading;
        #endregion

        #region Activities of Container
        public Container.BeingDischarged containerBeingDischarged;
        public Container.TransportingToYard containerTransportingToYard;
        public Container.BeingStacked containerBeingStacked;
        public Container.Dwelling containerDwelling;
        public Container.BeingUnstacked containerBeingUnstacked;
        public Container.TransportingToQuaySide containerTransportingToQuaySide;
        public Container.BeingLoaded containerBeingLoaded;
        #endregion

        #region Activities of QC
        public QC.BeingIdle qcBeingIdle;
        public QC.SettingUp qcSettingUp;
        public QC.RestoringtoDischarge qcRestoringtoDischarge;
        public QC.Discharging qcDischarging;
        public QC.HoldingonDischarging qcHoldingonDischarging;
        public QC.RestoringtoLoad qcRestoringtoLoad;
        public QC.Loading qcLoading;
        public QC.HoldingonLoading qcHoldingonLoading;
        #endregion

        #region Activities of AGV
        public AGV.BeingIdle agvBeingIdle;
        public AGV.Picking agvPicking;
        public AGV.DeliveringtoYard agvDeliveringtoYard;
        public AGV.HoldingatYard agvHoldingatYard;
        public AGV.DeliveringtoQuaySide agvDeliveringtoQuaySide;
        public AGV.HoldingatQuaySide agvHoldingatQuaySide;
        #endregion

        #region Activities of YC
        public YC.Repositioning ycRepositioning;
        public YC.Picking ycPicking;
        public YC.Stacking ycStacking;
        public YC.Unstacking ycunstacking;
        public YC.HoldingonUnstacking ycHoldingonUnstacking;
        #endregion
        public void QuaySideGenerator(int numberofBerths)
        {
            Dictionary<string, Tuple<double, double>> QCcontrolPoints = FileReader.ReadControlpointsInfo("conf//QC_controlpoint.csv");

            for (int id = 0; id < numberofBerths; id++)
            {
                List<QC> qcs = new List<QC>();
                for (int q = 0; q < NumberofQCsPerBerth; q++)
                {
                    ControlPoint cp = new ControlPoint
                    {
                        Id = "QC_CP" + (id * NumberofQCsPerBerth + q).ToString(),
                        Xcoordinate = QCcontrolPoints[(id * NumberofQCsPerBerth + q).ToString()].Item1,
                        Ycoordinate = QCcontrolPoints[(id * NumberofQCsPerBerth + q).ToString()].Item2
                    };
                    List<AGV> agvs = new List<AGV>();
                    for (int a = 0; a < NumberofInitialAGVsPerQC; a++)
                    {
                        AGV agv = new AGV 
                        {
                            Id = "AGV"+ (id * NumberofQCsPerBerth * NumberofInitialAGVsPerQC + q* NumberofInitialAGVsPerQC + a).ToString(),
                            CurrentLocation = cp
                        };                        
                        AGVs.Add(agv);
                        agvBeingIdle.RequestToStart(agv);
                    }

                    QC qc = new QC {Id =$"qc{id* NumberofQCsPerBerth+q}", CP = cp};
                    QCs.Add(qc);
                    qcs.Add(qc);
                    qcBeingIdle.RequestToStart(qc);
                }

                Berth berth = new Berth { Id = "berth" + id.ToString(), EquippedQCs = qcs};
                foreach (QC qc in berth.EquippedQCs) qc.LocatedBerth = berth;
                Berths.Add(berth);
                berthBeingIdle.RequestToStart(berth);
            }               
        }

        public void VesselGenerator()
        {            
            var containersInfo = FileReader.ReadContainersInfo("conf//transhipment_round1.csv");
            var vesselArrivalTime = FileReader.ReadVesselArrivalTimes("conf//vessel_arrival_time_round1.csv");
            int numberofVeesel = containersInfo.Count;

            for (int orig = 0; orig < numberofVeesel; orig++)
            {
                Dictionary<string, int> dischargingContainersInformation = containersInfo["vessel " + orig.ToString()];
                Dictionary<string, int> loadingContainersInformation = new Dictionary<string, int>();
                for (int dest = 0; dest < numberofVeesel; dest++)
                {
                    loadingContainersInformation["vessel " + dest.ToString()] = containersInfo["vessel " + dest.ToString()]
                        ["vessel " + orig.ToString()];
                }

                for (int week = 0; week < RunningWeeks; week++)
                {
                    int _week = week;
                    string _orig = orig.ToString();

                    Vessel vessel = new Vessel
                    {
                        Id = "vessel " + _orig,
                        DischargingContainersInformation = new Dictionary<string, int>(dischargingContainersInformation),
                        LoadingContainersInformation = _week < WarmUpweeks ? null : new Dictionary<string, int>(loadingContainersInformation),
                        //ArrivalTime = vesselArrivalTime["vessel " + _orig] + TimeSpan.FromDays(7 * _week),
                        Week = _week
                    };
                    Vessels.Add(vessel);
                    Schedule(() => vesselWaiting.RequestToStart(vessel), vesselArrivalTime["vessel " + _orig] + TimeSpan.FromDays(7* _week));
                }
            }
        }

        public void YardGenerator()
        {
            Dictionary<string, Tuple<double, double>> YCcontrolPoints = FileReader.ReadControlpointsInfo("conf//YC_controlpoint.csv");
            int numberofYCs = YCcontrolPoints.Count;
            NumberofYCs = numberofYCs;
            for (int i = 0; i < numberofYCs; i++)
            {
                ControlPoint controlPoint = new ControlPoint { Id = "YC_CP"+i.ToString(), 
                    Xcoordinate = YCcontrolPoints[i.ToString()].Item1,
                    Ycoordinate = YCcontrolPoints[i.ToString()].Item2
                };
                YardBlock yardBlock = new YardBlock { Id = "Block" + i.ToString(), 
                    Capacity = BlockCapacity, CP = controlPoint
                };
                YardBlocks.Add(yardBlock);
                YC yC = new YC { Id = "YC" + i.ToString(), ServedBlock = yardBlock, CP = controlPoint};
                yardBlock.EquipedYC = yC;
                ycRepositioning.RequestToStart(yC);
                YCs.Add(yC);
            }
        }

        public void Initialize()
        {
            QuaySideGenerator(NumberofBerths);
            VesselGenerator();
            YardGenerator();
            WarmUp(StartTime);
        }

        public PortSimModel() : base()
        {
#if DebugofBerth
            DebugofBerth = true;
#endif

#if DebugofVessel
            DebugofVessel = true;
#endif

#if DebugofQCLine
            DebugofQCLine = true;
#endif

#if DebugofQC
            DebugofQC = true;
#endif

#if DebugofContainer
            DebugofContainer = true;
#endif

#if DebugofAGV
            DebugofAGV = true;
#endif

#if DebugofYC
            DebugofYC = true;
#endif
#if Debugofagvewatingyd
            Debugofagvewatingyd = true;
#endif
            #region Activities of Berth
            berthBeingIdle = AddChild(new Berth.BeingIdle());
            berthBeingOccupied = AddChild(new Berth.BeingOccupied());

            berthBeingIdle.FlowTo(berthBeingOccupied).FlowTo(berthBeingIdle);
            #endregion

            #region Activities of Vessel
            vesselWaiting = AddChild(new Vessel.Waiting());
            vesselBerthing = AddChild(new Vessel.Berthing());

            vesselWaiting.FlowTo(vesselBerthing).Terminate();
            #endregion

            #region Activities of QCLine
            qcLineDischarging = AddChild(new QCLine.Discharging());
            qcLineLoading = AddChild(new QCLine.Loading());

            qcLineDischarging.FlowTo(qcLineLoading).Terminate();
            #endregion

            #region Activities of Container
            containerBeingDischarged = AddChild(new Container.BeingDischarged());
            containerTransportingToYard = AddChild(new Container.TransportingToYard());
            containerBeingStacked = AddChild(new Container.BeingStacked());
            containerDwelling = AddChild(new Container.Dwelling());
            containerBeingUnstacked = AddChild(new Container.BeingUnstacked());
            containerTransportingToQuaySide = AddChild(new Container.TransportingToQuaySide());
            containerBeingLoaded = AddChild(new Container.BeingLoaded());

            containerBeingDischarged.FlowTo(containerTransportingToYard).FlowTo(containerBeingStacked)
                .FlowTo(containerDwelling).FlowTo(containerBeingUnstacked).FlowTo(containerTransportingToQuaySide)
                .FlowTo(containerBeingLoaded).Terminate();
            #endregion

            #region Activities of QC
            qcBeingIdle = AddChild(new BeingIdle());
            qcSettingUp = AddChild(new SettingUp());
            qcRestoringtoDischarge = AddChild(new RestoringtoDischarge());
            qcDischarging = AddChild(new Discharging());
            qcHoldingonDischarging = AddChild(new HoldingonDischarging());
            qcRestoringtoLoad = AddChild(new RestoringtoLoad());
            qcLoading = AddChild(new Loading());
            qcHoldingonLoading = AddChild(new HoldingonLoading());

            qcBeingIdle.FlowTo(qcSettingUp).FlowTo(qcRestoringtoDischarge).FlowTo(qcDischarging).FlowTo(qcHoldingonDischarging);
            qcHoldingonDischarging.FlowToBranch(qcRestoringtoDischarge, (qc) => !QCTerminateDischarging(qc, qcRestoringtoDischarge));
            qcHoldingonDischarging.FlowToBranch(qcRestoringtoLoad, (qc) => (_QCTerminateDischarging & QCToLaoding(qc, qcRestoringtoLoad)));
            qcHoldingonDischarging.FlowToBranch(qcBeingIdle, (qc) => (_QCTerminateDischarging & !_QCToLaoding));
            qcRestoringtoLoad.FlowTo(qcLoading).FlowTo(qcHoldingonLoading);
            qcHoldingonLoading.FlowToBranch(qcRestoringtoLoad, (qc) => !QCTerminateLoading(qc, qcRestoringtoLoad));
            qcHoldingonLoading.FlowToBranch(qcBeingIdle, (qc) => _QCTerminateLoading);
            #endregion

            #region Activities of AGV
            agvBeingIdle = AddChild(new AGV.BeingIdle());
            agvPicking = AddChild(new AGV.Picking());
            agvDeliveringtoYard = AddChild(new AGV.DeliveringtoYard());
            agvHoldingatYard = AddChild(new AGV.HoldingatYard());
            agvDeliveringtoQuaySide = AddChild(new AGV.DeliveringtoQuaySide());
            agvHoldingatQuaySide = AddChild(new AGV.HoldingatQuaySide());

            agvBeingIdle.FlowTo(agvPicking).FlowToBranch(agvDeliveringtoYard, agv => agv.InDischarging);
            agvDeliveringtoYard.FlowTo(agvHoldingatYard).FlowTo(agvBeingIdle);
            agvPicking.FlowToBranch(agvDeliveringtoQuaySide, agv => !agv.InDischarging).FlowTo(agvHoldingatQuaySide).FlowTo(agvBeingIdle);

            #endregion Activities of AGV

            #region Activities YC
            ycRepositioning = AddChild(new YC.Repositioning());
            ycPicking = AddChild(new Picking());
            ycStacking = AddChild(new Stacking());
            ycunstacking = AddChild(new Unstacking());
            ycHoldingonUnstacking = AddChild(new HoldingonUnstacking());

            ycRepositioning.FlowTo(ycPicking).FlowToBranch(ycStacking, yc=> YCinDischarging(yc)).FlowTo(ycRepositioning);
            ycPicking.FlowToBranch(ycunstacking, yc => !_YCinDischarging).FlowTo(ycHoldingonUnstacking).FlowTo(ycRepositioning);
            #endregion

            #region Connect message conditions
            berthBeingIdle.OnReadyToDepart += qcBeingIdle.TryFinish;
            berthBeingOccupied.OnStart += vesselBerthing.TryStart;

            vesselWaiting.OnReadyToDepart += berthBeingIdle.TryFinish;
            //vesselWaiting.OnReadyToDepart += qcBeingIdle.TryFinish;
            vesselBerthing.OnStart += qcLineDischarging.CreateQCLine;
            vesselBerthing.OnReadyToDepart += berthBeingOccupied.TryFinish;

            qcLineDischarging.OnStart += containerBeingDischarged.GenerateContainers;
            qcLineLoading.OnStart += containerDwelling.TryFinish;
            qcLineLoading.OnReadyToDepart += vesselBerthing.TryFinish;

            containerBeingDischarged.OnRequestToStart += qcRestoringtoDischarge.TryFinish;
            containerBeingDischarged.OnReadyToDepart += agvBeingIdle.TryFinish;
            containerTransportingToYard.OnStart += qcHoldingonDischarging.TryFinish;
            containerTransportingToYard.OnReadyToDepart += ycRepositioning.TryFinish;
            containerBeingStacked.OnStart += agvHoldingatYard.TryFinish;
            containerDwelling.OnStart += qcLineDischarging.TryFinish;
            containerDwelling.OnReadyToDepart += agvBeingIdle.TryFinish;
            containerTransportingToQuaySide.OnStart += ycHoldingonUnstacking.TryFinish;
            containerTransportingToQuaySide.OnReadyToDepart += qcRestoringtoLoad.TryFinish;
            containerBeingLoaded.OnStart += agvHoldingatQuaySide.TryFinish;
            containerBeingLoaded.OnReadyToDepart += qcLineLoading.TryFinish;
            containerBeingLoaded.OnReadyToDepart += qcHoldingonLoading.TryFinish;

            qcBeingIdle.OnStart += qcBeingIdle.ReBeingIdleQC;
            qcSettingUp.OnStart += vesselBerthing.TryStart;
            qcDischarging.OnStart += containerBeingDischarged.TryStart;
            qcDischarging.OnReadyToDepart += containerBeingDischarged.TryFinish;
            qcLoading.OnStart += containerBeingLoaded.TryStart;
            qcLoading.OnReadyToDepart += containerBeingLoaded.TryFinish;

            agvPicking.OnStart += ycRepositioning.TryFinish;
            agvDeliveringtoYard.OnStart += containerTransportingToYard.TryStart;
            agvDeliveringtoYard.OnReadyToDepart += containerTransportingToYard.TryFinish;
            agvDeliveringtoQuaySide.OnStart += containerTransportingToQuaySide.TryStart;
            agvDeliveringtoQuaySide.OnReadyToDepart += containerTransportingToQuaySide.TryFinish;

            ycRepositioning.OnStart += ycRepositioning.ReBeingIdleYC;
            ycStacking.OnStart += containerBeingStacked.TryStart;
            ycStacking.OnReadyToDepart += containerBeingStacked.TryFinish;
            ycunstacking.OnStart += containerBeingUnstacked.TryStart;
            ycunstacking.OnReadyToDepart += containerBeingUnstacked.TryFinish;
            ycunstacking.OnReadyToDepart += agvDeliveringtoQuaySide.TryStart;
            #endregion
        }

        public bool _QCTerminateDischarging;
        public bool QCTerminateDischarging(QC qc, QC.RestoringtoDischarge restoringtoDischarge)
        {
            bool _qcTerminateDischarging = false;
            int numofRestContainers = 0;
            if (qc.ServedVessel != null)
            {
                foreach (var info in qc.ServedVessel.DischargingContainersInformation)
                {
                    if (info.Value != 0)
                    {
                        //Console.WriteLine($"{info.Key},{info.Value}");
                        numofRestContainers += info.Value;
                    }
                }

                if (restoringtoDischarge.ProcessingList.FindAll(q => q.ServedVessel.Id == qc.ServedVessel.Id).Count+
                    restoringtoDischarge.PendingList.FindAll(q => q.ServedVessel.Id == qc.ServedVessel.Id).Count == numofRestContainers)//we have enough QCs in discharging;
                    _qcTerminateDischarging = true; 
            }
            _QCTerminateDischarging = _qcTerminateDischarging;

            return _qcTerminateDischarging;
        }

        public bool _QCToLaoding = false;
        public bool QCToLaoding(QC qc, QC.RestoringtoLoad restoringtoLoad)
        {
            bool qcToLaoding = false;
            int numofRestContainers = 0;
            if (qc.ServedVessel.LoadingContainersInformation != null)
            {
                qcToLaoding = true;
                foreach (var info in qc.ServedVessel.LoadingContainersInformation)
                {
                    if (info.Value != 0)
                    {
                        //Console.WriteLine($"{info.Key},{info.Value}");
                        numofRestContainers += info.Value;
                    }
                }
                if(restoringtoLoad.ContainerPendingList.FindAll(container => container.LoadingVesselID == qc.ServedVessel.Id && container.Week +1 == qc.ServedVessel.Week).Count > 0 &&
                    numofRestContainers == restoringtoLoad.PendingList.Count + restoringtoLoad.ProcessingList.FindAll(q => q.ServedVessel.Id == qc.ServedVessel.Id).Count+
                    restoringtoLoad.CompletedList.FindAll(q => q.ServedVessel.Id == qc.ServedVessel.Id).Count)
/*                if (restoringtoLoad.ContainerPendingList.Count > 0 && numofRestContainers == restoringtoLoad.PendingList.Count + restoringtoLoad.ProcessingList.Count + restoringtoLoad.CompletedList.Count)*///we have enough QCs in loading;
                    qcToLaoding = false;
            }
            _QCToLaoding = qcToLaoding;
            return qcToLaoding;
        }

        public bool _QCTerminateLoading = false;
        public bool QCTerminateLoading(QC qc, QC.RestoringtoLoad restoringtoLoad)
        {
            bool _qcTerminateLoading = false;
            int numofRestContainers = 0;

            if (qc.ServedVessel != null)
            {
                foreach (var info in qc.ServedVessel.LoadingContainersInformation)
                {
                    if (info.Value != 0)
                    {
                        //Console.WriteLine($"{info.Key},{info.Value}");
                        numofRestContainers += info.Value;
                    }
                }
                if (restoringtoLoad.PendingList.FindAll(q => q.ServedVessel == qc.ServedVessel).Count+
                    restoringtoLoad.ProcessingList.FindAll(q => q.ServedVessel == qc.ServedVessel).Count+
                    restoringtoLoad.CompletedList.FindAll(q => q.ServedVessel == qc.ServedVessel).Count == numofRestContainers)
                    _qcTerminateLoading = true;

                //Console.WriteLine($"{_qcTerminateLoading},{restoringtoLoad.PendingList.FindAll(q => q.ServedVessel == qc.ServedVessel).Count}," +
                //    $"{restoringtoLoad.ProcessingList.FindAll(q => q.ServedVessel == qc.ServedVessel).Count}," +
                //    $"{restoringtoLoad.CompletedList.FindAll(q => q.ServedVessel == qc.ServedVessel).Count}," +
                //    $"{numofRestContainers}," +
                //    $"{restoringtoLoad.ContainerPendingList.FindAll(container => container.TargetQC.LocatedBerth == qc.LocatedBerth).Count}");
            }

    //        Console.WriteLine($"{qc.Id}, {_qcTerminateLoading}, ReservedNum: {qc.ReservedNum}, RestContainers: {restoringtoLoad.ContainerPendingList.FindAll(container => container.TargetQC == qc).Count}," +
    //$"Processing:{qcRestoringtoLoad.ProcessingList.FindAll(q => q.ServedVessel.Id == qc.ServedVessel.Id).Count}," +
    //$"Completed:{qcRestoringtoLoad.CompletedList.FindAll(q => q.ServedVessel.Id == qc.ServedVessel.Id).Count}," +
    //$"RestInform:{numofRestContainers}");
            _QCTerminateLoading = _qcTerminateLoading;
            return _qcTerminateLoading;
        }

        public bool _YCinDischarging;
        public bool YCinDischarging(YC yc)
        {
            _YCinDischarging = yc.HeldContainer.InDischarging;
            return _YCinDischarging;
        }
    }
}

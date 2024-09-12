using WSC_SimChallenge_2024_Net.PortSimulation;
using System.ComponentModel;
using WSC_SimChallenge_2024_Net.PortSimulation.Entity;
using System;
using static WSC_SimChallenge_2024_Net.PortSimulation.Entity.QC;
using static WSC_SimChallenge_2024_Net.PortSimulation.Entity.YC;
using WSC_SimChallenge_2024_Net.StrategyMaking;

namespace WSC_SimChallenge_2024_Net.PortSimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Simulation running...");
            PortSimModel WSCPort = new PortSimModel()
            {
                NumberofAGVs = 12 * 2,//Times of groups of vessels
                StartTime = new DateTime(2024, 5, 4, 0, 0, 0)
            };
            WSCPort.Initialize();
            DecisionMaker WSCPortDecisionMaker = new DecisionMaker();
            DecisionMaker.WSCPort = WSCPort;
            Default WSCPortDefaulter = new Default(WSCPort);
            WSCPort.Run(TimeSpan.FromDays(7 * PortSimModel.RunningWeeks));

            if (PortSimModel.DebugofBerth)
            {
                Console.WriteLine($"berthBeingIdle.CompletedList:{WSCPort.berthBeingIdle.CompletedList.Count}");
                Console.WriteLine($"berthBeingOccupied.CompletedList:{WSCPort.berthBeingOccupied.CompletedList.Count}");
            }


            if (PortSimModel.DebugofVessel)
            {
                Console.WriteLine($"vesselWaiting.CompletedList:{WSCPort.vesselWaiting.CompletedList.Count}");
                Console.WriteLine($"vesselBerthing.PendingList:{WSCPort.vesselBerthing.PendingList.Count}");
                Console.WriteLine($"vesselBerthing.CompletedList:{WSCPort.vesselBerthing.CompletedList.Count}");
            }

            if (PortSimModel.DebugofQCLine)
            {
                Console.WriteLine($"qcLineDischarging.PendingList:{WSCPort.qcLineDischarging.PendingList.Count}");
                Console.WriteLine($"qcLineDischarging.CompletedList:{WSCPort.qcLineDischarging.CompletedList.Count}");
                Console.WriteLine($"qcLineLoading.PendingList:{WSCPort.qcLineLoading.PendingList.Count}");
                Console.WriteLine($"qcLineLoading.CompletedList:{WSCPort.qcLineLoading.CompletedList.Count}");
            }

            if (PortSimModel.DebugofContainer)
            {
                Console.WriteLine($"containerBeingDischarged.PendingList:{WSCPort.containerBeingDischarged.PendingList.Count}");
                Console.WriteLine($"containerBeingDischarged.CompletedList:{WSCPort.containerBeingDischarged.CompletedList.Count}");
                Console.WriteLine($"containerTransportingToYard.PendingList:{WSCPort.containerTransportingToYard.PendingList.Count}");
                Console.WriteLine($"containerTransportingToYard.CompletedList:{WSCPort.containerTransportingToYard.CompletedList.Count}");
                Console.WriteLine($"containerBeingStacked.PendingList:{WSCPort.containerBeingStacked.PendingList.Count}");
                Console.WriteLine($"containerBeingStacked.CompletedList:{WSCPort.containerBeingStacked.CompletedList.Count}");
                Console.WriteLine($"containerDwelling.CompletedList:{WSCPort.containerDwelling.CompletedList.Count}");
                Console.WriteLine($"containerBeingUnstacke.PendingList:{WSCPort.containerBeingUnstacked.PendingList.Count}");
                Console.WriteLine($"containerBeingUnstacke.CompletedList:{WSCPort.containerBeingUnstacked.CompletedList.Count}");
                Console.WriteLine($"containerTransportingToQuaySide.PendingList:{WSCPort.containerTransportingToQuaySide.PendingList.Count}");
                Console.WriteLine($"containerTransportingToQuaySide.ProcessingList:{WSCPort.containerTransportingToQuaySide.ProcessingList.Count}");
                Console.WriteLine($"containerTransportingToQuaySide.CompletedList:{WSCPort.containerTransportingToQuaySide.CompletedList.Count}");
                Console.WriteLine($"containerBeingLoaded.PendingList:{WSCPort.containerBeingLoaded.PendingList.Count}");
                Console.WriteLine($"containerBeingLoaded.CompletedList:{WSCPort.containerBeingLoaded.CompletedList.Count}");
            }

            if (PortSimModel.DebugofQC)
            {
                Console.WriteLine($"qcBeingIdle.CompletedList:{WSCPort.qcBeingIdle.CompletedList.Count}");
                Console.WriteLine($"qcSettingUp.CompletedList:{WSCPort.qcSettingUp.CompletedList.Count}");
                Console.WriteLine($"qcRestoringtoDischarge.CompletedList:{WSCPort.qcRestoringtoDischarge.CompletedList.Count}");
                Console.WriteLine($"qcRestoringtoDischarge.ContainerPendingList:{WSCPort.qcRestoringtoDischarge.ContainerPendingList.Count}");
                Console.WriteLine($"qcDischarging.CompletedList:{WSCPort.qcDischarging.CompletedList.Count}");
                Console.WriteLine($"qcHoldingonDischarging.CompletedList:{WSCPort.qcHoldingonDischarging.CompletedList.Count}");
                Console.WriteLine($"qcHoldingonDischarging.ContainerPendingList:{WSCPort.qcHoldingonDischarging.ContainerPendingList.Count}");
                Console.WriteLine($"qcRestoringtoLoad.CompletedList:{WSCPort.qcRestoringtoLoad.CompletedList.Count}");
                Console.WriteLine($"qcLoading.CompletedList:{WSCPort.qcLoading.CompletedList.Count}");
                Console.WriteLine($"qcHoldingonLoading.CompletedList:{WSCPort.qcHoldingonLoading.CompletedList.Count}");
                Console.WriteLine($"qcRestoringtoLoad.ContainerPendingList:{WSCPort.qcRestoringtoLoad.ContainerPendingList.Count}");
            }

            if (PortSimModel.DebugofAGV)
            {
                Console.WriteLine($"agvBeingIdle.PendingList:{WSCPort.agvBeingIdle.PendingList.Count}");
                Console.WriteLine($"agvBeingIdle.ProcessingList:{WSCPort.agvBeingIdle.ProcessingList.Count}");
                Console.WriteLine($"agvBeingIdle.CompletedList:{WSCPort.agvBeingIdle.CompletedList.Count}");
                Console.WriteLine($"agvBeingIdle.ContainersPending:{WSCPort.agvBeingIdle.ContainersPending.Count}");
                Console.WriteLine($"agvPicking.PendingList:{WSCPort.agvPicking.PendingList.Count}");
                Console.WriteLine($"agvPicking.ProcessingList:{WSCPort.agvPicking.ProcessingList.Count}");
                Console.WriteLine($"agvPicking.CompletedList:{WSCPort.agvPicking.CompletedList.Count}");
                Console.WriteLine($"agvDeliveringtoYard.PendingLis:{WSCPort.agvDeliveringtoYard.PendingList.Count}");
                Console.WriteLine($"agvDeliveringtoYard.ProcessingLis:{WSCPort.agvDeliveringtoYard.ProcessingList.Count}");
                Console.WriteLine($"agvDeliveringtoYard.CompletedList:{WSCPort.agvDeliveringtoYard.CompletedList.Count}");
                Console.WriteLine($"agvHoldingatYard.PendingLis:{WSCPort.agvHoldingatYard.PendingList.Count}");
                Console.WriteLine($"agvHoldingatYard.ProcessingList:{WSCPort.agvHoldingatYard.ProcessingList.Count}");
                Console.WriteLine($"agvHoldingatYard.CompletedList:{WSCPort.agvHoldingatYard.CompletedList.Count}");
                Console.WriteLine($"agvHoldingatYard.ContainersPending:{WSCPort.agvHoldingatYard.ContainersPending.Count}");
                Console.WriteLine($"agvDeliveringtoQuaySide.PendingList:{WSCPort.agvDeliveringtoQuaySide.PendingList.Count}");
                Console.WriteLine($"agvDeliveringtoQuaySide.ProcessingList:{WSCPort.agvDeliveringtoQuaySide.ProcessingList.Count}");
                Console.WriteLine($"agvDeliveringtoQuaySide.CompletedList:{WSCPort.agvDeliveringtoQuaySide.CompletedList.Count}");
                Console.WriteLine($"agvHoldingatQuaySide.PendingList:{WSCPort.agvHoldingatQuaySide.PendingList.Count}");
                Console.WriteLine($"agvHoldingatQuaySide.ProcessingLis:{WSCPort.agvHoldingatQuaySide.ProcessingList.Count}");
                Console.WriteLine($"agvHoldingatQuaySide.CompletedList:{WSCPort.agvHoldingatQuaySide.CompletedList.Count}");
            }

            if (PortSimModel.DebugofYC)
            {
                Console.WriteLine($"ycRepositioning.PendingList:{WSCPort.ycRepositioning.PendingList.Count}");
                Console.WriteLine($"ycRepositioning.ProcessingList:{WSCPort.ycRepositioning.ProcessingList.Count}");
                Console.WriteLine($"ycRepositioning.CompletedList:{WSCPort.ycRepositioning.CompletedList.Count}");
                Console.WriteLine($"ycRepositioning.ConatinerPendingList:{WSCPort.ycRepositioning.ConatinerPendingList.Count}");
                Console.WriteLine($"ycPicking.PendingList:{WSCPort.ycPicking.PendingList.Count}");
                Console.WriteLine($"ycPicking.ProcessingList:{WSCPort.ycPicking.ProcessingList.Count}");
                Console.WriteLine($"ycPicking.CompletedList:{WSCPort.ycPicking.CompletedList.Count}");
                Console.WriteLine($"ycStacking.PendingList:{WSCPort.ycStacking.PendingList.Count}");
                Console.WriteLine($"ycStacking.ProcessingList:{WSCPort.ycStacking.ProcessingList.Count}");
                Console.WriteLine($"ycStacking.CompletedList:{WSCPort.ycStacking.CompletedList.Count}");
                Console.WriteLine($"ycUnstacking.PendingList:{WSCPort.ycunstacking.PendingList.Count}");
                Console.WriteLine($"ycUnstacking.ProcessingList:{WSCPort.ycunstacking.ProcessingList.Count}");
                Console.WriteLine($"ycUnstacking.CompletedList:{WSCPort.ycunstacking.CompletedList.Count}");
                Console.WriteLine($"ycHoldingonUnstacking.PendingList:{WSCPort.ycHoldingonUnstacking.PendingList.Count}");
                Console.WriteLine($"ycHoldingonUnstacking.ProcessingList:{WSCPort.ycHoldingonUnstacking.ProcessingList.Count}");
                Console.WriteLine($"ycHoldingonUnstacking.CompletedList:{WSCPort.ycHoldingonUnstacking.CompletedList.Count}");
            }

            for (int i = 0; i < WSCPort.Vessels.Count;i++) 
            {
                //Console.WriteLine($"{WSCPort.Vessels[i].Id},{WSCPort.Vessels[i].ArrivalTime},{WSCPort.Vessels[i].StartBerthingTime}，{WSCPort.ClockTime}");
                if (WSCPort.Vessels[i].ArrivalTime != DateTime.MinValue && WSCPort.Vessels[i].StartBerthingTime == DateTime.MinValue)
                    WSCPort.Vessels[i].StartBerthingTime = WSCPort.ClockTime;
            }
            int numofDelayedVessels = WSCPort.Vessels.FindAll(vessel => vessel.ArrivalTime != DateTime.MinValue && vessel.StartBerthingTime != DateTime.MinValue &&
            vessel.StartBerthingTime - vessel.ArrivalTime > TimeSpan.FromHours(2)).Count;
            int numofArrivalVessles = WSCPort.Vessels.FindAll(vessel => vessel.ArrivalTime != DateTime.MinValue).Count;

            Console.WriteLine($"Number of delayed vessels: {numofDelayedVessels}; Number of arrival vessels: {numofArrivalVessles}");
            Console.WriteLine($"Rate of delayed vessels: {((double)numofDelayedVessels / (double)numofArrivalVessles * 100).ToString("0.00")} %");
            Console.WriteLine($"Simulation completed");
            Console.WriteLine(String.Concat(Enumerable.Repeat("*", 70)));

            Console.WriteLine("Debug Checking:");
            WSCPort.Run(TimeSpan.FromDays(300));//release containers by running additional time without discharging;  
            Console.WriteLine($"Discharging condition:{PortSimModel.Discharging == WSCPort.containerDwelling.CompletedList.Count * (PortSimModel.RunningWeeks)}");
            Console.WriteLine($"Loading condition:{PortSimModel.Loading == WSCPort.containerDwelling.CompletedList.Count * (PortSimModel.RunningWeeks- PortSimModel.WarmUpweeks)}");
            Console.WriteLine($"Flow condition:{PortSimModel.Discharging - PortSimModel.Loading == WSCPort.containerDwelling.CompletedList.Count}");
            Console.WriteLine($"Berth condition:{WSCPort.NumberofBerths == WSCPort.berthBeingIdle.CompletedList.Count}");
            Console.WriteLine($"Vessel condition:{WSCPort.vesselWaiting.CompletedList.Count == 0}");
            Console.WriteLine($"QC condition:{WSCPort.qcBeingIdle.CompletedList.Count == WSCPort.NumberofQCs}");
            Console.WriteLine($"AGV condition:{WSCPort.NumberofAGVs == WSCPort.agvBeingIdle.CompletedList.Count}");
            Console.WriteLine($"YC condition:{WSCPort.NumberofYCs == WSCPort.ycRepositioning.CompletedList.Count}");

            //Console.WriteLine($"containerDwelling.CompletedList:{WSCPort.containerDwelling.CompletedList.Count}");
            //Console.WriteLine($"{PortSimModel.Discharging},{PortSimModel.Loading}");

            //using (StreamWriter SW = new StreamWriter("..\\Distance Matrix.csv"))
            //{
            //    List<ControlPoint> CPs = new List<ControlPoint>();

            //    foreach (QC qc in WSCPort.QCs)
            //        CPs.Add(qc.CP);
            //    foreach (YC yc in WSCPort.YCs)
            //        CPs.Add(yc.CP);

            //    SW.Write($",");

            //    foreach (ControlPoint cp in CPs)
            //        SW.Write($"{cp.Id},");
            //    SW.WriteLine();

            //    foreach (ControlPoint cp in CPs)
            //    {
            //        SW.Write($"{cp.Id},");
            //        foreach (ControlPoint _cp in CPs)
            //        {
            //            SW.Write($"{AGV.CalculateDistance(cp,_cp)},");
            //        }
            //        SW.WriteLine();
            //    }
                                   
            //    SW.Close();
            //}
        }
    }
}
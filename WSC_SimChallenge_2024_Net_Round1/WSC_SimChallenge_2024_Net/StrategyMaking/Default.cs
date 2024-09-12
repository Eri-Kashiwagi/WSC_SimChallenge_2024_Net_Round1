using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSC_SimChallenge_2024_Net.PortSimulation;
using WSC_SimChallenge_2024_Net.PortSimulation.Entity;

namespace WSC_SimChallenge_2024_Net.StrategyMaking
{
    class Default
    {
        public static PortSimModel WSCPort { get; set; }

        public static Berth CustomeizedAllocatedBerth(Vessel arrivalVessel)//for vessel arrival and service, we only consider FIFO;
        {
            Berth allocatedBerth = null;//To determine the berth for vessel; you can choose berth from "currentIdleBerths";
            List<Berth> currentIdleBerths = WSCPort.berthBeingIdle.CompletedList;//This is the list of berths that you can allocate

            allocatedBerth = currentIdleBerths[0];//This is an example: to allocate the earliest being idle berth;
                                                  //Use known information to determine the "allocatedBerth";
                                                  //If there is an arrival vessel, to determine which one idle berth you want to allocate;
                                                  //Define the logic;
            return allocatedBerth;
        }

        public static AGV CustomeizedAllocatedAGVs(Container container)
        {
            AGV allocatedAGV = null;
            List<AGV> currentIdleAGVs = WSCPort.agvBeingIdle.CompletedList;//This is the list of AGVs that you can allocate
            allocatedAGV = currentIdleAGVs.OrderBy(agv => AGV.CalculateDistance(agv.CurrentLocation, container.CurrentLocation)).FirstOrDefault();//This is an example: to allocate the nearest idle AGV;

            return allocatedAGV;
        }

        public static YardBlock CustomeizedDetermineYardBlock(AGV agv)
        {
            YardBlock yardBlock = null;
            List<YardBlock> yardBlocks = WSCPort.YardBlocks;

            yardBlock = yardBlocks.FindAll(block => block.Capacity > block.ReservedSlots + block.StackedContainers.Count)
                .OrderBy(block => AGV.CalculateDistance(block.CP, agv.CurrentLocation)).FirstOrDefault();//This is an example: to find the nearest yardblock with free capacity;

            return yardBlock;
        }

        public Default(PortSimModel wscPort)
        {
            WSCPort = wscPort;
        }

        public static QC DetermineQC(Container container)
        {
            Berth berth = WSCPort.Berths.Find(b => b.BerthedVessel != null && b.BerthedVessel.Id == container.LoadingVesselID &&
            b.BerthedVessel.Week == container.Week + 1);

            QC qc = berth.EquippedQCs[berth.CurrentWorkQC];
            berth.CurrentWorkQC++;
            if (berth.CurrentWorkQC == 3)
                berth.CurrentWorkQC = 0;

            return qc;
        }
    }
}

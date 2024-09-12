using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSC_SimChallenge_2024_Net.PortSimulation.Entity;

namespace WSC_SimChallenge_2024_Net.PortSimulation
{
    class DecisionMaker
    {
        public static PortSimModel WSCPort { get; set; }        
        public static Berth CustomeizedAllocatedBerth(Vessel arrivalVessel)
        {
            Berth allocatedBerth = null;

            return allocatedBerth;
        }

        public static AGV CustomeizedAllocatedAGVs(Container container)
        {
            AGV allocatedAGV = null;

            return allocatedAGV;
        }

        public static YardBlock CustomeizedDetermineYardBlock(AGV agv)
        {
            YardBlock yardBlock = null;

            return yardBlock;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSC_SimChallenge_2024_Net.PortSimulation.Entity
{
    public class ControlPoint
    {
        public string Id;
        public double Xcoordinate, Ycoordinate;
        public override string ToString()
        {
            return $"CP[{Id}]";
        }
    }
}

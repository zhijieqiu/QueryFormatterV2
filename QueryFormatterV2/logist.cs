using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportAgent
{
    class logist
    {
        public double cal_sim(Dictionary<int, double> dis)
        {
            List<double> para = new List<double>() { 21.8096046, 18.8633022, 2.6177423, 0.00221198052, 0.479654878, 12.9978352, 0.5590005, 0.136288434, 0.136288434, -33.0850945 };
            double result=0.0;
            foreach (var pair in dis)
            {
                int index = pair.Key;
                double value = pair.Value;
                result += para[index] * value;
            }
            result += para[8];
            result=1.0 / (1.0 + Math.Exp(-result));
            return result;
        }
        public double cal_sim(List<double> dis)
        {
            //List<double> para = new List<double>() { 1.493675 , 3.904567  ,  1.660158   , 1.567777, -8.320376 };
            List<double> para = new List<double>() { 1.493675, 3.904567, 1.660158, 1.567777, -4.320376 };
            double result = 0.0;
            int i = 0;
            foreach (double dv in dis)
            {

                result += para[i] * dv;
                i++;
            }
            result += para[4];
            //Console.WriteLine(result);
            result = 1.0 / (1.0 + Math.Exp(-result));
           // Console.WriteLine(result);
            return result;
        }
    }
}

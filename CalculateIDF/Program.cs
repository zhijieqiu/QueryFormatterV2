using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CalculateIDF
{
    class Program
    {
        static void Main(string[] args)
        {
            string dir = @"D:\chengji\SupportAgent\FormatQuery\data\";
            string file = dir + "fullAdCorpu.df.tsv";
            string outfile = dir + "idf.tsv";

            using (StreamReader sr = new StreamReader(file))
            using (StreamWriter sw = new StreamWriter(outfile))
            {
                string line = sr.ReadLine();
                double docCnt = Convert.ToDouble(line.Split('\t')[1]);

                while((line = sr.ReadLine()) != null)
                {
                    string w = line.Split('\t')[0];
                    double wCnt = Convert.ToDouble(line.Split('\t')[1]);
                    double idf = Math.Log(1 + (docCnt - wCnt + 0.5) / (wCnt + 0.5));
                    sw.WriteLine(w + "\t" + idf);
                }
            }
        }
    }
}

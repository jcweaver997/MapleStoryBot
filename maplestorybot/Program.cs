using Emgu.CV;
using System;
using System.Windows.Forms;

namespace maplestorybot
{
    internal class Program
    {
        public static void Main(string[] args)
        {

            MS2 ms2 = new MS2();
            ms2.Start();
            Blank b = new Blank();
            b.Start(ms2);
            //Fishing f = new Fishing(ms2);
            //f.Start();
            //FireDragon fd = new FireDragon(ms2);
            //fd.Start();

        }
    }
}
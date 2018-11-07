using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interceptor;
using System.Threading;

namespace maplestorybot
{
    class Blank
    {
        public void Start(MS2 ms2)
        {
            Console.WriteLine("STARTING CUSTOM SCRIPT");
            while (true)
            {
                if (ms2.Ms2Focus())
                {
                    // START CODE HERE

                    // Example code, presses the S key every second, then clicks
                    ms2.input.SendKey(Keys.S, KeyState.Down);
                    Thread.Sleep(50);
                    ms2.input.SendKey(Keys.S, KeyState.Up);
                    Thread.Sleep(950);
                    ms2.input.SendLeftClick();
                }
            }
        }
    }
}

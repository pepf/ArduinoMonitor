using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class Extra
    {
        public static double map(int x, int in_min, int in_max, int out_min, int out_max)
        {
          return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
    }
}

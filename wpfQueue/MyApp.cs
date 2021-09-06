using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfQueue
{
    public static class MyApp
    {
        public static Queue<FilaItem> filaMultiCore = new Queue<FilaItem>();
        public static Queue<FilaItem> filaSingleCore = new Queue<FilaItem>();
        public static long IDfilaMultiCore = 1;
        public static long IDfilaSingleCore = 1;
    }
}

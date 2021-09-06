using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfQueue
{
    public static class MyApp
    {
        public static List<Queue<FilaItem>> filaMultiCore = new List<Queue<FilaItem>>();
        public static Queue<FilaItem> filaSingleCore = new Queue<FilaItem>();


        public static List<FilaItem> arrItemsProcessar = new List<FilaItem>();

    }
}

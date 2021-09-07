using System;
using System.Collections.Generic;

namespace wpfQueue
{
    public static class MyApp
    {
        public static List<Tuple<enTipoFila,Queue<FilaItem>>> filas = new List<Tuple<enTipoFila, Queue<FilaItem>>>();


        public static List<FilaItem> arrItemsProcessar = new List<FilaItem>();

    }
}

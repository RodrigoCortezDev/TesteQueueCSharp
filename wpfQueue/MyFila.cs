using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfQueue
{
    public class MyFila        
    {
        public MyFila(enTipoFila _tipoFila, Queue<FilaItem> _fila, bool _IsRegarregado)
        {
            tipoFila = _tipoFila;
            fila = _fila;
            IsNecessitaProcessar = _IsRegarregado;
        }


        public enTipoFila tipoFila { get; set; }
        public Queue<FilaItem> fila { get; set; }
        public bool IsNecessitaProcessar { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfQueue
{
    public enum enTipoFila { todos, tipo1, tipo2, tipo3  };
    public enum enStatus {Pendente, Concluido, Erro  };
    public enum enTipoCore {Single, Multi };


    public class FilaItem
    {
        public long id { get; set; }
        public enTipoFila tipo { get; set; }
        public string strJsonConteudo { get; set; }
        public string props { get; set; }
        public int qtdeTentativas { get; set; }
        public enStatus status { get; set; }
        public enTipoCore tipoCore { get; set; }
    }
}

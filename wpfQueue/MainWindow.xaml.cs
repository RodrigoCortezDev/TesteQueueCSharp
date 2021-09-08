using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace wpfQueue
{
    public partial class MainWindow : Window
    {
        private List<KeyValuePair<enTipoFila, int>> arrTipoProcess = new List<KeyValuePair<enTipoFila, int>>();
        private List<long> arrProcessamentos = new List<long>();
        private DispatcherTimer tmAtulizaListas;
        private bool IsValidaErro = false;


        public MainWindow()
        {
            InitializeComponent();

            //Seta a qtde de paralelismo de cada tipo de tarefa
            arrTipoProcess.Add(new KeyValuePair<enTipoFila, int>(enTipoFila.tipo1, 3));
            arrTipoProcess.Add(new KeyValuePair<enTipoFila, int>(enTipoFila.tipo2, 50));
            arrTipoProcess.Add(new KeyValuePair<enTipoFila, int>(enTipoFila.tipo3, 50));


            //tempo de atualização de telas
            tmAtulizaListas = new DispatcherTimer();
            tmAtulizaListas.Interval = new TimeSpan(0, 0, 0, 0, 300);
            tmAtulizaListas.Tick += (e, s) => { tmAtulizaListas.Stop(); ExibeListagem(); tmAtulizaListas.Start(); };
            tmAtulizaListas.Start();


            //Inicializando as filas
            foreach (var tipo in Enum.GetValues(typeof(enTipoFila)).Cast<enTipoFila>())
            {
                //Não crio fila para o TODOS
                if (tipo == enTipoFila.todos)
                    continue;

                //Cria-se uma fila para cada tipo de tarefa definida no ENUM
                MyApp.filas.Add(new MyFila(tipo, new Queue<FilaItem>(), true));

            }
            
            //Inicia as escutas das filas
            Task.Run(() => HabilitaFilas());
        }



        private void btCarregaBanco_Click(object sender, RoutedEventArgs e)
        {
            //Simula a criação de 100 items no banco
            var limit = MyApp.arrItemsProcessar.Count + 100;
            for (int i = MyApp.arrItemsProcessar.Count + 1; i <= limit; i++)
            {
                var x = new Random().Next(1, 4);


                enTipoFila tipoFila = enTipoFila.tipo1;
                switch (x)
                {
                    case 1:
                        tipoFila = enTipoFila.tipo1;
                        break;
                    case 2:
                        tipoFila = enTipoFila.tipo2;
                        break;
                    case 3:
                        tipoFila = enTipoFila.tipo3;
                        break;
                    default:
                        break;
                }

                MyApp.arrItemsProcessar.Add(new FilaItem() { id = i, strJsonConteudo = "{ x: 1, z: 'asdasdad' }", props = DateTime.Now.ToString(), qtdeTentativas = 0, status = enStatus.Pendente, tipo = tipoFila });
            }
        }

        private void btExibeResult_Click(object sender, RoutedEventArgs e)
        {
            lvResultadoMulti.Items.Clear();
            try
            {
                foreach (var item in MyApp.arrItemsProcessar)
                    lvResultadoMulti.Items.Add($"ITEM: {item.id} - TIPO: {item.tipo} - QTD: {item.qtdeTentativas} - STATUS: {item.status}");

                //Validando duplicidade (fiz só para checagem)
                var query = arrProcessamentos.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key + " (" + y.Count() + ")").ToList();
            }
            catch
            {
            }
        }

        private void chkSimulaErros_Click(object sender, RoutedEventArgs e)
        {
            IsValidaErro = chkSimulaErros.IsChecked ?? false;
        }




        public int getQtdeProcessPorTipo(enTipoFila tipo)
        {
            //Metodo para saber quantos paralelismos vao ser aplicados por tarefa
            try
            {
                int intQtdeProcessador = arrTipoProcess.FirstOrDefault(f => f.Key == tipo).Value;
                if (intQtdeProcessador == 0)
                    intQtdeProcessador = 1;

                return intQtdeProcessador;
            }
            catch
            {
                return 1;
            }

        }

        public void ExibeListagem()
        {
            //Substituir por observablecolletion depois
            lvItensTipo1.Items.Clear();
            lvItensTipo2.Items.Clear();
            lvItensTipo3.Items.Clear();
            try
            {
                foreach (var item in MyApp.filas.Where(x => x.tipoFila == enTipoFila.tipo1).Select(s => s.fila).FirstOrDefault())
                    lvItensTipo1.Items.Add($"ITEM: {item.id} - QTD: {item.qtdeTentativas}");
                foreach (var item in MyApp.filas.Where(x => x.tipoFila == enTipoFila.tipo2).Select(s => s.fila).FirstOrDefault())
                    lvItensTipo2.Items.Add($"ITEM: {item.id} - QTD: {item.qtdeTentativas}");
                foreach (var item in MyApp.filas.Where(x => x.tipoFila == enTipoFila.tipo3).Select(s => s.fila).FirstOrDefault())
                    lvItensTipo3.Items.Add($"ITEM: {item.id} - QTD: {item.qtdeTentativas}");

            }
            catch
            {
            }
        }

        public void carregaFilaBanco(enTipoFila enTipoCarregar)
        {
            //Carrega items do "banco" (neste exemplo o banco seria a listagem no MyApp
            var arrFila = MyApp.arrItemsProcessar.Where(x => x.status == enStatus.Pendente && x.tipo == enTipoCarregar).OrderBy(o => o.id).Take(500).ToList();

            //Pega a lista do mesmo tipo para preenche-la
            Queue<FilaItem> arrFilaTipada = MyApp.filas.Where(f => f.tipoFila == enTipoCarregar).Select(s => s.fila).FirstOrDefault();
            if (arrFilaTipada == null)
            {
                arrFilaTipada = new Queue<FilaItem>();
                MyApp.filas.Add(new MyFila(enTipoCarregar, arrFilaTipada, false));
            }

            //Percorre o que achou no banco jogando na fila (metodo Enqueue)
            arrFila.ForEach(item => { arrFilaTipada.Enqueue(item); });
        }





        public void HabilitaFilas()
        {
            //Metodo que inicia a escuta das filas
            //Toda fila tem uma variavel "IsPodeProcessar" que serve para dizer que a fila está liberada, ja processou o lote.
            //Se a fila está liberada e não restou nada (validação feita para evitar concorrencias) ai pega mais do banco
            do
            {
                Thread.Sleep(500);


                MyApp.filas.ForEach(fila => 
                {
                    if (fila.fila.Count == 0)
                        carregaFilaBanco(fila.tipoFila);

                    if (fila.IsPodeProcessar == true && fila.fila.Count > 0)
                        Task.Run(() => ProcessaFilas(getQtdeProcessPorTipo(fila.tipoFila), ref fila));
                });

            } while (true);
        }


        public void ProcessaFilas(int intQtdeProcess, ref MyFila myFila)
        {
            //Ao iniciar o processamento da fila em questão, ja marca ela como não pode processar mais, pois vai ser processado este lote.
            try
            {
                myFila.IsPodeProcessar = false;
                do
                {
                    var tasks = new List<Task>();
                    for (int i = 1; i <= intQtdeProcess; i++)
                    {
                        var itemProcessar = myFila.fila.Dequeue();
                        tasks.Add(Task.Run(() => ProcessaItems(itemProcessar)));
                    }

                    Task.WaitAll(tasks.ToArray());

                } while (myFila.fila.Count > 0);
            }
            catch 
            {
                //Tratamentos
            }
            finally
            {
                myFila.IsPodeProcessar = true;
            }
        }


        public void ProcessaItems(FilaItem itemProcessar)
        {
            //Devido ao paralelismo é necessário alguns tratamentos
            //o item a processar pode vir null
            if (itemProcessar == null)
                return;

            //Aconteceu um caso do ultimo item poder ser processador mais de uma vez
            //com essa checagem caso isso ocorra, ele vai abortar pois no inicio do processamento eu ja seto o status
            //caso d~e erro eu atualizo o status.
            if (itemProcessar.status != enStatus.Pendente)
                return;


            try
            {
                //Ja inicia jogando esses valores para evitar concorrencia do paralelismo
                itemProcessar.qtdeTentativas++;
                itemProcessar.status = enStatus.Concluido;


                //Valida o processamento
                switch (itemProcessar.tipo)
                {
                    case enTipoFila.tipo1:
                        //proceessa
                        break;
                    case enTipoFila.tipo2:
                        //proceessa
                        break;
                    case enTipoFila.tipo3:
                        //proceessa
                        break;
                    default:
                        break;
                }
                Thread.Sleep(150); //SIMULANDO ALGO DEMORADO

                //simulando erros aleatórios
                if (IsValidaErro)
                {
                    int x = new Random().Next(1, 4);
                    if (x == 3)
                    {
                        throw new Exception("Erro");
                    }
                }

                //Atualiza no banco com o resultado do processamento.
                MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().status = enStatus.Concluido;
            }
            catch
            {
                //Se for menor que 3 mantem o status PENDENTE no banco ou seja nem mexe, para que seja recuperado na proxima passada.
                //porem se ja tentou 3x ai seta o status de erro e não tanta mais
                if (itemProcessar.qtdeTentativas > 3)
                {
                    itemProcessar.status = enStatus.Erro;
                    MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().status = enStatus.Erro;
                }
                else
                {
                    itemProcessar.status = enStatus.Pendente;
                    MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().status = enStatus.Pendente;
                }
            }
            finally
            {
                //Atualiza sempre no "banco" a qtde de tentativas
                MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().qtdeTentativas = itemProcessar.qtdeTentativas;
                arrProcessamentos.Add(itemProcessar.id);
            }
        }

        
    }
}

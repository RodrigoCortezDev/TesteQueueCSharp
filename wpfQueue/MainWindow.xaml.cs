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

        private DispatcherTimer tmAtulizaListas;


        public MainWindow()
        {
            InitializeComponent();

            arrTipoProcess.Add(new KeyValuePair<enTipoFila, int>(enTipoFila.tipo2, 3));
            arrTipoProcess.Add(new KeyValuePair<enTipoFila, int>(enTipoFila.tipo3, 3));


            tmAtulizaListas = new DispatcherTimer();
            tmAtulizaListas.Interval = new TimeSpan(0, 0, 0, 0, 300);
            tmAtulizaListas.Tick += (e, s) => { tmAtulizaListas.Stop(); ExibeListagem(); tmAtulizaListas.Start(); };
            tmAtulizaListas.Start();


            if (MyApp.filas.Count == 0)
            {
                MyApp.filas.Add(new Tuple<enTipoFila, Queue<FilaItem>>(enTipoFila.tipo1, new Queue<FilaItem>()));
                MyApp.filas.Add(new Tuple<enTipoFila, Queue<FilaItem>>(enTipoFila.tipo2, new Queue<FilaItem>()));
                MyApp.filas.Add(new Tuple<enTipoFila, Queue<FilaItem>>(enTipoFila.tipo3, new Queue<FilaItem>()));
            }

            Task.Run(() => HabilitaFilas());
        }



        private void btCarregaBanco_Click(object sender, RoutedEventArgs e)
        {
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
            }
            catch
            {
            }
        }





        public int getProcessPorTipo(enTipoFila tipo)
        {
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
                foreach (var item in MyApp.filas.Where(x => x.Item1 == enTipoFila.tipo1).Select(s => s.Item2).FirstOrDefault())
                    lvItensTipo1.Items.Add($"ITEM: {item.id} - QTD: {item.qtdeTentativas}");
                foreach (var item in MyApp.filas.Where(x => x.Item1 == enTipoFila.tipo2).Select(s => s.Item2).FirstOrDefault())
                    lvItensTipo2.Items.Add($"ITEM: {item.id} - QTD: {item.qtdeTentativas}");
                foreach (var item in MyApp.filas.Where(x => x.Item1 == enTipoFila.tipo3).Select(s => s.Item2).FirstOrDefault())
                    lvItensTipo3.Items.Add($"ITEM: {item.id} - QTD: {item.qtdeTentativas}");

            }
            catch
            {
            }
        }


        public void carregaFilaBanco(enTipoFila enTipoCarregar)
        {
            //Carrega items do banco que forem do tipo MULTI CORE (Envios de email, processamentos que podem serem feitos desordenados)            
            var arrFila = MyApp.arrItemsProcessar.Where(x => x.status == enStatus.Pendente && x.tipo == enTipoCarregar).OrderBy(o => o.id).Take(500).ToList();

            //tenta achar uma lista em uso do mesmo tipo ou alguma vazia
            Queue<FilaItem> arrFilaTipada = MyApp.filas.Where(f => f.Item1 == enTipoCarregar).Select(s => s.Item2).FirstOrDefault();
            if (arrFilaTipada == null)
            {
                arrFilaTipada = new Queue<FilaItem>();
                MyApp.filas.Add(new Tuple<enTipoFila, Queue<FilaItem>>(enTipoCarregar, arrFilaTipada));
            }

            arrFila.ForEach(item => { arrFilaTipada.Enqueue(item); });
        }





        public void HabilitaFilas()
        {
            do
            {
                MyApp.filas.ForEach(fila =>
                {
                    if (fila.Item2.Count == 0)
                    {
                        Thread.Sleep(500);
                        carregaFilaBanco(fila.Item1);
                    }

                    if (fila.Item2.Count == 0)
                        return;

                    ProcessaFilas(getProcessPorTipo(fila.Item1), fila.Item2);
                });


            } while (true);
        }


        public async void ProcessaFilas(int intQtdeProcess, Queue<FilaItem> arrFila)
        {
            do
            {
                if (arrFila.Count == 0)
                    break;


                if (intQtdeProcess == 1)
                {
                    Thread.Sleep(50);
                    await Task.Run(() => ProcessaItems(ref arrFila));
                }
                else
                {
                    var tasks = new List<Task>();
                    for (int i = 1; i <= intQtdeProcess; i++)
                    {
                        Thread.Sleep(300);
                        tasks.Add(Task.Run(() => ProcessaItems(ref arrFila)));
                    }
                    await Task.WhenAll(tasks);
                }

                Thread.Sleep(50);

            } while (true);
        }


        public void ProcessaItems(ref Queue<FilaItem> arrFila)
        {
            if (arrFila.Count == 0)
                return;


            FilaItem itemProcessar = arrFila.Dequeue();
            if (itemProcessar == null)
                return;

            try
            {
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
                itemProcessar.qtdeTentativas++;

                //simulando erros aleatórios
                int x = new Random().Next(1, 4);
                if (x == 3)
                {
                    itemProcessar.status = enStatus.Erro;
                    throw new Exception("Erro");
                }

                //Atualiza no banco com o resultado do processamento.
                itemProcessar.status = enStatus.Concluido;
                MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().status = enStatus.Concluido;
            }
            catch
            {
                if (itemProcessar.qtdeTentativas <= 3)
                {
                    //Recolocando na fila
                    arrFila.Enqueue(itemProcessar);
                }
                else
                {
                    //Atuliza banco informando que item falhou pois ja tentou 3x
                    MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().status = enStatus.Erro;
                }
            }
            finally
            {
                MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().qtdeTentativas = itemProcessar.qtdeTentativas;
            }
        }
    }
}

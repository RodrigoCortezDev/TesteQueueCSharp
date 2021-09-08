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


        public MainWindow()
        {
            InitializeComponent();

            arrTipoProcess.Add(new KeyValuePair<enTipoFila, int>(enTipoFila.tipo2, 1));
            arrTipoProcess.Add(new KeyValuePair<enTipoFila, int>(enTipoFila.tipo2, 1));
            arrTipoProcess.Add(new KeyValuePair<enTipoFila, int>(enTipoFila.tipo3, 1));


            tmAtulizaListas = new DispatcherTimer();
            tmAtulizaListas.Interval = new TimeSpan(0, 0, 0, 0, 300);
            tmAtulizaListas.Tick += (e, s) => { tmAtulizaListas.Stop(); ExibeListagem(); tmAtulizaListas.Start(); };
            tmAtulizaListas.Start();


            if (MyApp.filas.Count == 0)
            {
                MyApp.filas.Add(new MyFila(enTipoFila.tipo1, new Queue<FilaItem>(), false));
                MyApp.filas.Add(new MyFila(enTipoFila.tipo2, new Queue<FilaItem>(), false));
                MyApp.filas.Add(new MyFila(enTipoFila.tipo3, new Queue<FilaItem>(), false));
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

                var query = arrProcessamentos.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key + " (" + y.Count() + ")").ToList();
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
            //Carrega items do banco que forem do tipo MULTI CORE (Envios de email, processamentos que podem serem feitos desordenados)            
            var arrFila = MyApp.arrItemsProcessar.Where(x => x.status == enStatus.Pendente && x.tipo == enTipoCarregar).OrderBy(o => o.id).Take(500).ToList();

            //tenta achar uma lista em uso do mesmo tipo ou alguma vazia
            Queue<FilaItem> arrFilaTipada = MyApp.filas.Where(f => f.tipoFila == enTipoCarregar).Select(s => s.fila).FirstOrDefault();
            if (arrFilaTipada == null)
            {
                arrFilaTipada = new Queue<FilaItem>();
                MyApp.filas.Add(new MyFila(enTipoCarregar, arrFilaTipada, false));
            }

            arrFila.ForEach(item => { arrFilaTipada.Enqueue(item); });
        }





        public void HabilitaFilas()
        {
            do
            {
                Thread.Sleep(500);
                foreach (var fila in MyApp.filas)
                {
                    if (fila.fila.Count == 0)
                    {
                        carregaFilaBanco(fila.tipoFila);

                        if (fila.fila.Count > 0)
                            fila.IsNecessitaProcessar = true;
                    }

                    if (fila.IsNecessitaProcessar == true)
                        Task.Run(() => ProcessaFilas(getProcessPorTipo(fila.tipoFila), fila)); 
                }

            } while (true);
        }


        public async void ProcessaFilas(int intQtdeProcess, MyFila myFila)
        {
            myFila.IsNecessitaProcessar = false;
            var fila = myFila.fila;


            do
            {
                if (fila.Count == 0)
                    break;

                if (intQtdeProcess == 1)
                    await Task.Run(() => ProcessaItems(ref fila));
                else
                {
                    var tasks = new List<Task>();
                    for (int i = 1; i <= intQtdeProcess; i++)
                        tasks.Add(Task.Run(() => ProcessaItems(ref fila)));

                    await Task.WhenAll(tasks);
                }
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
                //int x = new Random().Next(1, 4);
                //if (x == 3)
                //{
                //    throw new Exception("Erro");
                //}

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
                    itemProcessar.status = enStatus.Erro;
                    MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().status = enStatus.Erro;
                }
            }
            finally
            {
                MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().qtdeTentativas = itemProcessar.qtdeTentativas;
                arrProcessamentos.Add(itemProcessar.id);
            }
        }
    }
}

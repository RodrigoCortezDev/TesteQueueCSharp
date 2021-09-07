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

            Task.Run(() => ProcessaFila());
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

                MyApp.arrItemsProcessar.Add(new FilaItem() { id = i, strJsonConteudo = "{ x: 1, z: 'asdasdad' }", props = DateTime.Now.ToString(), qtdeTentativas = 0, status = enStatus.Pendente, tipo = tipoFila});
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






        public void ExibeListagem()
        {
            //Substituir por observablecolletion depois
            lvItensTipo1.Items.Clear();
            lvItensTipo2.Items.Clear();
            lvItensTipo3.Items.Clear();
            try
            {
                foreach (var item in MyApp.filas.Where(x => x.Any(a => a.tipo == enTipoFila.tipo1)).FirstOrDefault())
                    lvItensTipo1.Items.Add($"ITEM: {item.id} - QTD: {item.qtdeTentativas}");
                foreach (var item in MyApp.filas.Where(x => x.Any(a => a.tipo == enTipoFila.tipo2)).FirstOrDefault())
                    lvItensTipo2.Items.Add($"ITEM: {item.id} - QTD: {item.qtdeTentativas}");
                foreach (var item in MyApp.filas.Where(x => x.Any(a => a.tipo == enTipoFila.tipo3)).FirstOrDefault())
                    lvItensTipo3.Items.Add($"ITEM: {item.id} - QTD: {item.qtdeTentativas}");

            }
            catch
            {
            }
        }


        public void carregaFilaBanco()
        {
            //Carrega items do banco que forem do tipo MULTI CORE (Envios de email, processamentos que podem serem feitos desordenados)            
            var arrFila = MyApp.arrItemsProcessar.Where(x => x.status == enStatus.Pendente).OrderBy(o => o.id).Take(500).ToList();
            var arrTipos = arrFila.Select(s => s.tipo).Distinct();

            foreach (var tipo in arrTipos)
            {
                //tenta achar uma lista em uso do mesmo tipo ou alguma vazia
                Queue<FilaItem> arrFilaTipada = MyApp.filas.FirstOrDefault(f => f.Any(a => a.tipo == tipo) || f.Count == 0);
                if (arrFilaTipada == null)
                {
                    arrFilaTipada = new Queue<FilaItem>();
                    MyApp.filas.Add(arrFilaTipada);
                }

                arrFila.Where(x => x.tipo == tipo).ToList().ForEach(item => { arrFilaTipada.Enqueue(item); });
            }
        }


        public async void ProcessaFila()
        {
            do
            {
                //Se a soma das qtde restantes das filas for zero ai carrega
                if (MyApp.filas.Sum(x => x.Count()) == 0)
                    await Task.Run(() => { Thread.Sleep(500); carregaFilaBanco(); });

                if (MyApp.filas.Sum(x => x.Count()) == 0)
                    continue;

                var tasks = new List<Task>();

                MyApp.filas.ForEach(fila =>
                {
                    if (fila.Count == 0)
                        return;

                    int? intQtdeProcessador = arrTipoProcess.FirstOrDefault(f => f.Key == fila.First().tipo).Value;
                    if (intQtdeProcessador == null || intQtdeProcessador == 0)
                        intQtdeProcessador = 1;

                    for (int i = 1; i <= intQtdeProcessador; i++)
                    {
                        if (intQtdeProcessador > 1)
                            Thread.Sleep(50);

                        tasks.Add(Task.Run(() => ProcessaItemsFila(ref fila)));
                    }
                });

                await Task.WhenAll(tasks.ToArray());
            } while (true);
        }




        public void ProcessaItemsFila(ref Queue<FilaItem> arrFila)
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


                //simulando erros aleatórios
                int x = new Random().Next(1, 5);
                if (x == 4)
                {
                    itemProcessar.status = enStatus.Erro;
                    throw new Exception("Erro");
                }

                //Atualiza no banco com o resultado do processamento.
                MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().status = enStatus.Concluido;
            }
            catch
            {
                itemProcessar.qtdeTentativas++;

                //Recolocando na fila
                if (itemProcessar.qtdeTentativas <= 3)
                {
                    arrFila.Enqueue(itemProcessar);
                }
                else
                {
                    //Atuliza banco informando que item falhou pois ja tentou 3x
                    MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().status = enStatus.Erro;
                }
            }
        }
    }
}

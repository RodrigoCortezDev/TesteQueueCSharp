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
        private int intQtdeProcessador = 3;


        private long lngRealizadoSingle = 0;
        private long lngRealizadoMult = 0;
        private DispatcherTimer tmAtulizaListas;



        public MainWindow()
        {
            InitializeComponent();

            tmAtulizaListas = new DispatcherTimer();
            tmAtulizaListas.Interval = new TimeSpan(0, 0, 0, 0, 300);
            tmAtulizaListas.Tick += (e, s) => { tmAtulizaListas.Stop(); ExibeListagemSingleCore(); ExibeListagemMultiCore(); tmAtulizaListas.Start(); };
            tmAtulizaListas.Start();

            Task.Run(() => ProcessaFilaSingleCore());
            Task.Run(() => ProcessaFilaMultiCore());
        }


        private void btCarregaBanco_Click(object sender, RoutedEventArgs e)
        {
            var limit = MyApp.arrItemsProcessar.Count + 100;
            for (int i = MyApp.arrItemsProcessar.Count + 1; i <= limit; i++)
            {
                var x = new Random().Next(1, 4);

                enTipoCore tipoDefinir = enTipoCore.Single;
                if (x >= 2)
                    tipoDefinir = enTipoCore.Multi;


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

                MyApp.arrItemsProcessar.Add(new FilaItem() { id = i, strJsonConteudo = "{ x: 1, z: 'asdasdad' }", props = DateTime.Now.ToString(), qtdeTentativas = 0, status = enStatus.Pendente, tipo = tipoFila, tipoCore = tipoDefinir });
            }
        }


        private void btExibeResult_Click(object sender, RoutedEventArgs e)
        {
            lvResultadoSingle.Items.Clear();
            lvResultadoMulti.Items.Clear();
            try
            {
                foreach (var item in MyApp.arrItemsProcessar.Where(x => x.tipoCore == enTipoCore.Single))
                    lvResultadoSingle.Items.Add($"ITEM: {item.id} - TIPO: {item.tipo} - QTD: {item.qtdeTentativas} - STATUS: {item.status}");

                foreach (var item in MyApp.arrItemsProcessar.Where(x => x.tipoCore == enTipoCore.Multi))
                    lvResultadoMulti.Items.Add($"ITEM: {item.id} - TIPO: {item.tipo} - QTD: {item.qtdeTentativas} - STATUS: {item.status}");
            }
            catch
            {
            }
        }





        #region SINGLE CORE

        public void ExibeListagemSingleCore()
        {
            //Substituir por observablecolletion depois
            lvItensSingleCore.Items.Clear();
            try
            {
                foreach (var item in MyApp.filaSingleCore)
                    lvItensSingleCore.Items.Add($"ITEM: {item.id} - TIPO: {item.tipo.ToString()} - QTD: {item.qtdeTentativas}");

                tbRealizadoSingleCore.Text = $"Realizado: {lngRealizadoSingle} / {MyApp.arrItemsProcessar.Where(x => x.tipoCore == enTipoCore.Single).ToList().Count}";
            }
            catch
            {
            }
        }


        public void carregaFilaBancoSingleCore()
        {
            //Carrega items do banco que forem do tipo SINGLE CORE (envios de nota, cancelamento de nota)
            var arrFila = MyApp.arrItemsProcessar.Where(x => x.status == enStatus.Pendente && x.tipoCore == enTipoCore.Single).OrderBy(o => o.id).Take(100).ToList();
            arrFila.ForEach(item => { MyApp.filaSingleCore.Enqueue(item); });
        }


        public async void ProcessaFilaSingleCore()
        {
            do
            {
                if (MyApp.filaSingleCore.Count == 0)
                    await Task.Run(() => { Thread.Sleep(500); carregaFilaBancoSingleCore(); });

                if (MyApp.filaSingleCore.Count == 0)
                    continue;

                await Task.Run(() => ProcessaItemsFila(ref MyApp.filaSingleCore));

            } while (true);
        }

        #endregion



        #region MULTI CORE

        public void ExibeListagemMultiCore()
        {
            //Substituir por observablecolletion depois
            lvItensMultiCore.Items.Clear();
            try
            {
                foreach (var fila in MyApp.filaMultiCore)
                    foreach (var item in fila)
                        lvItensMultiCore.Items.Add($"ITEM: {item.id} - TIPO: {item.tipo.ToString()} - QTD: {item.qtdeTentativas}");

                tbRealizadoMultiCore.Text = $"Realizado: {lngRealizadoMult} / {MyApp.arrItemsProcessar.Where(x => x.tipoCore == enTipoCore.Multi).ToList().Count}";
            }
            catch
            {
            }
        }


        public void carregaFilaBancoMultiCore()
        {
            //Carrega items do banco que forem do tipo MULTI CORE (Envios de email, processamentos que podem serem feitos desordenados)            
            var arrFila = MyApp.arrItemsProcessar.Where(x => x.status == enStatus.Pendente && x.tipoCore == enTipoCore.Multi).OrderBy(o => o.id).Take(200).ToList();
            var arrTipos = arrFila.Select(s => s.tipo).Distinct();

            foreach (var tipo in arrTipos)
            {
                //tenta achar uma lista em uso do mesmo tipo ou alguma vazia
                Queue<FilaItem> arrFilaTipada = MyApp.filaMultiCore.FirstOrDefault(f => f.Any(a => a.tipo == tipo) || f.Count == 0);
                if (arrFilaTipada == null)
                {
                    arrFilaTipada = new Queue<FilaItem>();
                    MyApp.filaMultiCore.Add(arrFilaTipada);
                }

                arrFila.Where(x => x.tipo == tipo).ToList().ForEach(item => { arrFilaTipada.Enqueue(item); });
            }
        }


        public async void ProcessaFilaMultiCore()
        {
            do
            {
                //Se a soma das qtde restantes das filas for zero ai carrega
                if (MyApp.filaMultiCore.Sum(x => x.Count()) == 0)
                    await Task.Run(() => { Thread.Sleep(500); carregaFilaBancoMultiCore(); });

                if (MyApp.filaMultiCore.Sum(x => x.Count()) == 0)
                    continue;

                var tasks = new List<Task>();

                MyApp.filaMultiCore.ForEach(fila =>
                {
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

        #endregion



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


                //Atualizando o realizado
                if (itemProcessar.tipoCore == enTipoCore.Single)
                    lngRealizadoSingle++;
                else if (itemProcessar.tipoCore == enTipoCore.Multi)
                    lngRealizadoMult++;

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


                    //Atualizando o realizado
                    if (itemProcessar.tipoCore == enTipoCore.Single)
                        lngRealizadoSingle++;
                    else if (itemProcessar.tipoCore == enTipoCore.Multi)
                        lngRealizadoMult++;
                }
            }
            finally
            {

            }
        }
    }
}

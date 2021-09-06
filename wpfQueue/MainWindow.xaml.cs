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
        private int intQtdeProcessador = 5;


        private long lngRealizadoSingle = 1;
        private long lngRealizadoMult = 1;



        public MainWindow()
        {
            InitializeComponent();

            ProcessaFilaSingleCore();
            ProcessaFilaMultiCore();
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
            lvResultado.Items.Clear();
            try
            {
                foreach (var item in MyApp.arrItemsProcessar)
                    lvResultado.Items.Add("ITEM: " + item.id + " - PROPS: " + item.props + " - QTD:" + item.qtdeTentativas + " - STATUS: " + item.status.ToString());
            }
            catch
            {
            }
        }





        #region SINGLE CORE

        public void ExibeListagemSingleCore()
        {
            //Substituir por observablecolletion depois
            Dispatcher.BeginInvoke(new Action(() =>
            {
                lvItensSingleCore.Items.Clear();
                try
                {
                    foreach (var item in MyApp.filaSingleCore)
                        lvItensSingleCore.Items.Add($"ITEM: {item.id} - TIPO: {item.tipo.ToString()} - QTD: {item.qtdeTentativas}");
                }
                catch
                {
                }
            }));
        }


        public void carregaFilaBancoSingleCore()
        {
            //Carrega items do banco que forem do tipo SINGLE CORE (envios de nota, cancelamento de nota)
            var arrFila = MyApp.arrItemsProcessar.Where(x => x.status == enStatus.Pendente && x.tipoCore == enTipoCore.Single).ToList();
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

                await Task.Run(() => { ProcessaItemsFila("S"); });

            } while (true);
        }

        #endregion



        #region MULTI CORE

        public void ExibeListagemMultiCore()
        {
            //Substituir por observablecolletion depois
            Dispatcher.BeginInvoke(new Action(() =>
            {
                lvItensMultiCore.Items.Clear();
                try
                {
                    foreach (var item in MyApp.filaMultiCore)
                        lvItensMultiCore.Items.Add($"ITEM: {item.id} - TIPO: {item.tipo.ToString()} - QTD: {item.qtdeTentativas}");
                }
                catch
                {
                }
            }));

        }


        public void carregaFilaBancoMultiCore()
        {
            //Carrega items do banco que forem do tipo MULTI CORE (Envios de email, processamentos que podem serem feitos desordenados)
            var arrFila = MyApp.arrItemsProcessar.Where(x => x.status == enStatus.Pendente && x.tipoCore == enTipoCore.Multi).ToList();
            arrFila.ForEach(item => { MyApp.filaMultiCore.Enqueue(item); });
        }


        public async void ProcessaFilaMultiCore()
        {
            do
            {
                if (MyApp.filaMultiCore.Count == 0)
                    await Task.Run(() => { Thread.Sleep(500); carregaFilaBancoMultiCore(); });

                if (MyApp.filaMultiCore.Count == 0)
                    continue;

                var tasks = new List<Task>();
                for (int i = 1; i <= intQtdeProcessador; i++)
                {
                    if (intQtdeProcessador > 1)
                        Thread.Sleep(50);

                    tasks.Add(Task.Factory.StartNew(() => { ProcessaItemsFila("M"); }));
                }
                await Task.WhenAll(tasks);

            } while (true);
        }

        #endregion



        public void ProcessaItemsFila(string strTipo)
        {
            FilaItem itemProcessar = null;


            if (strTipo == "S")
            {
                if (MyApp.filaSingleCore.Count == 0)
                    return;


                itemProcessar = MyApp.filaSingleCore.Dequeue();
            }
            else if (strTipo == "M")
            {
                if (MyApp.filaMultiCore.Count == 0)
                    return;

                itemProcessar = MyApp.filaMultiCore.Dequeue();

            }


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
                Thread.Sleep(200); //SIMULANDO ALGO DEMORADO


                //simulando erros aleatórios
                int x = new Random().Next(1, 4);
                if (x == 3)
                {
                    itemProcessar.status = enStatus.Erro;
                    throw new Exception("Erro");
                }

                //Atualiza no banco com o resultado do processamento.
                MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().status = enStatus.Concluido;


                //Atualizando o realizado
                if (strTipo == "S")
                    Dispatcher.BeginInvoke(new Action(() => { tbRealizadoSingleCore.Text = $"Realizado: {lngRealizadoSingle++} / {MyApp.arrItemsProcessar.Where(x=>x.tipoCore == enTipoCore.Single).ToList().Count}"; }));
                else if (strTipo == "M")
                    Dispatcher.BeginInvoke(new Action(() => { tbRealizadoMultiCore.Text = $"Realizado: {lngRealizadoMult++} / {MyApp.arrItemsProcessar.Where(x => x.tipoCore == enTipoCore.Multi).ToList().Count}"; }));

            }
            catch 
            {
                itemProcessar.qtdeTentativas++;

                //Recolocando na fila
                if (itemProcessar.qtdeTentativas <= 3)
                {
                    if (strTipo == "S")
                        MyApp.filaSingleCore.Enqueue(itemProcessar);
                    else if (strTipo == "M")
                        MyApp.filaMultiCore.Enqueue(itemProcessar);
                }
                else
                {
                    //Atuliza banco informando que item falhou pois ja tentou 3x
                    MyApp.arrItemsProcessar.Where(x => x.id == itemProcessar.id).First().status = enStatus.Erro;


                    //Atualizando o realizado
                    if (strTipo == "S")
                        Dispatcher.BeginInvoke(new Action(() => { tbRealizadoSingleCore.Text = $"Realizado: {lngRealizadoSingle++} / {MyApp.arrItemsProcessar.Where(x => x.tipoCore == enTipoCore.Single).ToList().Count}"; }));
                    else if (strTipo == "M")
                        Dispatcher.BeginInvoke(new Action(() => { tbRealizadoMultiCore.Text = $"Realizado: {lngRealizadoMult++} / {MyApp.arrItemsProcessar.Where(x => x.tipoCore == enTipoCore.Multi).ToList().Count}"; }));
                }
            }
            finally
            {
                if (strTipo == "S")
                    ExibeListagemSingleCore();
                else if (strTipo == "M")
                    ExibeListagemMultiCore();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace wpfQueue
{
    public partial class MainWindow : Window
    {
        private int intQtdeProcessador = 5;


        public MainWindow()
        {
            InitializeComponent();


            //Inicia o Workers
            ProcessaFilaSingleCore();
            ProcessaFilaMultiCore();
        }




        #region SINGLE CORE
        public void ExibeListagemSingleCore()
        {
            //Substituir por observablecolletion depois
            lvItensSingleCore.Items.Clear();
            try
            {
                foreach (var item in MyApp.filaSingleCore)
                    lvItensSingleCore.Items.Add("ITEM: " + item.id + " - PROPS: " + item.props + " - QTD:" + item.qtdeTentativas);

                tbRealizadoSingleCore.Text = lvItensRealizadoSingleCore.Items.Count.ToString();
            }
            catch
            {
            }
        }



        public void carregaFilaBancoSingleCore()
        {
            //Carrega items do banco que forem do tipo SINGLE CORE (envios de nota, cancelamento de nota)
            int x = 30; //new Random().Next(1, 30);
            while (x >= 0)
            {
                MyApp.filaSingleCore.Enqueue(new FilaItem() { id = MyApp.IDfilaSingleCore, tipo = enTipoFila.tipo1, strJsonConteudo = "", props = DateTime.Now.ToString(), status = enStatus.Pendente });
                MyApp.IDfilaSingleCore++;
                x--;
            }
        }


        public async void ProcessaFilaSingleCore()
        {
            do
            {
                if (MyApp.filaSingleCore.Count == 0)
                    await Task.Run(() => { carregaFilaBancoSingleCore(); });

                await Task.Run(() => { ProcessaItemsFila("S"); });

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
                foreach (var item in MyApp.filaMultiCore)
                    lvItensMultiCore.Items.Add("ITEM: " + item.id + " - PROPS: " + item.props + " - QTD:" + item.qtdeTentativas);

                tbRealizadoMultiCore.Text = lvItensRealizadoMultiCore.Items.Count.ToString();
            }
            catch
            {
            }
        }



        public void carregaFilaBancoMultiCore()
        {
            //Carrega items do banco que forem do tipo MULTI CORE (Envios de email, processamentos que podem serem feitos desordenados)
            int x = 30; //new Random().Next(1, 30);
            while (x >= 0)
            {
                MyApp.filaMultiCore.Enqueue(new FilaItem() { id = MyApp.IDfilaMultiCore, tipo = enTipoFila.tipo1, strJsonConteudo = "", props = DateTime.Now.ToString(), status = enStatus.Pendente });
                MyApp.IDfilaMultiCore++;
                x--;
            }
        }



        public async void ProcessaFilaMultiCore()
        {
            do
            {
                if (MyApp.filaMultiCore.Count == 0)
                    await Task.Run(() => { carregaFilaBancoMultiCore(); });


                var tasks = new List<Task>();
                for (int i = 1; i <= intQtdeProcessador; i++)
                {
                    if (intQtdeProcessador > 1)
                        Thread.Sleep(100);

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
                Thread.Sleep(500); //SIMULANDO ALGO DEMORADO


                //simulando erros aleatórios
                int x = new Random().Next(1, 4);
                if (x == 3)
                {
                    itemProcessar.status = enStatus.Erro;
                    throw new Exception();
                }

                //Atualiza no banco com o resultado do processamento.
                itemProcessar.status = enStatus.Concluido;

                //Atualizando o realizado
                if (strTipo == "S")
                    Dispatcher.BeginInvoke(new Action(() => { lvItensRealizadoSingleCore.Items.Add($"ITEM({strTipo}): {itemProcessar.id} - QTD: {itemProcessar.qtdeTentativas}"); }));
                else if (strTipo == "M")
                    Dispatcher.BeginInvoke(new Action(() => { lvItensRealizadoMultiCore.Items.Add($"ITEM({strTipo}): {itemProcessar.id} - QTD: {itemProcessar.qtdeTentativas}"); }));

            }
            catch (Exception ex)
            {

                if (itemProcessar.qtdeTentativas <= 3)
                {
                    //Recolocando na fila
                    if (strTipo == "S")
                        MyApp.filaSingleCore.Enqueue(itemProcessar);
                    else if (strTipo == "M")
                        MyApp.filaMultiCore.Enqueue(itemProcessar);
                }
                else
                {
                    //Atuliza banco informando que item falhou
                }
                itemProcessar.qtdeTentativas++;
            }
            finally
            {
                if (strTipo == "S")
                    Dispatcher.BeginInvoke(new Action(() => { ExibeListagemSingleCore(); }));
                else if (strTipo == "M")
                    Dispatcher.BeginInvoke(new Action(() => { ExibeListagemMultiCore(); }));
            }
        }
    }
}

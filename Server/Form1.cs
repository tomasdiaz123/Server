using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class Form1 : Form
    {
        enum Command
        {
            Login,      //Entrada/conectar
            Logout,     //Saída/desconectar
            Message,    //Envio de mensagem para todos os clientes
            List,       //Obter lista dos utilizadores
            Null,        //auxiliar
            Atacar
        }
        //Estrutura com informação de todos os clientes ligados ao servidor
        struct ClientInfo
        {
            public EndPoint endpoint;   //Socket para o cliente
            public string strName;      //Nome do cliente no Chat
        }

        //Colecção de todos os clientes no Chat(array do tipo ClientInfo)
        ArrayList clientList;

        //Socket principal que aguarda conexões
        Socket serverSocket;
        int cnt;
        byte[] byteData = new byte[1024];
        Data msgToSend = new Data();
        public Form1()
        {
            clientList = new ArrayList();
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            cnt = 0;
            try
            {
                CheckForIllegalCrossThreadCalls = false;

                //Tipo de socket -> UDP
                serverSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram, ProtocolType.Udp);

                //IP do servidor a aguardar ligação na porta 1000
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 8000);

                //Associar o IP ao Socket
                serverSocket.Bind(ipEndPoint);

                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                //Identificar clientes 
                EndPoint epSender = (EndPoint)ipeSender;

                //Receber dados
                serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length,
                    SocketFlags.None, ref epSender, new AsyncCallback(OnReceive), epSender);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSServerUDP",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint epSender = (EndPoint)ipeSender;

                serverSocket.EndReceiveFrom(ar, ref epSender);

                //Transformar o array de bytes recebido do utilizador num objecto de dados
                Data msgReceived = new Data(byteData);
                //Enviar o objecto em resposta aos pedidos dos clientes
                Data msgToSend = new Data();

                byte[] message;
                message = msgToSend.ToByte();
                if(msgReceived.cmdCommand == Command.Login)
                {
                    ClientInfo clientI = new ClientInfo();
                    clientI.strName = msgReceived.strName;
                    clientI.endpoint = epSender;
                    clientList.Add(clientI);
                }
                if(msgReceived.cmdCommand == Command.Atacar)
                {
                    foreach(ClientInfo clientI in clientList)
                    {
                        if(clientI.strName != msgReceived.strName)
                        {
                            msgToSend.strMessage = msgReceived.strMessage;
                            msgToSend.cmdCommand = msgReceived.cmdCommand;
                            msgToSend.strName = clientI.strName;
                            message = msgToSend.ToByte();
                            //Enviar a posição ao client 2 depois clienmt 2 ve se acerta se ss message = Acertou!
                            serverSocket.BeginSendTo(message, 0, message.Length, SocketFlags.None, clientI.endpoint,
                              new AsyncCallback(OnSend), clientI.endpoint);
                        }
                    }
                   
                }
                
              

                //Se o utilizador saiu, não é necessário continuar a aguardar dados
                if (msgReceived.cmdCommand != Command.Logout)
                {
                    //Aguardar dados do cliente
                    serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref epSender,
                        new AsyncCallback(OnReceive), epSender);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Servidor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void OnSend(IAsyncResult ar)
        {
            try
            {
                serverSocket.EndSend(ar);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Servidor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Estrutura de dados para servidores e clientes poderem comunicar
        class Data
        {
            public Data()
            {
                this.cmdCommand = Command.Null;
                this.strMessage = null;
                this.strName = null;
            }

            //Converte os bytes num objecto do tipo Data
            public Data(byte[] data)
            {
                //4 bytes para o comando
                this.cmdCommand = (Command)BitConverter.ToInt32(data, 0);

                //5-8 segundos bytes para o nome
                int nameLen = BitConverter.ToInt32(data, 4);

                //9-12 para a mensagem
                int msgLen = BitConverter.ToInt32(data, 8);

                //Garantir que a string strName passou para o array de bytes
                if (nameLen > 0)
                    this.strName = Encoding.UTF8.GetString(data, 12, nameLen);
                else
                    this.strName = null;

                //Verificar se a mensagem tem conteúdo
                if (msgLen > 0)
                    this.strMessage = Encoding.UTF8.GetString(data, 12 + nameLen, msgLen);
                else
                    this.strMessage = null;
            }

            //Converter a estrutura de dados num array de bytes
            public byte[] ToByte()
            {
                List<byte> result = new List<byte>();

                //primeiros 4 bytes para o comando
                result.AddRange(BitConverter.GetBytes((int)cmdCommand));

                //adicionar o nome
                if (strName != null)
                    result.AddRange(BitConverter.GetBytes(strName.Length));
                else
                    result.AddRange(BitConverter.GetBytes(0));

                //adicionar mensagem
                if (strMessage != null)
                    result.AddRange(BitConverter.GetBytes(strMessage.Length));
                else
                    result.AddRange(BitConverter.GetBytes(0));

                if (strName != null)
                    result.AddRange(Encoding.UTF8.GetBytes(strName));

                //adicionar a mensagem
                if (strMessage != null)
                    result.AddRange(Encoding.UTF8.GetBytes(strMessage));

                return result.ToArray();
            }

            public string strName;      //Nome do cliente no Chat
            public string strMessage;   //Messagem
            public Command cmdCommand;  //Tipo de comando (login, logout, send message, ...)
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}

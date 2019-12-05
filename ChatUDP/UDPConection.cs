using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace ChatUDP
{
    class UDPConn
    {
        
        private int portaQueReceberaOsDados = 0;
        private string ipDestino = null;
        private int portaDestino = 0;
        private UdpClient udpClientServer = null;
        private IPEndPoint grupoQueRecebe = null; 

        public UDPConn( int portaQueReceberaOsDados,int portaQueEnviaOsDados)
        {
            
            this.portaQueReceberaOsDados = portaQueReceberaOsDados;
            udpClientServer = new UdpClient(portaQueEnviaOsDados);
            grupoQueRecebe = new IPEndPoint(IPAddress.Any, portaQueReceberaOsDados);
        }
        
        public void CreateUDPConn(string ipDestino, int portaDestino)
        {
            this.ipDestino = ipDestino;
            this.portaDestino = portaDestino;
        }
        
        public string DataReceive()
        {

            byte[] bytes = udpClientServer.Receive(ref grupoQueRecebe);
            var texto = Encoding.ASCII.GetString(bytes);
            return texto;
        }
        public string SenderIP()
        {
            return grupoQueRecebe.Address.ToString();
        }
        public string SenderDoor()
        {
            return grupoQueRecebe.Port.ToString();
        }
        public void SendText(String texto,int porta)
        {
            IPEndPoint ip =  new IPEndPoint(IPAddress.Parse(ipDestino),porta);
            byte[] sendbuf = Encoding.ASCII.GetBytes(texto);
            udpClientServer.Send(sendbuf,sendbuf.Length,ip);
        }
        public void SendTextToList(List<string> ips,int porta,string texto)
        {
                foreach(string ip in ips){
                    CreateUDPConn(ip,porta);
                    SendText(texto,porta);
                }
        }
        public void Close()
        {
            udpClientServer.Close();
        }

    }
}

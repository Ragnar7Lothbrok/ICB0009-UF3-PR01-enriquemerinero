using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using VehiculoClass;
using CarreteraClass;


namespace NetworkStreamNS
{
    public class NetworkStreamClass
    {
        
        //Método para escribir en un NetworkStream los datos de tipo Carretera
        public static void  EscribirDatosCarreteraNS(NetworkStream NS, Carretera C)
        {            
            byte[] datos = C.Serializar();
            NS.Write(datos, 0, datos.Length);               
        }

        //Metódo para leer de un NetworkStream los datos que de un objeto Carretera
        public static Carretera LeerDatosCarreteraNS (NetworkStream NS)
        {
            byte[] buffer = new byte[4096];
            int bytesLeidos = NS.Read(buffer, 0, buffer.Length);

            byte[] datosLeidos = new byte[bytesLeidos];
            Array.Copy(buffer, datosLeidos, bytesLeidos);

            return Carretera.Deserializar(datosLeidos);
        }

        //Método para enviar datos de tipo Vehiculo en un NetworkStream
        public static void  EscribirDatosVehiculoNS(NetworkStream NS, Vehiculo V)
        {            
            byte[] datos = V.Serializar();
            NS.Write(datos, 0, datos.Length);                      
        }

        //Metódo para leer de un NetworkStream los datos que de un objeto Vehiculo
        public static Vehiculo LeerDatosVehiculoNS (NetworkStream NS)
        {
            byte[] buffer = new byte[1024];
            int bytesLeidos = NS.Read(buffer, 0, buffer.Length);

            byte[] datosLeidos = new byte[bytesLeidos];
            Array.Copy(buffer, datosLeidos, bytesLeidos);

            return Vehiculo.Deserializar(datosLeidos);
        }

        public static string LeerMensajeNetworkStream (NetworkStream NS)
        {
            byte[] bufferLectura = new byte[1024];

            int bytesLeidos = 0;
            var tmpStream = new MemoryStream();
            byte[] bytesTotales; 
            do
            {
                int bytesLectura = NS.Read(bufferLectura,0,bufferLectura.Length);
                tmpStream.Write(bufferLectura, 0, bytesLectura);
                bytesLeidos = bytesLeidos + bytesLectura;
            }while (NS.DataAvailable);

            bytesTotales = tmpStream.ToArray();            

            return Encoding.Unicode.GetString(bytesTotales, 0, bytesLeidos);                 
        }

        public static void  EscribirMensajeNetworkStream(NetworkStream NS, string Str)
        {            
            byte[] MensajeBytes = Encoding.Unicode.GetBytes(Str);
            NS.Write(MensajeBytes,0,MensajeBytes.Length);                        
        }                          

    }
}

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
        // Enviar un objeto Carretera por el stream
        public static void EscribirDatosCarreteraNS(NetworkStream NS, Carretera C)
        {
            byte[] datos = C.CarreteraABytes();
            NS.Write(datos, 0, datos.Length);
        }

        // Leer un objeto Carretera desde el stream
        public static Carretera LeerDatosCarreteraNS(NetworkStream NS)
        {
            byte[] buffer = new byte[4096];
            int bytesLeidos = NS.Read(buffer, 0, buffer.Length);

            byte[] datosLeidos = new byte[bytesLeidos];
            Array.Copy(buffer, datosLeidos, bytesLeidos);

            return Carretera.BytesACarretera(datosLeidos);
        }

        // Enviar un objeto Vehiculo por el stream
        public static void EscribirDatosVehiculoNS(NetworkStream NS, Vehiculo V)
        {
            byte[] datos = V.VehiculoaBytes();
            NS.Write(datos, 0, datos.Length);
        }

        // Leer un objeto Vehiculo desde el stream
        public static Vehiculo LeerDatosVehiculoNS(NetworkStream NS)
        {
            byte[] buffer = new byte[1024];
            int bytesLeidos = NS.Read(buffer, 0, buffer.Length);

            byte[] datosLeidos = new byte[bytesLeidos];
            Array.Copy(buffer, datosLeidos, bytesLeidos);

            return Vehiculo.BytesAVehiculo(datosLeidos);
        }

        // Leer un mensaje string desde el stream
        public static string LeerMensajeNetworkStream(NetworkStream NS)
        {
            byte[] bufferLectura = new byte[1024];
            int bytesLeidos = 0;
            var tmpStream = new MemoryStream();
            byte[] bytesTotales;

            do
            {
                int bytesLectura = NS.Read(bufferLectura, 0, bufferLectura.Length);
                tmpStream.Write(bufferLectura, 0, bytesLectura);
                bytesLeidos += bytesLectura;
            } while (NS.DataAvailable);

            bytesTotales = tmpStream.ToArray();

            return Encoding.Unicode.GetString(bytesTotales, 0, bytesLeidos);
        }

        // Enviar un mensaje string al stream
        public static void EscribirMensajeNetworkStream(NetworkStream NS, string Str)
        {
            byte[] MensajeBytes = Encoding.Unicode.GetBytes(Str);
            NS.Write(MensajeBytes, 0, MensajeBytes.Length);
        }
    }
}

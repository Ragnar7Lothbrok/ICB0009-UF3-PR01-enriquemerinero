using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using VehiculoClass;
using CarreteraClass;

namespace NetworkStreamNS
{
    public class NetworkStreamClass
    {
        // Enviar un objeto Carretera por el stream (con prefijo de longitud)
        public static void EscribirDatosCarreteraNS(NetworkStream NS, Carretera C)
        {
            byte[] datos = C.CarreteraABytes();
            byte[] longitud = BitConverter.GetBytes(datos.Length);
            NS.Write(longitud, 0, longitud.Length);
            NS.Write(datos, 0, datos.Length);
        }

        // Leer un objeto Carretera desde el stream (leyendo primero la longitud)
        public static Carretera LeerDatosCarreteraNS(NetworkStream NS)
        {
            byte[] bufferLongitud = new byte[4];
            NS.Read(bufferLongitud, 0, 4);
            int longitudDatos = BitConverter.ToInt32(bufferLongitud, 0);

            byte[] bufferDatos = new byte[longitudDatos];
            int totalLeidos = 0;

            while (totalLeidos < longitudDatos)
            {
                int leidos = NS.Read(bufferDatos, totalLeidos, longitudDatos - totalLeidos);
                totalLeidos += leidos;
            }

            return Carretera.BytesACarretera(bufferDatos);
        }

        // Enviar un objeto Vehiculo (más sencillo porque es pequeño)
        public static void EscribirDatosVehiculoNS(NetworkStream NS, Vehiculo V)
        {
            byte[] datos = V.VehiculoaBytes();
            byte[] longitud = BitConverter.GetBytes(datos.Length);
            NS.Write(longitud, 0, longitud.Length);
            NS.Write(datos, 0, datos.Length);
        }

        // Leer un objeto Vehiculo
        public static Vehiculo LeerDatosVehiculoNS(NetworkStream NS)
        {
            byte[] bufferLongitud = new byte[4];
            NS.Read(bufferLongitud, 0, 4);
            int longitudDatos = BitConverter.ToInt32(bufferLongitud, 0);

            byte[] bufferDatos = new byte[longitudDatos];
            int totalLeidos = 0;

            while (totalLeidos < longitudDatos)
            {
                int leidos = NS.Read(bufferDatos, totalLeidos, longitudDatos - totalLeidos);
                totalLeidos += leidos;
            }

            return Vehiculo.BytesAVehiculo(bufferDatos);
        }

        // Leer un mensaje string
        public static string LeerMensajeNetworkStream(NetworkStream NS)
        {
            byte[] bufferLectura = new byte[1024];
            int bytesLeidos = NS.Read(bufferLectura, 0, bufferLectura.Length);
            return Encoding.Unicode.GetString(bufferLectura, 0, bytesLeidos);
        }

        // Enviar un mensaje string
        public static void EscribirMensajeNetworkStream(NetworkStream NS, string mensaje)
        {
            byte[] mensajeBytes = Encoding.Unicode.GetBytes(mensaje);
            NS.Write(mensajeBytes, 0, mensajeBytes.Length);
        }
    }
}

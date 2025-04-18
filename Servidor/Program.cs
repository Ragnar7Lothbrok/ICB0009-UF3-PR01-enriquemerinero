using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using NetworkStreamNS;
using CarreteraClass;
using VehiculoClass;

namespace Servidor
{
    class Program
    {
        static int siguienteId = 1;
        static object lockId = new object();
        static Random rng = new Random();

        static void Main(string[] args)
        {
            Console.WriteLine("🚦 Servidor iniciado...");

            TcpListener servidor = new TcpListener(IPAddress.Any, 5000);
            servidor.Start();

            Console.WriteLine("📡 Esperando conexiones en el puerto 5000...");

            while (true)
            {
                TcpClient cliente = servidor.AcceptTcpClient();

                Thread hiloCliente = new Thread(() =>
                {
                    int idAsignado;
                    string direccionAsignada;

                    // Asignar ID único con bloqueo
                    lock (lockId)
                    {
                        idAsignado = siguienteId;
                        siguienteId++;
                    }

                    // Asignar dirección aleatoria
                    direccionAsignada = (rng.Next(2) == 0) ? "Norte" : "Sur";

                    Console.WriteLine($"🛠️ Gestionando nuevo vehículo... ID: {idAsignado}, Dirección: {direccionAsignada}");

                    NetworkStream stream = cliente.GetStream();
                    Console.WriteLine($"📡 Stream de red obtenido para vehículo ID {idAsignado}");

                    // === HANDSHAKE ===
                    string mensajeInicio = NetworkStreamClass.LeerMensajeNetworkStream(stream);
                    Console.WriteLine($"📨 Mensaje recibido del cliente: {mensajeInicio}");

                    if (mensajeInicio == "INICIO")
                    {
                        // Enviar ID al cliente
                        NetworkStreamClass.EscribirMensajeNetworkStream(stream, idAsignado.ToString());
                        Console.WriteLine($"📤 ID enviado al cliente: {idAsignado}");

                        // Esperar confirmación del cliente
                        string confirmacion = NetworkStreamClass.LeerMensajeNetworkStream(stream);
                        Console.WriteLine($"✅ Confirmación de ID recibida: {confirmacion}");

                        if (confirmacion == idAsignado.ToString())
                        {
                            Console.WriteLine($"🔓 Handshake completado correctamente para cliente #{idAsignado}");
                        }
                        else
                        {
                            Console.WriteLine($"❌ ID incorrecto. Se esperaba: {idAsignado}");
                        }
                    }

                    cliente.Close();
                });

                hiloCliente.Start();
            }
        }
    }
}

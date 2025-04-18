using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using NetworkStreamNS;
using CarreteraClass;
using VehiculoClass;

namespace Servidor
{
    class Program
    {
        static int siguienteId = 1;
        static object lockId = new object();
        static object lockLista = new object();
        static Random rng = new Random();

        // Lista para almacenar todos los clientes conectados
        static List<Cliente> listaClientes = new List<Cliente>();

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

                    // Asignar ID único con protección
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

                        // Esperar confirmación
                        string confirmacion = NetworkStreamClass.LeerMensajeNetworkStream(stream);
                        Console.WriteLine($"✅ Confirmación de ID recibida: {confirmacion}");

                        if (confirmacion == idAsignado.ToString())
                        {
                            Console.WriteLine($"🔓 Handshake completado correctamente para cliente #{idAsignado}");

                            // === ETAPA 7: Guardar cliente en lista compartida ===
                            lock (lockLista)
                            {
                                listaClientes.Add(new Cliente(idAsignado, stream));
                                Console.WriteLine($"📦 Cliente añadido a la lista. Total conectados: {listaClientes.Count}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"❌ ID incorrecto. Se esperaba: {idAsignado}");
                        }
                    }

                    // Nota: NO cerramos el cliente aquí, porque su stream se está guardando y puede seguir usándose
                });

                hiloCliente.Start();
            }
        }
    }
}

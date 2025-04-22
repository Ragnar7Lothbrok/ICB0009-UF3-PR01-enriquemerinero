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
        static object lockVehiculos = new object();
        static Random rng = new Random();

        static List<Cliente> listaClientes = new List<Cliente>();
        static Carretera carretera = new Carretera();

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
                    try
                    {
                        int idAsignado;
                        string direccionAsignada;

                        lock (lockId)
                        {
                            idAsignado = siguienteId;
                            siguienteId++;
                        }

                        direccionAsignada = (rng.Next(2) == 0) ? "Norte" : "Sur";
                        Console.WriteLine($"🛠️ Gestionando nuevo vehículo... ID: {idAsignado}, Dirección: {direccionAsignada}");

                        NetworkStream stream = cliente.GetStream();
                        Console.WriteLine($"📡 Stream de red obtenido para vehículo ID {idAsignado}");

                        string mensajeInicio = NetworkStreamClass.LeerMensajeNetworkStream(stream);
                        Console.WriteLine($"📨 Mensaje recibido del cliente: {mensajeInicio}");

                        if (mensajeInicio == "INICIO")
                        {
                            NetworkStreamClass.EscribirMensajeNetworkStream(stream, idAsignado.ToString());
                            Console.WriteLine($"📤 ID enviado al cliente: {idAsignado}");

                            string confirmacion = NetworkStreamClass.LeerMensajeNetworkStream(stream);
                            Console.WriteLine($"✅ Confirmación de ID recibida: {confirmacion}");

                            if (confirmacion == idAsignado.ToString())
                            {
                                Console.WriteLine($"🔓 Handshake completado correctamente para cliente #{idAsignado}");

                                lock (lockLista)
                                {
                                    listaClientes.Add(new Cliente(idAsignado, stream));
                                    Console.WriteLine($"📦 Cliente añadido a la lista. Total conectados: {listaClientes.Count}");
                                }

                                // === Recibir vehículo inicial ===
                                Vehiculo vehiculoRecibido = NetworkStreamClass.LeerDatosVehiculoNS(stream);
                                vehiculoRecibido.Direccion = direccionAsignada;
                                NetworkStreamClass.EscribirDatosVehiculoNS(stream, vehiculoRecibido);

                                lock (lockVehiculos)
                                {
                                    carretera.AñadirVehiculo(vehiculoRecibido);
                                    Console.WriteLine($"🚗 Vehículo añadido a la carretera → ID: {vehiculoRecibido.Id}, Dirección: {vehiculoRecibido.Direccion}, Posición: {vehiculoRecibido.Pos}");
                                    carretera.MostrarBicicletas();
                                }

                                // === Escuchar actualizaciones del vehículo ===
                                try
                                {
                                    while (!vehiculoRecibido.Acabado)
                                    {
                                        vehiculoRecibido = NetworkStreamClass.LeerDatosVehiculoNS(stream);

                                        lock (lockVehiculos)
                                        {
                                            carretera.ActualizarVehiculo(vehiculoRecibido);
                                            carretera.MostrarBicicletas();
                                            EnviarCarreteraATodosLosClientes();
                                        }
                                    }

                                    Console.WriteLine($"🏁 Vehículo #{vehiculoRecibido.Id} ha terminado su recorrido.");

                                    // Esperar mensaje de "FIN"
                                    string mensajeFin = NetworkStreamClass.LeerMensajeNetworkStream(stream);
                                    if (mensajeFin == "FIN")
                                    {
                                        Console.WriteLine($"👋 Cliente #{idAsignado} ha confirmado finalización. Eliminando de la lista.");
                                        lock (lockLista)
                                        {
                                            listaClientes.RemoveAll(c => c.Id == idAsignado);
                                        }
                                    }
                                }
                                catch (IOException ex)
                                {
                                    Console.WriteLine($"⚠️ Cliente #{idAsignado} se desconectó inesperadamente: {ex.Message}");
                                    lock (lockLista)
                                    {
                                        listaClientes.RemoveAll(c => c.Id == idAsignado);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"❌ ID incorrecto. Se esperaba: {idAsignado}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error inesperado en el hilo del cliente: {ex.Message}");
                    }
                });

                hiloCliente.Start();
            }
        }

        static void EnviarCarreteraATodosLosClientes()
        {
            lock (lockLista)
            {
                foreach (Cliente c in listaClientes)
                {
                    try
                    {
                        NetworkStreamClass.EscribirDatosCarreteraNS(c.Stream, carretera);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error al enviar carretera al cliente #{c.Id}: {ex.Message}");
                    }
                }
            }
        }
    }
}

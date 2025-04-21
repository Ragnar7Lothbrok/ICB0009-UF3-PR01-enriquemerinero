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

                            // === EJERCICIO 2: ETAPA 2 ===
                            Vehiculo vehiculoRecibido = NetworkStreamClass.LeerDatosVehiculoNS(stream);
                            vehiculoRecibido.Direccion = direccionAsignada; // asignamos la dirección desde el servidor

                            lock (lockVehiculos)
                            {
                                carretera.AñadirVehiculo(vehiculoRecibido);
                                Console.WriteLine($"🚗 Vehículo añadido a la carretera → ID: {vehiculoRecibido.Id}, Dirección: {vehiculoRecibido.Direccion}, Posición: {vehiculoRecibido.Pos}");
                                carretera.MostrarBicicletas();
                            }
                        }
                        else
                        {
                            Console.WriteLine($"❌ ID incorrecto. Se esperaba: {idAsignado}");
                        }
                    }
                });

                hiloCliente.Start();
            }
        }
    }
}

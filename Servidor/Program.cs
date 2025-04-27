using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using NetworkStreamNS;
using CarreteraClass;
using VehiculoClass;

namespace Servidor
{
    public class ClienteConectado
    {
        public int Id { get; set; }
        public NetworkStream Stream { get; set; }

        public ClienteConectado(int id, NetworkStream stream)
        {
            Id = id;
            Stream = stream;
        }
    }

    class Program
    {
        static int contadorId = 1;
        static List<ClienteConectado> listaClientes = new List<ClienteConectado>();
        static Carretera carreteraGlobal = new Carretera();
        static Vehiculo? vehiculoEnPuente = null;

        static Carretera carreteraAnterior = null; // 🔥 Nueva variable para guardar la última carretera impresa

        static readonly object lockId = new object();
        static readonly object lockLista = new object();
        static readonly object lockCarretera = new object();
        static readonly object lockPuente = new object();

        static void Main(string[] args)
        {
            int puerto = 13000;
            TcpListener servidor = new TcpListener(IPAddress.Any, puerto);
            servidor.Start();
            Console.WriteLine("Servidor iniciado. Esperando conexiones de clientes...");

            while (true)
            {
                TcpClient clienteConectado = servidor.AcceptTcpClient();
                Console.WriteLine("Cliente conectado.");

                Thread hiloCliente = new Thread(() => GestionarCliente(clienteConectado));
                hiloCliente.Start();
            }
        }

        static void EnviarCarreteraATodos(Carretera carretera)
        {
            lock (lockLista)
            {
                List<ClienteConectado> clientesDesconectados = new List<ClienteConectado>();

                foreach (var cliente in listaClientes)
                {
                    try
                    {
                        NetworkStreamClass.EscribirDatosCarreteraNS(cliente.Stream, carretera);
                    }
                    catch (IOException)
                    {
                        Console.WriteLine($"[Servidor] Error al enviar carretera al cliente con ID: {cliente.Id}. Lo marcaré como desconectado.");
                        clientesDesconectados.Add(cliente);
                    }
                    catch (ObjectDisposedException)
                    {
                        Console.WriteLine($"[Servidor] El stream del cliente {cliente.Id} ya estaba cerrado. Eliminando.");
                        clientesDesconectados.Add(cliente);
                    }
                }

                foreach (var desconectado in clientesDesconectados)
                {
                    listaClientes.Remove(desconectado);
                }

                if (clientesDesconectados.Count > 0)
                {
                    Console.WriteLine($"[Servidor] {clientesDesconectados.Count} cliente(s) eliminado(s) de la lista.");
                }
            }
        }

        static void GestionarCliente(TcpClient cliente)
        {
            NetworkStream stream = cliente.GetStream();
            Console.WriteLine($"[Hilo {Thread.CurrentThread.ManagedThreadId}] NetworkStream obtenido correctamente.");

            string mensajeInicio = NetworkStreamClass.LeerMensajeNetworkStream(stream);
            if (mensajeInicio != "INICIO")
            {
                Console.WriteLine($"[Hilo {Thread.CurrentThread.ManagedThreadId}] Error: no se recibió 'INICIO'.");
                cliente.Close();
                return;
            }

            Console.WriteLine($"[Hilo {Thread.CurrentThread.ManagedThreadId}] Mensaje 'INICIO' recibido correctamente.");

            int idAsignado;
            string direccion;
            lock (lockId)
            {
                idAsignado = contadorId;
                contadorId++;
            }

            direccion = new Random().Next(2) == 0 ? "Norte" : "Sur";

            Console.WriteLine($"[Hilo {Thread.CurrentThread.ManagedThreadId}] ID asignado: {idAsignado} - Dirección: {direccion}");

            lock (lockLista)
            {
                listaClientes.Add(new ClienteConectado(idAsignado, stream));
                Console.WriteLine($"[Hilo {Thread.CurrentThread.ManagedThreadId}] Cliente añadido a la lista. Total conectados: {listaClientes.Count}");
            }

            NetworkStreamClass.EscribirMensajeNetworkStream(stream, idAsignado.ToString());
            Console.WriteLine($"[Hilo {Thread.CurrentThread.ManagedThreadId}] ID enviado al cliente.");

            string confirmacion = NetworkStreamClass.LeerMensajeNetworkStream(stream);
            if (confirmacion == idAsignado.ToString())
            {
                Console.WriteLine($"[Hilo {Thread.CurrentThread.ManagedThreadId}] Confirmación recibida del cliente con ID: {confirmacion}");
            }
            else
            {
                Console.WriteLine($"[Hilo {Thread.CurrentThread.ManagedThreadId}] Error: el cliente ha confirmado un ID incorrecto.");
                cliente.Close();
                return;
            }

            while (true)
            {
                try
                {
                    Vehiculo vehiculoRecibido = NetworkStreamClass.LeerDatosVehiculoNS(stream);

                    lock (lockPuente)
                    {
                        if ((vehiculoRecibido.Direccion == "Norte" && vehiculoRecibido.Pos == 30) ||
                            (vehiculoRecibido.Direccion == "Sur" && vehiculoRecibido.Pos == 50))
                        {
                            if (vehiculoEnPuente == null)
                            {
                                vehiculoEnPuente = vehiculoRecibido;
                                Console.WriteLine($"[Servidor] Vehículo {vehiculoRecibido.Id} está cruzando el puente.");
                            }
                            else if (vehiculoEnPuente.Id != vehiculoRecibido.Id)
                            {
                                Console.WriteLine($"[Servidor] Vehículo {vehiculoRecibido.Id} esperando: puente ocupado por {vehiculoEnPuente.Id}.");
                                continue;
                            }
                        }

                        if ((vehiculoRecibido.Direccion == "Norte" && vehiculoRecibido.Pos == 50) ||
                            (vehiculoRecibido.Direccion == "Sur" && vehiculoRecibido.Pos == 30))
                        {
                            if (vehiculoEnPuente != null && vehiculoEnPuente.Id == vehiculoRecibido.Id)
                            {
                                vehiculoEnPuente = null;
                                Console.WriteLine($"[Servidor] Vehículo {vehiculoRecibido.Id} ha salido del puente.");
                            }
                        }
                    }

                    lock (lockCarretera)
                    {
                        carreteraGlobal.ActualizarVehiculo(vehiculoRecibido);

                        // 🔥 Solo mostramos si hay cambio en la carretera
                        bool cambio = false;
                        if (carreteraAnterior == null || carreteraAnterior.VehiculosEnCarretera.Count != carreteraGlobal.VehiculosEnCarretera.Count)
                        {
                            cambio = true;
                        }
                        else
                        {
                            for (int i = 0; i < carreteraGlobal.VehiculosEnCarretera.Count; i++)
                            {
                                if (carreteraAnterior.VehiculosEnCarretera[i].Id != carreteraGlobal.VehiculosEnCarretera[i].Id ||
                                    carreteraAnterior.VehiculosEnCarretera[i].Pos != carreteraGlobal.VehiculosEnCarretera[i].Pos)
                                {
                                    cambio = true;
                                    break;
                                }
                            }
                        }

                        if (cambio)
                        {
                            MostrarEstadoCarretera();

                            carreteraAnterior = new Carretera
                            {
                                VehiculosEnCarretera = carreteraGlobal.VehiculosEnCarretera.Select(v => new Vehiculo
                                {
                                    Id = v.Id,
                                    Pos = v.Pos,
                                    Direccion = v.Direccion,
                                    Velocidad = v.Velocidad,
                                    Acabado = v.Acabado,
                                    Parado = v.Parado
                                }).ToList(),
                                NumVehiculosEnCarrera = carreteraGlobal.NumVehiculosEnCarrera
                            };
                        }
                    }

                    EnviarCarreteraATodos(carreteraGlobal);

                    if ((vehiculoRecibido.Direccion == "Norte" && vehiculoRecibido.Pos >= 100) ||
                        (vehiculoRecibido.Direccion == "Sur" && vehiculoRecibido.Pos <= 0))
                    {
                        Console.WriteLine($"[Hilo {Thread.CurrentThread.ManagedThreadId}] El vehículo {vehiculoRecibido.Id} ha llegado a destino.");
                        break;
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine($"[Hilo {Thread.CurrentThread.ManagedThreadId}] Error: el cliente se ha desconectado inesperadamente.");
                    break;
                }
            }

            cliente.Close();
        }

        static void MostrarEstadoCarretera()
        {
            lock (lockCarretera)
            {
                Console.WriteLine("\n🌍 Estado actual de la carretera:");
                foreach (var v in carreteraGlobal.VehiculosEnCarretera.OrderBy(v => v.Pos))
                {
                    Console.WriteLine($"- Vehículo #{v.Id} ({v.Direccion}) → {v.Pos} km");
                }
                Console.WriteLine();
            }
        }
    }
}

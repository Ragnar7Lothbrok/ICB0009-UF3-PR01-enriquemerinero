using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using VehiculoClass;
using CarreteraClass;
using NetworkStreamNS;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("🚗 Cliente iniciando conexión...");

                TcpClient cliente = new TcpClient("127.0.0.1", 5000);
                Console.WriteLine("✅ Conectado al servidor.");

                NetworkStream stream = cliente.GetStream();
                Console.WriteLine("📡 Stream de red obtenido en el cliente.");

                // === HANDSHAKE ===
                NetworkStreamClass.EscribirMensajeNetworkStream(stream, "INICIO");
                Console.WriteLine("📤 Mensaje 'INICIO' enviado al servidor.");

                string idRecibido = NetworkStreamClass.LeerMensajeNetworkStream(stream);
                Console.WriteLine($"📨 ID recibido del servidor: {idRecibido}");

                NetworkStreamClass.EscribirMensajeNetworkStream(stream, idRecibido);
                Console.WriteLine("📤 Confirmación enviada al servidor.");

                // === Crear vehículo ===
                int id = int.Parse(idRecibido);
                Random rnd = new Random();
                Vehiculo nuevoVehiculo = new Vehiculo
                {
                    Id = id,
                    Pos = 0,
                    Velocidad = rnd.Next(100, 501),
                    Acabado = false,
                    Direccion = "", // Se recibirá del servidor
                    Parado = false
                };

                NetworkStreamClass.EscribirDatosVehiculoNS(stream, nuevoVehiculo);
                Console.WriteLine($"🚗 Vehículo enviado al servidor → ID: {nuevoVehiculo.Id}, Velocidad: {nuevoVehiculo.Velocidad}ms");

                // === Recibir el vehículo con dirección ===
                nuevoVehiculo = NetworkStreamClass.LeerDatosVehiculoNS(stream);
                Console.WriteLine($"📩 Dirección asignada: {nuevoVehiculo.Direccion}");

                // === Hilo de escucha de carretera ===
                Thread hiloEscucha = new Thread(() =>
                {
                    try
                    {
                        while (true)
                        {
                            Carretera carreteraRecibida = NetworkStreamClass.LeerDatosCarreteraNS(stream);
                            Console.WriteLine("📡 Estado actual de la carretera recibido del servidor:");

                            foreach (Vehiculo v in carreteraRecibida.VehiculosEnCarretera)
                            {
                                string estado = v.Acabado ? "✅ Acabado" : $"📍 Pos: {v.Pos}";
                                Console.WriteLine($"🚗 Vehículo #{v.Id} ({v.Direccion}) → {estado}");
                            }

                            Console.WriteLine();
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        Console.WriteLine("ℹ️ Hilo de recepción interrumpido correctamente.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error al recibir datos del servidor: {ex.Message}");
                    }
                });

                hiloEscucha.Start();

                // === Movimiento del vehículo según dirección ===
                if (nuevoVehiculo.Direccion == "Norte")
                {
                    for (int i = 1; i <= 100; i++)
                    {
                        Thread.Sleep(nuevoVehiculo.Velocidad);
                        nuevoVehiculo.Pos = i;
                        if (i == 100) nuevoVehiculo.Acabado = true;

                        Console.WriteLine($"🏁 Enviado → ID: {nuevoVehiculo.Id}, Posición: {nuevoVehiculo.Pos}, Velocidad: {nuevoVehiculo.Velocidad}ms");
                        NetworkStreamClass.EscribirDatosVehiculoNS(stream, nuevoVehiculo);
                    }
                }
                else // Sur
                {
                    for (int i = 100; i >= 0; i--)
                    {
                        Thread.Sleep(nuevoVehiculo.Velocidad);
                        nuevoVehiculo.Pos = i;
                        if (i == 0) nuevoVehiculo.Acabado = true;

                        Console.WriteLine($"🏁 Enviado → ID: {nuevoVehiculo.Id}, Posición: {nuevoVehiculo.Pos}, Velocidad: {nuevoVehiculo.Velocidad}ms");
                        NetworkStreamClass.EscribirDatosVehiculoNS(stream, nuevoVehiculo);
                    }
                }

                // === Avisar fin y cerrar conexión ===
                NetworkStreamClass.EscribirMensajeNetworkStream(stream, "FIN");
                hiloEscucha.Interrupt();
                cliente.Close();
                Console.WriteLine("🔌 Conexión cerrada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error de conexión: {ex.Message}");
            }
        }
    }
}

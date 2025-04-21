using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using NetworkStreamNS;
using CarreteraClass;
using VehiculoClass;

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

                // === EJERCICIO 2: ETAPA 2 ===
                // Crear objeto Vehiculo
                int id = int.Parse(idRecibido);
                Random rnd = new Random();

                Vehiculo nuevoVehiculo = new Vehiculo
                {
                    Id = id,
                    Pos = 0,
                    Velocidad = rnd.Next(100, 501),
                    Acabado = false,
                    Direccion = "", // el servidor de momento la conoce
                    Parado = false
                };

                // Enviar objeto Vehiculo al servidor
                NetworkStreamClass.EscribirDatosVehiculoNS(stream, nuevoVehiculo);
                Console.WriteLine($"📤 Vehículo enviado al servidor → ID: {nuevoVehiculo.Id}, Velocidad: {nuevoVehiculo.Velocidad}ms");

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

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
        static bool escuchando = true;

        static void Main(string[] args)
        {
            TcpClient cliente = new TcpClient();

            try
            {
                cliente.Connect("127.0.0.1", 13000);
                Console.WriteLine("Conectado al servidor correctamente.");

                NetworkStream stream = cliente.GetStream();

                NetworkStreamClass.EscribirMensajeNetworkStream(stream, "INICIO");
                Console.WriteLine("Mensaje 'INICIO' enviado al servidor.");

                string respuestaServidor = NetworkStreamClass.LeerMensajeNetworkStream(stream);
                int id = int.Parse(respuestaServidor);
                Console.WriteLine($"ID recibido desde el servidor: {id}");

                NetworkStreamClass.EscribirMensajeNetworkStream(stream, id.ToString());
                Console.WriteLine("ID confirmado al servidor.");

                string direccion = new Random().Next(2) == 0 ? "Norte" : "Sur";

                Vehiculo v = new Vehiculo();
                v.Id = id;
                v.Direccion = direccion;
                v.Pos = v.Direccion == "Norte" ? 0 : 100;

                Console.WriteLine($"→ Dirección asignada: {v.Direccion}");
                Console.WriteLine($"→ Velocidad del vehículo: {v.Velocidad} ms entre cada paso.");

                Carretera carreteraAnterior = null;

                // 🔥 HILO DE ESCUCHA DE CARRETERA
                Thread hiloEscuchaCarretera = new Thread(() =>
                {
                    while (escuchando)
                    {
                        try
                        {
                            Carretera carreteraRecibida = NetworkStreamClass.LeerDatosCarreteraNS(stream);

                            bool cambio = false;
                            if (carreteraAnterior == null || carreteraRecibida.VehiculosEnCarretera.Count != carreteraAnterior.VehiculosEnCarretera.Count)
                            {
                                cambio = true;
                            }
                            else
                            {
                                for (int i = 0; i < carreteraRecibida.VehiculosEnCarretera.Count; i++)
                                {
                                    if (carreteraAnterior.VehiculosEnCarretera[i].Id != carreteraRecibida.VehiculosEnCarretera[i].Id ||
                                        carreteraAnterior.VehiculosEnCarretera[i].Pos != carreteraRecibida.VehiculosEnCarretera[i].Pos)
                                    {
                                        cambio = true;
                                        break;
                                    }
                                }
                            }

                            if (cambio)
                            {
                                Console.WriteLine("[Actualización desde servidor] Vehículos en carretera:");
                                foreach (Vehiculo veh in carreteraRecibida.VehiculosEnCarretera)
                                {
                                    Console.WriteLine($"  → ID: {veh.Id} | Dirección: {veh.Direccion} | Posición: {veh.Pos} km");
                                }
                            }

                            carreteraAnterior = carreteraRecibida;
                        }
                        catch
                        {
                            break; // Si el servidor cierra, salimos del hilo
                        }
                    }
                });

                hiloEscuchaCarretera.Start();

                // 🔥 HILO PRINCIPAL: MOVIMIENTO DEL VEHÍCULO
                while (!v.Acabado)
                {
                    if ((v.Direccion == "Norte" && v.Pos >= 30 && v.Pos <= 50) || 
                        (v.Direccion == "Sur" && v.Pos >= 30 && v.Pos <= 50))
                    {
                        Console.WriteLine($"→ Intentando entrar al puente...");

                        bool puedeCruzar = false;

                        while (!puedeCruzar)
                        {
                            NetworkStreamClass.EscribirDatosVehiculoNS(stream, v);
                            Thread.Sleep(500);

                            if (carreteraAnterior != null)
                            {
                                Vehiculo yo = carreteraAnterior.VehiculosEnCarretera.Find(veh => veh.Id == v.Id);

                                if (yo != null)
                                {
                                    v.Pos = yo.Pos;

                                    // 🔥 NUEVO: Salir del bucle si ya he salido del puente
                                    if (!(v.Pos >= 30 && v.Pos <= 50))
                                    {
                                        puedeCruzar = true;
                                        break;
                                    }

                                    if ((v.Direccion == "Norte" && v.Pos >= 30 && v.Pos <= 50) ||
                                        (v.Direccion == "Sur" && v.Pos >= 30 && v.Pos <= 50))
                                    {
                                        puedeCruzar = true;
                                        Console.WriteLine("→ Acceso permitido: cruzando el puente.");

                                        // 🔥 Avanzar para salir del punto de entrada
                                        if (v.Direccion == "Norte")
                                        {
                                            v.Pos += 1;
                                        }
                                        else
                                        {
                                            v.Pos -= 1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // No estamos en el puente: movemos normal
                        NetworkStreamClass.EscribirDatosVehiculoNS(stream, v);

                        if (!v.Acabado)
                        {
                            if (v.Direccion == "Norte")
                            {
                                v.Pos += 1;
                                if (v.Pos >= 100)
                                {
                                    v.Pos = 100;
                                    v.Acabado = true;
                                }
                            }
                            else // Sur
                            {
                                v.Pos -= 1;
                                if (v.Pos <= 0)
                                {
                                    v.Pos = 0;
                                    v.Acabado = true;
                                }
                            }
                        }

                        Thread.Sleep(v.Velocidad);
                    }
                }

                escuchando = false; // 🔥 Parar el hilo de escucha
                Console.WriteLine("✓ Vehículo ha llegado a destino. Fin de la simulación.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

            Console.WriteLine("Pulsa ENTER para cerrar el cliente.");
            Console.ReadLine();
            cliente.Close();
        }
    }
}

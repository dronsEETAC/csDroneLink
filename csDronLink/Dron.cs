using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MAVLink;

using System.Threading;
using System.Collections.Concurrent;
using System.Net.Sockets;

// MIRAR ESTO. SI LO QUITO NO RECONOCE LA CLASE WaitingRequest
using static csDronLink.MessageHandler;
using System.IO;
using System.Data.Entity.Core.Metadata.Edm;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Data;
using GMap.NET.MapProviders;



namespace csDronLink
{

    public partial class Dron
    {
        // Atributos
        byte id; // identificador del dron (un número a partir del 1)
        // El identificador del dron es necesario cuando se trabaja con un enjambre de drones
        // simulado

        MAVLink.MavlinkParse mavlink = new MAVLink.MavlinkParse();
        // El modo puede ser "simulacion" o "produccion"
        string modo;

        // Para la comunicación en producción
        SerialPort puertoSerie = new SerialPort();
        // Para la comunicación en simulación
        NetworkStream puertoTCP;

        // Ultimo comando de navegación, que usará el bucle de navegación para recordarle
        // al dron hacia dónde debe navegar
        byte[] navPacket;


        // Los datos de telemetría que me interesan 
        float relative_alt;
        float lat;
        float lon;
        float heading;


        // Aquí guardaré la referencia a la función que tengo que ejecutar
        // si el cliente me pide que le envíe los datos de telemetría.
        Action<byte, List<(string nombre, float valor)>> ProcesarTelemetria = null;

        // Cuando reciba un comando de navegación debo poner en marcha el 
        // bucle de navegación para que recuerde al autopiloto que mantenga
        // el rumbo. Con esta variable controlamos el bucle de navagación
        Boolean navegando = false;

        // Velocidad para las operaciones de navegación
        int velocidad = 1;

        MessageHandler messageHandler;

        // Constrictor, conexión, registro de telemetria y envio de mensajes
        public Dron(byte id = 1)
        {
            // Al crear la dron se establece su identificador (1 por defecto)
            this.id = id;
        }
      
        private void EnviarMensaje (byte[] packet)
        {
            if (modo == "produccion")
                puertoSerie.Write(packet, 0, packet.Length);
            else
                puertoTCP.Write(packet, 0, packet.Length);
        }
        private void RegistrarTelemetria (MAVLinkMessage msg)
        {   
            // Cada vez que se recibe un mensaje con datos de telemetria me los guardo
            // De momento solo me interesan la altitud, latitud, longitud y heading, pero se pueden
            // recoger muchos otros (nivel de bateria, etc.)
            MAVLink.mavlink_global_position_int_t position = (MAVLink.mavlink_global_position_int_t)msg.data;
            this.relative_alt = position.relative_alt / 1000.0f; // Convertir mm a metros
            this.lat = position.lat;
            this.lon = position.lon;
            this.heading = position.hdg;
            // Si me han pedido que envíe los datos de telemetría al cliente lo hago
            if (this.ProcesarTelemetria != null)
            {
                List<(string nombre, float valor)> telemetria = new List<(string nombre, float valor)>();
                telemetria.Add(("Alt",this.relative_alt));
                telemetria.Add(("Lat", this.lat));
                telemetria.Add(("Lon",this.lon));
                telemetria.Add(("Heading",this.heading));
                // Los envío a la función que me indicó el cliente
                // Envio también el identificador del dron
                this.ProcesarTelemetria(this.id, telemetria);
            }
        }
        public void Conectar(string modo, string conector = null)
        {
            this.modo = modo;
            if (modo == "produccion")
            {
                // Configuro el puerto serie
                puertoSerie.PortName = conector;
                puertoSerie.BaudRate = 57600;
                puertoSerie.Open();
                // Pongo en marcha en message handler
                messageHandler = new MessageHandler(modo, puertoSerie);
            } else
            {
                // Configuro la conexión con el simulador
                string ip = "127.0.0.1";
                // El puerto depende del identificador:
                // 1 -> 5763
                // 2 -> 5773
                // id -> 5763 + (id-1)*10
                int port = 5763 + (this.id - 1) * 10;
                TcpClient client = new TcpClient(ip, port);
                puertoTCP = client.GetStream();
                messageHandler = new MessageHandler(modo, puertoTCP);

            }


            // Hago una petición asíncrona al handler para que me envíe todos los mensajes
            // del tipo indicado, que son los que contienen los datos de telemetria que me interesan
            // Le indico que cuando llegue un mensaje de ese tipo ejecute la función RegistrarTelemetria
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            messageHandler.RegisterHandler(msgType, RegistrarTelemetria);
            
            // Ahora le pido al autopiloto que me envíe mensajes del tipo indicado (los que contienen
            // los datos de telemetría, cada 2 segundos)
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL,
                param1 = (float)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT, // ID del mensaje que queremos recibir
                param2 = 200000, // Intervalo en microsegundos (1 Hz = 1,000,000 µs)
            };

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            EnviarMensaje(packet);
            Console.WriteLine("Enviado el mensaje de conexion");
        }


    }
}
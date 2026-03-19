using GMap.NET.MapProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Metadata.Edm;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// MIRAR ESTO. SI LO QUITO NO RECONOCE LA CLASE WaitingRequest
using static csDronLink.MessageHandler;
using static MAVLink;



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
        float mode;
        //0:    "STABILIZE";
        //3:    "AUTO";
        //4:    "GUIDED";
        //5:    "LOITER";
        //6:    "RTL";
        //9:    "LAND";

        float relative_alt;
        float lat;
        float lon;
        float heading;
        float x;
        float y;

        float voltage;
        float current;
        int remaining;

        float rollDeg;
        float pitchDeg;

        int freq; // frecuencia a la que se enviará la telemetría


            // Si me han pedido que envíe los datos de bateria al cliente lo hago


        // Aquí guardaré la referencia a la función que tengo que ejecutar
        // si el cliente me pide que le envíe los datos de telemetría.
        Action<byte, List<(string nombre, float valor)>> ProcesarTelemetria = null;
        Action<byte, List<(string nombre, float valor)>> ProcesarTelemetriaLocal = null;
        Action<byte, List<(string nombre, float valor)>> ProcesarNivelBateria = null;

        // Cuando reciba un comando de navegación debo poner en marcha el 
        // bucle de navegación para que recuerde al autopiloto que mantenga
        // el rumbo. Con esta variable controlamos el bucle de navagación
        Boolean navegando = false; 

        // Velocidad para las operaciones de navegación
        int velocidad = 1;

        // Para abortar una mision
        Boolean abortarMision;

        Thread t;
        MessageHandler messageHandler;
        Action brench; //Para avisar en caso de violacion

        // Constrictor, conexión, registro de telemetria y envio de mensajes
        public Dron(byte id = 1)
        {
            // Al crear la dron se establece su identificador (1 por defecto)
            this.id = id;
        }
        public byte GetId ()
        {
            return this.id;
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
                telemetria.Add(("Mode", this.mode));
                telemetria.Add(("RollDeg", this.rollDeg));
                telemetria.Add(("PitchDeg", this.pitchDeg));

                // Los envío a la función que me indicó el cliente
                // Envio también el identificador del dron
                this.ProcesarTelemetria(this.id, telemetria);
            }
        }
        private void RegistrarTelemetriaLocal(MAVLinkMessage msg)
        {
            // Cada vez que se recibe un mensaje con datos de telemetria me los guardo
            // De momento solo me interesan la altitud, latitud, longitud y heading, pero se pueden
            // recoger muchos otros (nivel de bateria, etc.)
            MAVLink.mavlink_local_position_ned_t position = (MAVLink.mavlink_local_position_ned_t)msg.data;
         
            this.x = position.x;
            this.y = position.y;
            // Si me han pedido que envíe los datos de telemetría al cliente lo hago
            if (this.ProcesarTelemetriaLocal != null)
            {
                List<(string nombre, float valor)> telemetria = new List<(string nombre, float valor)>();
                telemetria.Add(("x", this.x));
                telemetria.Add(("y", this.y));
                // Los envío a la función que me indicó el cliente
                // Envio también el identificador del dron
                this.ProcesarTelemetriaLocal(this.id, telemetria);
            }
        }
        private void RegistrarNivelBateria(MAVLinkMessage msg)
        {
            // Cada vez que se recibe un mensaje con datos de telemetria me los guardo
            // Este mensaje trae datos del nivel de bateria
            MAVLink.mavlink_sys_status_t sysStatus = (MAVLink.mavlink_sys_status_t)msg.data;


            this.voltage = sysStatus.voltage_battery / 1000.0f;   // mV → V
            this.current = sysStatus.current_battery / 100.0f;    // cA → A
            this.remaining = sysStatus.battery_remaining;           // %
     
       
            // Si me han pedido que envíe los datos de bateria al cliente lo hago
            if (this.ProcesarNivelBateria != null)
            {
                List<(string nombre, float valor)> telemetria = new List<(string nombre, float valor)>();
                telemetria.Add(("voltage", this.voltage));
                telemetria.Add(("current", this.current));
                telemetria.Add(("remaining", this.remaining));
                // Los envío a la función que me indicó el cliente
                // Envio también el identificador del dron
                this.ProcesarNivelBateria(this.id, telemetria);
            }
        }

        private void RegistrarAttitude(MAVLinkMessage msg)
        {
            // 
            // Este mensaje trae estos datos:
            //uint32_t time_boot_ms
            //float roll
            //float pitch
            //float yaw
            //float rollspeed
            //float pitchspeed
            //float yawspeed

            MAVLink.mavlink_attitude_t attitude = (MAVLink.mavlink_attitude_t)msg.data;

            // Vienen en radianes
            float rollRad = attitude.roll;
            float pitchRad = attitude.pitch;

          
            // Convertir a grados
            this.rollDeg = rollRad * (180 / (float) Math.PI);
            this.pitchDeg = pitchRad * (180 / (float)Math.PI);
        }

        private void TratarHeartbeat(MAVLinkMessage msg)
        {
            // Cada vez que se recibe un mensaje con datos de telemetria me los guardo
            // Este mensaje trae datos del modo de vuelo
            MAVLink.mavlink_heartbeat_t beat = (MAVLink.mavlink_heartbeat_t)msg.data;
            this.mode = (float) beat.custom_mode;
        }

        public void Conectar(string modo, string conector = null, int freq = 4)
        {
            this.freq = freq;
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
            // del tipo indicado, que son los que contienen los datos me interesan
            // Le indico que cuando llegue un mensaje de ese tipo ejecute la función TratarHeartbeat
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.HEARTBEAT).ToString();
            messageHandler.RegisterHandler(msgType, TratarHeartbeat);


            // Hago una petición asíncrona al handler para que me envíe todos los mensajes
            // del tipo indicado, que son los que contienen los datos de telemetria que me interesan
            // Le indico que cuando llegue un mensaje de ese tipo ejecute la función RegistrarTelemetria
            msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            messageHandler.RegisterHandler(msgType, RegistrarTelemetria);

            int periodo = (1000000 / this.freq);
            // Ahora le pido al autopiloto que me envíe mensajes del tipo indicado (los que contienen
            // los datos de telemetría, cada 2 segundos)
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL,
                param1 = (float)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT, // ID del mensaje que queremos recibir
                param2 =periodo, // Intervalo en microsegundos (1 Hz = 1,000,000 µs)
            };

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            EnviarMensaje(packet);

            // Hago una petición asíncrona al handler para que me envíe todos los mensajes
            // del tipo indicado, que son los que contienen los datos de telemetria LOCAL que me interesan
            // Le indico que cuando llegue un mensaje de ese tipo ejecute la función RegistrarTelemetriaLocal
            msgType = ((int)MAVLink.MAVLINK_MSG_ID.LOCAL_POSITION_NED).ToString();
            messageHandler.RegisterHandler(msgType, RegistrarTelemetriaLocal);

            // Ahora le pido al autopiloto que me envíe mensajes del tipo indicado (los que contienen
            // los datos de telemetría, cada 2 segundos)
            req = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL,
                param1 = (float)MAVLink.MAVLINK_MSG_ID.LOCAL_POSITION_NED, // ID del mensaje que queremos recibir
                param2 = periodo, // Intervalo en microsegundos (1 Hz = 1,000,000 µs)
            };

            packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            EnviarMensaje(packet);


            // Hago una petición asíncrona al handler para que me envíe todos los mensajes
            // del tipo indicado, que son los que contienen los datos de bateria que me interesan
            // Le indico que cuando llegue un mensaje de ese tipo ejecute la función RegistrarNivelBateria 'SYS_STATUS'
            msgType = ((int)MAVLink.MAVLINK_MSG_ID.SYS_STATUS).ToString();
            messageHandler.RegisterHandler(msgType, RegistrarNivelBateria);

            // Ahora le pido al autopiloto que me envíe mensajes del tipo indicado (los que contienen
            // los datos de telemetría, cada 2 segundos)
            req = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL,
                param1 = (float)MAVLink.MAVLINK_MSG_ID.SYS_STATUS, // ID del mensaje que queremos recibir
                param2 = periodo, // Intervalo en microsegundos (1 Hz = 1,000,000 µs)
            };

            packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            EnviarMensaje(packet);


            // Esto es para obtener el angulo de pith y de roll del dron
            msgType = ((int)MAVLink.MAVLINK_MSG_ID.ATTITUDE).ToString();
            messageHandler.RegisterHandler(msgType, RegistrarAttitude);

            // Ahora le pido al autopiloto que me envíe mensajes del tipo indicado (los que contienen
            // los datos de telemetría, cada 2 segundos)
            req = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.SET_MESSAGE_INTERVAL,
                param1 = (float)MAVLink.MAVLINK_MSG_ID.ATTITUDE, // ID del mensaje que queremos recibir
                param2 = periodo, // Intervalo en microsegundos (1 Hz = 1,000,000 µs)
            };

            packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            EnviarMensaje(packet);


            Console.WriteLine("Enviado el mensaje de conexion");
        }


    }
}
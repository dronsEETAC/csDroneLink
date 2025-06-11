using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace csDronLink
{
    public partial class Dron
    {
        // Despegue, aterrizaje y RTL
        private void _Despegar(int altitud, Action<byte, object> f = null, object param = null)
        {
            // Primero ponemos el dron en modo GUIDED
            PonModoGuiado();

            // Despues armamos armamos
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t();
            req.target_system = this.id;
            req.target_component = 1;
            req.command = (ushort)MAVLink.MAV_CMD.COMPONENT_ARM_DISARM;
            req.param1 = 1;
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            EnviarMensaje(packet);

            // Y ahora despegamos
            req = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.TAKEOFF,
                param7 = altitud // Altura deseada en metros
            };

            // Generar paquete MAVLink
            packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            // Enviar comando al dron
            EnviarMensaje(packet);

            // Aqui espero el mensaje que indique que ya ha alcanzado la altura de despeque
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            MAVLink.MAVLinkMessage message = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                condition: ComprobarEnAire,
                parameters: altitud,
                timeout: -1
            );
            if (f != null)
                f(this.id, param);

        }
        public void Despegar(int altitud, Boolean bloquear = true, Action<byte,object> f = null, object param = null)
        {
            // Si la llamada es bloqueante llamo a la función 
            if (bloquear)
            {
                this._Despegar(altitud);
            }
            // Si no es bloqueante pongo en marcha un thread para que se ocupe y no bloqueo al cliente
            else
            {
                Thread t = new Thread(() => _Despegar(altitud, f, param));
                t.Start();
            }

        }
        public void _RTL(Action<byte, object> f = null, object param = null)
        {
            // paro el bucle de navegación si está activo
            this.navegando = false;

            MAVLink.mavlink_set_mode_t setMode = new MAVLink.mavlink_set_mode_t
            {
                target_system = this.id,
                base_mode = (byte)MAVLink.MAV_MODE_FLAG.CUSTOM_MODE_ENABLED,
                custom_mode = 6 // Modo RTL en ArduPilot
            };

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.SET_MODE, setMode);
            // Enviar comando al dron
            //serialPort1.Write(packet, 0, packet.Length);
            EnviarMensaje(packet);

            // Pedimos un mensaje de telemetria que indique que ya está en tierra
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            var mensaje = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                condition: ComprobarEnTierra,
                timeout: -1
            );

            if (f != null)
                f(this.id, param);

        }
        public void _Aterrizar(Action<byte, object> f = null, object param = null)
        {
            // paro el bucle de navegación si está activo
            this.navegando = false;

            // Crear el paquete para el comando de aterrizaje
            MAVLink.mavlink_command_long_t req = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,  // ID del sistema (1 es el sistema principal)
                target_component = 1,  // ID del componente (1 es el autopiloto)
                command = (ushort)MAVLink.MAV_CMD.LAND,  // Comando de aterrizaje
                param1 = 0,  // No se usa (0 es valor por defecto)
                param2 = 0,  // No se usa (0 es valor por defecto)
                param3 = 0,  // No se usa (0 es valor por defecto)
                param4 = 0,  // No se usa (0 es valor por defecto)
                param5 = 0,  // No se usa (0 es valor por defecto)
                param6 = 0,  // No se usa (0 es valor por defecto)
                param7 = 0   // No se usa (0 es valor por defecto)
            };

            // Generar el paquete MAVLink para el comando de aterrizaje
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);
            // Enviar comando al dron
            //serialPort1.Write(packet, 0, packet.Length);
            EnviarMensaje(packet);

            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            var mensaje = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                condition: ComprobarEnTierra,
                 timeout: -1
            );


            if (f != null)
                f(this.id,param);

        }
        public void RTL(Boolean bloquear = true, Action<byte,object> f = null, object param = null)
        {
            if (bloquear)
            {
                this._RTL();
            }
            else
            {
                Thread t = new Thread(() => _RTL(f, param));
                t.Start();
            }
        }
        public void Aterrizar(Boolean bloquear = true, Action<byte,object> f = null, object param = null)
        {
            if (bloquear)
            {
                this._Aterrizar();
            }
            else
            {
                Thread t = new Thread(() => _Aterrizar(f, param));
                t.Start();
            }
        }

    }
}

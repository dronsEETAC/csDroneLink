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
        // Miscelanea
        public void PonModoGuiado()
        {
            // Primero ponemos el dron en modo GUIDED
            MAVLink.mavlink_set_mode_t setMode = new MAVLink.mavlink_set_mode_t
            {
                target_system = this.id,
                base_mode = (byte)MAVLink.MAV_MODE_FLAG.CUSTOM_MODE_ENABLED,
                custom_mode = 4 // GUIDED Mode en ArduPilot
            };

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.SET_MODE, setMode);
            EnviarMensaje(packet);
        }
        public void CambiaVelocidad(int velocidad)
        {
            this.velocidad = velocidad;
            // Crear el mensaje COMMAND_LONG
            MAVLink.mavlink_command_long_t speedCommand = new MAVLink.mavlink_command_long_t();
            speedCommand.target_system = this.id;      // ID del sistema (drone)
            speedCommand.target_component = 1; // ID del componente
            speedCommand.command = (ushort)MAVLink.MAV_CMD.DO_CHANGE_SPEED;
            speedCommand.param1 = 1;   // Tipo de velocidad (1 = Aérea, 0 = Terrestre, 2 = Vertical)
            speedCommand.param2 = velocidad;   // Velocidad en m/s (Aquí ponemos 7 m/s)
            speedCommand.param3 = -1;  // Sin aceleración específica (-1 usa la por defecto)
            speedCommand.param4 = 0;   // No se usa
            speedCommand.param5 = 0;   // No se usa
            speedCommand.param6 = 0;   // No se usa
            speedCommand.param7 = 0;   // No se usa

            // Generar el paquete MAVLink
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, speedCommand);

            // Enviar el paquete al dron
            EnviarMensaje(packet);

        }
        public void _CambiarHeading(float nuevoHeading, Action<byte,object> f = null, object param = null)
        {
            // paro el bucle de navegación, si es que está activo
            this.navegando = false;
            var req = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,       // ID del dron
                target_component = 1, // ID del componente (normalmente autopiloto)
                command = (ushort)MAVLink.MAV_CMD.CONDITION_YAW,
                param1 = nuevoHeading,  // Ángulo de heading (en grados)
                param2 = 30,     // Velocidad de giro (grados por segundo)
                param3 = 1,             // Sentido horario
                param4 = 0,     // heading absoluto
                param5 = 0,             // No usado
                param6 = 0,             // No usado
                param7 = 0       // 0 = Absoluto, 1 = Relativo
            };
            // Generar el paquete MAVLink
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, req);

            // Enviar el paquete al dron
            EnviarMensaje(packet);

            // Espero a que se complete la operación
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            var mensaje = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                condition: ComprobarOrientacion,
                parameters: nuevoHeading,
                timeout: -1
            );

            if (f != null)
                f(this.id, param);
        }
        public void CambiarHeading(float nuevoHeading, Boolean bloquear = true, Action<byte,object> f = null, object param = null)
        {
            if (bloquear)
            {
                this._CambiarHeading(nuevoHeading);
            }
            else
            {
                Thread t = new Thread(() => _CambiarHeading(nuevoHeading, f, param));
                t.Start();
            }
        }

    }
}

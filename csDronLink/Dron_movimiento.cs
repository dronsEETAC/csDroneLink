using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MAVLink;

namespace csDronLink
{
    public partial class Dron
    {

        // Movimiento
        public void _Mover(string direccion, int distancia, Action<byte, object> f = null, object param = null)
        {
            // paro el bucle de navegación si es necesario
            // Las operaciones de movimiento indican la distancia. No es necesario recordarle al autopiloto
            // el comando periódicamente

            // ATENCIÓN: EN LOS MOVIMIENTOS EN DIAGONAL HABRIA QUE RECTIFICAR LA DISTANCIA PORQUE SI NO 
            // LA DISTANCIA RECORRIDA ES MAYOR QUE LA INDICADA, DE ACUERDO CON EL TEOREMA DE PITAGORAS

            if (this.navegando)
                this.navegando = false;

            int dx = 0, dy = 0, dz = 0;

            if (direccion == "ForwardLeft")
            {
                dx = distancia;
                dy = -distancia;
            }

            if (direccion == "Forward")
            {
                dx = distancia;
                dy = 0;
            }

            if (direccion == "ForwardRight")
            {
                dx = distancia;
                dy = distancia;
            }

            if (direccion == "Left")
            {
                dx = 0;
                dy = -distancia;
            }

            if (direccion == "Stop")
            {
                dx = 0;
                dy = 0;
            }

            if (direccion == "Right")
            {
                dx = 0;
                dy = distancia;
            }
            if (direccion == "BackLeft")
            {
                dx = -distancia;
                dy = -distancia;
            }

            if (direccion == "Back")
            {
                dx = -distancia;
                dy = 0;
            }

            if (direccion == "BackRight")
            {
                dx = -distancia;
                dy = distancia;
            }
            if (direccion == "Up")
            {
                dz = -distancia;
            }
            if (direccion == "Down")
            {
                dz = distancia;
            }

            // Crear el mensaje de movimiento en coordenadas NED (Norte-Este-Abajo)
            MAVLink.mavlink_set_position_target_local_ned_t moveCmd = new MAVLink.mavlink_set_position_target_local_ned_t
            {
                target_system = this.id,
                target_component = 1,
                coordinate_frame = (byte)MAVLink.MAV_FRAME.BODY_OFFSET_NED, // Sistema de coordenadas local NED
                type_mask = 0b_0000110111111000,
                x = dx,
                y = dy,
                z = dz,
                vx = 0,
                vy = 0,
                vz = 0,
                afy = 0,
                afz = 0,
                yaw = 0,
                yaw_rate = 0
            };

            // Generar paquete MAVLink
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.SET_POSITION_TARGET_LOCAL_NED, moveCmd);

            // Enviar comando al dron
            EnviarMensaje(packet);
            // Espero 2 segundos a que el dron coja velocidad
            // Porque inmediatamente esperaré a que la velocidad vuelva a ser 0
            Thread.Sleep(2000);


            // Aqui espero el mensaje que indique que ya ha llegado al destino 
            // Lo sabre porque la velocidad será cero
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            MAVLink.MAVLinkMessage message = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                condition: ComprobarParado,
                timeout: -1
            );

            if (f != null)
                f(this.id, param);


        }
        public void Mover(string direccion, int distancia, Boolean bloquear = true, Action<byte,object> f = null, object param = null)
        {
            if (bloquear)
            {
                this._Mover(direccion, distancia);
            }
            else
            {
                Thread t = new Thread(() => _Mover(direccion, distancia, f, param));
                t.Start();
            }
        }

        private void _IrAlPunto(float lat, float lon, float alt, Action<byte,object> f = null, object param = null)
        {
            // Paramos el bucle de navegación (por si estuviera en marcha)
            this.navegando = false;
            var msg = new MAVLink.mavlink_set_position_target_global_int_t
            {
                target_system = this.id, // ID del dron
                target_component = 1, // Componente (generalmente 1)
                coordinate_frame = (byte)MAV_FRAME.GLOBAL_RELATIVE_ALT_INT,
                type_mask = (ushort)(
               POSITION_TARGET_TYPEMASK.VX_IGNORE |
               POSITION_TARGET_TYPEMASK.VY_IGNORE |
               POSITION_TARGET_TYPEMASK.VZ_IGNORE |
               POSITION_TARGET_TYPEMASK.AX_IGNORE |
               POSITION_TARGET_TYPEMASK.AY_IGNORE |
               POSITION_TARGET_TYPEMASK.AZ_IGNORE |
               POSITION_TARGET_TYPEMASK.YAW_IGNORE |
               POSITION_TARGET_TYPEMASK.YAW_RATE_IGNORE
           ), // Solo usamos lat, lon y alt
                lat_int = (int)(lat * 1e7), // Convertimos a formato entero
                lon_int = (int)(lon * 1e7),
                alt = alt,
                yaw = 0, // Mantener el rumbo actual
                yaw_rate = 0
            };

            // Generar el paquete MAVLink
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLINK_MSG_ID.SET_POSITION_TARGET_GLOBAL_INT, msg);

            // Enviar el paquete al dron
            EnviarMensaje(packet);

            // espero 2 segundos a que el dron empiece a moverse
            Thread.Sleep(2000);

            // Aqui espero el mensaje que indique que ya ha llegado al destino 
            // Lo sabre porque la velocidad será cero
            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT).ToString();
            MAVLink.MAVLinkMessage message = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                condition: ComprobarParado,
                timeout: -1
            );

            if (f != null)
                f(this.id, param);
        }
        public void IrAlPunto(float lat, float lon, float alt, Boolean bloquear = true, Action<byte,object> f = null, object param = null)
        {
            if (bloquear)
            {
                this._IrAlPunto(lat, lon, alt);
            }
            else
            {
                Thread t = new Thread(() => _IrAlPunto(lat, lon, alt, f, param));
                t.Start();
            }
        }

    }
}

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
        // Navegacion
        private void _BucleNavegacion()
        {
            // En este bucle repetimos cada segundo el ultimo comando de navegación
            // porque el dron solo navega pocos segundos si no se le insiste periódicamente.
            this.navegando = true;
            while (this.navegando)
            {
                // vuelvo a enviar el último comando de navegación
                //serialPort1.Write(this.navPacket, 0, this.navPacket.Length);
                EnviarMensaje(this.navPacket);
                Thread.Sleep(1000);
            }
        }
        public void Navegar(string direccion)
        {
            int vx = 0, vy = 0, vz = 0;
            int velocidad = this.velocidad;

            if (direccion == "NorthWest")
            {
                vx = velocidad;
                vy = -velocidad;
            }

            if (direccion == "North")
            {
                vx = velocidad;
                vy = 0;
            }

            if (direccion == "NorthEast")
            {
                vx = velocidad;
                vy = velocidad;
            }

            if (direccion == "West")
            {
                vx = 0;
                vy = -velocidad;
            }

            if (direccion == "Stop")
            {
                vx = 0;
                vy = 0;
            }

            if (direccion == "East")
            {
                vx = 0;
                vy = velocidad;
            }
            if (direccion == "SouthWest")
            {
                vx = -velocidad;
                vy = -velocidad;
            }

            if (direccion == "South")
            {
                vx = -velocidad;
                vy = 0;
            }

            if (direccion == "SouthEast")
            {
                vx = -velocidad;
                vy = velocidad;
            }
            if (direccion == "Up")
            {
                vz = -velocidad;
            }
            if (direccion == "Down")
            {
                vz = velocidad;
            }

            // Crear el mensaje SET_POSITION_TARGET_LOCAL_NED para navegar en la dirección indicada
            MAVLink.mavlink_set_position_target_local_ned_t moveCmd = new MAVLink.mavlink_set_position_target_local_ned_t
            {
                target_system = this.id,        // ID del sistema (autopiloto)
                target_component = 1,     // ID del componente (controlador de vuelo)
                coordinate_frame = (byte)MAVLink.MAV_FRAME.LOCAL_NED,  // Sistema de coordenadas local NED
                type_mask = 0b_0000111111000111, // Ignorar posición, usar velocidad
                x = 0,                    // No especificamos una posición en X
                y = 0,                    // No especificamos una posición en Y
                z = 0,                  // Mantener altura a 10 metros (-10 en NED)
                vx = vx,               // Velocidad hacia el norte (metros/segundo)
                vy = vy,                   // No moverse en la dirección este/oeste
                vz = vz,                   // Mantener la altura
                afx = 0,                  // Sin aceleración específica
                afy = 0,                  // Sin aceleración específica
                afz = 0,                  // Sin aceleración específica
                yaw = float.NaN,          // No cambiar la orientación
                yaw_rate = 0              // No rotar
            };

            // Generar el paquete MAVLink
            this.navPacket = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.SET_POSITION_TARGET_LOCAL_NED, moveCmd);
            EnviarMensaje(this.navPacket);
            // Pongo en marcha el bucle de navegación si es necesario
            if (!this.navegando)
            {
                Thread t = new Thread(() => _BucleNavegacion());
                t.Start();
            }

        }

    }
}

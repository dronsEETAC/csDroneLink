using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csDronLink
{
    public partial class Dron
    {
        private bool ComprobarEnAire(MAVLink.MAVLinkMessage msg, object targetAlt)
        {
            // es la función que comprobará que la altitud del dron es la indicada como 
            // parámetro. Se usará para ver si el dron ha alcanzado la altura indicada en el despegue
            // Recupero la altitud a partir del mensaje recibido
            var position = (MAVLink.mavlink_global_position_int_t)msg.data;
            float altitud = position.relative_alt / 1000.0f;
            // retorno el resultado de realizar la comprobacion (con un margen del 10%)
            return altitud > (int)targetAlt * 0.90;
        }
        private bool ComprobarParado(MAVLink.MAVLinkMessage msg, object param = null)
        {
            // verifica si el mensaje indica que la velocidad del dron es cero
            // Servirá para detectar que el dron ha llegado al destino
            var position = (MAVLink.mavlink_global_position_int_t)msg.data;
            float vx = position.vx;
            float vy = position.vy;
            float vz = position.vz;
            double velocidad = Math.Sqrt(vx * vx + vy * vy + vz * vz) / 100;
            return velocidad < 0.1;
        }
        private bool ComprobarEnTierra(MAVLink.MAVLinkMessage msg, object param = null)
        {
            // es la función que comprobará que la altitud del dron es menor de 50 cm. Se usará para 
            // detectar el fin de la operación de aterrizaje o RTL
            var position = (MAVLink.mavlink_global_position_int_t)msg.data;
            float altitud = position.relative_alt / 1000.0f;
            return altitud < 0.50;
        }
        private bool ComprobarOrientacion(MAVLink.MAVLinkMessage msg, object grados)
        {
            // es la función que comprobará que la orientación es la indicada
            // Se usa para determiar cuándo ha acabado la operación de cambio de heading
            var position = (MAVLink.mavlink_global_position_int_t)msg.data;
            float heading = position.hdg / 100.0f;
            return Math.Abs(heading - (float)grados) < 5;
        }
        private bool ComprobarEnWaypoint(MAVLink.MAVLinkMessage msg, object n)
        // Se usa para comprobar que el mensaje indica que se ha llegado al  waypoint
        // con índice n
        {

            var seq = ((MAVLink.mavlink_mission_item_reached_t)msg.data).seq;
            return seq == (int)n;
        }

    }
}

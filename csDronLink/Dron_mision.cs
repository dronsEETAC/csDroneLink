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
        // Mision
        public void CargarMision(List<(float lat, float lon)> mision)
        {

            // primero borramos la misión que tenga el autopiloto
            // Enviar comando para borrar todas las misiones del autopiloto
            MAVLink.mavlink_mission_clear_all_t clearMission = new MAVLink.mavlink_mission_clear_all_t
            {
                target_system = this.id,
                target_component = 1
            };
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_COUNT, clearMission);
            EnviarMensaje(packet);


            // POR ALGUNA RAZON que no controlo tengo que enviar dos veces el primer waypoint de la misión
            (float lat, float lon) primero = mision[0];
            mision.Insert(0, primero);

            // Lista con los comandos
            List<MAVLink.mavlink_mission_item_int_t> wploader = new List<MAVLink.mavlink_mission_item_int_t>();
            int seq = 0;
            foreach (var wp in mision)
            {
                wploader.Add(new MAVLink.mavlink_mission_item_int_t()
                {
                    target_system = this.id,
                    target_component = 1,
                    seq = (ushort)seq,
                    frame = (byte)MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT,
                    command = (ushort)MAVLink.MAV_CMD.WAYPOINT,
                    current = (byte)(seq == 0 ? 1 : 0), // El primer waypoint es el actual
                    autocontinue = 1,
                    //param1 = mision.Count,
                    x = (int)(wp.lat * 1e7),  // Latitud en formato entero
                    y = (int)(wp.lon * 1e7),  // Longitud en formato entero
                    z = 20,               // Altitud en metros. VER COMO HACER PARA QUE ESTO PUEDA CAMBIARSE
                    mission_type = (byte)MAVLink.MAV_MISSION_TYPE.MISSION
                });
                seq++;
            }
            // Envio el número total de waypoints
            var countMsg = new MAVLink.mavlink_mission_count_t
            {
                target_system = this.id,
                target_component = 1,
                count = (ushort)mision.Count,
                mission_type = (byte)MAVLink.MAV_MISSION_TYPE.MISSION
            };

            packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_COUNT, countMsg);
            EnviarMensaje(packet);
            string msgType;
            // Ahora espero que el autopiloto me vaya pidiendo los waypoints uno a uno
            while (true)
            {
                msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST).ToString();
                MAVLink.MAVLinkMessage request = messageHandler.WaitForMessageBlock(
                    msgType,
                    timeout: -1
                );
                int next = ((MAVLink.mavlink_mission_request_t)request.data).seq;
                MAVLink.mavlink_mission_item_int_t msg1 = wploader[next];
                packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_INT, msg1);
                EnviarMensaje(packet);
                if (next == wploader.Count - 1) break; // Ya estan todos
            }

            // Espero la confirmación final
            msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ACK).ToString();
            MAVLink.MAVLinkMessage response2 = messageHandler.WaitForMessageBlock(
                msgType,
                timeout: -1
            );


        }
        private void _EjecutarMision(Action<byte,object> EnWaypoint = null, Action<byte,object> f = null, object param = null)
        {
            // Enviar solicitud de número de waypoints
            MAVLink.mavlink_mission_request_list_t requestList = new MAVLink.mavlink_mission_request_list_t
            {
                target_system = this.id,
                target_component = 1
            };
            // Enviar el comando al autopiloto
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST_LIST, requestList);
            EnviarMensaje(packet);

            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_COUNT).ToString();

            var mensaje = messageHandler.WaitForMessageBlock(
                msgType, // GLOBAL_POSITION_INT
                timeout: -1 // tiempo indefinido
            );
            // Recojo el numero de waypoints de la misión
            int numWaypoints = ((MAVLink.mavlink_mission_count_t)mensaje.data).count;

            // Doy la orden de iniciar la misión
            MAVLink.mavlink_command_long_t cmd = new MAVLink.mavlink_command_long_t
            {
                target_system = this.id,
                target_component = 1,
                command = (ushort)MAVLink.MAV_CMD.MISSION_START,
                confirmation = 0,
            };

            // Enviar el comando al autopiloto
            packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, cmd);
            EnviarMensaje(packet);


            // Ahora espero que se llegue a cada uno de los waypoints
            for (int i = 1; i < numWaypoints; i++)
            {
                msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_REACHED).ToString();
                mensaje = messageHandler.WaitForMessageBlock(
                    msgType,
                    condition: ComprobarEnWaypoint,
                    parameters: i,
                    timeout: -1
                );
                // Llamo a la función que me piden, pasandole el índice del waypoint alcanzado
                if (EnWaypoint != null)
                    EnWaypoint(this.id, i);
            }
            PonModoGuiado();
            if (f != null)
                f(this.id, param);
        }
        public void EjecutarMision(Boolean bloquear = true, Action<byte,object> EnWaypoint = null, Action<byte,object> f = null, object param = null)
        {
            // EnWaypointy es la función que se activará cada vez que se alcance uno de los waypoints,
            // pasándole como parámetro el índice del waypoint alcanzado
            if (bloquear)
            {
                this._EjecutarMision();
            }
            else
            {
                Thread t = new Thread(() => _EjecutarMision(EnWaypoint, f, param));
                t.Start();
            }
        }

    }
}

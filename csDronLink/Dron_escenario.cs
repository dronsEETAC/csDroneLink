using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csDronLink
{
    public partial class Dron
    {
        // Escenario
        public void EstableceEscenario(List<List<(float lat, float lon)>> scenario)
        {
            /* Un escenario es una lista de fences. El primero siempre es el de inclusión que define
             * los límites del area de vuelo. Los restantes son fences de exclusión, que representan los obstaculos
             * del área de vuelo.
             * Cada fence es una lista de PointLatLng. 
             * Si la lista tiene 3 PointLatLng o más es un fence de tipo polígono. Si tiene solo dos entonces
             * es un fence de tipo círculo. En ese caso, el primer PointLatLng es el centro del círculo y la latitud 
             * del segundo PointLatLng es en realidad el radio del círculo.
             * El primer fence (el de inclusión) debe ser de tipo polígono
             * */

            // Tomo el fence de inclusión
            List<(float lat, float lon)> waypoints = scenario[0];
            // En esta lista prepararé los comandos para fijar los waypoints
            List<MAVLink.mavlink_mission_item_int_t> wploader = new List<MAVLink.mavlink_mission_item_int_t>();
            int seq = 0;

            // preparo los comandos 
            foreach (var wp in waypoints)
            {
                wploader.Add(new MAVLink.mavlink_mission_item_int_t()
                {
                    target_system = this.id,
                    target_component = 1,
                    seq = (ushort)seq,
                    frame = (byte)MAVLink.MAV_FRAME.GLOBAL,
                    command = (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_INCLUSION,
                    param1 = waypoints.Count,
                    x = (int)(wp.lat * 1e7),
                    y = (int)(wp.lon * 1e7),
                    mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                });
                seq++;
            }


            // ahora preparamos los obstáculos
            for (int i = 1; i < scenario.Count; i++)
            {
                waypoints = scenario[i];
                if (waypoints.Count == 2)
                // es un circulo
                {
                    wploader.Add(new MAVLink.mavlink_mission_item_int_t()
                    {
                        target_system = this.id,
                        target_component = 1,
                        seq = (ushort)seq,
                        frame = (byte)MAVLink.MAV_FRAME.GLOBAL,
                        command = (ushort)MAVLink.MAV_CMD.FENCE_CIRCLE_EXCLUSION,
                        param1 = Convert.ToSingle(waypoints[1].lat), // en realidad es el radio
                        x = (int)(waypoints[0].lat * 1e7),
                        y = (int)(waypoints[0].lon * 1e7),
                        mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                    });
                    seq++;
                }
                else
                {
                    // es un polígono

                    foreach (var wp in waypoints)
                    {
                        wploader.Add(new MAVLink.mavlink_mission_item_int_t()
                        {
                            target_system = this.id,
                            target_component = 1,
                            seq = (ushort)seq,
                            frame = (byte)MAVLink.MAV_FRAME.GLOBAL,
                            command = (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_EXCLUSION,
                            param1 = waypoints.Count,
                            x = (int)(wp.lat * 1e7),
                            y = (int)(wp.lon * 1e7),
                            mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                        });
                        seq++;
                    }
                }
            }
            // Envío el número de waypoints
            var msg = new MAVLink.mavlink_mission_count_t
            {
                target_system = this.id,
                target_component = 1,
                count = (ushort)wploader.Count,
                mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
            };

            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_COUNT, msg);
            EnviarMensaje(packet);


            // Ahora espero a que el autopiloto me vaya pidiendo los waypoints uno a uno
            string msgType;
            while (true)
            {
                msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST).ToString();
                MAVLink.MAVLinkMessage request = messageHandler.WaitForMessageBlock(
                    msgType,
                    timeout: -1
                );
                // El mensaje contiene el número de waypoint que está pidiendo el autopiloto
                int next = ((MAVLink.mavlink_mission_request_t)request.data).seq;
                // envío el comando que me pide
                MAVLink.mavlink_mission_item_int_t msg2 = wploader[next];
                packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_INT, msg2);
                //serialPort1.Write(packet, 0, packet.Length);
                EnviarMensaje(packet);
                if (next == wploader.Count - 1) break; // Ya los he enviado todos
            }
            // Espero a que me confirme que ha recibido todo bien
            msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ACK).ToString();
            MAVLink.MAVLinkMessage response = messageHandler.WaitForMessageBlock(
                msgType,
                timeout: -1
            );

        }

    }
}

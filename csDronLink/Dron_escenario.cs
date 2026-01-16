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
        public void AvisoBrench(MAVLink.MAVLinkMessage msg)
        {
            this.brench();
        }
        public void EstableceEscenario(List<List<(float lat, float lon)>> scenario, Action brench = null)
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

            if (brench != null)
            {
                this.brench = brench;
                msgType = ((int)MAVLink.MAVLINK_MSG_ID.FENCE_STATUS).ToString();
                messageHandler.RegisterHandler(msgType, AvisoBrench);
            }

        }



        public List<List<(float lat, float lon)>> LeeEscenario()
        {

            // 1) Request FENCE item count
            var msg = new MAVLink.mavlink_mission_request_list_t
            {
                target_system = this.id,
                target_component = 1,
                mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
            };
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST_LIST, msg);
            EnviarMensaje(packet);

            string msgType = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_COUNT).ToString();
            MAVLink.MAVLinkMessage response = messageHandler.WaitForMessageBlock(msgType, timeout: -1);
            int count = ((MAVLink.mavlink_mission_count_t)response.data).count;

            Console.WriteLine($"Drone {this.id}: The scenario has {count} items");

            // 2) Download all items
            var wploader = new List<MAVLink.mavlink_mission_item_int_t>();

            for (int i = 0; i < count; i++)
            {
                // 1) Request with INT variant (consistent with ITEM_INT)
                var reqInt = new MAVLink.mavlink_mission_request_int_t
                {
                    target_system = this.id,
                    target_component = 1,
                    seq = (ushort)i,
                    mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                };

                // If your library only has GenerateMAVLinkPacket10, use that; otherwise 20 is fine
                byte[] packetReq = mavlink.GenerateMAVLinkPacket10(
                    MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST_INT, reqInt);
                EnviarMensaje(packetReq);

                // 2) Wait for MISSION_ITEM_INT (use a FINITE timeout, e.g., 1500 ms)
                string idItemInt = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ITEM_INT).ToString();
                MAVLink.MAVLinkMessage resp = messageHandler.WaitForMessageBlock(idItemInt, timeout: 1500);

                // 3) Retry/fallback if needed
                if (resp == null)
                {
                    // Some firmwares still reply with MISSION_ITEM (non-INT).
                    // Try the non-INT request and wait for ITEM (non-INT).
                    var req = new MAVLink.mavlink_mission_request_t
                    {
                        target_system = this.id,
                        target_component = 1,
                        seq = (ushort)i,
                        mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                    };
                    byte[] packetReqOld = mavlink.GenerateMAVLinkPacket10(
                        MAVLink.MAVLINK_MSG_ID.MISSION_REQUEST, req);
                    EnviarMensaje(packetReqOld);

                    string idItem = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ITEM).ToString();
                    resp = messageHandler.WaitForMessageBlock(idItem, timeout: 1500);

                    if (resp == null)
                        throw new TimeoutException($"No response for fence seq={i}.");

                    // Convert ITEM -> ITEM_INT to unify downstream parsing
                    var wpOld = (MAVLink.mavlink_mission_item_t)resp.data;
                    var wpInt = new MAVLink.mavlink_mission_item_int_t
                    {
                        target_system = wpOld.target_system,
                        target_component = wpOld.target_component,
                        seq = wpOld.seq,
                        frame = wpOld.frame,
                        command = wpOld.command,
                        current = wpOld.current,
                        autocontinue = wpOld.autocontinue,
                        param1 = wpOld.param1,
                        param2 = wpOld.param2,
                        param3 = wpOld.param3,
                        param4 = wpOld.param4,
                        // x,y in degrees -> *1e7 to simulate INT
                        x = (int)Math.Round(wpOld.x * 1e7),
                        y = (int)Math.Round(wpOld.y * 1e7),
                        z = wpOld.z,
                        mission_type = (byte)MAVLink.MAV_MISSION_TYPE.FENCE
                    };
                    wploader.Add(wpInt);
                    continue;
                }

                // Here I received ITEM_INT
                var wp = (MAVLink.mavlink_mission_item_int_t)resp.data;

                // (optional) check that it is indeed the expected seq and mission_type FENCE
                if (wp.seq != (ushort)i || wp.mission_type != (byte)MAVLink.MAV_MISSION_TYPE.FENCE)
                {
                    // If it doesn't match, you can repeat the request or handle as an error
                    // For simplicity, I still add it and continue
                }

                wploader.Add(wp);
            }

            // 4) Final ACK: DO NOT block infinitely; some firmwares do not send ACK for download
            string idAck = ((int)MAVLink.MAVLINK_MSG_ID.MISSION_ACK).ToString();
            var ack = messageHandler.WaitForMessageBlock(idAck, timeout: 500); // optional

            Console.WriteLine($"Drone {this.id}: Downloaded {wploader.Count} items");


            // 4) Parsing into polygons + type
            var scenarioResult = new List<List<(float lat, float lon)>>();

            List<(float lat, float lon)> currentFence = null;
            int expectedVertices = 0;
            string currentType = "";

            foreach (var item in wploader.OrderBy(x => x.seq))
            {
                if (item.command == (ushort)MAVLink.MAV_CMD.FENCE_CIRCLE_EXCLUSION)
                { 
                    scenarioResult.Add(new List<(float lat, float lon)>
                    {
                         (item.x/ 1e7f, item.y/ 1e7f),  // centro
                         (item.param1, 0),  // radio
                    });
                    Console.WriteLine("Añado circulo");
                }
                else
                {
                    bool isInclusion = item.command == (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_INCLUSION;
                    bool isExclusion = item.command == (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_EXCLUSION;
                    if (!isInclusion && !isExclusion)
                        continue;

                    // If a new polygon starts
                    if (expectedVertices == 0)
                    {
                        expectedVertices = (int)Math.Round(item.param1); // number of vertices declared in the first polygon item
                        currentFence = new List<(float lat, float lon)>(capacity: Math.Max(3, expectedVertices));
                        currentType = isInclusion ? "Inclusion" : "Exclusion";
                    }

                    // Add vertex (lat/lon in 1e7)
                    float lat = item.x / 1e7f;
                    float lon = item.y / 1e7f;
                    currentFence.Add((lat, lon));
                    expectedVertices--;

                    // If polygon is complete -> push and clear
                    if (expectedVertices == 0)
                    {
                        scenarioResult.Add((currentFence));
                        currentFence = null;
                        currentType = "";
                        Console.WriteLine("Añado {0}", currentType);
                    }
                }

                //Console.WriteLine($"Drone {this.id}: Parsed {scenarioResult.Count} fences");

                // If pending vertices remain (incomplete data), return what we have
                //if (expectedVertices > 0 && currentFence != null && currentFence.Count > 0)
                //{
                //    scenarioResult.Add(currentFence);
                //}
            }

            Console.WriteLine($"Drone {this.id}: Final scenario has {scenarioResult.Count} fences");
            return scenarioResult;
        }
    }
}

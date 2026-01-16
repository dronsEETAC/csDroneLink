using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static MAVLink;

namespace csDronLink
{
    public partial class Dron
    {
        public void SendRC(ushort roll, ushort pitch, ushort throttle, ushort yaw)
        {
            // Crear el mensaje RC_CHANNELS_OVERRIDE
            var msg = new mavlink_rc_channels_override_t
            {
                target_system = this.id,
                target_component = 1,
                chan1_raw = roll,      // Aileron (Roll)
                chan2_raw = pitch,     // Elevator (Pitch)
                chan3_raw = throttle,  // Throttle
                chan4_raw = yaw,       // Rudder (Yaw)
                chan5_raw = 0,
                chan6_raw = 0,
                chan7_raw = 0,
                chan8_raw = 0
            };
            // Generar el paquete MAVLink
            byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.RC_CHANNELS_OVERRIDE, msg);

            // Enviar el paquete al dron
            EnviarMensaje(packet);

        }

    }
}

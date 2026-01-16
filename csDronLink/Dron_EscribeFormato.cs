using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MAVLink;

namespace csDronLink
{
    public partial class Dron
    {
        public void EscribeFormato(MAVLinkMessage msg)
        {
            Console.WriteLine($"Mensaje recibido: {msg.msgid} ({msg.msgtypename})");

            // Obtener el buffer binario completo del mensaje MAVLink
            byte[] raw = msg.buffer; // o msg.data según versión

            // Mostrar bytes en formato hexadecimal
            Console.WriteLine("Hex: " + BitConverter.ToString(raw));

            // Mostrar bytes en formato binario (bits)
            Console.WriteLine("Bits:");
            foreach (byte b in raw)
            {
                Console.Write(Convert.ToString(b, 2).PadLeft(8, '0') + " ");
            }

            Console.WriteLine("\n");
        }
    }
}

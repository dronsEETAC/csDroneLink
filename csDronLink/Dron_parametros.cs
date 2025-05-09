using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static csDronLink.MessageHandler;

namespace csDronLink
{
    public partial class Dron
    {
        // Parametros
        public List<float> LeerParametros(List<string> parametros)
        {
            // En la lista resultante dejaré los valores de los parámetros en el mismo
            // orden en el que aparecen en la lista de parámetros

            List<float> resultado = new List<float>();

            // Crear un vector de bytes con un tamaño fijo para el nombre del parámetro
            byte[] paramIdBytes = new byte[16];

            foreach (string param in parametros)
            {
                // Primero hago una petición síncrona pero no bloqueante. 
                // Lo hago así porque si primero pido y luego hago la petición
                // pudiera pasar que el mensaje llegase antes de que la petición se haya 
                // registrado. Así que primero registro, luego pido y luego espero
                string msgType = ((int)MAVLink.MAVLINK_MSG_ID.PARAM_VALUE).ToString();
                WaitingRequest waiting = messageHandler.WaitForMessageNoBlock(msgType);

                // Convertir el nombre del parámetro 
                Array.Copy(Encoding.ASCII.GetBytes(param), paramIdBytes, param.Length);

                // Crear la solicitud para obtener el valor del parámetro RTL_ALT
                MAVLink.mavlink_param_request_read_t req = new MAVLink.mavlink_param_request_read_t
                {
                    target_system = this.id,  // ID del sistema (1 es el sistema principal)
                    target_component = 1,  // ID del componente (1 es el autopiloto)
                    param_index = -1,  // Índice del parámetro (no se usa para leer por nombre)
                    param_id = paramIdBytes,  // El parámetro debe ser un arreglo de bytes de longitud fija
                };

                // Generar el paquete MAVLink para solicitar el parámetro
                byte[] packet = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.PARAM_REQUEST_READ, req);

                EnviarMensaje(packet);

                // ahora espero la respuesta
                var response = messageHandler.WaitNow(waiting, timeout: 10000);

                if (response != null)
                {
                    // aqui tengo la respuesta
                    var paramResponse = (MAVLink.mavlink_param_value_t)response.data;
                    string res = Encoding.ASCII.GetString(paramResponse.param_id).Trim();
                    // Verificamos si el parámetro es el correcto (NO ESTOY SEGURO DE QUE HAGA FALTA)
                    res = res.Split('\0')[0];
                    if (res == param)
                        resultado.Add(paramResponse.param_value);
                }
                // hACEMOS ALGO SI NO LLEGA EL PARAMETRO EN 10 SEGUNDOS?

            }
            return resultado;
        }
        public void EscribirParametros(List<(string parametro, float valor)> parametros)
        {
            byte[] paramIdBytes = new byte[16];

            for (int i = 0; i < parametros.Count; i++)
            {
                // datos del siguiente parámetro a escribir
                string parametro = parametros[i].parametro;
                float valor = parametros[i].valor;

                // Convertir el nombre del parámetro en un vector de bytes
                Array.Copy(Encoding.ASCII.GetBytes(parametro), paramIdBytes, parametro.Length);

                // Crear la solicitud para establecer el valor del parámetro 
                MAVLink.mavlink_param_set_t peticion = new MAVLink.mavlink_param_set_t
                {
                    target_system = this.id,      // ID del sistema (1 es el sistema principal)
                    target_component = 1,   // ID del componente (1 es el autopiloto)
                    param_value = valor,  // Nuevo valor del parámetro 
                    param_id = paramIdBytes,    // Nombre del parámetro en un array de bytes
                    param_type = (byte)MAVLink.MAV_PARAM_TYPE.REAL32  // Tipo de dato (float de 32 bits)
                };

                // Generar el paquete MAVLink para enviar el nuevo valor del parámetro
                byte[] paquete = mavlink.GenerateMAVLinkPacket10(MAVLink.MAVLINK_MSG_ID.PARAM_SET, peticion);

                // Enviar la solicitud de actualización del parámetro al dron
                EnviarMensaje(paquete);
            }
        }


    }
}

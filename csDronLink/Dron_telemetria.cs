using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csDronLink
{
    public partial class Dron
    {
        // Telemetria
        public void EnviarDatosTelemetria( Action<byte, List<(string nombre, float valor)>> f)
        {
            // El cliente me pide que ejecute la función f cada vez que reciba un mensaje
            // con datos de telemetría
            this.ProcesarTelemetria = f;

        }
        public void DetenerDatosTelemetria()
        {
            // El cliente ya no quiere datos de telemetría
            this.ProcesarTelemetria = null;
        }

    }
}

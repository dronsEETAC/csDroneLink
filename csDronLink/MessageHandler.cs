using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MAVLink;

namespace csDronLink
{

    public class MessageHandler
    {
        // Esta es la clase para gestionar los mensajes que se reciben del autopiloto.
        // La idea es que haya un solo punto de recepción de los mensajes (en el método MessageLoop)
        // y de ahí se reenvía a los que haya solicitado ese tipo de mensaje.
        // Se consideran dos tipos de peticiones. Las síncronas son peticiones de un mensaje
        // concreto que el solicitante necesita en ese instante. Por tanto el solicitante quedará
        // bloqueado hasta que se reciba el mensaje esperado.
        // Las peticiones asíncronas no dejan bloqueado al solicitante. Simplemente se registra
        // el solicitante para que cada vez que llegue un mensaje del tipo indicado se le envíe.

        private MavlinkParse mavlink;
        string modo;
        SerialPort puertoSerie;
        NetworkStream puertoTCP;

        private bool running = true;
        Thread messageThread;
        private readonly object lockObj = new object();

        // Lista para las peticiones asíncronas
        private ConcurrentDictionary<string, List<Action<MAVLinkMessage>>> handlers = new ConcurrentDictionary<string, List<Action<MAVLinkMessage>>>();

        // Esta es la estructura que representa a una petición síncrona
        public class WaitingRequest
        {
            public string MsgType { get; }
            public Func<MAVLinkMessage, object, bool> Condition { get; }
            public object Params { get; }
            public BlockingCollection<MAVLinkMessage> Queue { get; }

            public WaitingRequest(string msgType, Func<MAVLinkMessage, object, bool> condition, object parameters, BlockingCollection<MAVLinkMessage> queue)
            {
                // Al registrar un petición síncrona guardamos:
                // el tipo de mensaje que se solicita
                MsgType = msgType;
                // la función que verificará si el mensaje cumple una cierta condición
                Condition = condition;
                // parámetros que puede necesitar la función que verifica la condición
                Params = parameters;
                // la cola en la que se quedará bloqueado el solicitante hasta que el manager
                // encole en ella el mensaje que cumpla las condiciones establecidas
                Queue = queue;

                /// LOS 3 CASOS TIPICOS:
                /// 1. Petición de mensaje que no requiere condición: petición del valor de un parametro
                /// 2. Petición con condición: petición de la altitud del dron, pero condicionado a que esa
                /// altitud sea inferior a 0.5 metros. Haremos eso cuando queramos saber si el dron acaba 
                /// de aterrizar
                /// 3. Petición con condición y parámetro: petición de la altitud pero condicionado a que 
                /// sea mayor que el valor recibido como parámetro. Usaremos eso cuando queramos saber si
                /// el dron ha alcanzado la altura de despegue establecida.
            }
        }

        // Lista para las peticiones síncronas
        private List<WaitingRequest> waitingThreads = new List<WaitingRequest>();

        private MAVLinkMessage LeerMensaje()
        {
            if (modo == "produccion")
                return mavlink.ReadPacket(puertoSerie.BaseStream);
            else
                return mavlink.ReadPacket(puertoTCP);
        }
        public MessageHandler(string modo, object canal)
        {
            this.modo = modo;
            if (modo == "produccion")
                this.puertoSerie = (SerialPort)canal;
            else
                this.puertoTCP = (NetworkStream)canal;

            this.mavlink = new MavlinkParse();
            // Pongo en marcha el thread que estará continuamente recibiendo y repartiendo los 
            // mensajes
            this.messageThread = new Thread(MessageLoop) { IsBackground = true };
            this.messageThread.Start();
        }
        private void MessageLoop()
        {
            while (running)
            {

                var packet = LeerMensaje();
                if (packet == null) continue;
                // Obtengo el Id del mensaje
                string msgType = packet.msgid.ToString();
                lock (lockObj)
                {
                    // Procesar peticiones síncronas
                    foreach (var waiting in waitingThreads.ToList())
                    {
                        // Recorro la lista de peticiones para ver cuáles hay esperando 
                        // un mensaje de ese tipo

                        if (waiting.MsgType == msgType && (waiting.Condition == null || waiting.Condition(packet, waiting.Params)))
                        {
                            // He comprobado que el mensaje es de ese tipo y si se había
                            // puesto una condición, he llamado a la función corresponiente 
                            // para verificar que se cumple la condición pasandole los parámetros 
                            // correspondientes
                            // Como se han satisfecho las condiciones pongo em mensaje en la cola
                            // para que le llege al solicitante, que quedará desbloqueado y podrá continuar

                            waiting.Queue.Add(packet);
                            // Elimino la petición de la cola porque ya se ha satisfecho
                            waitingThreads.Remove(waiting);
                            break;
                        }
                    }
                }
                // Procesar handlers asíncronos
                // Buscar en la lista de handlers todos los del tipo de mensaje recibido
                // y obtener los callbacks asociados
                if (handlers.TryGetValue(msgType, out var callbacks))
                {
                    // Ahora llamo a cada uno de los callbacks pasandole el mensaje recibido
                    foreach (var callback in callbacks)
                    {
                        Task.Run(() => callback(packet));
                    }
                }
            }
        }

        public void RegisterHandler(string msgType, Action<MAVLinkMessage> callback)
        {
            // Registro una petición asíncrona indicando el tipo de mensaje que interesa y el 
            // callback que hay que llamar (pasandole el mensaje) cada vez que se reciba un mensaje 
            // de ese tipo
            //LO CIERTO ES QUE NO ENTIENDO MUCHO LO QUE HACE LA FUNCIÓN. VER SI HAY UNA ALTERNATIVA
            // MAS CLARA
            handlers.AddOrUpdate(msgType, new List<Action<MAVLinkMessage>> { callback }, (key, list) =>
            {
                list.Add(callback);
                return list;
            });
        }

        public void UnregisterHandler(string msgType, Action<MAVLinkMessage> callback)
        {
            if (handlers.ContainsKey(msgType))
            {
                handlers[msgType].Remove(callback);
                if (!handlers[msgType].Any()) handlers.TryRemove(msgType, out _);
            }
        }

        public WaitingRequest WaitForMessageNoBlock(string msgType, Func<MAVLinkMessage, object, bool> condition = null, object parameters = null)
        {
            // Esta es una petición síncrona pero NO BLOQUEANTE
            // El solicitante tendrá que bloquearse después haciendo un WaitNow
            // preparo la cola en la que voy a hacer esperar al solicitante
            var msgQueue = new BlockingCollection<MAVLinkMessage>();
            // creo un objeto con el tipo de mensaja, la condición, los parámetros 
            // para la condición y a cola

            var waiting = new WaitingRequest(msgType, condition, parameters, msgQueue);
            // Añado a la lista de solicitudes
            lock (lockObj) waitingThreads.Add(waiting);
            // Retorno el objeto para que se pueda hacer el WaitNow
            return waiting;
        }
        public MAVLinkMessage WaitForMessageBlock(string msgType, Func<MAVLinkMessage, object, bool> condition = null, object parameters = null, int timeout = 3000)
        {
            // Esta es una petición síncrona BLOQUEANTE
            // preparo la cola en la que voy a hacer esperar al solicitante
            var msgQueue = new BlockingCollection<MAVLinkMessage>();
            // creo un objeto con el tipo de mensaja, la condición, los parámetros 
            // para la condición y a cola
            var waiting = new WaitingRequest(msgType, condition, parameters, msgQueue);
            // coloco el objeto en la cola de peticiones

            lock (lockObj) waitingThreads.Add(waiting);
            // hago que el solicitante espere
            return WaitNow(waiting, timeout);
        }

        public MAVLinkMessage WaitNow(WaitingRequest waiting, int timeout)
        {
            // el timeout está en MILISEGUNDOS
            // El valor -1 en timeout indica que esperará indefinidamente
            // Espero el mensaje
            if (waiting.Queue.TryTake(out var msg, timeout))
            {
                lock (lockObj) waitingThreads.Remove(waiting);
                return msg;
            }
            else
            {
                // ha habido timeout
                lock (lockObj) waitingThreads.Remove(waiting);
                return null;
            }

        }

        public void Stop()
        {
            running = false;
            messageThread.Join();
        }

    }

}

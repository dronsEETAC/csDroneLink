using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using csDronLink;


namespace TestsConsola
{
    internal class Program
    {
        static void test_basico(Dron dron)
        {
            Console.WriteLine("Despego a 20 m");
            dron.Despegar(20);
            Console.WriteLine("Muevo 50 metros hacia atras");
            dron.Mover("Back", 50);
            
            Console.WriteLine("Aterrizo");
            dron.Aterrizar();
            Console.WriteLine("Fin");
            Console.ReadKey();
        }
        static void test_navegacion(Dron dron)
        {
            Console.WriteLine("Despego a 20 m");
            dron.Despegar(20);
            Console.WriteLine("Navego 10 segundos al norte");
            dron.Navegar("North");
            Thread.Sleep(10000);
            Console.WriteLine("Navego 20 segundos al este");
            dron.Navegar("East");
            Thread.Sleep(20000);
            Console.WriteLine("RTL");
            dron.RTL();
            Console.WriteLine("Fin");
            Console.ReadKey();
        }
        static void ProcesarTelemetria(List<(string nombre, float valor)> telemetria)
        {
            foreach (var telem in telemetria)
                Console.Write("{0}: {1} ---", telem.nombre, telem.valor);
            Console.WriteLine();
        }
        static void test_telemetria(Dron dron)
        {
            Console.WriteLine("Pido datos de telemetria ");
            dron.EnviarDatosTelemetria(ProcesarTelemetria);
            //Console.WriteLine("Despego a 20 m");
            //dron.Despegar(20);
            //Console.WriteLine("Muevo 50 metros hacia la izquierda");
            //dron.Mover("Left", 50);
            //Console.WriteLine("Detengo el envio de datos");
            //dron.DetenerDatosTelemetria();
            //Console.WriteLine("Aterrizo");
            //dron.Aterrizar();
            //Console.WriteLine("Fin");
            //Console.ReadKey();
        }
        static void test_cambioHeading(Dron dron)
        {
            Console.WriteLine("Despego a 20 m");
            dron.Despegar(20);
            Console.WriteLine("Pongo el heading en 90 grados");
            dron.CambiarHeading(90);
            Console.WriteLine("Muevo 10 metros hacia delante");
            dron.Mover("Forward", 10);
            Console.WriteLine("Pongo el heading en 180 grados");
            dron.CambiarHeading(180);
            Console.WriteLine("Muevo 10 metros hacia delante");
            dron.Mover("Forward", 10);
            Console.WriteLine("Pongo el heading en 270 grados");
            dron.CambiarHeading(270);
            Console.WriteLine("Muevo 10 metros hacia delante");
            dron.Mover("Forward", 10);

         
            Console.WriteLine("RTL");
            dron.RTL();
            Console.WriteLine("Fin");
            Console.ReadKey();
        }

        static void EnDestino (object dron)
        {
            Console.WriteLine("En destino");
            Dron miDron = (Dron)dron;
            miDron.Aterrizar();
            Console.WriteLine("En tierra");

        } 
        static void EnAire (object dron)
        {
            Console.WriteLine("En el aire");
            Dron miDron = (Dron) dron;
            Console.WriteLine("Ya estamos en el aire");
            Console.WriteLine("Muevo 20 metros hacia atras");
            miDron.Mover("Back", 20, bloquear: false, f:EnDestino, param : miDron);
        }


        static void test_llamadasNoBloqueantes(Dron dron)
        {
            Console.WriteLine("Despego a 20 m");
            dron.Despegar(20, bloquear: false, f: EnAire, param: dron);
            while (true)
            {
                Console.WriteLine("Haciendo otras cosas");
                Thread.Sleep(5000);
            }
        }
        static void EnWayPoint (object n)
        {
            Console.WriteLine("He llegado al waypoint "+ n);
        }
        static void FinMision (object dron)
        {
            Console.WriteLine("Retorno");
            Dron miDron = (Dron)dron;
            miDron.RTL();
            Console.WriteLine("Fin");
            Console.ReadKey();

        }
        static void test_mision (Dron dron)
        {
            // Se asume que el dron está en el centro del Nou Camp
            // En este misión el dron visitará las 4 esquinas del campo
            List<(float lat, float lon)> mision= new List<(float lat, float lon)>();
         
            mision.Add((41.381231903161684f, 2.1222157786711398f));
            mision.Add((41.38144678751476f, 2.1229592490702984f));
            mision.Add((41.380573014924174f, 2.1234296078942556f));
            mision.Add((41.38035516016044f, 2.122686439511463f));
            Console.WriteLine("Cargo la mision");
            dron.CargarMision(mision);

            Console.WriteLine("Despego a 20 m");
            dron.Despegar(20);
            Console.WriteLine("Ejecuto la mision");
            dron.EjecutarMision(bloquear:false, EnWaypoint: EnWayPoint, f: FinMision, param:dron);
           
        }
        static void test_escenario (Dron dron)
        {
            // El escenario se situa en el Nou Camp. Consta de un fence de
            // inclusión que marca los límites del terreno de juego, dos obstaculos
            // correspondientes a las areas pequeñas y un obstaculo correspondiente
            // a círculo central
            // Para comprobar el correcto funcionamiento hay que ver con Mission Planner
            // si se han cargado bien los fences
            List<(float lat, float lon)> limites = new List<(float lat, float lon)>
                {
                    (41.381231903161684f, 2.1222157786711398f),  
                    (41.38144678751476f, 2.1229592490702984f),  
                    (41.380573014924174f, 2.1234296078942556f),   
                    (41.38035516016044f, 2.122686439511463f)   
                };

            // Las dos áreas pequeñas como obstáculos
            List<(float lat, float lon)> areaPequeña1 = new List<(float lat, float lon)>
                {
                     (41.3812630831125f, 2.122367454083514f),  
                     (41.38140016195083f, 2.1228174183374695f), 
                     (41.38126985244463f, 2.1228873375949764f), 
                     (41.381135311836346f, 2.1224407565309003f)  
                };
         
            List<(float lat, float lon)> areaPequeña2 = new List<(float lat, float lon)>
                {
                     (41.380528000007885f, 2.1227596575244694f), 
                     (41.38066592656998f, 2.1232051108585854f),  
                     (41.38053646176858f, 2.123277285576012f),   
                     (41.380406996709425f, 2.122828449052016f)  
                };
       

            // Y el circulo central también como obstaculo
            
            List<(float lat, float lon)> circuloCentral = new List<(float lat, float lon)>
                {
                     (41.38089947817193f, 2.122819453881491f),  // centro
                     (10f, 0),  // radio
                };
            List<List<(float lat, float lon)>> escenario = new List<List<(float lat, float lon)>>
            {
                limites,
                areaPequeña1,
                areaPequeña2,
                circuloCentral
            };
            dron.EstableceEscenario(escenario);
            Console.WriteLine("Escenario cargado. Compruebalo con Mission Planner");
            Console.ReadKey();

        }
        static void test_irAPunto (Dron dron)
        {
            // Se asume que el dron está en el centro del Nou Camp
            // El dron irá a las 4 esquinas del campo, aumentando la
            // altitud de 5 en 5 metros
            Console.WriteLine("Despego a 20 m");
            dron.Despegar(20);
            Console.WriteLine("Visito la primera esquina");
            dron.IrAlPunto(41.381231903161684f, 2.1222157786711398f, 25);
            Console.WriteLine("Visito la segunda esquina");
            dron.IrAlPunto(41.38144678751476f, 2.1229592490702984f, 30);
            Console.WriteLine("Visito la tercera esquina");
            dron.IrAlPunto(41.380573014924174f, 2.1234296078942556f, 35);
            Console.WriteLine("Visito la cuarta esquina");
            dron.IrAlPunto(41.38035516016044f, 2.122686439511463f, 40);
            Console.WriteLine("Retorno");
            dron.RTL();
            Console.WriteLine("Fin");
            Console.ReadKey();

           
        }
        static void Main(string[] args)
        {
            Dron miDron = new Dron();
            miDron.Conectar("produccion", "COM21");
            Console.WriteLine("Conectado");
            //test_basico(miDron);
            //test_navegacion(miDron);
            test_telemetria(miDron);
            //test_cambioHeading(miDron);
            //test_llamadasNoBloqueantes(miDron);
            //test_mision (miDron);
            //test_escenario(miDron);
            //test_irAPunto (miDron);
            Console.ReadKey();

        }
    }
}

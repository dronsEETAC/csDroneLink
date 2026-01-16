using GMap.NET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;
using static MAVLink;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using csDronLink;




namespace SimpleExample
{
    public partial class simpleexample : Form
    {

        Dron dron = new Dron();
        // para gestionar el mapa
        private GMapControl gmap;
        private GMapOverlay overlay;
        private GMapRoute ruta;

        // Para representar el dron
        private Bitmap iconoPersonalizado; //imagen de dron
        GMarkerGoogle dronIcon; // marcador para la posición del dron
        GMapRoute dronHeading; // linea marcando el heading

        // aqui iremos guardando los waypoints de la misión
        List<(float lat, float lon)> mision;

        PointLatLng home; // coordenadas del DroneLab o del Nou Camp (a elegir)
        string nombreHome;

        public simpleexample()
        {
            // No queremos que nos molesten con la excepción Cross-Threading
            CheckForIllegalCrossThreadCalls = false;

            InitializeComponent();

            // Hacemos que el formulario principal ocupe toda la pantalla
            this.WindowState = FormWindowState.Maximized;

         

            // Configuramos los 9 botones de movimiento. Todos ellos tendrán asociada la misma función
            // para gestionar el evento click, pero en el tag ponemos la palabra que identifica la dirección 
            // del movimiento, que es la palabra que hay que pasarle como parámetro al dron para que haga la
            // operación. El texto es el código de una flechita que representa la dirección del movimineto.

            Font letraGrande = new Font("Arial", 14);
            Font letraPequeña = new Font("Arial", 12);

            button26.Text = "\u2196";
            button26.Tag = "ForwardLeft";
            button26.Click += movButton_Click;
            button26.Font = letraGrande;


            button25.Text = "\u25B2";
            button25.Tag = "Forward";
            button25.Click += movButton_Click;
            button25.Font = letraGrande;


            button24.Text = "\u2197";
            button24.Tag = "ForwardRight";
            button24.Click += movButton_Click;
            button24.Font = letraGrande;


            button19.Text = "\u25C4";
            button19.Tag = "Left";
            button19.Click += movButton_Click;
            button19.Font = letraGrande;


            button8.Text = "Stop";
            button8.Tag = "Stop";
            button8.Click += movButton_Click;
            button8.Font = letraPequeña;


            button5.Text = "\u25BA";
            button5.Tag = "Right";
            button5.Click += movButton_Click;
            button5.Font = letraGrande;


            button4.Text = "\u2199";
            button4.Tag = "BackLeft";
            button4.Click += movButton_Click;
            button4.Font = letraGrande;


            button3.Text = "\u25BC";
            button3.Tag = "Back";
            button3.Click += movButton_Click;
            button3.Font = letraGrande;


            button2.Text = "\u2198";
            button2.Tag = "BackRight";
            button2.Click += movButton_Click;
            button2.Font = letraGrande;


            // Ahora configuramos los botones de navegación

            button9.Text = "NW";
            button9.Tag = "NorthWest";
            button9.Click += navButton_Click;
            button9.Font = letraGrande;


            button10.Text = "N";
            button10.Tag = "North";
            button10.Click += navButton_Click;
            button10.Font = letraGrande;


            button11.Text = "NE";
            button11.Tag = "NorthEast";
            button11.Click += navButton_Click;
            button11.Font = letraGrande;


            button12.Text = "W";
            button12.Tag = "West";
            button12.Click += navButton_Click;
            button12.Font = letraGrande;


            button13.Text = "Stop";
            button13.Tag = "Stop";
            button13.Click += navButton_Click;
            button13.Font = letraPequeña;


            button14.Text = "E";
            button14.Tag = "East";
            button14.Click += navButton_Click;
            button14.Font = letraGrande;


            button15.Text = "SW";
            button15.Tag = "SouthWest";
            button15.Click += navButton_Click;
            button15.Font = letraGrande;


            button16.Text = "S";
            button16.Tag = "South";
            button16.Click += navButton_Click;
            button16.Font = letraGrande;


            button17.Text = "SE";
            button17.Tag = "SouthEast";
            button17.Click += navButton_Click;
            button17.Font = letraGrande;


            // ahora configuro los botores de arriba y abajo


            button21.Text = "Arriba";
            button21.Tag = "Up";
            button21.Click += movButton_Click;
            button21.Font = letraPequeña;

            button20.Text = "Abajo";
            button20.Tag = "Down";
            button20.Click += movButton_Click;
            button20.Font = letraPequeña;

            // Cargamos la imagen del dron (un punto rojo)
            iconoPersonalizado = new Bitmap("dron.png"); 
            // y ajustamos su trabajo
            iconoPersonalizado = RedimensionarImagen(iconoPersonalizado, 20, 20);           
      
            // Configuración del formulario
            //this.Text = "Mapa Interactivo - Círculos de Clic";
            //this.Size = new System.Drawing.Size(800, 600);

            // Configurar el control del mapa
            gmap = new GMapControl
            {
                Dock = DockStyle.Fill,
                Visible = false, // Inicialmente oculto
                CanDragMap = true, // Permite arrastrar el mapa
                DragButton = MouseButtons.Left, // Usa el clic izquierdo para arrastrar
                MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter, // Zoom con la rueda del mouse
                IgnoreMarkerOnMouseWheel = true, // Evita interferencias con marcadores
                MinZoom = 5,
                MaxZoom = 20,
                Zoom = 18,
                ShowCenter = false
            };
            gmap.MouseDown += GMapControl1_MouseDown; // capturo el evento de click en raton
            this.Controls.Add(gmap);


        }

        private Bitmap RedimensionarImagen(Bitmap img, int ancho, int alto)
        {
            Bitmap nuevaImagen = new Bitmap(ancho, alto);
            using (Graphics g = Graphics.FromImage(nuevaImagen))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, ancho, alto);
            }
            return nuevaImagen;
        }

        private void AñadirWaypoint((float lat, float lon) point)
        {
            // Cuando estoy creando una misión, vengo aquí al clicar
            // en el mapa

            if (this.mision == null)
                // es el primer waypoint de la misión
                this.mision = new List<(float lat, float lon)>();

            PointLatLng p = new PointLatLng(point.lat, point.lon);
            // añado un marcador
            GMarkerGoogle marker = new GMarkerGoogle(p, GMarkerGoogleType.blue_dot);
            overlay.Markers.Add(marker);
            // añado el punto a la misión
            this.mision.Add(point);

            if (this.mision.Count > 1)
            {
                // Hay que dibujar la linea que va del anterior al nuevo
                PointLatLng previo = new PointLatLng(
                    this.mision[this.mision.Count - 2].lat,
                    this.mision[this.mision.Count - 2].lon);
                List<PointLatLng> points = new List<PointLatLng>
                {
                    previo,
                    p
                };
                GMapRoute route = new GMapRoute(points, "Line")
                {
                    Stroke = new System.Drawing.Pen(System.Drawing.Color.Blue, 2) // Color y grosor de la línea
                };
                overlay.Routes.Add(route);
            }

        }
        private void GMapControl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // aqui vendre cuando pulse el boton derecho del raton

                PointLatLng point = gmap.FromLocalToLatLng(e.X, e.Y);

                // muestro el desplegable con las 3 opciones (solo las dos primeras
                // están operativas
                ContextMenuStrip contextMenu = new ContextMenuStrip();

                ToolStripMenuItem option1 = new ToolStripMenuItem("Volar aquí");
                ToolStripMenuItem option2 = new ToolStripMenuItem("Pon waypoint");
                ToolStripMenuItem option3 = new ToolStripMenuItem("Opción 3");
                // indico lo que ha que hacer según la opción elegida
                option1.Click += (s, ev) => dron.IrAlPunto((float)point.Lat, (float)point.Lng, 20, bloquear: false);
                option2.Click += (s, ev) => AñadirWaypoint(((float)point.Lat, (float)point.Lng));

                contextMenu.Items.Add(option1);
                contextMenu.Items.Add(option2);

                contextMenu.Show(gmap, e.Location);
            }
        }


        private void but_connect_Click(object sender, EventArgs e)
        {
       

            if (nombreHome == null)
                MessageBox.Show("No has elegido la zona de vuelo (DroneLab o Nou Camp");
            else
            {
                if (prodRadio.Checked)
                    dron.Conectar("produccion", CMB_comport.Text);
                else
                    dron.Conectar("simulacion");

                but_connect.BackColor = Color.Green;
                but_connect.ForeColor = Color.White;

                // Acabo la configuración del mapa
                gmap.MapProvider = GMapProviders.GoogleSatelliteMap;
                GMaps.Instance.Mode = AccessMode.ServerOnly;


                // Situo el mapa en el home elegido (DroneLab o Nou Camp
                gmap.Position = home;

                gmap.Visible = true; // Mostrar el mapa

                // Crear un overlay para los marcadores y rutas
                overlay = new GMapOverlay("circulos");
                gmap.Overlays.Add(overlay);
                panelMapa.Controls.Add(gmap);
            }
        }


        private void CMB_comport_Click(object sender, EventArgs e)
        {
            // cargo la lista de los puertos disponibles
            CMB_comport.DataSource = SerialPort.GetPortNames();
        }


        private void simpleexample_Load(object sender, EventArgs e)
        {

        }



        private void EnAire(byte id,object param)
        {
            // Esto es lo que haré cuando el dron haya alcanzado la altura de despegue
            despegarBtn.BackColor = Color.Green;
            despegarBtn.ForeColor = Color.White;
            despegarBtn.Text = (string) param;
        }

        private void but_takeoff_Click(object sender, EventArgs e)
        {
            // Click en boton para dspegar
            // Llamada no bloqueante para no bloquear el formulario
            dron.Despegar(int.Parse(alturaBox.Text), bloquear: false, EnAire, "Volando");

            despegarBtn.BackColor = Color.Yellow;
        }

        private void navButton_Click(object sender, EventArgs e)
        {
            // Aqui vendremos cuando se clique cualquiera de los botones de navagación
            // En el tag del boton tenemos la dirección de navegación.
            Button b = (Button)sender;
            string tag = b.Tag.ToString();
            dron.Navegar(tag);

        }
        private void movButton_Click(object sender, EventArgs e)
        {
            // Aqui vendremos cuando se clique cualquiera de los botones de movimiento
            // En el tag del boton tenemos la dirección de navegación.
       
            Button b = (Button)sender;
            string direccion = b.Tag.ToString();
            // recupero la distancia que debe recorrer el dron
            int distancia = Convert.ToInt32(pasoLbl.Text);
            dron.Mover(direccion, distancia, bloquear: false);
        }
        private void EnTierra(byte id,object mensaje)
        {
            // Aqui vendre cuando el dron esté en tierra
            // El mensaje me dice si vengo de un aterrizaje o de un RTL
            if ((string) mensaje == "Aterrizaje")
                button7.BackColor = Color.Green;
            else
                button6.BackColor = Color.Green;
        }



        private void aterrizarBtn_Click(object sender, EventArgs e)
        {
            // Click en el botón de aterrizar
            dron.Aterrizar(bloquear: false, EnTierra, "Aterrizaje");
            button7.BackColor = Color.Yellow;
        }

        private void RTLBtn_Click(object sender, EventArgs e)
        {
            // Click en el botón de RTL
            dron.RTL(bloquear: false, EnTierra, "RTL");
            button6.BackColor = Color.Yellow;
        }

        private void enviarTelemetriaBtn_Click(object sender, EventArgs e)
        {

            dron.EnviarDatosTelemetria(ProcesarTelemetria);
        }

        private void detenerTelemetriaBtn_Click(object sender, EventArgs e)
        {
            dron.DetenerDatosTelemetria();
        }

        private void ProcesarTelemetria(byte id, List<(string nombre, float valor)> telemetria)
        {
            // Aqui vendre cada vez que llegue un paquete de telemetría
            double lat = ((double)telemetria[1].valor) / 0.1E+8;
            double lon = ((double)telemetria[2].valor) / 0.1E+8;
            double heading = ((double)telemetria[3].valor) / 100;

            // Coloco los datos de telemetria en su sitio
            altitudLbl.Text = telemetria[0].valor.ToString();
            latitudLbl.Text = lat.ToString();
            longitudLbl.Text = lon.ToString();
            headLbl.Text = heading.ToString();

            // Actualizo posición y heading del dron en el mapa
            PointLatLng point = new PointLatLng(lat, lon);
            int angulo = Convert.ToInt16(telemetria[3].valor / 100);

            // Defino la linea que debe señalar el heading
            double anguloRad = angulo * Math.PI / 180.0;
            // La línea que señala el heading va desde la posición del dron
            // hasta 50 metros en la dirección del heading
            double distanciaGrados = 50 / 111139.0;
            // Calculo hasta donde debe llegar la línea
            double lat2 = lat + (distanciaGrados * Math.Cos(anguloRad));
            double lon2 = lon + (distanciaGrados * Math.Sin(anguloRad) / Math.Cos(lat * Math.PI / 180));

            // Ahora preparo la linea
            List<PointLatLng> puntosLinea = new List<PointLatLng>
            {
                new PointLatLng(lat, lon),  // Punto de inicio
                new PointLatLng(lat2, lon2) // Punto final
            };
       
            // Borro el marcador y linea de heading del dron si es que hay
            if (overlay.Markers.Contains(dronIcon))
            {
                overlay.Markers.Remove(dronIcon);
                overlay.Routes.Remove(dronHeading);
            }
            dronHeading = new GMapRoute(puntosLinea, "miLinea")
            {
                Stroke = new Pen(Color.Red, 2) // Color rojo, grosor 2
            };
            // Añado la linea
            overlay.Routes.Add(dronHeading);
            // y el icono del dron
            dronIcon = new GMarkerGoogle(point, iconoPersonalizado);
            // Añado un offset para que el centro del icono esté exactamente en la
            // posición del mapa en la que está el dron
            dronIcon.Offset = new Point(-iconoPersonalizado.Width / 2, -iconoPersonalizado.Height / 2);
            overlay.Markers.Add(dronIcon);

        }

        private void distanciaTrackBar_Scroll(object sender, EventArgs e)
        {
            // Recojo el valor de la distancia a aplicar en las operaciones
            // de movimiento
            int n = trackBar1.Value;
            pasoLbl.Text = n.ToString();
        }
        private void headingTrackBar_Scroll(object sender, EventArgs e)
        {
            // Recojo el valor del heading seleccionado
            int n = trackBar2.Value;
            headingLbl.Text = n.ToString();
        }


        private void headingTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            // Cuando se libera la barra de desplazamiento recojo el valor
            // definitivo para el heading y lo envío al dron
            float valorSeleccionado = trackBar2.Value;
            dron.CambiarHeading(valorSeleccionado, bloquear: false);
        }

        private void velocidadTrackBar_Scroll(object sender, EventArgs e)
        {
            // Recojo y muestro el valor la velocidad según se mueve 
            // la barra de desplazamiento
            int n = trackBar3.Value;
            velocidadLbl.Text = n.ToString();

        }

        private void velocidadTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            // Cuando se libera la barra de desplazamiento recojo el valor
            // definitivo para la velocidad y lo envío al dron
            int valorSeleccionado = trackBar3.Value;
            dron.CambiaVelocidad(valorSeleccionado);
        }
        private void DibujarLimites(List<(float lat, float lon)> limites)
        {
            // Dibujo el poligono que define los límites del area de vuelo (geofence
            // de inclusió)
            // Convierto los puntos
            List<PointLatLng> puntos = new List<PointLatLng>();
            foreach ((float lat, float lon) p in limites)
                puntos.Add(new PointLatLng(p.lat, p.lon));
            // Dibujo en el mapa el polígono que represeta el geofence de inclusión
            GMapPolygon polygon = new GMapPolygon(puntos, "Polygon");
            // Establecer un relleno completamente transparente
            polygon.Fill = new SolidBrush(Color.FromArgb(0, 0, 0, 0)); // ARGB (0 = totalmente transparente)
            polygon.Stroke = new Pen(Color.Red, 2); // Solo borde rojo
            overlay.Polygons.Add(polygon);
        }

        private void DibujarCirculo(List<(float lat, float lon)> datos, int segments = 36)
        {
            // Dibujo el círculo que representa un obstaculo
            // Se representa mediante un polígono de tantos lados como indique 
            // segments
          
            double lat = datos[0].lat;
            double lon = datos[0].lon;
            double radiusMeters = datos[1].lat;

            double radiusLat = radiusMeters / 111320.0; // Aproximación de metros a grados en latitud
            double radiusLon = radiusMeters / (111320.0 * Math.Cos(lat * Math.PI / 180.0)); // Ajuste para la longitud
            List<PointLatLng> points = new List<PointLatLng>();
            for (int i = 0; i < segments; i++)
            {
                double angle = (Math.PI / 180) * (i * (360.0 / segments));
                double offsetX = radiusLon * Math.Cos(angle);
                double offsetY = radiusLat * Math.Sin(angle);

                points.Add(new PointLatLng(lat + offsetY, lon + offsetX));
            }

            GMapPolygon circle = new GMapPolygon(points, "Circle");

            // Relleno semitransparente
            circle.Fill = new SolidBrush(Color.FromArgb(50, Color.Black));
            circle.Stroke = new Pen(Color.Black, 2); // Contorno azul

            overlay.Polygons.Add(circle);
        }

        private void DibujarObstaculo(List<(float lat, float lon)> limites)
        {
            // Convierto los puntos
            List<PointLatLng> puntos = new List<PointLatLng>();
            foreach ((float lat, float lon) p in limites)
                puntos.Add(new PointLatLng(p.lat, p.lon));
            // Dibujo el polígono que representa un obstaculo
            GMapPolygon polygon = new GMapPolygon(puntos, "Polygon");
            polygon.Fill = new SolidBrush(Color.FromArgb(50, Color.Black));
            polygon.Stroke = new Pen(Color.Black, 2); // Solo borde rojo
            overlay.Polygons.Add(polygon);
        }

        private void enviarEscenarioBtn_Click(object sender, EventArgs e)
        {
            // Dibujo el escenario segun la zona de vuelo elegida
            if (nombreHome == "Nou Camp")
            {
                // Envio un escenario predefinido formado por el rectangulo
                // de juego del Nou Camp como limites
                List<(float lat, float lon)> fenceInclusion = new List<(float lat, float lon)>
                {
                   (41.381231903161684f, 2.1222157786711398f),  // Punto 1
                    (41.38144678751476f, 2.1229592490702984f),  // Punto 2
                    (41.380573014924174f, 2.1234296078942556f),   // Punto 3
                    (41.38035516016044f, 2.122686439511463f)   // Punto 3
                };

                List<List<(float lat, float lon)>> scenario = new List<List<(float lat, float lon)>>();
                scenario.Add(fenceInclusion);
                DibujarLimites(fenceInclusion);

                // Las dos áreas pequeñas como obstáculos
                List<(float lat, float lon)> fenceEnclusion = new List<(float lat, float lon)>
                {
                     (41.3812630831125f, 2.122367454083514f),  // Punto 1
                     (41.38140016195083f, 2.1228174183374695f),  // Punto 2
                     (41.38126985244463f, 2.1228873375949764f),   // Punto 3
                     (41.381135311836346f, 2.1224407565309003f)   // Punto 3
                };
                scenario.Add(fenceEnclusion);
                DibujarObstaculo(fenceEnclusion);
                fenceEnclusion = new List<(float lat, float lon)>
                {
                     (41.380528000007885f, 2.1227596575244694f),  // Punto 1
                     (41.38066592656998f, 2.1232051108585854f),  // Punto 2
                     (41.38053646176858f, 2.123277285576012f),   // Punto 3
                     (41.380406996709425f, 2.122828449052016f)   // Punto 3
                };
                scenario.Add(fenceEnclusion);
                DibujarObstaculo(fenceEnclusion);

                // Y el circulo central también como obstaculo
                fenceEnclusion = new List<(float lat, float lon)>
                {
                     (41.38089947817193f, 2.122819453881491f),  // Centro
                     (10f, 0),  // Radio
                };
                scenario.Add(fenceEnclusion);
                DibujarCirculo(fenceEnclusion);

                dron.EstableceEscenario(scenario);
        
                button27.BackColor = Color.Green;
                button27.ForeColor = Color.White;
            } else
            {
                // Envio un escenario predefinido formado por el rectangulo
                // con los límites del droneLab
                List<(float lat, float lon)> fenceInclusion = new List<(float lat, float lon)>
                {
                    (41.27643157806237f, 1.9882249167791768f),  
                    (41.27660756926631f, 1.9890211145457177f),  
                    (41.276378780608624f, 1.9891254292263607f),   
                    (41.276194789148306f, 1.9883313603308532f)  
                };

                List<List<(float lat, float lon)>> scenario = new List<List<(float lat, float lon)>>();
                scenario.Add(fenceInclusion);
                DibujarLimites(fenceInclusion);

                // Y un poligono como obstaculo
                List<(float lat, float lon)> fenceEnclusion = new List<(float lat, float lon)>
                {
                     (41.27638358037891f, 1.9884803813032008f), 
                     (41.27640757922506f, 1.9887081705037886f),  
                     (41.27650197460089f, 1.9888018408292638f),  
                     (41.27655797179322f, 1.988935959704376f),
                     (41.27639157999527f, 1.9890317589008852f),  
                     (41.27626838579467f, 1.988493154529402f)   
                };
                scenario.Add(fenceEnclusion);
                DibujarObstaculo(fenceEnclusion);
                dron.EstableceEscenario(scenario);
                button27.BackColor = Color.Green;
                button27.ForeColor = Color.White;
            }


        }
        private void FinMision (byte id, object param=null)
        {
            // Borro los puntos y lineas de la misión
            overlay.Markers.Clear();
            overlay.Routes.Clear();
            mision = null;
            dron.PonModoGuiado();
        }
        private void EnWaypoint (byte id, object n)
        {
            // Acabo de llegar al siguiente waypoint de la misión
            Console.WriteLine("Estoy en waypoint " + (int) n);
            (float lat, float lon) waypoint = mision[(int) n];

            PointLatLng p = new PointLatLng(waypoint.lat, waypoint.lon);
            // añado un marcador de color verde
            GMarkerGoogle marker = new GMarkerGoogle(p, GMarkerGoogleType.green_dot);
            overlay.Markers.Add(marker);
        }

        private void ejecutarMisionBtn_Click(object sender, EventArgs e)
        {
            // Ejecuto la misión indicando que al llegar al siguiente waypoint
            // ejecute EnWaypoit y cuando acabe ejecute FinMision
            dron.EjecutarMision(bloquear: false, EnWaypoint:EnWaypoint, FinMision);
            button28.BackColor = Color.Green;
            button28.ForeColor = Color.White;
          
        }

        private void cargarMisionBtn_Click(object sender, EventArgs e)
        {
            // En mision están los waypoints que se han ido recogiendo
            // cuando el usuario clicado sobre el mapa
            dron.CargarMision(mision);
            button29.BackColor = Color.Green;
            button29.ForeColor = Color.White;
        }

        private void parámetrosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Abro el formulario para leer/escribir los parámetros
            Parametros f = new Parametros(dron);
            f.ShowDialog();
        }

        private void DroneLabBtn_Click(object sender, EventArgs e)
        {
         
            nombreHome = "DroneLab";
            home = new PointLatLng(41.27641528631191, 1.988679188034754);
            button18.BackColor = Color.Green;
            button18.ForeColor = Color.White;
        }

        private void NouCampBtn_Click(object sender, EventArgs e)
        {
            nombreHome = "Nou Camp";
            home = new PointLatLng(41.38090032929851, 2.12283331445973);
            button1.BackColor = Color.Green;
            button1.ForeColor = Color.White;

        }


        private void simRadio_CheckedChanged(object sender, EventArgs e)
        {
            CMB_comport.Visible = false;
        }

        private void prodRadio_CheckedChanged(object sender, EventArgs e)
        { 
            CMB_comport.Visible = true;
        }

        private void ponGuiadoBtn_Click(object sender, EventArgs e)
        {
            dron.PonModoGuiado();
        }

        private void button30_Click(object sender, EventArgs e)
        {
            mision = null;
        }
    }

}


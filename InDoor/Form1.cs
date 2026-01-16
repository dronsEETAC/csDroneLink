using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using csDronLink;

namespace InDoor
{
    
    public partial class Form1 : Form
    {
        private PanelSinParpadeo panel;
        private int lineLength = 50; // Longitud de la línea
        TransformadorNEDCanvasEscalado conversor;
        Dron dron = new Dron();
        double heading = -1;
        double headingInicial;
        double posX;
        double posY;
        double destinoX = 100000;
        double destinoY = 100000;
        public Form1()
        {
            InitializeComponent();
            InitializeGridPanel();
            // No queremos que nos molesten con la excepción Cross-Threading
            CheckForIllegalCrossThreadCalls = false;
        }

        private void InitializeGridPanel()
        {
            panel = new PanelSinParpadeo
            {
                Width = 700,
                Height = 700,
                Location = new Point(300, 10),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            panel.Paint += panel_Paint;
            panel.MouseDown += Panel_MouseDown;
            panel.MouseMove+= Panel_MouseHover;
            Controls.Add(panel);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            panel.Size = new Size(700, 700);
        }

        private void panel_Paint(object sender, PaintEventArgs e)
        {
            int metrosFisicos = 10;
            double escalaPxPorMetro = panel.Width / (double)metrosFisicos; // 70 px/m
            double centroX = panel.Width / 2.0;
            double centroY = panel.Height / 2.0;

            using (Pen gridPen = new Pen(Color.LightGray, 1))
            {
                // Dibujar líneas verticales (Este-Oeste)
                for (double x = centroX; x <= panel.Width; x += escalaPxPorMetro)
                    e.Graphics.DrawLine(gridPen, (float)x, 0, (float)x, panel.Height);

                for (double x = centroX - escalaPxPorMetro; x >= 0; x -= escalaPxPorMetro)
                    e.Graphics.DrawLine(gridPen, (float)x, 0, (float)x, panel.Height);

                // Dibujar líneas horizontales (Norte-Sur)
                for (double y = centroY; y <= panel.Height; y += escalaPxPorMetro)
                    e.Graphics.DrawLine(gridPen, 0, (float)y, panel.Width, (float)y);

                for (double y = centroY - escalaPxPorMetro; y >= 0; y -= escalaPxPorMetro)
                    e.Graphics.DrawLine(gridPen, 0, (float)y, panel.Width, (float)y);
            }


            // Dibujar círculo rojo en el centro
            int radius = 10;
            int centerX = panel.Width / 2;
            int centerY = panel.Height / 2;

            Rectangle circleBounds = new Rectangle(
                centerX - radius,
                centerY - radius,
                radius * 2,
                radius * 2
            );

            using (Brush redBrush = new SolidBrush(Color.Red))
            {
                e.Graphics.FillEllipse(redBrush, circleBounds);
            }

            if ((button1.Text == "Conectado") && (conversor != null))
            {

                // Dibujar la localizacion del dron

                var (panelX, panelY) = conversor.NedACanvas(posX, posY);


                circleBounds = new Rectangle(
                    (int)panelX - radius,
                    (int)panelY - radius,
                    radius * 2,
                    radius * 2
                );

                using (Brush greenBrush = new SolidBrush(Color.Green))
                {
                    e.Graphics.FillEllipse(greenBrush, circleBounds);
                }




                // Línea verde con ángulo respecto a la vertical
                double angulo = (heading - headingInicial) % 360;
                double angleRad = angulo * Math.PI / 180.0; // grados → radianes

                int endX = (int) panelX + (int)(lineLength * Math.Sin(angleRad));
                int endY = (int) panelY - (int)(lineLength * Math.Cos(angleRad));

                using (Pen greenPen = new Pen(Color.Green, 1))
                {
                    e.Graphics.DrawLine(greenPen, (int) panelX, (int) panelY, endX, endY);
                }
                if (destinoX != 10000)
                {
                // Dibujar círculo azul en el destino
                    radius = 10;
                

                    circleBounds = new Rectangle(
                        (int) destinoX - radius,
                        (int) destinoY - radius,
                        radius * 2,
                        radius * 2
                    );

                    using (Brush blueBrush = new SolidBrush(Color.Blue))
                    {
                        e.Graphics.FillEllipse(blueBrush, circleBounds);
                    }
                }
            }

        }
        private void ProcesarTelemetria(byte id, List<(string nombre, float valor)> telemetria)
        {
            if (heading == -1)
            {
                // Es el primer paquete de telemetría. Ahora se cual es el heading inicial
                heading = ((double)telemetria[3].valor) / 100;
                headingInicial = heading;
                conversor = new TransformadorNEDCanvasEscalado(heading, 700, 700, 10, 10);
            } else
                heading = ((double)telemetria[3].valor) / 100;




        }
        private void ProcesarTelemetriaLocal(byte id, List<(string nombre, float valor)> telemetria)
        {
            posX = (double)telemetria[0].valor;
            posY = (double)telemetria[1].valor;
            panel.Invalidate();

        }

        private void button1_Click(object sender, EventArgs e)
        {

            dron.Conectar("simulacion");
            dron.EnviarDatosTelemetria(ProcesarTelemetria);
            dron.EnviarDatosTelemetriaLocal(ProcesarTelemetriaLocal);
            button1.BackColor = Color.Green;
            button1.ForeColor = Color.White;
            button1.Text = "Conectado";
            panel.Invalidate();


        }
        private void EnAire(byte id, object param)
        // La librería nos envia siempre en los callbacks como primer parámetro el id del dron
        {
            button3.BackColor = Color.Green;
            button3.ForeColor = Color.White;
            button3.Text = (string)param;
        }
        private void EnTierra(byte id, object param)
        // La librería nos envia siempre en los callbacks como primer parámetro el id del dron
        {
            button4.BackColor = Color.Green;
            button4.ForeColor = Color.White;
            button4.Text = (string)param;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            dron.Despegar(5, bloquear: false, EnAire, "Volando");
            button3.BackColor = Color.Yellow;
            button3.Text = "Despegando";

        }

        private void button4_Click(object sender, EventArgs e)
        {
            dron.Aterrizar(bloquear: false, EnTierra, "En tierra");
            button4.BackColor = Color.Yellow;
            button4.Text = "Aterrizando";

        }
        private void EnDestino(byte id, object param)
        // La librería nos envia siempre en los callbacks como primer parámetro el id del dron
        {
            // hago que ya no se pinte el circulo azul indicando el destino que se acaa de alcanzar
            destinoX = 100000;

        }


        private void Panel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var (X, Y) = conversor.CanvasANed(e.X, e.Y);
                destinoX = e.X;
                destinoY = e.Y;
                dron.IrAPosicionLocal ((float)X,(float) Y,-10, bloquear: false, EnDestino, "En destino"); 

            }
        }
        private void Panel_MouseHover(object sender, MouseEventArgs e)
        {
            if (conversor != null)
            {
                var (X, Y) = conversor.CanvasANed(e.X, e.Y);
                info.Text = X.ToString() + ", " + Y.ToString();
            }

            
        }
    }
}

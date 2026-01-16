using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InDoor
{
    public class TransformadorNEDCanvasEscalado
    {
        private readonly double headingInicialRad;

        private readonly double anchoCanvas;
        private readonly double altoCanvas;

        private readonly double anchoFisico;
        private readonly double altoFisico;

        private readonly double cx; // centro X canvas
        private readonly double cy; // centro Y canvas

        private readonly double escalaX; // px por metro
        private readonly double escalaY;

        public TransformadorNEDCanvasEscalado(double headingInicialDeg,
                                              double anchoCanvasPx,
                                              double altoCanvasPx,
                                              double anchoFisicoM,
                                              double altoFisicoM)
        {
            headingInicialRad = headingInicialDeg * Math.PI / 180.0;

            anchoCanvas = anchoCanvasPx;
            altoCanvas = altoCanvasPx;

            anchoFisico = anchoFisicoM;
            altoFisico = altoFisicoM;

            // Centro del canvas en píxeles
            cx = anchoCanvasPx / 2.0;
            cy = altoCanvasPx / 2.0;

            // Escala (pixeles por metro)
            escalaX = anchoCanvasPx / anchoFisicoM;
            escalaY = altoCanvasPx / altoFisicoM;
        }

        /// <summary>
        /// Convierte posición NED (metros) a coordenadas canvas (píxeles).
        /// </summary>
        public (double canvasX, double canvasY) NedACanvas(double xNedM, double yNedM)
        {
            // Rotar según heading inicial
            double verticalM = xNedM * Math.Cos(headingInicialRad) + yNedM * Math.Sin(headingInicialRad);
            double horizontalM = -xNedM * Math.Sin(headingInicialRad) + yNedM * Math.Cos(headingInicialRad);

            // Escalar a píxeles
            double horizontalPx = horizontalM * escalaX;
            double verticalPx = verticalM * escalaY;

            // Transformar al canvas (origen en el centro, Y invertido)
            double canvasX = cx + horizontalPx;
            double canvasY = cy - verticalPx;

            return (canvasX, canvasY);
        }

        /// <summary>
        /// Convierte coordenadas canvas (píxeles) a posición NED (metros).
        /// </summary>
        public (double xNed, double yNed) CanvasANed(double canvasXPx, double canvasYPx)
        {
            // Diferencia desde el centro del canvas
            double horizontalPx = canvasXPx - cx;
            double verticalPx = -(canvasYPx - cy);

            // Escalar a metros
            double horizontalM = horizontalPx / escalaX;
            double verticalM = verticalPx / escalaY;

            // Rotación inversa para volver a NED
            double xNedM = verticalM * Math.Cos(headingInicialRad) - horizontalM * Math.Sin(headingInicialRad);
            double yNedM = verticalM * Math.Sin(headingInicialRad) + horizontalM * Math.Cos(headingInicialRad);

            return (xNedM, yNedM);
        }
    }
}

using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace csDronLink
{
    public class MiJoystick
    {
        Joystick joystick;
        bool joysticActivo = false;
        Dron miDron;
        Action<byte> identificacion;
        public MiJoystick(Dron dron, Action<byte> identificacion = null)
        {

            miDron = dron;
            this.identificacion = identificacion;
            // Inicializa DirectInput
            var directInput = new DirectInput();

            // Busca dispositivos tipo Joystick
            var joystickGuid = Guid.Empty;
            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            // Si no encontró Gamepad, prueba con Joystick
            if (joystickGuid == Guid.Empty)
            {
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;
            }

            if (joystickGuid == Guid.Empty)
            {
                Console.WriteLine("No se encontró ningún joystick conectado.");
                Console.ReadKey();
                return;
            }

            // Crea el joystick
            joystick = new Joystick(directInput, joystickGuid);

            Console.WriteLine($"Usando joystick: {joystick.Information.ProductName}");

            // Configura el buffer de datos
            joystick.Properties.BufferSize = 128;
            joystick.Acquire();
            joysticActivo = true;
            Thread t = new Thread(() => _joystick_loop());
            t.Start();
        }
        public void PararJoystick()
        {
            joysticActivo = false;
        }
        private ushort map (int valor)
        { 
            double x = (valor / 65535.0);
            return (ushort) ((x + 1) * 1000);
        }
        private void _joystick_loop()
        {
            ushort throtlle = 1500;
            ushort yaw = 1500;
            ushort pitch = 1500;
            ushort roll = 1500;

            // Bucle de lectura
            while (joysticActivo)
            {
             
                joystick.Poll();
                var datas = joystick.GetBufferedData();
                foreach (var state in datas)
                {
                    switch (state.Offset)
                    {
                        case JoystickOffset.Buttons0:
                            if (state.Value == 128)
                                miDron.Aterrizar();
                            //else if (state.Value == 0)
                            //    Console.WriteLine("Has soltado botón 0");
                            break;
                        //case JoystickOffset.Buttons1:
                        //    if (state.Value == 128)
                        //        Console.WriteLine("Has pulsado el botón 1");
                        //    else if (state.Value == 0)
                        //        Console.WriteLine("Has soltado botón 1");
                        //    break;
                        case JoystickOffset.Buttons2:
                            if (state.Value == 128)
                                miDron.RTL();
                            //else if (state.Value == 0)
                            //    Console.WriteLine("Has soltado botón 2");
                            break;
                        case JoystickOffset.Buttons3:
                            if (state.Value == 128)
                                miDron.PonModoLoiter();
                        //    else if (state.Value == 0)
                        //        Console.WriteLine("Has soltado botón 3");
                            break;
                        case JoystickOffset.Buttons4:
                            if (state.Value == 128)
                                this.identificacion(miDron.GetId());
                           
                            break;
                            //case JoystickOffset.Buttons5:
                            //    if (state.Value == 128)
                            //        Console.WriteLine("Has pulsado el botón 5");
                            //    else if (state.Value == 0)
                            //        Console.WriteLine("Has soltado botón 5");
                            //    break;
                            //case JoystickOffset.Buttons6:
                            //    if (state.Value == 128)
                            //        Console.WriteLine("Has pulsado el botón 6");
                            //    else if (state.Value == 0)
                            //        Console.WriteLine("Has soltado botón 6");
                            //    break;
                            //case JoystickOffset.Buttons7:
                            //    if (state.Value == 128)
                            //        Console.WriteLine("Has pulsado el botón 7");
                            //    else if (state.Value == 0)
                            //        Console.WriteLine("Has soltado botón 7");
                            //    break;
                            //case JoystickOffset.Buttons8:
                            //    if (state.Value == 128)
                            //        Console.WriteLine("Has pulsado el botón 8");
                            //    else if (state.Value == 0)
                            //        Console.WriteLine("Has soltado botón 8");
                            break;
                        case JoystickOffset.Buttons9:
                            if (state.Value == 128)
                                miDron.Despegar(10);
                            //else if (state.Value == 0)
                            //    Console.WriteLine("Has soltado botón 9");
                            break;
                        //case JoystickOffset.Buttons10:
                        //    if (state.Value == 128)
                        //        Console.WriteLine("Has pulsado el botón 10");
                        //    else if (state.Value == 0)
                        //        Console.WriteLine("Has soltado botón 10");
                        //    break;
                        //case JoystickOffset.Buttons11:
                        //    if (state.Value == 128)
                        //        Console.WriteLine("Has pulsado el botón 11");
                        //    else if (state.Value == 0)
                        //        Console.WriteLine("Has soltado botón 11");
                        //    break;
                        case JoystickOffset.X:
                            yaw = map(state.Value);
                            break;
                        case JoystickOffset.Y:
                            throtlle = map(65535-state.Value);
                            break;
                        case JoystickOffset.Z:
                            roll = map(state.Value);
                            break;
                        case JoystickOffset.RotationZ:
                            pitch = map(state.Value);
                            break;
                        //case JoystickOffset.PointOfViewControllers0:
                        //    if (state.Value == 0)
                        //        Console.WriteLine("Hat N");
                        //    if (state.Value == 9000)
                        //        Console.WriteLine("Hat R");
                        //    if (state.Value == 18000)
                        //        Console.WriteLine("Hat S");
                        //    if (state.Value == 27000)
                        //        Console.WriteLine("Hat W");
                        //    if (state.Value == -1)
                        //        Console.WriteLine("Has soltado el Hat");
                        //    break;
                        default:
                            // Puedes dejarlo vacío o comentar
                            break;
                    }
                }
          

                miDron.SendRC(roll, pitch, throtlle, yaw);
                Thread.Sleep(100);
            }
        }
    }
}

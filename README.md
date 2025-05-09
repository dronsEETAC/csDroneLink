# Librería dronLink para C#    
## 1. Presentación
La librería csDronLink es una versión de la librería dronLink para aplicaciones en C#. El modelo de programación es el mismo que el 
de dronLink, aunque el nombre de las funciones es distinto y en algunos casos los parámetros también.    
     
Este repositorio contiene 4 carpetas:    
* *csDronLink*, que contiene el código de la librería.
* *docs*, que contiene la descripcion de los métodos disponibles en la versión actual  
* *SimpleExample*, que contiene una aplicación de formularios (no tan simple) que usa muchas de las funciones de la librería.     
* *TestConsola*, con una colección de funciones de test para probar muchas de las funciones de la librería. Mirar ese código es la mejor forma de ver cómo usar las funciones de la librería.
     
El repositorio también contiene el fichero *csDronLink.dll*. Este es el fichero que hay que descargar para usar la librería. Basta agregar esta dll como referencia a nuestro proyecto en Visual C# para poder usarla.      
    

## 2. Modelo de programación de csDronLink
csDronLink esta implementada en forma de clase **(la clase Dron)** con sus atributos y una
variedad de métodos para operar con el dron. La clase con los atributos y algunos métodos está definida en el
fichero _Dron.cs_ y el resto de métodos están en los diferentes ficheros con nombres _Dron_xxx_.cs_
(Dron_telemetria.cs, Dron_mision.cs, etc.).

Muchos de los métodos pueden activarse **de forma bloqueante o de forma no bloqueante**. En
el primer caso, **el control no se devuelve al programa que hace la llamada hasta que la
operación ordenada haya acabado**. Si la llamada es no bloqueante entonces **el control se
devuelve inmediatamente** para que el programa pueda hacer otras cosas mientras se realiza la
operación.

Un buen ejemplo de método con estas dos opciones es _Despegar_, que tiene la siguiente cabecera:

```
public void Despegar(int altitud, Boolean bloquear = true, Action<object> f = null, object param = null)
```

Al llamar a este método hay que pasarle como parámetro la **altura de despegue**. Por defecto la
operación es **bloqueante**. Por tanto, el programa que hace la llamada no se reanuda hasta que 
el dron esté a la altura indicada. En el caso de usar la opción no bloqueante se puede indicar el
nombre de la función que queremos que se ejecute cuando la operación haya acabado (función a la que
llamamos habitualmente **callback**). En el caso de _Despegar_, cuando el dron  esté a la altura indicada
se activará la funcion callback que se le pasa como parámetro _f_. Incluso podemos indicar un parámetro que queremos que
se le pasen a ese callback  en el momento en que se active. 

Los siguientes ejemplos aclararán estas cuestiones.

_Ejemplo 1_

```
dron = Dron();
dron.Conectar ("simulacion"); // me conecto al simulador
Console.WriteLine ("Conectado");
dron.Despegar (8);
Console.WriteLine ("En el aire a 8 metros de altura")
```

En este ejemplo todas las llamadas son bloqueantes.


_Ejemplo 2_

```
dron = Dron();
dron.Conectar ("simulacion"); // me conecto al simulador
Console.WriteLine ("Conectado");
dron.Despegar (8, bloquear : false); // llamada no bloqueante, sin callback
Console.WriteLine ("Hago otras cosas mientras se realiza el despegue");
```
En este caso la llamada no es bloqueante. El programa continua su ejecución 
mientras el dron  realiza el despegue. 

_Ejemplo 3_

```
dron = Dron();
dron.Conectar ("simulacion"); // me conecto al simulador
Console.WriteLine ("Conectado");
dron.Despegar (8, bloquear : false, f: EnAire); // llamada no bloqueante, con callback
Console.WriteLine ("Hago otras cosas mientras se realiza el despegue");
```
De nuevo una llamada no bloqueante. Pero en este caso le estamos indicando que cuando 
el dron esté a la altura indicada ejecute la función EnAire. Esta función debe estar declarada,
por ejemplo asi:
```
private void EnAire (object param=null)
{
    Console.WriteLine ("El dron está en el aire");
}
```

       
_Ejemplo 4_

```
dron = Dron();
dron.Conectar ("simulacion"); // me conecto al simulador
Console.WriteLine ("Conectado");
dron.Despegar (8, bloquear : false, f:EnAire, param: "En el aire"); // llamada no bloqueante, con callback y parámetro
Console.WriteLine ("Hago otras cosas mientras se realiza el despegue");
```
De nuevo una llamada no bloqueante. Pero en este caso le estamos indicando un callback que tiene un parámetro. La función
de callback puede ser esta
```
private void EnAire (object texto)
{
    Console.WriteLine ((string) texto);
}
```
La modalidad no bloqueante en las llamadas a la librería es especialmente útil **cuando
queremos interactuar con el dron mediante una interfaz gráfica**. Por ejemplo, no vamos a
querer que se bloquee la interfaz mientras el dron ejecuta una operación de movimiento. Seguramente querremos seguir
procesando los datos de telemetría mientras el dron se mueve, para mostrar al usuario, por
ejemplo, la posición del dron en el mapa.     
   
 ## 3. Videos explicativos
 En este video se explica cómo está organizada la librería (cosa que puede ser útil para el desarrollador de código para librería) y se muestran en funcionamiento los programas de test (esto será útil para el usuario de la librería). [![DroneEngineeringEcosystem Badge](https://img.shields.io/badge/DEE-csDronLink_organización-pink.svg)](https://youtu.be/1axWKmvyCt4-)        

 Este vídeo muestra en funcionamiento la aplicación de ejemplo (Simple Example). [![DroneEngineeringEcosystem Badge](https://img.shields.io/badge/DEE-csDronLink_demoAplicación-pink.svg)](https://youtu.be/uCsa8FmVrcI-)        
       
 Y aquí se explica brevemente el código de esa aplicación.   [![DroneEngineeringEcosystem Badge](https://img.shields.io/badge/DEE-csDronLink_códigoAplicación-pink.svg)](https://youtu.be/MuSE1AkeNsw-)        
   
 Este vídeo muestra cómo crear una aplicación super sencilla desde cero, usando la librería. [![DroneEngineeringEcosystem Badge](https://img.shields.io/badge/DEE-csDronLink_demoDesdeCero-pink.svg)](https://youtu.be/I4_2nhkEZI8-)       

 ## 4. Trabajo futuro   
 Lo que hay aquí es una versión operativa pero muy preliminar de la librería. Hay mucho trabajo por delante, no solo para ampliar sus funcionalidades, sino también para mejorar las que tiene ya. Algunas mejoras pendientes son estas:
 * Incorporar a la misión elementos tales como la altura de cada waypoint, o hacer que el dron pare un tiempo o cambie de heading en cada waypoint.
 * Hacer que en la operación de volar aquí se pueda indicar la altura del waypoint de destino.

También se pueden introducir mejoras en la aplicación de ejemplo:
* Hacerla más robusta. Por ejemplo, peta si no se elije el modo (simulación o producción)
* Cambiar los colores de los botores según lo que haga el usuario (por ejemplo, al acabar de ejecutar la misión hacer que los botones de cargar y ejecutar vuelvan al color naranja)
* Añadir la velocidad y el nivel de batería a los datos de telemetría
* Diseñar el escenario, igual que se diseña el plan de vuelo.


# Procedural Terrain

Generador de Terrenos Procedurales

[Diagrama del Sistema](https://viewer.diagrams.net/?tags=%7B%7D&highlight=0000FF&edit=_blank&layers=1&nav=1&title=Terrain%20Generator.drawio#Uhttps%3A%2F%2Fdrive.google.com%2Fuc%3Fid%3D1KO6204giaVHnVrKZILEUBGv1iPFyigEg%26export%3Ddownload)

## GENERACIÓN del Mapas de Alturas

El Mapa de Alturas se genera con **Ruido de Perlin**.
Para modificar el resultado se ajustan los parámetros del ruido generado.
Los **octavos** se añaden sobre el ruido para dar mayor calidad, y son remuestreados del ruido modificando la función de onda. Siendo la **persistencia** proporcional a la amplitud de cada octavo, es decir, la influencia de cada octavos sobre el mapa. Y la **lacunarity** proporcional a la frecuencia de cada octavo, el cual añade detalle y genera un terreno más irregular.

### Explicación detallada del Perlin Noise

<https://www.redblobgames.com/articles/noise/introduction.html>

### Aplicación del Perlin Noise a la Generación de Terreno

<https://www.redblobgames.com/maps/terrain-from-noise/>

## Generación de Texturas

Se crea evaluando el mapa de alturas con un gradiente de color.

## Generación de Malla

La Malla se genera muestreando el ruido como vértices, y añadiendo los triángulos, normales y uvs necesarios para renderizar.

Unity tiene limitaciones en la cantidad de vértices que puede tener una Malla, por lo que tiene un tamaño límite. En concreto 128x128 (27)
Tenemos 2 opciones. Insertar los mapas de alturas en un Terrain de Unity que es un objeto nativo, y adaptarnos a él. O crear un Generador Infinito de Terreno por submallas que llamaremos “Chunk”.

## Generador Infinito de Terreno por Chunks

Este sistema genera, centrado en un punto, únicamente las mallas del terreno que estén hasta una máxima distancia del punto central, los Chunks visibles. Y además, según la distancia al punto central reduce la resolución de la malla, lo cual se conoce como Nivel de Detalle o LoD (Level of Detail).

Actualiza el terreno de forma dinámica, si es que el centro no es estático. Actualizando tanto el LoD, como generando nuevos Chunks en la dirección adonde el centro se mueve, y escondiendo los Chunks que se dejan atrás y superan la máxima distancia.
Todo esto busca mejorar el rendimiento del generador, tanto reduciendo la carga de renderizado, como el tiempo de generación.

## Aplicación en el Terrain de Unity

Por otra parte, Unity ya nos brinda un objeto nativo que genera un terreno a partir de un mapa de alturas, y modifica la malla por medio de submallas, aplicándoles un LoD proporcional a la cercanía con la cámara. El funcionamiento es similar a mi implementación de terreno infinito, salvo por la generación infinita y dinámica, que se podría implementar.

El tiempo de generación es muy similar.
Pero ahora solo nos encargamos de generar el Mapa de Alturas, y Unity se encarga de generar la malla y optimizarla. Es mucho más sencillo y el resultado es de mayor calidad.

Y el tamaño máximo del terreno es muchísimo mayor, hasta 4096x4096 (212), a la orden de 25 veces por encima de la Malla anterior. Pero esto es gracias a las submallas con distinto LoD.

Para mi sorpresa, el generador infinito es más rápido en comparación con el que usa Terrenos de Unity.
Esto se puede deber a que en mi generador infinito la superficie de generación es circular tomando la distancia máxima como un radio, y en el Terreno de Unity funciona con la anchura del terreno, teniendo una superficie cuadrada.

Extrapolando el tiempo que tarda el generador para 1 Chunk, 12ms, de 128x128, el equivalente a un terreno de 4096x4096, serían 32x32 Chunks = 1024 Chunks.
En total tardaría 12.288ms en generar la misma superficie de 4096x4096.
La diferencia con los 13.000ms del otro generador es muy pequeña, y podemos considerar que debido a la mayor calidad del Terreno de Unity, realiza algunos procesos de postprocesado adicionales, los cuales, si los ignorábamos, probablemente tendría un mejor tiempo que el Generador Infinito.

Dado que la generación de Terrenos de Unity es más sencilla, no conlleva mucha diferencia con el Generador Infinito que he creado, y nos ahorra un tiempo de desarrollo y mantenimiento, debido a que es una utilidad de Unity nativa y estable.

## Árboles y otros Obstaculos Naturales

Estoy desarrollando un sistema de población del terreno con árboles, arbustos, y otros obstáculos naturales.

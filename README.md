# SignBridge

Aprende el alfabeto de la Lengua de Señas Americana (ASL) con tu cámara y convierte palabras deletreadas en voz.
Aplicación .NET 10 MAUI para **Windows** y **Android**. Toda la IA se ejecuta **en el dispositivo** con ONNX Runtime: sin internet y sin que ninguna imagen salga del equipo.

## Funciones

- **Practicar**: la app muestra una letra, el dibujo de la seña y cómo formarla. La haces frente a la cámara y te califica en tiempo real (puntos, racha y mejor racha). Recorre todas las letras, en orden aleatorio, antes de repetir alguna.
- **Señas a voz**: deletreas una palabra letra por letra; la app muestra la seña que entendió (con su dibujo de referencia) y lee la palabra en voz alta con la voz sin conexión del dispositivo (en español cuando está disponible).
- **Lo que ve la IA**: esqueleto en vivo de los 21 puntos de la mano detectados, en miniatura o dibujado sobre tu mano (botón ✋).
- Funciona con la mano derecha y con la izquierda.

## Demo

[Video: deletreo de "MATIAS" y lectura en voz alta](docs/SignBridge-demo-MATIAS.mp4) (34 s). Es el motor real de la app (`SignBridge.Core`, mismo modelo y mismos umbrales) procesando fotos del dataset ASL Letters, dibujado con la interfaz de Señas a voz; la voz es la de Windows (español de México).

## Cómo funciona

```
cuadro de cámara ──► detector de palma ──► modelo de puntos de la mano ──► 63 rasgos ──► clasificador MLP ──► letra + probabilidad
                     (MediaPipe, ONNX)      (MediaPipe, 21 puntos)          (relativos a la     (26 letras)
                                                                              muñeca, sin escala)
```

El clasificador solo ve la **forma de la mano**, nunca el fondo, la ropa ni el color de piel.
Esa es la decisión de diseño clave; ver *Evaluación de modelos* más abajo.

Arquitectura (hexagonal, puertos y adaptadores):

| Proyecto | Rol |
|---|---|
| `src/SignBridge.Core` | Dominio y reconocimiento, sin interfaz: `ISignRecognizer` (puerto), `LandmarkSignRecognizer` (adaptador), `PracticeSession`, `WordBuilder`, `AslAlphabet`. |
| `src/SignBridge` | App MAUI: páginas, view models (MVVM Toolkit), ciclo de cámara, texto a voz. Depende solo del puerto; `RecognizerProvider` es el único lugar que elige el adaptador. |
| `tests/SignBridge.Core.Tests` | Pruebas xUnit, incluidas pruebas de paridad del pipeline en C# contra las salidas de referencia en Python. |
| `tools/landmark_reference.py` | Regenera las salidas de referencia de Python que usan las pruebas de paridad. |

## Cómo ejecutarla

Requisitos: SDK de .NET 10 y Visual Studio 2026 (Insiders) con la carga de trabajo **.NET MAUI**.

```bash
git clone https://github.com/mattiasribera/SignBridge.git
```

**Windows**
1. Abre `SignBridge.slnx`.
2. Elige `SignBridge` como proyecto de inicio, selecciona **Windows Machine** y presiona **F5**.
3. Permite el acceso a la cámara si Windows lo pide (Configuración › Privacidad › Cámara › *Permitir que las aplicaciones de escritorio accedan a la cámara*).

**Android (depuración USB, sin firmar APK)**
1. En el teléfono: activa las *Opciones de desarrollador* y la *Depuración USB*, conéctalo por USB y acepta el aviso.
2. En Visual Studio elige el teléfono en **Android Local Devices** y presiona **F5**.
3. Requiere Android 7.0 (API 24) o superior.

Línea de comandos:

```bash
dotnet test tests/SignBridge.Core.Tests
dotnet build src/SignBridge -f net10.0-windows10.0.19041.0
dotnet build src/SignBridge -f net10.0-android
```

## Evaluación de modelos (por qué puntos de la mano)

Todos los candidatos se probaron **entre datasets distintos**: con personas y fondos que el modelo nunca vio al entrenar.

| Enfoque | Personas nuevas (ASL-HG P9–P10) | Fotos en ambientes reales (ASL Letters) | Tamaño |
|---|---|---|---|
| Clasificador de imagen EfficientNet-B0 (primer intento) | 16,8 % | 38–48 % | 16 MB |
| Clasificadores de imagen SigLIP2 / ViT | 44–48 % | 45–52 % | 330+ MB |
| Puntos de la mano + MLP v1 | 87,1 % | 71,0 % | ~8 MB |
| **Puntos de la mano + MLP v2 (incluido)** | **91,1 %** | **80,3 %** | ~8 MB |

El primer modelo llegaba al 99 % en su propio dataset, pero fallaba con webcams reales: sus datos eran de una sola persona
frente a un fondo gris, así que aprendió la escena en lugar de la mano. Lección: **evaluar siempre con datos de otras personas.**

La versión v2 se entrenó con más personas y con copias espejadas de cada muestra (por eso reconoce ambas manos).
Las cifras de v2 se midieron con particiones reservadas antes del entrenamiento final, que luego usó todos los datos.

Límites conocidos de este piloto:
- Solo el alfabeto ASL (no LSB, Lengua de Señas Boliviana). El reconocedor es un puerto, así que un modelo LSB puede reemplazarlo sin tocar la interfaz.
- La J y la Z requieren movimiento; un modelo de un solo cuadro no puede juzgarlas, por eso Practicar las omite.
- La precisión con webcams reales todavía está lejos de ser perfecta; ayudan la buena luz y la mano completa a la vista.

## Créditos y licencias

- Modelos de detección de palma y de puntos de la mano: [OpenCV Zoo](https://huggingface.co/opencv) `palm_detection_mediapipe` y `handpose_estimation_mediapipe` (Apache-2.0), derivados de Google MediaPipe.
- Clasificador de puntos de la mano entrenado con:
  - [juanjodurillo/asl-hg](https://huggingface.co/datasets/juanjodurillo/asl-hg) (CC-BY-4.0)
  - [EitanG98/asl_letters](https://huggingface.co/datasets/EitanG98/asl_letters) (Unlicense)
  - [ronibandini/AmericanSignLanguage](https://huggingface.co/datasets/ronibandini/AmericanSignLanguage) (MIT)
  - [aliciiavs/sign_language_image_dataset](https://huggingface.co/datasets/aliciiavs/sign_language_image_dataset), [Hemg/sign_language_dataset](https://huggingface.co/datasets/Hemg/sign_language_dataset) y [niki-stha/asl-detection-roboflow](https://huggingface.co/datasets/niki-stha/asl-detection-roboflow): **no declaran licencia**; se usaron solo con fines académicos. Para un uso comercial habría que reentrenar sin ellos (el modelo v1 no los usa).
- Las fotos de prueba son de `EitanG98/asl_letters`.
- Dibujos de las señas: serie *Sign language A–Z* de Wikimedia Commons (dominio público).
- Tipografía [Atkinson Hyperlegible](https://github.com/googlefonts/atkinson-hyperlegible) (Braille Institute, SIL Open Font License 1.1).
- Íconos [Material Symbols Rounded](https://github.com/google/material-design-icons) (Google, Apache-2.0), reducidos a los íconos usados.

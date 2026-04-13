# HPIS_APP - Guía para Desarrolladores 🚀

Este documento está diseñado para familiarizar a cualquier desarrollador con la arquitectura, el funcionamiento interno y la estructura del proyecto **HPIS_APP**.

---

## 📌 1. ¿Qué es la aplicación?

**HPIS_APP** es una aplicación inmersiva de Realidad Virtual (VR) construida en **Unity** y diseñada para visores **Meta Quest**. Su propósito principal es recibir retroalimentación (feedback) desde un servidor externo a través de WebSockets, y representar esto dinámicamente en el entorno del usuario a través de paneles visuales (UI Canvas), animaciones 3D, pistas de audio espaciales y agentes conversacionales apoyados en Modelos de Lenguaje Grande (LLMs) multimodal.

Es ideal para escenarios educativos, de simulación o de asistencia en vivo, donde un usuario de VR requiere instrucciones visuales o auditivas precisas superpuestas en su entorno físico/virtual.

---

## ⚙️ 2. Arquitectura General y Flujo de Datos

La aplicación sigue una arquitectura impulsada por eventos (event-driven) reactiva a la conexión de red:

1. **Recepción de Datos:** `WebSocketClient` se conecta a un servidor local (por defecto `192.168.4.2:7890`). Recibe tramas de texto en formato JSON.
2. **Procesamiento de Datos:** El JSON es parseado y procesado por `DataManager`. Este actúa como el "cerebro" o controlador central, interpretando qué tipo de evento llegó (una animación, un mensaje de LLM, un sonido).
3. **Ejecución y Presentación:** Dependiendo del contenido:
   - `FeedbackManager` se encarga de cambiar los menús UI, mostrar ventanas emergentes, encender flechas direccionales o disparar animaciones 3D.
   - `HpisLlmAgent` entra en juego si el prompt involucra inteligencia artificial conversacional, a veces apoyado por síntesis de voz (Text-to-Speech) y captura de la cámara Passthrough (multimodalidad).

---

## 📁 3. Estructura del Proyecto (Dónde encontrar qué)

El proyecto está configurado bajo la estructura típica de Unity. Lo más importante se encuentra en la carpeta `Assets/Scripts`:

*   **`Assets/Scripts/Core`**: Lógica central de la aplicación.
*   **`Assets/Scripts/LLMHPIS`**: Lógica relacionada a la integración de IA y LLM.
*   **`Assets/Prefabs`**: Modelos 3D y Canvases UI listos para instanciar en escena.
*   **`Assets/MetaXR` y `Assets/Oculus`**: SDKs nativos de Meta para interacción, Passthrough, hand-tracking y anclaje espacial (`OVRSpatialAnchor`).

---

## 🛠️ 4. Explicación de los Scripts Principales

Si te piden hacer modificaciones, estos son los archivos clave y lo que hace cada uno:

### Red y Datos
*   **`WebSocketClient.cs`**: Maneja la conexión asíncrona hacia el backend de control usando la librería `WebSocketSharp`. Maneja bucles de reconexión y delega los JSON recibidos al Thread Principal. 
    *   *Modifícalo si:* Necesitas cambiar protocolos de red, añadir autenticación o headers, o si el servidor cambia de IP/Puerto (se guarda en `ConnectionConfig.json` persistente).
*   **`DataManager.cs`**: Transforma el string gigante de JSON del WebSocker en objetos utilizables de C# (`JsonUtility` / `Newtonsoft`). Valida la información y enruta a los managers correspondientes.
    *   *Modifícalo si:* Se agragan nuevas variables o "comandos" en el JSON enviado desde el backend (por ejemplo, añadir un nuevo tipo de feedback que antes no existía).

### Feedback Visual y Espacial
*   **`CanvasAnchorPlacer.cs`**: Script vital que permite al usuario (o desarrollador en Editor mediante teclado/mouse) posicionar en el espacio los paneles y puntos de anclaje antes de o durante la experiencia. Responde a los joysticks (Meta Quest) o a `W/A/S/D` y Mouse en el PC.
    *   *Modifícalo si:* Quieres cambiar cómo el usuario interactúa para arrastrar los paneles de UI o cambiar cómo funciona el "Gatillo + Joystick" para escalar.
*   **`FeedbackManager.cs`**: El controlador de vistas más pesado de la app. Tiene las referencias a los paneles (Canvas), iconos, textos e instancias 3D. Muestra/oculta elementos según lo determine el `DataManager`.
    *   *Modifícalo si:* Quieres añadir nuevas animaciones, alterar las transiciones visuales, modificar qué colores/imágenes se muestran al detectar ciertos eventos o integrar un nuevo prefab visual 3D.
*   **`UIManager.cs` / `UIReferenceHolder.cs`**: Scripts de apoyo que simplemente contienen las referencias hacia los objetos de interfaz (Botones, Textos de estado de conexión, LEDs de colores).

### Inteligencia Artificial (LLM)
*   **`HpisLlmAgent.cs` / `HpisLlmAgentHelper.cs`**: Módulos que heredan / envuelven el `LlmAgent` del Meta SDK. Su principal fusión es recibir prompts del servidor y dárselos al modelo local/remoto de Meta. Tiene la capacidad de interceptar la cámara (Passthrough Camera `MRUK`) del Quest para enviar imágenes y texto de forma multimodal. Transforma el retorno de texto a voz (Texto-to-Speech). Se añadió el soporte de poder usar `.StopSpeaking()` cuando el cliente cancela la interacción.
    *   *Modifícalo si:* Cambias de proveedor de IA (Pasar de Llama local a OpenAI / Claude, etc.), o alterar la configuración del prompt del sistema (System Prompt).

### Cámara
*   **`CameraDisplay.cs` / `PiPDisplayController.cs`**: Manejables para habilitar un Picture-in-Picture (PiP). Muestran una cámara secundaria o información superpuesta para monitoreo general.

---

## 🏃 5. Guía de Tareas Comunes para el Desarrollador

Aquí tienes ejemplos rápidos de cómo abordarias ciertos incidentes o cambios de requerimientos:

▶ **Tarea: La IP del servidor cambió y la App ya en Quest no se conecta.**
*   **Solución:** Al ejecutar la primera vez, `WebSocketClient.cs` crea un archivo `ConnectionConfig.json` en `Application.persistentDataPath` del Quest. Utiliza SideQuest o Meta Quest Developer Hub para editar ese archivo, no hace falta re-compilar. Si vas a re-compilar, cambia el valor base publico `serverIp` en el inspector del GameObject que tiene al WebSocketClient o en el código.

▶ **Tarea: Se necesita añadir soporte para un nuevo campo "color" que llega desde el JSON del servidor.**
1.  Abre `DataManager.cs` e identifica la clase modelo que deserializa la data. Agrégale `public string color;`
2.  Abre `FeedbackManager.cs`, ubica la función que aplica los visuales y procesa la información leyendo la nueva propiedad desde el JSON mapeado, ejemplo: `if(data.color == "red") miTexto.color = Color.red;`

▶ **Tarea: Alterar qué modelo 3D sale cuando el profe manda una "Alerta".**
*   **Solución:** Revisa el Prefab principal que agrupa tus Canvas (probablemente referenciado en `FeedbackManager.cs`). En el script `FeedbackManager.cs`, habrá una referencia serializada al GameObject de esa animación o a la lista de Prefabs a instanciar. Lo cambias desde el Inspector de Unity.

▶ **Tarea: El Agente IA no detiene su voz cuando el proceso se cancela.**
*   **Solución:** Ya existe la función `StopSpeaking()` introducida en `HpisLlmAgent.cs`. Asegúrate que en `DataManager.cs` o donde se maneje el evento de cerrado/cancelación se invoque `llmAgent.StopSpeaking();`.

---

## 📝 6. Consejos Adicionales (Editor vs VR)
Gran parte de la usabilidad se compiló utilizando directivas de preprocesador `#if UNITY_EDITOR`. El script `CanvasAnchorPlacer.cs` se comportará como un juego de PC con teclado y click de ratón en el Editor de Unity, pero al ser buildeado como APK compilará la versión para Controles de Meta Quest, con soporte a Joysticks y gatillos nativos. Si creas mecánicas nuevas, intenta siempre diseñar dos controles: uno de debuggueo (Mouse/Teclado) y uno real (Oculus OVRInput).

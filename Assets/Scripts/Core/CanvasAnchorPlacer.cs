using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace HPIS.Anchors
{
    /// <summary>
    /// Instancia un prefab de canvas, permite al usuario posicionarlo en tiempo real
    /// siguiendo un transform (como un controlador). Al presionar 'A' (OVRInput.Button.One), 
    /// crea un OVRSpatialAnchor, y conecta el UIReferenceHolder a los managers.
    /// Al presionar 'B' (OVRInput.Button.Two), destruye el ancla y reinicia el proceso.
    /// El joystick derecho ajusta la escala (izq/der) y la profundidad (arr/abj).
    /// </summary>
    public class CanvasAnchorPlacer : MonoBehaviour
    {
        [Header("Prefab del Canvas")]
        [Tooltip("Arrastra aquí tu prefab del Canvas que contiene el UIReferenceHolder.")]
        [SerializeField] private GameObject canvasPrefab;

        [Header("Controlador")]
        [Tooltip("El transform del controlador que guiará el posicionamiento del canvas.")]
        [SerializeField] private Transform controllerTransform;

        [Header("Posicionamiento Canvas")]
        [Tooltip("Desplazamiento posicional local del canvas respecto al origen del controlador.")]
        [SerializeField] private Vector3 canvasPositionOffset = new Vector3(0.0f, 0.1f, 0.5f);

        [Tooltip("Desplazamiento rotacional local del canvas respecto a la orientación del controlador.")]
        [SerializeField] private Vector3 canvasRotationOffset = new Vector3(15.0f, 0.0f, 0.0f);

        [Header("Ajustes en Tiempo Real")]
        [Tooltip("Velocidad con la que cambia la escala con el joystick.")]
        [SerializeField] private float scaleSpeed = 0.6f;
        [Tooltip("Velocidad con la que cambia la profundidad (eje Z) con el joystick.")]
        [SerializeField] private float depthSpeed = 0.6f;

        [Header("Animation Anchor")]
        [Tooltip("El transform que contendrá el anchor para las animaciones")]
        [SerializeField] private Transform animationAnchorContainer;
        [Header("Animation Anchor Auto-Positioning")]
        [Tooltip("Desplazamiento posicional del anchor de animación respecto a la pantalla de video del canvas.")]
        [SerializeField] private Vector3 animationAutoPositionOffset = new Vector3(0f, -1.39f, 0f);

        [Tooltip("Desplazamiento rotacional del anchor de animación respecto a la pantalla de video del canvas.")]
        [SerializeField] private Vector3 animationAutoRotationOffset = Vector3.zero;

        [Tooltip("Escala del anchor de animación.")]
        [SerializeField] private Vector3 animationAutoScale = new Vector3(0.43f, 0.43f, 0.43f);

        private GameObject _canvasInstance;
        private bool _isAnchored = false;
        
        private GameObject _animationAnchorObject;
        private bool _isAnimationAnchored = false;

        // Variables para la escala
        private Vector3 _initialScale;
        private float _currentScaleMultiplier = 1.0f;

        void Start()
        {
            if (canvasPrefab == null || controllerTransform == null)
            {
                Debug.LogError("[CanvasAnchorPlacer] El prefab del Canvas o el Transform del controlador no están asignados. Desactivando script.");
                enabled = false;
                return;
            }
            
            StartPlacement();
        }

        private void ApplyOffset()
        {
            if (_canvasInstance == null || controllerTransform == null) return;

            Quaternion targetRotation = controllerTransform.rotation * Quaternion.Euler(canvasRotationOffset);
            Vector3 targetPosition = controllerTransform.position + controllerTransform.TransformDirection(canvasPositionOffset);

            _canvasInstance.transform.SetPositionAndRotation(targetPosition, targetRotation);
        }



        void Update()
        {
#if UNITY_EDITOR
            // Lógica para el editor de Unity (sin gafas)
            if (Keyboard.current.spaceKey.wasPressedThisFrame) // Anclar con Barra Espaciadora
            {
                if (!_isAnchored)
                {
                    PlaceAndAnchorCanvas();
                }
            }

            if (Keyboard.current.rKey.wasPressedThisFrame) // Resetear con 'R'
            {
                ResetPlacement();
            }

            if (!_isAnchored && _canvasInstance != null)
            {
                EditorUpdateCanvasPosition();
            }
#else
            // Lógica para el dispositivo (Meta Quest)
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                if (!_isAnchored)
                {
                    PlaceAndAnchorCanvas();
                }
            }
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                if (_isAnchored)
                {
                    ResetPlacement();
                }
            }
            if (!_isAnchored && _canvasInstance != null)
            {
                HandleCanvasJoystickInput();
                ApplyOffset();
            }
#endif

            // Actualización en tiempo real del offset para facilitar el ajuste desde el Inspector de Unity
            if (_isAnimationAnchored && _animationAnchorObject != null && _canvasInstance != null)
            {
                // Al estar emparentado, los offsets ahora son valores locales relativos a la escala del Canvas.
                // Ya no necesitamos multiplicar por _currentScaleMultiplier, el motor de Unity lo hace automáticamente.
                _animationAnchorObject.transform.localPosition = animationAutoPositionOffset;
                _animationAnchorObject.transform.localScale = animationAutoScale;
                // No actualizamos la rotación constantemente para respetar las rotaciones dinámicas del JSON (FeedbackManager)
            }
        }

#if UNITY_EDITOR
        private void EditorUpdateCanvasPosition()
        {
            if (_canvasInstance == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Posiciona el canvas en el punto de colisión
                _canvasInstance.transform.position = hit.point;
                // Orienta el canvas para que mire hacia la cámara, alineado con la normal de la superficie
                _canvasInstance.transform.rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0, 180, 0);
            }
            else
            {
                // Si no hay colisión, posiciona el canvas a una distancia fija de la cámara
                _canvasInstance.transform.position = ray.GetPoint(10.0f);
                _canvasInstance.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            }
        }
#endif



        private void HandleCanvasJoystickInput()
        {
            Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

            // Izquierda/Derecha: Cambiar la escala del canvas
            if (Mathf.Abs(thumbstick.x) > 0.1f)
            {
                _currentScaleMultiplier += thumbstick.x * scaleSpeed * Time.deltaTime;
                _currentScaleMultiplier = Mathf.Max(0.1f, _currentScaleMultiplier);
                _canvasInstance.transform.localScale = _initialScale * _currentScaleMultiplier;
            }

            // Arriba/Abajo: Cambiar la profundidad (offset en Z) del canvas
            if (Mathf.Abs(thumbstick.y) > 0.1f)
            {
                canvasPositionOffset.z += thumbstick.y * depthSpeed * Time.deltaTime;
            }
        }



        private void StartPlacement()
        {
            _isAnchored = false;
            _canvasInstance = Instantiate(canvasPrefab, Vector3.zero, Quaternion.identity);
            _canvasInstance.name = "CanvasPreview";
            // Guardar la escala inicial y resetear el multiplicador
            _initialScale = _canvasInstance.transform.localScale;
            _currentScaleMultiplier = 1.0f;
#if !UNITY_EDITOR
            ApplyOffset();
#endif
            Debug.Log("[CanvasAnchorPlacer] Mueve el mouse para posicionar el canvas. 'Espacio' para anclar. 'R' para reiniciar.");
        }

        private void AutoPlaceAnimationAnchor(UIReferenceHolder uiHolder)
        {
            if (_animationAnchorObject != null)
            {
                Destroy(_animationAnchorObject);
            }

            _animationAnchorObject = new GameObject("AnimationStaticRoot");

            Transform targetTransform = _canvasInstance.transform;

            // Emparentamos DE FORMA PERMANENTE al Canvas root en lugar del videoDisplay. 
            // Si lo emparentamos al videoDisplay y este se oculta, la animación se apagaría por error.
            _animationAnchorObject.transform.SetParent(targetTransform, false);

            _animationAnchorObject.transform.localPosition = animationAutoPositionOffset;
            _animationAnchorObject.transform.localRotation = Quaternion.Euler(animationAutoRotationOffset);
            _animationAnchorObject.transform.localScale = animationAutoScale;

            _isAnimationAnchored = true;

            var feedbackManager = FindFirstObjectByType<FeedbackManager>();
            if (feedbackManager != null)
            {
                feedbackManager.SetAnimationAnchor(_animationAnchorObject.transform);
            }

            Debug.Log("[CanvasAnchorPlacer] Animation anchor posicionado automáticamente en la pantalla de video.");
        }

        private void PlaceAndAnchorCanvas()
        {
            if (_canvasInstance == null || _isAnchored) return;

            _isAnchored = true;
            Debug.Log("[CanvasAnchorPlacer] Posición fijada. Creando ancla y conectando managers...");
            var anchor = _canvasInstance.AddComponent<OVRSpatialAnchor>();
            _canvasInstance.name = "AnchoredCanvas_HPIS";
            
            var uiHolder = _canvasInstance.GetComponent<UIReferenceHolder>();
            if (uiHolder == null)
            {
                Debug.LogError("[CanvasAnchorPlacer] ¡Crítico! El prefab del canvas no tiene UIReferenceHolder. No se pueden conectar los managers.");
                return;
            }

            LinkManagers(uiHolder);

            // Colocar automáticamente el ancla de animación
            AutoPlaceAnimationAnchor(uiHolder);
        }

        public void ResetPlacement()
        {
            if (_canvasInstance != null)
            {
                Destroy(_canvasInstance);
            }
            
            Debug.Log("[CanvasAnchorPlacer] Ancla anterior destruida. Reiniciando posicionamiento.");
            StartPlacement();
        }

        private void LinkManagers(UIReferenceHolder uiHolder)
        {
            var dataManager = FindFirstObjectByType<DataManager>();
            if (dataManager != null) { dataManager.uiHolder = uiHolder; }

            var wsClient = FindFirstObjectByType<WebSocketClient>();
            if (wsClient != null) { wsClient.uiHolder = uiHolder; }

            var uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null) { uiManager.uiHolder = uiHolder; }

            var feedbackManager = FindFirstObjectByType<FeedbackManager>();
            if (feedbackManager != null) { feedbackManager.uiHolder = uiHolder; }
            
            Debug.Log("Todos los managers han sido conectados al nuevo canvas.");
        }
    }
}
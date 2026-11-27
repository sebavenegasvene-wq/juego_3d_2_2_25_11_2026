using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class movimiento : MonoBehaviour
{
    private Rigidbody rb;
    private int contador_coleccionables = 0;
    public Text puntaje;

    // Movimiento con aceleración
    public float velocidadCaminar = 8f;
    public float velocidadCorrer = 14f;
    public float aceleracion = 30f;
    public float frenado = 15f;
    public float controlEnAire = 0.5f;

    // Impulso de velocidad (poder "IMPULSO")
    public float multiplicadorBoost = 1.5f;   // 1.5 = 50% más rápido
    public float duracionBoost = 5f;          // segundos que dura
    public GameObject contenedorBarra;        // fondo de la barra (opcional, con Canvas)
    public Image barraRelleno;                // la barra que se vacía (opcional, con Canvas)
    private float tiempoBoost = 0f;

    // Indicador de impulso dibujado por código (sin Canvas)
    public bool mostrarIndicadorPantalla = true;
    private GUIStyle estiloTexto;
    private float tiempoDesdeActivacion = 99f;

    // ───────────── VIDAS Y CHECKPOINTS ─────────────
    [Header("Vidas y checkpoints")]
    public int vidasMaximas = 3;
    public int danoObstaculo = 1;
    public int danoVacio = 2;
    public bool usarAlturaVacio = false;      // opcional: caer por debajo de esta altura cuenta como vacío
    public float alturaVacio = -2000f;        // OJO: tu destino de TP está en y = -1105, usa un valor más bajo
    private int vidas;
    private int ultimoDano = 1;
    private Vector3 puntoControl;
    private bool derrotado = false;
    private float tiempoDesdeDerrota = 0f;
    private float tiempoCheckpoint = 99f;
    private const int INDICE_VIDA = 2;

    // ───────────── GAME OVER (estilo Super Mario 64, dibujado por código) ─────────────
    [Header("Game Over (estilo Mario 64)")]
    public string textoGameOver = "GAME OVER";
    public float duracionIris = 1.1f;            // segundos que tarda en cerrarse el círculo
    public float retrasoLetrasGameOver = 0.15f;  // pausa en negro antes de la primera letra
    public float intervaloLetras = 0.12f;        // tiempo entre una letra y la siguiente
    public AudioClip sonidoGameOver;             // opcional: jingle de derrota
    [Range(0f, 1f)] public float volumenGameOver = 1f;
    private Vector2 irisCentro;
    private Texture2D texturaIris;
    private static readonly Color[] coloresLetraGameOver =
    {
        new Color(0.95f, 0.15f, 0.15f),  // rojo
        new Color(1.00f, 0.85f, 0.10f),  // amarillo
        new Color(0.20f, 0.75f, 0.30f),  // verde
        new Color(0.20f, 0.50f, 1.00f),  // azul
        new Color(1.00f, 0.50f, 0.10f)   // naranja
    };

    // ───────────── ENEMIGO PIZZA FACE ─────────────
    [Header("Enemigo Pizza Face")]
    public GameObject enemigoPizza;            // opcional: si lo dejas vacío, se busca en la escena o se crea solo por código
    public AudioClip risaPizzaFace;            // opcional: risa por si el enemigo no tiene una asignada
    public float retrasoAparicionEnemigo = 1.5f; // segundos después de "IT'S PIZZA TIME" antes de que aparezca
    public float distanciaAparicionEnemigo = 14f; // a qué distancia, DELANTE de ti, aparece (así lo ves y lo oyes llegar)
    public float fuerzaEmpuje = 9f;            // velocidad inicial del empujón
    public float duracionEmpuje = 0.35f;       // lo que dura el empujón
    public float empujeVertical = 3f;          // saltito al ser golpeado
    public float invulnerabilidadGolpe = 1.5f; // segundos sin poder recibir otro golpe
    private float tiempoEmpuje = 0f;
    private Vector3 dirEmpuje;
    public bool aparecerEnPrimerMapa = false;  // false: Pizza Face solo aparece en el segundo mapa (después del TP)
    private bool enSegundoMapa = false;

    // El enemigo lo usa para saber si debe perseguir
    public bool EnJuego { get { return !derrotado && !juegoGanado && !cargando; } }

    // ───────────── PODERES ALEATORIOS (Coleccionable2) ─────────────
    [Header("Poderes aleatorios")]
    public float duracionVuelo = 6f;           // segundos volando
    public float velocidadVuelo = 9f;          // velocidad vertical al volar (Espacio sube / Ctrl baja)
    public float duracionDashPoder = 15f;      // segundos que puedes usar el dash (doble W)
    public float velocidadDash = 35f;          // velocidad del impulso hacia adelante
    public float duracionImpulsoDash = 0.2f;   // lo que dura cada dash
    public float cooldownDash = 0.8f;          // espera entre dashes
    public float ventanaDobleToque = 0.3f;     // tiempo máximo entre las 2 pulsaciones de W
    public float duracionRuleta = 2.5f;        // segundos que gira la ruleta
    public float duracionResultadoRuleta = 2f; // segundos que se queda mostrando el resultado

    private const int TOTAL_PODERES = 4;
    private static readonly string[] nombresPoder = { "IMPULSO", "VUELO", "+1 VIDA", "DASH" };
    private static readonly Color[] coloresPoder =
    {
        new Color(1.00f, 0.90f, 0.20f),  // Impulso
        new Color(0.40f, 0.85f, 1.00f),  // Vuelo
        new Color(0.40f, 1.00f, 0.50f),  // +1 Vida
        new Color(1.00f, 0.50f, 0.20f)   // Dash
    };

    private float tiempoVuelo = 0f;
    private float tiempoDashPoder = 0f;

    private float ultimoToqueW = -99f;
    private float proximoDash = 0f;
    private float tiempoDashActual = 0f;
    private Vector3 dirDash;

    private float invulnerableHasta = 0f;
    private float tiempoMensajeVida = 99f;

    // Ruleta
    private bool ruletaActiva = false;
    private bool ruletaAplicada = false;
    private float tiempoRuleta = 0f;
    private int resultadoRuleta = 0;

    // Sistema de combo / estilo (tipo Devil May Cry), dibujado por código
    [Header("Combo de estilo")]
    public bool mostrarCombo = true;
    public float puntosColeccionable1 = 40f;
    public float puntosColeccionable2 = 60f;
    public float retrasoDecaimiento = 1.5f;
    public float decaimientoBase = 10f;
    public float decaimientoPorRango = 7f;
    private const float PUNTOS_POR_RANGO = 100f;
    private static readonly string[] letrasRango = { "D", "C", "B", "A", "S", "SS", "SSS" };
    private static readonly string[] nombresRango =
        { "DISMAL", "CRAZY", "BADASS", "APOCALYPTIC", "SAVAGE", "SICK STYLE", "SMOKIN' SEXY STYLE!" };
    private static readonly Color[] coloresRango =
    {
        new Color(0.65f, 0.65f, 0.70f),  // D
        new Color(0.40f, 0.75f, 1.00f),  // C
        new Color(0.40f, 0.95f, 0.55f),  // B
        new Color(1.00f, 0.88f, 0.30f),  // A
        new Color(1.00f, 0.55f, 0.15f),  // S
        new Color(1.00f, 0.28f, 0.22f),  // SS
        new Color(1.00f, 0.20f, 0.65f)   // SSS (arcoíris)
    };
    private float puntosEstilo = 0f;
    private int rangoActual = 0;
    private int combo = 0;
    private float tiempoUltimoPickup = -99f;
    private float tiempoDesdePickup = 99f;
    private float tiempoDesdeCambioRango = 99f;
    private float alfaCombo = 0f;
    private GUIStyle estiloLetra;

    // Salto
    public float fuerzaSalto = 8f;
    public int saltosMaximos = 2;
    private int saltosRestantes;
    private bool enSuelo = false;

    // ───────────── DOBLE SALTO EN PICADA ─────────────
    // El salto en el aire lanza al personaje hacia adelante, con la panza hacia el frente/suelo
    [Header("Doble salto en picada")]
    public bool dobleSaltoPicada = true;
    public float velocidadPicada = 16f;        // velocidad horizontal del lanzamiento
    public float duracionPicada = 0.5f;        // segundos que dura el empuje
    public float fuerzaSaltoPicada = 6f;       // impulso hacia arriba al lanzarse
    public float tiempoMaximoPicada = 4f;      // seguridad: la picada se cancela si no aterrizas en este tiempo
    public float duracionDeslizar = 0.8f;      // segundos que se desliza al aterrizar
    [Range(0f, 1f)] public float factorDeslizar = 1f;   // fracción de la velocidad que conserva al deslizarse
    private bool enPicada = false;             // true desde que te lanzas hasta que tocas el suelo
    private float inicioPicada = 0f;
    private float tiempoPicada = 0f;           // tiempo que dura el empuje hacia adelante
    private Vector3 dirPicada;
    private Vector3 velHorizPrevia;            // velocidad horizontal justo antes del impacto
    private float tiempoDeslizar = 0f;
    private float velDeslizar = 0f;
    private Vector3 dirDeslizar;
    private PersonajeRoblox personaje;

    // Sonidos
    public AudioClip sonidoDoh;
    [Range(0f, 1f)] public float volumen = 1f;
    public AudioClip sonidoColeccionable2;
    [Range(0f, 1f)] public float volumen2 = 1f;

    // Victoria
    public GameObject cilindroWin;
    public GameObject textoGanaste;
    public float segundosParaReiniciar = 5f;
    private int totalMonedas;
    private bool juegoGanado = false;

    // ───────────── ANIMACIÓN DE VICTORIA (estilo estrella de Super Mario 64) ─────────────
    [Header("Animación de victoria (estrella)")]
    public bool animacionEstrella = true;
    public float duracionAnimacionWin = 2.8f;  // segundos de animación antes de la pantalla "¡GANASTE!"
    public float alturaSaltoWin = 2.5f;        // altura del salto al agarrar la estrella
    public float alturaEstrella = 2.6f;        // a qué altura sobre el personaje queda la estrella
    public float escalaEstrella = 1f;
    public float zoomCamaraWin = 0.8f;         // 0.8 = acerca la cámara un 20% (1 = sin zoom)
    public float correccionRotacionWin = 0f;   // si tu modelo mira hacia otro lado, corrige aquí (grados en Y)
    public AudioClip sonidoVictoria;           // opcional: "¡Yahoo!" / fanfarria
    [Range(0f, 1f)] public float volumenVictoria = 1f;

    private bool animWinActiva = false;
    private float tiempoAnimWin = 0f;
    private float retrasoWin = 0f;
    private Quaternion rotInicioWin;
    private float fovOriginal = 0f;
    private GameObject estrellaWin;
    private Material matDorado;
    private float chispaAcum = 0f;

    private class Chispa
    {
        public Transform t;
        public Vector3 vel;
        public float vida;
        public float max;
    }
    private List<Chispa> chispas = new List<Chispa>();

    // ───────────── RUEDA DE EMOTES ─────────────
    [Header("Rueda de emotes")]
    public KeyCode teclaRueda = KeyCode.G;
    public bool barajarAlAbrir = true;         // los 4 emotes aleatorios cambian cada vez que abres la rueda
    public float radioRueda = 230f;            // distancia de los botones al centro
    public float zonaMuerta = 80f;             // radio del centro donde no se selecciona nada

    [Header("Static de Miku")]
    public string textoStatic = "STATIC · MIKU";
    public AnimationClip animacionStatic;      // tu animación (opcional). Vacía = baile por código
    public Animator animadorPersonaje;         // opcional: si está vacío se busca en el personaje
    public AudioClip musicaStatic;             // tu canción (opcional)
    [Range(0f, 1f)] public float volumenStatic = 0.8f;
    public float duracionStatic = 0f;          // 0 = automático (lo que dure la animación/canción, o 10 s)
    public float bpmStatic = 128f;             // ritmo del baile por código
    public bool silenciarMusicaDeFondo = true; // calla la música del juego mientras suena tu canción

    private class Emote
    {
        public string nombre;
        public float duracion;
        public Color color;
        public Emote(string n, float d, Color c) { nombre = n; duracion = d; color = c; }
    }

    private static readonly Emote[] poolEmotes =
    {
        new Emote("GIRO",       1.6f, new Color(1.00f, 0.85f, 0.20f)),   // 0
        new Emote("BAILE",      3.5f, new Color(1.00f, 0.45f, 0.70f)),   // 1
        new Emote("SALTITOS",   2.4f, new Color(0.50f, 0.90f, 0.40f)),   // 2
        new Emote("REVERENCIA", 2.0f, new Color(0.60f, 0.70f, 1.00f)),   // 3
        new Emote("DISCO",      3.5f, new Color(0.80f, 0.40f, 1.00f)),   // 4
        new Emote("TEMBLOR",    1.5f, new Color(1.00f, 0.40f, 0.30f)),   // 5
        new Emote("MAREO",      3.0f, new Color(1.00f, 0.60f, 0.20f)),   // 6
        new Emote("PENDULO",    3.0f, new Color(0.40f, 0.90f, 0.90f))    // 7
    };

    private static readonly Color colorMiku = new Color(0.22f, 0.78f, 0.73f);
    private const int ID_STATIC = -1;
    private const int ESPACIOS = 5;

    private int[] espaciosRueda = new int[ESPACIOS];
    private float[] escalaEspacio = new float[ESPACIOS];
    private bool ruedaAbierta = false;
    private float tiempoRueda = 0f;
    private int seleccionRueda = -1;

    private bool haciendoEmote = false;
    private bool usandoAnimador = false;
    private int idActivo = 0;
    private float tEmote = 0f;
    private float durEmote = 1f;
    private Quaternion rotBaseEmote;
    private float proximoSaltoEmote = 0f;

    private AudioSource fuenteEmote;
    private List<AudioSource> silenciadas = new List<AudioSource>();
    private PlayableGraph grafoEmote;
    private bool grafoActivo = false;

    private CursorLockMode cursorPrevioRueda = CursorLockMode.None;
    private bool cursorVisiblePrevioRueda = true;

    private Texture2D texDisco;
    private Texture2D texAro;
    private GUIStyle estiloEmote;

    // ───────────── MOD MENU (para probar el juego) ─────────────
    [Header("Mod menu (pruebas)")]
    public bool modMenuActivo = true;          // desmárcalo para quitarlo del juego final
    public KeyCode teclaModMenu = KeyCode.F9;
    private bool menuAbierto = false;
    private bool vidasInfinitas = false;
    private bool tiempoCongelado = false;
    private bool saltosInfinitos = false;
    private Vector2 scrollMod;
    private string avisoMod = "";
    private float tiempoAvisoMod = 99f;
    private CursorLockMode cursorPrevio = CursorLockMode.None;
    private bool cursorVisiblePrevio = true;

    // tp
    public GameObject portalTP;
    public int monedasParaTP = 5;
    private Vector3 destinoTP = new Vector3(-168.9687f, -1105.54f, 17.68513f);

    private bool reiniciando = false;

    // Pantallas (dibujadas por código, sin Canvas)
    [Header("Pantallas")]
    public float duracionCarga = 1f;
    public bool pantallaGanasteCodigo = true;
    private bool cargando = false;
    private float inicioCarga;
    private float tiempoDesdeGanar = 0f;

    // ───────────── CONTADOR DE MONEDAS (dibujado por código) ─────────────
    [Header("Contador de monedas")]
    public bool mostrarContador = true;
    private float tiempoDesdeMoneda = 99f;

    // ───────────── TEMPORIZADOR ─────────────
    [Header("Temporizador")]
    public float tiempoTP = 120f;              // segundos al teletransportarte (2:00)
    public float tiempoWin = 90f;              // segundos al aparecer el cilindro win (1:30)
    public float segundosAlerta = 10f;         // el reloj parpadea en rojo con estos segundos
    public bool mostrarRelojSiempre = true;    // true: el reloj se ve aunque aún no haya arrancado
    public bool iniciarTemporizadorAlComenzar = true;   // true: la cuenta arranca al dar Play
    public float tiempoInicio = 120f;          // segundos con los que arranca al dar Play (2:00)
    private bool temporizadorActivo = false;
    private bool tiempoAgotado = false;
    private float tiempoRestante = 0f;
    private float duracionTemporizador = 1f;
    private float tiempoDesdeInicioTemporizador = 99f;

    // ───────────── MÚSICA ─────────────
    [Header("Música")]
    public AudioClip musicaFondo;              // opcional: música normal del juego
    public AudioClip musicaTiempoAgotado;      // música que suena cuando se acaba el tiempo
    [Range(0f, 1f)] public float volumenMusica = 0.5f;
    private AudioSource fuenteMusica;

    // ───────────── PIZZA TIME (cuando se acaba el tiempo) ─────────────
    [Header("Pizza Time")]
    public string textoPizzaTime = "IT'S PIZZA TIME!";
    public float duracionTextoPizza = 3.5f;    // segundos que se ve el texto
    public float retrasoColapso = 1.5f;        // espera antes de que empiece a desvanecerse el primer piso
    public float duracionColapso = 20f;        // en cuántos segundos se reparte la caída de todos los pisos
    public float duracionAdvertencia = 0.8f;   // temblor de cada piso antes de desvanecerse
    public float duracionDesvanecer = 2.5f;    // lo que tarda cada piso en desvanecerse
    private float tiempoPizza = 99f;
    private bool colapsoActivo = false;
    private float tiempoColapso = 0f;

    private class PlataformaCaida
    {
        public GameObject obj;
        public Collider[] cols;
        public Material[] mats;
        public Color[] coloresOriginales;
        public bool[] tieneColor;
        public Vector3 posInicial;
        public float retraso;
        public bool transparente;
        public bool collidersApagados;
        public bool terminada;
    }
    private List<PlataformaCaida> plataformasCaida = new List<PlataformaCaida>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        personaje = GetComponent<PersonajeRoblox>();
        saltosRestantes = saltosMaximos;
        vidas = vidasMaximas;
        puntoControl = transform.position;   // checkpoint inicial = donde empiezas

        foreach (GameObject zona in GameObject.FindGameObjectsWithTag("kill"))
        {
            Renderer r = zona.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        foreach (GameObject zona in GameObject.FindGameObjectsWithTag("vacio"))
        {
            Renderer r = zona.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        totalMonedas = GameObject.FindGameObjectsWithTag("Coleccionable").Length
                     + GameObject.FindGameObjectsWithTag("Coleccionable2").Length;
        Debug.Log("Monedas totales en el mapa: " + totalMonedas);

        if (cilindroWin != null) cilindroWin.SetActive(false);
        else Debug.LogWarning("Falta asignar Cilindro Win en el Jugador");

        if (textoGanaste != null) textoGanaste.SetActive(false);
        else Debug.LogWarning("Falta asignar Texto Ganaste en el Jugador");

        if (portalTP != null) portalTP.SetActive(false);
        else Debug.LogWarning("Falta asignar Portal TP en el Jugador");

        if (contenedorBarra != null) contenedorBarra.SetActive(false);

        // Busca / crea al enemigo y lo deja apagado (solo sale cuando se acaba el tiempo)
        PrepararEnemigo();

        // Fuente de audio para la música (se crea sola, no tienes que añadir nada)
        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.playOnAwake = false;
        fuenteMusica.loop = true;
        fuenteMusica.spatialBlend = 0f;
        fuenteMusica.volume = volumenMusica;
        CambiarMusica(musicaFondo);

        IniciarRuedaEmotes();

        if (iniciarTemporizadorAlComenzar) IniciarTemporizador(tiempoInicio);
    }

    void Update()
    {
        if (modMenuActivo && Input.GetKeyDown(teclaModMenu)) AlternarModMenu();
        if (saltosInfinitos && saltosRestantes < 1) saltosRestantes = 1;

        // Salto y doble salto (no mientras vuelas)
        if (Input.GetKeyDown(KeyCode.Space) && saltosRestantes > 0 && !cargando && !juegoGanado && !derrotado && tiempoVuelo <= 0f)
        {
            bool saltoEnAire = !enSuelo;
            Vector3 v = rb.linearVelocity;

            if (dobleSaltoPicada && saltoEnAire)
            {
                // Doble salto: lanzamiento hacia adelante con la panza hacia adelante
                dirPicada = DireccionPicada();
                tiempoPicada = duracionPicada;
                tiempoDeslizar = 0f;
                enPicada = true;
                inicioPicada = Time.time;
                velHorizPrevia = dirPicada * velocidadPicada;
                v.x = dirPicada.x * velocidadPicada;
                v.z = dirPicada.z * velocidadPicada;
                v.y = fuerzaSaltoPicada;
                // La pose dura hasta que toques el suelo (tiempoMaximoPicada es solo un seguro)
                if (personaje != null) personaje.IniciarPicada(tiempoMaximoPicada);
            }
            else
            {
                CancelarPicada();   // saltar desde el suelo corta cualquier deslizamiento
                v.y = fuerzaSalto;
            }

            rb.linearVelocity = v;
            saltosRestantes--;
            enSuelo = false;
        }

        // Dash: doble toque de W (solo con el poder activo)
        if (Input.GetKeyDown(KeyCode.W) && !cargando && !juegoGanado && !derrotado)
        {
            if (tiempoDashPoder > 0f
                && Time.time - ultimoToqueW <= ventanaDobleToque
                && Time.time >= proximoDash)
            {
                IniciarDash();
                ultimoToqueW = -99f;
            }
            else
            {
                ultimoToqueW = Time.time;
            }
        }

        // Seguridad: la picada se cancela si pasa demasiado tiempo o si empiezas a volar
        if (enPicada && (Time.time - inicioPicada > tiempoMaximoPicada || tiempoVuelo > 0f)) CancelarPicada();

        if (juegoGanado) tiempoDesdeGanar += Time.unscaledDeltaTime;
        tiempoDesdeActivacion += Time.deltaTime;
        tiempoMensajeVida += Time.deltaTime;
        tiempoAvisoMod += Time.unscaledDeltaTime;

        if (derrotado) tiempoDesdeDerrota += Time.unscaledDeltaTime;
        tiempoCheckpoint += Time.deltaTime;

        if (usarAlturaVacio && !cargando && transform.position.y < alturaVacio)
            PerderVidas(danoVacio, "caída al vacío");

        tiempoDesdeMoneda += Time.deltaTime;
        tiempoDesdeInicioTemporizador += Time.deltaTime;
        ActualizarTemporizador();
        tiempoPizza += Time.deltaTime;
        ActualizarColapso();

        ActualizarBoost();
        ActualizarPoderes();
        ActualizarRuleta();
        ActualizarEstilo();
        ActualizarRuedaEmotes();
        AplicarEmoteCuerpo();
    }

    // Dirección de la picada: hacia donde empujas (relativo a la cámara) o, si no tocas nada, hacia donde mira la cámara
    private Vector3 DireccionPicada()
    {
        Camera camara = Camera.main;
        Vector3 adelante = (camara != null) ? camara.transform.forward : transform.forward;
        Vector3 derecha = (camara != null) ? camara.transform.right : transform.right;
        adelante.y = 0f;
        derecha.y = 0f;
        if (adelante.sqrMagnitude < 0.001f) adelante = transform.forward;
        adelante.Normalize();
        derecha.Normalize();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = adelante * v + derecha * h;

        if (dir.sqrMagnitude < 0.01f) dir = adelante;
        return dir.normalized;
    }

    // Cualquier superficie "de piso" (normal hacia arriba) termina la picada, tenga el tag que tenga
    private void IntentarAterrizarPicada(Collision collision)
    {
        if (!enPicada) return;
        if (Time.time - inicioPicada < 0.1f) return;   // margen para no cancelar al despegar

        foreach (ContactPoint contacto in collision.contacts)
        {
            if (contacto.normal.y > 0.5f)
            {
                AterrizarPicada();
                return;
            }
        }
    }

    // Al tocar el suelo: termina la picada y empieza un pequeño deslizamiento que frena solo
    private void AterrizarPicada()
    {
        enPicada = false;
        tiempoPicada = 0f;

        Vector3 vh = velHorizPrevia;
        vh.y = 0f;
        dirDeslizar = (vh.sqrMagnitude > 0.01f) ? vh.normalized : dirPicada;
        velDeslizar = Mathf.Min(vh.magnitude, velocidadPicada) * factorDeslizar;
        tiempoDeslizar = duracionDeslizar;

        if (personaje != null) personaje.IniciarDeslizamiento(duracionDeslizar);
    }

    // Corta la picada y el deslizamiento (salto desde el suelo, dash, respawn, teletransporte...)
    private void CancelarPicada()
    {
        enPicada = false;
        tiempoPicada = 0f;
        tiempoDeslizar = 0f;
        tiempoEmpuje = 0f;   // también corta el empujón del enemigo
        if (personaje != null) personaje.TerminarPicada();
    }

    // ───────────── PODERES ─────────────

    private string DuracionPoderTexto(int poder)
    {
        switch (poder)
        {
            case 0: return duracionBoost.ToString("0.#") + " segundos";
            case 1: return duracionVuelo.ToString("0.#") + " segundos";
            case 2: return "Instantáneo";
            default: return duracionDashPoder.ToString("0.#") + " segundos";
        }
    }

    private string DescripcionPoder(int poder)
    {
        switch (poder)
        {
            case 0: return "Velocidad x" + multiplicadorBoost.ToString("0.#");
            case 1: return "Espacio sube · Ctrl baja";
            case 2: return "Recuperas 1 vida";
            default: return "Pulsa W dos veces rápido";
        }
    }

    // Elige el poder de la ruleta: con la vida llena, +1 VIDA tiene 0% de salir
    private int ElegirPoder()
    {
        if (vidas < vidasMaximas) return Random.Range(0, TOTAL_PODERES);

        // Vida llena: 0% de que salga +1 VIDA
        int r = Random.Range(0, TOTAL_PODERES - 1);
        return (r >= INDICE_VIDA) ? r + 1 : r;
    }

    private void IniciarRuleta()
    {
        // Si ya había una ruleta girando, se aplica su premio antes de empezar otra
        if (ruletaActiva && !ruletaAplicada) AplicarPoder(resultadoRuleta);

        ruletaActiva = true;
        ruletaAplicada = false;
        tiempoRuleta = 0f;
        resultadoRuleta = ElegirPoder();
    }

    private void ActualizarRuleta()
    {
        if (!ruletaActiva) return;

        tiempoRuleta += Time.deltaTime;

        if (!ruletaAplicada && tiempoRuleta >= duracionRuleta)
        {
            ruletaAplicada = true;
            AplicarPoder(resultadoRuleta);
        }

        if (tiempoRuleta >= duracionRuleta + duracionResultadoRuleta)
        {
            ruletaActiva = false;
        }
    }

    private void AplicarPoder(int poder)
    {
        switch (poder)
        {
            case 0:
                ActivarBoost();
                break;
            case 1:
                tiempoVuelo = duracionVuelo;
                rb.useGravity = false;
                Vector3 v = rb.linearVelocity;
                v.y = 0f;
                rb.linearVelocity = v;
                break;
            case 2:
                vidas = Mathf.Min(vidas + 1, vidasMaximas);
                break;
            case 3:
                tiempoDashPoder = duracionDashPoder;
                break;
        }
        Debug.Log("Poder obtenido: " + nombresPoder[poder]);
    }

    private void ActualizarPoderes()
    {
        if (tiempoVuelo > 0f)
        {
            tiempoVuelo -= Time.deltaTime;
            if (tiempoVuelo <= 0f)
            {
                tiempoVuelo = 0f;
                rb.useGravity = true;
            }
        }

        if (tiempoDashPoder > 0f)
        {
            tiempoDashPoder -= Time.deltaTime;
            if (tiempoDashPoder < 0f) tiempoDashPoder = 0f;
        }
    }

    private void IniciarDash()
    {
        Vector3 dir;
        Camera camara = Camera.main;
        if (camara != null) dir = camara.transform.forward;
        else dir = transform.forward;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        dirDash = dir.normalized;

        CancelarPicada();
        tiempoDashActual = duracionImpulsoDash;
        proximoDash = Time.time + cooldownDash;
    }

    // ───────────── VIDAS / DAÑO / CHECKPOINTS ─────────────

    private void PerderVidas(int cantidad, string origen)
    {
        if (reiniciando || derrotado || juegoGanado || cargando) return;
        if (Time.time < invulnerableHasta) return;

        if (!vidasInfinitas) vidas = Mathf.Max(0, vidas - cantidad);
        ultimoDano = cantidad;
        tiempoMensajeVida = 0f;
        Debug.Log("Daño (" + origen + "): -" + cantidad + " | Vidas: " + vidas);

        if (vidas <= 0)
        {
            Derrota();
            return;
        }

        invulnerableHasta = Time.time + 1f;
        RespawnEnCheckpoint();
    }

    // Golpe del enemigo: quita vida y empuja suavemente (sin respawn en el checkpoint)
    public void GolpeEnemigo(Vector3 origen, int dano)
    {
        if (reiniciando || derrotado || juegoGanado || cargando) return;
        if (Time.time < invulnerableHasta) return;

        if (!vidasInfinitas) vidas = Mathf.Max(0, vidas - dano);
        ultimoDano = dano;
        tiempoMensajeVida = 0f;
        Debug.Log("Daño (Pizza Face): -" + dano + " | Vidas: " + vidas);

        if (vidas <= 0)
        {
            Derrota();
            return;
        }

        invulnerableHasta = Time.time + invulnerabilidadGolpe;

        CancelarPicada();
        tiempoDashActual = 0f;

        // Dirección del empuje: lejos del enemigo
        Vector3 dir = transform.position - origen;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = -transform.forward;
        dirEmpuje = dir.normalized;
        tiempoEmpuje = duracionEmpuje;

        Vector3 v = rb.linearVelocity;
        v.y = empujeVertical;
        rb.linearVelocity = v;
        enSuelo = false;
    }

    // Deja listo al enemigo: lo busca, lo copia si es un prefab, o lo crea por código
    private void PrepararEnemigo()
    {
        // 1) No lo arrastraste: lo busca en la escena (aunque esté apagado)
        if (enemigoPizza == null)
        {
            PizzaFaceEnemigo existente = FindFirstObjectByType<PizzaFaceEnemigo>(FindObjectsInactive.Include);
            if (existente != null) enemigoPizza = existente.gameObject;
        }
        // 2) Arrastraste un prefab de la carpeta Project (no es un objeto de la escena): crea una copia
        else if (!enemigoPizza.scene.IsValid())
        {
            enemigoPizza = Instantiate(enemigoPizza);
            Debug.Log("Pizza Face: el objeto asignado era un prefab, creé una copia en la escena");
        }

        // 3) No existe ninguno: lo crea por código (el modelo también se arma solo)
        if (enemigoPizza == null)
        {
            enemigoPizza = new GameObject("PizzaFace");
            enemigoPizza.SetActive(false);
            Debug.Log("Pizza Face: no había enemigo en la escena, lo creé por código");
        }

        PizzaFaceEnemigo pf = enemigoPizza.GetComponent<PizzaFaceEnemigo>();
        if (pf == null) pf = enemigoPizza.AddComponent<PizzaFaceEnemigo>();
        if (pf.risa == null) pf.risa = risaPizzaFace;

        enemigoPizza.SetActive(false);
    }

    // Aviso en pantalla (solo con el mod menu activo) + mensaje en la Console
    private void AvisoMod(string mensaje)
    {
        Debug.Log(mensaje);
        if (!modMenuActivo) return;
        avisoMod = mensaje;
        tiempoAvisoMod = 0f;
    }

    private string EstadoEnemigo()
    {
        if (enemigoPizza == null) return "no existe";
        if (!enemigoPizza.activeInHierarchy) return "apagado (listo para salir)";
        return "ACTIVO a " + Vector3.Distance(transform.position, enemigoPizza.transform.position).ToString("0") + " m de ti";
    }

    // Espera un poco después de "IT'S PIZZA TIME" y hace aparecer al enemigo
    private IEnumerator AparecerEnemigo()
    {
        AvisoMod("Se acabó el tiempo: Pizza Face sale en " + retrasoAparicionEnemigo.ToString("0.#") + " s");
        yield return new WaitForSeconds(retrasoAparicionEnemigo);
        if (!EnJuego)
        {
            AvisoMod("Pizza Face NO sale: el juego ya terminó o está cargando");
            yield break;
        }
        SpawnEnemigoAhora();
    }

    // Delante de la cámara, a distanciaAparicionEnemigo, un poco en alto
    private Vector3 PosicionAparicionEnemigo()
    {
        Camera cam = Camera.main;
        Vector3 adelante = (cam != null) ? cam.transform.forward : transform.forward;
        adelante.y = 0f;
        if (adelante.sqrMagnitude < 0.001f) adelante = transform.forward;
        return transform.position + adelante.normalized * distanciaAparicionEnemigo + Vector3.up * 3f;
    }

    // Hace aparecer a Pizza Face YA (lo usan el temporizador y el mod menu)
    public void SpawnEnemigoAhora()
    {
        if (enemigoPizza == null) PrepararEnemigo();

        Vector3 pos = PosicionAparicionEnemigo();

        PizzaFaceEnemigo pf = enemigoPizza.GetComponent<PizzaFaceEnemigo>();
        if (pf == null) pf = enemigoPizza.AddComponent<PizzaFaceEnemigo>();
        if (pf.risa == null) pf.risa = risaPizzaFace;

        // Si ya estaba activo, solo lo trae hacia ti
        if (enemigoPizza.activeSelf && enemigoPizza.activeInHierarchy)
        {
            enemigoPizza.transform.position = pos;
            AvisoMod("Pizza Face ya estaba activo: lo traje hacia ti");
            return;
        }

        pf.Aparecer(pos);   // lo autoriza, lo desengancha de padres apagados, lo coloca y suena la risa
        AvisoMod("¡Pizza Face apareció!" + (pf.risa == null ? "  (sin audio de risa asignado)" : ""));
    }

    // Reubica al jugador SIN conservar nada del impulso anterior
    private void RespawnEnCheckpoint()
    {
        CancelarPicada();
        tiempoDashActual = 0f;
        tiempoVuelo = 0f;
        rb.useGravity = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = puntoControl + Vector3.up * 1f;

        enSuelo = false;
        saltosRestantes = saltosMaximos;
    }

    private void ActivarCheckpoint(GameObject cp)
    {
        if (puntoControl == cp.transform.position) return;
        puntoControl = cp.transform.position;
        tiempoCheckpoint = 0f;
        Debug.Log("Checkpoint guardado: " + cp.name);
    }

    private void Derrota()
    {
        if (derrotado) return;
        derrotado = true;
        tiempoDesdeDerrota = 0f;
        ruletaActiva = false;

        // Game Over estilo Mario 64: el iris se cierra sobre el personaje
        Vector2 centroIris = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Camera camIris = Camera.main;
        if (camIris != null)
        {
            Vector3 sp = camIris.WorldToScreenPoint(transform.position + Vector3.up);
            if (sp.z > 0f) centroIris = new Vector2(sp.x, Screen.height - sp.y);
        }
        irisCentro = centroIris;

        if (fuenteMusica != null) fuenteMusica.Stop();   // la música calla
        if (sonidoGameOver != null) AudioSource.PlayClipAtPoint(sonidoGameOver, transform.position, volumenGameOver);

        CancelarPicada();
        tiempoDashActual = 0f;
        tiempoVuelo = 0f;
        rb.useGravity = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("¡Derrota!");

        Invoke(nameof(ReiniciarJuego), segundosParaReiniciar);
    }

    // ───────────── TEMPORIZADOR / MÚSICA ─────────────

    private void IniciarTemporizador(float segundos)
    {
        tiempoRestante = segundos;
        duracionTemporizador = Mathf.Max(1f, segundos);
        temporizadorActivo = true;
        tiempoAgotado = false;
        tiempoDesdeInicioTemporizador = 0f;
        CambiarMusica(musicaFondo);   // si se acabó el tiempo antes, vuelve la música normal
        Debug.Log("Temporizador iniciado: " + segundos + "s");
    }

    private void ActualizarTemporizador()
    {
        if (fuenteMusica != null) fuenteMusica.volume = volumenMusica;

        if (!temporizadorActivo || juegoGanado || derrotado || cargando) return;
        if (tiempoCongelado) return;

        tiempoRestante -= Time.deltaTime;
        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            temporizadorActivo = false;
            tiempoAgotado = true;
            CambiarMusica(musicaTiempoAgotado);
            Debug.Log("¡Se acabó el tiempo!");
            tiempoPizza = 0f;      // muestra "IT'S PIZZA TIME!"

            // Pizza Face aparece SOLO cuando se acaba el tiempo (con su risa).
            // Va ANTES del colapso para que un problema con los pisos no lo impida.
            if (EnSegundoMapa || aparecerEnPrimerMapa) StartCoroutine(AparecerEnemigo());
            else AvisoMod("Pizza Face NO sale: sigues en el 1er mapa (usa el portal, o marca 'Aparecer En Primer Mapa')");

            IniciarColapso();      // los pisos "pizo" empiezan a desvanecerse
        }
    }

    private void CambiarMusica(AudioClip clip)
    {
        if (fuenteMusica == null || clip == null) return;
        if (fuenteMusica.clip == clip && fuenteMusica.isPlaying) return;

        fuenteMusica.clip = clip;
        fuenteMusica.volume = volumenMusica;
        fuenteMusica.loop = true;
        fuenteMusica.Play();
    }

    // ───────────── COLAPSO DE PLATAFORMAS ─────────────
    // Los pisos más lejanos al jugador (en el momento en que se acaba el tiempo) se desvanecen primero

    // Si el tag no existe en Unity, avisa en vez de romper todo con una excepción
    private static GameObject[] BuscarPorTag(string tag)
    {
        try { return GameObject.FindGameObjectsWithTag(tag); }
        catch (UnityException)
        {
            Debug.LogWarning("El tag '" + tag + "' no existe: créalo en Tags and Layers.");
            return new GameObject[0];
        }
    }

    private void IniciarColapso()
    {
        if (colapsoActivo) return;
        plataformasCaida.Clear();

        GameObject[] pisos = BuscarPorTag(EnSegundoMapa ? "pizo2" : "pizo");
        float distMax = 0.01f;
        foreach (GameObject go in pisos)
            distMax = Mathf.Max(distMax, Vector3.Distance(go.transform.position, transform.position));

        foreach (GameObject go in pisos)
        {
            PlataformaCaida p = new PlataformaCaida();
            p.obj = go;
            p.cols = go.GetComponentsInChildren<Collider>();

            List<Material> lista = new List<Material>();
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                foreach (Material m in r.materials)
                    if (m != null) lista.Add(m);

            p.mats = lista.ToArray();
            p.coloresOriginales = new Color[p.mats.Length];
            p.tieneColor = new bool[p.mats.Length];
            for (int i = 0; i < p.mats.Length; i++)
            {
                p.tieneColor[i] = p.mats[i].HasProperty("_BaseColor") || p.mats[i].HasProperty("_Color");
                if (p.tieneColor[i]) p.coloresOriginales[i] = p.mats[i].color;
            }

            p.posInicial = go.transform.position;
            float lejania = Vector3.Distance(go.transform.position, transform.position) / distMax;
            p.retraso = (1f - lejania) * duracionColapso;   // el más lejano: retraso 0 (cae primero)
            plataformasCaida.Add(p);
        }

        colapsoActivo = true;
        tiempoColapso = 0f;
        Debug.Log("Colapso iniciado: " + plataformasCaida.Count + " plataformas");
    }

    private void ActualizarColapso()
    {
        if (!colapsoActivo) return;

        tiempoColapso += Time.deltaTime;
        float t0 = tiempoColapso - retrasoColapso;
        if (t0 < 0f) return;

        bool quedan = false;
        foreach (PlataformaCaida p in plataformasCaida)
        {
            if (p.terminada) continue;
            if (p.obj == null) { p.terminada = true; continue; }

            quedan = true;
            float t = t0 - p.retraso;
            if (t < 0f) continue;

            if (t < duracionAdvertencia)
            {
                // Temblor de advertencia
                float fuerza = 0.06f * (t / Mathf.Max(0.01f, duracionAdvertencia));
                Vector3 temblor = new Vector3(Mathf.Sin(Time.time * 60f + p.retraso),
                                              0f,
                                              Mathf.Cos(Time.time * 53f + p.retraso)) * fuerza;
                p.obj.transform.position = p.posInicial + temblor;
                continue;
            }

            // Desvanecimiento: se vuelve transparente y se hunde un poco
            float f = Mathf.Clamp01((t - duracionAdvertencia) / Mathf.Max(0.01f, duracionDesvanecer));

            if (!p.transparente)
            {
                foreach (Material m in p.mats) PrepararTransparencia(m);
                p.transparente = true;
            }

            for (int i = 0; i < p.mats.Length; i++)
            {
                if (!p.tieneColor[i]) continue;
                Color c = p.coloresOriginales[i];
                c.a = c.a * (1f - f);
                p.mats[i].color = c;
            }

            p.obj.transform.position = p.posInicial + Vector3.down * (0.5f * f);

            // A la mitad del desvanecimiento deja de ser sólido
            if (f >= 0.5f && !p.collidersApagados) ApagarPlataforma(p);

            if (f >= 1f)
            {
                p.obj.SetActive(false);
                p.terminada = true;
            }
        }

        if (!quedan) colapsoActivo = false;
    }

    private void ApagarPlataforma(PlataformaCaida p)
    {
        p.collidersApagados = true;

        foreach (Collider c in p.cols)
        {
            if (c == null) continue;

            // Si estabas parado encima, deja de contar como "en suelo" (los bounds se leen antes de apagar)
            Bounds b = c.bounds;
            b.Expand(2f);
            if (b.Contains(transform.position))
            {
                enSuelo = false;
                if (saltosRestantes == saltosMaximos) saltosRestantes = saltosMaximos - 1;
            }
            c.enabled = false;
        }
    }

    // Convierte el material a transparente en tiempo de ejecución (Standard o URP Lit)
    private static void PrepararTransparencia(Material m)
    {
        if (m.HasProperty("_Surface"))          // URP / HDRP
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
        }
        else if (m.HasProperty("_Mode"))        // Standard (Built-in)
        {
            m.SetFloat("_Mode", 2f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
        }
    }

    // ───────────── ESTILO / BOOST ─────────────

    private void AgregarEstilo(float puntos)
    {
        combo++;
        puntosEstilo = Mathf.Min(puntosEstilo + puntos, PUNTOS_POR_RANGO * letrasRango.Length - 0.01f);
        tiempoUltimoPickup = Time.time;
        tiempoDesdePickup = 0f;

        int nuevoRango = Mathf.FloorToInt(puntosEstilo / PUNTOS_POR_RANGO);
        if (nuevoRango > rangoActual) tiempoDesdeCambioRango = 0f;
        rangoActual = nuevoRango;
    }

    private void ActualizarEstilo()
    {
        float dt = Time.deltaTime;
        tiempoDesdePickup += dt;
        tiempoDesdeCambioRango += dt;

        if (puntosEstilo > 0f && Time.time - tiempoUltimoPickup > retrasoDecaimiento)
        {
            puntosEstilo -= (decaimientoBase + rangoActual * decaimientoPorRango) * dt;
            if (puntosEstilo <= 0f)
            {
                puntosEstilo = 0f;
                combo = 0;
            }
            rangoActual = Mathf.Clamp(Mathf.FloorToInt(puntosEstilo / PUNTOS_POR_RANGO), 0, letrasRango.Length - 1);
        }

        alfaCombo = Mathf.MoveTowards(alfaCombo, puntosEstilo > 0f ? 1f : 0f, dt * 4f);
    }

    private void ActualizarBoost()
    {
        if (tiempoBoost <= 0f) return;

        tiempoBoost -= Time.deltaTime;

        if (barraRelleno != null)
        {
            barraRelleno.fillAmount = Mathf.Clamp01(tiempoBoost / duracionBoost);
        }

        if (tiempoBoost <= 0f && contenedorBarra != null)
        {
            contenedorBarra.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (cargando || juegoGanado || derrotado)
        {
            if (!rb.isKinematic)
            {
                Vector3 vq = rb.linearVelocity;
                vq.x = 0f;
                vq.z = 0f;
                rb.linearVelocity = vq;
            }
            return;
        }

        bool volando = tiempoVuelo > 0f;

        // Guarda la velocidad horizontal antes del impacto (para calcular el deslizamiento al aterrizar)
        if (enPicada) velHorizPrevia = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Velocidad vertical al volar
        float vyVuelo = 0f;
        if (volando)
        {
            if (Input.GetKey(KeyCode.Space)) vyVuelo += velocidadVuelo;
            if (Input.GetKey(KeyCode.LeftControl)) vyVuelo -= velocidadVuelo;
        }

        // Empuje suave del enemigo: arranca con fuerza y se va apagando
        if (tiempoEmpuje > 0f)
        {
            tiempoEmpuje -= Time.fixedDeltaTime;
            float te = Mathf.Clamp01(tiempoEmpuje / Mathf.Max(0.01f, duracionEmpuje));
            Vector3 ve = dirEmpuje * (fuerzaEmpuje * te);
            ve.y = volando ? vyVuelo : rb.linearVelocity.y;
            rb.linearVelocity = ve;
            return;
        }

        // Dash en curso: ignora el movimiento normal
        if (tiempoDashActual > 0f)
        {
            tiempoDashActual -= Time.fixedDeltaTime;
            Vector3 vd = dirDash * velocidadDash;
            vd.y = volando ? vyVuelo : rb.linearVelocity.y;
            rb.linearVelocity = vd;
            return;
        }

        // Picada del doble salto en curso: dirección fija, frena un poco al final
        if (tiempoPicada > 0f)
        {
            tiempoPicada -= Time.fixedDeltaTime;
            float t = Mathf.Clamp01(tiempoPicada / Mathf.Max(0.01f, duracionPicada));
            Vector3 vp = dirPicada * (velocidadPicada * Mathf.Lerp(0.6f, 1f, t));
            vp.y = volando ? vyVuelo : rb.linearVelocity.y;
            rb.linearVelocity = vp;
            return;
        }

        // Deslizamiento al aterrizar de la picada: arranca rápido y frena hasta detenerse
        if (tiempoDeslizar > 0f)
        {
            tiempoDeslizar -= Time.fixedDeltaTime;
            float ts = Mathf.Clamp01(tiempoDeslizar / Mathf.Max(0.01f, duracionDeslizar));
            Vector3 vs = dirDeslizar * (velDeslizar * ts);
            vs.y = volando ? vyVuelo : rb.linearVelocity.y;
            rb.linearVelocity = vs;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Camera camara = Camera.main;
        if (camara == null) return;

        Transform cam = camara.transform;
        Vector3 adelante = cam.forward;
        Vector3 derecha = cam.right;
        adelante.y = 0f;
        derecha.y = 0f;
        adelante.Normalize();
        derecha.Normalize();

        Vector3 dir = Vector3.ClampMagnitude(adelante * v + derecha * h, 1f);

        float velocidadMax = Input.GetKey(KeyCode.LeftShift) ? velocidadCorrer : velocidadCaminar;

        float multiplicador = (tiempoBoost > 0f) ? multiplicadorBoost : 1f;
        velocidadMax *= multiplicador;

        Vector3 velActual = rb.linearVelocity;
        velActual.y = 0f;

        Vector3 velObjetivo = dir * velocidadMax;

        float cambio = (dir.sqrMagnitude > 0.01f) ? aceleracion * multiplicador : frenado;
        if (!enSuelo && !volando) cambio *= controlEnAire;

        Vector3 velNueva = Vector3.MoveTowards(velActual, velObjetivo, cambio * Time.fixedDeltaTime);

        velNueva.y = volando ? vyVuelo : rb.linearVelocity.y;
        rb.linearVelocity = velNueva;
    }

    // Suelo válido: "pizo" (primer mapa) o "pizo2" (segundo mapa)
    private static bool EsPiso(GameObject go)
    {
        return go.CompareTag("pizo") || go.CompareTag("pizo2");
    }

    private void OnCollisionStay(Collision collision)
    {
        IntentarAterrizarPicada(collision);

        if (!EsPiso(collision.gameObject)) return;

        foreach (ContactPoint contacto in collision.contacts)
        {
            if (contacto.normal.y > 0.5f)
            {
                enSuelo = true;
                if (rb.linearVelocity.y <= 0.1f) saltosRestantes = saltosMaximos;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (EsPiso(collision.gameObject))
        {
            if (enSuelo && saltosRestantes == saltosMaximos && rb.linearVelocity.y <= 0.1f)
            {
                saltosRestantes = saltosMaximos - 1;
            }
            enSuelo = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        IntentarAterrizarPicada(collision);

        if (collision.gameObject.CompareTag("win")) Ganar();
        else if (collision.gameObject.CompareTag("kill"))
            PerderVidas(danoObstaculo, collision.gameObject.name);
        else if (collision.gameObject.CompareTag("vacio"))
            PerderVidas(danoVacio, collision.gameObject.name);
        else if (collision.gameObject.CompareTag("checkpoint"))
            ActivarCheckpoint(collision.gameObject);
        else if (collision.gameObject.CompareTag("tp"))
        {
            Debug.Log("Teletransporte por tp: " + collision.gameObject.name);
            Teletransportar();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Coleccionable"))
        {
            if (sonidoDoh != null) AudioSource.PlayClipAtPoint(sonidoDoh, transform.position, volumen);
            RecogerMoneda(other.gameObject);
            AgregarEstilo(puntosColeccionable1);
        }
        else if (other.gameObject.CompareTag("Coleccionable2"))
        {
            if (sonidoColeccionable2 != null) AudioSource.PlayClipAtPoint(sonidoColeccionable2, transform.position, volumen2);
            RecogerMoneda(other.gameObject);
            AgregarEstilo(puntosColeccionable2);
            IniciarRuleta();   // poder aleatorio
        }
        else if (other.gameObject.CompareTag("win")) Ganar();
        else if (other.gameObject.CompareTag("kill"))
            PerderVidas(danoObstaculo, other.gameObject.name);
        else if (other.gameObject.CompareTag("vacio"))
            PerderVidas(danoVacio, other.gameObject.name);
        else if (other.gameObject.CompareTag("checkpoint"))
            ActivarCheckpoint(other.gameObject);
        else if (other.gameObject.CompareTag("tp"))
        {
            Debug.Log("Teletransporte por tp: " + other.gameObject.name);
            Teletransportar();
        }
    }

    private void ActivarBoost()
    {
        tiempoBoost = duracionBoost;
        tiempoDesdeActivacion = 0f;

        if (contenedorBarra != null) contenedorBarra.SetActive(true);
        if (barraRelleno != null) barraRelleno.fillAmount = 1f;
    }

    private void RecogerMoneda(GameObject moneda)
    {
        moneda.SetActive(false);
        contador_coleccionables++;
        tiempoDesdeMoneda = 0f;
        Debug.Log("Coleccionables recogidos: " + contador_coleccionables + " de " + totalMonedas);
        if (puntaje != null) puntaje.text = contador_coleccionables + "";

        if (contador_coleccionables >= monedasParaTP && portalTP != null)
        {
            portalTP.SetActive(true);
        }

        if (contador_coleccionables >= totalMonedas)
        {
            if (cilindroWin != null) cilindroWin.SetActive(true);
            IniciarTemporizador(tiempoWin);   // 1:30 para llegar al win
        }
    }

    private void ReiniciarJuego()
    {
        if (reiniciando) return;
        reiniciando = true;

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Teletransportar()
    {
        if (cargando) return;
        StartCoroutine(CargaTP());
    }

    private IEnumerator CargaTP()
    {
        cargando = true;
        inicioCarga = Time.unscaledTime;

        CancelarPicada();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        transform.position = destinoTP;
        puntoControl = destinoTP;

        // Segundo mapa: si el enemigo había salido en el primero, se reinicia para que vuelva a aparecer aquí
        enSegundoMapa = true;
        if (enemigoPizza != null && enemigoPizza.activeSelf) enemigoPizza.SetActive(false);

        yield return new WaitForSecondsRealtime(duracionCarga);

        rb.isKinematic = false;
        cargando = false;

        IniciarTemporizador(tiempoTP);   // 2:00 desde que llegas
    }

    // ───────────── ANIMACIÓN DE VICTORIA ─────────────

    private void IniciarAnimacionVictoria()
    {
        retrasoWin = duracionAnimacionWin;
        tiempoAnimWin = 0f;
        animWinActiva = true;

        CancelarPicada();
        tiempoDashActual = 0f;
        ruletaActiva = false;

        // Salto real con física: sube, y aterriza solo donde esté el suelo
        Vector3 v = rb.linearVelocity;
        v.x = 0f;
        v.z = 0f;
        v.y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * Mathf.Max(0.1f, alturaSaltoWin));
        rb.linearVelocity = v;

        rotInicioWin = transform.rotation;

        Camera cam = Camera.main;
        fovOriginal = (cam != null && !cam.orthographic) ? cam.fieldOfView : 0f;

        if (sonidoVictoria != null)
        {
            if (fuenteMusica != null) fuenteMusica.Stop();   // la música calla, como en Mario 64
            AudioSource.PlayClipAtPoint(sonidoVictoria, transform.position, volumenVictoria);
        }

        CrearEstrellaWin();
    }

    // ───────────── RUEDA DE EMOTES ─────────────

    private void IniciarRuedaEmotes()
    {
        fuenteEmote = gameObject.AddComponent<AudioSource>();
        fuenteEmote.playOnAwake = false;
        fuenteEmote.loop = false;
        fuenteEmote.spatialBlend = 0f;

        if (animadorPersonaje == null) animadorPersonaje = GetComponentInChildren<Animator>();
        BarajarEspacios();
    }

    // El espacio 0 (arriba) es siempre Static de Miku; los otros 4 salen al azar sin repetirse
    private void BarajarEspacios()
    {
        espaciosRueda[0] = ID_STATIC;

        List<int> bolsa = new List<int>();
        for (int i = 0; i < poolEmotes.Length; i++) bolsa.Add(i);

        for (int k = 1; k < ESPACIOS; k++)
        {
            int r = Random.Range(0, bolsa.Count);
            espaciosRueda[k] = bolsa[r];
            bolsa.RemoveAt(r);
        }
    }

    private void ActualizarRuedaEmotes()
    {
        if (!EnJuego || menuAbierto)
        {
            if (ruedaAbierta) CerrarRueda(false);
            if (haciendoEmote) TerminarEmote();
            return;
        }

        if (Input.GetKeyDown(teclaRueda) && !ruedaAbierta) AbrirRueda();

        if (ruedaAbierta)
        {
            tiempoRueda += Time.unscaledDeltaTime;
            seleccionRueda = CalcularSeleccion();

            for (int i = 0; i < ESPACIOS; i++)
                escalaEspacio[i] = Mathf.MoveTowards(escalaEspacio[i], (i == seleccionRueda) ? 1f : 0f, Time.unscaledDeltaTime * 8f);

            // Atajos: 1-5 usan el emote directamente
            for (int i = 0; i < ESPACIOS; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    CerrarRueda(true);
                    Reproducir(espaciosRueda[i]);
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CerrarRueda(true);
                return;
            }

            // Soltar la tecla: usa el emote señalado
            if (Input.GetKeyUp(teclaRueda))
            {
                int sel = seleccionRueda;
                CerrarRueda(true);
                if (sel >= 0) Reproducir(espaciosRueda[sel]);
            }
        }

        if (haciendoEmote) ActualizarEmote();
    }

    private void AbrirRueda()
    {
        if (barajarAlAbrir) BarajarEspacios();

        ruedaAbierta = true;
        tiempoRueda = 0f;
        seleccionRueda = -1;
        for (int i = 0; i < ESPACIOS; i++) escalaEspacio[i] = 0f;

        cursorPrevioRueda = Cursor.lockState;
        cursorVisiblePrevioRueda = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CerrarRueda(bool restaurarCursor)
    {
        if (!ruedaAbierta) return;
        ruedaAbierta = false;

        if (restaurarCursor)
        {
            Cursor.lockState = cursorPrevioRueda;
            Cursor.visible = cursorVisiblePrevioRueda;
        }
    }

    // Qué botón señala el mouse (0 = arriba, sentido de las agujas del reloj). -1 = centro
    private int CalcularSeleccion()
    {
        float e = Screen.height / 1080f;
        Vector2 centro = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 d = (Vector2)Input.mousePosition - centro;

        if (d.magnitude < zonaMuerta * e) return -1;

        float ang = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;   // 0 = arriba
        if (ang < 0f) ang += 360f;
        return Mathf.RoundToInt(ang / (360f / ESPACIOS)) % ESPACIOS;
    }

    private void Reproducir(int id)
    {
        TerminarEmote();   // corta el anterior, si lo había

        haciendoEmote = true;
        idActivo = id;
        tEmote = 0f;
        rotBaseEmote = transform.rotation;
        proximoSaltoEmote = Time.time + 0.1f;
        usandoAnimador = false;

        if (id == ID_STATIC) IniciarStatic();
        else durEmote = poolEmotes[id].duracion;
    }

    private void IniciarStatic()
    {
        float largoAnim = (animacionStatic != null) ? animacionStatic.length : 0f;
        float largoMusica = (musicaStatic != null) ? musicaStatic.length : 0f;
        float auto = Mathf.Max(largoAnim, largoMusica);
        durEmote = (duracionStatic > 0f) ? duracionStatic : (auto > 0f ? auto : 10f);

        // Tu animación (si la pusiste): se reproduce sobre el Animator del personaje
        if (animadorPersonaje != null && animacionStatic != null)
        {
            AnimationPlayableUtilities.PlayClip(animadorPersonaje, animacionStatic, out grafoEmote);
            grafoActivo = true;
            usandoAnimador = true;
        }

        // Tu canción (si la pusiste)
        if (musicaStatic != null)
        {
            if (silenciarMusicaDeFondo)
            {
                foreach (AudioSource a in GetComponents<AudioSource>())
                {
                    if (a == fuenteEmote || !a.isPlaying || a.mute) continue;
                    a.mute = true;
                    silenciadas.Add(a);
                }
            }
            fuenteEmote.clip = musicaStatic;
            fuenteEmote.volume = volumenStatic;
            fuenteEmote.loop = false;
            fuenteEmote.Play();
        }
    }

    private void ActualizarEmote()
    {
        tEmote += Time.deltaTime;

        // Un emote se corta al moverte o saltar
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f
            || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f
            || Input.GetKeyDown(KeyCode.Space))
        {
            TerminarEmote();
            return;
        }

        if (tEmote >= durEmote)
        {
            TerminarEmote();
            return;
        }

        // SALTITOS: pequeños saltos reales
        if (idActivo == 2 && Time.time >= proximoSaltoEmote && EnSueloEmote())
        {
            Vector3 v = rb.linearVelocity;
            v.y = 6f;
            rb.linearVelocity = v;
            proximoSaltoEmote = Time.time + 0.6f;
        }
    }

    private bool EnSueloEmote()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 1.4f, ~0, QueryTriggerInteraction.Ignore);
    }

    private void TerminarEmote()
    {
        if (!haciendoEmote) return;
        haciendoEmote = false;

        if (grafoActivo)
        {
            if (grafoEmote.IsValid()) grafoEmote.Destroy();
            grafoActivo = false;
        }

        if (fuenteEmote != null && fuenteEmote.isPlaying) fuenteEmote.Stop();

        foreach (AudioSource a in silenciadas)
            if (a != null) a.mute = false;
        silenciadas.Clear();

        if (personaje != null) personaje.rotacionEmote = Quaternion.identity;   // vuelve a la pose normal
        else if (!usandoAnimador) transform.rotation = rotBaseEmote;
        usandoAnimador = false;
    }

    private void AplicarEmoteCuerpo()
    {
        if (!haciendoEmote || usandoAnimador) return;
        Quaternion offset = OffsetEmote(idActivo, tEmote, durEmote);

        // El modelo del pug es un objeto aparte que PersonajeRoblox gira solo,
        // así que el emote se le pasa a él (rotar al Jugador no se vería)
        if (personaje != null) personaje.rotacionEmote = offset;
        else transform.rotation = rotBaseEmote * offset;
    }

    // Movimiento de cada emote (giros e inclinaciones del cuerpo, calculados por código)
    private Quaternion OffsetEmote(int id, float t, float dur)
    {
        float p = Mathf.Clamp01(t / Mathf.Max(0.01f, dur));
        float k = Mathf.Clamp01(t / 0.15f) * Mathf.Clamp01((dur - t) / 0.25f);   // entra y sale suave

        switch (id)
        {
            case ID_STATIC:
            {
                // Baile por código al ritmo de bpmStatic: balanceo, inclinación y rebote en cada tiempo
                float beat = t * (bpmStatic / 60f);
                float yaw = Mathf.Sin(beat * Mathf.PI) * 55f;
                float roll = Mathf.Sin(beat * Mathf.PI * 2f) * 14f;
                float pitch = -Mathf.Abs(Mathf.Sin(beat * Mathf.PI)) * 12f;
                return Quaternion.Euler(pitch * k, yaw * k, roll * k);
            }
            case 0:   // GIRO: dos vueltas
                return Quaternion.Euler(0f, 720f * Mathf.SmoothStep(0f, 1f, p), 0f);

            case 1:   // BAILE
                return Quaternion.Euler(-Mathf.Abs(Mathf.Sin(t * 5f)) * 6f * k,
                                        Mathf.Sin(t * 5f) * 35f * k,
                                        Mathf.Sin(t * 10f) * 12f * k);

            case 2:   // SALTITOS (los saltos los hace ActualizarEmote)
                return Quaternion.Euler(Mathf.Sin(t * 10f) * 7f * k, Mathf.Sin(t * 5f) * 20f * k, 0f);

            case 3:   // REVERENCIA
            {
                float b = Mathf.SmoothStep(0f, 1f, Mathf.Sin(p * Mathf.PI));
                return Quaternion.Euler(45f * b, 0f, 0f);
            }
            case 4:   // DISCO: poses a golpe de ritmo
            {
                int beat = Mathf.FloorToInt(t * 4f);
                float s = (beat % 2 == 0) ? -1f : 1f;
                float f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(((t * 4f) % 1f) * 4f));
                float yaw = Mathf.Lerp(-s * 40f, s * 40f, f);
                float roll = Mathf.Lerp(-s * 12f, s * 12f, f);
                return Quaternion.Euler(0f, yaw * k, roll * k);
            }
            case 5:   // TEMBLOR
                return Quaternion.Euler(Mathf.Sin(t * 90f) * 4f * k, Mathf.Sin(t * 77f) * 8f * k, Mathf.Sin(t * 83f) * 4f * k);

            case 6:   // MAREO: gira mientras se tambalea
                return Quaternion.Euler(Mathf.Sin(t * 2f) * 20f * k,
                                        720f * Mathf.SmoothStep(0f, 1f, p),
                                        Mathf.Cos(t * 2f) * 20f * k);

            case 7:   // PENDULO: balanceo lento de lado a lado
                return Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f) * 40f * k);
        }
        return Quaternion.identity;
    }

    private void DibujarEmotes()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (!ruedaAbierta && !haciendoEmote) return;

        if (estiloEmote == null)
        {
            estiloEmote = new GUIStyle(GUI.skin.label);
            estiloEmote.alignment = TextAnchor.MiddleCenter;
            estiloEmote.fontStyle = FontStyle.Bold;
            estiloEmote.clipping = TextClipping.Overflow;
        }
        if (texDisco == null) texDisco = CrearCirculo(false);
        if (texAro == null) texAro = CrearCirculo(true);

        if (ruedaAbierta) DibujarRueda();
        if (haciendoEmote) DibujarEmoteActivo();

        GUI.color = Color.white;
    }

    private void DibujarRueda()
    {
        float e = Screen.height / 1080f;
        float W = Screen.width;
        float H = Screen.height;
        Vector2 c = new Vector2(W / 2f, H / 2f);

        float entrada = Mathf.Clamp01(tiempoRueda / 0.15f);
        float suave = 1f - (1f - entrada) * (1f - entrada);
        float alfa = suave;

        // Fondo oscuro
        GUI.color = new Color(0f, 0f, 0f, 0.55f * alfa);
        GUI.DrawTexture(new Rect(0f, 0f, W, H), Texture2D.whiteTexture);

        // Disco central
        float dc = 2f * zonaMuerta * e;
        Rect rc = new Rect(c.x - dc / 2f, c.y - dc / 2f, dc, dc);
        GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.85f * alfa);
        GUI.DrawTexture(rc, texDisco);
        GUI.color = new Color(1f, 1f, 1f, 0.35f * alfa);
        GUI.DrawTexture(rc, texAro);

        // Los 5 botones
        float R = radioRueda * e * suave;
        for (int i = 0; i < ESPACIOS; i++)
        {
            float ang = i * (2f * Mathf.PI / ESPACIOS);
            Vector2 pos = c + new Vector2(Mathf.Sin(ang), -Mathf.Cos(ang)) * R;

            int id = espaciosRueda[i];
            bool miku = (id == ID_STATIC);
            Color col = miku ? colorMiku : poolEmotes[id].color;
            string nombre = miku ? textoStatic : poolEmotes[id].nombre;

            float esc = 1f + 0.25f * escalaEspacio[i];
            float diam = 150f * e * esc;
            Rect r = new Rect(pos.x - diam / 2f, pos.y - diam / 2f, diam, diam);

            GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.85f * alfa);
            GUI.DrawTexture(r, texDisco);

            float brillo = 0.25f + 0.45f * escalaEspacio[i];
            GUI.color = new Color(col.r, col.g, col.b, brillo * alfa);
            GUI.DrawTexture(r, texDisco);

            GUI.color = new Color(col.r, col.g, col.b, alfa);
            GUI.DrawTexture(r, texAro);

            // Símbolo (♪ para Miku, la inicial para los demás) y número de atajo
            TextoEmote(r, miku ? "♪" : nombre.Substring(0, 1), (miku ? 74f : 62f) * e, Color.white, alfa);
            TextoEmote(new Rect(r.x + 8f * e, r.y + 4f * e, 36f * e, 36f * e), (i + 1).ToString(), 22f * e,
                       new Color(1f, 1f, 1f, 0.7f), alfa);

            TextoEmote(new Rect(pos.x - 170f * e, pos.y + diam / 2f + 2f * e, 340f * e, 34f * e),
                       nombre, 24f * e, col, alfa);
        }

        // Nombre del emote señalado, en el centro
        if (seleccionRueda >= 0)
        {
            int idSel = espaciosRueda[seleccionRueda];
            bool m = (idSel == ID_STATIC);
            TextoEmote(new Rect(c.x - 150f * e, c.y - 18f * e, 300f * e, 36f * e),
                       m ? "STATIC" : poolEmotes[idSel].nombre, 22f * e, m ? colorMiku : poolEmotes[idSel].color, alfa);
        }

        TextoEmote(new Rect(0f, H * 0.92f, W, 30f * e),
                   "Suelta " + teclaRueda + " sobre un emote  ·  1-5 directo  ·  Esc cancela",
                   20f * e, new Color(1f, 1f, 1f, 0.7f), alfa);
    }

    private void DibujarEmoteActivo()
    {
        float e = Screen.height / 1080f;
        float W = Screen.width;
        float H = Screen.height;

        float alfa = Mathf.Clamp01(tEmote / 0.25f) * Mathf.Clamp01((durEmote - tEmote) / 0.4f);
        bool miku = (idActivo == ID_STATIC);
        Color col = miku ? colorMiku : poolEmotes[idActivo].color;
        string nombre = miku ? textoStatic : poolEmotes[idActivo].nombre;

        TextoEmote(new Rect(0f, H * 0.82f, W, 60f * e), miku ? "♪ " + nombre + " ♪" : nombre, 46f * e, col, alfa);
        TextoEmote(new Rect(0f, H * 0.82f + 56f * e, W, 30f * e), "Muévete para cancelar", 20f * e,
                   new Color(1f, 1f, 1f, 0.6f), alfa);

        if (miku) DibujarNotas(alfa);
    }

    // Notas musicales que suben alrededor del personaje durante Static
    private void DibujarNotas(float alfa)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.2f);
        if (sp.z <= 0f) return;

        float e = Screen.height / 1080f;
        Vector2 ps = new Vector2(sp.x, Screen.height - sp.y);

        for (int i = 0; i < 7; i++)
        {
            float ph = (tEmote * 0.5f + i / 7f) % 1f;
            float x = ps.x + Mathf.Sin(i * 2.3f + tEmote * 2.2f) * (60f + i * 6f) * e;
            float y = ps.y - ph * 260f * e;
            float a = Mathf.Sin(ph * Mathf.PI) * alfa;

            TextoEmote(new Rect(x - 30f * e, y - 30f * e, 60f * e, 60f * e),
                       (i % 2 == 0) ? "♪" : "♫", (34f + (i % 3) * 8f) * e, colorMiku, a);
        }
    }

    // Texto con sombra (rueda de emotes)
    private void TextoEmote(Rect r, string t, float tam, Color c, float alfa)
    {
        estiloEmote.fontSize = Mathf.RoundToInt(tam);
        float o = Mathf.Max(1f, 2f * Screen.height / 1080f);

        GUI.color = new Color(0f, 0f, 0f, 0.85f * alfa);
        GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), t, estiloEmote);
        GUI.color = new Color(c.r, c.g, c.b, c.a * alfa);
        GUI.Label(r, t, estiloEmote);
    }

    // Disco o aro blanco con borde suave
    private Texture2D CrearCirculo(bool aro)
    {
        const int N = 128;
        Texture2D t = new Texture2D(N, N, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[N * N];
        float c = (N - 1) / 2f;
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (N * 0.5f);
                float a = aro
                    ? Mathf.Clamp01(1f - Mathf.Abs(d - 0.92f) / 0.06f)
                    : 1f - Mathf.Clamp01((d - 0.94f) / 0.05f);
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    private void OnDestroy()
    {
        if (ruedaAbierta) CerrarRueda(true);
        TerminarEmote();
        if (texDisco != null) Destroy(texDisco);
        if (texAro != null) Destroy(texAro);
    }

    // ───────────── MOD MENU ─────────────

    // Mapa 2: ya te teletransportaste, o estás en la zona del destino del portal
    private bool EnSegundoMapa
    {
        get { return enSegundoMapa || transform.position.y < destinoTP.y + 300f; }
    }

    private void AlternarModMenu()
    {
        menuAbierto = !menuAbierto;
        if (menuAbierto)
        {
            cursorPrevio = Cursor.lockState;
            cursorVisiblePrevio = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (!juegoGanado && !derrotado)
        {
            Cursor.lockState = cursorPrevio;
            Cursor.visible = cursorVisiblePrevio;
        }
    }

    private void QuitarPoderes()
    {
        tiempoBoost = 0f;
        if (contenedorBarra != null) contenedorBarra.SetActive(false);
        tiempoVuelo = 0f;
        rb.useGravity = true;
        tiempoDashPoder = 0f;
        ruletaActiva = false;
    }

    // Deja 0,2 s al reloj: se activa Pizza Time y sale el enemigo como en el juego real
    private void ForzarFinTiempo()
    {
        if (juegoGanado || derrotado || cargando) return;
        if (temporizadorActivo) tiempoRestante = 0.2f;
        else IniciarTemporizador(0.2f);
    }

    private bool Boton(string texto)
    {
        return GUILayout.Button(texto, GUILayout.Height(28f));
    }

    private void DibujarModMenu()
    {
        if (!modMenuActivo) return;

        float e = Screen.height / 1080f;

        // Aviso de depuración (por qué sale o no sale el enemigo, etc.)
        if (tiempoAvisoMod < 6f)
        {
            float a = Mathf.Clamp01((6f - tiempoAvisoMod) / 0.8f);
            estiloTexto.fontSize = Mathf.RoundToInt(26f * e);
            TextoConSombra(new Rect(0f, Screen.height * 0.08f, Screen.width, 40f * e),
                           avisoMod, estiloTexto, new Color(1f, 0.9f, 0.3f), a, 2f * e);
            GUI.color = Color.white;
        }

        if (!menuAbierto)
        {
            estiloTexto.fontSize = Mathf.RoundToInt(16f * e);
            TextoConSombra(new Rect(Screen.width - 270f * e, Screen.height - 36f * e, 250f * e, 26f * e),
                           teclaModMenu + " · MOD MENU", estiloTexto, new Color(1f, 1f, 1f, 0.55f), 1f, 1f * e);
            GUI.color = Color.white;
            return;
        }

        Matrix4x4 matrizOriginal = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(e, e, 1f));   // el menú escala con la resolución

        float alto = Screen.height / e - 250f;
        Rect caja = new Rect(20f, 230f, 340f, alto);
        GUI.color = new Color(0f, 0f, 0f, 0.88f);
        GUI.DrawTexture(caja, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(caja.x + 10f, caja.y + 8f, caja.width - 20f, caja.height - 16f));
        GUILayout.Label("MOD MENU  (" + teclaModMenu + " para cerrar)");
        scrollMod = GUILayout.BeginScrollView(scrollMod);

        GUILayout.Label("── VIDAS ──");
        vidasInfinitas = GUILayout.Toggle(vidasInfinitas, " Vidas infinitas");
        if (Boton("Llenar vidas")) vidas = vidasMaximas;

        GUILayout.Label("── PODERES ──");
        GUILayout.BeginHorizontal();
        if (Boton("Impulso")) AplicarPoder(0);
        if (Boton("Vuelo")) AplicarPoder(1);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (Boton("Dash")) AplicarPoder(3);
        if (Boton("+1 Vida")) AplicarPoder(2);
        GUILayout.EndHorizontal();
        if (Boton("Ruleta aleatoria")) IniciarRuleta();
        if (Boton("Quitar poderes")) QuitarPoderes();
        saltosInfinitos = GUILayout.Toggle(saltosInfinitos, " Saltos infinitos");

        GUILayout.Label("── MAPA ──");
        if (Boton("Mostrar portal TP"))
        {
            if (portalTP != null) portalTP.SetActive(true);
            else Debug.LogWarning("Falta asignar Portal TP en el Jugador");
        }
        if (Boton("Teletransportar al mapa 2")) Teletransportar();
        if (Boton("Mostrar cilindro Win"))
        {
            if (cilindroWin != null) cilindroWin.SetActive(true);
            else Debug.LogWarning("Falta asignar Cilindro Win en el Jugador");
        }
        if (Boton("Volver al checkpoint")) RespawnEnCheckpoint();
        if (Boton("Ganar (probar animación)")) Ganar();

        GUILayout.Label("── TIEMPO ──");
        tiempoCongelado = GUILayout.Toggle(tiempoCongelado, " Congelar reloj");
        if (Boton("Terminar tiempo (Pizza Time)")) ForzarFinTiempo();
        GUILayout.BeginHorizontal();
        if (Boton("+60 s")) { if (temporizadorActivo) tiempoRestante += 60f; }
        if (Boton("Reiniciar reloj")) IniciarTemporizador(EnSegundoMapa ? tiempoTP : tiempoInicio);
        GUILayout.EndHorizontal();

        GUILayout.Label("── ENEMIGO ──");
        GUILayout.Label("Estado: " + EstadoEnemigo());
        if (Boton("Spawnear Pizza Face")) SpawnEnemigoAhora();
        if (Boton("Quitar Pizza Face"))
        {
            if (enemigoPizza != null) enemigoPizza.SetActive(false);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        GUI.matrix = matrizOriginal;
        GUI.color = Color.white;
    }

    private void LateUpdate()
    {
        // Mod menu abierto: mantiene el cursor libre aunque otro script lo bloquee
        if (menuAbierto)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (!animWinActiva) return;

        tiempoAnimWin += Time.deltaTime;
        float t = tiempoAnimWin;

        // ── Personaje: gira 360° en el salto y luego posa mirando a la cámara ──
        const float TIEMPO_SALTO = 0.9f;
        Camera cam = Camera.main;

        Quaternion rotPose = rotInicioWin;
        if (cam != null)
        {
            Vector3 haciaCam = cam.transform.position - transform.position;
            haciaCam.y = 0f;
            if (haciaCam.sqrMagnitude > 0.001f) rotPose = Quaternion.LookRotation(haciaCam);
        }
        rotPose *= Quaternion.Euler(0f, correccionRotacionWin, 0f);

        if (t < TIEMPO_SALTO)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / TIEMPO_SALTO);
            transform.rotation = Quaternion.Euler(0f, 360f * p, 0f) * rotInicioWin;
        }
        else
        {
            float b = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - TIEMPO_SALTO) / 0.3f));
            Quaternion pose = rotPose * Quaternion.Euler(-10f, 0f, Mathf.Sin(t * 5f) * 4f);   // se echa un poco hacia atrás, festejando
            transform.rotation = Quaternion.Slerp(rotInicioWin, pose, b);
        }

        // ── Cámara: zoom suave al personaje ──
        if (cam != null && fovOriginal > 0f)
        {
            float z = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.8f));
            cam.fieldOfView = Mathf.Lerp(fovOriginal, fovOriginal * zoomCamaraWin, z);
        }

        // ── Estrella: aparece, sube sobre la cabeza, gira y brilla ──
        if (estrellaWin != null)
        {
            float ts = Mathf.Clamp01((t - 0.25f) / 0.5f);
            float sube = ts * ts * (3f - 2f * ts);
            float pulso = 1f + 0.08f * Mathf.Sin(t * 10f);

            Vector3 pos = transform.position + Vector3.up * (1f + (alturaEstrella - 1f) * sube);
            pos += Vector3.up * (0.15f * Mathf.Sin(t * 6f));

            estrellaWin.transform.position = pos;
            estrellaWin.transform.rotation = Quaternion.Euler(0f, t * 240f, 0f);
            estrellaWin.transform.localScale = Vector3.one * escalaEstrella * sube * pulso;

            // Chispas doradas
            if (ts > 0.3f)
            {
                chispaAcum += Time.deltaTime * 45f;
                while (chispaAcum >= 1f)
                {
                    chispaAcum -= 1f;
                    CrearChispa(pos);
                }
            }
        }

        // Mover y apagar chispas
        for (int i = chispas.Count - 1; i >= 0; i--)
        {
            Chispa c = chispas[i];
            c.vida -= Time.deltaTime;
            if (c.vida <= 0f || c.t == null)
            {
                if (c.t != null) Destroy(c.t.gameObject);
                chispas.RemoveAt(i);
                continue;
            }
            c.t.position += c.vel * Time.deltaTime;
            c.t.localScale = Vector3.one * 0.14f * (c.vida / c.max);
        }
    }

    private GameObject NuevaPrimitiva(PrimitiveType tipo)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        Collider c = g.GetComponent<Collider>();
        if (c != null)
        {
            c.enabled = false;   // sin colliders: es solo decoración
            Destroy(c);
        }
        return g;
    }

    private Material CrearMaterial(Color color, bool brillo)
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
        temp.SetActive(false);
        Material m = new Material(temp.GetComponent<Renderer>().sharedMaterial);
        Destroy(temp);

        m.color = color;
        if (m.HasProperty("_EmissionColor"))
        {
            if (brillo)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.05f) * 1.3f);
            }
            else
            {
                m.SetColor("_EmissionColor", Color.black);
            }
        }
        return m;
    }

    private void CrearEstrellaWin()
    {
        if (estrellaWin != null) return;

        matDorado = CrearMaterial(new Color(1f, 0.85f, 0.1f), true);
        Material matOjos = CrearMaterial(new Color(0.05f, 0.05f, 0.05f), false);

        estrellaWin = new GameObject("EstrellaWin");
        estrellaWin.transform.position = transform.position + Vector3.up;
        estrellaWin.transform.localScale = Vector3.zero;

        MeshFilter mf = estrellaWin.AddComponent<MeshFilter>();
        mf.mesh = CrearMallaEstrella(0.6f, 0.27f, 0.22f);
        MeshRenderer mr = estrellaWin.AddComponent<MeshRenderer>();
        mr.sharedMaterial = matDorado;

        // Ojitos de la estrella (por delante y por detrás)
        for (int lado = -1; lado <= 1; lado += 2)
        {
            for (int ojo = -1; ojo <= 1; ojo += 2)
            {
                GameObject o = NuevaPrimitiva(PrimitiveType.Sphere);
                o.transform.SetParent(estrellaWin.transform, false);
                o.transform.localPosition = new Vector3(0.09f * ojo, 0.05f, 0.14f * lado);
                o.transform.localScale = new Vector3(0.07f, 0.13f, 0.06f);
                o.GetComponent<Renderer>().sharedMaterial = matOjos;
            }
        }
    }

    // Estrella de 5 puntas en 3D (doble cara, sombreado plano)
    private static Mesh CrearMallaEstrella(float rExt, float rInt, float grosor)
    {
        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();

        Vector3 centroF = new Vector3(0f, 0f, -grosor);
        Vector3 centroB = new Vector3(0f, 0f, grosor);

        Vector3[] anillo = new Vector3[10];
        for (int i = 0; i < 10; i++)
        {
            float a = (90f + i * 36f) * Mathf.Deg2Rad;
            float r = (i % 2 == 0) ? rExt : rInt;
            anillo[i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        }

        for (int i = 0; i < 10; i++)
        {
            Vector3 p1 = anillo[i];
            Vector3 p2 = anillo[(i + 1) % 10];
            AgregarTriDoble(v, t, centroF, p1, p2);
            AgregarTriDoble(v, t, centroB, p2, p1);
        }

        Mesh m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // Cada triángulo se agrega dos veces, con vértices propios, para verse desde ambos lados
    private static void AgregarTriDoble(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        t.Add(i); t.Add(i + 1); t.Add(i + 2);

        v.Add(a); v.Add(c); v.Add(b);
        t.Add(i + 3); t.Add(i + 4); t.Add(i + 5);
    }

    private void CrearChispa(Vector3 centro)
    {
        GameObject g = NuevaPrimitiva(PrimitiveType.Cube);
        g.transform.position = centro + Random.insideUnitSphere * 0.4f;
        g.transform.rotation = Random.rotation;
        g.transform.localScale = Vector3.one * 0.14f;
        g.GetComponent<Renderer>().sharedMaterial = matDorado;

        Chispa c = new Chispa();
        c.t = g.transform;
        c.vel = Random.onUnitSphere * Random.Range(1.5f, 3.5f);
        c.max = Random.Range(0.5f, 0.9f);
        c.vida = c.max;
        chispas.Add(c);
    }

    private void Ganar()
    {
        if (juegoGanado) return;
        juegoGanado = true;

        // Termina el vuelo para que no quede flotando
        tiempoVuelo = 0f;
        rb.useGravity = true;

        retrasoWin = 0f;
        if (animacionEstrella) IniciarAnimacionVictoria();

        if (textoGanaste != null && !pantallaGanasteCodigo) textoGanaste.SetActive(true);
        tiempoDesdeGanar = 0f;
        Debug.Log("¡Ganaste!");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Invoke(nameof(ReiniciarJuego), segundosParaReiniciar + retrasoWin);
    }

    // ───────────── DIBUJO (OnGUI) ─────────────

    private void OnGUI()
    {
        if (estiloTexto == null)
        {
            estiloTexto = new GUIStyle(GUI.skin.label);
            estiloTexto.alignment = TextAnchor.MiddleCenter;
            estiloTexto.fontStyle = FontStyle.Bold;
        }
        if (estiloLetra == null)
        {
            estiloLetra = new GUIStyle(GUI.skin.label);
            estiloLetra.alignment = TextAnchor.MiddleCenter;
            estiloLetra.fontStyle = FontStyle.BoldAndItalic;
        }

        DibujarCombo();
        DibujarIndicadorImpulso();
        DibujarPoderesActivos();
        DibujarRuleta();
        DibujarYahoo();
        DibujarPantallaGanaste();
        DibujarVidas();
        DibujarContador();
        DibujarTemporizador();
        DibujarPizzaTime();
        DibujarPantallaDerrota();
        DibujarEmotes();
        DibujarPantallaCarga();   // siempre al final para que quede encima de todo
        DibujarModMenu();
    }

    // Contador de monedas (arriba a la izquierda, debajo de los corazones)
    private void DibujarContador()
    {
        if (!mostrarContador) return;

        float e = Screen.height / 1080f;
        Rect panel = new Rect(30f * e, 100f * e, 300f * e, 110f * e);
        bool completo = totalMonedas > 0 && contador_coleccionables >= totalMonedas;
        Color dorado = new Color(1f, 0.85f, 0.2f);
        Color colorPrincipal = completo ? new Color(0.4f, 1f, 0.5f) : dorado;

        // Rebote al recoger una moneda
        float pop = 1f + 0.22f * (1f - Mathf.Clamp01(tiempoDesdeMoneda / 0.25f));
        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), new Vector2(panel.x, panel.center.y));
        Matrix4x4 matrizPop = GUI.matrix;

        // Fondo y franja lateral
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = colorPrincipal;
        GUI.DrawTexture(new Rect(panel.x, panel.y, 5f * e, panel.height), Texture2D.whiteTexture);

        // Moneda que "gira" (se ensancha y se aplasta)
        Vector2 centroMoneda = new Vector2(panel.x + 52f * e, panel.y + 46f * e);
        float giro = 0.15f + 0.85f * Mathf.Abs(Mathf.Cos(Time.time * 2.5f));
        GUIUtility.ScaleAroundPivot(new Vector2(giro, 1f), centroMoneda);
        GUI.color = new Color(0.75f, 0.5f, 0.05f);
        GUI.DrawTexture(new Rect(centroMoneda.x - 26f * e, centroMoneda.y - 26f * e, 52f * e, 52f * e), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.85f, 0.25f);
        GUI.DrawTexture(new Rect(centroMoneda.x - 20f * e, centroMoneda.y - 20f * e, 40f * e, 40f * e), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 0.75f, 0.85f);
        GUI.DrawTexture(new Rect(centroMoneda.x - 14f * e, centroMoneda.y - 14f * e, 12f * e, 12f * e), Texture2D.whiteTexture);
        GUI.matrix = matrizPop;

        // Texto
        estiloTexto.fontSize = Mathf.RoundToInt(20f * e);
        TextoConSombra(new Rect(panel.x + 90f * e, panel.y + 4f * e, panel.width - 100f * e, 24f * e),
                       "MONEDAS", estiloTexto, new Color(1f, 1f, 1f, 0.7f), 1f, 1.5f * e);

        estiloTexto.fontSize = Mathf.RoundToInt(46f * e);
        TextoConSombra(new Rect(panel.x + 90f * e, panel.y + 26f * e, panel.width - 100f * e, 56f * e),
                       contador_coleccionables + " / " + totalMonedas,
                       estiloTexto, colorPrincipal, 1f, 2.5f * e);

        // Barra de progreso
        float progreso = (totalMonedas > 0) ? Mathf.Clamp01((float)contador_coleccionables / totalMonedas) : 0f;
        Rect fondoBarra = new Rect(panel.x + 16f * e, panel.y + 90f * e, panel.width - 32f * e, 10f * e);
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        GUI.DrawTexture(fondoBarra, Texture2D.whiteTexture);
        Rect relleno = fondoBarra;
        relleno.width *= progreso;
        GUI.color = colorPrincipal;
        GUI.DrawTexture(relleno, Texture2D.whiteTexture);

        // Marca de cuántas monedas abren el portal
        if (totalMonedas > 0 && monedasParaTP <= totalMonedas)
        {
            float xMarca = fondoBarra.x + fondoBarra.width * ((float)monedasParaTP / totalMonedas);
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(xMarca - 1.5f * e, fondoBarra.y - 3f * e, 3f * e, 16f * e), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
        GUI.matrix = matrizOriginal;
    }

    private static readonly Vector2[] desplazamientosContorno =
    {
        new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(0f, -1f), new Vector2(0f, 1f),
        new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f)
    };

    // Texto con contorno negro grueso
    private void TextoContorno(Rect r, string texto, GUIStyle estilo, Color color, float alfa, float grosor)
    {
        GUI.color = new Color(0f, 0f, 0f, alfa);
        foreach (Vector2 d in desplazamientosContorno)
            GUI.Label(new Rect(r.x + d.x * grosor, r.y + d.y * grosor, r.width, r.height), texto, estilo);

        GUI.color = new Color(color.r, color.g, color.b, alfa);
        GUI.Label(r, texto, estilo);
    }

    // Texto de color con borde de otro color
    private void TextoConBorde(Rect r, string texto, GUIStyle estilo, Color color, Color borde, float alfa, float grosor)
    {
        GUI.color = new Color(borde.r, borde.g, borde.b, alfa);
        foreach (Vector2 d in desplazamientosContorno)
            GUI.Label(new Rect(r.x + d.x * grosor, r.y + d.y * grosor, r.width, r.height), texto, estilo);

        GUI.color = new Color(color.r, color.g, color.b, alfa);
        GUI.Label(r, texto, estilo);
    }

    // "IT'S PIZZA TIME!" estilo Pizza Tower cuando se acaba el tiempo
    private void DibujarPizzaTime()
    {
        if (tiempoPizza > duracionTextoPizza) return;

        float e = Screen.height / 1080f;
        float t = tiempoPizza;
        float alfa = Mathf.Clamp01((duracionTextoPizza - t) / 0.5f);
        float cy = Screen.height * 0.40f;

        // Destello blanco inicial
        if (t < 0.3f)
        {
            GUI.color = new Color(1f, 1f, 1f, (1f - t / 0.3f) * 0.8f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        }

        // Banda roja con bordes amarillos que se abre
        float abre = Mathf.Clamp01(t / 0.15f);
        float altoBanda = 260f * e * abre;
        GUI.color = new Color(0.55f, 0.02f, 0.02f, 0.8f * alfa);
        GUI.DrawTexture(new Rect(0f, cy - altoBanda / 2f, Screen.width, altoBanda), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.85f, 0.1f, alfa);
        GUI.DrawTexture(new Rect(0f, cy - altoBanda / 2f, Screen.width, 6f * e), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0f, cy + altoBanda / 2f - 6f * e, Screen.width, 6f * e), Texture2D.whiteTexture);

        // Entrada con golpe (grande -> normal), luego late, tiembla y se balancea
        float entrada = Mathf.Clamp01(t / 0.25f);
        float suave = 1f - Mathf.Pow(1f - entrada, 3f);
        float escala = (t < 0.25f) ? Mathf.Lerp(3.2f, 1f, suave) : 1f + 0.04f * Mathf.Sin(t * 14f);
        float giro = Mathf.Sin(Time.time * 9f) * 2.5f;
        Vector2 temblor = (t < 0.25f)
            ? Vector2.zero
            : new Vector2(Mathf.Sin(Time.time * 47f), Mathf.Cos(Time.time * 39f)) * 5f * e;
        Color color = (Mathf.FloorToInt(Time.time * 8f) % 2 == 0)
            ? new Color(1f, 0.9f, 0.15f)
            : new Color(1f, 0.35f, 0.15f);

        Vector2 centro = new Vector2(Screen.width / 2f, cy);
        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(escala, escala), centro);
        GUIUtility.RotateAroundPivot(giro, centro);

        estiloLetra.fontSize = Mathf.RoundToInt(130f * e);
        TextoContorno(new Rect(temblor.x, cy - 90f * e + temblor.y, Screen.width, 180f * e),
                      textoPizzaTime, estiloLetra, color, alfa, 5f * e);

        estiloTexto.fontSize = Mathf.RoundToInt(34f * e);
        TextoConSombra(new Rect(temblor.x, cy + 82f * e + temblor.y, Screen.width, 44f * e),
                       "¡EL SUELO SE ESTÁ DESVANECIENDO!", estiloTexto, Color.white, alfa, 2f * e);

        GUI.matrix = matrizOriginal;
        GUI.color = Color.white;
    }

    // Reloj de cuenta regresiva (arriba a la derecha)
    private void DibujarTemporizador()
    {
        bool enEspera = !temporizadorActivo && !tiempoAgotado;
        if (enEspera && !mostrarRelojSiempre) return;

        // En espera muestra el tiempo que te darán al llegar al portal (tiempoTP)
        float restMostrar = enEspera ? tiempoTP : tiempoRestante;
        float durMostrar = enEspera ? Mathf.Max(1f, tiempoTP) : duracionTemporizador;

        float e = Screen.height / 1080f;
        float ancho = 260f * e;
        float alto = 104f * e;
        Rect panel = new Rect(Screen.width - ancho - 30f * e, 30f * e, ancho, alto);

        bool alerta = !enEspera && (tiempoAgotado || tiempoRestante <= segundosAlerta);
        float parpadeo = alerta ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 6f)) : 1f;
        Color color = alerta ? new Color(1f, 0.25f, 0.25f)
                             : (enEspera ? new Color(0.7f, 0.7f, 0.7f) : Color.white);

        float pop = 1f + 0.3f * (1f - Mathf.Clamp01(tiempoDesdeInicioTemporizador / 0.3f));
        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), panel.center);

        // Fondo y franja superior
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(color.r, color.g, color.b, 1f);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 5f * e), Texture2D.whiteTexture);

        // Texto
        estiloTexto.fontSize = Mathf.RoundToInt(20f * e);
        TextoConSombra(new Rect(panel.x, panel.y + 8f * e, panel.width, 24f * e),
                       enEspera ? "TIEMPO · " + Mathf.CeilToInt(tiempoTP / 60f) + " MIN"
                                : tiempoAgotado ? "¡TIEMPO AGOTADO!"
                                : (tiempoRestante <= 60f ? "¡ÚLTIMO MINUTO!"
                                                         : "QUEDAN " + Mathf.CeilToInt(tiempoRestante / 60f) + " MINUTOS"),
                       estiloTexto, new Color(1f, 1f, 1f, 0.75f), 1f, 1.5f * e);

        int total = Mathf.CeilToInt(restMostrar);
        string texto = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        estiloTexto.fontSize = Mathf.RoundToInt(50f * e);
        TextoConSombra(new Rect(panel.x, panel.y + 30f * e, panel.width, 54f * e),
                       texto, estiloTexto, color, parpadeo, 2.5f * e);

        // Barra que se vacía
        float progreso = Mathf.Clamp01(restMostrar / durMostrar);
        Rect fondoBarra = new Rect(panel.x + 16f * e, panel.y + 86f * e, panel.width - 32f * e, 10f * e);
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        GUI.DrawTexture(fondoBarra, Texture2D.whiteTexture);
        Rect relleno = fondoBarra;
        relleno.width *= progreso;
        GUI.color = new Color(color.r, color.g, color.b, 1f);
        GUI.DrawTexture(relleno, Texture2D.whiteTexture);

        // Una marca por cada minuto que falta: cuando la barra pasa una marca, se fue un minuto
        for (float minuto = 60f; minuto < durMostrar - 0.5f; minuto += 60f)
        {
            float xMarca = fondoBarra.x + fondoBarra.width * (minuto / durMostrar);
            GUI.color = new Color(1f, 1f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(xMarca - 1.5f * e, fondoBarra.y - 4f * e, 3f * e, fondoBarra.height + 8f * e), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
        GUI.matrix = matrizOriginal;
    }

    // Corazones de vida (arriba a la izquierda)
    private void DibujarVidas()
    {
        float e = Screen.height / 1080f;
        estiloTexto.fontSize = Mathf.RoundToInt(48f * e);

        for (int i = 0; i < vidasMaximas; i++)
        {
            Color c = (i < vidas) ? new Color(1f, 0.2f, 0.25f) : new Color(0.35f, 0.35f, 0.35f);
            TextoConSombra(new Rect(30f * e + i * 55f * e, 30f * e, 55f * e, 55f * e),
                           "♥", estiloTexto, c, (i < vidas) ? 1f : 0.6f, 2f * e);
        }
        GUI.color = Color.white;
    }

    // Pantalla de derrota estilo Super Mario 64:
    // 1) un círculo (iris) se cierra sobre el personaje, 2) en negro caen las letras de GAME OVER
    private void DibujarPantallaDerrota()
    {
        if (!derrotado) return;

        float e = Screen.height / 1080f;
        float W = Screen.width;
        float H = Screen.height;
        float t = tiempoDesdeDerrota;

        // ── 1) Iris que se cierra ──
        float p = Mathf.Clamp01(t / Mathf.Max(0.1f, duracionIris));
        if (p < 1f)
        {
            float suave = p * p * (3f - 2f * p);
            float dx = Mathf.Max(irisCentro.x, W - irisCentro.x);
            float dy = Mathf.Max(irisCentro.y, H - irisCentro.y);
            float radioInicial = Mathf.Sqrt(dx * dx + dy * dy);
            DibujarIris(irisCentro, Mathf.Lerp(radioInicial, 0f, suave));
        }
        else
        {
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, W, H), Texture2D.whiteTexture);
        }

        // ── 2) Letras que caen con rebote ──
        float tl = t - duracionIris - retrasoLetrasGameOver;
        string txt = textoGameOver;

        if (tl > 0f && !string.IsNullOrEmpty(txt))
        {
            estiloLetra.fontSize = Mathf.RoundToInt(150f * e);
            float sep = 6f * e;

            float[] anchos = new float[txt.Length];
            float total = 0f;
            for (int i = 0; i < txt.Length; i++)
            {
                anchos[i] = (txt[i] == ' ')
                    ? estiloLetra.fontSize * 0.4f
                    : estiloLetra.CalcSize(new GUIContent(txt[i].ToString())).x;
                total += anchos[i] + sep;
            }

            float x = (W - total) / 2f;
            float yBase = H * 0.42f - 100f * e;

            for (int i = 0; i < txt.Length; i++)
            {
                float ti = tl - i * intervaloLetras;
                if (ti > 0f && txt[i] != ' ')
                {
                    float pop = Mathf.Clamp01(ti / 0.5f);
                    float caida = (1f - RebotarSuave(pop)) * 420f * e;                 // cae desde arriba
                    float ondula = (pop >= 1f) ? Mathf.Sin(Time.unscaledTime * 4f + i * 0.7f) * 8f * e : 0f;
                    float alfa = Mathf.Clamp01(ti / 0.08f);

                    TextoConBorde(new Rect(x, yBase - caida + ondula, anchos[i], 200f * e),
                                  txt[i].ToString(), estiloLetra,
                                  coloresLetraGameOver[i % coloresLetraGameOver.Length], Color.white,
                                  alfa, 5f * e);
                }
                x += anchos[i] + sep;
            }

            // ── 3) Cuenta atrás para reiniciar ──
            float finLetras = txt.Length * intervaloLetras + 0.6f;
            if (tl > finLetras)
            {
                float a = Mathf.Clamp01((tl - finLetras) / 0.3f);
                float parpadeo = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
                int restante = Mathf.CeilToInt(Mathf.Max(0f, segundosParaReiniciar - t));

                estiloTexto.fontSize = Mathf.RoundToInt(32f * e);
                TextoConSombra(new Rect(0f, H * 0.66f, W, 44f * e),
                               "Reiniciando en " + restante + "...",
                               estiloTexto, Color.white, a * parpadeo, 2f * e);
            }
        }

        GUI.color = Color.white;
    }

    // Círculo que se cierra: un cuadrado con un agujero redondo + 4 rectángulos negros alrededor
    private void DibujarIris(Vector2 c, float r)
    {
        if (texturaIris == null) CrearTexturaIris();

        float W = Screen.width;
        float H = Screen.height;

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(c.x - r, c.y - r, 2f * r, 2f * r), texturaIris);
        GUI.DrawTexture(new Rect(0f, 0f, Mathf.Max(0f, c.x - r), H), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(c.x + r, 0f, Mathf.Max(0f, W - (c.x + r)), H), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(c.x - r, 0f, 2f * r, Mathf.Max(0f, c.y - r)), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(c.x - r, c.y + r, 2f * r, Mathf.Max(0f, H - (c.y + r))), Texture2D.whiteTexture);
    }

    // Textura 256x256: transparente dentro del círculo, opaca fuera (borde suave)
    private void CrearTexturaIris()
    {
        const int N = 256;
        texturaIris = new Texture2D(N, N, TextureFormat.RGBA32, false);
        texturaIris.wrapMode = TextureWrapMode.Clamp;
        texturaIris.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[N * N];
        float c = (N - 1) / 2f;
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (N * 0.5f);
                byte a = (byte)(Mathf.Clamp01((d - 0.96f) / 0.04f) * 255f);
                px[y * N + x] = new Color32(255, 255, 255, a);
            }
        }
        texturaIris.SetPixels32(px);
        texturaIris.Apply();
    }

    // Curva de rebote (0 -> 1 rebotando al final)
    private static float RebotarSuave(float x)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if (x < 1f / d1) return n1 * x * x;
        if (x < 2f / d1) { x -= 1.5f / d1; return n1 * x * x + 0.75f; }
        if (x < 2.5f / d1) { x -= 2.25f / d1; return n1 * x * x + 0.9375f; }
        x -= 2.625f / d1;
        return n1 * x * x + 0.984375f;
    }

    // Lista de poderes activos (abajo a la izquierda) + avisos de daño y checkpoint
    private void DibujarPoderesActivos()
    {
        float e = Screen.height / 1080f;
        float x = 30f * e;
        float y = Screen.height - 60f * e;
        float alto = 40f * e;
        float ancho = 420f * e;

        estiloTexto.fontSize = Mathf.RoundToInt(28f * e);

        if (tiempoDashPoder > 0f)
        {
            TextoConSombra(new Rect(x, y, ancho, alto),
                           "DASH (W W)  " + tiempoDashPoder.ToString("0.0") + "s",
                           estiloTexto, coloresPoder[3], 1f, 2f * e);
            y -= alto;
        }
        if (tiempoVuelo > 0f)
        {
            TextoConSombra(new Rect(x, y, ancho, alto),
                           "VUELO  " + tiempoVuelo.ToString("0.0") + "s",
                           estiloTexto, coloresPoder[1], 1f, 2f * e);
        }

        // Aviso al perder vidas
        if (tiempoMensajeVida < 2f)
        {
            float a = Mathf.Clamp01((2f - tiempoMensajeVida) / 0.5f);
            estiloTexto.fontSize = Mathf.RoundToInt(48f * e);
            TextoConSombra(new Rect(0f, Screen.height * 0.25f, Screen.width, 70f * e),
                           "-" + ultimoDano + (ultimoDano == 1 ? " VIDA" : " VIDAS"),
                           estiloTexto, new Color(1f, 0.25f, 0.25f), a, 3f * e);
        }

        // Aviso al guardar checkpoint
        if (tiempoCheckpoint < 2f)
        {
            float a = Mathf.Clamp01((2f - tiempoCheckpoint) / 0.5f);
            estiloTexto.fontSize = Mathf.RoundToInt(36f * e);
            TextoConSombra(new Rect(0f, Screen.height * 0.33f, Screen.width, 50f * e),
                           "CHECKPOINT", estiloTexto, new Color(0.4f, 1f, 0.5f), a, 2f * e);
        }
        GUI.color = Color.white;
    }

    // Ruleta: gira por los poderes, frena en el premio y muestra su duración
    private void DibujarRuleta()
    {
        if (!ruletaActiva) return;

        float e = Screen.height / 1080f;
        float total = duracionRuleta + duracionResultadoRuleta;
        float alfa = Mathf.Clamp01((total - tiempoRuleta) / 0.4f) * Mathf.Clamp01(tiempoRuleta / 0.2f);

        float p = Mathf.Clamp01(tiempoRuleta / duracionRuleta);
        float suave = 1f - Mathf.Pow(1f - p, 3f);
        int pasos = Mathf.FloorToInt(suave * (5 * TOTAL_PODERES + resultadoRuleta));
        int actual = pasos % TOTAL_PODERES;
        int anterior = (actual + TOTAL_PODERES - 1) % TOTAL_PODERES;
        int siguiente = (actual + 1) % TOTAL_PODERES;
        bool termino = tiempoRuleta >= duracionRuleta;

        float anchoP = 700f * e;
        float altoP = 330f * e;
        Rect panel = new Rect((Screen.width - anchoP) / 2f, Screen.height * 0.16f, anchoP, altoP);

        // Fondo
        GUI.color = new Color(0f, 0f, 0f, 0.78f * alfa);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);

        // Título
        estiloTexto.fontSize = Mathf.RoundToInt(34f * e);
        TextoConSombra(new Rect(panel.x, panel.y + 10f * e, panel.width, 45f * e),
                       "¡RULETA DE PODERES!", estiloTexto, Color.white, alfa, 2f * e);

        // Franja de selección
        Color colorActual = coloresPoder[actual];
        Rect franja = new Rect(panel.x + 30f * e, panel.y + 118f * e, panel.width - 60f * e, 90f * e);
        GUI.color = new Color(colorActual.r, colorActual.g, colorActual.b, 0.25f * alfa);
        GUI.DrawTexture(franja, Texture2D.whiteTexture);

        // Anterior / actual / siguiente
        estiloTexto.fontSize = Mathf.RoundToInt(32f * e);
        TextoConSombra(new Rect(panel.x, panel.y + 68f * e, panel.width, 44f * e),
                       nombresPoder[anterior], estiloTexto, new Color(1f, 1f, 1f, 0.45f), alfa, 2f * e);
        TextoConSombra(new Rect(panel.x, panel.y + 214f * e, panel.width, 44f * e),
                       nombresPoder[siguiente], estiloTexto, new Color(1f, 1f, 1f, 0.45f), alfa, 2f * e);

        float latido = termino ? 1f + 0.08f * Mathf.Sin(Time.time * 12f) : 1f;
        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(latido, latido), franja.center);
        estiloTexto.fontSize = Mathf.RoundToInt(66f * e);
        TextoConSombra(franja, nombresPoder[actual], estiloTexto, colorActual, alfa, 4f * e);
        GUI.matrix = matrizOriginal;

        // Resultado: descripción y duración
        if (termino)
        {
            estiloTexto.fontSize = Mathf.RoundToInt(24f * e);
            TextoConSombra(new Rect(panel.x, panel.y + 262f * e, panel.width, 30f * e),
                           DescripcionPoder(actual), estiloTexto, Color.white, alfa, 2f * e);
            TextoConSombra(new Rect(panel.x, panel.y + 292f * e, panel.width, 30f * e),
                           "Duración: " + DuracionPoderTexto(actual), estiloTexto, colorActual, alfa, 2f * e);
        }

        GUI.color = Color.white;
    }

    // "¡YAHOO!" mientras el personaje agarra la estrella
    private void DibujarYahoo()
    {
        if (!animWinActiva || tiempoAnimWin < 0.35f || tiempoAnimWin > duracionAnimacionWin) return;

        float e = Screen.height / 1080f;
        float t = tiempoAnimWin - 0.35f;
        float pop = Mathf.Clamp01(t / 0.25f);
        float escala = Mathf.Lerp(2.5f, 1f, 1f - (1f - pop) * (1f - pop));
        float alfa = Mathf.Clamp01((duracionAnimacionWin - tiempoAnimWin) / 0.4f) * Mathf.Clamp01(t / 0.1f);

        Vector2 centro = new Vector2(Screen.width / 2f, Screen.height * 0.2f);
        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(escala, escala), centro);
        GUIUtility.RotateAroundPivot(Mathf.Sin(Time.time * 8f) * 3f, centro);

        estiloLetra.fontSize = Mathf.RoundToInt(120f * e);
        TextoContorno(new Rect(0f, centro.y - 80f * e, Screen.width, 160f * e),
                      "¡YAHOO!", estiloLetra, new Color(1f, 0.88f, 0.25f), alfa, 5f * e);

        GUI.matrix = matrizOriginal;
        GUI.color = Color.white;
    }

    private void DibujarPantallaGanaste()
    {
        if (!juegoGanado || !pantallaGanasteCodigo) return;

        float tg = tiempoDesdeGanar - retrasoWin;   // espera a que termine la animación de la estrella
        if (tg < 0f) return;

        float e = Screen.height / 1080f;
        float fade = Mathf.Clamp01(tg / 0.5f);

        GUI.color = new Color(0f, 0f, 0f, 0.8f * fade);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        float pop = Mathf.Clamp01((tg - 0.2f) / 0.35f);
        float suave = 1f - (1f - pop) * (1f - pop);
        float escala = Mathf.Lerp(2.2f, 1f, suave);
        float alfaTitulo = Mathf.Clamp01((tg - 0.2f) / 0.2f);

        Vector2 centro = new Vector2(Screen.width / 2f, Screen.height * 0.40f);
        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(escala, escala), centro);

        estiloLetra.fontSize = Mathf.RoundToInt(170f * e);
        TextoConSombra(new Rect(centro.x - 700f * e, centro.y - 110f * e, 1400f * e, 220f * e),
                       "¡GANASTE!", estiloLetra, new Color(1f, 0.88f, 0.25f), alfaTitulo, 7f * e);

        GUI.matrix = matrizOriginal;

        float alfaSub = Mathf.Clamp01((tg - 0.7f) / 0.3f);
        estiloTexto.fontSize = Mathf.RoundToInt(42f * e);
        TextoConSombra(new Rect(0f, Screen.height * 0.58f, Screen.width, 60f * e),
                       "Monedas: " + contador_coleccionables + " / " + totalMonedas,
                       estiloTexto, Color.white, alfaSub, 2f * e);

        int restante = Mathf.CeilToInt(Mathf.Max(0f, segundosParaReiniciar - tg));
        estiloTexto.fontSize = Mathf.RoundToInt(28f * e);
        TextoConSombra(new Rect(0f, Screen.height * 0.67f, Screen.width, 40f * e),
                       "Reiniciando en " + restante + "...",
                       estiloTexto, new Color(1f, 1f, 1f, 0.8f), alfaSub, 2f * e);

        GUI.color = Color.white;
    }

    private void DibujarPantallaCarga()
    {
        if (!cargando) return;

        float e = Screen.height / 1080f;
        float transcurrido = Time.unscaledTime - inicioCarga;
        float progreso = Mathf.Clamp01(transcurrido / duracionCarga);
        float alfa = Mathf.Clamp01((duracionCarga - transcurrido) / 0.2f);

        GUI.color = new Color(0.04f, 0.04f, 0.08f, alfa);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        string puntos = new string('.', (int)(transcurrido * 4f) % 4);
        estiloTexto.fontSize = Mathf.RoundToInt(64f * e);
        GUI.color = new Color(1f, 1f, 1f, alfa);
        GUI.Label(new Rect(Screen.width / 2f - 400f * e, Screen.height / 2f - 70f * e, 800f * e, 90f * e),
                  "CARGANDO" + puntos, estiloTexto);

        Rect fondo = new Rect(Screen.width / 2f - 250f * e, Screen.height / 2f + 50f * e, 500f * e, 10f * e);
        GUI.color = new Color(1f, 1f, 1f, 0.2f * alfa);
        GUI.DrawTexture(fondo, Texture2D.whiteTexture);
        Rect relleno = fondo;
        relleno.width *= progreso;
        GUI.color = new Color(1f, 0.9f, 0.2f, alfa);
        GUI.DrawTexture(relleno, Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    private void TextoConSombra(Rect r, string texto, GUIStyle estilo, Color color, float alfa, float desplazamiento)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.85f * alfa);
        GUI.Label(new Rect(r.x + desplazamiento, r.y + desplazamiento, r.width, r.height), texto, estilo);
        GUI.color = new Color(color.r, color.g, color.b, alfa);
        GUI.Label(r, texto, estilo);
    }

    private void DibujarCombo()
    {
        if (!mostrarCombo || alfaCombo <= 0.01f) return;

        float e = Screen.height / 1080f;
        float cx = Screen.width - 220f * e;
        float cy = 300f * e;

        Color color = (rangoActual >= letrasRango.Length - 1)
            ? Color.HSVToRGB(Mathf.Repeat(Time.time * 0.7f, 1f), 0.75f, 1f)
            : coloresRango[rangoActual];

        float pop = 1f + 0.30f * (1f - Mathf.Clamp01(tiempoDesdePickup / 0.15f));
        float t = Mathf.Clamp01(tiempoDesdeCambioRango / 0.4f);
        float escalaRango = 1f + 0.55f * (1f - t);
        float giro = Mathf.Sin(tiempoDesdeCambioRango * 45f) * 7f * (1f - t);

        Vector2 centro = new Vector2(cx, cy);
        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(pop * escalaRango, pop * escalaRango), centro);
        GUIUtility.RotateAroundPivot(giro, centro);

        estiloLetra.fontSize = Mathf.RoundToInt((rangoActual >= 5 ? 150f : 190f) * e);
        TextoConSombra(new Rect(cx - 190f * e, cy - 120f * e, 380f * e, 200f * e),
                       letrasRango[rangoActual], estiloLetra, color, alfaCombo, 6f * e);

        estiloTexto.fontSize = Mathf.RoundToInt(30f * e);
        TextoConSombra(new Rect(cx - 200f * e, cy + 80f * e, 400f * e, 40f * e),
                       nombresRango[rangoActual], estiloTexto, color, alfaCombo, 2f * e);

        float progreso = (puntosEstilo - rangoActual * PUNTOS_POR_RANGO) / PUNTOS_POR_RANGO;
        Rect fondoBarra = new Rect(cx - 130f * e, cy + 128f * e, 260f * e, 12f * e);
        GUI.color = new Color(0f, 0f, 0f, 0.6f * alfaCombo);
        GUI.DrawTexture(new Rect(fondoBarra.x - 2f * e, fondoBarra.y - 2f * e, fondoBarra.width + 4f * e, fondoBarra.height + 4f * e), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, 0.2f * alfaCombo);
        GUI.DrawTexture(fondoBarra, Texture2D.whiteTexture);
        Rect relleno = fondoBarra;
        relleno.width *= Mathf.Clamp01(progreso);
        GUI.color = new Color(color.r, color.g, color.b, alfaCombo);
        GUI.DrawTexture(relleno, Texture2D.whiteTexture);

        estiloTexto.fontSize = Mathf.RoundToInt(26f * e);
        TextoConSombra(new Rect(cx - 150f * e, cy + 148f * e, 300f * e, 34f * e),
                       "COMBO x" + combo, estiloTexto, Color.white, alfaCombo, 2f * e);

        GUI.color = Color.white;
        GUI.matrix = matrizOriginal;
    }

    private void DibujarIndicadorImpulso()
    {
        if (!mostrarIndicadorPantalla || tiempoBoost <= 0f) return;

        float escala = Screen.height / 1080f;
        float ancho = 460f * escala;
        float alto = 130f * escala;
        Rect panel = new Rect((Screen.width - ancho) / 2f, 40f * escala, ancho, alto);

        float pop = Mathf.Clamp01(tiempoDesdeActivacion / 0.25f);
        float rebote = 1f + 0.4f * (1f - pop);
        float alfa = Mathf.Clamp01(tiempoBoost / 1f);

        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(rebote, rebote), panel.center);

        GUI.color = new Color(0f, 0f, 0f, 0.6f * alfa);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);

        estiloTexto.fontSize = Mathf.RoundToInt(44f * escala);
        GUI.color = new Color(1f, 0.9f, 0.2f, alfa);
        GUI.Label(new Rect(panel.x, panel.y + 8f * escala, panel.width, 55f * escala),
                  "¡IMPULSO!", estiloTexto);

        estiloTexto.fontSize = Mathf.RoundToInt(24f * escala);
        GUI.color = new Color(1f, 1f, 1f, alfa);
        GUI.Label(new Rect(panel.x, panel.y + 58f * escala, panel.width, 30f * escala),
                  "Velocidad x" + multiplicadorBoost.ToString("0.#") + "  ·  " + tiempoBoost.ToString("0.0") + "s",
                  estiloTexto);

        float margen = 24f * escala;
        Rect fondoBarra = new Rect(panel.x + margen, panel.y + 98f * escala, panel.width - margen * 2f, 14f * escala);
        GUI.color = new Color(1f, 1f, 1f, 0.25f * alfa);
        GUI.DrawTexture(fondoBarra, Texture2D.whiteTexture);

        Rect relleno = fondoBarra;
        relleno.width *= Mathf.Clamp01(tiempoBoost / duracionBoost);
        GUI.color = new Color(1f, 0.9f, 0.2f, alfa);
        GUI.DrawTexture(relleno, Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.matrix = matrizOriginal;
    }
}
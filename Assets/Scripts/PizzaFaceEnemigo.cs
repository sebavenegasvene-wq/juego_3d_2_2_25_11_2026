using UnityEngine;

// Enemigo estilo "Pizza Face" de Pizza Tower.
// - Solo aparece cuando movimiento.cs lo llama con Aparecer() (al acabarse el tiempo).
// - Flota, te persigue sin parar y, al tocarte, te quita una vida y te empuja suavemente.
// - Al aparecer suena una risa.
// - El modelo 3D (pizza con cara) se construye por código, no necesitas ningún modelo.
public class PizzaFaceEnemigo : MonoBehaviour
{
    [Header("Referencias")]
    public movimiento jugador;                 // si lo dejas vacío, lo busca solo
    public AudioClip risa;                     // risa al aparecer
    [Range(0f, 1f)] public float volumenRisa = 1f;

    [Header("Persecución")]
    public float velocidad = 6f;               // el jugador camina a 8, así que se puede escapar corriendo
    public float alturaFlote = 0.5f;           // a qué altura del jugador flota
    public float amplitudBalanceo = 0.6f;      // vaivén vertical
    public float frecuenciaBalanceo = 4f;

    [Header("Golpe")]
    public float radioContacto = 1.6f;         // distancia a la que cuenta como "te tocó"
    public int dano = 1;
    public float pausaTrasGolpe = 1.2f;        // se aleja un poco después de golpear
    public float velocidadRetroceso = 3f;

    [Header("Modelo (generado por código)")]
    public bool crearModeloPorCodigo = true;   // desmárcalo si quieres usar tu propio modelo como hijo
    public float escalaModelo = 1.6f;
    public bool mirarACamara = true;           // la cara siempre te mira de frente

    private AudioSource fuente;
    private Transform modelo;
    private bool autorizado = false;           // solo true cuando movimiento.cs lo manda a aparecer
    private float inicioAparicion = 0f;
    private float reanudaEn = 0f;

    // Colores tomados de la imagen
    private static readonly Color cContorno = new Color(0.05f, 0.04f, 0.04f);
    private static readonly Color cCorteza = new Color(0.82f, 0.55f, 0.38f);
    private static readonly Color cQueso = new Color(0.96f, 0.87f, 0.52f);
    private static readonly Color cTomate = new Color(0.88f, 0.18f, 0.13f);
    private static readonly Color cPimiento = new Color(0.45f, 0.78f, 0.25f);

    // Profundidad de los ingredientes (la cara mira hacia -Z)
    private const float Z_ING = -0.17f;

    // ───────────── API: la llama movimiento.cs ─────────────
    public void Aparecer(Vector3 posicion)
    {
        autorizado = true;
        if (transform.parent != null) transform.SetParent(null, true);   // por si estaba dentro de un objeto apagado
        transform.localScale = Vector3.one;
        transform.position = posicion;
        gameObject.SetActive(true);            // al activarse suena la risa (OnEnable)
    }

    void Awake()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;              // 2D: se escucha igual en todo el mapa

        if (crearModeloPorCodigo) CrearModelo();
    }

    void OnEnable()
    {
        // Si el objeto estaba activo en la escena al dar Play, se esconde solo:
        // únicamente puede aparecer cuando se acaba el tiempo
        if (!autorizado)
        {
            gameObject.SetActive(false);
            return;
        }

        if (jugador == null) jugador = FindFirstObjectByType<movimiento>();
        reanudaEn = 0f;
        inicioAparicion = Time.time;
        if (risa != null) fuente.PlayOneShot(risa, volumenRisa);   // ¡Jajaja!
    }

    void Update()
    {
        // Mirar a la cámara
        if (mirarACamara && Camera.main != null)
        {
            Vector3 d = transform.position - Camera.main.transform.position;
            if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
        }

        // Animación del modelo: aparece con rebote, late y se balancea riéndose
        if (modelo != null)
        {
            float pop = Mathf.Clamp01((Time.time - inicioAparicion) / 0.4f);
            float latido = 1f + 0.05f * Mathf.Sin(Time.time * 10f);
            modelo.localScale = Vector3.one * escalaModelo * Mathf.SmoothStep(0f, 1f, pop) * latido;
            modelo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 6f) * 8f);
        }

        if (jugador == null || !jugador.EnJuego) return;

        Vector3 objetivo = jugador.transform.position + Vector3.up * alturaFlote;
        Vector3 haciaJugador = objetivo - transform.position;
        float dist = haciaJugador.magnitude;

        // Vaivén suave
        transform.position += Vector3.up * Mathf.Cos(Time.time * frecuenciaBalanceo) * amplitudBalanceo * Time.deltaTime;

        // Pausa después de golpear: se aleja un poco
        if (Time.time < reanudaEn)
        {
            transform.position -= haciaJugador.normalized * velocidadRetroceso * Time.deltaTime;
            return;
        }

        if (dist <= radioContacto)
        {
            jugador.GolpeEnemigo(transform.position, dano);
            reanudaEn = Time.time + pausaTrasGolpe;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidad * Time.deltaTime);
    }

    // ───────────── MODELO 3D POR CÓDIGO ─────────────

    // Convierte un punto de la imagen (en píxeles) a coordenadas del modelo
    private static Vector3 Px(float x, float y)
    {
        return new Vector3((x - 545f) / 350f, -(y - 525f) / 350f, 0f);
    }

    private GameObject Parte(PrimitiveType tipo, Vector3 pos, Vector3 escala, Vector3 euler, Color color)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        Collider c = g.GetComponent<Collider>();
        if (c != null) Destroy(c);             // sin colliders: el contacto se calcula por distancia

        g.transform.SetParent(modelo, false);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = euler;
        g.transform.localScale = escala;

        Renderer r = g.GetComponent<Renderer>();
        r.material.color = color;
        return g;
    }

    // Disco (cilindro acostado): diámetro y grosor
    private void Disco(float x, float y, float z, float diametro, float grosor, Color color)
    {
        Parte(PrimitiveType.Cylinder, new Vector3(x, y, z),
              new Vector3(diametro, grosor / 2f, diametro), new Vector3(90f, 0f, 0f), color);
    }

    // Ingrediente redondo aplastado (tomate, nariz...)
    private void Bola(float x, float y, float diametro, Color color)
    {
        Parte(PrimitiveType.Sphere, new Vector3(x, y, Z_ING),
              new Vector3(diametro, diametro, 0.16f), Vector3.zero, color);
    }

    // Tira curva hecha de elipsoides siguiendo una lista de puntos (x1,y1,x2,y2,... en píxeles)
    private void Trazo(float[] pts, float grosor, Color color)
    {
        for (int i = 0; i + 3 < pts.Length; i += 2)
        {
            Vector3 a = Px(pts[i], pts[i + 1]);
            Vector3 b = Px(pts[i + 2], pts[i + 3]);
            Vector3 medio = (a + b) / 2f;
            medio.z = Z_ING;
            Vector3 d = b - a;
            float largo = d.magnitude * 1.25f + grosor * 0.5f;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Parte(PrimitiveType.Sphere, medio, new Vector3(largo, grosor, 0.14f), new Vector3(0f, 0f, ang), color);
        }
    }

    private void CrearModelo()
    {
        GameObject raiz = new GameObject("ModeloPizza");
        modelo = raiz.transform;
        modelo.SetParent(transform, false);
        modelo.localScale = Vector3.one * escalaModelo;

        // Contorno negro, corteza y queso
        Disco(0f, 0f, 0.03f, 2.12f, 0.30f, cContorno);
        Disco(0f, 0f, 0f, 2.00f, 0.26f, cCorteza);
        Disco(0f, 0.01f, -0.02f, 1.72f, 0.28f, cQueso);

        // Ojos: dos pepperonis de distinto tamaño
        Vector3 ojoIzq = Px(402f, 422f);
        Vector3 ojoDer = Px(672f, 448f);
        Bola(ojoIzq.x, ojoIzq.y, 0.34f, cTomate);
        Bola(ojoDer.x, ojoDer.y, 0.50f, cTomate);

        // Cejas de pimiento (enojadas)
        Trazo(new float[] { 315f, 298f, 400f, 262f, 460f, 305f, 492f, 352f }, 0.11f, cPimiento);
        Trazo(new float[] { 605f, 345f, 650f, 300f, 715f, 262f, 790f, 300f }, 0.11f, cPimiento);

        // Nariz de champiñón (varias bolitas)
        Vector3 n = Px(495f, 575f);
        Bola(n.x, n.y, 0.36f, cCorteza);
        Bola(n.x - 0.15f, n.y - 0.05f, 0.20f, cCorteza);
        Bola(n.x + 0.16f, n.y - 0.06f, 0.20f, cCorteza);
        Bola(n.x + 0.02f, n.y + 0.13f, 0.22f, cCorteza);

        // Pliegue de la mejilla izquierda
        Trazo(new float[] { 270f, 430f, 300f, 480f, 360f, 520f, 390f, 560f }, 0.09f, cPimiento);

        // Sonrisa grande de pimiento (dos arcos)
        Trazo(new float[] { 275f, 525f, 330f, 610f, 420f, 665f, 540f, 680f, 650f, 665f, 760f, 620f, 820f, 570f }, 0.13f, cPimiento);
        Trazo(new float[] { 300f, 585f, 330f, 700f, 400f, 780f, 500f, 815f, 600f, 800f, 690f, 740f, 745f, 625f }, 0.12f, cPimiento);

        // Mueca del lado derecho
        Trazo(new float[] { 610f, 570f, 700f, 545f, 790f, 520f, 830f, 515f }, 0.08f, cPimiento);
    }
}
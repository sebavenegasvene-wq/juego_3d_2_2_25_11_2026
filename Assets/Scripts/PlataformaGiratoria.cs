using UnityEngine;

// Pon este script en la plataforma (el objeto que tiene el Collider).
// - Gira lentamente.
// - El jugador gira junto con ella mientras está encima.
// - Está "anclada" al sistema de colapso: si no tiene el tag "pizo"/"pizo2" se le pone,
//   así desaparece con las demás plataformas cuando se acaba el tiempo.
[DisallowMultipleComponent]
public class PlataformaGiratoria : MonoBehaviour
{
    [Header("Giro")]
    public float velocidadGiro = 20f;          // grados por segundo (20 = una vuelta cada 18 s)
    public Vector3 eje = Vector3.up;           // eje local de giro (arriba = gira como un plato)
    public bool invertirSentido = false;

    [Header("Jugador")]
    public bool llevarJugador = true;          // el jugador gira con la plataforma mientras está encima

    [Header("Desaparición con el tiempo")]
    public string tagPiso = "pizo";            // tag que se le pone si no tiene "pizo"/"pizo2" (usa "pizo2" en el mapa 2)

    private Rigidbody jugadorEncima;
    private float ultimoContacto = -99f;

    void Awake()
    {
        // Ancla la plataforma al colapso: los pisos con estos tags son los que desaparecen
        if (!string.IsNullOrEmpty(tagPiso) && !gameObject.CompareTag("pizo") && !gameObject.CompareTag("pizo2"))
        {
            try { gameObject.tag = tagPiso; }
            catch (UnityException)
            {
                Debug.LogWarning("PlataformaGiratoria: el tag '" + tagPiso + "' no existe, créalo en Tags and Layers.");
            }
        }
    }

    void FixedUpdate()
    {
        if (eje.sqrMagnitude < 0.0001f) return;

        float grados = velocidadGiro * (invertirSentido ? -1f : 1f) * Time.fixedDeltaTime;
        if (Mathf.Abs(grados) < 0.0001f) return;

        Vector3 ejeMundo = transform.TransformDirection(eje.normalized);
        Quaternion delta = Quaternion.AngleAxis(grados, ejeMundo);
        transform.rotation = delta * transform.rotation;

        // Arrastra al jugador alrededor del centro de la plataforma
        if (llevarJugador && jugadorEncima != null && Time.fixedTime - ultimoContacto < 0.15f)
        {
            Vector3 desfase = jugadorEncima.position - transform.position;
            jugadorEncima.position = transform.position + delta * desfase;
        }
    }

    void OnCollisionStay(Collision colision)
    {
        if (!llevarJugador) return;

        Rigidbody otro = colision.rigidbody;
        if (otro == null || otro.isKinematic) return;
        if (otro.GetComponent<movimiento>() == null) return;   // solo el jugador

        // Solo cuenta si está parado ENCIMA (no si choca de lado)
        foreach (ContactPoint punto in colision.contacts)
        {
            if (Mathf.Abs(punto.normal.y) > 0.5f && otro.position.y > punto.point.y)
            {
                jugadorEncima = otro;
                ultimoContacto = Time.fixedTime;
                return;
            }
        }
    }
}

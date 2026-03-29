using UnityEngine;

public class DestroyPArticle : MonoBehaviour
{
    public float lifetime = 15f;


    void Start()
    {
        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }

    
}

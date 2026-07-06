using UnityEngine;

namespace ZakhanPixelParticles
{

	public class PixelParticles_Shoot : MonoBehaviour
{
    public Rigidbody2D Bullet;

    void Start()
    {
        Create();
    }

    void Create()
    {
        Instantiate(Bullet, transform.position, transform.rotation);
    }
}
}

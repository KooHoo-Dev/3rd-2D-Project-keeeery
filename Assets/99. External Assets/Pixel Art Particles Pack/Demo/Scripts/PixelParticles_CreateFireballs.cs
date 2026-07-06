using UnityEngine;

namespace ZakhanPixelParticles
{

	public class PixelParticles_CreateFireballs : MonoBehaviour {

    public Rigidbody2D Fireball;

    private bool CreateInstances = true;
    private Rigidbody2D Instance;
    public float Time = 1.0f;

	void Start () {
        Create();
        InvokeRepeating("Create", 1.0f, Time);
	}
    void Update()
    {
        if(Instance == null)
        {
            CreateInstances = true;
        }
    }

    void Create()
    {
        if (CreateInstances)
        {
            Instance = Instantiate(Fireball, transform.position, transform.rotation); 
            CreateInstances = false;
        }
    }
   private void OnDisable()
    {
          if(Instance)
             Destroy(Instance.gameObject);
     }
   }
}

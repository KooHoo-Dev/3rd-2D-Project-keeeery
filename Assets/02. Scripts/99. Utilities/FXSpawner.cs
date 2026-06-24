using UnityEngine;

public class FxSpawner : MonoBehaviour
{
    [SerializeField] private Transform fxRoot;

    public void Spawn(GameObject prefab, Vector3 position, int direction = 1)
    {
        GameObject fx = Instantiate(prefab, position, Quaternion.identity, fxRoot);

        Vector3 scale = fx.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        fx.transform.localScale = scale;
    }
}
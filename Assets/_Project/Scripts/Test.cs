using UnityEngine;

public class Test : MonoBehaviour
{
    private void Start()
    {
        HelloWorld();
    }

    [ContextMenu("Say Hello")]
    public void HelloWorld()
    {
        Debug.Log("goodmorning");
    }
}
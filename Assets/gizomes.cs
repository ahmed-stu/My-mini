using UnityEngine;

public class gizomes : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    // ضع هذا في أي MonoBehaviour على BobberSpawnPoint
void OnDrawGizmos() {
  Gizmos.color = Color.yellow;
  Gizmos.DrawWireSphere(transform.position, 0.1f);
}

}

using UnityEngine;
using Mirror;

public class ItemBox : NetworkBehaviour
{
    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var handler = collision.gameObject.GetComponent<ItemEffectHandler>();

            if (handler != null)
            {
                int randomEffect = Random.Range(0, 3);

                handler.Svr_ApplyItemEffect(randomEffect);

                NetworkServer.Destroy(gameObject);
            }
        }
    }
}

using UnityEngine;
namespace MODZ.RTS.Units
{
    public class HealthPack : MonoBehaviour
    {
        public int healthIncreaseAmount = 50;

        private void OnTriggerEnter(Collider other)
        {
            // Check for CapsuleCollider
            CapsuleCollider cc = other.GetComponent<CapsuleCollider>();
            if (cc != null)
            {
                // It's a unit with a CapsuleCollider. Now check whether it's a player unit or an enemy unit.
                Player.PlayerUnit pU = other.GetComponent<Player.PlayerUnit>();
                Enemy.EnemyUnit eU = other.GetComponent<Enemy.EnemyUnit>();

                if (pU != null) // It's a player unit.
                {
                    Debug.Log("Papastammm");
                    if (pU.currentHealth + healthIncreaseAmount > pU.health)
                    {
                        pU.currentHealth = pU.health;
                    }
                    else
                    {
                        pU.currentHealth += healthIncreaseAmount;
                    }
                    Destroy(gameObject);
                }
                else if (eU != null) // It's an enemy unit.
                {
                    if (eU.currentHealth + healthIncreaseAmount > eU.health)
                    {
                        eU.currentHealth = eU.health;
                    }
                    else
                    {
                        eU.currentHealth += healthIncreaseAmount;
                    }
                    Destroy(gameObject);
                }
            }
        }
    }
}


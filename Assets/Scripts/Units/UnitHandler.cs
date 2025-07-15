using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MODZ.RTS.Player;

namespace MODZ.RTS.Units
{
    public class UnitHandler : MonoBehaviour
    {
        public static UnitHandler instance;

        [SerializeField]
        public BasicUnit worker, warrior, healer, mage;

        public LayerMask pUnitLayer, eUnitLayer;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            pUnitLayer = LayerMask.NameToLayer("Player Units");
            eUnitLayer = LayerMask.NameToLayer("Enemy Units");
        }

        public UnitStatType.Base GetBasicUnitStats(string type)
        {
            BasicUnit unit;
            switch (type)
            {
                case "fighter":
                    unit = worker;
                    break;
                case "ranger":
                    unit = warrior;
                    break;
                case "priest":
                    unit = healer;
                    break;
                case "mage":
                    unit = mage;
                    break;
                default:
                    Debug.Log($"Unit Type: {type} could not be found or does not exist!");
                    return null;
            }
            return unit.baseStats;
        }
        public void SetBasicUnitStats(Transform type)
        {
            Transform pUnits = PlayerManager.instance.playerUnits;
            Transform eUnits = PlayerManager.instance.enemyUnits;
            foreach (Transform child in type)
            {
                foreach (Transform unit in child)
                {   //aferoume to teleutaio grama giati sta units leei Healers kai oxi healer 
                    string unitName = child.name.Substring(0, child.name.Length - 1).ToLower();
                    var stats = GetBasicUnitStats(unitName);

                    if (type == pUnits)
                    {
                        Player.PlayerUnit pU = unit.GetComponent<Player.PlayerUnit>();
                        pU.baseStats = GetBasicUnitStats(unitName);
                    }
                    else if (type == eUnits)
                    {
                        Enemy.EnemyUnit eU = unit.GetComponent<Enemy.EnemyUnit>();
                        eU.baseStats = GetBasicUnitStats(unitName);
                    }

                }
            }
        }
    }
}

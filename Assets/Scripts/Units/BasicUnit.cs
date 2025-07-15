using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MODZ.RTS.Units
{
    [CreateAssetMenu(fileName = "New Unit", menuName = "New Unit/Basic")]
    public class BasicUnit : ScriptableObject
    {
        public enum unitType
        {
            Fighter,
            Ranger,
            Priest,
            Mage
        }

        [Header("Unit Settings")]
        public unitType type;

        public new string name;

        public GameObject playerPrefab;
        public GameObject enemyPrefab;

        [Header("Unit Stats")]

        public UnitStatType.Base baseStats;
    }
}


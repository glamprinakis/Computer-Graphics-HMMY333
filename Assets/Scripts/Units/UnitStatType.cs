using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MODZ.RTS.Units
{
    public class UnitStatType : ScriptableObject
    {
        [System.Serializable]
        public class Base
        {
            public float movingRange, atkRange, attack, health, armor;
        }
    }

}

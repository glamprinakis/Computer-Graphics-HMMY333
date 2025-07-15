using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalVariables
{
    public struct characterAttributes
    {
        public string cClass;
        public int dexteriry;
        public int constitution;
        public int charisma;
        public int stength;
        public int vitality;
    }

    public static List<characterAttributes> characters = new List<characterAttributes>();

    public static int map;

    public static string enemyType;

    public static string difficulty;

}


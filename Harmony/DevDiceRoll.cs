using System.IO;
using HarmonyLib;
using UnityEngine;

public class OcbDevDiceRoll : IModApi
{

    public void InitMod(Mod mod)
    {
        Debug.Log("Loading OCB Dev Dice Roll Patch: " + GetType().ToString());
    }

}

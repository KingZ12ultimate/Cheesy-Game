using UnityEngine;

[CreateAssetMenu(fileName = "GlobalInfo", menuName = "Scriptable Objects/GlobalInfo")]
public class GlobalInfo : ScriptableObject
{
    public float bulletDamage = 20f;
    public float bulletLifeSpan = 2f;
}

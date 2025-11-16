using UnityEngine;

public class LayerHelpers
{
    public static bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return ((mask.value & (1 << obj.layer)) != 0);
    }

}

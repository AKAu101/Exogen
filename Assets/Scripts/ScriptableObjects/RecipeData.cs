using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeData", menuName = "Scriptable Objects/RecipeData")]
public class RecipeData : ScriptableObject
{
    //Currently Value is Fixed at 1 for everything, could be changed later to use itemstacks etc.

    [SerializeField] private ItemData firstIngredient;
    [SerializeField] private ItemData secondIngredient;

    [SerializeField] private ItemData result;

    public ItemData FirstIngredient => firstIngredient;
    public ItemData SecondIngredient => secondIngredient;
    public ItemData Result => result;


}

public readonly struct RecipeKey : IEquatable<RecipeKey>
{
    public readonly ItemData A;
    public readonly ItemData B;

    public RecipeKey(ItemData x, ItemData y)
    {
        // Normalize so (A,B) == (B,A)
        // Use instance IDs to pick a canonical order.
        if (x == null || y == null) { A = x; B = y; return; }
        int ix = x.GetInstanceID();
        int iy = y.GetInstanceID();
        if (ix <= iy) { A = x; B = y; }
        else { A = y; B = x; }
    }

    public bool Equals(RecipeKey other) => ReferenceEquals(A, other.A) && ReferenceEquals(B, other.B);
    public override bool Equals(object obj) => obj is RecipeKey r && Equals(r);
    public override int GetHashCode() => HashCode.Combine(A, B); // order-independent because we normalized
}
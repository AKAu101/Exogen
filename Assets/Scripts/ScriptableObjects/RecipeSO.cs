using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeSO", menuName = "Scriptable Objects/RecipeSO")]
public class RecipeSO : ScriptableObject
{
    //Currently Value is Fixed at 1 for everything, could be changed later to use itemstacks etc.

    [SerializeField] private ItemSO firstIngredient;
    [SerializeField] private ItemSO secondIngredient;

    [SerializeField] private ItemSO result;

    public ItemSO FirstIngredient => firstIngredient;
    public ItemSO SecondIngredient => secondIngredient;
    public ItemSO Result => result;


}

public readonly struct RecipeKey : IEquatable<RecipeKey>
{
    public readonly ItemSO A;
    public readonly ItemSO B;

    public RecipeKey(ItemSO x, ItemSO y)
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
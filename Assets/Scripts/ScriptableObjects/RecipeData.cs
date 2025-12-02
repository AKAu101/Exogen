using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeData", menuName = "Scriptable Objects/RecipeData")]
public class RecipeData : ScriptableObject
{
    //Currently Value is Fixed at 1 for everything, could be changed later to use itemstacks etc.

    [SerializeField] private ItemData firstIngredient;
    [SerializeField] private ItemData secondIngredient;
    [SerializeField] private ItemData thirdIngredient;
    [SerializeField] private ItemData fourthIngredient;

    [SerializeField] private ItemData result;

    public ItemData FirstIngredient => firstIngredient;
    public ItemData SecondIngredient => secondIngredient;
    public ItemData ThirdIngredient => thirdIngredient;
    public ItemData FourthIngredient => fourthIngredient;
    public ItemData Result => result;


}

public readonly struct RecipeKey : IEquatable<RecipeKey>
{
    public readonly ItemData A;
    public readonly ItemData B;
    public readonly ItemData C;
    public readonly ItemData D;

    public RecipeKey(ItemData w, ItemData x, ItemData y, ItemData z)
    {
        // Normalize so any permutation of (w,x,y,z) results in the same key
        // Sort by instance ID to create canonical order
        ItemData[] items = new ItemData[] { w, x, y, z };
        System.Array.Sort(items, (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return a.GetInstanceID().CompareTo(b.GetInstanceID());
        });

        A = items[0];
        B = items[1];
        C = items[2];
        D = items[3];
    }

    public bool Equals(RecipeKey other) =>
        ReferenceEquals(A, other.A) &&
        ReferenceEquals(B, other.B) &&
        ReferenceEquals(C, other.C) &&
        ReferenceEquals(D, other.D);

    public override bool Equals(object obj) => obj is RecipeKey r && Equals(r);
    public override int GetHashCode() => HashCode.Combine(A, B, C, D);
}
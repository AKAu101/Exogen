using Generals;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class RecipeBook : Singleton<RecipeBook>
{

    [SerializeField] private List<RecipeSO> recipes;

    private Dictionary<RecipeKey, RecipeSO> lookup;

    protected override void Awake()
    {
        base.Awake();

        lookup = new Dictionary<RecipeKey, RecipeSO>(recipes.Count);
        foreach (var r in recipes)
        {
            var key = new RecipeKey(r.FirstIngredient, r.SecondIngredient);
            lookup[key] = r;
        }
    }

    public bool TryGetRecipe(ItemSO a, ItemSO b, out RecipeSO recipe) 
        => lookup.TryGetValue(new RecipeKey(a, b), out recipe);
}

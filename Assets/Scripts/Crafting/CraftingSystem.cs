using Generals;
using System.Collections.Generic;
using UnityEngine;

public class CraftingSystem : Singleton<CraftingSystem>
{

    [SerializeField] private List<RecipeData> recipes;

    private Dictionary<RecipeKey, RecipeData> lookup;

    protected override void Awake()
    {
        base.Awake();
        ServiceLocator.Instance.Register(this);

        lookup = new Dictionary<RecipeKey, RecipeData>(recipes.Count);
        foreach (var r in recipes)
        {
            var key = new RecipeKey(r.FirstIngredient, r.SecondIngredient, r.ThirdIngredient, r.FourthIngredient);
            lookup[key] = r;
        }
    }

    public bool TryGetRecipe(ItemData a, ItemData b, ItemData c, ItemData d, out RecipeData recipe)
        => lookup.TryGetValue(new RecipeKey(a, b, c, d), out recipe);
}

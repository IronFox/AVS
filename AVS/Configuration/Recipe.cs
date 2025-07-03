using Nautilus.Crafting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AVS.Configuration
{
    public readonly struct RecipeIngredient
    {
        public TechType Type { get; }
        public int Amount { get; }
        public RecipeIngredient(TechType type, int amount)
        {
            Type = type;
            Amount = amount;
        }
    }

    /// <summary>
    /// Sequential builder for a <see cref="Recipe"/>.
    /// </summary>
    public class RecipeBuilder
    {
        private List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        public RecipeBuilder Add(TechType type, int amount)
        {
            if (type == TechType.None || amount <= 0)
                return this;
            ingredients.Add(new RecipeIngredient(type, amount));
            return this;
        }
        public Recipe Build()
        {
            return new Recipe(ingredients);
        }

    }


    /// <summary>
    /// Readonly vehicle construction recipe.
    /// </summary>
    public class Recipe : IEnumerable<RecipeIngredient>, IEquatable<Recipe>
    {
        public static Recipe Example { get; } = new Recipe(new List<RecipeIngredient>
            {
                new RecipeIngredient(TechType.Titanium, 2),
                new RecipeIngredient(TechType.Quartz, 1),
                new RecipeIngredient(TechType.PowerCell, 1)
            });

        /// <summary>
        /// Converts a <see cref="RecipeData"/> to a <see cref="Recipe"/>.
        /// </summary>
        /// <param name="recipeData">Data to import</param>
        /// <returns>Imported data</returns>
        public static Recipe Import(RecipeData recipeData, Recipe fallback)
        {
            if (recipeData is null
                || recipeData.Ingredients == null
                || recipeData.Ingredients.Count == 0
                )
            {
                Logger.Error("RecipeData is null or has no ingredients. Returning fallback Recipe.");
                return fallback ?? throw new ArgumentNullException(nameof(fallback), "fallback must not be null");
            }
            if (recipeData.craftAmount != 1)
            {
                Logger.Error("RecipeData produces amounts other than 1. Returning fallback Recipe.");
                return fallback ?? throw new ArgumentNullException(nameof(fallback), "fallback must not be null");
            }
            if (recipeData.linkedItemCount != 1)
            {
                Logger.Error("RecipeData has non-empty linked item count. Returning fallback Recipe.");
                return fallback ?? throw new ArgumentNullException(nameof(fallback), "fallback must not be null");
            }
            return new Recipe(recipeData.Ingredients.Select(
                ingredient => new RecipeIngredient(ingredient.techType, ingredient.amount)
                ));
        }


        private Dictionary<TechType, int> IngredientsDictionary { get; } = new Dictionary<TechType, int>();
        public Recipe(IEnumerable<RecipeIngredient> ingredients)
        {
            foreach (var ingredient in ingredients)
            {
                IngredientsDictionary[ingredient.Type] = ingredient.Amount;
            }
        }

        public RecipeData ToRecipeData()
        {
            var recipeData = new RecipeData();
            foreach (var ingredient in IngredientsDictionary)
            {
                recipeData.Ingredients.Add(new CraftData.Ingredient(ingredient.Key, ingredient.Value));
            }
            return recipeData;
        }

        public IEnumerator<RecipeIngredient> GetEnumerator()
        {
            foreach (var kvp in IngredientsDictionary)
            {
                yield return new RecipeIngredient(kvp.Key, kvp.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Checks if the recipe is valid for vehicle registration.
        /// </summary>
        /// <param name="vehicleName"></param>
        /// <returns></returns>
        public bool CheckValidity(string vehicleName)
        {
            bool badRecipeFlag = false;
            foreach (var ingredient in this)
            {
                try
                {
                    if (ingredient.Type.EncodeKey() is null)
                        throw new KeyNotFoundException($"TechType {ingredient.Type} is not registered or does not exist.");
                }
                catch (System.Exception e)
                {
                    Logger.LogException($"Vehicle Recipe Error: {vehicleName}'s recipe had an invalid tech type: {ingredient.Type}. Probably you are referencing an unregistered/non-existent techtype", e);
                    badRecipeFlag = true;
                }
            }
            return !badRecipeFlag;
        }
        public bool Equals(Recipe other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (IngredientsDictionary.Count != other.IngredientsDictionary.Count)
                return false;

            foreach (var kvp in IngredientsDictionary)
            {
                if (!other.IngredientsDictionary.TryGetValue(kvp.Key, out int otherAmount))
                    return false;
                if (kvp.Value != otherAmount)
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is Recipe otherRecipe)
                return Equals(otherRecipe);
            return false;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (var kvp in IngredientsDictionary.OrderBy(k => k.Key))
            {
                hash = hash * 31 + kvp.Key.GetHashCode();
                hash = hash * 31 + kvp.Value.GetHashCode();
            }
            return hash;
        }

        public static bool operator ==(Recipe left, Recipe right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(Recipe left, Recipe right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return string.Join(", ", IngredientsDictionary.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        }
    }
}
